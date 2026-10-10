using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

internal sealed class DashSignal {
    sealed class Flow {
        internal uint Next;internal bool Started,Synced;
        internal byte[] Buffer=new byte[8192];internal int Count,PendingBytes;
        internal readonly Dictionary<uint,byte[]> Pending=new Dictionary<uint,byte[]>();
        internal void Append(byte[] data,int offset){
            int length=data.Length-offset;if(Count+length>Buffer.Length)Array.Resize(ref Buffer,Math.Max(Count+length,Buffer.Length*2));
            System.Buffer.BlockCopy(data,offset,Buffer,Count,length);Count+=length;
        }
        internal uint FirstPending(){uint first=0;int distance=int.MaxValue;foreach(uint seq in Pending.Keys){int delta=unchecked((int)(seq-Next));if(delta<distance){first=seq;distance=delta;}}return first;}
    }
    internal sealed class Reading {internal int Actor;internal uint Value;internal double Timestamp;}
    internal struct Snapshot {internal Reading Reading;internal double LastTraffic;internal int Samples,Errors;}
    static readonly object protocolGate=new object();static readonly Dictionary<string,HashSet<int>> protocols=new Dictionary<string,HashSet<int>>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,Flow> streams=new Dictionary<string,Flow>();readonly HashSet<int> known;readonly object gate=new object();readonly byte[][] decompression=new byte[5][];
    Reading reading;double lastTraffic;int samples,errors;
    internal uint Maximum=113900; // Maximum validated against this character's HUD.
    internal Action<Reading> OnReading;
    internal Action<byte[],int,int,double,string> OnFrame;
    internal Action OnReset;
    internal DashSignal(string root){
        string path=Path.GetFullPath(Path.Combine(root,"protocol","sync-opcodes.json"));lock(protocolGate){HashSet<int> opcodes;
            if(!protocols.TryGetValue(path,out opcodes)){var json=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(path));opcodes=new HashSet<int>(((System.Collections.IEnumerable)json["syncOpcodes"]).Cast<object>().Select(Convert.ToInt32));opcodes.Add(65535);protocols[path]=opcodes;}known=opcodes;
        }
    }
    internal Snapshot ReadSnapshot(){lock(gate){return new Snapshot{Reading=reading,LastTraffic=lastTraffic,Samples=samples,Errors=errors};}}
    internal void Reset(){lock(gate){streams.Clear();reading=null;lastTraffic=0;if(OnReset!=null)OnReset();}}
    internal Reading Current {get{lock(gate){return reading;}}}
    internal double LastTraffic {get{lock(gate){return lastTraffic;}}}
    internal int Samples {get{lock(gate){return samples;}}}
    internal int Errors {get{lock(gate){return errors;}}}
    internal void Consume(Segment s,double ts){
        if(s.SrcPort!=13328)return;
        lock(gate){lastTraffic=ts;Flow flow;
            if(!streams.TryGetValue(s.Key,out flow)){flow=new Flow();if(streams.Count>=32)streams.Clear();streams[s.Key]=flow;reading=null;}
            uint seq=s.Seq;if((s.Flags&2)!=0){seq++;if(flow.Started){flow=new Flow();streams[s.Key]=flow;reading=null;}}
            if(!flow.Started){flow.Next=seq;flow.Started=true;}
            int delta=unchecked((int)(seq-flow.Next));
            if(delta>0){byte[] previous;if(flow.Pending.TryGetValue(seq,out previous))flow.PendingBytes-=previous.Length;flow.Pending[seq]=s.Data;flow.PendingBytes+=s.Data.Length;
                if(flow.Pending.Count>64||flow.PendingBytes>262144){errors++;flow.Count=0;flow.Synced=false;flow.Next=flow.FirstPending();}else return;
            }else{int overlap=-delta;if(overlap>=s.Data.Length)return;flow.Append(s.Data,overlap);flow.Next=unchecked(seq+(uint)s.Data.Length);}
            while(flow.Pending.Count>0){uint first=flow.FirstPending();delta=unchecked((int)(first-flow.Next));if(delta>0)break;
                byte[] data=flow.Pending[first];flow.Pending.Remove(first);flow.PendingBytes-=data.Length;if(-delta<data.Length){flow.Append(data,-delta);flow.Next=unchecked(first+(uint)data.Length);}}
            Cut(flow,ts,s.Key);
        }
    }
    static bool ReadVar(byte[] data,ref int position,int end,out uint value){
        value=0;for(int shift=0;shift<35;shift+=7){if(position>=end)return false;byte item=data[position++];if(shift==28&&(item&0xf0)!=0)return false;value|=(uint)(item&127)<<shift;if(item<128)return true;}return false;
    }
    static int Frame(byte[] data,int at,int limit,out int body,out int end){
        uint length;body=at;end=at;if(!ReadVar(data,ref body,limit,out length))return 0;
        if(length<6||length>65535)return -1;end=body+(int)length-4;return end<=limit?1:0;
    }
    void Cut(Flow flow,double ts,string stream){
        int position=0;
        if(!flow.Synced){for(int candidate=0;candidate<flow.Count;candidate++){int at=candidate;bool good=true;
                for(int k=0;k<3;k++){int body,end;if(Frame(flow.Buffer,at,flow.Count,out body,out end)!=1||!known.Contains((flow.Buffer[body]<<8)|flow.Buffer[body+1])){good=false;break;}at=end;}
                if(good){position=candidate;flow.Synced=true;break;}}
            if(!flow.Synced){if(flow.Count>65535){System.Buffer.BlockCopy(flow.Buffer,flow.Count-65535,flow.Buffer,0,65535);flow.Count=65535;}return;}
        }
        while(position<flow.Count){if(flow.Buffer[position]==0){position++;continue;}int body,end,result=Frame(flow.Buffer,position,flow.Count,out body,out end);if(result==0)break;if(result<0){position++;errors++;continue;}
            Decode(flow.Buffer,body,end,ts,0,stream);position=end;}
        if(position>0){flow.Count-=position;if(flow.Count>0)System.Buffer.BlockCopy(flow.Buffer,position,flow.Buffer,0,flow.Count);}
        if(flow.Count>65535){errors++;System.Buffer.BlockCopy(flow.Buffer,flow.Count-65535,flow.Buffer,0,65535);flow.Count=65535;flow.Synced=false;}
    }
    void Decode(byte[] data,int start,int end,double ts,int depth,string stream=null){
        if(depth>4){errors++;return;}if(end-start<2)return;
        if(data[start]==255&&data[start+1]==255){
            try{if(end-start<7)throw new InvalidDataException();int expected=BitConverter.ToInt32(data,start+2);if(expected<=0||expected>1000000)throw new InvalidDataException();byte[] raw=decompression[depth];if(raw==null||raw.Length<expected){raw=new byte[Math.Max(expected,Math.Min(1000000,raw==null?8192:raw.Length*2))];decompression[depth]=raw;}Lz4Into(data,start+6,expected,end,raw);int position=0;
                while(position<expected){if(raw[position]==0){position++;continue;}int body,frameEnd;if(Frame(raw,position,expected,out body,out frameEnd)!=1)throw new InvalidDataException();Decode(raw,body,frameEnd,ts,depth+1,stream);position=frameEnd;}}
            catch(InvalidDataException){errors++;}return;
        }
        if(OnFrame!=null)OnFrame(data,start,end,ts,stream);
        if(data[start]!=0||data[start+1]!=0x8d)return;
        int cursor=start+2;uint actor;if(!ReadVar(data,ref cursor,end,out actor)||cursor>=end)return;int flags=data[cursor++];if((flags&~3)!=0)return;
        uint? candidate=null;
        for(int width=4;width<=8;width+=4){if((flags&(width==4?1:2))==0)continue;if(cursor>=end)return;int count=data[cursor++];
            for(int n=0;n<count;n++){if(cursor+1+width>end)return;int kind=data[cursor];if(width==4&&kind==3)candidate=BitConverter.ToUInt32(data,cursor+1);cursor+=1+width;}}
        if(cursor!=end||!candidate.HasValue||candidate.Value>100000000)return;
        reading=new Reading{Actor=(int)actor,Value=candidate.Value,Timestamp=ts};samples++;if(OnReading!=null)OnReading(reading);
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
