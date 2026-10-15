// Optional client-position stream. Protocol references and attribution:
// assets/licenses/Aion2ProtocolResearch-MIT.txt. No keys or decrypted bodies are logged.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

internal sealed class PositionRc4:IDisposable {
    readonly byte[] state=new byte[256];int i,j;
    internal PositionRc4(byte[] key){if(key==null||key.Length==0)throw new ArgumentException("Missing session key");for(int k=0;k<256;k++)state[k]=(byte)k;int n=0;for(int k=0;k<256;k++){n=(n+state[k]+key[k%key.Length])&255;byte v=state[k];state[k]=state[n];state[n]=v;}}
    internal byte[] Transform(byte[] input){var output=new byte[input.Length];for(int k=0;k<input.Length;k++){i=(i+1)&255;j=(j+state[i])&255;byte v=state[i];state[i]=state[j];state[j]=v;output[k]=(byte)(input[k]^state[(state[i]+state[j])&255]);}return output;}
    public void Dispose(){Array.Clear(state,0,state.Length);i=j=0;}
}

internal static class ClientMovement {
    internal static bool Parse(byte[] body,double timestamp,out EnemyDetailReader.Position position,out bool stationary){position=new EnemyDetailReader.Position();stationary=false;if(body==null||body.Length<3)return false;int op=(body[0]<<8)|body[1],offset;
        if(op==0x0037||op==0x0137){if(!((body.Length==29&&body[2]==0)||(body.Length==30&&body[2]==2)))return false;offset=3;stationary=body[2]==0;}
        else if(op==0x1837){if(body.Length!=28||body[2]!=0||body[3]!=0)return false;offset=4;stationary=true;}
        else if(op==0x0337){if(body.Length!=40||body[2]!=2)return false;offset=3;}
        else if(op==0x0b37){if(body.Length!=39)return false;offset=2;}
        else if(op==0x1037||op==0x1137){if(body.Length!=29)return false;offset=2;}
        else return false;
        ulong clock=BitConverter.ToUInt64(body,body.Length-8);if(clock<=1000000000000||clock>=3000000000000||!EnemyDetailReader.Coordinates(body,offset,body.Length,out position))return false;position.Time=timestamp;return true;
    }
}

internal sealed class ClientPositionStream:IDisposable {
    internal sealed class Reading {internal EnemyDetailReader.Position Position;internal double Traffic;internal bool Stationary;}
    sealed class CipherFrame {internal byte[] Body;internal double Time;}
    static readonly byte[] HandshakePrefix={0x10,0x36,0x8c,2,0x30,0x82,1,8,2,0x82,1,1,0};
    byte[] buffer=new byte[8192];int count,pendingBytes;uint next;bool started;
    readonly Dictionary<uint,byte[]> pending=new Dictionary<uint,byte[]>();readonly List<CipherFrame> waiting=new List<CipherFrame>();int waitingBytes;
    PositionRc4 cipher;internal readonly string ServerFlow;internal readonly int Pid;
    internal bool Handshake,Failed;internal string Error;internal long Frames,Positions;internal double Traffic,LastInput;internal Reading Latest;
    internal ClientPositionStream(string server,int pid){ServerFlow=server;Pid=pid;}
    internal bool Ready {get{return cipher!=null&&!Failed;}}
    void Fail(string reason){Failed=true;Error=reason;Latest=null;if(cipher!=null){cipher.Dispose();cipher=null;}waiting.Clear();waitingBytes=0;pending.Clear();pendingBytes=0;Array.Clear(buffer,0,buffer.Length);count=0;}
    static int Length(byte[] data,int at,int end,out int body,out int finish){uint length=0;body=at;finish=at;for(int shift=0;shift<35;shift+=7){if(body>=end)return 0;byte b=data[body++];if(shift==28&&(b&240)!=0)return -1;length|=(uint)(b&127)<<shift;if(b<128){if(length<6||length>65535)return -1;finish=body+(int)length-4;return finish<=end?1:0;}}return -1;}
    void Append(byte[] data,int offset){int n=data.Length-offset;if(n<=0)return;if(count+n>1048576){Fail("Client stream buffer limit reached");return;}if(buffer.Length<count+n)Array.Resize(ref buffer,Math.Min(1048576,Math.Max(buffer.Length*2,count+n)));Buffer.BlockCopy(data,offset,buffer,count,n);count+=n;}
    internal void Consume(Segment segment,double time){if(Failed)return;LastInput=Math.Max(LastInput,time);uint sequence=segment.Seq;if((segment.Flags&2)!=0)sequence++;if(!started){next=sequence;started=true;}int delta=unchecked((int)(sequence-next));if(delta>0){byte[] old;if(pending.TryGetValue(sequence,out old))pendingBytes-=old.Length;pending[sequence]=segment.Data;pendingBytes+=segment.Data.Length;if(pending.Count>64||pendingBytes>262144)Fail("Missing client traffic; reconnect Aion for exact distance");return;}
        if(-delta<segment.Data.Length){Append(segment.Data,-delta);next=unchecked(sequence+(uint)segment.Data.Length);}if(Failed)return;
        while(pending.Count>0){uint first=0;int best=int.MaxValue;foreach(uint key in pending.Keys){int d=unchecked((int)(key-next));if(d<best){best=d;first=key;}}if(best>0)break;var data=pending[first];pending.Remove(first);pendingBytes-=data.Length;if(-best<data.Length){Append(data,-best);next=unchecked(first+(uint)data.Length);}if(Failed)return;}
        Cut(time);
    }
    void Cut(double time){int at=0;if(!Handshake){bool found=false;for(int q=0;q<count;q++){int b,e;if(Length(buffer,q,count,out b,out e)!=1||e-b!=283||b>count-HandshakePrefix.Length)continue;bool same=true;for(int k=0;k<HandshakePrefix.Length;k++)if(buffer[b+k]!=HandshakePrefix[k]){same=false;break;}if(same){Handshake=found=true;at=e;break;}}if(!found){if(count>65536){Buffer.BlockCopy(buffer,count-65536,buffer,0,65536);count=65536;}return;}}
        while(at<count){if(buffer[at]==0){at++;continue;}int body,end;int parsed=Length(buffer,at,count,out body,out end);if(parsed==0)break;if(parsed<0){Fail("Invalid client frame; reconnect Aion for exact distance");return;}var encrypted=new byte[end-body];Buffer.BlockCopy(buffer,body,encrypted,0,encrypted.Length);if(cipher==null){waiting.Add(new CipherFrame{Body=encrypted,Time=time});waitingBytes+=encrypted.Length;if(waitingBytes>1048576){Fail("Session key unavailable before buffer limit");return;}}else Decode(encrypted,time);at=end;}
        if(at>0){count-=at;if(count>0)Buffer.BlockCopy(buffer,at,buffer,0,count);}
    }
    internal bool AcceptsKey(byte[] key){if(Failed||cipher!=null||!Handshake||waiting.Count==0||key==null||key.Length!=214)return false;using(var probe=new PositionRc4(key)){var bytes=probe.Transform(waiting[0].Body);try{return bytes.Length==158&&bytes[0]==0x13&&bytes[1]==0x36;}finally{Array.Clear(bytes,0,bytes.Length);}}}
    internal bool OfferKey(byte[] key){if(Failed||cipher!=null||!Handshake||waiting.Count==0||key==null||key.Length!=214)return false;var probe=new PositionRc4(key);var first=probe.Transform(waiting[0].Body);bool valid=first.Length==158&&first[0]==0x13&&first[1]==0x36;Array.Clear(first,0,first.Length);if(!valid){probe.Dispose();return false;}cipher=probe;Frames++;Traffic=waiting[0].Time;
        for(int n=1;n<waiting.Count;n++)Decode(waiting[n].Body,waiting[n].Time);waiting.Clear();waitingBytes=0;return true;
    }
    void Decode(byte[] encrypted,double time){var body=cipher.Transform(encrypted);try{Frames++;Traffic=Math.Max(Traffic,time);EnemyDetailReader.Position position;bool stationary;if(ClientMovement.Parse(body,time,out position,out stationary)){if(Latest==null||position.Time>=Latest.Position.Time)Latest=new Reading{Position=position,Traffic=Traffic,Stationary=stationary};Positions++;}}finally{Array.Clear(body,0,body.Length);}}
    internal void Placement(EnemyDetailReader.Position? position){if(!position.HasValue)Latest=null;else if(Latest==null||position.Value.Time>=Latest.Position.Time)Latest=new Reading{Position=position.Value,Traffic=Traffic,Stationary=true};}
    internal Reading Read(double now){if(!Ready||Latest==null||Math.Max(0,now-Traffic)>10)return null;
        // A complete stop remains valid while the same ordered stream stays live.
        // Do not freeze a moving sample after traffic/position updates disappear.
        if(!Latest.Stationary&&Math.Max(0,now-Latest.Position.Time)>2)return null;
        return new Reading{Position=Latest.Position,Traffic=Traffic,Stationary=Latest.Stationary};
    }
    public void Dispose(){Fail("Closed");}
}

internal sealed class ClientPositionReader:IDisposable {
    readonly object gate=new object(),workerGate=new object();readonly Dictionary<string,ClientPositionStream> flows=new Dictionary<string,ClientPositionStream>();readonly Func<int,byte[]> testFinder;readonly bool automatic;
    Timer timer;SessionKeyMemory memory;bool enabled,disposed;int generation,working;string error="";double nextProbe;internal Action Changed;
    internal ClientPositionReader(Func<int,byte[]> finder=null,bool background=true){testFinder=finder;automatic=background;}
    internal bool Enabled {get{lock(gate)return enabled;}}
    internal string ReadingStatus {get{lock(gate){if(!enabled)return "Exact distance disabled";if(error.Length>0)return error;bool handshake=false;foreach(var f in flows.Values){if(f.Ready)return f.Latest==null?"Move once to read your exact position":"Exact distance ready";if(f.Failed)return f.Error;handshake|=f.Handshake;}return handshake?"Waiting for the game session key":"Open Aion after enabling exact distance";}}}
    internal object Status {get{lock(gate){long frames=0,positions=0;int ready=0;foreach(var f in flows.Values){frames+=f.Frames;positions+=f.Positions;if(f.Ready)ready++;}return new{enabled=enabled,status=ReadingStatus,readySessions=ready,movementReadings=positions,decryptedFrames=frames,keysPersisted=false};}}}
    void Clear(){foreach(var f in flows.Values)f.Dispose();flows.Clear();}
    internal void Configure(bool value){lock(gate){if(disposed||enabled==value)return;enabled=value;generation++;Clear();error="";nextProbe=0;if(timer!=null){timer.Dispose();timer=null;}if(value&&automatic)timer=new Timer(delegate{Maintain();},null,0,2000);}
        if(!value&&Interlocked.CompareExchange(ref working,0,0)==0)lock(workerGate){if(memory!=null){memory.Dispose();memory=null;}}Notify();
    }
    internal void Reset(){lock(gate){generation++;Clear();error="";nextProbe=0;}Notify();}
    void Notify(){var change=Changed;if(change!=null)change();}
    internal void Consume(Segment segment,double timestamp,int pid){if(segment==null||segment.DstPort!=13328||pid<=0)return;bool changed=false;lock(gate){if(!enabled||disposed)return;string server=segment.Dst+":"+segment.DstPort+">"+segment.Src+":"+segment.SrcPort;ClientPositionStream flow;
            if(flows.TryGetValue(segment.Key,out flow)&&(segment.Flags&2)!=0){flow.Dispose();flows.Remove(segment.Key);flow=null;}
            if(flow==null){if(flows.Count>=4){string remove=null;double oldest=double.MaxValue;foreach(var item in flows)if(item.Value.Traffic<oldest){oldest=item.Value.Traffic;remove=item.Key;}if(remove!=null){flows[remove].Dispose();flows.Remove(remove);}}flows[segment.Key]=flow=new ClientPositionStream(server,pid);}
            long before=flow.Positions;bool handshake=flow.Handshake;flow.Consume(segment,timestamp);changed=before!=flow.Positions||handshake!=flow.Handshake||flow.Failed;
        }if(changed)Notify();
    }
    internal void ObserveServer(byte[] bytes,int start,int end,double time,string flow){if(bytes==null||start<0||end>bytes.Length||end-start<2)return;int op=(bytes[start]<<8)|bytes[start+1];if(op!=0x2136&&op!=0x2336)return;EnemyDetailReader.Position? placement=null;if(op==0x2336){int p=start+2;uint actor;EnemyDetailReader.Position position;if(!DpsSignal.Var(bytes,ref p,end,out actor)||actor!=0||!EnemyDetailReader.Coordinates(bytes,p+5,end,out position))return;position.Time=time;placement=position;}else if(end-start!=48||BitConverter.ToUInt32(bytes,start+2)>10000)return;bool changed=false;lock(gate){if(!enabled||disposed)return;foreach(var item in flows.Values)if(item.ServerFlow==flow){item.Placement(placement);changed=true;}}if(changed)Notify();}
    internal ClientPositionStream.Reading Read(string server,double now){lock(gate){if(!enabled||disposed)return null;foreach(var f in flows.Values)if(f.ServerFlow==server)return f.Read(now);return null;}}
    internal void Maintain(){if(Interlocked.CompareExchange(ref working,1,0)!=0)return;byte[] key=null;bool notify=false;try{int pid=0,version;double now=DpsHistory.UtcNow();lock(gate){if(!enabled||disposed||now<nextProbe)return;version=generation;double newest=double.MinValue;foreach(var f in flows.Values)if(f.Handshake&&!f.Ready&&!f.Failed&&f.LastInput>=newest){pid=f.Pid;newest=f.LastInput;}if(pid==0)return;nextProbe=now+2;notify=true;}
            lock(workerGate){if(testFinder!=null)key=testFinder(pid);else{if(memory==null)memory=new SessionKeyMemory();key=memory.ReadKey(pid,delegate{lock(gate)return disposed||!enabled||generation!=version;},delegate(byte[] candidate){lock(gate){if(disposed||!enabled||generation!=version)return false;foreach(var flow in flows.Values)if(flow.Pid==pid&&flow.AcceptsKey(candidate))return true;return false;}});}}
            lock(gate){if(!enabled||disposed||generation!=version)return;if(key!=null){foreach(var f in flows.Values)if(f.Pid==pid)f.OfferKey(key);error="";}else{error="Exact distance: session key unavailable; retrying";nextProbe=DpsHistory.UtcNow()+30;}}
        }catch(OperationCanceledException){}catch(Win32Exception ex){lock(gate){error="Exact distance cannot read Aion (Windows "+ex.NativeErrorCode+"). Run the helper as administrator.";nextProbe=DpsHistory.UtcNow()+30;}}
        catch(Exception ex){lock(gate){error="Exact distance: "+ex.Message;nextProbe=DpsHistory.UtcNow()+30;}}
        finally{if(key!=null)Array.Clear(key,0,key.Length);lock(workerGate){lock(gate){if((!enabled||disposed)&&memory!=null){memory.Dispose();memory=null;}}}Interlocked.Exchange(ref working,0);if(notify)Notify();}
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;enabled=false;generation++;if(timer!=null){timer.Dispose();timer=null;}Clear();}if(Interlocked.CompareExchange(ref working,0,0)==0)lock(workerGate){if(memory!=null){memory.Dispose();memory=null;}}}
}
