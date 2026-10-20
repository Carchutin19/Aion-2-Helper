using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

// Original reader of the MIT-documented collection/soul layouts. No upstream
// GPL application code is copied. Souls use Pet IDs, never kills or crystals.
internal sealed class PetProfileSave {public string Name;public int Server;internal Dictionary<int,long> Totals=new Dictionary<int,long>();public List<int> Pins=new List<int>();public string Updated;public Dictionary<string,long> Progress {get{return Totals.ToDictionary(p=>p.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),p=>p.Value);}set{Totals=new Dictionary<int,long>();if(value==null||value.Count>512)return;foreach(var pair in value){int id;if(int.TryParse(pair.Key,out id)&&id>0&&pair.Value>=0&&pair.Value<=1000000)Totals[id]=pair.Value;}}}}
internal sealed class PetSave {public int Version=1;public Dictionary<string,PetProfileSave> Profiles=new Dictionary<string,PetProfileSave>();public string Last;}
internal sealed class PetScopeChoice {internal string Key;internal string Label;public override string ToString(){return Label;}}
internal sealed class PetSnapshot {
    internal string Name,Quality,Error;internal int Server,Map,Revision;internal bool Live,Exact;internal Dictionary<int,long> Totals=new Dictionary<int,long>();internal int[] Pins=new int[0];
    internal PetProgress Progress(int id){long total;return Totals.TryGetValue(id,out total)?PetProgress.From(total):null;}
}
internal sealed class PetSoul {internal int Id,Amount;internal double Time;}
internal sealed class PetSignal {
    sealed class Flow {internal string Name;internal int Server,Map;internal uint Actor;internal double Time,ProfileTime,CollectionTime;internal Dictionary<int,long> Collection;internal bool Exact;internal List<PetSoul> Pending=new List<PetSoul>();}
    readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();readonly Queue<PetSoul> events=new Queue<PetSoul>();readonly PetAssets assets;readonly string savePath;PetSave saved=new PetSave();volatile string active;string key,error;double activeProfileTime,dirtyAt,activeTraffic;bool dirty;volatile bool enabled;int revision;
    internal PetSignal(PetAssets assets,string root,bool persist){this.assets=assets;savePath=persist?Path.Combine(root,"pets-state.json"):null;if(savePath!=null)try{if(File.Exists(savePath)&&new FileInfo(savePath).Length<1000000){var data=new JavaScriptSerializer{MaxJsonLength=1000000}.Deserialize<PetSave>(File.ReadAllText(savePath));if(data!=null&&data.Version==1&&data.Profiles!=null&&data.Profiles.Count<=16){saved=data;key=data.Last;foreach(var p in saved.Profiles.Values){if(p.Totals==null||p.Totals.Count>512)p.Totals=new Dictionary<int,long>();foreach(int id in p.Totals.Where(v=>v.Value<0||v.Value>1000000).Select(v=>v.Key).ToArray())p.Totals.Remove(id);if(p.Pins==null)p.Pins=new List<int>();p.Pins=p.Pins.Where(id=>assets.Pets.ContainsKey(id)).Distinct().Take(32).ToList();}}}}catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is InvalidOperationException))throw;error=ex.Message;}}
    internal int Revision {get{lock(gate)return revision;}}
    internal bool Recent(double now){double last=System.Threading.Interlocked.CompareExchange(ref activeTraffic,0,0);return last>0&&now>=last&&now-last<5;}
    internal void Configure(bool value){lock(gate){if(enabled==value)return;enabled=value;ResetLocked();}}
    void ResetLocked(){flows.Clear();active=null;activeProfileTime=0;events.Clear();revision++;}
    internal void Reset(){lock(gate)ResetLocked();}
    internal void Gap(){lock(gate){foreach(var f in flows.Values)f.Exact=false;revision++;}}
    static bool Collection(byte[] b,int start,int end,out Dictionary<int,long> totals){totals=null;int p=start+6;uint count;if(end-start<8||!DpsSignal.Var(b,ref p,end,out count)||count>512||count>(end-p)/12)return false;var levels=new Dictionary<int,int>();var points=new Dictionary<int,int>();
        for(int i=0;i<count;i++){uint id=BitConverter.ToUInt32(b,p),repeat=BitConverter.ToUInt32(b,p+4),level=BitConverter.ToUInt32(b,p+8);p+=12;if(id==0||id>int.MaxValue||id!=repeat||level>3||levels.ContainsKey((int)id))return false;levels[(int)id]=(int)level;}
        if(!DpsSignal.Var(b,ref p,end,out count)||count>512||count>(end-p)/8)return false;
        for(int i=0;i<count;i++){uint id=BitConverter.ToUInt32(b,p),point=BitConverter.ToUInt32(b,p+4);p+=8;if(id==0||id>int.MaxValue||point>74||points.ContainsKey((int)id))return false;points[(int)id]=(int)point;}
        var result=new Dictionary<int,long>();foreach(int id in levels.Keys.Union(points.Keys)){int level,point;levels.TryGetValue(id,out level);points.TryGetValue(id,out point);int goal=level==0?5:level==1?25:level==2?75:1;if(point>=goal)return false;result[id]=(level==0?0:level==1?5:level==2?30:105)+point;}totals=result;return true;
    }
    internal static bool Identity(byte[] b,int s,int e,out string name,out int server,out uint actor){name=null;server=0;actor=0;EnemyTargetSignal.Player player;if(e-s<128||!EnemyTargetSignal.PlayerState(b,s,e,true,out player))return false;int p=s+2;uint value;if(!DpsSignal.Var(b,ref p,e,out value))return false;var expected=Encoding.UTF8.GetBytes(player.Name);for(int q=p;q<Math.Min(p+16,e-12);q++){if(b[q]!=expected.Length||q+1+expected.Length+7>e)continue;bool same=true;for(int i=0;i<expected.Length;i++)if(b[q+1+i]!=expected[i]){same=false;break;}if(!same)continue;int after=q+1+expected.Length;uint role=BitConverter.ToUInt32(b,after+2);int found=BitConverter.ToUInt16(b,after);if(role<5||role>36||found<1000||found>=3000||(b[after+6]!=1&&b[after+6]!=2))continue;name=player.Name;server=found;actor=player.Actor;return true;}return false;}
    internal void Consume(byte[] b,int s,int e,double time,string flow){if(!enabled||b==null||s<0||e>b.Length||e-s<2||e-s>65536||string.IsNullOrEmpty(flow)||double.IsNaN(time)||double.IsInfinity(time))return;if(flow==active)System.Threading.Interlocked.Exchange(ref activeTraffic,time);int op=(b[s]<<8)|b[s+1];if(op!=0x0090&&op!=0x0D90&&op!=0x3336&&op!=0x2136&&op!=0x1536)return;
        Dictionary<int,long> collection=null;List<PetSoul> gains=null;string name=null;int server=0,map=0;uint actor=0;bool valid=true;
        if(op==0x0090)valid=Collection(b,s,e,out collection);
        else if(op==0x0D90){valid=e-s>=11&&b[s+2]>=1&&b[s+2]<=127&&e-s==3+b[s+2]*8;if(valid){gains=new List<PetSoul>();for(int p=s+3;p<e;p+=8){uint id=BitConverter.ToUInt32(b,p),amount=BitConverter.ToUInt32(b,p+4);if(id==0||id>int.MaxValue||amount==0||amount>=1000){valid=false;break;}gains.Add(new PetSoul{Id=(int)id,Amount=(int)amount,Time=time});}}}
        else if(op==0x3336){if(!Identity(b,s,e,out name,out server,out actor))return;}
        else if(op==0x2136){if(e-s!=48||BitConverter.ToUInt32(b,s+2)>10000)return;uint id=BitConverter.ToUInt32(b,s+6);if(id==0||id>=10000000)return;map=(int)id;}
        lock(gate){if(!enabled)return;Flow f;if(!flows.TryGetValue(flow,out f)){if(flows.Count>=4){string remove=flows.Where(v=>v.Key!=active).OrderBy(v=>v.Value.Time).Select(v=>v.Key).FirstOrDefault();if(remove==null)return;flows.Remove(remove);}flows[flow]=f=new Flow();}if(time<f.Time)return;f.Time=time;
            if(!valid){f.Exact=false;error="Unsupported pet message";revision++;return;}
            if(op==0x1536){if(active==flow){active=null;events.Clear();revision++;}return;}
            if(op==0x2136){if(f.Map!=map){f.Map=map;revision++;}return;}
            if(op==0x3336){bool same=f.Name==name&&f.Server==server&&f.Actor==actor;if(!same&&f.Name!=null){f.Collection=null;f.Pending.Clear();f.Exact=false;}f.Name=name;f.Server=server;f.Actor=actor;f.ProfileTime=time;if(time>=activeProfileTime){bool switched=active!=flow||key!=Key(name,server);active=flow;activeProfileTime=time;System.Threading.Interlocked.Exchange(ref activeTraffic,time);key=Key(name,server);if(switched)events.Clear();Profile(name,server);if(f.Collection!=null)Synchronize(f);revision++;}return;}
            if(op==0x0090){f.Collection=collection;f.CollectionTime=time;f.Exact=true;f.Pending.Clear();if(active==flow&&f.Name!=null)Synchronize(f);revision++;return;}
            foreach(var gain in gains){if(active==flow&&f.Name!=null&&f.Collection!=null)Gain(f,gain);else if(f.Collection!=null){if(f.Pending.Count<64)f.Pending.Add(gain);else{f.Exact=false;revision++;}}}
        }
    }
    static string Key(string name,int server){return server+"/"+name;}
    PetProfileSave Profile(string name,int server){string id=Key(name,server);PetProfileSave p;if(!saved.Profiles.TryGetValue(id,out p)){while(saved.Profiles.Count>=16){string remove=saved.Profiles.Keys.FirstOrDefault(k=>k!=key);if(remove==null)break;saved.Profiles.Remove(remove);}saved.Profiles[id]=p=new PetProfileSave{Name=name,Server=server};}saved.Last=id;return p;}
    void Synchronize(Flow f){var p=Profile(f.Name,f.Server);p.Totals=new Dictionary<int,long>(f.Collection);p.Updated=DateTime.UtcNow.ToString("o");error=null;dirty=true;foreach(var gain in f.Pending.ToArray())if(gain.Time>=f.CollectionTime)Gain(f,gain);f.Pending.Clear();revision++;}
    void Gain(Flow f,PetSoul gain){var p=Profile(f.Name,f.Server);long previous;if(!p.Totals.TryGetValue(gain.Id,out previous)){if(!f.Exact||!assets.Pets.ContainsKey(gain.Id)){if(events.Count<64)events.Enqueue(gain);revision++;return;}previous=0;}if(previous>=105)return;long next=Math.Min(1000000,previous+gain.Amount);p.Totals[gain.Id]=next;f.Collection[gain.Id]=next;p.Updated=DateTime.UtcNow.ToString("o");dirty=true;if(next<105){while(events.Count>=64)events.Dequeue();events.Enqueue(gain);}revision++;}
    internal PetSnapshot Read(){lock(gate){var state=new PetSnapshot{Revision=revision,Quality="Waiting for collection",Error=error};PetProfileSave p;if(key!=null&&saved.Profiles.TryGetValue(key,out p)){state.Name=p.Name;state.Server=p.Server;state.Totals=new Dictionary<int,long>(p.Totals);state.Pins=p.Pins.ToArray();state.Quality="Saved collection · not synchronized";}Flow f;if(active!=null&&flows.TryGetValue(active,out f)){state.Map=f.Map;state.Live=f.Collection!=null;state.Exact=state.Live&&f.Exact;state.Quality=state.Live?(f.Exact?"Collection synchronized":"Collection needs verification"):"Waiting for collection";}return state;}}
    internal PetSoul[] Drain(){lock(gate){var result=events.ToArray();events.Clear();return result;}}
    internal PetScopeChoice[] Profiles(){lock(gate)return saved.Profiles.Select(p=>new PetScopeChoice{Key=p.Key,Label=p.Value.Name+" · "+p.Value.Server}).OrderBy(p=>p.Label).ToArray();}
    internal PetSnapshot ReadProfile(string selected){lock(gate){if(selected==null||selected==key)return Read();PetProfileSave p;if(!saved.Profiles.TryGetValue(selected,out p))return new PetSnapshot{Quality="Waiting for collection"};return new PetSnapshot{Name=p.Name,Server=p.Server,Revision=revision,Quality="Saved collection · not synchronized",Totals=new Dictionary<int,long>(p.Totals),Pins=p.Pins.ToArray()};}}
    internal void Pin(int id,bool pin){lock(gate){PetProfileSave p;if(key==null||!saved.Profiles.TryGetValue(key,out p)||!assets.Pets.ContainsKey(id))return;long total;bool completed=p.Totals.TryGetValue(id,out total)&&total>=105;p.Pins.Remove(id);if(pin&&!completed&&p.Pins.Count<32)p.Pins.Add(id);dirty=true;revision++;}}
    internal void Save(double now,bool force=false){if(savePath==null)return;string text;lock(gate){if(!dirty)return;if(!force&&now-dirtyAt<2)return;dirtyAt=now;text=new JavaScriptSerializer{MaxJsonLength=1000000}.Serialize(saved);dirty=false;}try{string tmp=savePath+".tmp";File.WriteAllText(tmp,text,new UTF8Encoding(false));if(File.Exists(savePath))File.Replace(tmp,savePath,null);else File.Move(tmp,savePath);}catch(IOException ex){lock(gate){dirty=true;error=ex.Message;}}catch(UnauthorizedAccessException ex){lock(gate){dirty=true;error=ex.Message;}}}
}
