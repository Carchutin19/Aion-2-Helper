using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class CombatOverlayState {
    internal sealed class Effect {
        internal uint Instance,Skill,Id;
        internal long WireExpiry;
        internal double Ends,Updated;
        internal bool Party;
    }

    readonly object gate=new object();
    readonly Dictionary<uint,Effect> effects=new Dictionary<uint,Effect>();
    readonly Dictionary<uint,double> removed=new Dictionary<uint,double>();
    readonly Dictionary<uint,double> partyVitals=new Dictionary<uint,double>();
    Dictionary<ulong,uint> roster=new Dictionary<ulong,uint>();
    string flow,loadingFlow;
    uint self,loading,map;
    uint selected,dialogueActor;
    double dialogueTime;
    bool? combat;
    double lastBattle;
    volatile bool trackBuffs;

    internal bool? Combat {get{lock(gate)return combat;}}
    internal bool DialogueOpen(double now) {
        lock(gate)return dialogueActor!=0&&now>=dialogueTime&&now-dialogueTime<120;
    }
    internal void Configure(bool buffs) {
        lock(gate) {
            trackBuffs=buffs;
            if(!buffs){effects.Clear();removed.Clear();}
        }
    }
    void ClearWorld() {
        combat=null;lastBattle=0;
        selected=dialogueActor=0;dialogueTime=0;
        effects.Clear();removed.Clear();roster.Clear();partyVitals.Clear();
    }
    internal void Reset(){lock(gate){flow=loadingFlow=null;self=loading=map=0;ClearWorld();}}

    internal static bool TryBattle(byte[] b,int s,int e,out uint actor,out bool active) {
        int p=s+2;uint unused,flag;
        actor=0;active=false;
        if(!DpsSignal.Var(b,ref p,e,out actor)||actor==0||!DpsSignal.Var(b,ref p,e,out unused)||
           !DpsSignal.Var(b,ref p,e,out flag)||flag>1||(e-p!=0&&e-p!=8))return false;
        active=flag==1;
        return true;
    }

    // These attack variants apply a separate timed status to their own caster.
    // Match the observed pair after validating both source and target.
    static uint OwnAttackBuffSkill(uint skill,uint effect){
        if(skill==15110037&&effect==151100301)return 15110000; // Winter's Shackles
        if(skill==15250047&&effect==152500471)return 15250000; // Pyroclasm
        return 0;
    }

    internal bool Consume(byte[] b,int s,int e,double time,string key) {
        int op=(b[s]<<8)|b[s+1];
        bool identity=op==0x2036||op==0x3336||op==0x2136;
        bool partyUpdate=op==0x0092||op==0x0d92||op==0x1192||op==0x1b92;
        bool interaction=op==0x3438||op==0x41e2||op==0x43e2;
        if(!identity&&!partyUpdate&&!interaction&&op!=0x218d&&(!trackBuffs||(op!=0x2a38&&op!=0x2b38&&op!=0x2c38)))return false;
        lock(gate) {
            uint actor;string name;byte role;
            if(op==0x2036){
                if(!DashSignal.LoginActor(b,s,e,out actor))return false;
                ClearWorld();self=map=0;flow=null;loading=actor;loadingFlow=key;
                return true;
            }
            if(op==0x3336){
                if(!DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role))return false;
                if(loading!=0&&(loading!=actor||loadingFlow!=key))return false;
                bool changed=self!=actor||flow!=key;
                if(changed){ClearWorld();self=actor;flow=key;map=0;}
                loading=0;loadingFlow=null;
                return changed;
            }
            if(self==0||key!=flow)return false;
            if(interaction)return ReadInteraction(b,s,e,time,op);
            if(op==0x2136){
                if(e-s!=48)return false;
                uint next=BitConverter.ToUInt32(b,s+6);
                if(next==0||next>=10000000)return false;
                bool changed=map!=0&&map!=next;
                if(changed)ClearWorld();
                map=next;
                return changed;
            }
            if(op==0x218d){
                bool active;
                if(!TryBattle(b,s,e,out actor,out active)||actor!=self||time<lastBattle)return false;
                lastBattle=time;
                if(combat==active)return false;
                combat=active;
                return true;
            }
            if(op==0x0092||op==0x0d92){
                Dictionary<uint,string> names;Dictionary<uint,byte> roles;Dictionary<ulong,uint> members;
                if(!DpsSignal.ParsePartyRoster(b,s,e,out names,out roles,out members))return false;
                if(op==0x0092&&names.ContainsKey(self)){roster=members;partyVitals.Clear();}
                else if(op==0x0d92&&members.Count==1&&roster.Count>0&&roster.Count<12)
                    foreach(var member in members)roster[member.Key]=member.Value;
                return false;
            }
            if(op==0x1192){
                if(e-s!=10)return false;
                ulong id=BitConverter.ToUInt64(b,s+2);
                if(!roster.TryGetValue(id,out actor)||actor==self){roster.Clear();partyVitals.Clear();}
                else{roster.Remove(id);partyVitals.Remove(actor);}
                return false;
            }
            if(op==0x1b92){
                if(DpsSignal.PartyVital(b,s,e,out actor)&&actor!=self){
                    if(partyVitals.Count>=12&&!partyVitals.ContainsKey(actor))
                        foreach(uint expired in partyVitals.Where(v=>time-v.Value>120).Select(v=>v.Key).ToArray())partyVitals.Remove(expired);
                    if(partyVitals.Count<12||partyVitals.ContainsKey(actor))partyVitals[actor]=time;
                }
                return false;
            }
            if(!trackBuffs)return false;
            int at=s+2;
            if(!DpsSignal.Var(b,ref at,e,out actor)||actor!=self)return false;
            if(op==0x2c38){
                List<uint> removals;
                if(!PlayerShieldSignal.ReadRemovals(b,s,e,out actor,out removals))return false;
                bool changed=false;
                foreach(uint instance in removals)changed|=Remove(instance,time);
                return changed;
            }
            List<PlayerShieldSignal.Status> statuses;
            if(!PlayerShieldSignal.ReadStatuses(b,s,e,out actor,out statuses))return false;
            bool updated=false;
            foreach(var status in statuses){
                if(status.Removing){updated|=Remove(status.Instance,time);continue;}
                if(status.Source==0||status.Skill==0||status.Effect==0||status.Instance==0||
                    (status.Duration!=-1&&(status.Duration<=0||status.Duration>86400000)))continue;
                bool party=status.Source!=self;
                double partySeen;
                if(party&&!roster.ContainsValue(status.Source)&&
                    (roster.Count>0||!partyVitals.TryGetValue(status.Source,out partySeen)||time-partySeen>120||time<partySeen))continue;
                uint displaySkill=party?0:OwnAttackBuffSkill(status.Skill,status.Effect);
                if(displaySkill==0){
                    if(SkillCatalogue.Shared.IsDamage(status.Skill))continue;
                    displaySkill=status.Skill;
                }
                double gone;
                if(removed.TryGetValue(status.Instance,out gone)&&time<=gone)continue;
                Effect existing;
                if(effects.TryGetValue(status.Instance,out existing)){
                    if(time<existing.Updated)continue;
                    if(existing.Id==status.Effect&&existing.WireExpiry==status.Expiry&&existing.Skill==displaySkill&&existing.Party==party){
                        existing.Updated=time;
                        continue;
                    }
                }else if(effects.Count>=64){
                    uint oldest=effects.Aggregate((a,z)=>a.Value.Updated<=z.Value.Updated?a:z).Key;
                    effects.Remove(oldest);
                }
                effects[status.Instance]=new Effect{Instance=status.Instance,Skill=displaySkill,Id=status.Effect,Party=party,
                    WireExpiry=status.Expiry,Ends=status.Duration==-1?0:time+status.Duration/1000.0,Updated=time};
                updated=true;
            }
            return updated;
        }
    }

    bool ReadInteraction(byte[] b,int start,int end,double time,int opcode) {
        if(time<dialogueTime)return false;
        uint actor;
        if(opcode==0x3438){
            if(!EnemyHealthSignal.Selection(b,start,end,out actor))return false;
            selected=actor;
            if(dialogueActor==0||dialogueActor==selected)return false;
            dialogueActor=0;
        }else{
            // The interaction frames identify the selected NPC as a fixed-width ID.
            // The selection alone is also sent when the player simply targets an NPC.
            if(end-start!=(opcode==0x41e2?16:8)||b[start+2]!=0||b[start+3]!=0)return false;
            actor=BitConverter.ToUInt32(b,start+4);
            if(actor==0)return false;
            if(opcode==0x41e2){
                if(actor!=selected)return false;
                dialogueActor=actor;
            }else{
                if(actor!=dialogueActor)return false;
                dialogueActor=0;
            }
        }
        dialogueTime=time;
        return true;
    }

    bool Remove(uint instance,double time) {
        Effect entry;
        if(!effects.TryGetValue(instance,out entry)||time<entry.Updated)return false;
        effects.Remove(instance);
        if(removed.Count>=128)removed.Clear();
        removed[instance]=time;
        return true;
    }

    internal Effect[] ReadBuffs(double now,bool own,bool party) {
        lock(gate) {
            return effects.Values.Where(e=>(e.Party?party:own)&&(e.Ends==0||e.Ends>now))
                .OrderBy(e=>e.Instance).Take(12).Select(e=>new Effect{Instance=e.Instance,Skill=e.Skill,Id=e.Id,Party=e.Party,Ends=e.Ends}).ToArray();
        }
    }

    internal static void Verify(){
        var state=new CombatOverlayState();
        state.Configure(true);
        Action<byte[],double,string> feed=(b,t,f)=>state.Consume(b,0,b.Length,t,f);
        feed(EnemyTargetVerification.Player(13238,"Own",100,100,true),1,"A");
        feed(new byte[]{0x21,0x8d,0xb7,0x67,0,1},2,"A");
        if(state.Combat.HasValue)throw new Exception("Other actors must never change the combat opacity state");
        feed(new byte[]{0x21,0x8d,0xb6,0x67,0,1},3,"A");
        feed(new byte[]{0x21,0x8d,0xb6,0x67,0,0},4,"B");
        if(state.Combat!=true)throw new Exception("Combat state must be owned and scoped to the active flow");
        feed(new byte[]{0x21,0x8d,0xb6,0x67,0,0},5,"A");
        if(state.Combat!=false)throw new Exception("Explicit own combat end must be recognized");
        feed(PlayerShieldVerification.Status(13238,671),6,"A");
        var active=state.ReadBuffs(6,true,true);
        if(active.Length!=1||active[0].Party||active[0].Ends!=26)
            throw new Exception("Own timed effects must preserve their received duration");
        feed(PlayerShieldVerification.Status(13238,671),7,"A");
        if(state.ReadBuffs(26,true,true).Length!=0)
            throw new Exception("A refresh repeating the same wire expiry must not extend a buff");
        feed(PlayerShieldVerification.Remove(13238,671),8,"A");
        feed(PlayerShieldVerification.Status(13238,671),7.5,"A");
        if(state.ReadBuffs(9,true,true).Length!=0)throw new Exception("An old refresh must not resurrect a removed buff");
        feed(PlayerShieldVerification.Status(13238,672,151100301,15110037),9,"A");
        feed(PlayerShieldVerification.Status(13238,673,152500471,15250047),9,"A");
        feed(PlayerShieldVerification.Status(13238,674,150900001,15110037),9,"A");
        feed(PlayerShieldVerification.Status(13239,675,151100301,15110037),9,"A");
        var attackBuffs=state.ReadBuffs(9,true,false);
        if(attackBuffs.Length!=2||attackBuffs[0].Skill!=15110000||attackBuffs[1].Skill!=15250000||
           SkillCatalogue.Shared.Name(attackBuffs[1].Skill,"es")!="Ignición")
            throw new Exception("Only the verified own Winter's Shackles and Pyroclasm pairs use localized base skills");
        feed(PlayerShieldVerification.Remove(13238,672),9.1,"A");
        if(state.ReadBuffs(9.1,true,false).Length!=1)throw new Exception("A verified attack buff expires on its own instance removal");
        feed(DashSignal.LoginFixture(12),10,"A");
        if(state.Combat.HasValue||state.ReadBuffs(10,true,false).Length!=0)throw new Exception("Character loading resets combat and buff identity");

        var dialogue=new CombatOverlayState();
        Action<byte[],double,string> npc=(b,t,f)=>dialogue.Consume(b,0,b.Length,t,f);
        var select=new byte[]{0x34,0x38,0,0,0,0xa0,0x91,1};
        var open=new byte[]{0x41,0xe2,0,0,0xa0,0x48,0,0,0x88,0xeb,0x17,0x74,0xa1,1,0,0};
        var close=new byte[]{0x43,0xe2,0,0,0xa0,0x48,0,0};
        npc(EnemyTargetVerification.Player(13238,"Own",100,100,true),20,"A");
        npc(open,21,"A");
        npc(select,22,"A");
        npc(open,23,"B");
        if(dialogue.DialogueOpen(23))throw new Exception("Only a matching interaction in the active world can open a dialogue");
        npc(open,24,"A");
        if(!dialogue.DialogueOpen(24))throw new Exception("Observed NPC interaction opens the dialogue");
        npc(close,23,"A");
        npc(close,25,"B");
        if(!dialogue.DialogueOpen(25))throw new Exception("Foreign and older closes must be ignored");
        npc(close,26,"A");
        if(dialogue.DialogueOpen(26))throw new Exception("Matched close restores widgets");
        npc(open,27,"A");
        npc(new byte[]{0x34,0x38,0,0,0,0},28,"A");
        if(dialogue.DialogueOpen(28))throw new Exception("Deselect restores widgets when the close is missed");
        npc(select,29,"A");
        npc(open,30,"A");
        if(dialogue.DialogueOpen(151))throw new Exception("A missing close cannot hide the HUD indefinitely");
        dialogue.Reset();
        if(dialogue.DialogueOpen(31))throw new Exception("Transport reset clears the dialogue");

        var fade=new CombatOpacityMotion();
        fade.Step(0,true,false,null,30);
        if(fade.Effective(.7)!=.7||fade.Animating)throw new Exception("Unknown combat state keeps the widget opacity");
        fade.Step(1,true,false,false,30);
        fade.Step(1.2,true,false,false,30);
        if(fade.Effective(.7)<=.3||fade.Effective(.7)>=.7||!fade.Animating)throw new Exception("Idle opacity must fade");
        fade.Step(2,true,false,false,30);
        if(Math.Abs(fade.Effective(.7)-.3)>.0001||fade.Animating)throw new Exception("Idle opacity must reach the selected value");
        fade.Step(3,true,false,true,30);
        fade.Step(3.2,true,false,true,30);
        if(fade.Effective(.7)!=1||fade.Animating)throw new Exception("Combat must restore full opacity");
        fade.Step(4,true,true,false,30);
        if(fade.Effective(.7)!=.7)throw new Exception("Editing must restore the configured appearance");
    }
}

internal sealed class CombatOpacityMotion {
    double weight=1,offset,fromWeight=1,fromOffset,target,started;
    bool applying;
    internal double Effective(double configured){return weight*configured+offset;}
    internal bool Animating {get{return applying&&(weight>.0001||Math.Abs(offset-target)>.0001);}}
    internal void Step(double now,bool enabled,bool editing,bool? combat,int idlePercent) {
        if(!enabled||editing||!combat.HasValue){weight=1;offset=0;applying=false;return;}
        double desired=combat.Value?1:idlePercent/100.0;
        if(!applying||target!=desired){
            applying=true;fromWeight=weight;fromOffset=offset;target=desired;started=now;
        }
        double span=combat.Value?.16:.45;
        double progress=Math.Max(0,Math.Min(1,(now-started)/span));
        double smooth=progress*progress*(3-2*progress);
        weight=fromWeight*(1-smooth);
        offset=fromOffset+(target-fromOffset)*smooth;
    }
}
