using System;
using System.Collections.Generic;
using System.Linq;

// Original passive decoder. Wire-layout research and artwork provenance are
// documented in assets/skill-slots/README.md. No casts or keys are injected.
internal sealed class SkillSlotsSignal {
 internal sealed class Slot {internal int Index;internal uint Skill,Alternative,Third;internal Slot Copy(){return (Slot)MemberwiseClone();}}
 internal sealed class Cooldown {internal uint Raw,Peak;internal double At;internal bool FromCast;internal Cooldown Copy(){return (Cooldown)MemberwiseClone();}}
 internal sealed class View {internal uint Self,Map,ContextTarget;internal double FireMarkUntil;internal string Flow;internal long Revision;internal readonly List<Slot> Slots=new List<Slot>();internal readonly Dictionary<uint,Cooldown> Cooldowns=new Dictionary<uint,Cooldown>();internal readonly Dictionary<uint,uint> Chains=new Dictionary<uint,uint>();}
 sealed class Flow {internal uint Self,Map;internal double Traffic;internal bool HasBar;internal readonly SkillSlotContext Context=new SkillSlotContext();internal readonly Dictionary<int,Slot> Slots=new Dictionary<int,Slot>();internal readonly Dictionary<uint,Cooldown> Cds=new Dictionary<uint,Cooldown>();internal readonly Dictionary<uint,uint> Chains=new Dictionary<uint,uint>();}
 readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();string active;bool enabled;long revision;View cached;internal Action Changed;
 internal void Configure(bool on){lock(gate){if(enabled==on)return;enabled=on;if(!on){flows.Clear();active=null;}cached=null;revision++;}}
 internal void Reset(){lock(gate){flows.Clear();active=null;cached=null;revision++;}Notify();}
 void Notify(){var change=Changed;if(change!=null)change();}
 internal static uint Base(uint id){return id/10000*10000;}
 internal static bool Bar(byte[] b,int s,int e,out List<Slot> slots){slots=new List<Slot>();if(e-s<3||b[s+2]>64)return false;int p=s+3;var seen=new HashSet<int>();for(int i=0;i<b[s+2];i++){if(p+2>e)return false;int kind=b[p],n=kind==1?42:kind==0?25:0;if(n==0||p+n>e||b[p+1]>47||!seen.Add(b[p+1]))return false;if(kind==1){for(int z=2;z<15;z++)if(b[p+z]!=0)return false;for(int z=32;z<42;z++)if(b[p+z]!=0)return false;uint a=BitConverter.ToUInt32(b,p+15);if(a==0||b[p+19]!=4||BitConverter.ToUInt32(b,p+20)!=a)return false;slots.Add(new Slot{Index=b[p+1],Skill=a,Alternative=BitConverter.ToUInt32(b,p+24),Third=BitConverter.ToUInt32(b,p+28)});}else{if(b[p+2]!=1)return false;for(int z=3;z<11;z++)if(b[p+z]!=0)return false;for(int z=15;z<25;z++)if(b[p+z]!=0)return false;slots.Add(new Slot{Index=b[p+1]});}p+=n;}return p==e;}
 internal static bool Cooldowns(byte[] b,int s,int e,out Dictionary<uint,uint> values){values=new Dictionary<uint,uint>();if(e-s<3||b[s+2]>128)return false;int p=s+3;for(int i=0;i<b[s+2];i++){if(p+5>e)return false;uint id=BitConverter.ToUInt32(b,p);p+=4;uint raw;if(id==0||values.ContainsKey(id)||!DpsSignal.Var(b,ref p,e,out raw)||raw>864000000)return false;values[id]=raw;}return p==e;}
 // A complete 0238 cast has a source actor, optional cast flags, target/yaw,
 // optional position, two opaque fields and a cooldown interval in milliseconds.
 // Interpret only this bounded layout, never search the tail for plausible times.
 internal static bool Cast(byte[] b,int s,int e,out uint actor,out uint skill,out uint milliseconds,out bool chargeLayout){actor=skill=milliseconds=0;chargeLayout=false;if(e-s<15||e-s>256)return false;int p=s+2;uint opaque;if(!DpsSignal.Var(b,ref p,e,out actor)||actor==0||p+6>e)return false;byte flags=b[p++];if((flags&0xC0)!=0)return false;skill=BitConverter.ToUInt32(b,p);p+=5;if(skill==0)return false;if((flags&1)!=0){if(p>=e)return false;p++;}if(p>=e)return false;byte targetFlags=b[p++];if((targetFlags&0xFC)!=0||!DpsSignal.Var(b,ref p,e,out opaque))return false;if((targetFlags&1)!=0){if(p+4>e)return false;p+=4;}int floats=(targetFlags&2)!=0?4:1;if(p+4*floats>e)return false;for(int n=0;n<floats;n++){float f=BitConverter.ToSingle(b,p);if(float.IsNaN(f)||float.IsInfinity(f)||Math.Abs(f)>100000000)return false;p+=4;}if(!DpsSignal.Var(b,ref p,e,out opaque)||p>=e)return false;p++;if((flags&2)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;if(!DpsSignal.Var(b,ref p,e,out milliseconds)||milliseconds>86400000)return false;if((flags&4)!=0){if(p>=e)return false;p++;}if((flags&8)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;if((flags&16)!=0){if(p>=e)return false;p++;}if((flags&32)!=0&&!DpsSignal.Var(b,ref p,e,out opaque))return false;chargeLayout=(flags&12)!=0;return p==e;}
 static uint CooldownKey(uint id){return id>=11000000&&id<20000000?Base(id):id;}
 static void Interval(Flow f,uint id,uint milliseconds,double time,bool cast){id=CooldownKey(id);Cooldown old;if(!f.Cds.TryGetValue(id,out old)){if(f.Cds.Count>=512){var first=f.Cds.OrderBy(x=>x.Value.At).First();f.Cds.Remove(first.Key);}f.Cds[id]=old=new Cooldown();}
 // A zero-duration component of a multi-stage cast does not reset a running CD.
 // An explicit 4738 zero does reset it, as does natural interval expiry.
 if(cast&&milliseconds==0&&(old.Raw>0||old.FromCast))return;
 old.Peak=cast?milliseconds:milliseconds==0?0:Math.Max(old.Peak,milliseconds);old.Raw=milliseconds;old.At=time;old.FromCast=cast;}
 internal void Consume(byte[] b,int s,int e,double time,string key){if(b==null||s<0||e>b.Length||e-s<2||e-s>65536||string.IsNullOrEmpty(key)||double.IsNaN(time)||double.IsInfinity(time))return;int op=b[s]*256+b[s+1];bool changed=false;lock(gate){Flow f;if(!flows.TryGetValue(key,out f)){if(op!=0x3336&&op!=0x2136&&op!=0x0056&&op!=0x0556)return;if(flows.Count>=4){var old=flows.Where(x=>x.Key!=active).OrderBy(x=>x.Value.Traffic).FirstOrDefault();if(old.Key==null)return;flows.Remove(old.Key);}flows[key]=f=new Flow();}if(time<f.Traffic)return;f.Traffic=time;
 if(op==0x2136&&e-s==48){uint map=BitConverter.ToUInt32(b,s+6);if(map==0||map>=10000000)return;if(f.Map!=0&&f.Map!=map){f.Slots.Clear();f.Cds.Clear();f.Chains.Clear();f.Context.Clear();f.HasBar=false;f.Self=0;if(active==key)active=null;changed=true;}f.Map=map;}
 else if(op==0x3336){uint actor;string name;byte role;if(DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role)){if(f.Self!=0&&f.Self!=actor){f.Slots.Clear();f.Cds.Clear();f.Chains.Clear();f.Context.Clear();f.HasBar=false;}f.Self=actor;active=key;changed=true;}}
 else if(op==0x0056||op==0x0556){List<Slot> entries;if(!Bar(b,s,e,out entries))return;if(op==0x0056){f.Slots.Clear();f.HasBar=true;}if(!f.HasBar)return;foreach(var slot in entries){if(slot.Skill==0)f.Slots.Remove(slot.Index);else f.Slots[slot.Index]=slot;}f.Context.Enabled=f.Slots.Values.Any(IsFireContext);changed=true;}
 else if(f.Self!=0&&active==key&&SkillSlotContext.Handles(op)){changed=f.Context.Consume(b,s,e,time);}
 else if(f.Self!=0&&active==key&&op==0x0238){uint actor,skill,ms;bool charges;if(Cast(b,s,e,out actor,out skill,out ms,out charges)&&actor==f.Self&&!charges){Interval(f,skill,ms,time,true);changed=true;}}
 else if(f.Self!=0&&active==key&&op==0x4738){Dictionary<uint,uint> entries;if(!Cooldowns(b,s,e,out entries))return;foreach(var pair in entries)Interval(f,pair.Key,pair.Value,time,false);changed=true;}
 else if(f.Self!=0&&active==key&&op==0x1838&&e-s==11&&b[s+6]==1){uint first=BitConverter.ToUInt32(b,s+2),next=BitConverter.ToUInt32(b,s+7);if(first!=0&&next!=0){uint previous=Base(first);foreach(var root in f.Chains.Where(x=>Base(x.Value)==previous).Select(x=>x.Key).ToArray())f.Chains[root]=next;if(f.Chains.ContainsKey(previous)||f.Chains.Count<128)f.Chains[previous]=next;changed=true;}}
 // An entry means the CURRENT available destination of that root. Opening
 // B->C advances A->B to A->C. Closing B must not erase the already-open C.
 else if(f.Self!=0&&active==key&&op==0x1938&&e-s==7&&b[s+2]==1){uint id=Base(BitConverter.ToUInt32(b,s+3));foreach(var root in f.Chains.Where(x=>Base(x.Value)==id).Select(x=>x.Key).ToArray())f.Chains.Remove(root);changed=true;}
 if(changed){revision++;cached=null;}}
 if(changed)Notify();}
 internal View Read(double now){lock(gate){Flow f;if(!enabled||active==null||!flows.TryGetValue(active,out f)||f.Self==0||!f.HasBar||now+.25<f.Traffic||now-f.Traffic>15)return null;if(cached!=null)return cached;var v=new View{Self=f.Self,Map=f.Map,Flow=active,Revision=revision,ContextTarget=f.Context.Target,FireMarkUntil=f.Context.Until};foreach(var slot in f.Slots.Values.OrderBy(x=>x.Index))v.Slots.Add(slot.Copy());foreach(var p in f.Cds)v.Cooldowns[p.Key]=p.Value.Copy();foreach(var p in f.Chains)v.Chains[p.Key]=p.Value;return cached=v;}}
 internal static Cooldown For(View v,uint id){Cooldown c;return v!=null&&(v.Cooldowns.TryGetValue(id,out c)||v.Cooldowns.TryGetValue(Base(id),out c))?c:null;}
 // Both intervals are milliseconds: own Hellfire 41600ms corresponds to the
 // user's native42s display; later corrections replace the same time anchor.
 internal static double Remaining(Cooldown cd,double now){return cd==null?0:Math.Max(0,cd.Raw/1000.0-Math.Max(0,now-cd.At));}
 internal object Diagnostic(double now){lock(gate){Flow f=active!=null&&flows.ContainsKey(active)?flows[active]:null;return new{enabled,flows=flows.Count,ownActor=f==null?(uint?)null:f.Self,map=f==null?(uint?)null:f.Map,activeBar=f!=null&&f.HasBar,secondsSincePacket=f==null?(double?)null:Math.Max(0,now-f.Traffic),packetClockLead=f==null?(double?)null:Math.Max(0,f.Traffic-now)};}}
 internal static bool IsFireContext(Slot slot){return Base(slot.Skill)==15280000&&Base(slot.Alternative)==15050000&&slot.Third==0;}
 internal static uint DisplaySkill(View view,Slot slot){return DisplaySkill(view,slot,DpsHistory.UtcNow());}
 internal static uint DisplaySkill(View view,Slot slot,double now){uint next;if(view!=null&&view.Chains.TryGetValue(Base(slot.Skill),out next))return next;
 // This specific conditional pair is verified by assigned bar IDs, recorded Fire
 // Mark effects on the selected target and the user's native-bar observation.
 // No generic "switch whenever on cooldown" fallback for unverified contexts.
 if(view!=null&&IsFireContext(slot)&&view.ContextTarget!=0&&now<view.FireMarkUntil&&Remaining(For(view,slot.Skill),now)>0)return slot.Alternative;
 return slot.Skill;}
}

internal sealed class SkillSlotContext {
 sealed class Mark {internal uint Target,Instance;internal double Until;}
 readonly Dictionary<ulong,Mark> marks=new Dictionary<ulong,Mark>();internal uint Target;internal bool Enabled;double clockOffset,clockAt;bool clockKnown;
 internal void Clear(){marks.Clear();Target=0;Enabled=false;clockKnown=false;clockOffset=clockAt=0;}
 internal double Until {get{return marks.Values.Where(m=>m.Target==Target).Select(m=>m.Until).DefaultIfEmpty(0).Max();}}
 internal static bool Handles(int op){return op==0x0036||op==0x3138||op==0x3438||op==0x2a38||op==0x2b38||op==0x2c38||op==0x4236;}
 static ulong Key(uint actor,uint instance){return ((ulong)actor<<32)|instance;}
 internal bool Consume(byte[] b,int s,int e,double time){if(!Enabled)return false;int op=b[s]*256+b[s+1],p=s+2;uint actor;
 // The incoming server UTC clock and local capture clock can differ by seconds.
 // Map effect deadlines through a recent clock from this same confirmed flow.
 if(op==0x0036){if(e-s!=10)return false;long epoch=BitConverter.ToInt64(b,s+2);if(epoch<946684800000L||epoch>4102444800000L)return false;clockOffset=time-epoch/1000.0;clockAt=time;clockKnown=true;return false;}
 if(op==0x3138){if(e-s<7||b[p++]!=0||b[p++]!=0||!DpsSignal.Var(b,ref p,e,out actor)||actor>10000000||e-p!=2||b[p]!=0||b[p+1]!=0)return false;bool changed=Target!=actor;Target=actor;return changed;}
 if(op==0x3438){if(!EnemyHealthSignal.Selection(b,s,e,out actor))return false;bool changed=Target!=actor;Target=actor;return changed;}
 if(op==0x2a38||op==0x2b38){List<PlayerShieldSignal.Status> statuses;if(!PlayerShieldSignal.ReadStatuses(b,s,e,out actor,out statuses))return false;bool changed=false;foreach(var status in statuses){ulong key=Key(actor,status.Instance);if(marks.Remove(key))changed=true;
 // Only the exact Fire Mark effect observed in the controlled recording. Its
 // server millisecond expiry, mapped through0036, prevents a snapshot extending its life.
 if(status.Removing||status.Effect!=157100071||status.Duration<=0||status.Duration>60000)continue;if(!clockKnown||time-clockAt>2)continue;double until=status.Expiry/1000.0+clockOffset;if(until<=time||until>time+status.Duration/1000.0+.25)continue;
 foreach(var expired in marks.Where(x=>x.Value.Until<=time).Select(x=>x.Key).ToArray())marks.Remove(expired);
 if(marks.Count>=512)marks.Remove(marks.OrderBy(x=>x.Value.Until).First().Key);
 marks[key]=new Mark{Target=actor,Instance=status.Instance,Until=until};changed=true;}return changed;}
 if(op==0x2c38){uint count;if(!DpsSignal.Var(b,ref p,e,out actor)||!DpsSignal.Var(b,ref p,e,out count)||count>128)return false;var removed=new List<uint>();for(uint n=0;n<count;n++){if(p>=e)return false;int flags=b[p++];uint instance,unused;if(flags>7||!DpsSignal.Var(b,ref p,e,out instance)||p>=e)return false;p++;if((flags&1)!=0&&!DpsSignal.Var(b,ref p,e,out unused))return false;int skip=((flags&2)!=0?4:0)+((flags&4)!=0?4:0);if(e-p<skip)return false;p+=skip;removed.Add(instance);}if(p!=e)return false;bool changed=false;foreach(uint instance in removed)changed=marks.Remove(Key(actor,instance))||changed;return changed;}
 if(op==0x4236&&DpsSignal.Var(b,ref p,e,out actor)&&e-p==2&&b[p]==0){bool changed=false;foreach(var key in marks.Where(x=>x.Value.Target==actor).Select(x=>x.Key).ToArray()){marks.Remove(key);changed=true;}if(Target==actor){Target=0;changed=true;}return changed;}return false;
 }
}
