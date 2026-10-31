using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

internal static class TransportDiagnosticsVerification {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static Dictionary<string,object> Json(object value){var j=new JavaScriptSerializer();return j.Deserialize<Dictionary<string,object>>(j.Serialize(value));}
 static Dictionary<string,object> Flow(DashSignal s){return (Dictionary<string,object>)((System.Collections.IList)Json(s.SupportState())["flows"])[0];}
 static byte[] Frames(uint amount){var bytes=new List<byte>();for(int n=0;n<3;n++){bytes.AddRange(new byte[]{14,0,0x8d,1,1,1,3});bytes.AddRange(BitConverter.GetBytes(amount));}return bytes.ToArray();}
 static Segment Segment(uint seq,byte[] data){return new Segment{Src="192.0.2.1",Dst="192.0.2.2",SrcPort=13328,DstPort=51000,Seq=seq,Data=data};}
 internal static void Run(string root){
  DashSignal.VerifyProtocol(root); // Existing wrapped/retransmitted/compressed cases.
  string folder=Path.Combine(root,"transport-diagnostic-fixture");SupportDiagnostics.Start(folder);
  try{
   double clock=100;var s=new DashSignal(root);s.DiagnosticClock=delegate{return clock;};byte[] initial=Frames(100),later=Frames(200);s.Consume(Segment(100,initial),1000);uint next=(uint)(100+initial.Length);s.Consume(Segment(next+7,later),1001);
   Check(s.Samples==3,"Missing bytes must not synthesize or prematurely release readings");clock=135;
   var flow=Flow(s);Check(Convert.ToInt32(flow["pendingSegments"])==1&&Convert.ToDouble(flow["gapAgeSeconds"])==35&&Convert.ToInt32(flow["gapBytes"])==7,"Diagnostic age/distance expose even a below-limit gap");
   Check(Convert.ToInt32(flow["id"])==SupportDiagnostics.FlowId(Segment(0,new byte[0]).Key),"Opaque transport IDs correlate without writing socket endpoints");
   Check(s.Samples==3&&s.Errors==0,"Reading diagnostics cannot change retransmission policy");
   var missing=new byte[7];s.Consume(Segment(next,missing),1002);flow=Flow(s);
   Check(s.Samples==6&&s.Current.Value==200&&s.Errors==0&&Convert.ToInt32(flow["pendingSegments"])==0&&flow["gapAgeSeconds"]==null&&Convert.ToDouble(flow["maxGapAgeSeconds"])==35,"Late missing bytes drain exactly once and retain measured gap history");
   s.Consume(Segment(next,missing),1003);Check(s.Samples==6&&Convert.ToInt64(Flow(s)["duplicateSegments"])==1,"Duplicate metadata cannot duplicate gameplay");
   clock=140;uint end=next+7+(uint)later.Length;
   for(int n=0;n<65;n++)s.Consume(Segment(unchecked(end+9+(uint)(n*later.Length)),later),1004+n);
   var state=Json(s.SupportState());flow=Flow(s);Check(Convert.ToInt64(state["gapRecoveries"])==1&&Convert.ToInt64(flow["skippedBytes"])==9&&Convert.ToInt32(flow["pendingSegments"])==0,"Existing count-limit recovery is independently counted");
   Check(s.Samples==201&&s.Errors==1,"Count-limit recovery preserves normal decoded readings and original error count");
   var adapter=SupportDiagnostics.Adapter("fixture");adapter.Processed(Stopwatch.Frequency/125);adapter.Delivery(DpsHistory.UtcNow()-.25);var snap=Json(adapter.Snapshot());Check(Convert.ToInt64(snap["handlerCalls"])==1&&Convert.ToDouble(snap["maximumHandlerMilliseconds"])>=7.9&&Convert.ToDouble(snap["captureClockAgeSeconds"])>=.24,"Handler cost and capture-clock age recorded separately");
   adapter.Lane="standard";adapter.FilterApplied();snap=Json(adapter.Snapshot());Check(Convert.ToInt64(snap["nativeFilterEpoch"])==1&&Convert.ToInt64(snap["filterApplications"])==1&&Convert.ToString(snap["captureLane"])=="standard","Native filter applications carry lane and lifetime reset epoch");adapter.ResetDiagnostics();snap=Json(adapter.Snapshot());Check(Convert.ToInt64(snap["nativeFilterEpoch"])==1&&Convert.ToInt64(snap["filterApplications"])==0,"A new diagnostic counter baseline cannot conceal the native reset epoch");adapter.FilterApplied();Check(Convert.ToInt64(Json(adapter.Snapshot())["nativeFilterEpoch"])==2,"A real filter replacement starts a distinct native statistics epoch");
   Check(Marshal.SizeOf(typeof(Native.CaptureStatistics))>=24,"Native statistics buffer covers Windows compatibility fields");
   SupportDiagnostics.Decoder(s.SupportState());SupportDiagnostics.Flush(false);SupportDiagnostics.Stop();var summary=Json(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(folder,"support-summary.json"))));
   Check(Convert.ToInt32(summary["diagnosticsRevision"])==3&&summary["runtime"]!=null,"Report includes schema revision and Helper resource counters");
   foreach(string name in new[]{"support-log.jsonl","support-summary.json"}){string text=File.ReadAllText(Path.Combine(folder,name));Check(!text.Contains("192.0.2.1")&&!text.Contains("192.0.2.2")&&!text.Contains("fixture"),"Transport metadata cannot disclose endpoints or payload contents");}
  }finally{SupportDiagnostics.Stop();}
  File.WriteAllText(Path.Combine(root,"transport-diagnostics-test.txt"),"PASS: original protocol regressions; below-limit gap age/distance and eventual retransmission; opaque flow correlation; duplicate/count-limit behavior unchanged; error reason counts; capture-clock age vs handler cost; bounded metadata-only report, runtime counters, no socket endpoints or packet payloads. No live capture or packet sends.");
 }
}
