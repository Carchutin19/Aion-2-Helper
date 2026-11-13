using System;
using System.Collections.Generic;
using System.Linq;

// Original passive decoder. Wire-layout research and artwork provenance are
// documented in assets/skill-slots/README.md. No casts or keys are injected.
internal sealed class SkillSlotsSignal {
 internal sealed class Slot {internal int Index;internal uint Skill,Alternative,Third,Fourth;internal double CastAt;internal Slot Copy(){return (Slot)MemberwiseClone();}}
 internal sealed class Cooldown {internal uint Raw,Peak;internal double At;internal bool FromCast;internal Cooldown Copy(){return (Cooldown)MemberwiseClone();}}
 internal sealed class View {internal uint Self,Map,ContextTarget;internal double FireMarkUntil,StunUntil,HeartGoreUntil,EvasionUntil,InfiltrateUntil;internal bool Flying;internal string Flow;internal long Revision;internal readonly List<Slot> Slots=new List<Slot>();internal readonly Dictionary<uint,Cooldown> Cooldowns=new Dictionary<uint,Cooldown>();internal readonly Dictionary<uint,uint> Chains=new Dictionary<uint,uint>();}
 internal sealed class BarFault {public string Field;public int Offset,Record,Kind,Slot;public int? Value;}
 sealed class Flow {internal readonly BarTrace Bar=new BarTrace();internal uint Self,Map,AwaitingSelf,LoginActor;internal int Server;internal string SelfName,AwaitingName;internal double Traffic,RawTraffic;internal double? FullWireTime;internal bool HasBar,FullWhileAwaiting,AwaitingProfile;internal readonly SkillSlotContext Context=new SkillSlotContext();internal readonly Dictionary<int,Slot> Slots=new Dictionary<int,Slot>();internal readonly Dictionary<int,Slot> PendingPartials=new Dictionary<int,Slot>();internal readonly Dictionary<uint,Cooldown> Cds=new Dictionary<uint,Cooldown>();internal readonly Dictionary<uint,uint> Chains=new Dictionary<uint,uint>();}
 sealed class BarTrace {
  public long FullReceived,FullAccepted,FullRejected,PartialReceived,PartialAccepted,PartialRejected,PartialWithoutFull,MapPauses,MapResumes,ConnectionResumes;
  public int LastLength,LastRejectedLength;public string LastOpcode,LastRejectedOpcode,LastClear;public double? LastAt,LastRejectedAt,ClearedAt,PausedAt,ResumedAt;public BarFault LastRejection;
 }
 long resets;string lastReset;
 // Runtime reception and the UI share a clock; recorded replays retain their
 // supplied timestamps. Npcap can drift ahead of UTC during long sessions.
 internal Func<double> ReceiptClock;
 readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();string active;Flow lastOwnProfile;bool enabled,flashCasts;long revision;View cached;internal Action Changed;
 // The visibility switch controls publication. Keep tracking the current
 // character while hidden, since the server may only send the full bar at login.
 internal void Configure(bool on,bool animateCasts=false){lock(gate){
  bool flashes=on&&animateCasts;
  if(enabled==on&&flashCasts==flashes)return;
  if(flashCasts&&!flashes)foreach(var flow in flows.Values)ClearCastFlashes(flow);
  enabled=on;flashCasts=flashes;cached=null;revision++;
 }}
 internal void Reset(){lock(gate){resets++;lastReset="transport reset";flows.Clear();active=null;lastOwnProfile=null;cached=null;revision++;}Notify();}
 void Notify(){var change=Changed;if(change!=null)change();}
 internal static uint Base(uint id){return id/10000*10000;}
 internal static bool Bar(byte[] b,int s,int e,out List<Slot> slots){BarFault fault;return Bar(b,s,e,out slots,out fault);}
 static bool Reject(out BarFault fault,string field,int offset,int record=-1,int kind=-1,int slot=-1,int? value=null){fault=new BarFault{Field=field,Offset=offset,Record=record,Kind=kind,Slot=slot,Value=value};return false;}
 internal static bool Bar(byte[] b,int s,int e,out List<Slot> slots,out BarFault fault){
  slots=new List<Slot>();fault=null;
  if(b==null||s<0||e>b.Length||e-s<3)return Reject(out fault,"header length",0);
  if(b[s+2]>64)return Reject(out fault,"entry count",2,value:b[s+2]);
  int p=s+3;var seen=new HashSet<int>();
  for(int i=0;i<b[s+2];i++){
   if(p+2>e)return Reject(out fault,"entry header length",p-s,i);
   int kind=b[p],slot=b[p+1],n=kind==1?42:kind==0?25:0;
   if(n==0)return Reject(out fault,"entry kind",p-s,i,kind,slot,kind);
   if(p+n>e)return Reject(out fault,"entry length",p-s,i,kind,slot,e-p);
   if(slot>47)return Reject(out fault,"slot range",p+1-s,i,kind,slot,slot);
   if(!seen.Add(slot))return Reject(out fault,"duplicate slot",p+1-s,i,kind,slot,slot);
   if(kind==1){
    for(int z=2;z<15;z++)if(b[p+z]!=0)return Reject(out fault,"skill prefix",p+z-s,i,kind,slot,b[p+z]);
    // The last ten bytes are opaque metadata, not a guaranteed zero padding.
    // Edeln's received0056 has a nonzero byte at record1+32. Skip the bounded
    // tail without assigning it skill, cooldown or availability semantics.
    uint id=BitConverter.ToUInt32(b,p+15);
    if(id==0){
     bool partial=b[s]==5&&b[s+1]==0x56;
     if(!partial||(b[p+19]!=0&&b[p+19]!=4)||BitConverter.ToUInt32(b,p+20)!=0||BitConverter.ToUInt64(b,p+24)!=0)
      return Reject(out fault,"empty skill ID",p+15-s,i,kind,slot);
     slots.Add(new Slot{Index=slot});p+=n;continue;
    }
    if(b[p+19]!=4)return Reject(out fault,"skill marker",p+19-s,i,kind,slot,b[p+19]);
    if(BitConverter.ToUInt32(b,p+20)!=id)return Reject(out fault,"skill ID mirror",p+20-s,i,kind,slot);
    slots.Add(new Slot{Index=slot,Skill=id,Alternative=BitConverter.ToUInt32(b,p+24),Third=BitConverter.ToUInt32(b,p+28),Fourth=FourthSkill(b,p)});
   }else{
    if(b[p+2]!=1)return Reject(out fault,"item marker",p+2-s,i,kind,slot,b[p+2]);
    for(int z=3;z<11;z++)if(b[p+z]!=0)return Reject(out fault,"item prefix",p+z-s,i,kind,slot,b[p+z]);
    for(int z=15;z<25;z++)if(b[p+z]!=0)return Reject(out fault,"item suffix",p+z-s,i,kind,slot,b[p+z]);
    slots.Add(new Slot{Index=slot});
   }p+=n;
  }
  return p==e||Reject(out fault,"trailing bytes",p-s,value:e-p);
 }
 // Row 3 follows rows 0..2 in the fixed-size skill record. Retain only
 // catalogue-recognized base skills; unrelated nonzero tail metadata is ignored.
 static uint FourthSkill(byte[] bytes,int at){uint id=BitConverter.ToUInt32(bytes,at+32);
  return id>=11000000&&id<20000000&&Base(id)==id&&SkillCatalogue.Shared.HasExact(id)?id:0;}
 internal static bool Cooldowns(byte[] b,int s,int e,out Dictionary<uint,uint> values){values=new Dictionary<uint,uint>();if(e-s<3||b[s+2]>128)return false;int p=s+3;for(int i=0;i<b[s+2];i++){if(p+5>e)return false;uint id=BitConverter.ToUInt32(b,p);p+=4;uint raw;if(id==0||values.ContainsKey(id)||!DpsSignal.Var(b,ref p,e,out raw)||raw>864000000)return false;values[id]=raw;}return p==e;}
 // A complete 0238 cast has a source actor, optional cast flags, target/yaw,
 // optional position, two opaque fields and a cooldown interval in milliseconds.
 // Interpret only this bounded layout, never search the tail for plausible times.
 internal static bool Cast(byte[] b,int s,int e,out uint actor,out uint skill,out uint milliseconds,out bool chargeLayout){actor=skill=milliseconds=0;chargeLayout=false;if(e-s<15||e-s>256)return false;int p=s+2;uint opaque;if(!DpsSignal.Var(b,ref p,e,out actor)||actor==0||p+6>e)return false;byte flags=b[p++];if((flags&0xC0)!=0)return false;skill=BitConverter.ToUInt32(b,p);p+=5;if(skill==0)return false;if((flags&1)!=0){if(p>=e)return false;p++;}if(p>=e)return false;byte targetFlags=b[p++];if((targetFlags&0xFC)!=0||!DpsSignal.Var(b,ref p,e,out opaque))return false;if((targetFlags&1)!=0){if(p+4>e)return false;p+=4;}int floats=(targetFlags&2)!=0?4:1;if(p+4*floats>e)return false;for(int n=0;n<floats;n++){float f=BitConverter.ToSingle(b,p);if(float.IsNaN(f)||float.IsInfinity(f)||Math.Abs(f)>100000000)return false;p+=4;}if(!DpsSignal.Var(b,ref p,e,out opaque)||p>=e)return false;p++;if((flags&2)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;if(!DpsSignal.Var(b,ref p,e,out milliseconds)||milliseconds>86400000)return false;if((flags&4)!=0){if(p>=e)return false;p++;}if((flags&8)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;if((flags&16)!=0){if(p>=e)return false;p++;}if((flags&32)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;chargeLayout=(flags&12)!=0;return p==e;}
 static uint CooldownKey(uint id){return id>=11000000&&id<20000000?Base(id):id;}
 static void ClearCastFlashes(Flow flow){foreach(var slot in flow.Slots.Values)slot.CastAt=0;}
 static void ApplySlots(Flow flow,IEnumerable<Slot> entries){
  foreach(var slot in entries){if(slot.Skill==0)flow.Slots.Remove(slot.Index);else flow.Slots[slot.Index]=slot;}
 }
 void ResumeAssignments(Flow flow,int server,string name){
  // Dungeon exits can open a new flow and send only 0556 updates. Adopt the
  // last confirmed layout after a matching own profile, never combat state.
  var prior=lastOwnProfile;
  if(flow.HasBar||flow.LoginActor!=0||server==0||prior==null||prior==flow||!prior.HasBar
      ||prior.Server!=server||!string.Equals(prior.SelfName??prior.AwaitingName,name,StringComparison.Ordinal))return;
  foreach(var pair in prior.Slots){
   var slot=pair.Value.Copy();
   slot.CastAt=0;
   flow.Slots[pair.Key]=slot;
  }
  ApplySlots(flow,flow.PendingPartials.Values);
  flow.PendingPartials.Clear();
  flow.HasBar=true;
  flow.Bar.ConnectionResumes++;
  flow.Bar.ResumedAt=flow.Traffic;
 }
 static bool MarkOwnCast(Flow flow,uint skill,double time){
  if(!flow.HasBar)return false;
  uint key=CooldownKey(skill),component=skill%100;
  bool family=component<7||component>9;
  Slot found=null;
  foreach(var slot in flow.Slots.Values){
   uint chain;flow.Chains.TryGetValue(Base(slot.Skill),out chain);
   foreach(uint assigned in new[]{slot.Alternative,slot.Third,slot.Fourth}){uint following;if(flow.Chains.TryGetValue(Base(assigned),out following)&&Base(following)==key)chain=following;}
   if(skill!=slot.Skill&&skill!=slot.Alternative&&skill!=slot.Third&&skill!=slot.Fourth&&skill!=chain
       &&(!family||key!=CooldownKey(slot.Skill)&&key!=CooldownKey(slot.Alternative)
       &&key!=CooldownKey(slot.Third)&&key!=CooldownKey(slot.Fourth)&&key!=CooldownKey(chain)))continue;
   // A cast carries a skill ID but no bar position. Avoid flashing two slots
   // when the same skill is assigned more than once.
   if(found!=null)return false;
   found=slot;
  }
  // Do not extend one flash through rapid components of the same slot.
  if(found==null||found.CastAt>0&&time-found.CastAt<.15)return false;
  found.CastAt=time;return true;
 }
 static void Interval(Flow f,uint id,uint milliseconds,double time,bool cast){id=CooldownKey(id);Cooldown old;if(!f.Cds.TryGetValue(id,out old)){if(f.Cds.Count>=512){var first=f.Cds.OrderBy(x=>x.Value.At).First();f.Cds.Remove(first.Key);}f.Cds[id]=old=new Cooldown();}
 // A zero-duration component of a multi-stage cast does not reset a running CD.
 // An explicit 4738 zero does reset it, as does natural interval expiry.
 if(cast&&milliseconds==0&&(old.Raw>0||old.FromCast))return;
 old.Peak=cast?milliseconds:milliseconds==0?0:Math.Max(old.Peak,milliseconds);old.Raw=milliseconds;old.At=time;old.FromCast=cast;}
 internal void Consume(byte[] b,int s,int e,double time,string key){if(b==null||s<0||e>b.Length||e-s<2||e-s>65536||string.IsNullOrEmpty(key)||double.IsNaN(time)||double.IsInfinity(time))return;double rawTime=time;if(ReceiptClock!=null){time=ReceiptClock();if(double.IsNaN(time)||double.IsInfinity(time))return;}int op=b[s]*256+b[s+1];bool changed=false;lock(gate){Flow f;if(!flows.TryGetValue(key,out f)){if(op!=0x3336&&op!=0x2036&&op!=0x2136&&op!=0x0056&&op!=0x0556)return;if(flows.Count>=4){var old=flows.Where(x=>x.Key!=active).OrderBy(x=>x.Value.Traffic).FirstOrDefault();if(old.Key==null)return;flows.Remove(old.Key);}flows[key]=f=new Flow();}if(time<f.Traffic)return;f.Traffic=time;f.RawTraffic=rawTime;
 if(op==0x2036){
  uint actor;if(!DashSignal.LoginActor(b,s,e,out actor))return;
  lastOwnProfile=null;
  foreach(var flow in flows.Values)flow.PendingPartials.Clear();
  f.LoginActor=actor;f.AwaitingProfile=true;
  f.Self=f.AwaitingSelf=0;f.SelfName=f.AwaitingName=null;f.Server=0;
  f.Slots.Clear();f.HasBar=f.FullWhileAwaiting=false;f.FullWireTime=null;
  f.Cds.Clear();f.Chains.Clear();f.Context.Clear();
  f.Bar.LastClear="character login";f.Bar.ClearedAt=time;
  if(active==key)active=null;changed=true;
 }
 else if(op==0x2136&&e-s==48){uint map=BitConverter.ToUInt32(b,s+6);if(map==0||map>=10000000)return;
  // Even a same-map character switch has a validated entry marker. A new0056
  // may precede3336 in a different TCP segment; stage it against this boundary,
  // never against a timestamp tolerance or the previous character's layout.
  f.AwaitingProfile=true;f.FullWhileAwaiting=false;
  if(f.Map!=0&&f.Map!=map){
  // A map change does not necessarily resend0056 (recorded Pesadilla entry
  // and exit). Quarantine the received assignments/CD anchors until3336
  // confirms the same actor and name on this TCP flow; never publish while unconfirmed.
  if(f.Self!=0){f.AwaitingSelf=f.Self;f.AwaitingName=f.SelfName;}f.FullWhileAwaiting=false;f.Bar.MapPauses++;f.Bar.PausedAt=time;
  f.Chains.Clear();f.Context.Clear();f.PendingPartials.Clear();ClearCastFlashes(f);f.Self=0;if(active==key)active=null;changed=true;
 }f.Map=map;}
 else if(op==0x3336){uint actor;string name;int server;byte role;if(PetSignal.Identity(b,s,e,out name,out server,out actor)||DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role)){if(f.LoginActor!=0&&f.LoginActor!=actor)return;uint previous=f.Self!=0?f.Self:f.AwaitingSelf;string previousName=f.Self!=0?f.SelfName:f.AwaitingName;bool resuming=f.AwaitingSelf!=0;
  if(previous!=0&&(previous!=actor||!string.Equals(previousName,name,StringComparison.Ordinal)||server!=0&&f.Server!=0&&f.Server!=server)){
   f.Bar.LastClear="own actor changed";f.Bar.ClearedAt=time;f.Cds.Clear();f.Chains.Clear();f.Context.Clear();ClearCastFlashes(f);
   f.PendingPartials.Clear();
   // A fresh full bar received during loading is authoritative for the new
   // profile. Partial updates alone must never transfer the previous layout.
   if(!(f.FullWhileAwaiting&&(resuming||f.AwaitingProfile))&&f.FullWireTime!=rawTime){f.Slots.Clear();f.HasBar=false;}
  }else if(resuming&&f.HasBar){f.Bar.MapResumes++;f.Bar.ResumedAt=time;}
  else if(previous==0)ResumeAssignments(f,server,name);
  f.AwaitingSelf=f.LoginActor=0;f.AwaitingName=null;f.FullWhileAwaiting=f.AwaitingProfile=false;f.Self=actor;f.SelfName=name;f.Server=server;f.Context.Enabled=f.Slots.Values.Any(HasContext);active=key;lastOwnProfile=f;changed=true;}}
 else if(op==0x0056||op==0x0556){
  var trace=f.Bar;bool full=op==0x0056;if(full)trace.FullReceived++;else trace.PartialReceived++;
  trace.LastOpcode=full?"0056":"0556";trace.LastLength=e-s;trace.LastAt=time;
  List<Slot> entries;BarFault fault;
  if(!Bar(b,s,e,out entries,out fault)){if(full)trace.FullRejected++;else trace.PartialRejected++;trace.LastRejection=fault;trace.LastRejectedOpcode=trace.LastOpcode;trace.LastRejectedLength=e-s;trace.LastRejectedAt=time;return;}
  if(full){trace.FullAccepted++;f.FullWireTime=rawTime;f.Slots.Clear();f.PendingPartials.Clear();f.HasBar=true;if(f.AwaitingSelf!=0||f.AwaitingProfile){f.FullWhileAwaiting=true;if(active==key)active=null;}}
  if(!f.HasBar){trace.PartialWithoutFull++;foreach(var slot in entries)f.PendingPartials[slot.Index]=slot;return;}
  if(!full)trace.PartialAccepted++;
  if(SupportDiagnostics.Enabled)SupportDiagnostics.Event("hotbar-assignments",new{full,actor=f.Self,map=f.Map,
      slots=entries.Select(slot=>new{slot.Index,slot.Skill,slot.Alternative,slot.Third,slot.Fourth}).ToArray()});
  ApplySlots(f,entries);
  f.Context.Enabled=f.Slots.Values.Any(HasContext);changed=true;
 }
 else if(f.Self!=0&&active==key&&SkillSlotContext.Handles(op)){changed=f.Context.Consume(b,s,e,time,f.Self);}
 else if(f.Self!=0&&active==key&&op==0x0238){
  uint actor,skill,ms;bool charges;
  if(Cast(b,s,e,out actor,out skill,out ms,out charges)&&actor==f.Self){
   if(SupportDiagnostics.Enabled)SupportDiagnostics.Event("hotbar-own-cast",new{actor,skill,ms,charges,map=f.Map});
   if(!charges){Interval(f,skill,ms,time,true);changed=true;}
   if(flashCasts&&MarkOwnCast(f,skill,time))changed=true;
  }
 }
 else if(f.Self!=0&&active==key&&op==0x4738){Dictionary<uint,uint> entries;if(!Cooldowns(b,s,e,out entries))return;foreach(var pair in entries)Interval(f,pair.Key,pair.Value,time,false);changed=true;}
 else if(f.Self!=0&&active==key&&op==0x1838&&e-s==11&&b[s+6]==1){uint first=BitConverter.ToUInt32(b,s+2),next=BitConverter.ToUInt32(b,s+7);if(first!=0&&next!=0){uint previous=Base(first);foreach(var root in f.Chains.Where(x=>Base(x.Value)==previous).Select(x=>x.Key).ToArray())f.Chains[root]=next;if(f.Chains.ContainsKey(previous)||f.Chains.Count<128)f.Chains[previous]=next;if(SupportDiagnostics.Enabled)SupportDiagnostics.Event("hotbar-chain-open",new{first,next,map=f.Map});changed=true;}}
 // An entry means the CURRENT available destination of that root. Opening
 // B->C advances A->B to A->C. Closing B must not erase the already-open C.
 else if(f.Self!=0&&active==key&&op==0x1938&&e-s==7&&b[s+2]==1){uint id=Base(BitConverter.ToUInt32(b,s+3));foreach(var root in f.Chains.Where(x=>Base(x.Value)==id).Select(x=>x.Key).ToArray())f.Chains.Remove(root);if(SupportDiagnostics.Enabled)SupportDiagnostics.Event("hotbar-chain-close",new{id,map=f.Map});changed=true;}
 if(changed){revision++;cached=null;}changed=changed&&enabled;}
 if(changed)Notify();}
 internal View Read(double now){lock(gate){Flow f;if(!enabled||active==null||!flows.TryGetValue(active,out f)||f.Self==0||!f.HasBar||now+.25<f.Traffic||now-f.Traffic>15)return null;if(cached!=null)return cached;var v=new View{Self=f.Self,Map=f.Map,Flow=active,Revision=revision,ContextTarget=f.Context.Target,FireMarkUntil=f.Context.Until,StunUntil=f.Context.UntilFor(f.Context.Target,SkillSlotContext.Stun),HeartGoreUntil=f.Context.UntilFor(f.Self,SkillSlotContext.HeartGore),EvasionUntil=f.Context.EvasionUntil(f.Self),InfiltrateUntil=f.Context.UntilFor(f.Self,SkillSlotContext.InfiltrateWindow),Flying=f.Context.UntilFor(f.Self,SkillSlotContext.Flight)>0};foreach(var slot in f.Slots.Values.OrderBy(x=>x.Index))v.Slots.Add(slot.Copy());foreach(var p in f.Cds)v.Cooldowns[p.Key]=p.Value.Copy();foreach(var p in f.Chains)v.Chains[p.Key]=p.Value;return cached=v;}}
 internal static Cooldown For(View v,uint id){Cooldown c;return v!=null&&(v.Cooldowns.TryGetValue(id,out c)||v.Cooldowns.TryGetValue(Base(id),out c))?c:null;}
 // Both intervals are milliseconds: own Hellfire 41600ms corresponds to the
 // user's native42s display; later corrections replace the same time anchor.
 internal static double Remaining(Cooldown cd,double now){return cd==null?0:Math.Max(0,cd.Raw/1000.0-Math.Max(0,now-cd.At));}
 // Bounded metadata only: no packet payloads, endpoints, keys or chat are retained.
 internal object Diagnostic(double now){lock(gate){Flow f=active!=null&&flows.ContainsKey(active)?flows[active]:null;return new{barDiagnosticsVersion=6,transportFlowId=SupportDiagnostics.FlowId(active),receptionClock=ReceiptClock!=null,enabled,flows=flows.Count,ownActor=f==null?(uint?)null:f.Self,map=f==null?(uint?)null:f.Map,activeBar=f!=null&&f.HasBar,secondsSincePacket=f==null?(double?)null:Math.Max(0,now-f.Traffic),packetClockLead=f==null?(double?)null:Math.Max(0,f.Traffic-now),captureClockLead=f==null?(double?)null:f.RawTraffic-now,resets,lastReset,bar=f==null?null:f.Bar,barFlows=flows.Select(p=>new{active=p.Key==active,ownActor=p.Value.Self,awaitingOwnActor=p.Value.AwaitingSelf,map=p.Value.Map,activeBar=p.Value.HasBar,slots=p.Value.Slots.Count,bar=p.Value.Bar}).ToArray()};}}
 internal static bool IsFireContext(Slot slot){return Base(slot.Skill)==15280000&&Base(slot.Alternative)==15050000&&slot.Third==0;}
 static bool IsShadowFallContext(Slot slot){return Base(slot.Skill)==13360000&&Base(slot.Alternative)==13220000;}
 static uint AssignedVariant(Slot slot,uint skill){
  if(Base(slot.Alternative)==skill)return slot.Alternative;
  if(Base(slot.Third)==skill)return slot.Third;return Base(slot.Fourth)==skill?slot.Fourth:0;
 }
 static bool HasContext(Slot slot){return IsFireContext(slot)||IsShadowFallContext(slot)||
  Base(slot.Skill)==13340000||Base(slot.Skill)==13360000||
  AssignedVariant(slot,13210000)!=0||AssignedVariant(slot,13350000)!=0;}
 // Verified independent activations. This is skill metadata, not a slot pair
 // or character preset. An unclassified alternative may require an event.
 static bool UsesCooldownOnly(uint skill){
  uint id=Base(skill);
  return id==13390000||id==13310000||id==13270000||id==13300000||id==13100000;
 }
 internal static bool? Requirement(View view,Slot slot,uint skill,double now,StaggerSignal stagger){
  if(view==null)return null;
  uint current=Base(skill);
  // These two base skills are reactive even when no activation is open.
  if(current==13340000)return stagger!=null&&stagger.IsTargetStaggered(view.Flow,view.ContextTarget,now);
  if(current==13360000)return view.Flying||now<view.InfiltrateUntil;
  if(IsFireContext(slot)&&current==15050000&&view.ContextTarget!=0&&now<view.FireMarkUntil&&Remaining(For(view,slot.Skill),now)>0)return true;
  if(IsShadowFallContext(slot)&&current==13220000&&view.ContextTarget!=0&&now<view.StunUntil)return true;
  if(AssignedVariant(slot,13210000)!=0&&current==13210000&&now<view.EvasionUntil)return true;
  if(AssignedVariant(slot,13350000)!=0&&current==13350000&&now<view.HeartGoreUntil)return true;
  return null;
 }
 static uint Chained(View view,uint id){uint next;return view.Chains.TryGetValue(Base(id),out next)?next:id;}
 internal static uint DisplaySkill(View view,Slot slot){return DisplaySkill(view,slot,DpsHistory.UtcNow());}
 internal static uint DisplaySkill(View view,Slot slot,double now){uint next;if(view!=null&&view.Chains.TryGetValue(Base(slot.Skill),out next))return next;
 // This specific conditional pair is verified by assigned bar IDs, recorded Fire
 // Mark effects on the selected target and the user's native-bar observation.
 // No generic "switch whenever on cooldown" fallback for unverified contexts.
 if(view!=null){
  if(IsFireContext(slot)&&view.ContextTarget!=0&&now<view.FireMarkUntil&&Remaining(For(view,slot.Skill),now)>0)return slot.Alternative;
  if(IsShadowFallContext(slot)&&view.ContextTarget!=0&&now<view.StunUntil&&Remaining(For(view,slot.Alternative),now)<=0)return slot.Alternative;
  uint whirlwind=AssignedVariant(slot,13210000);
  if(whirlwind!=0&&now<view.EvasionUntil&&Remaining(For(view,whirlwind),now)<=0)return whirlwind;
  uint heart=AssignedVariant(slot,13350000);
  if(heart!=0&&now<view.HeartGoreUntil&&Remaining(For(view,heart),now)<=0)return heart;
  if(Remaining(For(view,slot.Skill),now)>0){
   if(UsesCooldownOnly(slot.Alternative)&&Remaining(For(view,slot.Alternative),now)<=0)return Chained(view,slot.Alternative);
   if(UsesCooldownOnly(slot.Third)&&Remaining(For(view,slot.Third),now)<=0)return Chained(view,slot.Third);
   if(UsesCooldownOnly(slot.Fourth)&&Remaining(For(view,slot.Fourth),now)<=0)return Chained(view,slot.Fourth);
  }
 }
 return slot.Skill;}
}

internal sealed class SkillSlotContext {
 internal const uint FireMark=157100071,Stun=100000301,HeartGore=133500081,EvasionMarkerA=1006,EvasionMarkerB=1007,InfiltrateWindow=3015,Flight=100000002;
 sealed class Mark {internal uint Target,Effect;internal double Until;internal long ServerExpiry;}
 readonly Dictionary<ulong,Mark> marks=new Dictionary<ulong,Mark>();internal uint Target;internal bool Enabled;double clockOffset,clockAt;bool clockKnown;
 internal void Clear(){marks.Clear();Target=0;Enabled=false;clockKnown=false;clockOffset=clockAt=0;}
 internal double Until {get{return UntilFor(Target,FireMark);}}
 internal double UntilFor(uint actor,uint effect){
  double until=0;
  if(actor!=0)foreach(var m in marks.Values)if(m.Target==actor&&m.Effect==effect)until=Math.Max(until,m.Until);
  return until;
 }
 internal double EvasionUntil(uint actor){
  // Successful evasions produced both effects with the same server expiry.
  Mark first=null,second=null;
  foreach(var mark in marks.Values){
   if(mark.Target!=actor)continue;
   if(mark.Effect==EvasionMarkerA&&(first==null||mark.ServerExpiry>first.ServerExpiry))first=mark;
   if(mark.Effect==EvasionMarkerB&&(second==null||mark.ServerExpiry>second.ServerExpiry))second=mark;
  }
  return first!=null&&second!=null&&first.ServerExpiry==second.ServerExpiry?Math.Min(first.Until,second.Until):0;
 }
 internal static bool Handles(int op){return op==0x0036||op==0x3138||op==0x3438||op==0x2a38||op==0x2b38||op==0x2c38||op==0x4236;}
 static ulong Key(uint actor,uint instance){return ((ulong)actor<<32)|instance;}
 internal bool Consume(byte[] b,int s,int e,double time,uint self=0){if(!Enabled)return false;int op=b[s]*256+b[s+1],p=s+2;uint actor;
 // The incoming server UTC clock and local capture clock can differ by seconds.
 // Map effect deadlines through a recent clock from this same confirmed flow.
 if(op==0x0036){if(e-s!=10)return false;long epoch=BitConverter.ToInt64(b,s+2);if(epoch<946684800000L||epoch>4102444800000L)return false;clockOffset=time-epoch/1000.0;clockAt=time;clockKnown=true;return false;}
 if(op==0x3138){if(e-s<7||b[p++]!=0||b[p++]!=0||!DpsSignal.Var(b,ref p,e,out actor)||actor>10000000||e-p!=2||b[p]!=0||b[p+1]!=0)return false;bool changed=Target!=actor;Target=actor;return changed;}
 if(op==0x3438){if(!EnemyHealthSignal.Selection(b,s,e,out actor))return false;bool changed=Target!=actor;Target=actor;return changed;}
 if(op==0x2a38||op==0x2b38){List<PlayerShieldSignal.Status> statuses;if(!PlayerShieldSignal.ReadStatuses(b,s,e,out actor,out statuses))return false;bool changed=false;foreach(var status in statuses){ulong key=Key(actor,status.Instance);if(marks.Remove(key))changed=true;
 // Opening wings adds a persistent own effect. Folding them explicitly removes
 // its instance, before landing or the first flight-energy recharge tick.
 if(!status.Removing&&status.Effect==Flight&&actor==self&&status.Source==self&&status.Skill==0&&status.Duration==-1){
  Store(key,actor,status,double.PositiveInfinity,time);changed=true;continue;
 }
 // Absolute server expiry keeps repeated snapshots from extending availability.
 bool evasion=actor==self&&status.Skill==0&&(status.Effect==EvasionMarkerA||status.Effect==EvasionMarkerB);
 bool supported=evasion||status.Effect==FireMark||status.Effect==Stun&&actor!=self||status.Effect==HeartGore&&actor==self&&status.Skill==13350008
     ||status.Effect==InfiltrateWindow&&actor==self&&status.Skill==13000100&&status.Source==self;
 if(status.Removing||!supported||status.Duration<=0||status.Duration>60000)continue;if(!clockKnown||time-clockAt>2)continue;double until=status.Expiry/1000.0+clockOffset;if(until<=time||until>time+status.Duration/1000.0+.25)continue;
 Store(key,actor,status,until,time);changed=true;}return changed;}
 if(op==0x2c38){uint count;if(!DpsSignal.Var(b,ref p,e,out actor)||!DpsSignal.Var(b,ref p,e,out count)||count>128)return false;var removed=new List<uint>();for(uint n=0;n<count;n++){if(p>=e)return false;int flags=b[p++];uint instance,unused;if(flags>7||!DpsSignal.Var(b,ref p,e,out instance)||p>=e)return false;p++;if((flags&1)!=0&&!DpsSignal.Var(b,ref p,e,out unused))return false;int skip=((flags&2)!=0?4:0)+((flags&4)!=0?4:0);if(e-p<skip)return false;p+=skip;removed.Add(instance);}if(p!=e)return false;bool changed=false;foreach(uint instance in removed)changed=marks.Remove(Key(actor,instance))||changed;return changed;}
 if(op==0x4236&&DpsSignal.Var(b,ref p,e,out actor)&&e-p==2&&b[p]==0){bool changed=false;foreach(var key in marks.Where(x=>x.Value.Target==actor).Select(x=>x.Key).ToArray()){marks.Remove(key);changed=true;}if(Target==actor){Target=0;changed=true;}return changed;}return false;
 }
 void Store(ulong key,uint actor,PlayerShieldSignal.Status status,double until,double time){
  foreach(var expired in marks.Where(x=>x.Value.Until<=time).Select(x=>x.Key).ToArray())marks.Remove(expired);
  if(marks.Count>=512)marks.Remove(marks.OrderBy(x=>x.Value.Until).First().Key);
  marks[key]=new Mark{Target=actor,Effect=status.Effect,Until=until,ServerExpiry=status.Expiry};
 }
}
