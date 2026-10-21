using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

// Original, passive resource reader. See assets/gathering/README.md. Three uses
// is a user-confirmed game rule, not a maximum decoded from an NPC HP field.
internal static class GatheringCatalogue {
 // DTO fields are populated by the JSON serializer.
 #pragma warning disable 0649
 internal sealed class Entry {public string en,es,group,typeEn,typeEs;}
 #pragma warning restore 0649
 internal static readonly Dictionary<string,Entry> Entries=Load();
 static Dictionary<string,Entry> Load(){using(var s=typeof(GatheringCatalogue).Assembly.GetManifestResourceStream("GatheringCatalogue.json")){if(s==null)return new Dictionary<string,Entry>();using(var r=new StreamReader(s))return new JavaScriptSerializer().Deserialize<Dictionary<string,Entry>>(r.ReadToEnd());}}
 internal static bool Known(uint id){return Entries.ContainsKey(id.ToString(System.Globalization.CultureInfo.InvariantCulture));}
 internal static string Name(uint id,string language){Entry e;return Entries.TryGetValue(id.ToString(),out e)?language=="es"?e.es:e.en:UiLanguage.Text("Unknown resource",language);}
 internal static string Type(uint id,string language){Entry e;if(!Entries.TryGetValue(id.ToString(),out e))return "";string native=language=="es"?e.typeEs:e.typeEn;if(!string.IsNullOrEmpty(native))return native;return e.group=="Food"?"":UiLanguage.Text(e.group=="Ore"?"Mineral":e.group=="Gemstone"?"Gemstone":e.group=="Herb"?"Herb":e.group=="Cotton"?"Fibre":e.group,language);}
}
internal sealed class GatheringSignal {
 internal sealed class Sample {internal uint Actor,Template;internal int Used;internal double Time;internal string Flow;internal int Remaining {get{return 3-Used;}}}
 sealed class Flow {internal uint Self,Selected,Candidate,Map;internal double Traffic;internal readonly Dictionary<uint,Sample> Nodes=new Dictionary<uint,Sample>();}
 readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();string active;bool enabled;internal Action Changed;
 internal void Configure(bool value){lock(gate){if(value==enabled)return;enabled=value;flows.Clear();active=null;}}
 internal void Reset(){lock(gate){flows.Clear();active=null;}Notify();}
 void Notify(){var callback=Changed;if(callback!=null)callback();}
 // Supported resource profile: varint actor,0400,template,position/rotation,
 // bounded static flags, timestamp, explicit consumed count and opaque tail.
 internal static bool Profile(byte[] b,int s,int e,out Sample value){value=null;int p=s+2;uint actor;if(!DpsSignal.Var(b,ref p,e,out actor)||actor==0||e-p!=52||b[p]!=4||b[p+1]!=0)return false;uint id=BitConverter.ToUInt32(b,p+2);if(!GatheringCatalogue.Known(id))return false;for(int i=0;i<3;i++){float n=BitConverter.ToSingle(b,p+6+4*i);if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>10000000)return false;}uint used=BitConverter.ToUInt32(b,p+37);if(used>3)return false;value=new Sample{Actor=actor,Template=id,Used=(int)used};return true;}
 internal void Consume(byte[] b,int s,int e,double time,string key){if(b==null||s<0||e>b.Length||e-s<2||e-s>65536||string.IsNullOrEmpty(key)||double.IsNaN(time)||double.IsInfinity(time))return;int op=(b[s]<<8)|b[s+1];if(op!=0x2136&&op!=0x3336&&op!=0x008d&&op!=0x3436&&op!=0x3438&&op!=0x3836&&op!=0x318D&&op!=0x3A36&&op!=0x3536)return;bool changed=false;lock(gate){if(!enabled)return;Flow f;if(!flows.TryGetValue(key,out f)){if(flows.Count==4){var oldest=flows.Where(x=>x.Key!=active).OrderBy(x=>x.Value.Traffic).FirstOrDefault();if(oldest.Key==null)return;flows.Remove(oldest.Key);}flows[key]=f=new Flow();}if(time<f.Traffic)return;if(f.Traffic>0&&time-f.Traffic>30){f.Nodes.Clear();f.Candidate=f.Selected=f.Self=0;if(active==key)active=null;changed=true;}f.Traffic=time;
 int p=s+2;uint actor,source;string name;byte role;
 if(op==0x2136&&e-s==48&&BitConverter.ToUInt32(b,s+2)<=10000){uint map=BitConverter.ToUInt32(b,s+6);if(map>0&&map<10000000){if(f.Map!=0&&f.Map!=map){f.Nodes.Clear();f.Candidate=f.Selected=f.Self=0;if(active==key)active=null;changed=true;}f.Map=map;}}
 else if(op==0x3336&&DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role)){if(f.Self!=0&&f.Self!=actor){f.Nodes.Clear();f.Selected=f.Candidate=0;}f.Self=actor;active=key;changed=true;}
 else if(op==0x008d){uint? current,maximum;bool energy;if(EnemyHealthSignal.Points(b,s,e,out actor,out current,out maximum,out energy)&&energy){if(f.Self!=0&&f.Self!=actor){f.Nodes.Clear();f.Selected=f.Candidate=0;}f.Self=actor;active=key;}}
 else if(op==0x3436){Sample node;if(Profile(b,s,e,out node)){Sample old;if(f.Nodes.TryGetValue(node.Actor,out old)&&old.Time>time)return;if(old!=null&&(old.Template!=node.Template||node.Used<old.Used)){if(f.Selected==node.Actor){f.Selected=0;changed=true;}if(f.Candidate==node.Actor)f.Candidate=0;}if(f.Nodes.Count>=512&&!f.Nodes.ContainsKey(node.Actor)){var first=f.Nodes.OrderBy(x=>x.Value.Time).First();if(first.Key==f.Selected)f.Selected=0;f.Nodes.Remove(first.Key);}node.Time=time;node.Flow=key;f.Nodes[node.Actor]=node;changed|=f.Selected==node.Actor;}}
 else if(op==0x3438&&key==active&&EnemyHealthSignal.Selection(b,s,e,out actor)){f.Candidate=f.Nodes.ContainsKey(actor)?actor:0;if(actor!=f.Selected){f.Selected=0;changed=true;}}
 else if(op==0x3836&&key==active&&DpsSignal.Var(b,ref p,e,out actor)&&DpsSignal.Var(b,ref p,e,out source)&&e-p==1&&b[p]==1&&source==f.Self&&f.Self!=0&&f.Nodes.ContainsKey(actor)){f.Selected=actor;f.Nodes[actor].Time=time;changed=true;}
 else if(op==0x318D&&key==active&&e-s>=37&&e-s<=4096&&b[s+2]==0&&b[s+3]==0){p=s+4;Sample node;if(DpsSignal.Var(b,ref p,e,out actor)&&e-p>=12&&(actor==f.Selected||actor==f.Candidate)&&f.Self!=0&&f.Nodes.TryGetValue(actor,out node)){uint attempt=BitConverter.ToUInt32(b,p);if(attempt>=1&&attempt<=3&&b[p+4]==1&&b[p+5]==0&&b[p+6]==0&&BitConverter.ToUInt32(b,p+7)==1){int used=(int)attempt-1;if(used>=node.Used){node.Used=used;node.Time=time;changed=actor==f.Selected;}}}} // Explicit current attempt ordinal; unknown minigame payload is not reward/HP data.
 else if(op==0x3A36&&DpsSignal.Var(b,ref p,e,out actor)&&DpsSignal.Var(b,ref p,e,out source)&&source!=0&&e-p==8){uint used=BitConverter.ToUInt32(b,p);Sample node;if(used<=3&&BitConverter.ToUInt32(b,p+4)==0&&f.Nodes.TryGetValue(actor,out node)){// State is authoritative even when another player used this node; focus is own only.
 if(used>=node.Used){node.Used=(int)used;node.Time=time;changed=key==active&&actor==f.Selected;if(used==3&&f.Selected==actor)f.Selected=0;}}}
 else if(op==0x3536&&DpsSignal.Var(b,ref p,e,out actor)&&e-p==3&&b[p+2]==0&&(b[p]==1||b[p]==4)){f.Nodes.Remove(actor);if(f.Candidate==actor)f.Candidate=0;if(f.Selected==actor){f.Selected=0;changed=true;}}
 }if(changed)Notify();}
 internal Sample Read(double now){lock(gate){Flow f;Sample n;if(!enabled||active==null||!flows.TryGetValue(active,out f)||f.Self==0||!f.Nodes.TryGetValue(f.Selected,out n)||n.Used>=3||now<f.Traffic||now-f.Traffic>15||now<n.Time||now-n.Time>30)return null;return new Sample{Actor=n.Actor,Template=n.Template,Used=n.Used,Time=n.Time,Flow=active};}}
}
