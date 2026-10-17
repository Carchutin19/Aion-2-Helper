using System;
using System.Collections.Generic;

// Only the two shield families verified in controlled captures. Other periodic
// values can be buffs, resources or healing and must not become shield capacity.
internal sealed class PlayerShieldSignal {
    sealed class Entry {
        internal uint Effect,Skill,Remaining;
        internal double Time,Expires;
        internal long WireExpiry;
        internal bool Removed;
    }
    internal sealed class Status {
        internal uint Instance,Effect,Skill;
        internal long Duration,Expiry;
        internal bool Removing;
    }
    readonly Dictionary<uint,Entry> entries=new Dictionary<uint,Entry>();
    internal void Clear(){entries.Clear();}
    static bool Family(uint effect,uint skill,bool tick){return (effect==(tick?1824001011u:182400101u)&&(skill==0||skill==18240010))||(effect==(tick?1873000211u:187300021u)&&(skill==0||skill==18730002));}
    internal static bool IsTick(uint effect,uint skill){return Family(effect,skill,true);}
    // Booleans share a bit byte even when ordinary fields occur between them.
    sealed class Wire {
        internal int Position;readonly byte[] bytes;readonly int end;int bits=8,bitByte;
        internal Wire(byte[] b,int start,int end){bytes=b;Position=start+2;this.end=end;}
        internal bool Done {get{return Position==end;}}
        internal bool Skip(int count){if(count<0||count>end-Position)return false;Position+=count;return true;}
        internal bool Byte(out int value){value=0;if(Position>=end)return false;value=bytes[Position++];return true;}
        internal bool Var(out uint value){return DpsSignal.Var(bytes,ref Position,end,out value);}
        internal bool U32(out uint value){value=0;if(end-Position<4)return false;value=BitConverter.ToUInt32(bytes,Position);Position+=4;return true;}
        internal bool I64(out long value){value=0;if(end-Position<8)return false;value=BitConverter.ToInt64(bytes,Position);Position+=8;return true;}
        internal bool Boolean(out bool value){value=false;if(bits==8){if(!Byte(out bitByte))return false;bits=0;}value=(bitByte&(1<<bits++))!=0;return true;}
    }
    internal static bool ReadStatuses(byte[] b,int start,int end,out uint actor,out List<Status> statuses){
        actor=0;statuses=null;var w=new Wire(b,start,end);uint count=1;bool single=b[start]==0x2b;
        if(!w.Var(out actor)||actor==0||(!single&&!w.Var(out count))||count>128)return false;
        var parsed=new List<Status>();
        for(uint i=0;i<count;i++){
            int flags,level;uint instance,effect,source=0,skill=0,unused;long duration,expiry;bool removing;
            if(!w.Byte(out flags)||flags>31||!w.Var(out instance)||!w.U32(out effect)||!w.I64(out duration)||!w.I64(out expiry)||((flags&1)!=0&&!w.Var(out source))||!w.Byte(out level)||((flags&2)!=0&&!w.U32(out skill))||!w.Boolean(out removing)||((flags&4)!=0&&!w.Var(out unused))||((flags&8)!=0&&!w.Skip(4))||((flags&16)!=0&&!w.Skip(12)))return false;
            parsed.Add(new Status{Instance=instance,Effect=effect,Skill=skill,Duration=duration,Expiry=expiry,Removing=removing});
        }
        bool last;if(single&&!w.Boolean(out last)||!w.Done)return false;statuses=parsed;return true;
    }
    Entry Get(uint instance){Entry e;if(entries.TryGetValue(instance,out e))return e;if(entries.Count>=32){uint remove=0;double oldest=double.MaxValue;foreach(var p in entries)if(p.Value.Time<oldest){remove=p.Key;oldest=p.Value.Time;}entries.Remove(remove);}entries[instance]=e=new Entry();return e;}
    internal bool Consume(byte[] b,int start,int end,double time,uint self){
        if(self==0||b==null||start<0||end>b.Length||end-start<2||double.IsNaN(time)||double.IsInfinity(time))return false;
        int actorPosition=start+2;uint target;if(!DpsSignal.Var(b,ref actorPosition,end,out target)||target!=self)return false;
        int op=(b[start]<<8)|b[start+1];
        if(op==0x2a38||op==0x2b38){
            uint actor;List<Status> statuses;if(!ReadStatuses(b,start,end,out actor,out statuses)||actor!=self)return false;
            bool changed=false;foreach(var s in statuses){if(!Family(s.Effect,s.Skill,false))continue;Entry e;if(entries.TryGetValue(s.Instance,out e)&&time<e.Time)continue;
                if(s.Removing){e=Get(s.Instance);e.Remaining=0;e.Removed=true;e.Time=time;changed=true;continue;}
                if(s.Duration<=0||s.Duration>300000)continue;
                e=Get(s.Instance);if(e.Removed&&time<=e.Time)continue;
                if(e.Effect!=s.Effect||e.Removed||e.WireExpiry!=s.Expiry){e.Remaining=0;e.Expires=time+s.Duration/1000.0;}
                e.WireExpiry=s.Expiry;e.Effect=s.Effect;e.Skill=s.Effect==182400101?18240010u:18730002u;e.Time=time;e.Removed=false;changed=true;
            }return changed;
        }
        if(op==0x0538){
            var w=new Wire(b,start,end);uint actor,source,instance,effect,remaining=0,absorbed=0,skill=0;int flags;
            if(!w.Var(out actor)||!w.Byte(out flags)||(flags&~11)!=0||!w.Var(out source)||!w.Var(out instance)||!w.U32(out effect)||((flags&1)!=0&&!w.Var(out remaining))||((flags&2)!=0&&!w.Var(out absorbed))||((flags&8)!=0&&!w.U32(out skill))||!w.Done||actor!=self||!Family(effect,skill,true)||remaining>1000000000)return false;
            Entry e;if(!entries.TryGetValue(instance,out e)||e.Removed||time<e.Time||time>=e.Expires||e.Effect!=effect/10||(skill!=0&&e.Skill!=skill))return false;
            // The source here can be the attacking NPC, not the shield caster.
            // Absent remaining is not zero: wait for the explicit removal.
            e.Time=time;if((flags&1)!=0)e.Remaining=remaining;return (flags&1)!=0;
        }
        if(op==0x2c38){
            var w=new Wire(b,start,end);uint actor,count;if(!w.Var(out actor)||!w.Var(out count)||count>128)return false;var removed=new List<uint>();
            for(uint i=0;i<count;i++){int flags,reason;uint instance,unused;if(!w.Byte(out flags)||flags>7||!w.Var(out instance)||!w.Byte(out reason)||((flags&1)!=0&&!w.Var(out unused))||((flags&2)!=0&&!w.Skip(4))||((flags&4)!=0&&!w.Skip(4)))return false;removed.Add(instance);}
            if(!w.Done||actor!=self)return false;bool changed=false;foreach(uint instance in removed){Entry e;if(entries.TryGetValue(instance,out e)&&time>=e.Time){e.Remaining=0;e.Time=time;e.Removed=true;changed=true;}}return changed;
        }return false;
    }
    internal uint Read(double now){
        // Concurrent stacking has not been validated. Show the largest confirmed
        // active shield, rather than adding capacities whose stacking is unknown.
        uint value=0;foreach(var e in entries.Values)if(!e.Removed&&now<e.Expires)value=Math.Max(value,e.Remaining);return value;
    }
}
