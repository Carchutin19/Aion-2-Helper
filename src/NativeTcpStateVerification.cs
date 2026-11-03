using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

internal static class NativeTcpStateVerification {
 static void Check(bool good,string message){if(!good)throw new Exception(message);}
 static byte[] Fixture(string name,params object[] args){return (byte[])typeof(CaptureRecoveryVerification).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
 static byte[] Frame(params byte[][] messages){return Fixture("Frame",new object[]{messages});}
 internal static void Run(string root){
  foreach(int state in new[]{0,1,2})Check(Recorder.InconclusiveGameState(state,"192.0.2.2","192.0.2.1",51000,13328),"Observed game-port state admitted only as inconclusive");
  foreach(int state in new[]{3,5,6,10,11,12,100})Check(!Recorder.InconclusiveGameState(state,"192.0.2.2","192.0.2.1",51000,13328),"Other native states retain existing policy");
  Check(!Recorder.InconclusiveGameState(1,"192.0.2.2","192.0.2.1",51000,443)&&!Recorder.InconclusiveGameState(2,"0.0.0.0","192.0.2.1",51000,13328)&&!Recorder.InconclusiveGameState(0,"192.0.2.2","::",51000,13328)&&!Recorder.InconclusiveGameState(1,"192.0.2.2","192.0.2.1",0,13328),"No web, wildcard or listener-only fallback");
  double clock=100;uint seq=100;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  using(var recorder=new Recorder(root,false)){
   typeof(Recorder).GetField("active",flags).SetValue(recorder,true);var ownership=(OwnedConnectionContinuity)typeof(Recorder).GetField("continuity",flags).GetValue(recorder);ownership.Clock=delegate{return clock;};
   var decoder=new DashSignal(root);var route=new GameFlowRouter(root,decoder);route.OnVerified=recorder.ConfirmGameplay;
   var skill=new SkillSlotsSignal();skill.Configure(true);var hp=new PlayerVitalSignal();hp.Configure(true);var text=new FloatingTextSignal();text.Configure(new FloatingTextOptions{Enabled=true},false);var stagger=new StaggerSignal();stagger.Configure(true);
   decoder.OnFrame=delegate(byte[] b,int s,int e,double t,string flow){route.ObserveFrame(b,s,e,flow);skill.Consume(b,s,e,t,flow);hp.Consume(b,s,e,t,flow);text.Consume(b,s,e,t,flow);stagger.Consume(b,s,e,t,flow);};
   var template=new Segment{Src="192.0.2.1",Dst="192.0.2.2",SrcPort=13328,DstPort=51000,Data=new byte[]{1}};
   var directions=new Dictionary<string,string>{{template.Key,"in"}};var owners=new Dictionary<string,int>{{template.Key,42}};var live=new Dictionary<int,long>{{42,10000}};var closed=new HashSet<string>();var uncertain=new HashSet<string>{template.Key};
   string first=null,last=null;bool proofRequired=false;
   Action<byte[]> send=delegate(byte[] bytes){var packet=new Segment{Src=template.Src,Dst=template.Dst,SrcPort=template.SrcPort,DstPort=template.DstPort,Seq=seq,Data=bytes};seq+=(uint)bytes.Length;OwnedConnectionContinuity.Decision decision;Check(recorder.ResolveOwned(packet,out decision),"Exact currently PID-owned row must stay resolvable");packet.ProtocolStream=decision.Stream;last=decision.Stream;if(first==null)first=last;proofRequired=decision.Ambiguous||decision.UncertainState;route.Observe(packet,clock,proofRequired);};
   recorder.ApplyOwnershipSnapshot(directions,owners,live,closed,uncertain);
   send(Frame(Fixture("Energy",601u,100u),Fixture("Energy",601u,101u),Fixture("Energy",601u,102u)));
   Check(proofRequired&&decoder.Current==null&&skill.Read(clock)==null,"An inconclusive row cannot dispatch energy or adopt a bar without own-profile proof");
   send(Frame(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Fixture("Bar"),Fixture("Energy",601u,103u)));
   Check(skill.Read(clock)!=null&&hp.Read(false,clock).Current==10000,"Quarantined login releases its actual profile and full bar after matching own energy");
   for(int n=0;n<160;n++){
    clock+=1;bool inconclusive=n%4!=3;
    recorder.ApplyOwnershipSnapshot(directions,owners,live,closed,inconclusive?uncertain:null);
    send(Frame(Fixture("Energy",601u,(uint)(200+n))));
    Check(first==last&&skill.Read(clock)!=null,"State 5/0/1/2 changes must preserve namespace, proof and hotbar over two minutes");
   }
   clock+=.1;send(Frame(ProgressVerification.Gauge(99,100,150),Fixture("Hit",601u,99u,321u),ProgressVerification.Gauge(99,0,150,true)));
   int hits=text.Drain(clock).Length;var gauge=stagger.Read();Check(hits==1&&gauge!=null&&gauge.BurstSerial>0&&decoder.Errors==0,"Floating damage and stagger keep updating without a framing gap: hits="+hits+", gauge="+(gauge==null?"null":gauge.BurstSerial.ToString())+", errors="+decoder.Errors);
   recorder.ApplyOwnershipSnapshot(directions,owners,live,closed,uncertain);
   var before=last;var rst=new Segment{Src=template.Src,Dst=template.Dst,SrcPort=template.SrcPort,DstPort=template.DstPort,Flags=4,Seq=seq,Data=new byte[0]};ownership.Control(rst);OwnedConnectionContinuity.Decision rejected;
   Check(!ownership.Resolve(rst,false,out rejected),"Wire RST still revokes uncertain-state proof immediately");
   rst.Flags=2;rst.Seq=seq;ownership.Control(rst);Check(ownership.Resolve(rst,false,out rejected)&&rejected.Stream!=before&&rejected.UncertainState,"New SYN has a distinct unverified namespace and retains its uncertainty flag");
   recorder.ApplyOwnershipSnapshot(new Dictionary<string,string>(),new Dictionary<string,int>(),live,closed);
   Check(!ownership.Resolve(rst,false,out rejected),"New SYN cannot inherit proof during an absent Windows row");
  }
  var missingGeneration=new OwnedConnectionContinuity();var key="192.0.2.1:13328>192.0.2.2:51000";missingGeneration.Update(new Dictionary<string,string>{{key,"in"}},new Dictionary<string,int>{{key,42}},new Dictionary<int,long>{{42,0}},new HashSet<string>(),new HashSet<string>{key});
  OwnedConnectionContinuity.Decision missing;Check(!missingGeneration.Resolve(new Segment{Src="192.0.2.1",Dst="192.0.2.2",SrcPort=13328,DstPort=51000,Data=new byte[]{1}},false,out missing),"Unreadable process lifetime cannot admit an inconclusive tuple");
  File.WriteAllText(Path.Combine(root,"native-tcp-state-test.txt"),"PASS: inconclusive PID-owned game states require protocol proof; no wildcard/web fallback; cold-login quarantine; 160 continuous state flips preserve profile/hotbar, damage and stagger; wire reset/SYN and absent-row proof guards; unreadable process lifetime rejected.");
 }
}
