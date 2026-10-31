using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
  using(var recorder=new Recorder(root,false)){var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(Recorder).GetField("flows",flags).SetValue(recorder,forward);typeof(Recorder).GetField("active",flags).SetValue(recorder,true);recorder.OnSegment=delegate{};Check(recorder.IncomingSocketKeys().Length==2&&recorder.Incoming("127.0.0.1:58449>127.0.0.1:58450"),"Both local destinations remain owned, verification-required candidates");typeof(Recorder).GetField("liveStandardWorkers",flags).SetValue(recorder,1);typeof(Recorder).GetField("liveWorkers",flags).SetValue(recorder,2);typeof(Recorder).GetMethod("WorkerEnded",flags).Invoke(recorder,new object[]{false});Check(!recorder.Active,"An idle alternate worker cannot conceal failure of all primary capture workers");}
  File.WriteAllText(Path.Combine(root,"capture-recovery-test.txt"),"PASS: actual dual-owned direction/owner merging, 400 reversed row enumerations, native-like filter flush callbacks on production lane class, 200 auxiliary updates preserve queued fragmented login, actual Hotbar/HP/Floating/Stagger receivers, received character handoff, ambiguous standard-port quarantine and ordered verification/deduplication, explicit outgoing mode, retained research policy. No live capture or packet sends; no affected-PC performance claim.");
 }
}
