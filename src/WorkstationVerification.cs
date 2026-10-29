using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;

internal static class WorkstationVerification {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static byte[] Fixture(string method,params object[] args){return (byte[])typeof(EnemyHealthVerification).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
 static void Feed(EnemyHealthSignal s,byte[] b,double t,string flow="A"){s.Consume(b,0,b.Length,t,flow);}
 internal static void Run(string root){
  var signal=new EnemyHealthSignal();signal.Configure(true);double t=DpsHistory.UtcNow();Feed(signal,EnemyTargetVerification.Player(111,"Own",14000,15000,true),t);
  Feed(signal,Fixture("Spawn",444u,3000u,5000u,2100752u),t);Feed(signal,Fixture("Select",444u),t);Check(signal.Read()!=null&&signal.Read().Npc==2100752,"Ordinary enemy before workstation");
  foreach(uint npc in new uint[]{2910003,2910004,2910005,2910006,2910007,2910010,2910011,2910012,2910013,2910014,2910025}){
   uint actor=npc+1000;var bytes=Fixture("Spawn",actor,26600u,26600u,npc);EnemyHealthSignal.Sample decoded;Check(EnemyHealthSignal.Spawn(bytes,0,bytes.Length,out decoded)&&decoded.Npc==npc,"Workstation profile still parses and retains native identity");Feed(signal,bytes,t);Feed(signal,Fixture("Select",actor),t);
   Check(signal.Read()==null,"Workstation selection cannot show HP/name/level or fall back to old enemy");Check(signal.Actor("A",actor)==null,"Target-of-target lookup also excludes workstation");
  }
  foreach(uint npc in new uint[]{2930060,2930061,2930093,2910008,2910015,2090619,2100752}){
   uint actor=npc+2000;Feed(signal,Fixture("Spawn",actor,12000u,12000u,npc),t);Feed(signal,Fixture("Select",actor),t);Check(signal.Read()!=null&&signal.Read().Npc==npc,"Merchants, unrelated2910 templates and unverified2090 names remain supported");
  }
  Feed(signal,EnemyTargetVerification.Player(555,"NeutralPlayer",20000,20000,false),t);Feed(signal,Fixture("Select",555u),t);Check(signal.Read()!=null&&signal.Read().IsPlayer,"Selected player not affected by workstation catalogue");
  var target=new EnemyTargetSignal();target.Configure(true);Feed(signal,Fixture("Select",444u),t);var focus=Fixture("Focus",444u,2910003u+1000);target.Consume(focus,0,focus.Length,t,"A");Check(target.Read(signal.Read(),signal)==null,"Real target-of-target resolution cannot reintroduce a station");
  File.WriteAllText(Path.Combine(root,"workstation-test.txt"),"PASS: explicit workstation ID suppression on selected HP/name/level and target-of-target lookup; native spawn parsing retained; no stale enemy fallback; neutral merchant/player, same-family artifact/Dredgion and unconfirmed name matches preserved. No language or broad-range heuristic.");
 }
 static byte[] Hex(string value){var b=new byte[value.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(value.Substring(i*2,2),16);return b;}
 internal static void Replay(string root,string path){var s=new EnemyHealthSignal();s.Configure(true);var json=new JavaScriptSerializer{MaxJsonLength=2000000};var actors=new Dictionary<uint,uint>();var stations=new HashSet<uint>();int stationSelections=0,merchantSelections=0,frames=0;
  foreach(string line in File.ReadLines(path)){var row=json.Deserialize<Dictionary<string,object>>(line);var b=Hex((string)row["hex"]);double t=Convert.ToDouble(row["ts"]);string flow=(string)row["stream"];Feed(s,b,t,flow);frames++;int op=b[0]*256+b[1];EnemyHealthSignal.Sample spawn;if(op==0x4136&&EnemyHealthSignal.Spawn(b,0,b.Length,out spawn))actors[spawn.Actor]=spawn.Npc;
   uint actor,npc;if(op==0x3438&&EnemyHealthSignal.Selection(b,0,b.Length,out actor)&&actors.TryGetValue(actor,out npc)){
    if(WorkstationTargets.Known(npc)){stations.Add(npc);stationSelections++;Check(s.Read()==null&&s.Actor(flow,actor)==null,"Captured station selection must be suppressed");}
    else if(npc==2930060||npc==2930061||npc==2930093){merchantSelections++;Check(s.Read()!=null&&s.Read().Npc==npc,"Captured neutral merchant selection must retain original bar data");}
   }
  }
  Check(stations.Contains(2910003)&&stations.Contains(2910005)&&stations.Contains(2910006)&&stations.Contains(2910007)&&merchantSelections>0,"Real replay covers four reproduced professions and merchant controls");
  File.WriteAllText(Path.Combine(root,"workstation-replay-details.json"),json.Serialize(new{frames,stationSelections,merchantSelections,stations}));File.WriteAllText(Path.Combine(root,"workstation-replay-test.txt"),"PASS: actual captured smithy, handicrafting, alchemy and cooking selections no longer expose target bars; captured neutral merchants retain received HP data. Production visibility was confirmed separately in original samples.");
 }
}
