using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// Windows DXGI's Logging channel exposes successful Present calls even on
// machines where its Analytic channel produces no completed PresentMon frames.
// No game handles, hooks, GPU telemetry, disk traces, or compositor FPS are used.
internal sealed class FpsEtw : IDisposable {
    static readonly Guid Provider = new Guid("ca11c036-0102-4a2d-a6ad-f03cfed5d3c9");
    readonly string session;
    readonly FpsSamples samples = new FpsSamples(0);
    readonly Dictionary<long, Pending> pending = new Dictionary<long, Pending>();
    volatile HashSet<int> gameProcesses = new HashSet<int>();
    readonly EventCallback callback;
    Thread reader;
    IntPtr properties;
    ulong controller, consumer = ulong.MaxValue;
    bool closed;
    readonly FpsGameDiscovery discovery=new FpsGameDiscovery();
    internal uint Error;
    struct Pending { internal int Pid; internal ulong Chain; internal long Time; }

    // These explicit layouts are the Windows SDK x64 ABI. Build.ps1 targets x64.
    [StructLayout(LayoutKind.Explicit, Size=448)]
    struct Logfile {
        [FieldOffset(8)] internal IntPtr LoggerName;
        [FieldOffset(28)] internal uint Mode;
        [FieldOffset(424)] internal IntPtr Callback;
    }
    [StructLayout(LayoutKind.Explicit, Size=112)]
    internal struct Record {
        [FieldOffset(4)] internal ushort Flags;
        [FieldOffset(8)] internal uint Thread;
        [FieldOffset(12)] internal int Pid;
        [FieldOffset(16)] internal long Time;
        [FieldOffset(24)] internal Guid Provider;
        [FieldOffset(40)] internal ushort Id;
        [FieldOffset(42)] internal byte Version;
        [FieldOffset(86)] internal ushort Length;
        [FieldOffset(96)] internal IntPtr Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct EnableParameters {internal uint Version, Property, Control; internal Guid Source; internal IntPtr Filters; internal uint Count;}
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void EventCallback(ref Record record);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode)] static extern uint StartTraceW(out ulong handle,string name,IntPtr properties);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode)] static extern uint ControlTraceW(ulong handle,string name,IntPtr properties,uint action);
    [DllImport("advapi32.dll")] static extern uint EnableTraceEx2(ulong handle,ref Guid provider,uint control,byte level,ulong any,ulong all,uint timeout,ref EnableParameters parameters);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern ulong OpenTraceW(ref Logfile file);
    [DllImport("advapi32.dll")] static extern uint ProcessTrace(ulong[] handles,uint count,IntPtr start,IntPtr end);
    [DllImport("advapi32.dll")] static extern uint CloseTrace(ulong handle);

    internal FpsEtw(string name,HashSet<int> processes=null) {
        if(IntPtr.Size!=8||!name.StartsWith("Aion2Helper-FPS-",StringComparison.Ordinal))throw new ArgumentException("Invalid FPS trace session");
        session=name;callback=Receive;
        byte[] label=System.Text.Encoding.Unicode.GetBytes(name+"\0");
        properties=Marshal.AllocHGlobal(120+label.Length);
        Marshal.Copy(new byte[120+label.Length],0,properties,120+label.Length);
        Marshal.WriteInt32(properties,0,120+label.Length); // WNODE.BufferSize
        Marshal.WriteInt32(properties,40,1); // QPC timestamps
        Marshal.WriteInt32(properties,44,0x20000); // WNODE_FLAG_TRACED_GUID
        Marshal.WriteInt32(properties,48,64); // 64 KB buffers
        Marshal.WriteInt32(properties,52,8);Marshal.WriteInt32(properties,56,32);
        Marshal.WriteInt32(properties,64,0x100); // REAL_TIME only, no file
        Marshal.WriteInt32(properties,68,1); // bounded one-second delivery
        Marshal.WriteInt32(properties,116,120);Marshal.Copy(label,0,IntPtr.Add(properties,120),label.Length);
        IntPtr logger=IntPtr.Zero;
        try {
            Check(StartTraceW(out controller,session,properties));
            if(processes==null){discovery.Update();processes=discovery.Ids;}
            SetProcesses(processes);
            logger=Marshal.StringToHGlobalUni(session);
            var logfile=new Logfile{LoggerName=logger,Mode=0x10001100,Callback=Marshal.GetFunctionPointerForDelegate(callback)}; // RECORD | REAL_TIME | RAW_TIMESTAMP
            consumer=OpenTraceW(ref logfile);if(consumer==ulong.MaxValue)Check((uint)Marshal.GetLastWin32Error());
            ulong traceHandle=consumer;reader=new Thread(delegate(){uint result=ProcessTrace(new[]{traceHandle},1,IntPtr.Zero,IntPtr.Zero);if(result!=0&&result!=1223)Error=result;});reader.IsBackground=true;reader.Start();
        }catch{Dispose();throw;}finally{if(logger!=IntPtr.Zero)Marshal.FreeHGlobal(logger);}
    }
    FpsEtw(){session="";callback=Receive;}
    internal static void Verify(){
        if(Marshal.SizeOf(typeof(Logfile))!=448||Marshal.SizeOf(typeof(Record))!=112||Marshal.SizeOf(typeof(EnableParameters))!=48)throw new Exception("ETW x64 ABI");
        IntPtr data=Marshal.AllocHGlobal(16);
        try{using(var trace=new FpsEtw()){
            trace.gameProcesses=new HashSet<int>{42};
            var r=new Record{Provider=Provider,Pid=42,Thread=7,Data=data,Length=16,Id=178};
            for(int i=0;i<=60;i++){
                Marshal.WriteInt64(data,0,123);Marshal.WriteInt32(data,8,0);Marshal.WriteInt32(data,12,512);
                r.Id=178;r.Time=(long)System.Math.Round(i*Stopwatch.Frequency/60.0);trace.Accept(r);
                Marshal.WriteInt32(data,0,0);r.Id=179;r.Time+=10;trace.Accept(r);trace.Accept(r);
            }
            if(trace.Value!=60)throw new Exception("Successful Present pairs must count once using actual timestamps");
            r.Id=178;r.Time=Stopwatch.Frequency*3;Marshal.WriteInt64(data,0,123);Marshal.WriteInt32(data,12,1);trace.Accept(r);
            r.Id=179;Marshal.WriteInt32(data,0,0);trace.Accept(r);
            r.Id=178;Marshal.WriteInt64(data,0,123);Marshal.WriteInt32(data,12,512);trace.Accept(r);
            r.Id=179;Marshal.WriteInt32(data,0,0x087A0001);trace.Accept(r);
            r.Id=178;Marshal.WriteInt64(data,0,123);trace.Accept(r);
            r.Id=179;Marshal.WriteInt32(data,0,unchecked((int)0x887A000A));trace.Accept(r);
            r.Pid=99;r.Id=178;trace.Accept(r);r.Id=179;Marshal.WriteInt32(data,0,0);trace.Accept(r);
            r.Pid=42;r.Id=178;r.Length=2;trace.Accept(r);r.Id=179;r.Length=4;trace.Accept(r);
            if(trace.Value!=60)throw new Exception("Test, occluded, failed, foreign, truncated and duplicate events must not inflate FPS");
            using(var empty=new FpsEtw())if(empty.Value!=null)throw new Exception("ETW must never invent a game reading");
        }}finally{Marshal.FreeHGlobal(data);}
    }
    static void Check(uint code){if(code!=0)throw new System.ComponentModel.Win32Exception((int)code);}
    internal void UpdateProcesses(){discovery.Update();SetProcesses(discovery.Ids);}
    internal void SetProcesses(HashSet<int> processes){
        if(gameProcesses.SetEquals(processes))return;
        // HashSets are published as immutable snapshots to the ETW callback.
        gameProcesses=processes;if(controller==0)return;
        IntPtr ids=IntPtr.Zero,pids=IntPtr.Zero,descriptors=IntPtr.Zero;
        try{
            ids=Marshal.AllocHGlobal(8);Marshal.Copy(new byte[]{1,0,2,0,178,0,179,0},0,ids,8);
            pids=Marshal.AllocHGlobal(Math.Max(1,processes.Count)*4);int at=0;
            foreach(int pid in processes){if(at==8)break;Marshal.WriteInt32(pids,at++*4,pid);}
            if(at==0){Marshal.WriteInt32(pids,0,0);at=1;}
            descriptors=Marshal.AllocHGlobal(32);
            Marshal.WriteInt64(descriptors,0,ids.ToInt64());Marshal.WriteInt32(descriptors,8,8);Marshal.WriteInt32(descriptors,12,unchecked((int)0x80000200));
            Marshal.WriteInt64(descriptors,16,pids.ToInt64());Marshal.WriteInt32(descriptors,24,at*4);Marshal.WriteInt32(descriptors,28,unchecked((int)0x80000004));
            var parameters=new EnableParameters{Version=2,Filters=descriptors,Count=2};Guid provider=Provider;
            Check(EnableTraceEx2(controller,ref provider,1,5,0x4000000000000000,0,0,ref parameters));
        }finally{if(ids!=IntPtr.Zero)Marshal.FreeHGlobal(ids);if(pids!=IntPtr.Zero)Marshal.FreeHGlobal(pids);if(descriptors!=IntPtr.Zero)Marshal.FreeHGlobal(descriptors);}
    }
    internal int? Value {get{return Error==0?samples.Read():null;}}
    internal static int Live(string root){var result=new System.Text.StringBuilder();int count=0;
        try{using(var trace=new FpsEtw("Aion2Helper-FPS-native-validation-"+Guid.NewGuid().ToString("N"))){var watch=Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<12){trace.UpdateProcesses();int? value=trace.Value;if(value.HasValue)count++;result.AppendLine(watch.Elapsed.TotalSeconds.ToString("F2")+" | "+value+" | error="+trace.Error);Thread.Sleep(250);}}}catch(Exception e){result.AppendLine(e.ToString());}
        System.IO.File.WriteAllText(System.IO.Path.Combine(root,"fps-native-test.txt"),(count>0?"PASS: real DXGI presentations "+count:"NO DATA")+"\r\n"+result);return count>0?0:1;
    }
    void Receive(ref Record record){try{Accept(record);}catch{Error=13;}}
    internal void Accept(Record r){
        if(r.Provider!=Provider||r.Version!=0||!gameProcesses.Contains(r.Pid))return;
        long key=((long)r.Pid<<32)|r.Thread;
        if(r.Id==178){
            int pointerSize=(r.Flags&0x20)!=0?4:8;
            if(r.Length<pointerSize+8)return;
            uint flags=unchecked((uint)Marshal.ReadInt32(r.Data,pointerSize+4));
            if((flags&1)!=0){pending.Remove(key);return;} // DXGI_PRESENT_TEST
            if(pending.Count>=256)pending.Clear();
            pending[key]=new Pending{Pid=r.Pid,Chain=pointerSize==4?unchecked((uint)Marshal.ReadInt32(r.Data)):unchecked((ulong)Marshal.ReadInt64(r.Data)),Time=r.Time};
        }else if(r.Id==179){Pending call;if(!pending.TryGetValue(key,out call))return;pending.Remove(key);
            // Count only successful real presentations; failed/occluded calls
            // and missing Start/Stop pairs must not inflate the result.
            if(r.Length>=4&&Marshal.ReadInt32(r.Data)==0&&r.Time>=call.Time)samples.Add(call.Pid,call.Chain,(double)call.Time/Stopwatch.Frequency);
        }
    }
    public void Dispose(){if(closed)return;closed=true;
        if(controller!=0&&properties!=IntPtr.Zero){ControlTraceW(controller,session,properties,1);controller=0;}
        if(consumer!=ulong.MaxValue){CloseTrace(consumer);consumer=ulong.MaxValue;}
        if(reader!=null)reader.Join(2000);
        if(properties!=IntPtr.Zero){Marshal.FreeHGlobal(properties);properties=IntPtr.Zero;}GC.KeepAlive(callback);
    }
}

// Discovery runs on the measurement worker, never the overlay's UI thread.
internal sealed class FpsGameDiscovery {
    internal HashSet<int> Ids=new HashSet<int>();internal bool HasWindow;long lastScan;
    internal void Update(){long now=Stopwatch.GetTimestamp();if(lastScan!=0&&(now-lastScan)/(double)Stopwatch.Frequency<2)return;lastScan=now;
        var ids=new HashSet<int>();bool window=false;
        foreach(var p in Process.GetProcessesByName("AION2"))try{if(ids.Count<8)ids.Add(p.Id);if(p.MainWindowHandle!=IntPtr.Zero)window=true;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}finally{p.Dispose();}
        HasWindow=window;if(!Ids.SetEquals(ids))Ids=ids;
    }
}
