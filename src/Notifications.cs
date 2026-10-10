using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class NotificationOptions {
    public bool Enabled=true,PartyInvites=true,Sound=true,Fade=true;
    public int BackgroundOpacity=210,FontSize=18,Volume=80;
    public double DurationSeconds=6,FadeSeconds=.25;
    public string Color="#EEEEEE";
    public Rectangle Bounds=new Rectangle(740,120,440,90);
    internal NotificationOptions Copy(){return (NotificationOptions)MemberwiseClone();}
    internal static bool SameAppearance(NotificationOptions a,NotificationOptions b){return a.BackgroundOpacity==b.BackgroundOpacity&&a.FontSize==b.FontSize&&a.Color==b.Color;}
    internal void Normalize(){BackgroundOpacity=Math.Max(0,Math.Min(255,BackgroundOpacity));FontSize=Math.Max(10,Math.Min(48,FontSize));Volume=Math.Max(0,Math.Min(100,Volume));DurationSeconds=Finite(DurationSeconds,6,1,60);FadeSeconds=Finite(FadeSeconds,.25,0,2);if(!EnergyBarOptions.ValidColor(Color))Color="#EEEEEE";Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(260,Math.Min(1600,Bounds.Width)),Math.Max(60,Math.Min(500,Bounds.Height)));}
    static double Finite(double value,double fallback,double min,double max){return double.IsNaN(value)||double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));}
    internal static NotificationOptions Read(Dictionary<string,object> data){object value;var o=new NotificationOptions();if(data.TryGetValue("notifications",out value))try{o=new JavaScriptSerializer().ConvertToType<NotificationOptions>(value)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(NotificationOptions a,NotificationOptions b){return a.Enabled==b.Enabled&&a.PartyInvites==b.PartyInvites&&a.Sound==b.Sound&&a.Fade==b.Fade&&a.BackgroundOpacity==b.BackgroundOpacity&&a.FontSize==b.FontSize&&a.Volume==b.Volume&&a.DurationSeconds==b.DurationSeconds&&a.FadeSeconds==b.FadeSeconds&&a.Color==b.Color&&a.Bounds==b.Bounds;}
}

// Incoming 0992 layout correlated with invitations accepted and declined on the
// Global client. Roster/join frames never trigger a notification.
internal sealed class NotificationSignal {
    readonly object gate=new object();readonly Queue<string> pending=new Queue<string>();
    readonly Dictionary<string,double> requests=new Dictionary<string,double>();readonly List<string> expired=new List<string>();
    volatile bool enabled,partyInvites,hasPending;string lastName;double lastTime,lastAccepted,decodeDelayMs;long received;string lastInviter;
    static readonly UTF8Encoding strictUtf8=new UTF8Encoding(false,true);
    internal bool HasPending {get{return hasPending;}}
    internal Action OnPending;
    internal double LastAccepted {get{lock(gate)return lastAccepted;}}
    internal bool DetectionReady {get{return true;}}
    internal long Received {get{lock(gate)return received;}}
    internal object Status {get{lock(gate)return new{ready=DetectionReady,received=received,lastInviter=lastInviter,lastTimestamp=lastTime,acceptedUtc=lastAccepted,decodeDelayMs=decodeDelayMs,queued=pending.Count};}}
    internal void Configure(NotificationOptions o){lock(gate){enabled=o.Enabled;partyInvites=o.PartyInvites;if(!enabled||!partyInvites){pending.Clear();hasPending=false;lastName=null;requests.Clear();}}}
    internal static bool Invitation(byte[] data,int start,int end,out string name,out string request){
        name=request=null;if(data==null||start<0||end<start||end>data.Length||end-start<55||end-start>126||data[start]!=9||data[start+1]!=0x92)return false;
        int count=data[start+30],tail=start+31+count;if(count<1||count>72||tail+23!=end||data[start+16]==0&&data[start+17]==0||data[tail]!=data[start+16]||data[tail+1]!=data[start+17])return false;
        for(int p=tail+2;p<tail+6;p++)if(data[p]!=0)return false;if(data[tail+14]!=1)return false;
        ulong party=BitConverter.ToUInt64(data,start+2),stamp=BitConverter.ToUInt64(data,tail+15),sender=0;for(int p=0;p<6;p++)sender|=(ulong)data[start+10+p]<<(8*p);if(party==0||sender==0||stamp==0)return false;
        try{name=strictUtf8.GetString(data,start+31,count);}catch(DecoderFallbackException){return false;}if(name.Length>24||string.IsNullOrWhiteSpace(name)||name.Trim()!=name)return false;foreach(char c in name)if(char.IsControl(c))return false;
        request=party.ToString("X16")+sender.ToString("X12")+stamp.ToString("X16");return true;
    }
    internal void Consume(byte[] data,int start,int end,double timestamp,string flow){if(!enabled||!partyInvites)return;if(data==null||start<0||end<start||end>data.Length||end-start<2||data[start]!=9||data[start+1]!=0x92||double.IsNaN(timestamp)||double.IsInfinity(timestamp)||timestamp<0)return;string name,key;if(!Invitation(data,start,end,out name,out key))return;lock(gate){if(!enabled||!partyInvites)return;expired.Clear();foreach(var entry in requests)if(timestamp-entry.Value>120)expired.Add(entry.Key);foreach(string old in expired)requests.Remove(old);if(requests.ContainsKey(key))return;if(requests.Count>=64){string oldest=null;double earliest=double.MaxValue;foreach(var entry in requests)if(entry.Value<earliest){earliest=entry.Value;oldest=entry.Key;}requests.Remove(oldest);}requests[key]=timestamp;received++;lastInviter=name;lastTime=timestamp;lastAccepted=DpsHistory.UtcNow();decodeDelayMs=Math.Max(0,(lastAccepted-timestamp)*1000);if(pending.Count>=8)pending.Dequeue();pending.Enqueue(name);hasPending=true;}var wake=OnPending;if(wake!=null)wake();}
    internal void Reset(){lock(gate){pending.Clear();hasPending=false;lastName=null;lastTime=lastAccepted=decodeDelayMs=0;lastInviter=null;requests.Clear();}}
    internal bool EnqueueValidatedInvitation(string name,double timestamp){if(string.IsNullOrWhiteSpace(name)||name.Length>64||double.IsNaN(timestamp)||double.IsInfinity(timestamp)||timestamp<0)return false;foreach(char c in name)if(char.IsControl(c))return false;lock(gate){if(!enabled||!partyInvites||name==lastName&&timestamp-lastTime<2)return false;lastName=name;lastTime=timestamp;if(pending.Count>=8)pending.Dequeue();pending.Enqueue(name);hasPending=true;return true;}}
    internal string Take(){if(!hasPending)return null;lock(gate){if(pending.Count==0)return null;string value=pending.Dequeue();hasPending=pending.Count>0;return value;}}
}

internal sealed class NotificationMotion {
    internal double Opacity;internal int TickInterval=16;double started,initial;bool active;
    internal bool Active {get{return active;}}
    internal void Show(double now){initial=Opacity;started=now;active=true;}
    internal bool Step(double now,NotificationOptions o,bool editing){TickInterval=16;if(editing){Opacity=1;return false;}if(!active){Opacity=0;return false;}double fade=o.Fade?o.FadeSeconds:0,elapsed=Math.Max(0,now-started),rise=fade*(1-initial);if(elapsed<rise)Opacity=initial+elapsed/fade;else if(elapsed<rise+o.DurationSeconds){Opacity=1;TickInterval=Math.Max(16,(int)Math.Ceiling((rise+o.DurationSeconds-elapsed)*1000));}else if(fade>0&&elapsed<rise+o.DurationSeconds+fade)Opacity=1-(elapsed-rise-o.DurationSeconds)/fade;else {Opacity=0;active=false;}return active;}
    internal void Clear(){active=false;Opacity=0;}
}

internal sealed class NotificationAudio:IDisposable {
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern uint mciSendString(string command,StringBuilder result,int length,IntPtr callback);
    readonly string file,alias="aionnotice"+Guid.NewGuid().ToString("N");bool opened;
    internal string Error {get;private set;}
    internal NotificationAudio(string root){file=Path.Combine(root,"assets","sounds","party-invite.mp3");}
    internal void Play(int volume){Error=null;if(!File.Exists(file)){Error="Notification sound file is missing.";return;}if(!opened){if(mciSendString("open \""+file+"\" type mpegvideo alias "+alias,null,0,IntPtr.Zero)!=0){Error="Notification sound could not be played.";return;}opened=true;}mciSendString("stop "+alias,null,0,IntPtr.Zero);mciSendString("setaudio "+alias+" volume to "+(Math.Max(0,Math.Min(100,volume))*10),null,0,IntPtr.Zero);if(mciSendString("play "+alias+" from 0",null,0,IntPtr.Zero)!=0)Error="Notification sound could not be played.";}
    internal void Stop(){if(opened)mciSendString("stop "+alias,null,0,IntPtr.Zero);}
    internal void Close(){if(opened){mciSendString("close "+alias,null,0,IntPtr.Zero);opened=false;}}
    public void Dispose(){Close();}
}

internal static class NotificationDesign {
    internal static Bitmap Render(int width,int height,string name,string language,NotificationOptions o,bool editing,FpsDesign.Workspace workspace){
        workspace.Prepare(width,height);var image=new Bitmap(width,height,PixelFormat.Format32bppPArgb);var mask=workspace.Mask;var mg=workspace.MaskGraphics;mg.Clear(Color.Black);
        using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;
            if(editing)using(var target=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(target,0,0,width,height);
            using(var shape=FpsDesign.Round(2,2,width-4,height-4,12))using(var background=new SolidBrush(Color.FromArgb(o.BackgroundOpacity,0,0,0)))g.FillPath(background,shape);
            using(var shape=FpsDesign.Round(2.5f,2.5f,width-5,height-5,12))using(var pen=new Pen(Color.FromArgb(35,255,255,255)))g.DrawPath(pen,shape);
            int pad=18,bodySize=Math.Min(o.FontSize,Math.Max(10,(height-22)/2)),titleHeight=Math.Max(16,(int)(bodySize*.9)),contentTop=Math.Max(titleHeight+12,(height-bodySize)/2);var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter;
            bool sample=editing||name==null;
            TextRenderer.DrawText(mg,UiLanguage.Text(sample?"Notification":"Party invitation",language),workspace.Font(Math.Max(10,bodySize*.75f),true),new Rectangle(pad,8,width-pad*2,titleHeight),Color.FromArgb(175,175,175),flags);
            string text=sample?UiLanguage.Text("This is a sample notification.",language):string.Format(UiLanguage.Text("{0} is inviting you to a party.",language),name);TextRenderer.DrawText(mg,text,workspace.Font(bodySize,false),new Rectangle(pad,contentTop,width-pad*2,Math.Max(bodySize+4,height-contentTop-10)),Color.White,flags);
            var data=mask.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);try{var pixels=workspace.Pixels;int length=data.Stride*height;var tint=ColorTranslator.FromHtml(o.Color);Marshal.Copy(data.Scan0,pixels,0,length);for(int p=0;p<length;p+=4){byte alpha=Math.Max(pixels[p],Math.Max(pixels[p+1],pixels[p+2]));pixels[p]=tint.B;pixels[p+1]=tint.G;pixels[p+2]=tint.R;pixels[p+3]=alpha;}Marshal.Copy(pixels,0,data.Scan0,length);}finally{mask.UnlockBits(data);}g.DrawImageUnscaled(mask,0,0);
            if(editing)using(var pen=new Pen(Color.FromArgb(140,145,162,163))){pen.DashStyle=DashStyle.Dot;using(var shape=FpsDesign.Round(.5f,.5f,width-1,height-1,12))g.DrawPath(pen,shape);}
        }return image;
    }
    internal static Rectangle Resize(Rectangle start,Point delta,int hit){bool left=hit==10||hit==13||hit==16,right=hit==11||hit==14||hit==17,top=hit==12||hit==13||hit==14,bottom=hit==15||hit==16||hit==17;int width=Math.Max(260,Math.Min(1600,start.Width+(left?-delta.X:right?delta.X:0))),height=Math.Max(60,Math.Min(500,start.Height+(top?-delta.Y:bottom?delta.Y:0)));return new Rectangle(left?start.Right-width:start.X,top?start.Bottom-height:start.Y,width,height);}
}

internal sealed class NotificationOverlay:Form {
    readonly EnergyOverlay owner;readonly NotificationSignal signal;readonly NotificationAudio audio;readonly LayeredImage image=new LayeredImage();readonly FpsDesign.Workspace workspace=new FpsDesign.Workspace();readonly NotificationMotion motion=new NotificationMotion();readonly Stopwatch clock=Stopwatch.StartNew();readonly Timer animation=new Timer();
    NotificationOptions options=new NotificationOptions();string name,language;bool editing,valid,disposed,dragging;Rectangle dragStart;Point mouseStart;int hit,wakeQueued;byte shownAlpha;Point shownLocation;double nextMaintenance,lastRaise,dispatchedUtc,firstVisibleUtc,dispatchDelayMs;readonly bool preview;
    internal NotificationOverlay(EnergyOverlay parent,string root,NotificationSignal source,bool previewMode){owner=parent;signal=source;preview=previewMode;audio=new NotificationAudio(root);signal.OnPending=Wake;Text="Aion 2 Helper - Notifications";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;animation.Interval=16;animation.Tick+=delegate{Pulse();};}
    void Wake(){if(!IsHandleCreated||IsDisposed||System.Threading.Interlocked.CompareExchange(ref wakeQueued,1,0)!=0)return;try{BeginInvoke((Action)delegate{System.Threading.Interlocked.Exchange(ref wakeQueued,0);if(!disposed)Pulse();});}catch(InvalidOperationException){System.Threading.Interlocked.Exchange(ref wakeQueued,0);}}
    internal string AudioError {get{return audio.Error;}}
    internal object WindowStatus {get{return new{enabled=options.Enabled,partyInvites=options.PartyInvites,sound=options.Sound,visible=Visible,opacity=motion.Opacity,editing=editing,animationTimer=animation.Enabled,audioError=audio.Error,dispatchedUtc=dispatchedUtc,firstVisibleUtc=firstVisibleUtc,dispatchDelayMs=dispatchDelayMs,firstVisibleDelayMs=firstVisibleUtc==0?0:Math.Max(0,(firstVisibleUtc-dispatchedUtc)*1000)};}}
    internal void Configure(NotificationOptions value,bool locked){
        bool stopInvite=options.PartyInvites&&!value.PartyInvites;
        bool changed=!NotificationOptions.SameAppearance(options,value)||language!=owner.Language||editing==locked;
        bool timing=options.Fade!=value.Fade||options.FadeSeconds!=value.FadeSeconds||options.DurationSeconds!=value.DurationSeconds;
        options=value.Copy();options.Normalize();signal.Configure(options);language=owner.Language;editing=!locked;
        Rectangle target=DpsDesign.UsableBounds(options.Bounds);if(target.Size!=Size)changed=true;if(target!=Bounds)Bounds=target;
        if(changed)valid=false;
        if(stopInvite){motion.Clear();animation.Stop();audio.Close();if(Visible)Hide();}
        if(!options.Sound)audio.Close();
        if(!options.Enabled){motion.Clear();animation.Stop();audio.Close();if(Visible)Hide();return;}
        if(timing)animation.Stop();
        int style=OverlayNative.GetWindowLong(Handle,-20),next=editing?style&~0x20:style|0x20;if(next!=style)OverlayNative.SetWindowLong(Handle,-20,next);
        nextMaintenance=0;Pulse();
    }
    internal void Test(){if(!options.Enabled)return;ShowNotice(null,true);}
    internal void VerifyNative(){if(Owner!=null)throw new Exception("Notifications must be independent of energy visibility");var o=new NotificationOptions{Enabled=true,Sound=false,Fade=false};Configure(o,true);Test();int uploads=image.Uploads;for(int i=0;i<500;i++)Pulse();if(image.Uploads!=uploads)throw new Exception("Notification hold/fade must reuse the uploaded bitmap");VerifyRenderCache(o);int style=OverlayNative.GetWindowLong(Handle,-20);if((style&0x20)==0||(style&0x08000000)==0||ShowInTaskbar||OverlayNative.SendMessage(Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()!=3)throw new Exception("Locked notification must pass clicks through, avoid activation and taskbar entry");motion.Clear();Pulse();VerifyWake();o.PartyInvites=false;Configure(o,true);if(Visible||animation.Enabled||motion.Opacity!=0)throw new Exception("Disabling invitations must dismiss the active invitation and stop animation");o.PartyInvites=true;Configure(o,true);motion.Clear();Pulse();if(animation.Enabled)throw new Exception("Idle notification must stop its animation timer");VerifyFadeTimer(o);Configure(o,false);if((OverlayNative.GetWindowLong(Handle,-20)&0x20)!=0||animation.Enabled)throw new Exception("Editable sample needs input but no animation or sound");o.Enabled=false;Configure(o,true);if(animation.Enabled||Visible)throw new Exception("Disabled notification must stop and hide");}
    void VerifyRenderCache(NotificationOptions original){
        int uploads=image.Uploads,presents=image.Presentations;for(int i=0;i<500;i++)Maintain(i*.016);if(image.Uploads!=uploads||image.Presentations!=presents)throw new Exception("Unchanged hold must reuse its native image and presentation");
        var changed=original.Copy();changed.Volume=30;changed.Sound=true;changed.DurationSeconds=30;changed.Bounds.X+=10;Configure(changed,true);if(image.Uploads!=uploads)throw new Exception("Audio, timing and moving must preserve notification glyphs");
        changed.Color="#123456";Configure(changed,true);if(image.Uploads!=uploads+1)throw new Exception("Text color change must redraw once");Configure(original,true);
        using(var process=Process.GetCurrentProcess()){uint before=OverlayNative.GetGuiResources(process.Handle,0);var resize=original.Copy();for(int i=0;i<120;i++){resize.Bounds.Width=440+i%3;Configure(resize,true);}Configure(original,true);uint after=OverlayNative.GetGuiResources(process.Handle,0);if(after>before+3)throw new Exception("Notification fonts, bitmaps and DCs must stay bounded after resizing");}
    }
    void VerifyFadeTimer(NotificationOptions original){
        var timed=original.Copy();timed.Fade=true;timed.FadeSeconds=.06;timed.DurationSeconds=1;timed.Sound=false;Configure(timed,true);Test();double deadline=clock.Elapsed.TotalSeconds+1.8;bool partial=false;
        while(clock.Elapsed.TotalSeconds<deadline&&animation.Enabled){Application.DoEvents();if(motion.Opacity>0&&motion.Opacity<1)partial=true;System.Threading.Thread.Sleep(5);}
        if(!partial||animation.Enabled||motion.Opacity!=0)throw new Exception("Fade and hold must complete without the shared polling timer");Configure(original,true);
    }
    void VerifyWake(){
        byte[] invitation=new byte[59];invitation[0]=9;invitation[1]=0x92;invitation[2]=1;invitation[10]=2;invitation[16]=0x15;invitation[17]=5;invitation[30]=5;Array.Copy(Encoding.UTF8.GetBytes("Edeln"),0,invitation,31,5);invitation[36]=0x15;invitation[37]=5;invitation[50]=1;invitation[51]=1;
        var worker=new System.Threading.Thread(delegate(){signal.Consume(invitation,0,invitation.Length,DpsHistory.UtcNow(),"wake-test");});worker.Start();worker.Join();double before=dispatchedUtc;Application.DoEvents();if(name!="Edeln"||dispatchedUtc<=before||dispatchDelayMs>1000)throw new Exception("Incoming capture event must wake the UI without a polling tick");
    }
    void ShowNotice(string player,bool sound,bool pulse=true){name=player;valid=false;dispatchedUtc=DpsHistory.UtcNow();firstVisibleUtc=0;dispatchDelayMs=signal.LastAccepted==0?0:Math.Max(0,(dispatchedUtc-signal.LastAccepted)*1000);motion.Show(clock.Elapsed.TotalSeconds);animation.Stop();if(sound&&options.Sound&&!preview)audio.Play(options.Volume);if(pulse)Pulse();}
    // The shared energy timer can tick at 60 Hz. Idle notifications need no
    // decoding lock, clock or shell lookup; fades have their own short timer.
    internal void Maintain(double now){
        if(disposed||!options.Enabled||!editing&&!motion.Active&&!signal.HasPending)return;
        if(animation.Enabled&&animation.Interval==16&&!signal.HasPending)return;
        if(now<nextMaintenance)return;nextMaintenance=now+.25;Pulse();
    }
    void Schedule(bool active){
        if(!active){if(animation.Enabled)animation.Stop();return;}
        int interval=motion.TickInterval;
        if(!animation.Enabled){animation.Interval=interval;animation.Start();}
        else if(interval==16||animation.Interval==16){if(animation.Interval!=interval)animation.Interval=interval;}
        // Keep the one-shot hold deadline. Resetting it on every maintenance
        // tick kills/recreates a native timer without changing its due time.
    }
    internal void Pulse(){
        if(disposed||!options.Enabled)return;
        if(!editing&&!motion.Active&&!signal.HasPending){if(Visible)Hide();return;}
        string invite=signal.Take();if(invite!=null){ShowNotice(invite,true,false);if(signal.HasPending)Wake();}
        double now=clock.Elapsed.TotalSeconds;bool active=motion.Step(now,options,editing);Schedule(active);
        bool shell=!preview&&DpsDesign.ShellUiActive();byte alpha=shell?(byte)0:(byte)Math.Round(motion.Opacity*255);
        if(alpha==0){if(Visible)Hide();return;}
        if(!valid){using(var bitmap=NotificationDesign.Render(Width,Height,name,language,options,editing,workspace))image.Upload(bitmap);valid=true;shownAlpha=0;}
        if(alpha!=shownAlpha||shownLocation!=Location||!preview&&!Visible){image.Present(Handle,Left,Top,alpha);shownAlpha=alpha;shownLocation=Location;}
        if(!preview&&!Visible)Show();if(firstVisibleUtc==0&&dispatchedUtc>0)firstVisibleUtc=DpsHistory.UtcNow();
        if(!preview&&now-lastRaise>=1){OverlayNative.RaiseWithoutFocus(Handle);lastRaise=now;}
    }
    void SaveGeometry(){var c=owner.Configuration;c.Notifications.Bounds=Bounds;owner.ApplyConfiguration(c);}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenTrayMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}Point p=PointToClient(new Point((short)(m.LParam.ToInt64()&0xffff),(short)((m.LParam.ToInt64()>>16)&0xffff)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){dragging=true;dragStart=Bounds;mouseStart=Cursor.Position;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){Point delta=new Point(Cursor.Position.X-mouseStart.X,Cursor.Position.Y-mouseStart.Y);Rectangle previous=Bounds;Bounds=DpsDesign.UsableBounds(hit==2?new Rectangle(dragStart.X+delta.X,dragStart.Y+delta.Y,dragStart.Width,dragStart.Height):NotificationDesign.Resize(dragStart,delta,hit));if(Size!=previous.Size)valid=false;Pulse();m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;SaveGeometry();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x46){var p=(OverlayNative.WindowPos)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.WindowPos));if((p.Flags&1)==0){p.Width=Math.Max(260,Math.Min(1600,p.Width));p.Height=Math.Max(60,Math.Min(500,p.Height));Marshal.StructureToPtr(p,m.LParam,false);}}
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;signal.OnPending=null;animation.Stop();animation.Dispose();audio.Dispose();image.Dispose();workspace.Dispose();}base.Dispose(disposing);}
}
