using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

internal static class GameFlowVerification {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Var(List<byte> b,uint value){do{byte x=(byte)(value&127);value>>=7;if(value>0)x|=128;b.Add(x);}while(value>0);}
    static byte[] Energy(uint actor,uint value){var b=new List<byte>(new byte[]{0,0x8d});Var(b,actor);b.AddRange(new byte[]{1,1,3});b.AddRange(BitConverter.GetBytes(value));return b.ToArray();}
    static byte[] Frames(params byte[][] bodies){var b=new List<byte>();foreach(var body in bodies){Var(b,(uint)body.Length+4);b.AddRange(body);}return b.ToArray();}
    static Segment S(int port,byte[] data,uint seq=100,int local=50000){return new Segment{Src="192.0.2.1",Dst="192.0.2.2",SrcPort=port,DstPort=local,Seq=seq,Data=data};}
    static Dictionary<string,object> Status(GameFlowRouter r){var j=new JavaScriptSerializer();return j.Deserialize<Dictionary<string,object>>(j.Serialize(r.Status));}
    static long Count(GameFlowRouter r,string name){return Convert.ToInt64(Status(r)[name]);}
    static DashSignal Sink(string root,List<int> ops){var result=new DashSignal(root);result.OnFrame=delegate(byte[] b,int s,int e,double t,string f){ops.Add((b[s]<<8)|b[s+1]);};return result;}
    internal static void Run(string root){
        foreach(int port in new[]{54304,62819,55949}){
            var ops=new List<int>();var sink=Sink(root,ops);var router=new GameFlowRouter(root,sink);
            byte[] data=Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Energy(601,100),Energy(601,200));
            var input=S(port,data);router.Observe(input,1000);
            Check(sink.Samples==2&&sink.Current.Actor==601&&ops.SequenceEqual(new[]{0x3336,0x008d,0x008d}),"Verified alternate stream replays initial own profile once and in order on any observed port");
            router.Observe(input,1000);Check(sink.Samples==2&&ops.Count==3,"Duplicate delivery across adapters does not double-dispatch frames");
            byte[] more=Frames(Energy(601,300));router.Observe(S(port,more,(uint)(100+data.Length)),1001);
            Check(sink.Samples==3&&sink.Current.Value==300&&Count(router,"bufferedBytes")==0,"Verified stream continues normally without discovery buffers");
        }
        {
            // Exercise real downstream receivers, including a hotbar received
            // before energy confirms which alternate socket carries gameplay.
            var vital=new PlayerVitalSignal();vital.Configure(true);
            var skills=new SkillSlotsSignal();skills.Configure(true);
            var sink=new DashSignal(root);sink.OnFrame=delegate(byte[] b,int s,int e,double t,string f){vital.Consume(b,s,e,t,f);skills.Consume(b,s,e,t,f);};
            sink.OnReset=delegate{vital.Reset();skills.Reset();};
            var r=new GameFlowRouter(root,sink);
            var bar=(byte[])typeof(SkillSlotsVerification).GetMethod("Bar",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            byte[] login=Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),bar,Energy(601,100));r.Observe(S(54304,login),1000);
            var hp=vital.Read(false,1000);var mp=vital.Read(true,1000);var view=skills.Read(1000);
            Check(hp!=null&&hp.Actor==601&&hp.Current==10000&&hp.Maximum==12000&&mp!=null&&mp.Current==4591,"Alternate-port adoption initializes actual HP/MP receivers from retained own profile");
            Check(view!=null&&view.Self==601&&view.Slots.Count==2,"Initial assigned hotbar reaches actual receiver before its first redraw");
            r.Reset();Check(vital.Read(false,1001)==null&&skills.Read(1001)==null,"Routing reset reaches actual vital and hotbar receivers");
        }
        {
            var ops=new List<int>();var sink=Sink(root,ops);var r=new GameFlowRouter(root,sink);
            byte[] a=Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Energy(602,100),Energy(602,200));r.Observe(S(54304,a),1000);
            Check(ops.Count==0,"Another actor's energy does not release own profile or queued frames");
            byte[] b=Frames(Energy(601,300));r.Observe(S(54304,b,(uint)(100+a.Length)),1001);
            Check(ops.Count==4&&ops[0]==0x3336&&sink.Current.Actor==601,"Delayed matching energy releases complete initial order");
            r.Reset();Check(sink.Current==null&&Count(r,"bufferedBytes")==0,"Transport reset clears routing and existing overlay identity");
            r.Observe(S(54304,Frames(Energy(601,100),Energy(601,200),Energy(601,300))),1002);Check(ops.Count==4,"Reset cannot reuse old own-profile verification");
        }
        {
            var ops=new List<int>();var sink=Sink(root,ops);var r=new GameFlowRouter(root,sink);
            r.Observe(S(55949,Frames(EnemyTargetVerification.Player(602,"Other",10000,12000,false),Energy(602,100),Energy(602,200))),1000);
            r.Observe(S(443,Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Energy(601,100),Energy(601,200))),1000);
            Check(ops.Count==0,"Foreign-player identity and web socket stay quarantined");
            r.Observe(S(55950,Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),EnemyTargetVerification.Player(602,"OtherSelf",10000,12000,true),Energy(602,100))),1000);
            Check(ops.Count==0,"Conflicting own-profile identity is not adopted");
        }
        {
            var ops=new List<int>();var sink=Sink(root,ops);var r=new GameFlowRouter(root,sink);
            byte[] data=Frames(EnemyTargetVerification.Player(601,"Fixture",10000,12000,true),Energy(601,100),Energy(601,200));
            r.Observe(S(54304,data.Take(2).ToArray()),1000);
            r.Observe(S(54304,data.Skip(9).ToArray(),109),1001);Check(ops.Count==0,"Out-of-order incomplete protocol is not dispatched");
            r.Observe(S(54304,data.Skip(2).Take(7).ToArray(),102),1002);Check(ops.Count==3&&sink.Samples==2,"Out-of-order buffered login is reassembled before adoption");
            r.Retain(delegate{return new string[0];});
            r.Observe(S(54304,Frames(Energy(601,100),Energy(601,200),Energy(601,300)),9999),1003);
            Check(ops.Count==3,"Closed Windows socket verification is not inherited by a reused tuple");
        }
        {
            var ops=new List<int>();var sink=Sink(root,ops);var r=new GameFlowRouter(root,sink);
            r.Observe(S(13328,Frames(Energy(601,100),Energy(601,200),Energy(601,300))),1000);
            Check(sink.Samples==3&&Count(r,"directSegments")==1,"Ordinary 13328 path remains immediate and unchanged");
        }
        {
            var ops=new List<int>();var sink=Sink(root,ops);var r=new GameFlowRouter(root,sink);
            for(int i=0;i<10;i++)r.Observe(S(55950,new byte[65535],(uint)(100+i*65535)),1000+i);
            Check(ops.Count==0&&Count(r,"bufferedBytes")==0,"Unknown stream hits byte limit and releases pending buffers");
            for(int i=0;i<25;i++)r.Observe(S(56000+i,new byte[1024],100,51000+i),1020+i);
            Check(Count(r,"omitted")>0&&Count(r,"bufferedBytes")<=4194304,"Candidate and global memory budgets remain bounded");
        }
        File.WriteAllText(Path.Combine(root,"game-flow-test.txt"),"PASS: functional alternate-port adoption only after coherent own profile and energy; complete initial-frame replay; no duplicate frames across adapter deliveries; continuing energy; three alternate ports; unchanged standard 13328; foreign/web/ambiguous rejection; delayed identity; out-of-order TCP; reset/closed socket isolation; bounded candidates and retained bytes. No live capture or packet sends.");
    }
}
