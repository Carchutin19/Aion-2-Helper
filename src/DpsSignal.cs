using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Own bounded reader of the observed global-client layouts. Protocol references
// and notices: THIRD-PARTY.md. No guessing by class, distance or target HP.
internal sealed class DpsSignal {
    internal struct Hit {internal uint Source,Target,Skill,Amount,PrimaryAmount;internal int Extras;internal bool Critical,Periodic,CriticalKnown,TagsKnown,Double,Perfect;internal byte Position;}
    internal sealed class Row {internal uint Actor;internal string Name;internal byte Role;internal long Damage,PeriodicDamage,DamagePrimary,DamageCritical,Healing,HealingReceived,HealingPrimary,HealingCritical,ReceivedPrimary,ReceivedCritical;internal int Hits,Criticals,DamageEvents,HealingEvents,HealingCriticals,ReceivedEvents,ReceivedCriticals;internal uint HighestHit;internal int TaggedDamageEvents,BackHits,FrontHits,DoubleHits,PerfectHits,BackCriticalHits,FrontCriticalHits,BackDoubleCriticalHits,FrontDoubleCriticalHits,BackPerfectHits,FrontPerfectHits;internal double Dps,Hps,ReceivedHps,LastImpact;}
    internal sealed class Snapshot {internal readonly List<Row> Rows=new List<Row>();internal double Duration,FirstImpact,LastImpact;internal long Revision,Pending,DirectFrames,ParsedDirect,UnknownTargets,UnknownSources,RecoveredImpacts;internal uint SelfActor;internal int Players,Npcs,PartyMembers;internal bool Active,PartyKnown,NeedsIdentification,PartyRecovered;internal string Status;}
    sealed class DeferredHit {internal Hit Hit;internal double Time;}
    sealed class TargetHealth {internal long Value;internal double Time;internal readonly List<DeferredHit> Hits=new List<DeferredHit>();}
    // Restart recovery is per impact, not inferred NPC identity. Only observed
    // offensive direction tags with a fresh, subsequent HP drop are eligible.
    readonly Dictionary<uint,TargetHealth> targetHealth=new Dictionary<uint,TargetHealth>();
    long recoveredImpacts;
    sealed class Parent {internal uint Actor;internal double Spawned;}
    sealed class PendingRoster {internal byte[] Bytes;internal double Time;}
    readonly Dictionary<string,PendingRoster> pendingRosters=new Dictionary<string,PendingRoster>();
    const int EntityLimit=4096;
    readonly object gate=new object();readonly Dictionary<uint,string> names=new Dictionary<uint,string>();
    readonly Dictionary<uint,byte> roles=new Dictionary<uint,byte>();
    readonly Dictionary<uint,double> activity=new Dictionary<uint,double>();
    readonly HashSet<uint> npcs=new HashSet<uint>(),party=new HashSet<uint>(),conflicted=new HashSet<uint>();
    readonly Dictionary<uint,Parent> parents=new Dictionary<uint,Parent>();readonly Dictionary<uint,Row> actors=new Dictionary<uint,Row>();
    // A full roster may arrive before energy identifies us. Keep at most one
    // bounded roster, and independently observed ongoing party vital updates.
    readonly HashSet<uint> explicitParty=new HashSet<uint>(),deferredParty=new HashSet<uint>();
    readonly Dictionary<uint,double> partyVitals=new Dictionary<uint,double>();
    readonly Dictionary<ulong,uint> partyDatabaseIds=new Dictionary<ulong,uint>();
    readonly List<uint> expiredParty=new List<uint>();bool deferredFull;
    DpsOptions options=new DpsOptions();string stream;uint localActor,currentMap;bool partyKnown,completed;double first,last,lastTraffic;long revision,pending,directFrames,parsedDirect,unknownTargets,unknownSources;
    internal Action<DpsHistoryRecord> EncounterCompleted;
    internal static bool Var(byte[] b,ref int p,int end,out uint v){v=0;for(int shift=0;shift<=28;shift+=7){if(p>=end)return false;byte x=b[p++];if(shift==28&&(x&240)!=0)return false;v|=(uint)(x&127)<<shift;if(x<128)return true;}return false;}
    static bool U32(byte[] b,ref int p,int end,out uint v){v=0;if(p+4>end)return false;v=BitConverter.ToUInt32(b,p);p+=4;return true;}
    internal static bool Direct(byte[] b,int start,int end,out Hit hit){
        hit=new Hit();int p=start+2;uint target,flags,unused,source,skill,type;
        if(end-start<2||end-start>65536||b[start]!=4||b[start+1]!=0x38||!Var(b,ref p,end,out target)||!Var(b,ref p,end,out flags)||!Var(b,ref p,end,out unused)||!Var(b,ref p,end,out source))return false;
        if((flags&4)==0||(flags&15)!=4&&(flags&15)!=6||!U32(b,ref p,end,out skill)||p>=end)return false;
        p++;if(!Var(b,ref p,end,out type))return false;int detailsStart=p;int marker=-1;
        for(int i=p;i+3<end;i++)if(b[i]>=1&&b[i]<=9&&b[i+1]==0&&b[i+2]==0&&b[i+3]==0){if(marker>=0)return false;marker=i;}
        if(marker<0)return false;p=marker+4;uint scalar,amount;
        if(!Var(b,ref p,end,out scalar)||!Var(b,ref p,end,out amount)||amount==0||amount>100000000||source==0||target==0)return false;
        int extras=0;ulong extraSum=0;if((flags&32)!=0){uint count;if(!Var(b,ref p,end,out count)||count<1||count>25)return false;extras=(int)count;for(int i=0;i<extras;i++){uint extra;if(!Var(b,ref p,end,out extra))return false;extraSum+=extra;}if(extraSum>=amount)return false;}
        hit=new Hit{Source=source,Target=target,Skill=skill,Amount=amount,PrimaryAmount=amount-(uint)extraSum,Extras=extras,Critical=type==3,CriticalKnown=type==2||type==3};
        // Observed optional plotter: flags byte, restoration varint, position
        // byte, then the four-byte sequence preceding the existing amount block.
        // No plotter or an unknown shape means unavailable, never a guessed tag.
        if((flags&2)!=0&&hit.CriticalKnown&&detailsStart<marker){int q=detailsStart;byte tags=b[q++];uint restored;if(Var(b,ref q,marker,out restored)&&q<marker&&b[q]<=2&&q+5==marker){hit.TagsKnown=true;hit.Position=b[q];hit.Double=(tags&8)!=0;hit.Perfect=(tags&4)!=0;}}
        return true;
    }
    internal static bool Periodic(byte[] b,int start,int end,out Hit hit){
        hit=new Hit();int p=start+2;uint target,source,instance,effect,pendingValue=0,amount=0,skill=0;
        if(end-start<2||end-start>128||b[start]!=5||b[start+1]!=0x38||!Var(b,ref p,end,out target)||p>=end)return false;
        byte flags=b[p++];if(flags!=0&&flags!=1&&flags!=2&&flags!=8&&flags!=9&&flags!=10&&flags!=11)return false;
        if(!Var(b,ref p,end,out source)||!Var(b,ref p,end,out instance)||!U32(b,ref p,end,out effect))return false;
        if((flags&1)!=0&&!Var(b,ref p,end,out pendingValue)||(flags&2)!=0&&!Var(b,ref p,end,out amount)||(flags&8)!=0&&!U32(b,ref p,end,out skill)||p!=end)return false;
        // Exact skill/effect pairs checked against our own NPC HP changes.
        // The same envelope also carries healing, potions and resources.
        bool damage=(skill==15390002&&(effect==1539000011||effect==1539000012))||
            skill==17070000&&effect==1707000011||skill==17080000&&effect==1708000011||
            skill==16140000&&effect==1614000011||skill==17400000&&effect==1740000011||skill==13730007&&effect==1373000712||
            // Weapon poison: repeated announced ticks and exact NPC HP drops,
            // with the weapon effect confirmed by the player. Not paralysis.
            skill==3001015&&effect==300101511;
        if(flags!=10||!damage||amount==0||amount>100000000||source==0||target==0)return false;
        hit=new Hit{Source=source,Target=target,Skill=skill,Amount=amount,Periodic=true};return true;
    }
    // Families independently correlated with positive HP updates in our own
    // global captures. Raw announced HP restoration, not effective/overhealing.
    internal static bool HealingSkill(uint skill){return skill==18160002||skill==18120000||skill==18170000||skill==18720001||skill==11720007||skill==11730008;}
    internal static bool HealingTick(byte[] b,int start,int end,out Hit hit){
        hit=new Hit();int p=start+2;uint target,source,instance,effect,left=0,amount=0,skill=0;
        if(end-start<2||end-start>128||b[start]!=5||b[start+1]!=0x38||!Var(b,ref p,end,out target)||p>=end)return false;
        byte flags=b[p++];if(flags!=10&&flags!=11)return false;
        if(!Var(b,ref p,end,out source)||!Var(b,ref p,end,out instance)||!U32(b,ref p,end,out effect)||(flags&1)!=0&&!Var(b,ref p,end,out left)||!Var(b,ref p,end,out amount)||!U32(b,ref p,end,out skill)||p!=end)return false;
        if(!((skill==18120000&&effect==1812000011)||(skill==17090000&&effect==1709000011))||source==0||target==0||amount==0||amount>100000000)return false;
        hit=new Hit{Source=source,Target=target,Skill=skill,Amount=amount,Periodic=true};return true;
    }
    Row CombatRow(uint actor){Row row;if(!actors.TryGetValue(actor,out row)){if(actors.Count>=128)return null;row=new Row{Actor=actor};actors[actor]=row;}return row;}
    bool StartImpact(double timestamp){if(last>0&&timestamp<last)return false;if(last==0||completed||timestamp-last>=options.IdleSeconds)ClearEncounter();if(first==0)first=timestamp;last=timestamp;return true;}
    bool RecordHealing(Hit hit,double timestamp){
        if(!ConfirmedPlayer(hit.Source,timestamp)||!ConfirmedPlayer(hit.Target,timestamp))return false;
        if(!StartImpact(timestamp))return true;var done=CombatRow(hit.Source);var received=CombatRow(hit.Target);if(done==null||received==null)return true;
        done.Healing+=hit.Amount;received.HealingReceived+=hit.Amount;done.LastImpact=received.LastImpact=timestamp;Touch(hit.Source,timestamp);Touch(hit.Target,timestamp);
        if(hit.CriticalKnown){done.HealingEvents++;received.ReceivedEvents++;done.HealingPrimary+=hit.PrimaryAmount;received.ReceivedPrimary+=hit.PrimaryAmount;if(hit.Critical){done.HealingCriticals++;received.ReceivedCriticals++;done.HealingCritical+=hit.PrimaryAmount;received.ReceivedCritical+=hit.PrimaryAmount;}}
        revision++;return true;
    }
    internal static bool EffectParent(byte[] b,int start,int end,out uint actor,out uint parent){
        actor=parent=0;int p=start+2;uint code,hp,max,unused,statusCaster;
        if(end-start<2||end-start>512||b[start]!=0x41||b[start+1]!=0x36||!Var(b,ref p,end,out actor)||p+9>end)return false;
        int outer=BitConverter.ToUInt16(b,p),description=b[p+2],state=BitConverter.ToUInt16(b,p+7);code=BitConverter.ToUInt32(b,p+3);p+=9;
        if(outer!=31||description!=0||state!=576||(code!=2920011&&code!=2920551))return false;
        p+=19;if(!Var(b,ref p,end,out hp)||!Var(b,ref p,end,out max)||hp>max||max==0)return false;p+=54;
        if(!Var(b,ref p,end,out unused)||unused!=1||p>=end||b[p++]!=17||!Var(b,ref p,end,out unused)||p+20>end)return false;
        p+=20;if(!Var(b,ref p,end,out statusCaster)||statusCaster!=actor||p+17>end)return false;
        p+=17;if(!U32(b,ref p,end,out parent)||!Var(b,ref p,end,out unused)||unused!=2||p+16>=end)return false;
        p+=16;if(!Var(b,ref p,end,out unused)||unused!=0||p!=end||actor==0||parent==0||parent>int.MaxValue||parent==actor)return false;return true;
    }
    static string Name(byte[] b,int p,int end){
        if(p>=end||b[p]<2||b[p]>24||p+1+b[p]>end)return null;
        try{string s=new UTF8Encoding(false,true).GetString(b,p+1,b[p]);foreach(char c in s)if(!char.IsLetterOrDigit(c))return null;return s;}catch(DecoderFallbackException){return null;}
    }
    internal static byte RoleFromWire(uint code){return code>=5&&code<=12?(byte)3:code>=29&&code<=36?(byte)2:code>=13&&code<=28?(byte)1:(byte)0;}
    void SetRole(uint actor,byte role){if(role==0||conflicted.Contains(actor))return;byte previous;if(!roles.TryGetValue(actor,out previous)||previous!=role){if(roles.Count<EntityLimit||roles.ContainsKey(actor)){roles[actor]=role;revision++;}}}
    void Bind(uint id,string name,byte role=0){if(id==0||name==null||conflicted.Contains(id))return;string previous;
        if(names.TryGetValue(id,out previous)&&previous!=name){names.Remove(id);roles.Remove(id);partyVitals.Remove(id);explicitParty.Remove(id);party.Remove(id);if(conflicted.Count<EntityLimit)conflicted.Add(id);revision++;return;}
        if(names.Count<EntityLimit||names.ContainsKey(id)){if(!names.ContainsKey(id))revision++;names[id]=name;parents.Remove(id);npcs.Remove(id);targetHealth.Remove(id);}
        SetRole(id,role);
        ResolveParty(lastTraffic);
    }
    uint Self {get{if(!string.IsNullOrEmpty(options.SelfName)){foreach(var pair in names)if(pair.Value==options.SelfName)return pair.Key;return 0;}return localActor;}}
    // Valid party-only vital packets establish membership before an appearance
    // packet supplies the display name. Keep those hits; never infer players
    // from their skill prefix, or keep expired/foreign-flow membership.
    bool ConfirmedPlayer(uint actor,double timestamp){double observed;return actor!=0&&!conflicted.Contains(actor)&&!npcs.Contains(actor)&&(actor==Self||names.ContainsKey(actor)||partyVitals.TryGetValue(actor,out observed)&&timestamp>=observed&&timestamp-observed<=120);}
    void FinishEncounter(string reason){if(completed||actors.Count==0||first<=0||last<=0)return;completed=true;if(EncounterCompleted==null)return;double duration=Math.Max(1,last-first);var record=new DpsHistoryRecord{Id=Guid.NewGuid().ToString("N"),Started=first,Ended=last,Duration=duration,Reason=reason,Scope=options.Scope,Map=currentMap,SelfActor=Self,PartyKnown=partyKnown,PartyRecovered=partyKnown&&explicitParty.Count==0,Party=new List<uint>(party),Pending=pending,DirectFrames=directFrames,ParsedDirect=parsedDirect,UnknownTargets=unknownTargets,UnknownSources=unknownSources};foreach(var pair in actors){string name;byte role;names.TryGetValue(pair.Key,out name);roles.TryGetValue(pair.Key,out role);record.Players.Add(DpsHistoryPlayer.Capture(pair.Value,name,role,duration));}record.Players.Sort(delegate(DpsHistoryPlayer a,DpsHistoryPlayer b){int order=b.Damage.CompareTo(a.Damage);return order==0?a.Actor.CompareTo(b.Actor):order;});EncounterCompleted(record);}
    internal void Finish(string reason){lock(gate)FinishEncounter(reason);}
    void ClearEncounter(string reason="Inactivity"){FinishEncounter(reason);actors.Clear();first=last=0;pending=0;completed=false;revision++;}
    void ClearParty(){party.Clear();explicitParty.Clear();deferredParty.Clear();partyVitals.Clear();partyDatabaseIds.Clear();partyKnown=false;deferredFull=false;revision++;}
    void ClearEntities(string reason="Connection or area change"){ClearEncounter(reason);targetHealth.Clear();names.Clear();roles.Clear();activity.Clear();npcs.Clear();parents.Clear();ClearParty();conflicted.Clear();localActor=currentMap=0;lastTraffic=0;}
    void ResolveParty(double now){
        uint self=Self;if(deferredParty.Count>0&&self!=0){if(!deferredFull||deferredParty.Contains(self)){if(deferredFull)explicitParty.Clear();foreach(uint id in deferredParty)if(!conflicted.Contains(id))explicitParty.Add(id);explicitParty.Add(self);}deferredParty.Clear();deferredFull=false;}
        expiredParty.Clear();foreach(var pair in partyVitals)if(now-pair.Value>120||conflicted.Contains(pair.Key)||npcs.Contains(pair.Key))expiredParty.Add(pair.Key);foreach(uint id in expiredParty)partyVitals.Remove(id);
        bool changed=false;expiredParty.Clear();foreach(uint id in party)if(id!=self&&!explicitParty.Contains(id)&&!partyVitals.ContainsKey(id))expiredParty.Add(id);foreach(uint id in expiredParty){party.Remove(id);changed=true;}
        if(self!=0){bool known=explicitParty.Count>0||partyVitals.Count>0;
            if(known)changed|=party.Add(self);else changed|=party.Remove(self);
            foreach(uint id in explicitParty)if(!conflicted.Contains(id)&&(party.Count<12||party.Contains(id)))changed|=party.Add(id);foreach(var pair in partyVitals)if((party.Count<12||party.Contains(pair.Key)))changed|=party.Add(pair.Key);
            if(partyKnown!=known){partyKnown=known;changed=true;}}
        if(changed)revision++;
    }
    internal static bool PartyVital(byte[] b,int start,int end,out uint actor){
        actor=0;int p=start+2;uint hp,max;if(end-start<2||b[start]!=0x1b||b[start+1]!=0x92||!Var(b,ref p,end,out actor)||actor==0||!Var(b,ref p,end,out hp)||!Var(b,ref p,end,out max)||max==0||hp>max||end-p!=25)return false;
        uint mp=BitConverter.ToUInt32(b,p),mpMax=BitConverter.ToUInt32(b,p+4);return mp<=mpMax&&mpMax<=10000000&&b[end-1]<=1;
    }
    void Touch(uint actor,double timestamp){double previous;if(activity.TryGetValue(actor,out previous)){if(timestamp>previous)activity[actor]=timestamp;}else if(activity.Count<EntityLimit)activity[actor]=timestamp;}
    internal void ResetTransport(){lock(gate){stream=null;pendingRosters.Clear();ClearEntities();}}
    internal void ResetEncounter(){lock(gate){ClearEncounter("Manual reset");targetHealth.Clear();}}
    internal void Configure(DpsOptions next){lock(gate){if(DpsOptions.Same(options,next))return;bool reset=options.Enabled!=next.Enabled;if(reset){stream=null;pendingRosters.Clear();ClearEntities("Meter disabled");}options=next.Copy();options.Normalize();revision++;}}
    internal static bool PlayerIdentity(byte[] b,int start,int end,bool self,out uint actor,out string name,out byte role){
        actor=0;name=null;role=0;int p=start+2;
        if(end-start<128||end-start>65536||!Var(b,ref p,end,out actor)||actor==0)return false;
        for(int i=p;i<Math.Min(end-12,p+16);i++){string n=Name(b,i,end);if(n==null)continue;int q=i+1+b[i]+(self?2:0);if(q+5>end)continue;uint code=BitConverter.ToUInt32(b,q);
            if((b[q+4]==1||b[q+4]==2)&&code>=5&&code<=36){name=n;role=RoleFromWire(code);return true;}}
        return false;
    }
    void SelectStream(string flow,double timestamp,bool reset){
        if(reset||stream!=flow)ClearEntities();stream=flow;lastTraffic=timestamp;
        PendingRoster staged;if(pendingRosters.TryGetValue(flow,out staged)&&timestamp>=staged.Time&&timestamp-staged.Time<=15)ReadParty(staged.Bytes,0,staged.Bytes.Length,false);
        pendingRosters.Clear();
    }
    internal void Consume(byte[] b,int start,int end,double timestamp,string flow){
        lock(gate){if(!options.Enabled||end-start<2)return;int opcode=(b[start]<<8)|b[start+1];
            if(stream==flow&&lastTraffic>0&&timestamp-lastTraffic>30)ClearEntities();
            // The instance roster precedes map load and the first dash. Keep a
            // bounded copy per candidate flow until a gameplay envelope selects it.
            if(opcode==0x0092&&end-start<=65536&&flow!=null){if(!pendingRosters.ContainsKey(flow)&&pendingRosters.Count>=4)pendingRosters.Clear();var copy=new byte[end-start];Buffer.BlockCopy(b,start,copy,0,copy.Length);pendingRosters[flow]=new PendingRoster{Bytes=copy,Time=timestamp};}
            uint identityActor;string identityName;byte identityRole;
            bool selfInfo=opcode==0x3336&&PlayerIdentity(b,start,end,true,out identityActor,out identityName,out identityRole);
            if(opcode==0x2136&&end-start==48&&BitConverter.ToUInt32(b,start+2)<=10000&&BitConverter.ToUInt32(b,start+6)>0&&BitConverter.ToUInt32(b,start+6)<10000000){
                uint map=BitConverter.ToUInt32(b,start+6);
                // A boss phase can teleport us on the same connection/map without
                // respawning the boss. Its entity and party identities stay valid.
                if(stream!=flow||currentMap!=0&&currentMap!=map)SelectStream(flow,timestamp,true);
                currentMap=map;
            }
            if(selfInfo){PlayerIdentity(b,start,end,true,out identityActor,out identityName,out identityRole);if(stream!=flow)SelectStream(flow,timestamp,true);if(localActor!=0&&localActor!=identityActor)ClearEntities();localActor=identityActor;Bind(identityActor,identityName,identityRole);ResolveParty(timestamp);}
            // Energy kind3 identifies the local character and gameplay stream.
            if(opcode==0x008d){int p=start+2;uint id;if(Var(b,ref p,end,out id)&&p<end){int flags=b[p++];bool energy=false,valid=(flags&~3)==0;
                for(int width=4;width<=8&&valid;width+=4)if((flags&(width==4?1:2))!=0){if(p>=end){valid=false;break;}int count=b[p++];for(int k=0;k<count;k++){if(p+1+width>end){valid=false;break;}if(width==4&&b[p]==3)energy=true;p+=1+width;}}
                if(valid&&energy&&p==end&&id>0){if(stream!=null&&stream!=flow)SelectStream(flow,timestamp,true);stream=flow;if(localActor!=0&&localActor!=id)ClearEntities();localActor=id;ResolveParty(timestamp);}
            }}
            if(stream==null)stream=flow;if(stream!=flow)return;
            lastTraffic=timestamp;
            if(opcode==0x008d){ObserveTargetHealth(b,start,end,timestamp);return;}
            if(opcode==0x048d){int p=start+2;uint unused,id;if(Var(b,ref p,end,out unused)){p+=4;if(Var(b,ref p,end,out id))Bind(id,Name(b,p+2,end));}return;}
            if(opcode==0x3336)return;
            if(opcode==0x0092||opcode==0x0d92){ReadParty(b,start,end,opcode==0x0d92);return;}
            if(opcode==0x1b92){uint id;if(PartyVital(b,start,end,out id)&&!conflicted.Contains(id)&&!npcs.Contains(id)&&(partyVitals.Count<12||partyVitals.ContainsKey(id))){partyVitals[id]=timestamp;ResolveParty(timestamp);}return;}
            if(opcode==0x1192&&end-start==10){uint id;ulong key=BitConverter.ToUInt64(b,start+2);if(partyDatabaseIds.TryGetValue(key,out id)&&id!=Self){explicitParty.Remove(id);deferredParty.Remove(id);partyVitals.Remove(id);party.Remove(id);partyDatabaseIds.Remove(key);revision++;ResolveParty(timestamp);}else ClearParty();return;}
            if(opcode==0x4136){int p=start+2;uint id;if(Var(b,ref p,end,out id)){targetHealth.Remove(id);parents.Remove(id);if(p+3<=end&&(b[p+2]==0||b[p+2]==1)&&npcs.Count<EntityLimit)npcs.Add(id);uint entity,parent;
                if(EffectParent(b,start,end,out entity,out parent)&&parents.Count<EntityLimit)parents[entity]=new Parent{Actor=parent,Spawned=timestamp};}return;}
            if(opcode==0x4536){uint id;string name;byte role;if(PlayerIdentity(b,start,end,false,out id,out name,out role))Bind(id,name,role);return;}
            Hit hit;bool periodic=opcode==0x0538;if(opcode!=0x0438&&!periodic)return;
            if(periodic){if(HealingTick(b,start,end,out hit)){RecordHealing(hit,timestamp);return;}if(!Periodic(b,start,end,out hit)){pending++;return;}}
            else {directFrames++;if(!Direct(b,start,end,out hit))return;parsedDirect++;}
            if(!periodic&&HealingSkill(hit.Skill)){RecordHealing(hit,timestamp);return;}
            // Incoming NPC attacks keep the selected player's widget visible,
            // even when that player is not attacking. Never add them to DPS.
            if(hit.Source!=hit.Target&&npcs.Contains(hit.Source)&&!parents.ContainsKey(hit.Source)&&(hit.Target==Self||names.ContainsKey(hit.Target))){Touch(hit.Target,timestamp);return;}
            bool weaponPoison=periodic&&hit.Skill==3001015; // Periodic() already validated the exact effect and flags.
            if(hit.Source==hit.Target||hit.Skill==11000100||!weaponPoison&&(hit.Skill/1000000<11||hit.Skill/1000000>19))return;
            Parent owner;if(!names.ContainsKey(hit.Source)&&parents.TryGetValue(hit.Source,out owner)&&timestamp>=owner.Spawned&&timestamp-owner.Spawned<=120)hit.Source=owner.Actor;
            if(!npcs.Contains(hit.Target)||names.ContainsKey(hit.Target)||parents.ContainsKey(hit.Target)||conflicted.Contains(hit.Source)||hit.Source==hit.Target){
                unknownTargets++;
                TargetHealth health;
                if(!npcs.Contains(hit.Target)&&!ConfirmedPlayer(hit.Target,timestamp)&&!conflicted.Contains(hit.Target)&&!parents.ContainsKey(hit.Target)&&ConfirmedPlayer(hit.Source,timestamp)&&!hit.Periodic&&hit.CriticalKnown&&hit.TagsKnown&&hit.Position!=0&&targetHealth.TryGetValue(hit.Target,out health)&&timestamp>=health.Time&&timestamp-health.Time<=.5){
                    if(health.Hits.Count<16)health.Hits.Add(new DeferredHit{Hit=hit,Time=timestamp});else health.Hits.Clear();
                }
                return;
            }
            RecordDamage(hit,timestamp);
        }
    }
    void RecordDamage(Hit hit,double timestamp){
            // Retain identified/local players only; never label an unknown NPC as
            // a player just because its skill happens to have a class prefix.
            if(!ConfirmedPlayer(hit.Source,timestamp)){pending++;unknownSources++;return;}
            // Cosmetic fallback for already identified damage sources only.
            // It never establishes identity, party membership or ownership.
            if(!roles.ContainsKey(hit.Source)){uint family=hit.Skill/1000000;SetRole(hit.Source,family==11||family==12?(byte)3:family==17||family==18?(byte)2:(byte)1);}
            if(!StartImpact(timestamp))return;Row row=CombatRow(hit.Source);if(row==null)return;
            row.Damage+=hit.Amount;row.HighestHit=Math.Max(row.HighestHit,hit.Periodic?hit.Amount:hit.PrimaryAmount);row.LastImpact=timestamp;Touch(hit.Source,timestamp);row.Hits+=1+hit.Extras;
            if(hit.CriticalKnown){row.DamageEvents++;row.DamagePrimary+=hit.PrimaryAmount;if(hit.Critical){row.Criticals++;row.DamageCritical+=hit.PrimaryAmount;}}
            if(hit.TagsKnown){row.TaggedDamageEvents++;if(hit.Double)row.DoubleHits++;if(hit.Perfect)row.PerfectHits++;
                if(hit.Position==1){row.BackHits++;if(hit.Critical)row.BackCriticalHits++;if(hit.Double&&hit.Critical)row.BackDoubleCriticalHits++;if(hit.Perfect)row.BackPerfectHits++;}
                else if(hit.Position==2){row.FrontHits++;if(hit.Critical)row.FrontCriticalHits++;if(hit.Double&&hit.Critical)row.FrontDoubleCriticalHits++;if(hit.Perfect)row.FrontPerfectHits++;}}
            if(hit.Periodic)row.PeriodicDamage+=hit.Amount;revision++;
    }
    void ObserveTargetHealth(byte[] b,int start,int end,double timestamp){
        int p=start+2;uint actor;long hp=-1;
        if(!Var(b,ref p,end,out actor)||actor==0||p>=end)return;
        int flags=b[p++];if((flags&~3)!=0)return;
        for(int width=4;width<=8;width+=4)if((flags&(width==4?1:2))!=0){
            uint count;if(!Var(b,ref p,end,out count)||count>128||count>(end-p)/(width+1))return;
            for(int i=0;i<count;i++){byte kind=b[p++];if(width==8&&kind==0){if(hp!=-1)return;hp=BitConverter.ToInt64(b,p);}p+=width;}
        }
        if(p!=end||hp<0||ConfirmedPlayer(actor,timestamp)||conflicted.Contains(actor)||npcs.Contains(actor)||parents.ContainsKey(actor)){targetHealth.Remove(actor);return;}
        TargetHealth state;
        if(!targetHealth.TryGetValue(actor,out state)){
            if(targetHealth.Count>=256){uint stale=0;foreach(var pair in targetHealth)if(timestamp-pair.Value.Time>2){stale=pair.Key;break;}if(stale==0)return;targetHealth.Remove(stale);}
            targetHealth[actor]=new TargetHealth{Value=hp,Time=timestamp};return;
        }
        if(timestamp<state.Time){state.Hits.Clear();return;}
        long sum=0;foreach(var pendingHit in state.Hits)sum+=pendingHit.Hit.Amount;
        // HP and announced damage may have different scales. A drop corroborates
        // only these direction-tagged impacts; no multiplier or NPC/boss label.
        if(timestamp-state.Time<=.5&&state.Value>hp&&sum>0&&state.Value-hp>=sum){
            foreach(var pendingHit in state.Hits)if(timestamp>=pendingHit.Time&&timestamp-pendingHit.Time<=.5&&ConfirmedPlayer(pendingHit.Hit.Source,timestamp)){long before=revision;RecordDamage(pendingHit.Hit,pendingHit.Time);if(revision!=before)recoveredImpacts++;}
        }
        state.Hits.Clear();state.Value=hp;state.Time=timestamp;
    }
    void ReadParty(byte[] b,int start,int end,bool addition){
        if(end-start>65536)return;var found=new Dictionary<uint,string>();var foundRoles=new Dictionary<uint,byte>();var database=new Dictionary<ulong,uint>();
        for(int p=start+2;p<end-64;p++){if(b[p+8]!=36)continue;uint actor=BitConverter.ToUInt32(b,p);ushort server=BitConverter.ToUInt16(b,p+4),realm=BitConverter.ToUInt16(b,p+6);if(actor==0||server<1||server>9999||realm<1||realm>9999||BitConverter.ToUInt16(b,p+51)!=server)continue;
            Guid guid;string uuid=Encoding.ASCII.GetString(b,p+9,36);if(!Guid.TryParse(uuid,out guid))continue;string name=Name(b,p+53,end);if(name==null)continue;int q=p+54+b[p+53];if(q+4>end)continue;uint code=BitConverter.ToUInt32(b,q);if(code<5||code>36)continue;
            if(found.ContainsKey(actor)||found.Count>=12)return;found[actor]=name;foundRoles[actor]=RoleFromWire(code);database[BitConverter.ToUInt64(b,p+45)]=actor;
        }
        // 0092 replaces the full roster. The observed 0D92 join update carries
        // only the added member; replacing here would discard the local player.
        if(found.Count==0||addition&&found.Count!=1)return;
        foreach(var pair in found)Bind(pair.Key,pair.Value,foundRoles[pair.Key]);uint self=Self;
        if(self!=0&&!addition&&!found.ContainsKey(self))return;
        if(!addition)ClearParty();else if(explicitParty.Count>=12)return;
        foreach(var pair in database)if(partyDatabaseIds.Count<12||partyDatabaseIds.ContainsKey(pair.Key))partyDatabaseIds[pair.Key]=pair.Value;
        if(self==0){if(!addition){deferredParty.Clear();deferredFull=true;}foreach(uint id in found.Keys)if(deferredParty.Count<12)deferredParty.Add(id);return;}
        explicitParty.Add(self);foreach(uint id in found.Keys)explicitParty.Add(id);ResolveParty(lastTraffic);
    }
    internal Snapshot Read(double now){lock(gate){if(last>0&&now-last>=options.IdleSeconds)FinishEncounter("Inactivity");ResolveParty(now);uint self=Self;var result=new Snapshot{Revision=revision,Pending=pending,PartyKnown=partyKnown,PartyMembers=party.Count,PartyRecovered=partyKnown&&explicitParty.Count==0,SelfActor=self,NeedsIdentification=options.Enabled&&self==0,Players=names.Count,Npcs=npcs.Count,DirectFrames=directFrames,ParsedDirect=parsedDirect,UnknownTargets=unknownTargets,UnknownSources=unknownSources,RecoveredImpacts=recoveredImpacts};result.Active=last>0&&now-last<options.IdleSeconds&&now-lastTraffic<4;
        // First to last accepted impact, matching offline results and avoiding
        // an idle-time denominator jump when the encounter finishes.
        result.Duration=first==0?0:Math.Max(1,last-first);result.FirstImpact=first;
        result.Status=!options.Enabled?"DPS disabled":self==0?"Dash once to identify your character":lastTraffic==0||now-lastTraffic>4?"Waiting for combat data":options.Scope=="party"&&!partyKnown?"Party not detected · showing yourself":actors.Count==0?"Waiting for combat":result.Active?"In combat":"Last encounter";
        foreach(var pair in actors){if(options.Scope=="self"&&pair.Key!=self||options.Scope=="party"&&!(partyKnown?party.Contains(pair.Key):pair.Key==self))continue;string name;byte role;names.TryGetValue(pair.Key,out name);roles.TryGetValue(pair.Key,out role);var v=pair.Value;result.LastImpact=Math.Max(result.LastImpact,v.LastImpact);result.Rows.Add(new Row{Actor=pair.Key,Name=name,Role=role,Damage=v.Damage,PeriodicDamage=v.PeriodicDamage,Hits=v.Hits,Criticals=v.Criticals,DamageEvents=v.DamageEvents,DamagePrimary=v.DamagePrimary,DamageCritical=v.DamageCritical,Healing=v.Healing,HealingReceived=v.HealingReceived,HealingEvents=v.HealingEvents,HealingCriticals=v.HealingCriticals,HealingPrimary=v.HealingPrimary,HealingCritical=v.HealingCritical,ReceivedEvents=v.ReceivedEvents,ReceivedCriticals=v.ReceivedCriticals,ReceivedPrimary=v.ReceivedPrimary,ReceivedCritical=v.ReceivedCritical,Dps=v.Damage/Math.Max(1,result.Duration),Hps=v.Healing/Math.Max(1,result.Duration),ReceivedHps=v.HealingReceived/Math.Max(1,result.Duration),LastImpact=v.LastImpact,HighestHit=v.HighestHit,TaggedDamageEvents=v.TaggedDamageEvents,BackHits=v.BackHits,FrontHits=v.FrontHits,DoubleHits=v.DoubleHits,PerfectHits=v.PerfectHits,BackCriticalHits=v.BackCriticalHits,FrontCriticalHits=v.FrontCriticalHits,BackDoubleCriticalHits=v.BackDoubleCriticalHits,FrontDoubleCriticalHits=v.FrontDoubleCriticalHits,BackPerfectHits=v.BackPerfectHits,FrontPerfectHits=v.FrontPerfectHits});}
        foreach(var pair in activity){if(options.Scope=="self"&&pair.Key!=self||options.Scope=="party"&&!(partyKnown?party.Contains(pair.Key):pair.Key==self))continue;result.LastImpact=Math.Max(result.LastImpact,pair.Value);}
        SortRows(result.Rows,self,options.PinSelfFirst,options.View);if(result.Rows.Count>10)result.Rows.RemoveRange(10,result.Rows.Count-10);return result;
    }}
    internal static long Total(Row row,string view){return view=="healing"?row.Healing:view=="received"?row.HealingReceived:row.Damage;}
    internal static double? CriticalShare(Row row,string view){long total=view=="healing"?row.HealingPrimary:view=="received"?row.ReceivedPrimary:row.DamagePrimary,crit=view=="healing"?row.HealingCritical:view=="received"?row.ReceivedCritical:row.DamageCritical;return total>0?(double?)(100.0*crit/total):null;}
    internal static double? CriticalRate(Row row,string view){int total=view=="healing"?row.HealingEvents:view=="received"?row.ReceivedEvents:row.DamageEvents,crit=view=="healing"?row.HealingCriticals:view=="received"?row.ReceivedCriticals:row.Criticals;return total>0?(double?)(100.0*crit/total):null;}
    internal static void SortRows(List<Row> rows,uint self,bool pin,string view="damage"){rows.Sort(delegate(Row a,Row b){if(pin&&self!=0&&(a.Actor==self)!=(b.Actor==self))return a.Actor==self?-1:1;int n=Total(b,view).CompareTo(Total(a,view));return n!=0?n:a.Actor.CompareTo(b.Actor);});}
    internal static object Export(Snapshot s){var rows=new List<object>();foreach(var row in s.Rows)rows.Add(new{actor=row.Actor,name=row.Name,role=row.Role==3?"tank":row.Role==2?"healer":row.Role==1?"damage":"unknown",damage=row.Damage,dps=row.Dps,highestHit=row.HighestHit,taggedDamageEvents=row.TaggedDamageEvents,backHits=row.BackHits,frontHits=row.FrontHits,doubleHits=row.DoubleHits,perfectHits=row.PerfectHits,candidatePeriodicDamage=row.PeriodicDamage,hits=row.Hits,damageCriticalShare=CriticalShare(row,"damage"),damageCriticalRate=CriticalRate(row,"damage"),healingDone=row.Healing,healingReceived=row.HealingReceived,hps=row.Hps,receivedHps=row.ReceivedHps,healingCriticalShare=CriticalShare(row,"healing"),healingCriticalRate=CriticalRate(row,"healing"),receivedCriticalShare=CriticalShare(row,"received"),receivedCriticalRate=CriticalRate(row,"received")});return new{status=s.Status,durationSeconds=s.Duration,active=s.Active,partyKnown=s.PartyKnown,partyMembers=s.PartyMembers,partyRecovered=s.PartyRecovered,needsIdentification=s.NeedsIdentification,pending=s.Pending,selfActor=s.SelfActor,players=s.Players,npcs=s.Npcs,directFrames=s.DirectFrames,parsedDirect=s.ParsedDirect,unknownTargets=s.UnknownTargets,unknownSources=s.UnknownSources,recoveredImpacts=s.RecoveredImpacts,rows=rows,experimental=true,completeDamage=false,completeHealing=false,rawHealing=true,criticalBasis="primary direct impacts; additional impacts and periodic ticks excluded"};}
}
