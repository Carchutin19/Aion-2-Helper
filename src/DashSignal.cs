using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

internal sealed class DashSignal {
    sealed class Flow {
        internal uint Next;internal bool Started,Synced,PrefixRecovery;
        internal byte[] Buffer=new byte[8192];internal int Count,PendingBytes;
        internal readonly Dictionary<uint,byte[]> Pending=new Dictionary<uint,byte[]>();
        internal bool GapWaiting;internal uint GapExpected;internal double GapSince,LastTimestamp;
        internal long GapRecoveries,SkippedBytes,Segments,Duplicates,OutOfOrder,Frames;internal int PeakPending;internal double MaxGapAge;internal string LastRecovery;
        internal void Append(byte[] data,int offset){
            int length=data.Length-offset;if(Count+length>Buffer.Length)Array.Resize(ref Buffer,Math.Max(Count+length,Buffer.Length*2));
            System.Buffer.BlockCopy(data,offset,Buffer,Count,length);Count+=length;
        }
        internal uint FirstPending(){uint first=0;int distance=int.MaxValue;foreach(uint seq in Pending.Keys){int delta=unchecked((int)(seq-Next));if(delta<distance){first=seq;distance=delta;}}return first;}
    }
    internal sealed class Reading {internal int Actor;internal uint Value;internal double Timestamp;}
    internal struct Snapshot {internal Reading Reading;internal uint DetectedMaximum;internal double LastTraffic;internal int Samples,Errors;}
    static readonly object protocolGate=new object();static readonly Dictionary<string,HashSet<int>> protocols=new Dictionary<string,HashSet<int>>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,Flow> streams=new Dictionary<string,Flow>();readonly HashSet<int> known;readonly object gate=new object();readonly byte[][] decompression=new byte[5][];
    Reading reading;double lastTraffic;int samples,errors;
    internal bool ProbeOnly,AllowOwnedPorts;
    // Measures gaps with a monotonic reception clock, without changing the
    // retransmission/reordering policy or imposing a speculative timeout.
    internal Func<double> DiagnosticClock;
    long gapRecoveries,framingErrors,containerErrors;
    internal uint Maximum=113900; // Manual fallback; never inferred from partially full energy.
    readonly Dictionary<string,Dictionary<uint,uint>> energyMaxima=new Dictionary<string,Dictionary<uint,uint>>();string readingStream;
    readonly Dictionary<string,uint> maps=new Dictionary<string,uint>();
    internal Action<Reading> OnReading;
    internal Action<byte[],int,int,double,string> OnFrame;
    internal Action OnReset;
    internal DashSignal(string root){
        string path=Path.GetFullPath(Path.Combine(root,"protocol","sync-opcodes.json"));lock(protocolGate){HashSet<int> opcodes;
            if(!protocols.TryGetValue(path,out opcodes)){var json=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(path));opcodes=new HashSet<int>(((System.Collections.IEnumerable)json["syncOpcodes"]).Cast<object>().Select(Convert.ToInt32));opcodes.Add(65535);protocols[path]=opcodes;}known=opcodes;
        }
    }
    uint DetectedMaximum(){Dictionary<uint,uint> actors;uint value;return reading!=null&&readingStream!=null&&energyMaxima.TryGetValue(readingStream,out actors)&&actors.TryGetValue((uint)reading.Actor,out value)?value:0;}
    internal Snapshot ReadSnapshot(){lock(gate){return new Snapshot{Reading=reading,DetectedMaximum=DetectedMaximum(),LastTraffic=lastTraffic,Samples=samples,Errors=errors};}}
    internal object SupportState(){lock(gate){double now=DiagnosticClock==null?0:DiagnosticClock();return new{samples,errors,lastTraffic,gapRecoveries,framingErrors,containerErrors,gapRecoveryPolicy="existing pending-count/byte limits; no timeout",knownOpcodes=known.Count,flows=streams.Select(pair=>{var f=pair.Value;return new{id=SupportDiagnostics.FlowId(pair.Key),started=f.Started,synced=f.Synced,buffered=f.Count,pendingSegments=f.Pending.Count,pendingBytes=f.PendingBytes,peakPending=f.PeakPending,prefixRecovery=f.PrefixRecovery,gapAgeSeconds=f.GapWaiting&&DiagnosticClock!=null?(double?)Math.Max(0,now-f.GapSince):null,maxGapAgeSeconds=Math.Max(f.MaxGapAge,f.GapWaiting?Math.Max(0,now-f.GapSince):0),gapBytes=f.Pending.Count==0?0:Math.Max(0,unchecked((int)(f.FirstPending()-f.Next))),gapRecoveries=f.GapRecoveries,skippedBytes=f.SkippedBytes,lastRecovery=f.LastRecovery,segments=f.Segments,duplicateSegments=f.Duplicates,outOfOrderSegments=f.OutOfOrder,decodedFrames=f.Frames,lastCaptureTimestamp=f.LastTimestamp};}).ToArray()};}}
    internal void Reset(){SupportDiagnostics.Event("decoder-reset",new{reason="Capture/transport reset"});lock(gate){streams.Clear();energyMaxima.Clear();maps.Clear();readingStream=null;reading=null;lastTraffic=0;if(OnReset!=null)OnReset();}}
    internal Reading Current {get{lock(gate){return reading;}}}
    internal double LastTraffic {get{lock(gate){return lastTraffic;}}}
    internal int Samples {get{lock(gate){return samples;}}}
    internal int Errors {get{lock(gate){return errors;}}}
    internal void Consume(Segment s,double ts){
        if(s.SrcPort!=13328&&!ProbeOnly&&!AllowOwnedPorts)return;
        lock(gate){lastTraffic=ts;double now=DiagnosticClock==null?0:DiagnosticClock();Flow flow;
            if(!streams.TryGetValue(s.Key,out flow)){flow=new Flow();if(streams.Count>=32)streams.Clear();streams[s.Key]=flow;reading=null;}
            uint seq=s.Seq;if((s.Flags&2)!=0){seq++;energyMaxima.Remove(s.Key);maps.Remove(s.Key);if(flow.Started){flow=new Flow();streams[s.Key]=flow;reading=null;}}
            if(!flow.Started){flow.Next=seq;flow.Started=true;}
            flow.LastTimestamp=ts;flow.Segments++;
            int delta=unchecked((int)(seq-flow.Next));
            if(delta>0){flow.OutOfOrder++;byte[] previous;if(flow.Pending.TryGetValue(seq,out previous)){flow.Duplicates++;flow.PendingBytes-=previous.Length;}flow.Pending[seq]=s.Data;flow.PendingBytes+=s.Data.Length;flow.PeakPending=Math.Max(flow.PeakPending,flow.Pending.Count);
                TrackGap(flow,now);
                if(flow.Pending.Count>64||flow.PendingBytes>262144)RecoverGap(flow,now,"pending limit");
                else return;
            }else{int overlap=-delta;if(overlap>=s.Data.Length){flow.Duplicates++;return;}flow.Append(s.Data,overlap);flow.Next=unchecked(seq+(uint)s.Data.Length);}
            DrainPending(flow);TrackGap(flow,now);
            Cut(flow,ts,s.Key);
        }
    }
    static void DrainPending(Flow flow){while(flow.Pending.Count>0){uint first=flow.FirstPending();int delta=unchecked((int)(first-flow.Next));if(delta>0)break;
        byte[] data=flow.Pending[first];flow.Pending.Remove(first);flow.PendingBytes-=data.Length;if(-delta<data.Length){flow.Append(data,-delta);flow.Next=unchecked(first+(uint)data.Length);}}}
    static void TrackGap(Flow flow,double now){if(flow.GapWaiting)flow.MaxGapAge=Math.Max(flow.MaxGapAge,Math.Max(0,now-flow.GapSince));if(flow.Pending.Count==0){flow.GapWaiting=false;return;}if(!flow.GapWaiting||flow.GapExpected!=flow.Next){flow.GapWaiting=true;flow.GapExpected=flow.Next;flow.GapSince=now;}}
    void RecoverGap(Flow flow,double now,string reason){uint first=flow.FirstPending();int skipped=Math.Max(0,unchecked((int)(first-flow.Next)));double waited=Math.Max(0,now-flow.GapSince);int discarded=flow.Count;
        errors++;gapRecoveries++;flow.GapRecoveries++;flow.SkippedBytes+=skipped;flow.LastRecovery=reason;
        // Existing count/byte-limit recovery. Never join bytes across a hole;
        // leave the existing boundary and partial-container policy unchanged.
        flow.Count=0;flow.Synced=false;flow.Next=first;DrainPending(flow);flow.MaxGapAge=Math.Max(flow.MaxGapAge,waited);flow.GapWaiting=false;TrackGap(flow,now);
        if(!ProbeOnly)SupportDiagnostics.Event("tcp-gap-recovery",new{reason,waitedSeconds=waited,skippedBytes=skipped,discardedPartialBytes=discarded,pendingSegments=flow.Pending.Count});
    }
    static bool ReadVar(byte[] data,ref int position,int end,out uint value){
        value=0;for(int shift=0;shift<35;shift+=7){if(position>=end)return false;byte item=data[position++];if(shift==28&&(item&0xf0)!=0)return false;value|=(uint)(item&127)<<shift;if(item<128)return true;}return false;
    }
    // Shared identity-only view of a global resource envelope. Unknown resource
    // kinds remain uninterpreted; a bounded, single current-energy value is
    // required before it can select the local character or gameplay stream.
    internal static bool EnergyActor(byte[] data,int start,int end,out uint actor){
        actor=0;if(data==null||start<0||end>data.Length||end-start<2||end-start>65536||data[start]!=0||data[start+1]!=0x8d)return false;
        int p=start+2;uint id;if(!ReadVar(data,ref p,end,out id)||id==0||id>int.MaxValue||p>=end)return false;int flags=data[p++];if(flags>3)return false;bool energy=false;
        for(int width=4;width<=8;width+=4)if((flags&(width==4?1:2))!=0){uint count;if(!ReadVar(data,ref p,end,out count)||count>(end-p)/(width+1))return false;
            for(uint i=0;i<count;i++){if(width==4&&data[p]==3){if(energy||BitConverter.ToUInt32(data,p+1)>100000000)return false;energy=true;}p+=width+1;}}
        if(p!=end||!energy)return false;actor=id;return true;
    }
    static int Frame(byte[] data,int at,int limit,out int body,out int end){
        // A uint prefix that already fails with five bytes cannot be
        // completed by later TCP segments. Only shorter prefixes may wait.
        uint length;body=at;end=at;if(!ReadVar(data,ref body,limit,out length))return limit-at>=5?-1:0;
        if(length<6||length>65535)return -1;end=body+(int)length-4;return end<=limit?1:0;
    }
    static bool IncompleteContainer(byte[] data,int body,int limit,int end){
        if(end<=limit||body>limit-6||data[body]!=255||data[body+1]!=255)return false;
        int expected=BitConverter.ToInt32(data,body+2);return expected>0&&expected<=1000000;
    }
    void Cut(Flow flow,double ts,string stream){
        int position=0;
        if(!flow.Synced){for(int candidate=0;candidate<flow.Count;candidate++){int at=candidate;bool good=true;
                for(int k=0;k<3;k++){int body,end;int result=Frame(flow.Buffer,at,flow.Count,out body,out end);
                    // An earlier confirmed boundary followed by a partial LZ4
                    // envelope must not sync to literal-looking frames inside it.
                    if(flow.PrefixRecovery&&k>0&&result==0&&end-candidate<=65535&&IncompleteContainer(flow.Buffer,body,flow.Count,end)){
                        if(candidate>0){flow.Count-=candidate;System.Buffer.BlockCopy(flow.Buffer,candidate,flow.Buffer,0,flow.Count);}
                        return;
                    }
                    if(result!=1||!known.Contains((flow.Buffer[body]<<8)|flow.Buffer[body+1])){good=false;break;}at=end;}
                if(good){position=candidate;flow.Synced=true;flow.PrefixRecovery=false;break;}}
            if(!flow.Synced){if(flow.Count>65535){System.Buffer.BlockCopy(flow.Buffer,flow.Count-65535,flow.Buffer,0,65535);flow.Count=65535;}return;}
        }
        while(position<flow.Count){if(flow.Buffer[position]==0){position++;continue;}int body,end,result=Frame(flow.Buffer,position,flow.Count,out body,out end);if(result==0)break;if(result<0){
                // A broken boundary also invalidates the following offsets.
                // Keep the remaining bytes and reacquire the existing three-
                // frame sync on the next segment instead of decoding arbitrary
                // payload bytes as actor/profile messages.
                flow.PrefixRecovery=body-position==5;position++;errors++;framingErrors++;flow.Synced=false;break;}
            Decode(flow.Buffer,body,end,ts,0,stream);position=end;}
        if(position>0){flow.Count-=position;if(flow.Count>0)System.Buffer.BlockCopy(flow.Buffer,position,flow.Buffer,0,flow.Count);}
        if(flow.Count>65535){errors++;framingErrors++;System.Buffer.BlockCopy(flow.Buffer,flow.Count-65535,flow.Buffer,0,65535);flow.Count=65535;flow.Synced=false;}
    }
    void Decode(byte[] data,int start,int end,double ts,int depth,string stream=null){
        if(depth>4){errors++;containerErrors++;return;}if(end-start<2)return;
        if(data[start]==255&&data[start+1]==255){
            try{if(end-start<7)throw new InvalidDataException();int expected=BitConverter.ToInt32(data,start+2);if(expected<=0||expected>1000000)throw new InvalidDataException();byte[] raw=decompression[depth];if(raw==null||raw.Length<expected){raw=new byte[Math.Max(expected,Math.Min(1000000,raw==null?8192:raw.Length*2))];decompression[depth]=raw;}Lz4Into(data,start+6,expected,end,raw);int position=0;
                while(position<expected){if(raw[position]==0){position++;continue;}int body,frameEnd;if(Frame(raw,position,expected,out body,out frameEnd)!=1)throw new InvalidDataException();Decode(raw,body,frameEnd,ts,depth+1,stream);position=frameEnd;}}
            catch(InvalidDataException){errors++;containerErrors++;}return;
        }
        // A new area invalidates the previous local energy sample even while
        // ordinary traffic continues. Same-map boss teleports keep valid data.
        if(stream!=null&&data[start]==0x21&&data[start+1]==0x36&&end-start==48&&BitConverter.ToUInt32(data,start+2)<=10000){
            uint map=BitConverter.ToUInt32(data,start+6),previous;
            if(map>0&&map<10000000){
                if(maps.TryGetValue(stream,out previous)&&previous!=map&&readingStream==stream){reading=null;readingStream=null;energyMaxima.Remove(stream);}
                if(maps.Count>=32&&!maps.ContainsKey(stream))maps.Clear();maps[stream]=map;
            }
        }
        Flow transport;if(stream!=null&&streams.TryGetValue(stream,out transport))transport.Frames++;
        if(!ProbeOnly)SupportDiagnostics.Frame((data[start]<<8)|data[start+1],end-start);if(OnFrame!=null)OnFrame(data,start,end,ts,stream);
        if(data[start]!=0||data[start+1]!=0x8d)return;
        int cursor=start+2;uint actor;if(!ReadVar(data,ref cursor,end,out actor)||cursor>=end)return;int flags=data[cursor++];if((flags&~3)!=0)return;
        uint? candidate=null,maximum=null;
        for(int width=4;width<=8;width+=4){if((flags&(width==4?1:2))==0)continue;uint count;if(!ReadVar(data,ref cursor,end,out count)||count>(end-cursor)/(width+1))return;
            for(int n=0;n<count;n++){int kind=data[cursor];if(width==4&&kind==3){if(candidate.HasValue)return;candidate=BitConverter.ToUInt32(data,cursor+1);}if(width==4&&kind==10){if(maximum.HasValue)return;maximum=BitConverter.ToUInt32(data,cursor+1);}cursor+=1+width;}}
        if(cursor!=end||actor==0||actor>int.MaxValue||candidate.HasValue&&candidate.Value>100000000||maximum.HasValue&&(maximum.Value==0||maximum.Value>100000000))return;
        // Global 008D u32 kind10 is the server's maximum energy, independent of
        // current kind3. It can precede the first dash or arrive by itself on a
        // level/stat change. Bind it to both TCP flow and actor, never nearby NPCs.
        if(maximum.HasValue&&stream!=null){Dictionary<uint,uint> actors;if(!energyMaxima.TryGetValue(stream,out actors)){if(energyMaxima.Count>=32)energyMaxima.Clear();actors=new Dictionary<uint,uint>();energyMaxima[stream]=actors;}if(actors.Count>=64&&!actors.ContainsKey(actor))actors.Clear();actors[actor]=maximum.Value;}
        if(!candidate.HasValue){if(maximum.HasValue&&reading!=null&&reading.Actor==(int)actor&&readingStream==stream&&OnReading!=null)OnReading(reading);return;}
        readingStream=stream;reading=new Reading{Actor=(int)actor,Value=candidate.Value,Timestamp=ts};samples++;if(OnReading!=null)OnReading(reading);
    }
    static int Extra(byte[] data,ref int position,int count,int end){
        if(count==15){byte more;do{if(position>=end)throw new InvalidDataException();more=data[position++];count+=more;}while(more==255);}return count;
    }
    internal static byte[] Lz4(byte[] data,int position,int expected,int end=-1){
        if(end<0)end=data.Length;if(expected<=0||expected>1000000||end>data.Length||position<0||position>end)throw new InvalidDataException();
        var output=new byte[expected];Lz4Into(data,position,expected,end,output);return output;
    }
    static void Lz4Into(byte[] data,int position,int expected,int end,byte[] output){int written=0;
        while(position<end){byte token=data[position++];int literal=Extra(data,ref position,token>>4,end);if(position+literal>end||written+literal>expected)throw new InvalidDataException();
            System.Buffer.BlockCopy(data,position,output,written,literal);position+=literal;written+=literal;if(position==end)break;
            if(position+2>end)throw new InvalidDataException();int offset=data[position]|data[position+1]<<8;position+=2;int match=Extra(data,ref position,token&15,end)+4;
            if(offset<=0||offset>written||written+match>expected)throw new InvalidDataException();
            int source=written-offset,available=offset;while(match>0){int count=Math.Min(available,match);System.Buffer.BlockCopy(output,source,output,written,count);written+=count;match-=count;available+=count;}}
        if(written!=expected)throw new InvalidDataException();
    }
    internal static void VerifyProtocol(string root){
        byte[] literal=Lz4(new byte[]{0x50,104,101,108,108,111},0,5);if(System.Text.Encoding.ASCII.GetString(literal)!="hello")throw new Exception("LZ4 literals");
        byte[] repeated=Lz4(new byte[]{0x10,97,1,0},0,5);if(System.Text.Encoding.ASCII.GetString(repeated)!="aaaaa")throw new Exception("LZ4 overlapping match");
        bool rejected=false;try{Lz4(new byte[]{0,0,0},0,4);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Invalid LZ4 offset");
        rejected=false;try{Lz4(new byte[]{0x50,104,101,108,108,111},0,5,5);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("LZ4 must not read past its containing frame");
        VerifyContainers(root);
        VerifyConnectionStart(root);
        VerifyEnergyMaximum(root);
        VerifyResynchronization(root);
        VerifyPrefixRecovery(root);
        byte[] frame=new byte[]{14,0,0x8d,1,1,1,3,100,0,0,0},data=new byte[44];for(int n=0;n<4;n++)System.Buffer.BlockCopy(frame,0,data,n*frame.Length,frame.Length);
        foreach(uint sequence in new uint[]{100,uint.MaxValue-4}){
            var decoder=new DashSignal(root);Func<int,int,Segment> segment=delegate(int offset,int count){var payload=new byte[count];System.Buffer.BlockCopy(data,offset,payload,0,count);return new Segment{Src="test",Dst="self",SrcPort=13328,DstPort=1234,Seq=unchecked(sequence+(uint)offset),Data=payload};};
            decoder.Consume(segment(0,6),1);var pending=segment(15,29);for(int i=0;i<100;i++)decoder.Consume(pending,1);decoder.Consume(segment(6,9),1);
            if(decoder.Samples!=4||decoder.Errors!=0||decoder.Current.Value!=100)throw new Exception("TCP split/out-of-order and duplicate pending segments");
            decoder.Consume(segment(0,44),1);if(decoder.Samples!=4)throw new Exception("TCP retransmissions must not duplicate readings");
            decoder.Reset();if(decoder.Current!=null)throw new Exception("Restarted capture must discard the old reading");var resumed=segment(0,44);resumed.Seq=unchecked(sequence+1000u);decoder.Consume(resumed,2);
            if(decoder.Samples!=8||decoder.Errors!=0||decoder.Current.Timestamp!=2)throw new Exception("Capture restart must resume at a new TCP sequence without waiting for missing old packets");
        }
    }
    static void VerifyResynchronization(string root){
        Func<byte,uint,byte[]> frame=delegate(byte actor,uint value){var b=new byte[]{14,0,0x8d,actor,1,1,3,0,0,0,0};Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,7,4);return b;};
        var bytes=new List<byte>();for(int i=0;i<3;i++)bytes.AddRange(frame(1,100));
        bytes.Add(1); // An impossible frame length invalidates the boundary.
        bytes.AddRange(frame(99,999));bytes.Add(1); // Plausible isolated payload is not a new boundary.
        for(int i=0;i<3;i++)bytes.AddRange(frame(1,200));
        foreach(uint sequence in new uint[]{100,uint.MaxValue-20}){
            var decoder=new DashSignal(root);var actors=new List<int>();decoder.OnReading=delegate(Reading r){actors.Add(r.Actor);};
            decoder.Consume(new Segment{Src="recovery",Dst="self",SrcPort=13328,DstPort=1234,Seq=sequence,Data=bytes.ToArray()},1);
            decoder.Consume(new Segment{Src="recovery",Dst="self",SrcPort=13328,DstPort=1234,Seq=unchecked(sequence+(uint)bytes.Count),Data=new byte[]{0}},2);
            if(decoder.Samples!=6||decoder.Current==null||decoder.Current.Actor!=1||decoder.Current.Value!=200||actors.Contains(99)||decoder.Errors!=1)throw new Exception("Invalid framing must reacquire confirmed boundaries without dispatching an isolated false actor");
        }
    }
    static void VerifyPrefixRecovery(string root){
        int body,end;
        for(int count=1;count<5;count++){var partial=Enumerable.Repeat((byte)0x80,count).ToArray();if(Frame(partial,0,partial.Length,out body,out end)!=0)throw new Exception("Fragmented length prefixes must wait for their remaining bytes");}
        foreach(var invalid in new[]{new byte[]{128,128,128,128,128},new byte[]{255,255,255,255,31},new byte[]{128,128,128,128,16}})
            if(Frame(invalid,0,invalid.Length,out body,out end)>=0)throw new Exception("Invalid five-byte lengths must not wait until the TCP buffer fills");
        var extended=new byte[]{134,128,128,128,0,0,141};if(Frame(extended,0,extended.Length,out body,out end)!=1||body!=5||end!=7)throw new Exception("A valid five-byte length must still decode");
        Func<byte,uint,byte[]> frame=delegate(byte actor,uint value){var b=new byte[]{14,0,0x8d,actor,1,1,3,0,0,0,0};Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,7,4);return b;};
        Func<byte[],byte[]> wrap=delegate(byte[] payload){var b=new List<byte>();uint length=(uint)payload.Length+4;while(length>=128){b.Add((byte)((length&127)|128));length>>=7;}b.Add((byte)length);b.AddRange(payload);return b.ToArray();};
        // Three frame-looking byte strings are data inside an unknown message,
        // not independent energy updates. LZ4 literals retain those byte strings.
        var opaque=new List<byte>{0x53,0x8d};for(int n=0;n<3;n++)opaque.AddRange(frame(99,999));opaque.AddRange(new byte[100]);
        var raw=new List<byte>();raw.AddRange(wrap(opaque.ToArray()));raw.AddRange(frame(1,300));
        var container=new List<byte>{255,255};container.AddRange(BitConverter.GetBytes(raw.Count));container.Add(0xf0);container.Add((byte)(raw.Count-15));container.AddRange(raw);
        var packed=wrap(container.ToArray());var initial=new List<byte>();for(int n=0;n<3;n++)initial.AddRange(frame(1,100));
        foreach(uint sequence in new uint[]{100,uint.MaxValue-40}){
            var decoder=new DashSignal(root);var actors=new List<int>();decoder.OnReading=delegate(Reading r){actors.Add(r.Actor);};
            uint next=sequence;Action<byte[],double> send=delegate(byte[] bytes,double ts){decoder.Consume(new Segment{Src="prefix-recovery",Dst="self",SrcPort=13328,DstPort=1234,Seq=next,Data=bytes},ts);next=unchecked(next+(uint)bytes.Length);};
            send(initial.ToArray(),1);
            send(new byte[]{128,128,128,128},2);
            if(decoder.Errors!=0||decoder.Samples!=3)throw new Exception("Four partial prefix bytes must not discard synchronized state");
            send(new byte[]{128},3);
            if(decoder.Errors!=1||decoder.streams.Values.Single().Count>5)throw new Exception("The fifth invalid prefix byte must start bounded recovery immediately");
            var partial=new List<byte>();partial.AddRange(frame(1,200));partial.AddRange(packed.Take(60));send(partial.ToArray(),4);
            if(decoder.Samples!=3||actors.Contains(99))throw new Exception("Recovery must wait for a pending outer container rather than dispatch its literal-looking inner bytes");
            var remainder=new List<byte>();remainder.AddRange(packed.Skip(60));remainder.AddRange(frame(1,400));send(remainder.ToArray(),5);
            if(decoder.Errors!=1||decoder.Samples!=6||decoder.Current.Actor!=1||decoder.Current.Value!=400||actors.Contains(99))throw new Exception("Valid outer frames must recover promptly without losing or inventing contained readings");
        }
    }
    static void VerifyEnergyMaximum(string root){
        var decoder=new DashSignal(root);int wakes=0;decoder.OnReading=delegate{wakes++;};
        Action<string,string,double> apply=delegate(string hex,string flow,double ts){var bytes=new byte[hex.Length/2];for(int n=0;n<bytes.Length;n++)bytes[n]=Convert.ToByte(hex.Substring(n*2,2),16);decoder.Decode(bytes,0,bytes.Length,ts,0,flow);};
        apply("008D0101010A70C00100","a",1); // Maximum can precede local identification.
        if(decoder.Current!=null||decoder.ReadSnapshot().DetectedMaximum!=0)throw new Exception("Maximum alone must not invent current energy/local identity");
        apply("008D01010103404B0100","a",2); // 84800 current, 114800 full.
        if(decoder.Current.Value!=84800||decoder.ReadSnapshot().DetectedMaximum!=114800)throw new Exception("Server maximum must work with partial energy");
        apply("008D0201010AA0860100","a",3);apply("008D0101010AA0860100","b",3);
        if(decoder.ReadSnapshot().DetectedMaximum!=114800)throw new Exception("Other actors/flows must not recalibrate local energy");
        apply("008D0101010A9CC10100","a",4); // Captured 115100 maximum-only stat change.
        if(decoder.ReadSnapshot().DetectedMaximum!=115100||decoder.Current.Value!=84800||decoder.Current.Timestamp!=2||decoder.Samples!=1||wakes!=2)throw new Exception("Maximum-only update must wake UI without falsifying energy freshness/samples");
        apply("008D0101010A70C0010000","a",5);apply("008D0101010A00000000","a",5);
        if(decoder.ReadSnapshot().DetectedMaximum!=115100)throw new Exception("Malformed/zero maximum must leave previous value intact");
        apply("008D0101010A70C00100","a",6);
        if(decoder.ReadSnapshot().DetectedMaximum!=114800)throw new Exception("Maximum decreases must also be honored");
        decoder.Reset();apply("008D01010103404B0100","a",7);
        if(decoder.ReadSnapshot().DetectedMaximum!=0)throw new Exception("Restart must clear stale actor/flow maxima");
        var map=new byte[48];map[0]=0x21;map[1]=0x36;Buffer.BlockCopy(BitConverter.GetBytes(1001u),0,map,6,4);
        decoder.Decode(map,0,map.Length,8,0,"a");decoder.Decode(map,0,map.Length,8.1,0,"a");
        if(decoder.Current==null)throw new Exception("Same-map phase teleport must retain energy");
        Buffer.BlockCopy(BitConverter.GetBytes(1002u),0,map,6,4);decoder.Decode(map,0,map.Length,9,0,"b");
        if(decoder.Current==null)throw new Exception("Unrelated flow map cannot clear energy");
        decoder.Decode(map,0,map.Length,10,0,"a");
        if(decoder.Current!=null)throw new Exception("Area transition must hide stale local energy until a new actual sample");
        apply("008D0101010A70C00100","a",11);
        if(decoder.Current!=null)throw new Exception("Maximum-only update must not reveal energy without a current sample");
        apply("008D01010103404B0100","a",12);
        if(decoder.Current==null||decoder.ReadSnapshot().DetectedMaximum!=114800)throw new Exception("New area actual sample must restore energy and automatic maximum");
    }
    static void VerifyConnectionStart(string root){
        // Initial1136/1536 envelopes precede compressed gameplay messages in
        // independently validated instance-entry streams. Keep the existing
        // three-boundary requirement; never sync to literals inside a bundle.
        var decoder=new DashSignal(root);var ops=new List<int>();decoder.OnFrame=delegate(byte[] b,int s,int e,double t,string flow){ops.Add((b[s]<<8)|b[s+1]);};
        byte[] first=new byte[]{8,0x11,0x36,0,0},second=new byte[]{8,0x15,0x36,0,0};
        var raw=new List<byte>();for(int n=0;n<3;n++)raw.AddRange(new byte[]{14,0,0x8d,1,1,1,3,100,0,0,0});
        var bundle=new List<byte>{(byte)(raw.Count+12),255,255};bundle.AddRange(BitConverter.GetBytes(raw.Count));bundle.Add(0xf0);bundle.Add((byte)(raw.Count-15));bundle.AddRange(raw);
        uint sequence=100;Action<byte[]> send=delegate(byte[] data){decoder.Consume(new Segment{Src="entry",SrcPort=13328,Dst="self",DstPort=1234,Seq=sequence,Data=data},1);sequence+=(uint)data.Length;};
        send(first);send(second);if(ops.Count!=0)throw new Exception("Entry handshake still requires three boundaries");
        send(bundle.Take(bundle.Count-1).ToArray());if(ops.Count!=0)throw new Exception("Partial third envelope cannot dispatch embedded literals");
        send(bundle.Skip(bundle.Count-1).ToArray());
        if(decoder.Errors!=0||decoder.Samples!=3||ops.Count!=5||ops[0]!=0x1136||ops[1]!=0x1536)throw new Exception("Confirmed connection-start envelopes must preserve complete compressed messages");
    }
    static void VerifyContainers(string root){
        Func<uint,byte[]> frame=delegate(uint value){var bytes=new byte[]{14,0,0x8d,1,1,1,3,0,0,0,0};System.Buffer.BlockCopy(BitConverter.GetBytes(value),0,bytes,7,4);return bytes;};
        Func<byte[],byte[]> container=delegate(byte[] raw){var bytes=new List<byte>{255,255};bytes.AddRange(BitConverter.GetBytes(raw.Length));bytes.Add((byte)(Math.Min(raw.Length,15)<<4));if(raw.Length>=15){int extra=raw.Length-15;while(extra>=255){bytes.Add(255);extra-=255;}bytes.Add((byte)extra);}bytes.AddRange(raw);return bytes.ToArray();};
        Func<byte[],byte[]> wrap=delegate(byte[] body){var bytes=new List<byte>();uint length=(uint)body.Length+4;while(length>=128){bytes.Add((byte)((length&127)|128));length>>=7;}bytes.Add((byte)length);bytes.AddRange(body);return bytes.ToArray();};
        var decoder=new DashSignal(root);var large=new byte[400];var first=frame(100);System.Buffer.BlockCopy(first,0,large,large.Length-first.Length,first.Length);var compressed=container(large);decoder.Decode(compressed,0,compressed.Length,1,0);var buffer=decoder.decompression[0];
        compressed=container(frame(200));decoder.Decode(compressed,0,compressed.Length,2,0);
        if(decoder.Samples!=2||decoder.Errors!=0||decoder.Current.Value!=200||decoder.decompression[0]!=buffer)throw new Exception("Reused decompression must ignore old bytes beyond the new output length");
        var inner=new List<byte>();inner.AddRange(frame(300));inner.AddRange(frame(400));var outer=new List<byte>();outer.AddRange(wrap(container(inner.ToArray())));outer.AddRange(frame(500));compressed=container(outer.ToArray());decoder.Decode(compressed,0,compressed.Length,3,0);
        if(decoder.Samples!=5||decoder.Errors!=0||decoder.Current.Value!=500||decoder.decompression[0]==decoder.decompression[1])throw new Exception("Nested compressed containers require independent buffers and must preserve trailing sibling frames");
        var malformed=container(frame(600));System.Buffer.BlockCopy(BitConverter.GetBytes(12),0,malformed,2,4);decoder.Decode(malformed,0,malformed.Length,4,0);
        if(decoder.Samples!=5||decoder.Errors!=1)throw new Exception("Truncated reused decompression must not produce a reading");
        compressed=container(frame(700));for(int depth=0;depth<5;depth++)compressed=container(wrap(compressed));decoder.Decode(compressed,0,compressed.Length,5,0);
        if(decoder.Samples!=5||decoder.Errors!=2)throw new Exception("Compressed nesting remains bounded");
    }
    internal static void ReplayTest(string root,string input) {
        var decoder=new DashSignal(root);var json=new JavaScriptSerializer();var samples=new List<Reading>();decoder.OnReading=delegate(Reading r){samples.Add(r);};
        foreach(string line in File.ReadLines(input)){var row=json.Deserialize<Dictionary<string,object>>(line);string[] endpoints=((string)row["stream"]).Split('>');string src=endpoints[0],dst=endpoints[1];string hex=(string)row["hex"];var data=new byte[hex.Length/2];for(int n=0;n<data.Length;n++)data[n]=Convert.ToByte(hex.Substring(n*2,2),16);
            decoder.Consume(new Segment{Src=src.Substring(0,src.LastIndexOf(':')),SrcPort=int.Parse(src.Substring(src.LastIndexOf(':')+1)),Dst=dst.Substring(0,dst.LastIndexOf(':')),DstPort=int.Parse(dst.Substring(dst.LastIndexOf(':')+1)),Seq=Convert.ToUInt32(row["seq"]),Flags=Convert.ToByte(row["flags"]),Data=data},Convert.ToDouble(row["ts"]));}
        string summary="Samples="+samples.Count+"; errors="+decoder.Errors+(samples.Count>0?"; actor="+samples[0].Actor+"; min="+samples.Min(x=>x.Value)+"; max="+samples.Max(x=>x.Value)+"; last="+samples.Last().Value:"");
        File.WriteAllText(Path.Combine(root,"replay-test.txt"),summary);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(input),"live-decoder-values.json"),json.Serialize(samples.Select(x=>new {ts=x.Timestamp,actor=x.Actor,value=x.Value}).ToArray()));
        if(samples.Count!=79||decoder.Errors!=0||samples.Min(x=>x.Value)!=18900||samples.Last().Value!=113900)throw new Exception("Replay regression: "+summary);
    }
}
