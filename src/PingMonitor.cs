using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

// Per-connection Windows TCP RTT. No probes, capture filters, game hooks or
// global TCP settings. Path statistics are enabled only for AION2's game port.
internal sealed class TcpPingReader:IDisposable {
    const int PathStats=3,PathSize=160,MaximumConnections=16;
    [DllImport("iphlpapi.dll")] static extern uint GetPerTcpConnectionEStats(byte[] row,int type,byte[] rw,uint rwVersion,uint rwSize,IntPtr ros,uint rosVersion,uint rosSize,byte[] rod,uint rodVersion,uint rodSize);
    [DllImport("iphlpapi.dll")] static extern uint GetPerTcp6ConnectionEStats(byte[] row,int type,byte[] rw,uint rwVersion,uint rwSize,IntPtr ros,uint rosVersion,uint rosSize,byte[] rod,uint rodVersion,uint rodSize);
    [DllImport("iphlpapi.dll")] static extern uint SetPerTcpConnectionEStats(byte[] row,int type,byte[] rw,uint version,uint size,uint offset);
    [DllImport("iphlpapi.dll")] static extern uint SetPerTcp6ConnectionEStats(byte[] row,int type,byte[] rw,uint version,uint size,uint offset);
    internal sealed class Connection {
        internal byte[] Row;internal int Family;internal bool Owned,HasCount;internal uint Count,Error;internal int? Value;internal double FreshAt,RetryAt;
        string key; // The native endpoint row is fixed for this connection.
        internal string Key {get{return key??(key=Family+":"+Convert.ToBase64String(Row));}}
    }
    readonly Dictionary<string,Connection> connections=new Dictionary<string,Connection>();
    readonly Func<List<Connection>> discover;readonly Func<Connection,byte[],byte[],uint> read;readonly Func<Connection,bool,uint> write;
    readonly byte[] rw=new byte[1],rod=new byte[PathSize];string selected;double discoveryAt;bool disposed;
    internal int? Value;internal string Status="Waiting for Aion 2";internal uint Error;internal int GameCount,ConnectionCount;
    internal TcpPingReader(){}
    internal TcpPingReader(Func<List<Connection>> discovery,Func<Connection,byte[],byte[],uint> reading,Func<Connection,bool,uint> setting){discover=discovery;read=reading;write=setting;}
    internal static List<Connection> ParseTable(byte[] table,int family,HashSet<int> pids){
        var found=new List<Connection>();int stride=family==2?24:56;if(table==null||table.Length<4)return found;
        uint count=BitConverter.ToUInt32(table,0);if(count>(table.Length-4)/stride)throw new IOException("Invalid TCP table size");
        for(int n=0;n<count&&found.Count<MaximumConnections;n++){int at=4+n*stride,state=family==2?0:48,pid=family==2?20:52,port=family==2?16:44;
            if(BitConverter.ToUInt32(table,at+state)!=5||!pids.Contains(BitConverter.ToInt32(table,at+pid))||((table[at+port]<<8)|table[at+port+1])!=13328)continue;
            var row=new byte[family==2?20:52];if(family==2)Buffer.BlockCopy(table,at,row,0,20);else{Buffer.BlockCopy(table,at+48,row,0,4);Buffer.BlockCopy(table,at,row,4,48);}found.Add(new Connection{Row=row,Family=family});
        }return found;
    }
    static byte[] Table(int family){int size=0;uint code=Native.GetExtendedTcpTable(IntPtr.Zero,ref size,false,family,5,0);if(code!=0&&code!=122)throw new System.ComponentModel.Win32Exception((int)code);
        for(int retry=0;retry<3;retry++){if(size<4||size>4*1024*1024)throw new IOException("TCP table exceeds bounded buffer");IntPtr buffer=Marshal.AllocHGlobal(size);try{int capacity=size;code=Native.GetExtendedTcpTable(buffer,ref size,false,family,5,0);if(code==122)continue;if(code!=0)throw new System.ComponentModel.Win32Exception((int)code);if(size>capacity)throw new IOException("TCP table changed size");var bytes=new byte[size];Marshal.Copy(buffer,bytes,0,size);return bytes;}finally{Marshal.FreeHGlobal(buffer);}}
        throw new IOException("TCP table changed repeatedly");
    }
    uint Get(Connection c){rw[0]=0;Array.Clear(rod,0,rod.Length);if(read!=null)return read(c,rw,rod);return c.Family==2?GetPerTcpConnectionEStats(c.Row,PathStats,rw,0,1,IntPtr.Zero,0,0,rod,0,PathSize):GetPerTcp6ConnectionEStats(c.Row,PathStats,rw,0,1,IntPtr.Zero,0,0,rod,0,PathSize);}
    uint Set(Connection c,bool active){rw[0]=(byte)(active?1:0);if(write!=null)return write(c,active);return c.Family==2?SetPerTcpConnectionEStats(c.Row,PathStats,rw,0,1,0):SetPerTcp6ConnectionEStats(c.Row,PathStats,rw,0,1,0);}
    void Release(Connection c){if(c.Owned){Set(c,false);c.Owned=false;}}
    void Discover(double now){var found=new List<Connection>();if(discover!=null){found=discover();GameCount=1;}else{var pids=new HashSet<int>();foreach(var process in Process.GetProcessesByName("AION2")){try{if(pids.Count<8)pids.Add(process.Id);}finally{process.Dispose();}}GameCount=pids.Count;if(pids.Count>0)foreach(int family in new[]{2,23})found.AddRange(ParseTable(Table(family),family,pids));}
        var current=new Dictionary<string,Connection>();foreach(var c in found){if(current.Count>=MaximumConnections)break;Connection old;current[c.Key]=connections.TryGetValue(c.Key,out old)?old:c;}
        foreach(var c in connections.Values)if(!current.ContainsKey(c.Key))Release(c);connections.Clear();foreach(var pair in current)connections.Add(pair.Key,pair.Value);ConnectionCount=connections.Count;discoveryAt=now+2;
    }
    internal static int? Decode(bool enabled,byte[] data){if(!enabled||data==null||data.Length!=PathSize||BitConverter.ToUInt32(data,128)==0)return null;uint rtt=BitConverter.ToUInt32(data,108);return rtt==uint.MaxValue||rtt>60000?null:(int?)rtt;}
    internal static bool Fresh(Connection c,double now){return c.Value.HasValue&&now-c.FreshAt<=5;}
    internal void Poll(double now){if(disposed)return;Error=0;if(now>=discoveryAt)Discover(now);foreach(var c in connections.Values){if(now<c.RetryAt){if(c.Error!=0)Error=c.Error;continue;}uint code=Get(c);if(code==0&&rw[0]==0){code=Set(c,true);if(code==0){c.Owned=true;code=Get(c);}}
            if(code!=0){Error=c.Error=code;c.Value=null;c.RetryAt=now+10;continue;}c.Error=0;c.RetryAt=0;int? value=Decode(rw[0]!=0,rod);uint count=BitConverter.ToUInt32(rod,128);
            if(value.HasValue&&(!c.HasCount||c.Count!=count)){c.Value=value;c.Count=count;c.HasCount=true;c.FreshAt=now;}else if(!value.HasValue)c.Value=null;
        }
        Connection best=null;if(selected!=null)connections.TryGetValue(selected,out best);if(best==null||!Fresh(best,now)){best=null;foreach(var c in connections.Values)if(Fresh(c,now)&&(best==null||c.FreshAt>best.FreshAt))best=c;}
        Value=best!=null&&Fresh(best,now)?best.Value:null;selected=Value.HasValue?best.Key:null;
        if(Value.HasValue)Error=0; // A retired/secondary flow cannot invalidate the live selected server.
        Status=Value.HasValue?"Measuring server latency":GameCount==0?"Waiting for Aion 2":ConnectionCount==0?"Waiting for the game server":Error==5?"Ping measurement needs permission":Error!=0?"Ping measurement unavailable":"Waiting for server RTT samples";
    }
    public void Dispose(){if(disposed)return;disposed=true;foreach(var c in connections.Values)Release(c);connections.Clear();Value=null;}
}

internal sealed class PingMonitor:IDisposable {
    readonly object gate=new object();readonly AutoResetEvent wake=new AutoResetEvent(false);Thread thread;bool enabled,closed;int interval=1000;int? value;long stamp;string status="Ping disabled",detail="";uint error;int connections;
    internal int? Value {get{lock(gate)return enabled&&(Stopwatch.GetTimestamp()-stamp)/(double)Stopwatch.Frequency<=5?value:null;}}
    internal string Status {get{lock(gate)return status;}}
    internal string Explanation(string language){lock(gate){string text=error==5?"Windows denied TCP statistics access. Close the helper and reopen it as administrator to measure ping.":error!=0?"Windows could not read server latency (error {0}). Copy Ping diagnostics to report this failure.":status=="Ping measurement unavailable"?"The TCP latency reader is unavailable. Copy Ping diagnostics to report this failure.":"Ping is the smoothed TCP round-trip time to the game server, not server processing time. No extra ping packets are sent. Fresh samples require game traffic.";return string.Format(CultureInfo.InvariantCulture,UiLanguage.Text(text,language),error);}}
    internal string Diagnostics {get{lock(gate){int? current=Value;return "Aion 2 Helper "+typeof(PingMonitor).Assembly.GetName().Version+Environment.NewLine+"Ping: "+status+"; Windows error: "+error+"; elevated: "+FpsMonitor.ProcessIsElevated+Environment.NewLine+"Game connections: "+connections+"; refresh: "+interval+" ms; RTT: "+(current.HasValue?current.Value.ToString():"unavailable")+Environment.NewLine+detail;}}}
    internal void Configure(bool active,int refresh){lock(gate){if(closed)return;int next=Math.Max(250,Math.Min(5000,refresh));bool changed=interval!=next;interval=next;if(enabled==active){if(changed)wake.Set();return;}enabled=active;value=null;status=active?"Waiting for Aion 2":"Ping disabled";error=0;detail="";if(active&&thread==null){thread=new Thread(Run){IsBackground=true,Name="Aion TCP RTT"};thread.Start();}wake.Set();}}
    void Run(){TcpPingReader reader=null;var clock=Stopwatch.StartNew();try{while(true){bool active;int delay;lock(gate){if(closed)break;active=enabled;delay=interval;}if(!active){if(reader!=null){reader.Dispose();reader=null;}wake.WaitOne();continue;}if(reader==null)reader=new TcpPingReader();
            try{reader.Poll(clock.Elapsed.TotalSeconds);lock(gate){if(enabled){value=reader.Value;stamp=Stopwatch.GetTimestamp();status=reader.Status;error=reader.Error;connections=reader.ConnectionCount;detail="";}}}
            catch(Exception ex){if(reader!=null){reader.Dispose();reader=null;}lock(gate){if(enabled){value=null;status="Ping measurement unavailable";error=ex is System.ComponentModel.Win32Exception?(uint)((System.ComponentModel.Win32Exception)ex).NativeErrorCode:0;detail=ex.GetType().Name;}}delay=Math.Max(5000,delay);}
            if(reader!=null&&reader.GameCount==0)delay=Math.Max(2000,delay);wake.WaitOne(delay);
        }}finally{if(reader!=null)reader.Dispose();}}
    public void Dispose(){Thread running;lock(gate){if(closed)return;closed=true;enabled=false;value=null;running=thread;wake.Set();}if(running!=null)running.Join();wake.Dispose();}
}
