using System;
using System.Collections.Generic;
using System.Linq;

// Verified alternate-port routing. Recorder has already checked exact incoming Windows
// socket ownership. Alternate sockets stay quarantined until an explicit own
// profile and matching energy arrive in the same reassembled protocol stream.
internal sealed class GameFlowRouter {
    sealed class Saved {internal Segment Segment;internal double Time;}
    sealed class Candidate {
        internal int Id,Port,Bytes,Segments,Errors;internal long Frames;
        internal uint Actor;internal int Matches;internal bool Conflict,Verified,Rejected;
        internal DashSignal Probe;internal readonly List<Saved> Pending=new List<Saved>();
        internal string State="waiting for own profile and energy";
    }
    readonly object gate=new object();readonly string root;readonly DashSignal target;
    readonly Dictionary<string,Candidate> candidates=new Dictionary<string,Candidate>();
    int nextId,omitted,buffered;long directSegments,forwardedSegments;
    const int MaxCandidates=16,MaxCandidateBytes=524288,MaxTotalBytes=4194304,MaxSegments=4096;
    internal GameFlowRouter(string root,DashSignal target){this.root=root;this.target=target;target.AllowOwnedPorts=true;}
    internal void Reset(){lock(gate){candidates.Clear();buffered=0;omitted=0;target.Reset();}}
    // Called on existing maintenance, never a second polling loop. A socket
    // absent from Windows must earn verification again if its tuple is reused.
    internal void Retain(Func<IEnumerable<string>> owned){lock(gate){var keys=new HashSet<string>(owned());foreach(string key in candidates.Keys.Where(k=>!keys.Contains(k)).ToArray()){buffered-=candidates[key].Bytes;candidates.Remove(key);}}}
    void Reject(Candidate c,string reason){buffered-=c.Bytes;c.Bytes=0;c.Pending.Clear();c.Probe=null;c.Rejected=true;c.State=reason;}
    internal void Observe(Segment segment,double timestamp){
        if(segment==null||segment.Data==null||segment.Data.Length==0)return;
        lock(gate){
            if(segment.SrcPort==13328){directSegments++;target.Consume(segment,timestamp);return;}
            if(segment.SrcPort==443)return; // Web sockets never become gameplay.
            Candidate c;
            if(candidates.TryGetValue(segment.Key,out c)&&(segment.Flags&2)!=0){buffered-=c.Bytes;candidates.Remove(segment.Key);c=null;}
            if(c==null){
                if(candidates.Count>=MaxCandidates){omitted++;return;}
                c=new Candidate{Id=++nextId,Port=segment.SrcPort};candidates[segment.Key]=c;
                c.Probe=new DashSignal(root);c.Probe.ProbeOnly=true;Candidate captured=c;
                c.Probe.OnFrame=delegate(byte[] b,int s,int e,double t,string flow){
                    captured.Frames++;
                    if(b[s]==0x33&&b[s+1]==0x36){uint actor;string name;byte role;
                        if(DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role)){
                            if(captured.Actor!=0&&captured.Actor!=actor)captured.Conflict=true;
                            captured.Actor=actor;
                        }
                    }
                    uint resourceActor;
                    if(captured.Actor!=0&&DashSignal.EnergyActor(b,s,e,out resourceActor)&&resourceActor==captured.Actor)captured.Matches++;
                };
            }
            c.Segments++;
            if(c.Rejected)return;
            if(c.Verified){forwardedSegments++;target.Consume(segment,timestamp);return;}
            if(c.Bytes+segment.Data.Length>MaxCandidateBytes||buffered+segment.Data.Length>MaxTotalBytes||c.Pending.Count>=MaxSegments){Reject(c,"probe limit reached; connection not adopted");return;}
            c.Bytes+=segment.Data.Length;buffered+=segment.Data.Length;c.Pending.Add(new Saved{Segment=segment,Time=timestamp});
            try{c.Probe.Consume(segment,timestamp);c.Errors=c.Probe.Errors;}
            catch(Exception error){Reject(c,"probe rejected malformed data");SupportDiagnostics.Error("game-flow-probe-error",error);return;}
            if(c.Conflict){Reject(c,"conflicting own profiles; connection not adopted");return;}
            if(c.Frames<3||c.Actor==0||c.Matches==0)return;
            c.Verified=true;c.State="verified own gameplay; dispatched to overlays";
            SupportDiagnostics.Event("game-flow-adopted",new{id=c.Id,serverPort=c.Port,ownProfileActor=c.Actor,ownEnergyMatches=c.Matches,frames=c.Frames,replayedSegments=c.Pending.Count});
            // Replay the same incoming segments, not reconstructed guessed
            // profiles. The production decoder retains its TCP sequence checks
            // and initial roster/hotbar/profile order. Duplicate adapter delivery
            // is discarded there just as on the ordinary game connection.
            foreach(var saved in c.Pending){forwardedSegments++;target.Consume(saved.Segment,saved.Time);}
            buffered-=c.Bytes;c.Bytes=0;c.Pending.Clear();c.Probe=null;
        }
    }
    internal object Status {get{lock(gate){return new{scope="Exact Aion-owned incoming sockets; alternate streams require own profile and matching energy",directSegments,forwardedSegments,bufferedBytes=buffered,omitted,candidates=candidates.Values.Select(c=>new{id=c.Id,serverPort=c.Port,segments=c.Segments,frames=c.Frames,decoderErrors=c.Errors,ownProfileActor=c.Actor,ownEnergyMatches=c.Matches,verified=c.Verified,bufferedBytes=c.Bytes,state=c.State}).ToArray()};}}}
}
