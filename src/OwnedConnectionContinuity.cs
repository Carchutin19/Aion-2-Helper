using System;
using System.Collections.Generic;
using System.Linq;

// A TCP-table poll is an observation, not a connection-close notification.
// Only an exact Windows-owned stream with a decoded own profile AND matching
// energy may bridge an absent observation. Never extend the absence deadline
// from packet arrivals, and never inherit proof across process/socket generations.
internal sealed class OwnedConnectionContinuity {
 internal sealed class Decision {internal bool Incoming,Ambiguous,Retained;internal int Owner;internal string Stream,PeerStream;}
 sealed class Entry {
  internal string Key,Direction,Stream;internal int Owner;internal long ProcessStart;
  internal double Seen,Payload;internal bool Present,Verified,Closed,AbsentAfterClose,Retaining;
  internal long Accepted,Retained,Rejected;internal uint Next,Syn;internal bool SequenceKnown,HasSyn;
 }
 const int Limit=128;internal const double MaximumAbsence=120,MaximumIdle=30;
 readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
 readonly Dictionary<string,long> rejected=new Dictionary<string,long>();
 Dictionary<int,long> processes=new Dictionary<int,long>();long generation,rejectedOverflow;
 internal Func<double> Clock=delegate{return System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;};
 static string Reverse(string key){int at=key.IndexOf('>');return at<0?"":key.Substring(at+1)+">"+key.Substring(0,at);}
 static bool Incoming(string direction){return direction=="in"||direction=="both";}
 bool Alive(Entry e){long start;return e.ProcessStart!=0&&processes.TryGetValue(e.Owner,out start)&&start==e.ProcessStart;}
 bool Retain(Entry e,double now){return !e.Closed&&e.Verified&&Alive(e)&&now-e.Seen<=MaximumAbsence&&now-e.Payload<=MaximumIdle;}
 Entry New(string key,string direction,int owner,long start,double now){return new Entry{Key=key,Direction=direction,Owner=owner,ProcessStart=start,Seen=now,Payload=now,Present=true,Stream=key+"#capture-"+(++generation)};}
 void Event(string kind,Entry e,string reason){SupportDiagnostics.Event(kind,new{flow=SupportDiagnostics.FlowId(e.Stream),reason,verified=e.Verified});}
 internal void Update(Dictionary<string,string> directions,Dictionary<string,int> owners,Dictionary<int,long> live,HashSet<string> closed){
  double now=Clock();processes=new Dictionary<int,long>(live);
  foreach(var e in entries.Values){e.Present=false;if(closed.Contains(e.Key)&&!e.Closed){e.Closed=true;Event("owned-stream-ended",e,"Windows closed connection");}}
  foreach(var row in directions){int owner;if(!owners.TryGetValue(row.Key,out owner))continue;long start;live.TryGetValue(owner,out start);Entry e;
   if(!entries.TryGetValue(row.Key,out e)||e.Owner!=owner||e.ProcessStart!=start||Incoming(e.Direction)!=Incoming(row.Value)||e.Closed&&e.AbsentAfterClose){
    if(e!=null)Event("owned-stream-ended",e,"process or connection generation changed");
    if(e==null&&entries.Count>=Limit){var remove=entries.Values.Where(x=>!x.Present&&!Retain(x,now)).OrderBy(x=>x.Seen).FirstOrDefault();if(remove==null)continue;entries.Remove(remove.Key);}
    entries[row.Key]=e=New(row.Key,row.Value,owner,start,now);
   }
   if(row.Value=="both")e.Direction="both"; // Keep the stronger proof requirement if a peer row later disappears.
   e.Present=true;e.Seen=now;if(e.Retaining){e.Retaining=false;Event("owned-stream-restored",e,"Windows ownership visible again");}
  }
  foreach(var e in entries.Values){
   if(!e.Present&&e.Closed)e.AbsentAfterClose=true;
   if(!e.Present&&!e.Closed&&!Alive(e)){e.Closed=true;Event("owned-stream-ended",e,"owner process ended or changed");}
   if(!e.Present&&e.Verified&&!e.Closed&&!Retain(e,now)){e.Closed=true;e.AbsentAfterClose=true;e.Retaining=false;Event("owned-stream-ended",e,"bounded continuity expired");}
  }
 }
 internal void Confirm(string stream){foreach(var e in entries.Values)if(e.Stream==stream&&!e.Closed&&!e.Verified){e.Verified=true;Event("owned-stream-verified",e,"decoded own profile and matching energy");return;}}
 internal void Control(Segment segment){
  Entry e;if(!entries.TryGetValue(segment.Key,out e))return;
  if((segment.Flags&2)!=0){ // Duplicate adapter delivery/retransmission is the same SYN.
   if(e.HasSyn&&e.Syn==segment.Seq)return;
   entries[e.Key]=New(e.Key,e.Direction,e.Owner,e.ProcessStart,Clock());entries[e.Key].Present=e.Present;entries[e.Key].HasSyn=true;entries[e.Key].Syn=segment.Seq;
   Event("owned-stream-ended",e,"captured SYN; new connection generation");
  }else if((segment.Flags&5)!=0&&!e.Closed){e.Closed=true;e.Verified=false;Event("owned-stream-ended",e,(segment.Flags&4)!=0?"captured RST":"captured FIN");}
 }
 internal bool Resolve(Segment segment,bool client,out Decision result){
  result=null;Entry e;double now=Clock();
  if(!entries.TryGetValue(segment.Key,out e)){Reject(segment.Key,"not observed owned");return false;}
  if(!e.Present&&e.Verified&&!e.Closed&&!Retain(e,now)){e.Closed=true;e.AbsentAfterClose=true;Event("owned-stream-ended",e,"bounded continuity expired");}
  if(e.Closed||!e.Present&&!Retain(e,now)){e.Rejected++;Reject(e.Stream,e.Closed?"connection ended":"ownership absent or continuity expired");return false;}
  bool incoming=e.Direction=="in"||e.Direction=="both";
  if(!incoming&&!client){Reject(e.Stream,"outgoing disabled");return false;}
  bool retained=!e.Present;if(retained&&!incoming){Reject(e.Stream,"outgoing ownership absent");return false;}
  int distance=unchecked((int)(segment.Seq-e.Next));
  if(e.SequenceKnown&&(distance>16777216||distance< -16777216)){
   if(retained){Reject(e.Stream,"sequence outside retained connection window");return false;}
   Event("owned-stream-ended",e,"new sequence generation on currently owned tuple");entries[e.Key]=e=New(e.Key,e.Direction,e.Owner,e.ProcessStart,now);
  }
  if(retained&&!e.Retaining){e.Retaining=true;Event("owned-stream-retained",e,"verified incoming stream; Windows row temporarily absent");}
  if(segment.Data.Length>0){uint next=unchecked(segment.Seq+(uint)segment.Data.Length+((segment.Flags&2)!=0?1u:0u));if(!e.SequenceKnown||unchecked((int)(next-e.Next))>0)e.Next=next;e.SequenceKnown=true;}
  e.Payload=now;e.Accepted++;if(retained)e.Retained++;
  Entry peer;entries.TryGetValue(Reverse(segment.Key),out peer);
  result=new Decision{Incoming=incoming,Ambiguous=e.Direction=="both",Retained=retained,Owner=e.Owner,Stream=e.Stream,PeerStream=peer==null?null:peer.Stream};return true;
 }
 void Reject(string key,string reason){if(!SupportDiagnostics.Enabled)return;string id=SupportDiagnostics.FlowId(key)+":"+reason;long value;if(rejected.TryGetValue(id,out value))rejected[id]=value+1;else if(rejected.Count<64)rejected[id]=1;else rejectedOverflow++;}
 internal Dictionary<string,string> EffectiveDirections(){double now=Clock();return entries.Values.Where(e=>!e.Closed&&(e.Present||Retain(e,now))&&(e.Direction=="in"||e.Direction=="both")).ToDictionary(e=>e.Key,e=>e.Direction);}
 internal string[] Streams(){double now=Clock();return entries.Values.Where(e=>!e.Closed&&(e.Present||Retain(e,now))&&(e.Direction=="in"||e.Direction=="both")).Select(e=>e.Stream).ToArray();}
 internal object Status(){double now=Clock();return new{counterScope="Recorder lifetime; compare snapshot deltas. Rejections count resolution attempts.",maximumAbsentSeconds=MaximumAbsence,maximumIdleSeconds=MaximumIdle,entries=entries.Values.Where(e=>e.Verified||e.Accepted>0).Select(e=>new{flow=SupportDiagnostics.FlowId(e.Stream),present=e.Present,verified=e.Verified,closed=e.Closed,absentSeconds=e.Present?0:Math.Max(0,now-e.Seen),accepted=e.Accepted,retained=e.Retained,rejected=e.Rejected}).ToArray(),rejections=rejected.Select(x=>new{flowAndReason=x.Key,count=x.Value}).ToArray(),rejectedOverflow};}
 internal void Reset(){entries.Clear();processes.Clear();rejected.Clear();rejectedOverflow=0;/* namespace counter never reused during this Recorder lifetime */}
}
