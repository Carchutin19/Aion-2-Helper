using System;
using System.Collections.Generic;

// Own resources only. A profile establishes identity and explicit maxima; the
// observed resource messages update that actor on the same connection.
internal sealed class PlayerVitalSignal {
    internal sealed class Sample {
        internal uint Actor,Current,Maximum,Shield;
        internal string Flow;
        internal double Time,Traffic;
    }
    sealed class Flow {
        internal readonly PlayerShieldSignal Shields=new PlayerShieldSignal();
        internal uint Self,Map;
        internal uint? Hp,HpMaximum,Mp,MpMaximum;
        internal double Traffic,ProfileTime,HpTime,MpTime;
        internal string LastClear="New connection";
        internal double ClearedAt;
        internal void Clear(string reason,double time){Self=0;Hp=HpMaximum=Mp=MpMaximum=null;ProfileTime=HpTime=MpTime=0;Shields.Clear();LastClear=reason;ClearedAt=time;}
    }
    internal struct Points {
        internal uint Actor;
        internal uint? Hp,HpMaximum,Mp,MpMaximum;
        internal bool Energy;
    }
    readonly object gate=new object();
    readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();
    bool enabled;string active;
    internal Action Changed;
    internal void Configure(bool value){bool changed;lock(gate){changed=enabled!=value;if(changed){enabled=value;flows.Clear();active=null;}}if(changed&&Changed!=null)Changed();}
    internal void Reset(){lock(gate){flows.Clear();active=null;}if(Changed!=null)Changed();}
    internal static bool ReadPoints(byte[] b,int start,int end,out Points result){
        result=new Points();int p=start+2;
        if(!DpsSignal.Var(b,ref p,end,out result.Actor)||result.Actor==0||p>=end)return false;
        int flags=b[p++];if(flags>3)return false;
        for(int width=4;width<=8;width+=4)if((flags&(width==4?1:2))!=0){
            uint count;if(!DpsSignal.Var(b,ref p,end,out count)||count>(end-p)/(width+1))return false;
            for(uint i=0;i<count;i++){
                int kind=b[p++];long value=width==4?BitConverter.ToInt32(b,p):BitConverter.ToInt64(b,p);p+=width;
                if(width==4&&kind==3){if(result.Energy||result.Actor>int.MaxValue||value<0||value>100000000)return false;result.Energy=true;}
                if(width==4&&kind==1){if(value<0||value>10000000||result.Mp.HasValue)return false;result.Mp=(uint)value;}
                // Controlled 4620 -> 4591 change matched both the game UI and
                // the next own profile. Kind8 is an explicit MP maximum.
                if(width==4&&kind==8){if(value<=0||value>10000000||result.MpMaximum.HasValue)return false;result.MpMaximum=(uint)value;}
                if(width==8&&(kind==0||kind==7)){
                    if(value<0||value>1000000000||(kind==7&&value==0))return false;
                    if(kind==0){if(result.Hp.HasValue)return false;result.Hp=(uint)value;}
                    else{if(result.HpMaximum.HasValue)return false;result.HpMaximum=(uint)value;}
                }
            }
        }
        return p==end;
    }
    internal void Consume(byte[] b,int start,int end,double time,string key){
        if(b==null||start<0||end>b.Length||end-start<2||end-start>65536||string.IsNullOrEmpty(key)||double.IsNaN(time)||double.IsInfinity(time))return;
        bool changed=false;
        lock(gate){
            if(!enabled)return;Flow f;
            if(!flows.TryGetValue(key,out f)){
                if(flows.Count>=4){string remove=null;double oldest=double.MaxValue;foreach(var pair in flows)if(pair.Value.Traffic<oldest){remove=pair.Key;oldest=pair.Value.Traffic;}if(remove!=null)flows.Remove(remove);}
                flows[key]=f=new Flow();
            }
            if(f.Traffic>0&&time-f.Traffic>30){f.Clear("Traffic gap over 30 seconds",time);changed=key==active;}
            f.Traffic=Math.Max(f.Traffic,time);
            int op=(b[start]<<8)|b[start+1];
            if(op==0x3336){
                EnemyTargetSignal.Player player;
                if(EnemyTargetSignal.PlayerState(b,start,end,true,out player)&&time>=f.ProfileTime){
                    if(f.Self!=player.Actor)f.Shields.Clear();
                    f.Self=player.Actor;f.Hp=player.Current;f.HpMaximum=player.Maximum;f.Mp=player.Mana;f.MpMaximum=player.ManaMaximum;
                    f.ProfileTime=f.HpTime=f.MpTime=time;active=key;changed=true;
                }
            }else if(op==0x2136&&end-start==48&&BitConverter.ToUInt32(b,start+2)<=10000){
                uint map=BitConverter.ToUInt32(b,start+6);
                if(map>0&&map<10000000){
                    if(f.Map!=map){
                        if(f.Map!=0)f.Shields.Clear();
                        // The first map announces metadata for this connection,
                        // not a change away from an already established map.
                        // Its confirmed own profile may precede it by minutes.
                        if(f.Map!=0&&(f.ProfileTime==0||Math.Max(0,time-f.ProfileTime)>2))f.Clear("Known map changed",time);
                        f.Map=map;active=key;changed=true;
                    }
                }
            }else if(op==0x008d){
                Points points;
                if(ReadPoints(b,start,end,out points)){
                    if(points.Energy){if(f.Self!=0&&f.Self!=points.Actor)f.Clear("Own actor changed",time);f.Self=points.Actor;active=key;}
                    if(f.Self!=0&&points.Actor==f.Self){
                        if(time>=f.HpTime&&(points.Hp.HasValue||points.HpMaximum.HasValue)){
                            if(points.Hp.HasValue)f.Hp=points.Hp;if(points.HpMaximum.HasValue)f.HpMaximum=points.HpMaximum;f.HpTime=time;changed=true;
                        }
                        if(time>=f.MpTime&&(points.Mp.HasValue||points.MpMaximum.HasValue)){if(points.Mp.HasValue)f.Mp=points.Mp;if(points.MpMaximum.HasValue)f.MpMaximum=points.MpMaximum;f.MpTime=time;changed=true;}
                    }
                }
            }else if(op==0x1b92){
                uint actor;
                if(DpsSignal.PartyVital(b,start,end,out actor)&&actor==f.Self){
                    int p=start+2;uint unused,hp,max;
                    if(DpsSignal.Var(b,ref p,end,out unused)&&DpsSignal.Var(b,ref p,end,out hp)&&DpsSignal.Var(b,ref p,end,out max)){
                        if(time>=f.HpTime){f.Hp=hp;f.HpMaximum=max;f.HpTime=time;changed=true;}
                        if(time>=f.MpTime){f.Mp=BitConverter.ToUInt32(b,p);f.MpMaximum=BitConverter.ToUInt32(b,p+4);f.MpTime=time;changed=true;}
                    }
                }
            }else if(op==0x4236){
                int p=start+2;uint actor;if(DpsSignal.Var(b,ref p,end,out actor)&&end-p==2&&b[p]==0&&actor==f.Self){f.Clear("Own actor removed",time);changed=true;}
            }else if(op==0x2a38||op==0x2b38||op==0x0538||op==0x2c38){
                changed=f.Shields.Consume(b,start,end,time,f.Self)||changed;
            }
        }
        if(changed&&Changed!=null)Changed();
    }
    internal Sample Read(bool mana,double now){
        lock(gate){Flow f;if(!enabled||active==null||!flows.TryGetValue(active,out f)||f.Self==0||Math.Max(0,now-f.Traffic)>5)return null;
            uint? current=mana?f.Mp:f.Hp,max=mana?f.MpMaximum:f.HpMaximum;
            if(!current.HasValue||!max.HasValue||max.Value==0||current.Value>max.Value)return null;
            return new Sample{Actor=f.Self,Current=current.Value,Maximum=max.Value,Shield=mana?0:f.Shields.Read(now),Flow=active,Time=mana?f.MpTime:f.HpTime,Traffic=f.Traffic};
        }
    }
    // Private status only: retain why data was cleared so a later report can be
    // investigated even when packet capture began after the failing transition.
    // No endpoint, name, packet contents or session key is exposed here.
    internal object Diagnostics(bool mana,double now){
        lock(gate){Flow f=null;if(active!=null)flows.TryGetValue(active,out f);uint? current=f==null?null:mana?f.Mp:f.Hp,max=f==null?null:mana?f.MpMaximum:f.HpMaximum;
            string reason=!enabled?"Disabled":f==null?"No active connection":f.Self==0?"Own actor unknown":Math.Max(0,now-f.Traffic)>5?"Connection stale":!current.HasValue?"Current value missing":!max.HasValue||max.Value==0?"Explicit maximum missing":current.Value>max.Value?"Current exceeds received maximum":"Live";
            return new{reason=reason,actor=f==null?0:f.Self,map=f==null?0:f.Map,current=current,maximum=max,profileAgeSeconds=f==null||f.ProfileTime==0?(double?)null:Math.Max(0,now-f.ProfileTime),lastClear=f==null?null:f.LastClear,clearedAt=f==null||f.ClearedAt==0?(double?)null:f.ClearedAt};
        }
    }
}
