using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;

internal sealed class FpsOptions {
    public bool Enabled=false,Background=true;
    public string Color="#00FF63";
    public int BackgroundOpacity=130,Softness=8,RefreshIntervalMs=250;
    public Rectangle Bounds=new Rectangle(30,30,112,44);
    internal FpsOptions Copy(){return (FpsOptions)MemberwiseClone();}
    internal static int NormalizeInterval(int value){return Math.Max(100,Math.Min(2000,value));}
    internal void Normalize(){RefreshIntervalMs=NormalizeInterval(RefreshIntervalMs);if(!EnergyBarOptions.ValidColor(Color))Color="#00FF63";BackgroundOpacity=Math.Max(0,Math.Min(255,BackgroundOpacity));Softness=Math.Max(0,Math.Min(24,Softness));Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(48,Math.Min(1600,Bounds.Width)),Math.Max(20,Math.Min(400,Bounds.Height)));}
    internal static FpsOptions Read(Dictionary<string,object> cfg){object data;var o=new FpsOptions();if(cfg.TryGetValue("fps",out data))try{o=new JavaScriptSerializer().ConvertToType<FpsOptions>(data)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(FpsOptions a,FpsOptions b){return a.Enabled==b.Enabled&&a.Background==b.Background&&a.Bounds==b.Bounds&&a.BackgroundOpacity==b.BackgroundOpacity&&a.Softness==b.Softness&&a.RefreshIntervalMs==b.RefreshIntervalMs&&string.Equals(a.Color,b.Color,StringComparison.OrdinalIgnoreCase);}
}

// A bounded one-second window per swap chain; never sum multiple game surfaces.
internal sealed class FpsSamples {
    sealed class Chain {internal readonly Queue<double> Times=new Queue<double>();internal double Last=-1;internal long Arrival;}
    readonly Dictionary<string,Chain> chains=new Dictionary<string,Chain>();readonly object gate=new object();
    int pidIndex=-1,chainIndex=-1,timeIndex=-1;readonly int processId;
    internal FpsSamples(int pid){processId=pid;}
    internal void Consume(string line){
        if(string.IsNullOrEmpty(line)||line.Length>8192)return;
        string[] fields=Csv(line);
        if(fields.Length>0&&fields[0].Trim('\uFEFF')=="Application") {pidIndex=Array.IndexOf(fields,"ProcessID");chainIndex=Array.IndexOf(fields,"SwapChainAddress");timeIndex=Array.IndexOf(fields,"TimeInSeconds");return;}
        if(pidIndex<0||chainIndex<0||timeIndex<0||fields.Length<=Math.Max(timeIndex,Math.Max(pidIndex,chainIndex)))return;
        int pid;double time;if(!int.TryParse(fields[pidIndex],out pid)||(processId>0?pid!=processId:!string.Equals(fields[0],"Aion2.exe",StringComparison.OrdinalIgnoreCase))||!double.TryParse(fields[timeIndex],NumberStyles.Float,CultureInfo.InvariantCulture,out time)||double.IsNaN(time)||double.IsInfinity(time)||time<0)return;
        Add(pid,fields[chainIndex],time);
    }
    internal void Add(int pid,string surface,double time){
        if(pid<=0||double.IsNaN(time)||double.IsInfinity(time)||time<0)return;
        lock(gate){string key=pid+"/"+surface;Chain chain;if(!chains.TryGetValue(key,out chain)){if(chains.Count>=8){string oldest=null;long stamp=long.MaxValue;foreach(var pair in chains)if(pair.Value.Arrival<stamp){oldest=pair.Key;stamp=pair.Value.Arrival;}chains.Remove(oldest);}chain=new Chain();chains.Add(key,chain);}
            if(time<=chain.Last)return;chain.Last=time;chain.Arrival=Stopwatch.GetTimestamp();chain.Times.Enqueue(time);
            while(chain.Times.Count>4096||(chain.Times.Count>1&&time-chain.Times.Peek()>1))chain.Times.Dequeue();
        }
    }
    internal int? Read(){lock(gate){Chain best=null;long now=Stopwatch.GetTimestamp();foreach(var chain in chains.Values)if((now-chain.Arrival)/(double)Stopwatch.Frequency<=2&&(best==null||chain.Times.Count>best.Times.Count))best=chain;
        if(best==null||best.Times.Count<2)return null;double duration=best.Last-best.Times.Peek();if(duration<=0)return null;return Math.Max(1,Math.Min(9999,(int)Math.Round((best.Times.Count-1)/duration)));}}
    internal static string[] Csv(string line){var result=new List<string>();var cell=new System.Text.StringBuilder();bool quoted=false;for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){result.Add(cell.ToString());cell.Length=0;}else cell.Append(c);}result.Add(cell.ToString());return result.ToArray();}
    internal static void Verify(){var s=new FpsSamples(42);s.Consume("Application,ProcessID,SwapChainAddress,TimeInSeconds");for(int i=0;i<=120;i++)s.Consume("\"AION,2.exe\",42,0x1,"+(i/120.0).ToString("R",CultureInfo.InvariantCulture));if(s.Read()!=120)throw new Exception("Actual presentation timestamps must give 120 FPS");for(int i=0;i<=30;i++)s.Consume("AION2.exe,42,0x2,"+(i/30.0).ToString("R",CultureInfo.InvariantCulture));if(s.Read()!=120)throw new Exception("Swap chains must not be added together");s.Consume("Other.exe,43,0x1,10");s.Consume("AION2.exe,42,0x1,NaN");s.Consume("AION2.exe,42,0x1,0.1");if(s.Read()!=120)throw new Exception("Foreign, invalid and out-of-order rows must be ignored");s.Consume("AION2.exe,42,0x1,100");if(s.Read()!=30)throw new Exception("A long pause must discard old frames");var empty=new FpsSamples(42);if(empty.Read()!=null)throw new Exception("No frames must not invent a reading");var names=new FpsSamples(0);names.Consume("Application,ProcessID,SwapChainAddress,TimeInSeconds");for(int i=0;i<=60;i++)names.Consume("AION2.exe,42,0x1,"+(i/60.0).ToString("R",CultureInfo.InvariantCulture));for(int i=0;i<=30;i++)names.Consume("Aion2.exe,84,0x1,"+(i/30.0).ToString("R",CultureInfo.InvariantCulture));names.Consume("Other.exe,99,0x1,10");if(names.Read()!=60)throw new Exception("Pre-armed name filtering must isolate game processes with matching swap-chain addresses");}
}

// The worker is the only process which may request elevation. Parent and worker
// communicate through a user-restricted pipe; no CSV files or frame logs on disk.
internal static class FpsWorker {
    static void StopCapture(Process capture,string tool,string session,string root){
        if(capture==null)return;
        try{if(!capture.HasExited){using(var stop=Process.Start(new ProcessStartInfo(tool,"--session_name "+session+" --terminate_existing_session"){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=root})){if(!stop.WaitForExit(2000))stop.Kill();}if(!capture.WaitForExit(2000))capture.Kill();}}catch{}finally{capture.Dispose();}
    }
    internal static int Run(string root,string[] args){
        if(args.Length!=5&&args.Length!=6)return 2;int refresh=250;if(args.Length==6&&!int.TryParse(args[5],out refresh))return 2;refresh=FpsOptions.NormalizeInterval(refresh);int parentId,targetId;if(!int.TryParse(args[1],out parentId)||!int.TryParse(args[2],out targetId)||!args[3].StartsWith("Aion2Helper-FPS-",StringComparison.Ordinal)||args[4].Length!=32)return 2;
        string tool=Path.Combine(root,"tools","presentmon","PresentMon.exe"),session=args[3];Process capture=null;FpsEtw native=null;
        using(var wake=new AutoResetEvent(false))using(var pipe=new NamedPipeClientStream(".",session,PipeDirection.InOut,PipeOptions.Asynchronous))try{
            pipe.Connect(15000);using(var writer=new StreamWriter(pipe,System.Text.Encoding.UTF8,1024,true)){writer.AutoFlush=true;writer.WriteLine(args[4]);
                var commandReader=new StreamReader(pipe,System.Text.Encoding.UTF8,false,1024,true);int stop=0;
                var watcher=new Thread(delegate(){try{string line;while((line=commandReader.ReadLine())!=null&&line!="STOP"){int value;if(line.StartsWith("INTERVAL ",StringComparison.Ordinal)&&int.TryParse(line.Substring(9),out value)){Interlocked.Exchange(ref refresh,FpsOptions.NormalizeInterval(value));wake.Set();}}}catch{}Interlocked.Exchange(ref stop,1);try{wake.Set();}catch(ObjectDisposedException){}});watcher.IsBackground=true;watcher.Start();
                uint nativeError=0;try{native=new FpsEtw(session+"-DXGI");}catch(System.ComponentModel.Win32Exception e){nativeError=(uint)e.NativeErrorCode;}
                string error="";var samples=new FpsSamples(0);bool attempted=false;var watch=Stopwatch.StartNew();
                using(var parent=Process.GetProcessById(parentId))while(Interlocked.CompareExchange(ref stop,0,0)==0&&!parent.HasExited){
                    if(native!=null)native.UpdateProcesses();int? direct=native==null?null:native.Value;int? fps=direct??samples.Read();
                    if(direct.HasValue&&capture!=null){StopCapture(capture,tool,session,root);capture=null;attempted=false;}
                    // Most machines can use this small DXGI trace alone. Keep
                    // PresentMon as a fallback for systems without Logging events.
                    if(!fps.HasValue&&capture==null&&!attempted&&watch.Elapsed.TotalSeconds>=4&&File.Exists(tool)){
                        attempted=true;capture=new Process{StartInfo=new ProcessStartInfo(tool,"--process_name Aion2.exe --session_name "+session+" --output_stdout --no_console_stats --v1_metrics --no_track_gpu --no_track_input --no_track_display"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=root}};
                        capture.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)samples.Consume(e.Data);};capture.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null){lock(samples){error=e.Data;}}};capture.Start();capture.BeginOutputReadLine();capture.BeginErrorReadLine();
                    }
                    string status="WAIT";if(fps.HasValue)status="FPS "+fps.Value;else if(nativeError==5)status="ADMIN";else if(native!=null&&native.Error!=0)status="ERROR Native trace "+native.Error;else if(capture!=null&&capture.HasExited){string message;lock(samples){message=error;}status=message.IndexOf("denied",StringComparison.OrdinalIgnoreCase)>=0?"ADMIN":"ERROR "+message.Replace('\r',' ').Replace('\n',' ');}
                    writer.WriteLine(status);wake.WaitOne(Interlocked.CompareExchange(ref refresh,0,0));
                }
            }
        }catch(IOException){return 0;}catch{return 1;}finally{if(native!=null)native.Dispose();StopCapture(capture,tool,session,root);}return 0;
    }
}

internal sealed class FpsMonitor:IDisposable {
    readonly string root;readonly object gate=new object();NamedPipeServerStream pipe;StreamWriter commands;Process worker;Thread listener;int targetId,refreshInterval=250;long lastMessage;int? fps;bool enabled,closed,blocked;double lastPoll=-2;readonly Stopwatch clock=Stopwatch.StartNew();
    internal string Status="FPS disabled";
    internal FpsMonitor(string folder){root=folder;}
    internal bool NeedsAdministrator {get{lock(gate)return enabled&&Status!="PresentMon is missing"&&!Value.HasValue;}}
    internal int? Value {get{lock(gate)return (Stopwatch.GetTimestamp()-lastMessage)/(double)Stopwatch.Frequency>Math.Max(2,refreshInterval*3/1000.0)?null:fps;}}
    internal void Configure(bool active,int interval=250){interval=FpsOptions.NormalizeInterval(interval);lock(gate){if(refreshInterval!=interval){refreshInterval=interval;try{if(commands!=null)commands.WriteLine("INTERVAL "+interval);}catch(IOException){}}}if(enabled==active)return;enabled=active;if(!active){Stop();Status="FPS disabled";}else{blocked=false;lastPoll=-2;Status="Waiting for Aion 2";}}
    static int GameProcess(){var candidates=Process.GetProcessesByName("AION2");int found=0;foreach(var p in candidates)try{if(found==0)found=p.Id;if(p.MainWindowHandle!=IntPtr.Zero)found=p.Id;}catch{}finally{p.Dispose();}return found;}
    internal void Tick(){if(!enabled||closed||clock.Elapsed.TotalSeconds-lastPoll<2)return;lastPoll=clock.Elapsed.TotalSeconds;targetId=GameProcess();if(worker==null&&!blocked)Start(false);if(targetId==0&&Status=="Waiting for game frames")Status="Waiting for Aion 2";}
    internal void AllowAdministrator(){if(!enabled||closed)return;Stop();targetId=GameProcess();blocked=false;Start(true);}
    void Start(bool elevated){

        string name="Aion2Helper-FPS-"+Guid.NewGuid().ToString("N"),token=Guid.NewGuid().ToString("N");
        var acl=new PipeSecurity();acl.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));acl.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid,null),PipeAccessRights.FullControl,AccessControlType.Allow));
        var connection=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,4096,4096,acl);pipe=connection;
        int ownId;using(var own=Process.GetCurrentProcess())ownId=own.Id;
        var start=new ProcessStartInfo(Application.ExecutablePath,"--fps-worker "+ownId+" "+targetId+" "+name+" "+token+" "+refreshInterval){WorkingDirectory=root,UseShellExecute=elevated,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};if(elevated)start.Verb="runas";
        try{worker=Process.Start(start);Status="Starting FPS measurement";blocked=true;
            listener=new Thread(delegate(){try{var pending=connection.BeginWaitForConnection(null,null);if(!pending.AsyncWaitHandle.WaitOne(20000))return;connection.EndWaitForConnection(pending);pending.AsyncWaitHandle.Close();var reader=new StreamReader(connection);string handshake=reader.ReadLine();if(handshake!=token)throw new IOException("FPS handshake mismatch (received "+(handshake==null?-1:handshake.Length)+" characters)");lock(gate){if(pipe!=connection)return;commands=new StreamWriter(connection){AutoFlush=true};commands.WriteLine("INTERVAL "+refreshInterval);}
                string line;while((line=reader.ReadLine())!=null){lock(gate){if(pipe!=connection)return;int value;if(line.StartsWith("FPS ",StringComparison.Ordinal)&&int.TryParse(line.Substring(4),out value)){fps=value;lastMessage=Stopwatch.GetTimestamp();Status="Measuring game FPS";}else{fps=null;if(line=="ADMIN")Status="FPS measurement needs permission";else if(line.StartsWith("ERROR",StringComparison.Ordinal))Status="FPS measurement unavailable";else Status=targetId==0?"Waiting for Aion 2":"Waiting for game frames";}}}
            }catch{}finally{lock(gate){if(pipe==connection){fps=null;Status="FPS measurement unavailable";}}}});listener.IsBackground=true;listener.Start();
        }catch(System.ComponentModel.Win32Exception){connection.Dispose();pipe=null;Status=elevated?"FPS permission request cancelled":"FPS measurement unavailable";blocked=true;}
    }
    void Stop(){NamedPipeServerStream old;Process process;lock(gate){old=pipe;pipe=null;process=worker;worker=null;try{if(commands!=null)commands.WriteLine("STOP");}catch{}commands=null;fps=null;}if(old!=null)old.Dispose();if(process!=null){ThreadPool.QueueUserWorkItem(delegate{try{process.WaitForExit(6000);}catch{}finally{process.Dispose();}});}targetId=0;}
    public void Dispose(){closed=true;Stop();}
}

internal static class FpsDesign {
    internal static Rectangle ResizeBounds(Rectangle start,Point delta,int hit){bool left=hit==10||hit==13||hit==16,right=hit==11||hit==14||hit==17,top=hit==12||hit==13||hit==14,bottom=hit==15||hit==16||hit==17;int w=Math.Max(48,Math.Min(1600,start.Width+(left?-delta.X:right?delta.X:0))),h=Math.Max(20,Math.Min(400,start.Height+(top?-delta.Y:bottom?delta.Y:0)));return new Rectangle(left?start.Right-w:start.X,top?start.Bottom-h:start.Y,w,h);}
    static GraphicsPath Round(float x,float y,float w,float h,float r){var p=new GraphicsPath();r=Math.Min(r,Math.Min(w,h)/2);if(r<=0){p.AddRectangle(new RectangleF(x,y,w,h));return p;}float d=r*2;p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;}
    internal static Bitmap Render(int width,int height,string text,FpsOptions o,bool editing){var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(bitmap)){g.Clear(System.Drawing.Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        if(editing)using(var b=new SolidBrush(System.Drawing.Color.FromArgb(1,0,0,0)))g.FillRectangle(b,0,0,width,height);
        if(o.Background&&o.BackgroundOpacity>0){int soft=Math.Min(o.Softness,Math.Min(width,height)/4);int passes=Math.Max(1,soft+1);double previous=0;for(int i=0;i<passes;i++){double desired=(o.BackgroundOpacity/255.0)*Math.Pow((i+1)/(double)passes,2);int alpha=(int)Math.Round(255*(desired-previous)/(1-previous));previous=desired;using(var brush=new SolidBrush(System.Drawing.Color.FromArgb(Math.Max(0,Math.Min(255,alpha)),0,0,0)))using(var shape=Round(i,i,width-2*i,height-2*i,Math.Max(2,height*.2f)))g.FillPath(brush,shape);}}
        // GDI grid fitting keeps small numerals on whole pixels. Render a
        // coverage mask first: ClearType RGB fringes cannot be composited onto
        // an arbitrary transparent game background directly.
        var flags=TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix;
        float fontSize=Math.Max(9,(float)Math.Round(height*.52));
        using(var measuredFont=new Font("Segoe UI",fontSize,FontStyle.Regular,GraphicsUnit.Pixel)){
            int measured=TextRenderer.MeasureText(text,measuredFont,new Size(int.MaxValue,int.MaxValue),flags).Width;
            if(measured>width-8)fontSize=Math.Max(8,(float)Math.Floor(fontSize*(width-8)/measured));
        }
        using(var mask=new Bitmap(width,height,PixelFormat.Format32bppArgb)){
            using(var mg=Graphics.FromImage(mask))using(var font=new Font("Segoe UI",fontSize,FontStyle.Regular,GraphicsUnit.Pixel)){
                mg.Clear(System.Drawing.Color.Black);TextRenderer.DrawText(mg,text,font,new Rectangle(4,0,width-8,height),System.Drawing.Color.White,flags);
            }
            var pixels=mask.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);var data=new byte[pixels.Stride*height];System.Runtime.InteropServices.Marshal.Copy(pixels.Scan0,data,0,data.Length);var tint=ColorTranslator.FromHtml(o.Color);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){int at=y*pixels.Stride+x*4;byte coverage=Math.Max(data[at],Math.Max(data[at+1],data[at+2]));data[at]=tint.B;data[at+1]=tint.G;data[at+2]=tint.R;data[at+3]=coverage;}
            System.Runtime.InteropServices.Marshal.Copy(data,0,pixels.Scan0,data.Length);mask.UnlockBits(pixels);g.DrawImageUnscaled(mask,0,0);
        }
        if(editing)using(var pen=new Pen(System.Drawing.Color.FromArgb(140,145,162,163))){pen.DashStyle=DashStyle.Dot;using(var shape=Round(.5f,.5f,width-1,height-1,6))g.DrawPath(pen,shape);}
    }return bitmap;}
}

internal sealed class FpsOverlay:Form {
    readonly EnergyOverlay owner;readonly FpsMonitor monitor;readonly LayeredImage image=new LayeredImage();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();FpsOptions options=new FpsOptions();bool editing,rendering,closing,dragging,disposed;readonly bool previewMode;Rectangle dragStart;Point mouseStart;int hit;string renderKey="";long lastRaise;
    internal FpsOverlay(EnergyOverlay helper,string root,bool preview){owner=helper;previewMode=preview;monitor=new FpsMonitor(root);Text="Aion 2 Helper - FPS";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;
        timer.Interval=250;timer.Tick+=delegate{monitor.Tick();UpdateWidget();};FormClosing+=delegate{closing=true;};}
    internal int? Value {get{return monitor.Value;}}
    internal string ReadingStatus {get{return monitor.Status;}}
    internal bool NeedsAdministrator {get{return monitor.NeedsAdministrator;}}
    internal void AllowAdministrator(){monitor.AllowAdministrator();}
    internal void Configure(FpsOptions value,bool locked,bool preview){options=value.Copy();options.Normalize();editing=!locked;Bounds=options.Bounds;if(!preview)monitor.Configure(options.Enabled,options.RefreshIntervalMs);if(preview)return;timer.Interval=options.RefreshIntervalMs;if(!options.Enabled){timer.Stop();Hide();return;}timer.Start();int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);renderKey="";UpdateWidget();}
    void UpdateWidget(){if(closing||!options.Enabled)return;int? value=monitor.Value;string text=value.HasValue?value.Value.ToString(CultureInfo.InvariantCulture)+" FPS":"\u2014 FPS";string key=Width+"/"+Height+"/"+text+"/"+editing;
        if(rendering)return;rendering=true;try{if(key!=renderKey){using(var bitmap=FpsDesign.Render(Width,Height,text,options,editing))image.Upload(bitmap);renderKey=key;image.Present(Handle,Left,Top,255);}if(!Visible){image.Present(Handle,Left,Top,255);Show();}long now=Stopwatch.GetTimestamp();if((now-lastRaise)/(double)Stopwatch.Frequency>=1){OverlayNative.RaiseWithoutFocus(Handle);lastRaise=now;}}finally{rendering=false;}}
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;closing=true;timer.Stop();timer.Dispose();monitor.Dispose();image.Dispose();}base.Dispose(disposing);}
    void SaveGeometry(){var cfg=owner.Configuration;cfg.Fps.Bounds=Bounds;owner.ApplyConfiguration(cfg);}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}Point p=PointToClient(new Point((short)(m.LParam.ToInt64()&0xffff),(short)((m.LParam.ToInt64()>>16)&0xffff)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||(hit>=10&&hit<=17)){dragging=true;dragStart=Bounds;mouseStart=Cursor.Position;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){var delta=new Point(Cursor.Position.X-mouseStart.X,Cursor.Position.Y-mouseStart.Y);Bounds=hit==2?new Rectangle(dragStart.X+delta.X,dragStart.Y+delta.Y,dragStart.Width,dragStart.Height):FpsDesign.ResizeBounds(dragStart,delta,hit);renderKey="";UpdateWidget();image.Present(Handle,Left,Top,255);m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;SaveGeometry();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x46){var pos=(OverlayNative.WindowPos)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.WindowPos));if((pos.Flags&1)==0){pos.Width=Math.Max(48,Math.Min(1600,pos.Width));pos.Height=Math.Max(20,Math.Min(400,pos.Height));System.Runtime.InteropServices.Marshal.StructureToPtr(pos,m.LParam,false);}m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x24){var info=(OverlayNative.MinMaxInfo)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.MinMaxInfo));info.MinTrackSize=new OverlayNative.Point(48,20);info.MaxTrackSize=new OverlayNative.Point(1600,400);System.Runtime.InteropServices.Marshal.StructureToPtr(info,m.LParam,false);m.Result=IntPtr.Zero;return;}
        base.WndProc(ref m);
    }
}
