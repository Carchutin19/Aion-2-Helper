using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Timer=System.Windows.Forms.Timer;

internal sealed class StaggerCountdownOptions {
    public bool Enabled=true,Glow=true,Shrink=true;
    public int Position=4,FontSize=24,GlowPercent=100,MinimumScalePercent=55,OffsetY=0;
    public string Color="#FFE08A";
    // The observed state lasts ~5s. This is an estimated display, never a reset.
    public double ReferenceSeconds=5;
    internal StaggerCountdownOptions Copy(){return (StaggerCountdownOptions)MemberwiseClone();}
    internal void Normalize(){OffsetY=Math.Max(-200,Math.Min(200,OffsetY));Position=Math.Max(0,Math.Min(5,Position));FontSize=Math.Max(10,Math.Min(64,FontSize));GlowPercent=Math.Max(0,Math.Min(200,GlowPercent));MinimumScalePercent=Math.Max(20,Math.Min(100,MinimumScalePercent));if(!EnergyBarOptions.ValidColor(Color))Color="#FFE08A";if(double.IsNaN(ReferenceSeconds)||double.IsInfinity(ReferenceSeconds))ReferenceSeconds=5;ReferenceSeconds=Math.Max(1,Math.Min(30,ReferenceSeconds));}
}

internal sealed class StaggerOptions {
    public EnergyBarOptions Bar=new EnergyBarOptions{Enabled=false,AutoHide=false,CurrentText=true,MaximumText=true,PercentageText=true,LowColor="#B85416",MediumColor="#287FA0",HighColor="#397EB8"};
    public Rectangle Bounds=new Rectangle(760,220,400,6);
    public StaggerCountdownOptions Countdown=new StaggerCountdownOptions();
    internal StaggerOptions Copy(){return new StaggerOptions{Bar=Bar.Copy(),Bounds=Bounds,Countdown=Countdown.Copy()};}
    internal void Normalize(){if(Bar==null)Bar=new EnergyBarOptions{Enabled=false,AutoHide=false};Bar.Normalize();Bar.AutoHide=false;if(Countdown==null)Countdown=new StaggerCountdownOptions();Countdown.Normalize();Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(40,Math.Min(32767,Bounds.Width)),Math.Max(3,Math.Min(300,Bounds.Height)));}
    internal static StaggerOptions Read(Dictionary<string,object> cfg){object value;var o=new StaggerOptions();if(cfg.TryGetValue("stagger",out value))try{o=new JavaScriptSerializer().ConvertToType<StaggerOptions>(value)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(StaggerOptions a,StaggerOptions b){return new JavaScriptSerializer().Serialize(a)==new JavaScriptSerializer().Serialize(b);}
}

// Exact observed 05E0 shapes. Entity IDs are scoped to their TCP gameplay flow.
// Selection follows the last directly attacked enemy once self is identified;
// before that, the last received gauge is a provisional fallback, not a UI lock.
internal sealed class StaggerSignal {
    internal sealed class Sample {internal uint Actor,Current,Maximum;internal byte State;internal string Flow;internal double Time,GroggyStarted,Traffic;internal double? ZeroStarted;internal bool Groggy;}
    sealed class Flow {internal readonly Dictionary<uint,Sample> Gauges=new Dictionary<uint,Sample>();internal double Traffic;internal uint Map;}
    readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();
    volatile bool enabled;string selfFlow,selectedFlow;uint self,target;Sample fallback;internal Action Changed;
    internal void Configure(bool value){lock(gate){if(enabled==value)return;enabled=value;ResetState();}}
    void ResetState(){flows.Clear();selfFlow=selectedFlow=null;self=target=0;fallback=null;}
    internal void Reset(){lock(gate)ResetState();}
    // Npcap and the UI use different clock sources. A newly delivered packet
    // can be a few milliseconds ahead of DateTime.UtcNow; it is still fresh.
    internal static bool Fresh(Sample sample,double now){return sample!=null&&Math.Max(0,now-sample.Traffic)<=10;}
    // State 2 is the visually validated active phase. Keep other observed full
    // states as actual data, but hide until another active-phase update arrives.
    // In particular, a positive refill does not itself prove stagger is usable.
    internal static bool Available(Sample sample){return sample!=null&&sample.State==2;}
    internal static bool DisplayAvailable(Sample sample,double now){return Fresh(sample,now)&&Available(sample)&&!ZeroExpired(sample,now);}
    internal static bool ZeroExpired(Sample sample,double now){return sample!=null&&sample.Current==0&&sample.ZeroStarted.HasValue&&Math.Max(0,now-sample.ZeroStarted.Value)>=5;}
    internal Sample Read(){lock(gate){Flow flow;Sample s=null;if(target!=0){if(flows.TryGetValue(selectedFlow,out flow))flow.Gauges.TryGetValue(target,out s);}else s=fallback;if(s==null)return null;return new Sample{Actor=s.Actor,Current=s.Current,Maximum=s.Maximum,State=s.State,Flow=s.Flow,Time=s.Time,Groggy=s.Groggy,GroggyStarted=s.GroggyStarted,ZeroStarted=s.ZeroStarted,Traffic=flows[s.Flow].Traffic};}}
    static bool EnergyActor(byte[] b,int start,int end,out uint id){int p=start+2;id=0;if(!DpsSignal.Var(b,ref p,end,out id)||p>=end||id==0)return false;int flags=b[p++];bool energy=false;if((flags&~3)!=0)return false;for(int width=4;width<=8;width+=4)if((flags&(width==4?1:2))!=0){uint count;if(!DpsSignal.Var(b,ref p,end,out count)||count>(end-p)/(width+1))return false;for(uint n=0;n<count;n++){if(width==4&&b[p]==3)energy=true;p+=width+1;}}return p==end&&energy;}
    internal void Consume(byte[] b,int start,int end,double time,string key){
        if(!enabled||b==null||start<0||end>b.Length||end-start<2||key==null||double.IsNaN(time)||double.IsInfinity(time))return;
        bool changed=false;lock(gate){if(!enabled)return;Flow flow;if(!flows.TryGetValue(key,out flow)){if(flows.Count>=4){string remove=null;double oldest=double.MaxValue;foreach(var pair in flows)if(pair.Key!=selfFlow&&pair.Key!=selectedFlow&&pair.Value.Traffic<oldest){oldest=pair.Value.Traffic;remove=pair.Key;}if(remove==null)return;flows.Remove(remove);if(fallback!=null&&fallback.Flow==remove)fallback=null;}flow=new Flow();flows[key]=flow;}flow.Traffic=Math.Max(flow.Traffic,time);
            int op=(b[start]<<8)|b[start+1],p=start+2;uint actor=0;string name;byte role;
            bool identified=op==0x008d&&EnergyActor(b,start,end,out actor);if(!identified){actor=0;if(op==0x3336)identified=DpsSignal.PlayerIdentity(b,start,end,true,out actor,out name,out role);}
            if(identified){if(selfFlow!=key||self!=actor){selfFlow=key;self=actor;selectedFlow=null;target=0;if(fallback!=null&&fallback.Flow!=key)fallback=null;changed=true;}}
            if(op==0x2136&&end-start==48){uint map=BitConverter.ToUInt32(b,start+6);if(flow.Map!=0&&flow.Map!=map){flow.Gauges.Clear();if(selectedFlow==key){target=0;selectedFlow=null;}if(fallback!=null&&fallback.Flow==key)fallback=null;changed=true;}flow.Map=map;}
            else if(op==0x4236&&DpsSignal.Var(b,ref p,end,out actor)){flow.Gauges.Remove(actor);if(fallback!=null&&fallback.Flow==key&&fallback.Actor==actor)fallback=null;if(selectedFlow==key&&target==actor){target=0;selectedFlow=null;fallback=null;changed=true;}}
            else if(op==0x0438){DpsSignal.Hit hit;Sample attacked;if(DpsSignal.Direct(b,start,end,out hit)&&(hit.Skill/1000000>=11&&hit.Skill/1000000<=19)&&flow.Gauges.TryGetValue(hit.Target,out attacked)){if(key==selfFlow&&hit.Source==self&&hit.Target!=self&&(target!=hit.Target||selectedFlow!=key)){target=hit.Target;selectedFlow=key;changed=true;}}}
            else if(op==0x05e0&&DpsSignal.Var(b,ref p,end,out actor)&&actor>0){int length=end-p;bool full=length==11&&b[p]==3&&b[p+1]==1&&b[p+10]<=3,zero=length==3&&b[p]==0&&b[p+1]==3&&b[p+2]==2;uint maximum=0,current=0;Sample value;
                if(full){maximum=BitConverter.ToUInt32(b,p+2);current=BitConverter.ToUInt32(b,p+6);if(maximum==0||maximum>10000000||current>maximum)return;}else if(!zero)return;
                if(!flow.Gauges.TryGetValue(actor,out value)){if(!full)return;if(flow.Gauges.Count>=128){uint remove=0;double oldest=double.MaxValue;foreach(var pair in flow.Gauges)if(!(key==selectedFlow&&pair.Key==target)&&pair.Value.Time<oldest){oldest=pair.Value.Time;remove=pair.Key;}if(remove==0)return;flow.Gauges.Remove(remove);}value=new Sample{Actor=actor,Flow=key};flow.Gauges[actor]=value;}
                if(time<value.Time)return;value.Time=time;value.State=full?b[p+10]:(byte)2;if(full){value.Current=current;value.Maximum=maximum;if(current>0||value.State!=2){value.Groggy=false;value.GroggyStarted=0;}}else{if(!value.Groggy)value.GroggyStarted=value.ZeroStarted.HasValue?value.ZeroStarted.Value:time;value.Groggy=true;value.Current=0;}
                if(value.Current>0)value.ZeroStarted=null;else if(!value.ZeroStarted.HasValue)value.ZeroStarted=time;
                if(selfFlow==null||selfFlow==key){fallback=value;changed=true;}
            }
        }if(changed){var wake=Changed;if(wake!=null)wake();}
    }
}

// Presentation only: the server gauge and its numeric labels remain unchanged.
internal struct StaggerDisplayProgress {
    internal double Ratio,Remaining;internal bool Refilling,Animating;
    internal static StaggerDisplayProgress Read(StaggerSignal.Sample sample,double utc,double seconds){
        var result=new StaggerDisplayProgress{Ratio=sample==null||sample.Maximum==0?0:(double)sample.Current/sample.Maximum,Remaining=-1};
        if(!StaggerSignal.Fresh(sample,utc)||!StaggerSignal.Available(sample)||!sample.Groggy||sample.Current!=0)return result;
        if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)seconds=5;
        double elapsed=Math.Max(0,utc-sample.GroggyStarted);
        result.Refilling=true;result.Ratio=Math.Max(0,Math.Min(1,elapsed/seconds));result.Remaining=Math.Max(0,seconds-elapsed);result.Animating=result.Remaining>0;
        return result;
    }
}

internal static class ProgressBarText {
    internal sealed class Label {internal int Position,Offset;internal string Text;}
    internal static List<Label> Labels(EnergyBarOptions o,bool known,uint current,uint maximum){var labels=new List<Label>();if(!known||maximum==0)return labels;AddLabel(labels,o.CurrentPosition,o.CurrentOffsetY,o.CurrentText?current.ToString(CultureInfo.InvariantCulture):null);AddLabel(labels,o.MaximumPosition,o.MaximumOffsetY,o.MaximumText?"/ "+maximum.ToString(CultureInfo.InvariantCulture):null);AddLabel(labels,o.PercentagePosition,o.PercentageOffsetY,o.PercentageText?(100.0*current/maximum).ToString("0",CultureInfo.InvariantCulture)+"%":null);return labels;}
    static void AddLabel(List<Label> labels,int position,int offset,string text){if(text==null)return;foreach(var label in labels)if(label.Position==position&&label.Offset==offset){label.Text+="  "+text;return;}labels.Add(new Label{Position=position,Offset=offset,Text=text});}
    internal static bool Same(EnergyBarOptions a,EnergyBarOptions b){return a.CurrentOffsetY==b.CurrentOffsetY&&a.MaximumOffsetY==b.MaximumOffsetY&&a.PercentageOffsetY==b.PercentageOffsetY&&a.CurrentText==b.CurrentText&&a.MaximumText==b.MaximumText&&a.PercentageText==b.PercentageText&&a.CurrentPosition==b.CurrentPosition&&a.MaximumPosition==b.MaximumPosition&&a.PercentagePosition==b.PercentagePosition&&a.TextSize==b.TextSize&&a.TextColor==b.TextColor;}
    internal static string[] Slots(EnergyBarOptions o,bool known,uint current,uint maximum){var slots=new string[6];if(!known||maximum==0)return slots;Add(slots,o.CurrentPosition,o.CurrentText?current.ToString(CultureInfo.InvariantCulture):null);Add(slots,o.MaximumPosition,o.MaximumText?"/ "+maximum.ToString(CultureInfo.InvariantCulture):null);Add(slots,o.PercentagePosition,o.PercentageText?(100.0*current/maximum).ToString("0",CultureInfo.InvariantCulture)+"%":null);return slots;}
    static void Add(string[] slots,int position,string value){if(value==null)return;slots[position]=slots[position]==null?value:slots[position]+"  "+value;}
}

internal static class ProgressBarPreview {
    internal static Bitmap Render(int width,double ratio,EnergyBarOptions o,StaggerCountdownOptions countdown,BarDesign.GlowWorkspace workspace){
        var labels=ProgressBarText.Labels(o,true,(uint)Math.Round(100*ratio),100);int min=0,max=112;
        foreach(var label in labels){int y=(label.Position<3?26:64)+label.Offset;min=Math.Min(min,y);max=Math.Max(max,y+24);}
        int countdownY=countdown==null?0:(countdown.Position<3?0:88)+countdown.OffsetY;if(countdown!=null&&countdown.Enabled){min=Math.Min(min,countdownY);max=Math.Max(max,countdownY+24);}
        var result=new Bitmap(width+10,max-min,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(result)){g.Clear(Color.Transparent);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var bar=BarDesign.RenderEmissive(width,4,ratio,false,true,1,o,workspace))g.DrawImageUnscaled(bar,0,52-min);
            using(var font=new Font("Segoe UI",Math.Min(o.TextSize,22),FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(ColorTranslator.FromHtml(o.TextColor)))foreach(var label in labels)using(var format=new StringFormat{Alignment=label.Position%3==0?StringAlignment.Near:label.Position%3==1?StringAlignment.Center:StringAlignment.Far,LineAlignment=StringAlignment.Center})g.DrawString(label.Text,font,brush,new RectangleF(5,(label.Position<3?26:64)+label.Offset-min,width,24),format);
            if(countdown!=null&&countdown.Enabled)using(var font=new Font("Segoe UI",Math.Min(countdown.FontSize,24),FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(ColorTranslator.FromHtml(countdown.Color)))using(var format=new StringFormat{Alignment=countdown.Position%3==0?StringAlignment.Near:countdown.Position%3==1?StringAlignment.Center:StringAlignment.Far,LineAlignment=StringAlignment.Center})g.DrawString("4.0 s",font,brush,new RectangleF(5,countdownY-min,width,24),format);
        }return result;
    }
}

// Separate text surface keeps the existing emissive bar geometry/pixels intact.
// It shares its parent tick and opacity, and never captures mouse input.
internal sealed class ProgressTextOverlay:Form {
    readonly LayeredImage image=new LayeredImage();string key;byte alpha;Rectangle shown;bool released;long nextRaise;
    internal ProgressTextOverlay(){FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;Text="Aion 2 Helper - Progress text";}
    internal void Update(Rectangle core,EnergyBarOptions o,bool known,uint current,uint maximum,byte opacity,StaggerCountdownOptions countdown=null,double remaining=-1){
        bool activeCountdown=countdown!=null&&countdown.Enabled&&remaining>0;var labels=ProgressBarText.Labels(o,known,current,maximum);bool any=activeCountdown||labels.Count>0;
        if(!o.Enabled||opacity==0||!any||DpsDesign.ShellUiActive()){if(Visible)Hide();return;}
        int line=o.TextSize+8,countdownLine=activeCountdown?countdown.FontSize+14:0;bool above=false,below=false;int minY=0,maxY=core.Height;string labelKey="";
        foreach(var label in labels){above|=label.Position<3;below|=label.Position>=3;int y=(label.Position<3?-line-2:core.Height+2)+label.Offset;minY=Math.Min(minY,y);maxY=Math.Max(maxY,y+line);labelKey+=label.Position+":"+label.Offset+":"+label.Text+"|";}
        int countdownY=activeCountdown?(countdown.Position<3?-countdownLine-2-(above?line+4:0):core.Height+2+(below?line+4:0))+countdown.OffsetY:0;
        if(activeCountdown){minY=Math.Min(minY,countdownY);maxY=Math.Max(maxY,countdownY+countdownLine);}
        var bounds=new Rectangle(core.X,core.Y+minY,core.Width,Math.Max(1,maxY-minY));if(Bounds!=bounds)Bounds=bounds;
        string next=labelKey+"/"+o.TextSize+o.TextColor+"/"+core.Width+"/"+core.Height+"/"+(activeCountdown?remaining.ToString("F1",CultureInfo.InvariantCulture)+"/"+countdown.Position+"/"+countdown.OffsetY+"/"+countdown.FontSize+"/"+countdown.Color+"/"+countdown.Glow+"/"+countdown.GlowPercent+"/"+countdown.Shrink+"/"+countdown.MinimumScalePercent+"/"+countdown.ReferenceSeconds.ToString(CultureInfo.InvariantCulture):"");
        if(next!=key){key=next;using(var bitmap=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var font=new Font("Segoe UI",o.TextSize,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(ColorTranslator.FromHtml(o.TextColor)))foreach(var label in labels){int y=(label.Position<3?-line-2:core.Height+2)+label.Offset-minY;var rect=new RectangleF(0,y,Width,line);using(var format=Format(label.Position)){g.DrawString(label.Text,font,brush,rect,format);}}
                if(activeCountdown){double fraction=Math.Max(0,Math.Min(1,remaining/countdown.ReferenceSeconds));float size=(float)(countdown.FontSize*(countdown.Shrink?countdown.MinimumScalePercent/100.0+(1-countdown.MinimumScalePercent/100.0)*fraction:1));var rect=new RectangleF(4,countdownY-minY,Math.Max(1,Width-8),countdownLine);using(var format=Format(countdown.Position))using(var path=new GraphicsPath()){path.AddString(remaining.ToString("F1",CultureInfo.InvariantCulture)+" s",FontFamily.GenericSansSerif,0,size,rect,format);var color=ColorTranslator.FromHtml(countdown.Color);if(countdown.Glow&&countdown.GlowPercent>0)for(int stroke=6;stroke>=2;stroke-=2)using(var pen=new Pen(Color.FromArgb(Math.Min(150,40*countdown.GlowPercent/100),color),stroke)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,path);}using(var brush=new SolidBrush(color))g.FillPath(brush,path);}}
                image.Upload(bitmap);
            }alpha=0;
        }
        if(alpha!=opacity||shown!=Bounds||!Visible){image.Present(Handle,Left,Top,opacity);alpha=opacity;shown=Bounds;}if(!Visible){Show();nextRaise=0;}long now=Stopwatch.GetTimestamp();if(now>=nextRaise){nextRaise=now+Stopwatch.Frequency;OverlayNative.RaiseWithoutFocus(Handle);}
    }
    static StringFormat Format(int position){return new StringFormat{Alignment=position%3==0?StringAlignment.Near:position%3==1?StringAlignment.Center:StringAlignment.Far,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter};}
    internal void Clear(){if(Visible)Hide();}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x20|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
    protected override void Dispose(bool disposing){if(disposing&&!released){released=true;image.Dispose();}base.Dispose(disposing);}
}

internal sealed class StaggerOverlay:Form {
    readonly EnergyOverlay owner;readonly StaggerSignal signal;readonly bool preview;readonly Timer timer=new Timer();readonly Stopwatch clock=Stopwatch.StartNew();readonly BarMotion motion=new BarMotion();readonly LayeredImage image=new LayeredImage();readonly BarDesign.GlowWorkspace glow=new BarDesign.GlowWorkspace();readonly ProgressTextOverlay texts=new ProgressTextOverlay();
    StaggerOptions options=new StaggerOptions();bool editing,disposed,dragging;int hit,wake;Point mouse;Rectangle original;string imageKey;byte alpha;Point location;double raise;
    internal StaggerOverlay(EnergyOverlay parent,StaggerSignal source,bool previewMode){owner=parent;signal=source;preview=previewMode;signal.Changed=Wake;Text="Aion 2 Helper - Stagger";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;timer.Interval=250;timer.Tick+=delegate{Pulse();};}
    internal string ReadingStatus {get{var s=signal.Read();return !options.Bar.Enabled?"Stagger bar disabled":s==null?"Waiting for enemy stagger data":s.Current+" / "+s.Maximum;}}
    internal object Status {get{var s=signal.Read();double utc=DpsHistory.UtcNow();return new{enabled=options.Bar.Enabled,visible=Visible,actor=s==null?0:s.Actor,current=s==null?0:s.Current,maximum=s==null?0:s.Maximum,state=s==null?(byte?)null:s.State,available=StaggerSignal.Available(s),groggy=s!=null&&s.Groggy,sampleAgeSeconds=s==null?(double?)null:utc-s.Time,trafficAgeSeconds=s==null?(double?)null:utc-s.Traffic,zeroAgeSeconds=s==null||!s.ZeroStarted.HasValue?(double?)null:Math.Max(0,utc-s.ZeroStarted.Value),visibilityReason=!options.Bar.Enabled?"disabled":DpsDesign.ShellUiActive()?"Windows shell in foreground":editing?"editing":s==null?"no gauge":!StaggerSignal.Fresh(s,utc)?"capture inactive":!StaggerSignal.Available(s)?"stagger phase unavailable":StaggerSignal.ZeroExpired(s,utc)?"zero gauge for five seconds":motion.Opacity<=0?"initial fade":"gauge available",selection="Last directly attacked enemy; latest gauge fallback before identification",countdownEstimated=true,visualFillEstimated=s!=null&&StaggerDisplayProgress.Read(s,utc,options.Countdown.ReferenceSeconds).Refilling,displayedPercentage=motion.Value*100};}}
    Rectangle Core {get{return new Rectangle(Left+5,Top+5,Width-10,Height-10);}set{Bounds=new Rectangle(value.X-5,value.Y-5,value.Width+10,value.Height+10);}}
    internal void Configure(StaggerOptions next,bool locked){options=next.Copy();options.Normalize();editing=!locked;signal.Configure(options.Bar.Enabled&&!preview);Core=options.Bounds;int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);imageKey=null;if(options.Bar.Enabled)timer.Start();else{timer.Stop();if(Visible)Hide();texts.Clear();}Pulse();}
    void Wake(){if(disposed||!IsHandleCreated||Interlocked.CompareExchange(ref wake,1,0)!=0)return;try{BeginInvoke((Action)delegate{Interlocked.Exchange(ref wake,0);Pulse();});}catch(InvalidOperationException){Interlocked.Exchange(ref wake,0);}}
    void Pulse(){if(disposed||!options.Bar.Enabled)return;var s=signal.Read();double now=clock.Elapsed.TotalSeconds,utc=DpsHistory.UtcNow();bool known=StaggerSignal.Fresh(s,utc)&&StaggerSignal.Available(s);
        uint current=preview?65:known?s.Current:0,maximum=preview?100:known?s.Maximum:0;bool fresh=preview||known;double ratio=maximum==0?0:(double)current/maximum;
        var progress=StaggerDisplayProgress.Read(s,utc,options.Countdown.ReferenceSeconds);
        motion.Step(now,editing,fresh,ratio,current,maximum,options.Bar,known?Math.Max(0,utc-s.Time):-1);
        // A linear countdown must reach its endpoint without smoothing lag.
        // Keep the visual motion value in sync for a seamless actual refill.
        if(!preview&&progress.Refilling)motion.SnapValue(progress.Ratio);
        bool visible=(editing||preview||StaggerSignal.DisplayAvailable(s,utc))&&motion.Opacity>0&&!DpsDesign.ShellUiActive();byte opacity=visible?(byte)Math.Round(motion.Opacity*255):(byte)0;
        if(opacity==0){if(Visible)Hide();texts.Clear();}else{string key=Core.Size+"/"+(int)Math.Round(motion.Value*Core.Width)+"/"+BarDesign.EnergyColor(motion.Value,options.Bar).ToArgb()+"/"+editing+"/"+fresh;if(imageKey!=key){imageKey=key;using(var bitmap=BarDesign.RenderEmissive(Core.Width,Core.Height,motion.Value,editing,fresh,1,options.Bar,glow))image.Upload(bitmap);alpha=0;}if(alpha!=opacity||location!=Location||!Visible){image.Present(Handle,Left,Top,opacity);alpha=opacity;location=Location;}if(!Visible)Show();if(now>=raise){raise=now+1;OverlayNative.RaiseWithoutFocus(Handle);}
            texts.Update(Core,options.Bar,fresh,current,maximum,opacity,options.Countdown,progress.Remaining);
        }
        bool countdown=visible&&progress.Animating;int interval=motion.Animating&&visible?16:countdown?33:250;if(timer.Interval!=interval)timer.Interval=interval;
    }
    void Save(){var cfg=owner.Configuration;cfg.Stagger.Bounds=Core;owner.ApplyConfiguration(cfg);}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}var p=PointToClient(new Point((short)m.LParam.ToInt64(),(short)(m.LParam.ToInt64()>>16)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenTrayMenu(Cursor.Position);return;}if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){original=Core;mouse=Cursor.Position;dragging=true;Capture=true;m.Result=IntPtr.Zero;return;}}if(m.Msg==0x200&&dragging){var delta=new Point(Cursor.Position.X-mouse.X,Cursor.Position.Y-mouse.Y);Core=hit==2?new Rectangle(original.X+delta.X,original.Y+delta.Y,original.Width,original.Height):BarDesign.ResizeBounds(original,delta,hit);Pulse();m.Result=IntPtr.Zero;return;}if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;Save();m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;signal.Changed=null;timer.Stop();timer.Dispose();texts.Dispose();image.Dispose();}base.Dispose(disposing);}
}
