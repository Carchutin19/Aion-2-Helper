using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

internal static class CaptureRecoveryVerification {
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
 static void Var(List<byte> b,uint value){do{byte x=(byte)(value&127);value>>=7;if(value!=0)x|=128;b.Add(x);}while(value!=0);}
 static byte[] Frame(params byte[][] messages){var b=new List<byte>();foreach(var body in messages){Var(b,(uint)body.Length+4);b.AddRange(body);}return b.ToArray();}
 static byte[] Energy(uint actor,uint amount){var b=new List<byte>{0,0x8d};Var(b,actor);b.AddRange(new byte[]{1,1,3});b.AddRange(BitConverter.GetBytes(amount));return b.ToArray();}
 static Segment S(byte[] b,uint seq=100,int port=13328){return new Segment{Src="192.0.2.1",Dst="192.0.2.2",SrcPort=port,DstPort=51000,Seq=seq,Data=b};}
 static Dictionary<string,string> Dual(bool reverse,out Dictionary<string,int> owners){var result=new Dictionary<string,string>();var pids=new Dictionary<string,int>();Action first=delegate{Recorder.AddOwnedSocket(result,pids,"127.0.0.1:58449","127.0.0.1:58450",11);};Action second=delegate{Recorder.AddOwnedSocket(result,pids,"127.0.0.1:58450","127.0.0.1:58449",22);};if(reverse){second();first();}else{first();second();}owners=pids;return result;}
 static byte[] Bar(){return (byte[])typeof(SkillSlotsVerification).GetMethod("Bar",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
 static byte[] Hit(uint actor,uint target,uint amount){return (byte[])typeof(FloatingTextVerification).GetMethod("Direct",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{actor,target,amount,15210000u,2u});}
 internal static void Run(string root){
  var sockets=new GameSocketFilter();Dictionary<string,int> a,b;var forward=Dual(false,out a);var reverse=Dual(true,out b);
  Check(forward.Count==2&&forward.All(x=>x.Value=="both")&&forward.OrderBy(x=>x.Key).SequenceEqual(reverse.OrderBy(x=>x.Key)),"Both Aion-owned endpoints merge independently of enumeration order");
  Check(a.OrderBy(x=>x.Key).SequenceEqual(b.OrderBy(x=>x.Key))&&a["127.0.0.1:58449>127.0.0.1:58450"]==22,"Incoming destination owner remains correct for dual-owned sockets");
  sockets.Update(forward,false);string stable=sockets.Expression;
  for(int i=0;i<400;i++){Dictionary<string,int> owners;sockets.Update(Dual(i%2==0,out owners),false);Check(sockets.Expression==stable,"Reversing both-owned TCP rows cannot flip the filter");}
  int standardApplies=0,alternateApplies=0;var queued=new Queue<byte[]>();var standard=new CaptureFilterLane(false,false);var alternate=new CaptureFilterLane(true,false);
  // Model Npcap clearing a handle's pending packets when installing its filter.
  // These callbacks exercise the actual production lane selector, not a copy.
  Action<string> installStandard=delegate{standardApplies++;queued.Clear();};Action<string> installAlternate=delegate{alternateApplies++;};
  standard.Update(sockets,false,installStandard);alternate.Update(sockets,false,installAlternate);
  byte[] login=Frame(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Bar(),Energy(601,100));queued.Enqueue(login.Take(7).ToArray());queued.Enqueue(login.Skip(7).ToArray());
  for(int i=0;i<200;i++){Dictionary<string,int> owners;var changing=Dual(i%2==0,out owners);Recorder.AddOwnedSocket(changing,owners,"192.0.2.2:"+(52000+i),"192.0.2.3:"+(55000+i),33);sockets.Update(changing,false);standard.Update(sockets,false,installStandard);alternate.Update(sockets,false,installAlternate);}
  Check(standardApplies==1&&alternateApplies>100&&queued.Count==2,"Auxiliary filter churn cannot flush normal gameplay's queued login");
  var skills=new SkillSlotsSignal();skills.Configure(true);var floats=new FloatingTextSignal();floats.Configure(new FloatingTextOptions{Enabled=true},false);var vital=new PlayerVitalSignal();vital.Configure(true);var stagger=new StaggerSignal();stagger.Configure(true);
  var decoder=new DashSignal(root);decoder.OnFrame=delegate(byte[] data,int start,int end,double time,string flow){skills.Consume(data,start,end,time,flow);floats.Consume(data,start,end,time,flow);vital.Consume(data,start,end,time,flow);stagger.Consume(data,start,end,time,flow);};uint sequence=100;
  while(queued.Count>0){var data=queued.Dequeue();decoder.Consume(S(data,sequence),1000);sequence+=(uint)data.Length;}
  Check(skills.Read(1000)!=null&&skills.Read(1000).Self==601&&skills.Read(1000).Slots.Count==2&&vital.Read(false,1000).Current==10000,"Actual own bar and HP initialize from retained fragmented login");
  var combat=Frame(ProgressVerification.Gauge(99,100,150),Hit(601,99,321));decoder.Consume(S(combat,sequence),1000.1);sequence+=(uint)combat.Length;
  var hits=floats.Drain(1000.2);Check(hits.Length==1&&hits[0].Amount==321&&stagger.Read().ConfirmedTarget,"Retained profile drives actual floating hits and confirmed stagger target");
  byte[] zero=Frame(ProgressVerification.Gauge(99,0,150,true));decoder.Consume(S(zero,sequence),1000.3);sequence+=(uint)zero.Length;
  Check(stagger.Read().Current==0&&stagger.Read().BurstSerial>0,"A subsequent stagger depletion uses fresh received data");
  byte[] unconfirmed=Frame(Energy(602,200),Hit(602,99,999),Energy(602,201));var missingProfile=S(unconfirmed);missingProfile.DstPort=51001;decoder.Consume(missingProfile,1000.4);Check(floats.Drain(1000.5).Length==0&&skills.Read(1000.5).Self==601,"A lost new profile cannot be replaced with guessed identity or previous bar assignments");
  byte[] nextLogin=Frame(EnemyTargetVerification.Player(602,"OtherFixture",9000,11000,true),Bar(),Energy(602,200));var next=S(nextLogin,(uint)(100+unconfirmed.Length));next.DstPort=51001;decoder.Consume(next,1001);
  Check(skills.Read(1001).Self==602,"A received new profile/bar replaces old stream identity");
  var sink=new DashSignal(root);int frames=0;sink.OnFrame=delegate{frames++;};var router=new GameFlowRouter(root,sink);byte[] noise=Frame(Energy(601,1),Energy(601,2),Energy(601,3));router.Observe(S(noise),1000,true);
  Check(frames==0,"Ambiguous ownership cannot use the ordinary-port direct shortcut");router.Observe(S(noise,(uint)(100+noise.Length)),1000.1,false);Check(frames==0,"An ambiguous stream stays quarantined if one endpoint disappears");
  byte[] proof=Frame(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Energy(601,4));router.Observe(S(proof,(uint)(100+noise.Length*2)),1000.2,true);Check(frames==8&&sink.Samples==7,"Coherent own profile and matching energy release buffered order once");
  router.Observe(S(proof,(uint)(100+noise.Length*2)),1000.3,true);Check(frames==8,"Duplicate ambiguous deliveries remain deduplicated");
  standard.Update(sockets,true,installStandard);Check(standardApplies==2,"Only explicit outgoing-position mode change updates the standard filter");
  var research=new CaptureFilterLane(false,true);string researchRule=null;research.Update(sockets,false,delegate(string expression){researchRule=expression;});Check(researchRule=="tcp","Explicit research recorder policy is preserved");
  using(var recorder=new Recorder(root,false)){var flags=BindingFlags.Instance|BindingFlags.NonPublic;recorder.ApplyOwnershipSnapshot(forward,a,new Dictionary<int,long>{{11,100},{22,200}},new HashSet<string>());typeof(Recorder).GetField("active",flags).SetValue(recorder,true);recorder.OnSegment=delegate{};Check(recorder.IncomingSocketKeys().Length==2&&recorder.Incoming("127.0.0.1:58449>127.0.0.1:58450"),"Both local destinations remain owned, verification-required candidates");typeof(Recorder).GetField("liveStandardWorkers",flags).SetValue(recorder,1);typeof(Recorder).GetField("liveWorkers",flags).SetValue(recorder,2);typeof(Recorder).GetMethod("WorkerEnded",flags).Invoke(recorder,new object[]{false});Check(!recorder.Active,"An idle alternate worker cannot conceal failure of all primary capture workers");}
  VerifyContinuity(root);
  File.WriteAllText(Path.Combine(root,"capture-recovery-test.txt"),"PASS: actual dual-owned direction/owner merging, 400 reversed row enumerations, native-like filter flush callbacks on production lane class, 200 auxiliary updates preserve queued fragmented login, actual Hotbar/HP/Floating/Stagger receivers, received character handoff, ambiguous standard-port quarantine and ordered verification/deduplication, explicit outgoing mode, retained research policy. Bounded exact incoming ownership retention through absent Windows observations, actual Recorder/router/decoder immediate damage and 65-packet no-gap regression, process generation and tuple reuse, SYN/FIN/RST, idle/hard expiry, unrelated tuple rejection. No live capture or packet sends; no affected-PC performance claim.");
 }
 static void VerifyContinuity(string root){
  byte[] control=new byte[54];control[12]=8;control[14]=0x45;control[17]=40;control[23]=6;control[46]=0x50;control[47]=2;
  Check(Segment.Parse(control,1)==null&&Segment.Parse(control,1,control.Length,true).Data.Length==0,"Only explicitly enabled capture parses payload-free SYN controls; research/ACK policy preserved");control[47]=16;Check(Segment.Parse(control,1,control.Length,true)==null,"Pure ACKs do not allocate ownership/decoder work");
  double clock=100;var owned=new OwnedConnectionContinuity();owned.Clock=delegate{return clock;};
  var packet=S(new byte[]{1});var directions=new Dictionary<string,string>{{packet.Key,"in"}};var owners=new Dictionary<string,int>{{packet.Key,42}};var live=new Dictionary<int,long>{{42,10000}};var empty=new Dictionary<string,string>();var none=new Dictionary<string,int>();var closed=new HashSet<string>();OwnedConnectionContinuity.Decision decision;
  owned.Update(directions,owners,live,closed);Check(owned.Resolve(packet,false,out decision),"Current exact Windows ownership accepted");string original=decision.Stream;
  owned.Update(empty,none,live,closed);Check(!owned.Resolve(packet,false,out decision),"A stream without coherent own protocol proof cannot survive absent ownership");
  owned.Update(directions,owners,live,closed);owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);clock+=1;owned.Update(empty,none,live,closed);
  for(int i=0;i<100;i++){clock+=.1;Check(owned.Resolve(packet,false,out decision)&&decision.Retained&&decision.Stream==original,"Validated exact incoming stream stays continuous through missing Windows observations");}
  var other=S(new byte[]{1});other.DstPort++;Check(!owned.Resolve(other,false,out decision),"An unrelated tuple never borrows proof");
  clock+=31;Check(!owned.Resolve(packet,false,out decision),"Idle continuation expires");
  owned.Update(directions,owners,live,closed);owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);clock+=1;owned.Update(empty,none,live,closed);
  for(int i=0;i<121;i++){clock+=1;bool accepted=owned.Resolve(packet,false,out decision);if(i<119)Check(accepted,"Continuous traffic allowed within hard deadline");else Check(!accepted,"Traffic cannot renew hard absence deadline");}
  owned.Update(directions,owners,live,closed);owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);live[42]=20000;owned.Update(empty,none,live,closed);Check(!owned.Resolve(packet,false,out decision),"PID reuse with new process start cannot inherit verified ownership");
  owned.Update(directions,owners,live,closed);owned.Resolve(packet,false,out decision);Check(decision.Stream!=original,"Process replacement gets distinct receiver namespace");owned.Confirm(decision.Stream);string before=decision.Stream;
  packet.Flags=4;owned.Control(packet);Check(!owned.Resolve(packet,false,out decision),"Captured RST immediately retires old proof even while TCP table is stale");
  packet.Flags=2;owned.Control(packet);Check(owned.Resolve(packet,false,out decision)&&decision.Stream!=before,"SYN begins a new unverified stream namespace");owned.Update(empty,none,live,closed);Check(!owned.Resolve(packet,false,out decision),"SYN cannot inherit old proof while ownership is missing");
  owned.Update(directions,owners,live,closed);owned.Resolve(packet,false,out decision);string synStream=decision.Stream;owned.Control(packet);owned.Resolve(packet,false,out decision);Check(decision.Stream==synStream,"Duplicate SYN cannot repeatedly change the receiver namespace");
  packet.Flags=1;owned.Control(packet);Check(!owned.Resolve(packet,false,out decision),"Captured FIN retires continuity even when table still lists connection");packet.Flags=2;packet.Seq=700;owned.Control(packet);owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);owned.Update(empty,none,live,closed);packet.Flags=0;packet.Seq=40000000;Check(!owned.Resolve(packet,false,out decision),"An unrelated new sequence cannot use a retained socket's proof");packet.Seq=100;
  owned.Update(directions,owners,live,closed);packet.Flags=0;owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);closed.Add(packet.Key);owned.Update(empty,none,live,closed);Check(!owned.Resolve(packet,false,out decision),"Explicit Windows close stops retention");closed.Clear();
  owned.Update(directions,owners,new Dictionary<int,long>{{42,0}},closed);owned.Resolve(packet,false,out decision);owned.Confirm(decision.Stream);owned.Update(empty,none,new Dictionary<int,long>{{42,0}},closed);Check(!owned.Resolve(packet,false,out decision),"Unreadable process generation cannot retain an absent socket");

  // Exercise the actual Recorder -> GameFlowRouter -> DashSignal -> receivers
  // path that previously rejected a frame and released a delayed count-limit burst.
  using(var recorder=new Recorder(root,false)){
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var state=(OwnedConnectionContinuity)typeof(Recorder).GetField("continuity",flags).GetValue(recorder);clock=100;state.Clock=delegate{return clock;};
   typeof(Recorder).GetField("active",flags).SetValue(recorder,true);var signal=new DashSignal(root);signal.DiagnosticClock=delegate{return clock;};var route=new GameFlowRouter(root,signal);route.OnVerified=recorder.ConfirmGameplay;
   var skills=new SkillSlotsSignal();skills.Configure(true);var floats=new FloatingTextSignal();floats.Configure(new FloatingTextOptions{Enabled=true},false);var vital=new PlayerVitalSignal();vital.Configure(true);var gauge=new StaggerSignal();gauge.Configure(true);
   signal.OnFrame=delegate(byte[] data,int start,int end,double time,string flow){route.ObserveFrame(data,start,end,flow);skills.Consume(data,start,end,time,flow);floats.Consume(data,start,end,time,flow);vital.Consume(data,start,end,time,flow);gauge.Consume(data,start,end,time,flow);};
   uint seq=100;Action<byte[],double> send=delegate(byte[] bytes,double time){var segment=S(bytes,seq);seq+=(uint)bytes.Length;OwnedConnectionContinuity.Decision accepted;Check(recorder.ResolveOwned(segment,out accepted),"Actual Recorder must keep each confirmed packet during missing table polls");segment.ProtocolStream=accepted.Stream;route.Observe(segment,time,accepted.Ambiguous);};
   live[42]=10000;recorder.ApplyOwnershipSnapshot(directions,owners,live,closed);send(Frame(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Bar(),Energy(601,100)),1000);
   Check(skills.Read(1000).Self==601&&vital.Read(false,1000).Current==10000,"Actual login and ownership proof decoded");
   clock+=1;recorder.ApplyOwnershipSnapshot(empty,none,live,closed);send(Frame(Hit(601,99,321)),1000.1);Check(floats.Drain(1000.1).Length==1,"Previously lost first damage event now arrives immediately during absent table row");
   for(int i=0;i<65;i++){clock+=.1;send(Frame(Energy(601,(uint)(200+i))),1000.2+i*.1);}
   send(Frame(ProgressVerification.Gauge(99,100,150),Hit(601,99,11),ProgressVerification.Gauge(99,0,150,true)),1007);
   Check(signal.Errors==0&&signal.Samples==66&&gauge.Read().BurstSerial>0&&floats.Drain(1007).Length==1&&skills.Read(1007).Self==601,"No gap, delayed burst or hotbar loss after 65 packets; actual stagger depletion remains fresh");
   recorder.ApplyOwnershipSnapshot(directions,owners,live,closed);send(Frame(Energy(601,999)),1008);Check(signal.Errors==0,"Restoring Windows snapshot neither resets nor duplicates stream");
   string previous=skills.Read(1008).Flow;var syn=S(new byte[0]);syn.Flags=2;state.Control(syn);seq=700;
   send(Frame(Energy(602,100),Energy(602,101),Energy(602,102)),1009);Check(floats.Drain(1009).Length==0,"Reused tuple without received own profile cannot adopt another character's damage");
   send(Frame(EnemyTargetVerification.Player(602,"OtherFixture",8000,9000,true),Bar(),Energy(602,200)),1010);Check(skills.Read(1010).Self==602&&skills.Read(1010).Flow!=previous&&vital.Read(false,1010).Current==8000,"New socket generation and character use fresh receiver identity");
  }
 }
 internal static void Replay(string root,string path){
  var normal=ReplayOne(root,path,false);var missing=ReplayOne(root,path,true);
  Check(normal.Frames>0&&normal.Frames==missing.Frames&&normal.Errors==missing.Errors&&normal.Damage==missing.Damage&&normal.Bars==missing.Bars,"Captured traffic must decode identically with absent ownership observations");
  Check(missing.Retained>0,"Captured comparison must actually exercise validated ownership retention");
  File.WriteAllText(Path.Combine(root,"capture-recovery-replay.json"),new JavaScriptSerializer().Serialize(new{baseline=normal,missingOwnership=missing,scope="Recorded incoming game packets, synthetic Windows poll absences after real decoded proof. Not a replay of the metadata-only recipient report."}));
  File.WriteAllText(Path.Combine(root,"capture-recovery-replay-test.txt"),"PASS: identical recorded decoded frames/errors/own damage/bar observations with simulated Windows ownership absences. Actual Recorder/router/decoder/receivers; no live capture or sends.");
 }
 sealed class ReplayResult {public long Frames,Damage,Bars,Retained,Packets;public int Errors;}
 static ReplayResult ReplayOne(string root,string path,bool simulate){
  var result=new ReplayResult();double clock=0,first=0;var live=new Dictionary<int,long>{{42,10000}};var rows=new Dictionary<string,string>();var owners=new Dictionary<string,int>();var proved=new HashSet<string>();var json=new JavaScriptSerializer();
  using(var recorder=new Recorder(root,false)){
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var continuity=(OwnedConnectionContinuity)typeof(Recorder).GetField("continuity",flags).GetValue(recorder);continuity.Clock=delegate{return clock;};typeof(Recorder).GetField("active",flags).SetValue(recorder,true);
   var decoder=new DashSignal(root);decoder.DiagnosticClock=delegate{return clock;};var router=new GameFlowRouter(root,decoder);router.OnVerified=delegate(string stream){recorder.ConfirmGameplay(stream);int at=stream.LastIndexOf("#capture-",StringComparison.Ordinal);proved.Add(at<0?stream:stream.Substring(0,at));};
   var floats=new FloatingTextSignal();floats.Configure(new FloatingTextOptions{Enabled=true},false);var skills=new SkillSlotsSignal();skills.Configure(true);
   decoder.OnFrame=delegate(byte[] b,int s,int e,double t,string stream){result.Frames++;router.ObserveFrame(b,s,e,stream);skills.Consume(b,s,e,t,stream);floats.Consume(b,s,e,t,stream);result.Damage+=floats.Drain(t).Length;var bar=skills.Read(t);if(bar!=null)result.Bars++;};
   foreach(string line in File.ReadLines(path)){
    var row=json.Deserialize<Dictionary<string,object>>(line);string key=(string)row["stream"];string direction=row.ContainsKey("direction")?(string)row["direction"]:"in";if(direction!="in"&&direction!="both")continue;
    int arrow=key.IndexOf('>'),left=key.LastIndexOf(':',arrow),right=key.LastIndexOf(':');int port=int.Parse(key.Substring(left+1,arrow-left-1));if(port!=13328)continue;
    string hex=(string)row["hex"];var bytes=new byte[hex.Length/2];for(int i=0;i<bytes.Length;i++)bytes[i]=Convert.ToByte(hex.Substring(i*2,2),16);
    double ts=Convert.ToDouble(row["ts"]);if(first==0)first=ts;clock=Math.Max(clock,ts-first);
    var segment=new Segment{Src=key.Substring(0,left),SrcPort=port,Dst=key.Substring(arrow+1,right-arrow-1),DstPort=int.Parse(key.Substring(right+1)),Seq=Convert.ToUInt32(row["seq"]),Flags=Convert.ToByte(row["flags"]),Data=bytes};
    rows[key]="in";owners[key]=42;var snapshot=new Dictionary<string,string>(rows);
    if(simulate&&clock%20>=7&&clock%20<14)foreach(string verified in proved)snapshot.Remove(verified);
    recorder.ApplyOwnershipSnapshot(snapshot,owners,live,new HashSet<string>());OwnedConnectionContinuity.Decision decision;
    Check(recorder.ResolveOwned(segment,out decision),"Captured known incoming stream must remain accepted");segment.ProtocolStream=decision.Stream;if(decision.Retained)result.Retained++;result.Packets++;router.Observe(segment,ts,decision.Ambiguous);
   }
   result.Errors=decoder.Errors;
  }return result;
 }
}
