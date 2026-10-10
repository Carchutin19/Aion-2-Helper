using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

// Local diagnostic recorder. No game memory access, packet sends, or injected code.
internal static class Native {
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] internal static extern bool SetDllDirectory(string path);
    [DllImport("iphlpapi.dll")] internal static extern uint GetExtendedTcpTable(IntPtr p, ref int size, bool order, int family, int tableClass, uint reserved);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_findalldevs(out IntPtr devices, StringBuilder error);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr pcap_lib_version();
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern void pcap_freealldevs(IntPtr devices);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)] internal static extern IntPtr pcap_open_live(string device, int snaplen, int promiscuous, int timeout, StringBuilder error);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_next_ex(IntPtr h, out IntPtr header, out IntPtr data);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_datalink(IntPtr h);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_setnonblock(IntPtr h, int value, StringBuilder error);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr pcap_getevent(IntPtr h);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_setmintocopy(IntPtr h,int bytes);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern void pcap_close(IntPtr h);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)] internal static extern int pcap_compile(IntPtr h, out Filter filter, string text, int optimize, uint mask);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern int pcap_setfilter(IntPtr h, ref Filter filter);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern void pcap_freecode(ref Filter filter);
    [DllImport("wpcap.dll", CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr pcap_geterr(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] internal struct Filter { public uint Length; public IntPtr Instructions; }
    [StructLayout(LayoutKind.Sequential)] internal struct Device { public IntPtr Next, Name, Description, Addresses; public uint Flags; }
}

internal sealed class Segment {
    internal string Src, Dst; internal int SrcPort, DstPort; internal uint Seq; internal byte Flags; internal byte[] Data;
    string key;internal string Key { get { return key??(key=Src+":"+SrcPort+">"+Dst+":"+DstPort); } }
    internal static int U16(byte[] b,int i) { return (b[i]<<8)|b[i+1]; }
    internal static uint U32(byte[] b,int i) { return ((uint)b[i]<<24)|((uint)b[i+1]<<16)|((uint)b[i+2]<<8)|b[i+3]; }
    internal static string Ip(byte[] b,int i,int n) { var x=new byte[n]; Buffer.BlockCopy(b,i,x,0,n); return new IPAddress(x).ToString(); }
    internal static Segment Parse(byte[] b,int link,int length=-1) {
        if(length<0)length=b.Length;if(length>b.Length)return null;
        int ip=0;
        if(link==1) { if(length<14)return null; int type=U16(b,12);ip=14;
            while(type==0x8100||type==0x88a8) {if(length<ip+4)return null;type=U16(b,ip+2);ip+=4;}
            if(type!=0x0800&&type!=0x86dd)return null;
        } else if(link==0||link==108)ip=4; else if(link!=12&&link!=101)return null;
        if(length<ip+20)return null;
        int tcp,end;string src,dst;
        if((b[ip]>>4)==4) {
            int ihl=(b[ip]&15)*4;if(ihl<20||length<ip+ihl||b[ip+9]!=6)return null;
            if((U16(b,ip+6)&0x3fff)!=0)return null;
            end=ip+U16(b,ip+2);tcp=ip+ihl;src=null;dst=null;
        } else if((b[ip]>>4)==6) {
            if(length<ip+40)return null;end=ip+40+U16(b,ip+4);int next=b[ip+6];tcp=ip+40;
            for(int k=0;k<8&&next!=6;k++) {
                if(length<tcp+2)return null;
                if(next==0||next==43||next==60) {int len=(b[tcp+1]+1)*8;next=b[tcp];tcp+=len;}
                else return null; // Fragmented/unsupported headers are counted as unsupported, never guessed.
            }
            if(next!=6)return null;src=null;dst=null;
        } else return null;
        if(end>length||end<tcp+20||length<tcp+20)return null;
        int start=tcp+(b[tcp+12]>>4)*4;if(start<tcp+20||start>end)return null;
        if(start==end)return null; // ACK-only packets do not carry application data.
        bool ipv4=(b[ip]>>4)==4;src=Ip(b,ip+(ipv4?12:8),ipv4?4:16);dst=Ip(b,ip+(ipv4?16:24),ipv4?4:16);
        var payload=new byte[end-start];Buffer.BlockCopy(b,start,payload,0,payload.Length);
        return new Segment {Src=src,Dst=dst,SrcPort=U16(b,tcp),DstPort=U16(b,tcp+2),Seq=U32(b,tcp+4),Flags=b[tcp+13],Data=payload};
    }
}

internal sealed class Recorder:IDisposable {
    readonly object sync=new object(); readonly string root;readonly bool writePackets;
    readonly object lifecycle=new object();readonly ManualResetEvent stopEvent=new ManualResetEvent(true);
    int maintenanceQueued,liveWorkers;long lastDiscoveryTicks;volatile bool requested,disposed;string lastStartError="";
    internal string LastStartError {get{lock(sync){return lastStartError;}}}
    internal Action<Segment,double> OnSegment;
    internal Action OnCaptureStarted;
    Dictionary<string,string> flows=new Dictionary<string,string>(); readonly List<Thread> workers=new List<Thread>();
    StreamWriter writer,marks; volatile bool active; DateTime started; string session;
    long bytes,packets;int openAdapters;string error=""; string processes="Game not detected";
    internal bool Active {get {return active;}}
    internal string Session {get {return session;}}
    internal Recorder(string folder,bool record=true){root=folder;writePackets=record;}
    internal string Status {get {lock(sync){return processes+" | "+flows.Count/2+" connections | "+openAdapters+" adapters\r\n"+packets+" segments / "+(bytes/1024)+" KB"+(error.Length>0?"\r\n"+error:"");}}}
    internal static string Q(string s) {return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    void Error(string s){lock(sync){error=s;}}
    internal void RefreshFlows() {
        var pids=new HashSet<int>();var names=new List<string>();
        int self;using(var current=Process.GetCurrentProcess())self=current.Id;
        var candidates=Process.GetProcessesByName("AION2");if(candidates.Length==0)candidates=Process.GetProcesses();
        foreach(var p in candidates) {try {string name=p.ProcessName;if(p.Id!=self&&name.IndexOf("aion",StringComparison.OrdinalIgnoreCase)>=0){pids.Add(p.Id);names.Add(name+" ("+p.Id+")");}} catch{} finally{p.Dispose();}}
        var updated=new Dictionary<string,string>();
        foreach(int family in new int[]{2,23}) {
            if(pids.Count==0)break;
            int size=0;Native.GetExtendedTcpTable(IntPtr.Zero,ref size,false,family,5,0);if(size<=0)continue;
            IntPtr memory=Marshal.AllocHGlobal(size);
            try {if(Native.GetExtendedTcpTable(memory,ref size,false,family,5,0)!=0)continue;
                byte[] table=new byte[size];Marshal.Copy(memory,table,0,size);int count=BitConverter.ToInt32(table,0);int stride=family==2?24:56;
                for(int n=0;n<count;n++) {int i=4+n*stride;if(i+stride>size)break;
                    int pid=BitConverter.ToInt32(table,i+(family==2?20:52));if(!pids.Contains(pid))continue;
                    int state=BitConverter.ToInt32(table,i+(family==2?0:48));if(state!=5)continue;
                    string local=Segment.Ip(table,i+(family==2?4:0),family==2?4:16);
                    string remote=Segment.Ip(table,i+(family==2?12:24),family==2?4:16);
                    int lp=Segment.U16(table,i+(family==2?8:20)),rp=Segment.U16(table,i+(family==2?16:44));
                    updated[local+":"+lp+">"+remote+":"+rp]="out";updated[remote+":"+rp+">"+local+":"+lp]="in";
                }
            }finally{Marshal.FreeHGlobal(memory);}
        }
        lock(sync){flows=updated;processes=names.Count==0?"Game not detected":String.Join(", ",names.ToArray());}
    }
    bool Tracked(Segment segment){
        lock(sync){if(!active)return false;if(flows.ContainsKey(segment.Key))return true;}
        // A dungeon creates a new TCP connection. Waiting for the normal UI
        // maintenance tick loses its initial roster and character appearance.
        // Confirm ownership through Windows before accepting the first payload;
        // discovery is throttled and the kernel filter remains unchanged.
        if(segment.SrcPort!=13328)return false;long now=DateTime.UtcNow.Ticks,previous=Interlocked.Read(ref lastDiscoveryTicks);
        if(now-previous<TimeSpan.TicksPerMillisecond*250||Interlocked.CompareExchange(ref lastDiscoveryTicks,now,previous)!=previous)return false;
        if(!active||disposed)return false;RefreshFlows();
        lock(sync)return active&&flows.ContainsKey(segment.Key);
    }
    internal void Start(){lock(lifecycle){if(disposed)throw new ObjectDisposedException("Recorder");StartCore();}}
    internal void MaintainAsync(bool enabled){
        if(disposed)return;requested=enabled;if(Interlocked.CompareExchange(ref maintenanceQueued,1,0)!=0)return;
        ThreadPool.QueueUserWorkItem(delegate{try{lock(lifecycle){
            if(disposed)return;if(!requested)StopCore();else {if(active)RefreshFlows();else {StopCore();StartCore();}if(!requested)StopCore();}
            lock(sync){lastStartError="";}
        }}catch(Exception ex){lock(sync){lastStartError=ex.Message;}}finally{Interlocked.Exchange(ref maintenanceQueued,0);}});
    }
    void StartCore() {
        if(active)return;NpcapSupport.Current.EnsureAvailable();RefreshFlows();
        lock(sync){if(flows.Count==0)throw new Exception("Enter the game with your character first: no Aion connections detected.");}
        Native.SetDllDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"Npcap"));
        var err=new StringBuilder(512);IntPtr head;
        if(Native.pcap_findalldevs(out head,err)!=0)throw new Exception(err.ToString());
        var devices=new List<string>();
        try {for(IntPtr ptr=head;ptr!=IntPtr.Zero;){var d=(Native.Device)Marshal.PtrToStructure(ptr,typeof(Native.Device));devices.Add(Marshal.PtrToStringAnsi(d.Name));ptr=d.Next;}} finally {Native.pcap_freealldevs(head);}
        if(devices.Count==0)throw new Exception("Npcap returned no adapters.");
        if(writePackets){session=Path.Combine(root,"captures",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(session);
        writer=new StreamWriter(Path.Combine(session,"segments.jsonl"),false,new UTF8Encoding(false));writer.AutoFlush=true;
        marks=new StreamWriter(Path.Combine(session,"markers.jsonl"),false,new UTF8Encoding(false));marks.AutoFlush=true;}
        started=DateTime.UtcNow;bytes=packets=0;error="";openAdapters=0;liveWorkers=devices.Count;stopEvent.Reset();if(OnCaptureStarted!=null)OnCaptureStarted();active=true;Mark("START");
        foreach(string name in devices) {string captured=name;var t=new Thread(delegate(){Capture(captured);});t.IsBackground=true;workers.Add(t);t.Start();}
    }
    void Capture(string device) {
        IntPtr h=IntPtr.Zero;EventWaitHandle packetEvent=null;bool opened=false;
        try {
            var err=new StringBuilder(512);h=Native.pcap_open_live(device,65535,0,100,err);
            if(h==IntPtr.Zero){Error("Could not open an adapter: "+err);return;}
            Native.Filter f;int compiled=Native.pcap_compile(h,out f,writePackets?"tcp":"tcp src port 13328",1,0xffffffff);
            if(compiled!=0)throw new Exception("Npcap filter: "+Marshal.PtrToStringAnsi(Native.pcap_geterr(h)));
            try {if(Native.pcap_setfilter(h,ref f)!=0)throw new Exception("Could not apply the TCP filter.");}finally{Native.pcap_freecode(ref f);}
            if(Native.pcap_setnonblock(h,1,err)!=0)throw new Exception("Non-blocking mode: "+err);
            try{if(Native.pcap_setmintocopy(h,1)==0){IntPtr handle=Native.pcap_getevent(h);if(handle!=IntPtr.Zero){packetEvent=new EventWaitHandle(false,EventResetMode.ManualReset);packetEvent.SafeWaitHandle=new SafeWaitHandle(handle,false);}}}catch(EntryPointNotFoundException){}
            var waits=packetEvent==null?null:new WaitHandle[]{stopEvent,packetEvent};var raw=new byte[65535];
            int link=Native.pcap_datalink(h);lock(sync){openAdapters++;opened=true;}
            while(active) {
                IntPtr hp,dp;int result=Native.pcap_next_ex(h,out hp,out dp);if(result==0){if(waits==null)stopEvent.WaitOne(15);else WaitHandle.WaitAny(waits,1000);continue;}if(result<0){Error("Capture stopped on an adapter: "+Marshal.PtrToStringAnsi(Native.pcap_geterr(h)));break;}
                // Windows timeval uses 32-bit longs, including on x64.
                int caplen=Marshal.ReadInt32(hp,8);if(caplen<0||caplen>65535)continue;
                Marshal.Copy(dp,raw,0,caplen);var s=Segment.Parse(raw,link,caplen);if(s==null)continue;
                if(!Tracked(s))continue;
                double ts=(uint)Marshal.ReadInt32(hp,0)+(uint)Marshal.ReadInt32(hp,4)/1000000.0;
                Action<Segment,double> handler;
                lock(sync) {string direction;if(!active||!flows.TryGetValue(s.Key,out direction))continue;
                    if(writer!=null){string hex=BitConverter.ToString(s.Data).Replace("-","");
                    writer.WriteLine("{\"ts\":"+ts.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+",\"stream\":"+Q(s.Key)+",\"direction\":"+Q(direction)+",\"adapter\":"+Q(device)+",\"seq\":"+s.Seq+",\"flags\":"+s.Flags+",\"hex\":"+Q(hex)+"}");}
                    handler=OnSegment;
                    bytes+=s.Data.Length;packets++;
                }
                if(handler!=null)handler(s,ts); // Decoder work never blocks flow refresh/status readers.
            }
        }catch(Exception ex){Error(ex.Message);}finally{if(packetEvent!=null)packetEvent.Dispose();if(h!=IntPtr.Zero)Native.pcap_close(h);if(opened)lock(sync){openAdapters--;}if(Interlocked.Decrement(ref liveWorkers)==0)active=false;}
    }
    internal void Mark(string name) {lock(sync){if(!active||marks==null)return;double ts=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;marks.WriteLine("{\"ts\":"+ts.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+",\"event\":"+Q(name)+"}");}}
    internal void Stop(){requested=false;lock(lifecycle){StopCore();}}
    public void Dispose(){requested=false;lock(lifecycle){if(disposed)return;disposed=true;StopCore();stopEvent.Dispose();}}
    void StopCore() {
        if(!active&&workers.Count==0)return;Mark("STOP");active=false;stopEvent.Set();
        foreach(var t in workers)t.Join(1500);workers.Clear();
        lock(sync){if(writer!=null){writer.Dispose();marks.Dispose();writer=null;marks=null;File.WriteAllText(Path.Combine(session,"summary.txt"),"Aion 2 Helper - Signal Probe\r\nStarted UTC: "+started.ToString("O")+"\r\n"+Status+"\r\nOnly TCP connections of processes with Aion in their name. No data is uploaded.\r\n",Encoding.UTF8);}}
    }
    internal void Tick(){RefreshFlows();if(writePackets&&active&&((DateTime.UtcNow-started).TotalSeconds>=180||bytes>=50*1024*1024))Stop();}
}

internal sealed class GuideWindow:Form {
    readonly Label instruction=new Label(); readonly Label detail=new Label();
    internal GuideWindow() {
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(18,22,30);ForeColor=Color.White;Opacity=0.95;Size=new Size(600,102);
        var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Left+(area.Width-Width)/2,area.Top+24);
        instruction.SetBounds(12,12,576,40);instruction.TextAlign=ContentAlignment.MiddleCenter;instruction.Font=new Font("Segoe UI",19,FontStyle.Bold);Controls.Add(instruction);
        detail.SetBounds(12,58,576,30);detail.TextAlign=ContentAlignment.MiddleCenter;detail.Font=new Font("Segoe UI",11);Controls.Add(detail);
    }
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get {var cp=base.CreateParams;cp.ExStyle|=0x08000000|0x20;return cp;}}
    internal void SetInstruction(string text,string sub){instruction.Text=text;detail.Text=sub;}
}

internal sealed class ProbeWindow:Form {
    readonly Recorder recorder;readonly Label status=new Label();readonly Label message=new Label();readonly Button start=new Button();readonly Button stop=new Button();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    readonly List<int> registered=new List<int>();
    GuideWindow guideWindow;DateTime guideStart;bool guided;int lastStage=-1;
    internal ProbeWindow(string root) {
        recorder=new Recorder(root);Text="Aion 2 Helper - Signal Probe";Size=new Size(620,440);MinimumSize=Size;
        BackColor=Color.FromArgb(18,22,30);ForeColor=Color.White;Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
        var title=new Label{Text="Inspect dash energy",Font=new Font("Segoe UI",18,FontStyle.Bold),AutoSize=true,Location=new Point(22,18)};Controls.Add(title);
        var guide=new Label{Text="1. Enter a quiet area with your character.\r\n2. Show the in-game energy bar and wait until it is full.\r\n3. Click START TEST and return to the game with Alt+Tab.\r\n4. Follow the prompts: wait / dash once / drain energy.\r\n\r\nThe test ends automatically after 90 seconds. F9 cancels it.",AutoSize=false,Size=new Size(570,130),Location=new Point(22,68)};Controls.Add(guide);
        start.Text="START TEST";start.SetBounds(22,207,175,38);start.Click+=delegate{try{recorder.Start();guideStart=DateTime.UtcNow;guided=true;lastStage=-1;guideWindow=new GuideWindow();guideWindow.Show();message.Text="Return to Aion with Alt+Tab. Follow the on-screen prompts.";}catch(Exception ex){message.Text=ex.Message;}};Controls.Add(start);
        stop.Text="Stop (F9)";stop.SetBounds(207,207,175,38);stop.Click+=delegate{StopCapture();};Controls.Add(stop);
        var note=new Label{Text="Follow the dash prompts. You do not need to press F6, F7, or F8.",AutoSize=true,Location=new Point(22,265)};Controls.Add(note);
        status.SetBounds(22,302,570,58);Controls.Add(status);message.SetBounds(22,365,570,48);message.Font=new Font("Segoe UI",9);message.Text="Diagnostic capture tool, separate from the live energy overlay.";Controls.Add(message);
        timer.Interval=200;int ticks=0;timer.Tick+=delegate{try{if(++ticks%10==0)recorder.Tick();GuideTick();status.Text=recorder.Status;start.Enabled=!recorder.Active;stop.Enabled=recorder.Active;}catch(Exception ex){message.Text=ex.Message;}};timer.Start();
        Shown+=delegate{if(Native.RegisterHotKey(Handle,4,0x4000,(uint)Keys.F9))registered.Add(4);else message.Text="F9 is already in use: click Stop to cancel.";};
        FormClosing+=delegate{timer.Stop();StopCapture();recorder.Dispose();timer.Dispose();foreach(int id in registered)Native.UnregisterHotKey(Handle,id);};
    }
    void Mark(string name){if(!recorder.Active)return;recorder.Mark(name);message.Text="Marker: "+name+" · "+DateTime.Now.ToString("HH:mm:ss");}
    void StopCapture(){guided=false;if(guideWindow!=null){guideWindow.Close();guideWindow=null;}recorder.Stop();if(recorder.Session!=null)message.Text="Test finished. Saved to captures\\"+Path.GetFileName(recorder.Session);}
    void GuideTick() {
        if(!guided)return;if(!recorder.Active){StopCapture();return;}
        int sec=(int)(DateTime.UtcNow-guideStart).TotalSeconds;if(sec>=90){recorder.Mark("GUIDE_END");StopCapture();System.Media.SystemSounds.Asterisk.Play();return;}
        int stage=sec<10?0:sec<12?1:sec<25?2:sec<27?3:sec<40?4:sec<42?5:sec<55?6:sec<61?7:8;
        if(stage!=lastStage){lastStage=stage;string[] markers={"PREPARE_PROMPT","DASH_PROMPT","RECOVER_PROMPT","DASH_PROMPT","RECOVER_PROMPT","DASH_PROMPT","RECOVER_PROMPT","EMPTY_PROMPT","RECOVER_PROMPT"};recorder.Mark(markers[stage]);if(stage==1||stage==3||stage==5||stage==7)System.Media.SystemSounds.Asterisk.Play();}
        string text=stage==0?"Return to Aion with Alt+Tab":stage==1||stage==3||stage==5?"Dash ONCE now":stage==7?"Dash, then sprint to drain your energy":"Wait. Do not dash.";
        guideWindow.SetInstruction(text,"Signal test · "+(90-sec)+" s remaining · F9 to cancel");
    }
    protected override void WndProc(ref Message m){if(m.Msg==0x0312){switch(m.WParam.ToInt32()){case 1:Mark("FULL");break;case 2:Mark("DASH");break;case 3:Mark("EMPTY");break;case 4:StopCapture();break;}}base.WndProc(ref m);}
}

internal static class Entry {
    static void Assert(bool value,string message) {if(!value)throw new Exception(message);}
    static void SelfTest(string root) {
        var b=new byte[58];b[12]=8;b[14]=0x45;b[16]=0;b[17]=44;b[23]=6;
        b[26]=127;b[29]=1;b[30]=10;b[33]=2;b[34]=0xc0;b[35]=0x01;b[36]=0x34;b[37]=0x10;
        b[41]=123;b[46]=0x50;b[47]=0x18;b[54]=1;b[55]=2;b[56]=3;b[57]=4;
        var s=Segment.Parse(b,1);Assert(s!=null&&s.Src=="127.0.0.1"&&s.Dst=="10.0.0.2","IPv4 addresses");
        Assert(s.SrcPort==49153&&s.DstPort==13328&&s.Seq==123&&s.Data.Length==4&&s.Data[3]==4,"TCP payload / ports / sequence");
        Assert(Segment.Parse(new byte[3],1)==null,"Truncated ethernet");b[20]=0x20;Assert(Segment.Parse(b,1)==null,"Reject IP fragments");b[20]=0;
        b[17]=100;Assert(Segment.Parse(b,1)==null,"Reject truncated payload");b[17]=44;
        var reused=new byte[65535];Buffer.BlockCopy(b,0,reused,0,b.Length);Assert(Segment.Parse(reused,1,57)==null,"Reusable packet buffer must respect actual captured length");Assert(Segment.Parse(reused,1,58).Data.Length==4,"Reusable packet buffer must ignore stale tail bytes");
        byte[] v6=new byte[65];v6[0]=0x60;v6[5]=25;v6[6]=6;v6[23]=1;v6[39]=2;v6[40]=1;v6[43]=2;v6[52]=0x50;v6[64]=42;
        var six=Segment.Parse(v6,12);Assert(six!=null&&six.Src=="::1"&&six.Dst=="::2"&&six.Data.Length==5&&six.Data[4]==42,"IPv6 raw packet");
        NpcapSupport.Current.EnsureAvailable();
        DashSignal.VerifyProtocol(root);
        var err=new StringBuilder(512);IntPtr head;Assert(Native.pcap_findalldevs(out head,err)==0,"Npcap enumeration: "+err);int devices=0,opened=0,events=0;
        try {for(IntPtr ptr=head;ptr!=IntPtr.Zero;){var d=(Native.Device)Marshal.PtrToStructure(ptr,typeof(Native.Device));devices++;string name=Marshal.PtrToStringAnsi(d.Name);
            IntPtr h=Native.pcap_open_live(name,65535,0,100,err);if(h!=IntPtr.Zero){try{Assert(Native.pcap_setnonblock(h,1,err)==0,"Npcap nonblock");Native.Filter f;Assert(Native.pcap_compile(h,out f,"tcp src port 13328",1,0xffffffff)==0,"BPF compile");try{Assert(Native.pcap_setfilter(h,ref f)==0,"BPF apply");}finally{Native.pcap_freecode(ref f);}if(Native.pcap_setmintocopy(h,1)==0&&Native.pcap_getevent(h)!=IntPtr.Zero)events++;opened++;}finally{Native.pcap_close(h);}}ptr=d.Next;}}
        finally{Native.pcap_freealldevs(head);}
        Assert(opened>0,"Npcap adapters cannot be opened: "+err);
        File.WriteAllText(Path.Combine(root,"selftest.txt"),"PASS: TCP IPv4/IPv6 and reusable buffer bounds; out-of-order/retransmitted/wrapped TCP; zero-copy frame decoding; LZ4 literals/overlap/bounds; Npcap game-only BPF and event-driven capture.\r\nAdapters: "+devices+"; opened: "+opened+"; native events: "+events,Encoding.UTF8);
    }
    [STAThread] static int Main(string[] args) {
        var english=System.Globalization.CultureInfo.GetCultureInfo("en-US");Thread.CurrentThread.CurrentCulture=english;Thread.CurrentThread.CurrentUICulture=english;
        string root=AppDomain.CurrentDomain.BaseDirectory;
        if(args.Length>0&&args[0]=="--dps-test"){try{DpsVerification.Run(root);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"dps-test.txt"),"FAIL: "+ex);return 1;}}
        if(args.Length>1&&args[0]=="--dps-replay-test"){try{DpsVerification.Replay(root,args[1]);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"dps-replay-test.txt"),"FAIL: "+ex);return 1;}}
        if(args.Length>0&&args[0]=="--fps-refresh-test")return FpsVerification.VerifyRefresh(root);
        if(args.Length>0&&args[0]=="--fps-standby-test")return FpsVerification.VerifyStandby(root);
        if(args.Length>0&&args[0]=="--fps-live-test")return FpsVerification.Live(root);
        if(args.Length>0&&args[0]=="--fps-native-test")return FpsEtw.Live(root);
        if(args.Length>0&&args[0]=="--fps-worker")return FpsWorker.Run(root,args);
        if(args.Length>0&&args[0]=="--fps-test"){try{FpsSamples.Verify();FpsVerification.Run(root);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"fps-test.txt"),"FAIL: "+ex);return 1;}}
        if(args.Length>0&&args[0]=="--settings-test") {try{SettingsVisual.Verify(root);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"settings-test.txt"),"FAIL: "+ex);return 1;}}
        if(args.Length>0&&args[0]=="--ui-test") {try{BarDesign.Verify(root);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"ui-test.txt"),"FAIL: "+ex);return 1;}}
        if(args.Length>1&&args[0]=="--replay-test") {try{DashSignal.ReplayTest(root,args[1]);return 0;}catch(Exception ex){File.AppendAllText(Path.Combine(root,"replay-test.txt"),"\r\nFAIL: "+ex);return 1;}}
        if(args.Length>0&&args[0]=="--selftest") {try{SelfTest(root);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(root,"selftest.txt"),"FAIL: "+ex,Encoding.UTF8);return 1;}}
        if(args.Length>0&&args[0]=="--diagnose") {
            var r=new Recorder(root);r.RefreshFlows();File.WriteAllText(Path.Combine(root,"diagnostic.txt"),r.Status,Encoding.UTF8);return 0;
        }
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0&&args[0]=="--ui-preview"){Application.Run(new EnergyOverlay(root,true));return 0;}
        if(args.Length>0&&args[0]=="--fps-settings"){var overlay=new EnergyOverlay(root);overlay.Shown+=delegate{overlay.BeginInvoke(new Action(overlay.OpenFpsSettings));};Application.Run(overlay);return 0;}
        if(args.Length>0&&args[0]=="--dps-settings"){var overlay=new EnergyOverlay(root);overlay.Shown+=delegate{overlay.BeginInvoke(new Action(overlay.OpenDpsSettings));};Application.Run(overlay);return 0;}
        if(args.Length>0&&args[0]=="--dps-history"){var overlay=new EnergyOverlay(root);overlay.Shown+=delegate{overlay.BeginInvoke(new Action(overlay.OpenDpsHistory));};Application.Run(overlay);return 0;}
        if(args.Length>0&&args[0]=="--settings"){var overlay=new EnergyOverlay(root);overlay.Shown+=delegate{overlay.BeginInvoke(new Action(overlay.OpenSettings));};Application.Run(overlay);return 0;}
        string appName=Path.GetFileNameWithoutExtension(Application.ExecutablePath);
        if((args.Length>0&&args[0]=="--overlay")||appName.Equals("AionDash",StringComparison.OrdinalIgnoreCase)||appName.Equals("Aion2Helper",StringComparison.OrdinalIgnoreCase))Application.Run(new EnergyOverlay(root));else Application.Run(new ProbeWindow(root));return 0;
    }
}
