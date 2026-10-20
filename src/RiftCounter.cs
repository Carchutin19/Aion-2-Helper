using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal sealed class RiftCounterPaint:IDisposable {
    readonly CrispOverlayText title=new CrispOverlayText(),subtitle=new CrispOverlayText(),digits=new CrispOverlayText();
    Font measurement;int measuredSize;
    internal Bitmap Render(RiftCounterOptions o,string language,string remaining,bool editing){
        var size=o.ActualBounds.Size;int w=size.Width,h=size.Height;float scale=h/82f;
        var image=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;
            if(editing)using(var target=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(target,0,0,w,h);
            var band=new Rectangle((int)(35*scale),(int)(9*scale),w-(int)(37*scale),h-(int)(18*scale));
            using(var background=new LinearGradientBrush(band,Color.Transparent,Color.Transparent,LinearGradientMode.Horizontal)){background.InterpolationColors=new ColorBlend{Colors=new[]{Color.FromArgb(0,0,0,0),Color.FromArgb(o.BackgroundOpacity,0,0,0),Color.FromArgb(o.BackgroundOpacity,0,0,0),Color.FromArgb(0,0,0,0)},Positions=new[]{0f,.13f,.5f,1f}};g.FillRectangle(background,band);}
            NotificationArtwork.Draw(g,"rift",new Rectangle((int)(9*scale),(int)(11*scale),(int)(60*scale),(int)(60*scale)),new NotificationOptions{IconGlow=o.IconGlow,GlowPercent=o.GlowPercent});
            int x=(int)(83*scale),right=w-(int)(13*scale),top=(int)(15*scale);var outline=new TextOutlineOptions{Enabled=o.Outline,Width=Math.Max(1,(int)Math.Round(o.OutlineWidth*scale)),Opacity=100};
            int font=Math.Max(10,(int)Math.Round(o.FontSize*scale));if(measurement==null||font!=measuredSize){if(measurement!=null)measurement.Dispose();measurement=new Font("Segoe UI",font,FontStyle.Bold,GraphicsUnit.Pixel);measuredSize=font;}int natural=TextRenderer.MeasureText("00:00",measurement,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine).Width;
            int clockWidth=Math.Min(right-x,Math.Max((int)(70*scale),natural+(int)(3*scale)));
            title.Draw(g,UiLanguage.Text("Spacetime Rift",language),Math.Max(10,(int)(14*scale)),true,Color.White,new Rectangle(x,top,right-x,(int)(22*scale)),0,false,outline);
            subtitle.Draw(g,UiLanguage.Text("Portal closes in",language),Math.Max(9,(int)(12*scale)),false,ColorTranslator.FromHtml("#CDC5D8"),new Rectangle(x,(int)(42*scale),Math.Max(20,right-x-clockWidth),(int)(24*scale)),0,true,outline);
            digits.Draw(g,remaining,font,true,ColorTranslator.FromHtml(o.Color),new Rectangle(right-clockWidth,(int)(34*scale),clockWidth,(int)(34*scale)),2,true,outline);
            if(editing)using(var pen=new Pen(Color.FromArgb(140,159,135,183))){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,0,0,w-1,h-1);}
        }return image;
    }
    public void Dispose(){title.Dispose();subtitle.Dispose();digits.Dispose();if(measurement!=null)measurement.Dispose();}
}
internal sealed class RiftCounterOverlay:Form {
    readonly EnergyOverlay owner;readonly bool preview;readonly LayeredImage image=new LayeredImage();readonly RiftCounterPaint paint=new RiftCounterPaint();
    RiftCounterOptions options=new RiftCounterOptions();bool editing,disposed,dragging;string language,renderKey,remaining="09:59";bool available;
    Point mouseStart;Rectangle dragStart;int hit,startScale;byte alpha;Point presented;internal int Uploads {get{return image.Uploads;}}
    internal RiftCounterOverlay(EnergyOverlay owner,bool preview){this.owner=owner;this.preview=preview;Text="Aion 2 Helper - Spacetime Rift";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;}
    internal void Configure(RiftCounterOptions next,bool locked){if(!RiftCounterOptions.SameAppearance(options,next)||language!=owner.Language||editing==locked)renderKey=null;options=next.Copy();options.Normalize();editing=!locked;language=owner.Language;Bounds=Fit(options.ActualBounds);int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);Present();}
    static Rectangle Fit(Rectangle b){var area=Screen.FromRectangle(b).WorkingArea;b.Width=Math.Min(b.Width,area.Width);b.Height=Math.Min(b.Height,area.Height);b.X=Math.Max(area.Left,Math.Min(area.Right-b.Width,b.X));b.Y=Math.Max(area.Top,Math.Min(area.Bottom-b.Height,b.Y));return b;}
    internal void Update(string text,bool show){remaining=text??"00:00";available=show;Present();}
    void Present(){if(disposed)return;bool show=options.Enabled&&options.Opacity>0&&(editing||available)&&(!DpsDesign.ShellUiActive()||preview);if(!show){if(Visible)Hide();return;}string text=editing?"09:59":remaining,key=text+"/"+editing+"/"+Width+"/"+Height;
        if(key!=renderKey){using(var bitmap=paint.Render(options,language,text,editing))image.Upload(bitmap);renderKey=key;alpha=0;}
        byte next=(byte)Math.Round(options.Opacity*2.55);if(alpha!=next||presented!=Location||!preview&&!Visible){image.Present(Handle,Left,Top,next);alpha=next;presented=Location;}if(!Visible&&!preview)Show();
    }
    internal object Status {get{return new{enabled=options.Enabled,visible=Visible,editing=editing,visualPreviewOnly=editing,remaining=available?remaining:null,scalePercent=options.ScalePercent,uploads=image.Uploads,timer=false,x=Left,y=Top,width=Width,height=Height};}}
    internal void VerifyNative(){var o=new RiftCounterOptions();Configure(o,true);Update("09:59",true);int uploads=image.Uploads,presentations=image.Presentations;for(int i=0;i<500;i++)Update("09:59",true);if(image.Uploads!=uploads||image.Presentations!=presentations)throw new Exception("Unchanged countdown must cache its bitmap and native presentation");Update("09:58",true);if(image.Uploads!=uploads+1)throw new Exception("One second change redraws once");o.Enabled=false;Configure(o,true);Update("09:57",true);if(image.Uploads!=uploads+1||Visible)throw new Exception("Disabled Rift counter stops rendering");o.Enabled=true;o.Opacity=0;Configure(o,true);Update("09:56",true);if(image.Uploads!=uploads+1)throw new Exception("Transparent Rift counter must not render invisible seconds");o.Opacity=100;Configure(o,false);int style=OverlayNative.GetWindowLong(Handle,-20);if((style&0x20)!=0)throw new Exception("Unlocked Rift counter accepts editing input");Configure(o,true);if((OverlayNative.GetWindowLong(Handle,-20)&0x20)==0||ShowInTaskbar||OverlayNative.SendMessage(Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()!=3)throw new Exception("Locked Rift counter passes clicks through without activation");}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenRiftSettings();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}Point p=PointToClient(new Point((short)(m.LParam.ToInt64()&65535),(short)((m.LParam.ToInt64()>>16)&65535)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){dragging=true;dragStart=Bounds;mouseStart=Cursor.Position;startScale=options.ScalePercent;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){int dx=Cursor.Position.X-mouseStart.X,dy=Cursor.Position.Y-mouseStart.Y;
            if(hit==2)Location=new Point(dragStart.X+dx,dragStart.Y+dy);else{bool left=hit==10||hit==13||hit==16;double ratio=hit==12||hit==15?Math.Max(.1,(dragStart.Height+2*(hit==12?-dy:dy))/(double)dragStart.Height):Math.Max(.1,(dragStart.Width+2*(left?-dx:dx))/(double)dragStart.Width);options.ScalePercent=Math.Max(50,Math.Min(300,(int)Math.Round(startScale*ratio)));Bounds=Fit(options.ActualBounds);renderKey=null;}Present();m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;var c=owner.Configuration;var b=options.Bounds;b.X=Left+Width/2-b.Width/2;b.Y=Top+Height/2-b.Height/2;c.Notifications.Rift.Counter.Bounds=b;c.Notifications.Rift.Counter.ScalePercent=options.ScalePercent;owner.ApplyConfiguration(c);m.Result=IntPtr.Zero;return;}
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;paint.Dispose();image.Dispose();}base.Dispose(disposing);}
}
internal sealed class RiftService:IDisposable {
    readonly EnergyOverlay owner;readonly NotificationOverlay notifications;readonly bool preview;readonly RiftCounterOverlay counter;readonly RiftAlertLedger ledger;readonly Func<bool> gameProbe;readonly Action<RiftAlertLedger.Notice> emit;
    internal readonly RiftIdentity Identity=new RiftIdentity();NotificationOptions options=new NotificationOptions();string region;DateTimeOffset nextSecond=DateTimeOffset.MinValue,nextGame=DateTimeOffset.MinValue,nextNotice=DateTimeOffset.MinValue,lastNow=DateTimeOffset.MinValue;bool gameOpen,disposed;RiftCalendar.Window active;DateTimeOffset? nextOpening;
    long submitted;string lastNotice;
    internal RiftService(EnergyOverlay owner,NotificationOverlay notifications,string root,bool preview,Func<bool> gameProbe=null,Action<RiftAlertLedger.Notice> emit=null){this.owner=owner;this.notifications=notifications;this.preview=preview;this.gameProbe=gameProbe;this.emit=emit;ledger=new RiftAlertLedger(preview?null:System.IO.Path.Combine(root,"rift-alert-state.json"));counter=new RiftCounterOverlay(owner,preview);}
    internal string Region {get{return options.Rift.Region=="auto"?RiftCalendar.RegionForServer(Identity.Server):options.Rift.Region;}}
    internal string Description {get{var now=DateTimeOffset.UtcNow;string key=Region;if(RiftCalendar.Zone(key)==null)return Identity.Server>0?UiLanguage.Text("Server region not recognized. Select it manually.",owner.Language)+" ("+Identity.Server+")":UiLanguage.Text("Waiting for character server",owner.Language);var next=nextOpening??RiftCalendar.Next(now,key);return RiftCalendar.Label(key,owner.Language)+(options.Rift.Region=="auto"?" · "+Identity.Server:"")+(next.HasValue?" · "+UiLanguage.Text("Next opening",owner.Language)+": "+next.Value.ToLocalTime().ToString("HH:mm"):"");}}
    internal void Configure(NotificationOptions next,bool locked){options=next.Copy();options.Normalize();Identity.Configure((!preview||gameProbe!=null)&&(options.Rift.Counter.Enabled||options.Enabled&&(options.Rift.Opening||options.Rift.Warning||options.Rift.Closing)));counter.Configure(options.Rift.Counter,locked);nextSecond=nextNotice=DateTimeOffset.MinValue;Maintain(DateTimeOffset.UtcNow);}
    internal void Maintain(DateTimeOffset now){if(preview&&gameProbe==null||disposed)return;if(now<lastNow){nextSecond=nextNotice=nextGame=DateTimeOffset.MinValue;nextOpening=null;}lastNow=now;if(now<nextSecond)return;nextSecond=now.AddSeconds(1);bool needed=options.Rift.Counter.Enabled||options.Enabled&&(options.Rift.Opening||options.Rift.Warning||options.Rift.Closing);if(!needed){counter.Update(null,false);return;}string current=Region;if(RiftCalendar.Zone(current)==null){if(region!=null)notifications.CancelRiftNotices();region=null;active=null;nextOpening=null;counter.Update(null,false);return;}
        if(region!=current||!nextOpening.HasValue||now>=nextOpening.Value||active!=null&&now>=active.Closing){if(region!=current)notifications.CancelRiftNotices();region=current;active=RiftCalendar.Active(now,region);nextOpening=RiftCalendar.Next(now,region);nextGame=nextNotice=DateTimeOffset.MinValue;}
        if(now>=nextGame){bool open=gameProbe==null?ShugoSchedule.GameOpen():gameProbe();if(gameOpen&&!open)notifications.CancelRiftNotices();gameOpen=open;nextGame=now.AddSeconds(5);}
        if(now>=nextNotice){nextNotice=now.AddSeconds(60-now.Second).AddMilliseconds(-now.Millisecond);var notice=ledger.Poll(now,region,options.Rift,options.Enabled&&gameOpen);if(notice!=null){submitted++;lastNotice=notice.Closing?"closing":notice.Opening?"opening":"warning";if(emit!=null)emit(notice);else notifications.ShowRift(notice.Opening,notice.Minutes,notice.At,notice.Closing);}}
        counter.Update(active==null?null:RiftCalendar.Remaining(active.Closing,now),gameOpen&&active!=null);
    }
    internal object Status {get{return new{region=Region,server=Identity.Server,scheduled=true,gameOpen=gameOpen,submittedNotifications=submitted,lastScheduledNotice=lastNotice,nextOpening=nextOpening.HasValue?nextOpening.Value.ToString("o"):null,opening=active==null?null:active.Opening.ToString("o"),closing=active==null?null:active.Closing.ToString("o"),ledgerError=ledger.Error,counter=counter.Status};}}
    public void Dispose(){if(disposed)return;disposed=true;counter.Dispose();}
}
