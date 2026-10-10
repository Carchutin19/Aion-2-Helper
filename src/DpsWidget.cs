using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class DpsOptions {
    public bool Enabled=false,Background=true;
    public string Scope="self",SelfName="",Color="#FF6403",View="damage";
    public bool RoleColors=true,AutoHide=false,Fade=true,Smooth=true;
    public bool ShowSelfAsYou=false,PinSelfFirst=false;
    public bool HistoryAutoDelete=false;public int HistoryAge=7,HistoryMaxRecords=100,HistoryMinimumSeconds=60;public string HistoryAgeUnit="days";
    public string DamageColor="#E05252",HealerColor="#43BA85",TankColor="#4A92E4";
    public double HideAfterSeconds=15,FadeSeconds=.2,SmoothingSeconds=.15;
    public int RefreshIntervalMs=500,IdleSeconds=10,BackgroundOpacity=210,MaxRows=5;
    public Rectangle Bounds=new Rectangle(40,160,380,220);
    internal DpsOptions Copy(){return (DpsOptions)MemberwiseClone();}
    internal void Normalize(){HistoryMinimumSeconds=Math.Max(0,Math.Min(3600,HistoryMinimumSeconds));HistoryAge=Math.Max(1,Math.Min(3650,HistoryAge));HistoryMaxRecords=Math.Max(1,Math.Min(1000,HistoryMaxRecords));if(HistoryAgeUnit!="hours"&&HistoryAgeUnit!="days")HistoryAgeUnit="days";if(View!="damage"&&View!="healing"&&View!="received")View="damage";if(Scope!="self"&&Scope!="party"&&Scope!="global")Scope="self";SelfName=(SelfName??"").Trim();if(SelfName.Length>24)SelfName=SelfName.Substring(0,24);if(!EnergyBarOptions.ValidColor(Color))Color="#FF6403";if(!EnergyBarOptions.ValidColor(DamageColor))DamageColor="#E05252";if(!EnergyBarOptions.ValidColor(HealerColor))HealerColor="#43BA85";if(!EnergyBarOptions.ValidColor(TankColor))TankColor="#4A92E4";HideAfterSeconds=Finite(HideAfterSeconds,15,1,300);FadeSeconds=Finite(FadeSeconds,.2,0,2);SmoothingSeconds=Finite(SmoothingSeconds,.15,0,2);RefreshIntervalMs=Math.Max(250,Math.Min(2000,RefreshIntervalMs));IdleSeconds=Math.Max(1,Math.Min(120,IdleSeconds));MaxRows=Math.Max(1,Math.Min(10,MaxRows));BackgroundOpacity=Math.Max(0,Math.Min(255,BackgroundOpacity));Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(260,Math.Min(1600,Bounds.Width)),Math.Max(110,Math.Min(900,Bounds.Height)));}
    static double Finite(double v,double fallback,double min,double max){return double.IsNaN(v)||double.IsInfinity(v)?fallback:Math.Max(min,Math.Min(max,v));}
    internal static DpsOptions Read(Dictionary<string,object> settings){object data;var o=new DpsOptions();if(settings.TryGetValue("dps",out data))try{o=new JavaScriptSerializer().ConvertToType<DpsOptions>(data)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(DpsOptions a,DpsOptions b){return a.HistoryMinimumSeconds==b.HistoryMinimumSeconds&&a.HistoryAutoDelete==b.HistoryAutoDelete&&a.HistoryAge==b.HistoryAge&&a.HistoryAgeUnit==b.HistoryAgeUnit&&a.HistoryMaxRecords==b.HistoryMaxRecords&&a.Enabled==b.Enabled&&a.Background==b.Background&&a.View==b.View&&a.Scope==b.Scope&&a.SelfName==b.SelfName&&a.Color==b.Color&&a.Bounds==b.Bounds&&a.RefreshIntervalMs==b.RefreshIntervalMs&&a.IdleSeconds==b.IdleSeconds&&a.BackgroundOpacity==b.BackgroundOpacity&&a.MaxRows==b.MaxRows&&a.RoleColors==b.RoleColors&&a.DamageColor==b.DamageColor&&a.HealerColor==b.HealerColor&&a.TankColor==b.TankColor&&a.AutoHide==b.AutoHide&&a.HideAfterSeconds==b.HideAfterSeconds&&a.Fade==b.Fade&&a.FadeSeconds==b.FadeSeconds&&a.Smooth==b.Smooth&&a.SmoothingSeconds==b.SmoothingSeconds&&a.ShowSelfAsYou==b.ShowSelfAsYou&&a.PinSelfFirst==b.PinSelfFirst;}
}

// Only visible rows animate. Packet polling retains its configured interval;
// the faster timer runs during visual transitions, not continuously at idle.
internal sealed class DpsMotion {
    sealed class Track {internal double Value,Target,Changed;}
    readonly Dictionary<uint,Track> tracks=new Dictionary<uint,Track>();
    readonly List<uint> removed=new List<uint>();
    internal readonly Dictionary<uint,double> Ratios=new Dictionary<uint,double>();
    internal double Opacity,LastActivity,HideDeadline;internal bool Animating;
    string view;bool started,targetVisible,needsIdentification;double previous,encounter=-1,created;
    internal void Set(DpsSignal.Snapshot snapshot,DpsOptions options,double now,double utc){
        if(!started){created=previous=now;started=true;}
        needsIdentification=snapshot.NeedsIdentification;
        if(encounter!=snapshot.FirstImpact||view!=options.View){tracks.Clear();Ratios.Clear();encounter=snapshot.FirstImpact;view=options.View;}
        LastActivity=snapshot.LastImpact>0?now-Math.Max(0,utc-snapshot.LastImpact):created;
        removed.Clear();foreach(uint id in tracks.Keys){bool present=false;foreach(var row in snapshot.Rows)if(row.Actor==id){present=true;break;}if(!present)removed.Add(id);}
        foreach(uint id in removed){tracks.Remove(id);Ratios.Remove(id);}
        long maximum=DpsDesign.MaximumTotal(snapshot,options.View);
        for(int i=0;i<Math.Min(options.MaxRows,snapshot.Rows.Count);i++){var row=snapshot.Rows[i];double ratio=Math.Max(0,Math.Min(1,DpsSignal.Total(row,options.View)/(double)maximum));Track t;
            if(!tracks.TryGetValue(row.Actor,out t)){t=new Track{Changed=now,Value=options.Smooth?0:ratio};tracks[row.Actor]=t;}
            if(t.Target!=ratio){t.Target=ratio;t.Changed=now;}Ratios[row.Actor]=t.Value;
        }
    }
    internal bool Step(double now,bool editing,DpsOptions options){
        double dt=Math.Max(0,now-previous);previous=now;bool pixels=false;Animating=false;
        HideDeadline=LastActivity+options.HideAfterSeconds;
        bool show=needsIdentification||editing||!options.AutoHide||now<HideDeadline;
        if(show!=targetVisible){targetVisible=show;dt=0;}
        double old=Opacity,fade=options.Fade?options.FadeSeconds:0;
        Opacity=needsIdentification||editing||fade<=0?(show?1:0):Math.Max(0,Math.Min(1,Opacity+(show?1:-1)*dt/fade));
        if(Opacity!=(show?1:0))Animating=true;
        foreach(var pair in tracks){var t=pair.Value;double value=t.Value,smooth=options.Smooth?options.SmoothingSeconds:0;
            t.Value=smooth<=0?t.Target:t.Value+(t.Target-t.Value)*(1-Math.Exp(-Math.Min(dt,Math.Max(0,now-t.Changed))/smooth));
            if(Math.Abs(t.Target-t.Value)<.001)t.Value=t.Target;
            if(Math.Abs(t.Target-t.Value)>.001&&Opacity>0)Animating=true;
            if(Math.Abs(value-t.Value)>.00001)pixels=true;Ratios[pair.Key]=t.Value;
        }
        return pixels;
    }
    internal void Reset(){tracks.Clear();Ratios.Clear();Opacity=0;started=false;encounter=-1;Animating=false;targetVisible=false;needsIdentification=false;}
}

internal sealed class DpsPaintWorkspace:IDisposable {
    internal readonly FpsDesign.Workspace Pixels=new FpsDesign.Workspace();internal Font Body,Small;internal Bitmap Panel;float scale=-1;
    internal void Prepare(int width,int height,float next){Pixels.Prepare(width,height);if(scale==next)return;scale=next;if(Body!=null){Body.Dispose();Small.Dispose();}Body=new Font("Segoe UI",Math.Max(10,12*scale),FontStyle.Regular,GraphicsUnit.Pixel);Small=new Font("Segoe UI",Math.Max(9,10*scale),FontStyle.Regular,GraphicsUnit.Pixel);}
    internal void ReplacePanel(Bitmap panel){if(Panel!=null)Panel.Dispose();Panel=panel;}
    public void Dispose(){Pixels.Dispose();if(Panel!=null)Panel.Dispose();if(Body!=null){Body.Dispose();Small.Dispose();}}
}

internal static class DpsDesign {
    [StructLayout(LayoutKind.Sequential)] struct NativeRect {internal int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct AppBarData {internal uint Size;internal IntPtr Window;internal uint Callback,Edge;internal NativeRect Bounds;internal IntPtr Parameter;}
    [DllImport("shell32.dll")] static extern IntPtr SHAppBarMessage(uint message,ref AppBarData data);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    static Rectangle lastScreen,lastWorking,safeArea;static double areaExpires;
    static IntPtr lastForeground;static bool shellActive;
    internal static bool ShellUiActive(){IntPtr foreground=GetForegroundWindow();if(foreground==lastForeground)return shellActive;lastForeground=foreground;shellActive=false;uint process;GetWindowThreadProcessId(foreground,out process);try{using(var p=Process.GetProcessById((int)process)){string name=p.ProcessName;shellActive=name.Equals("explorer",StringComparison.OrdinalIgnoreCase)||name.Equals("StartMenuExperienceHost",StringComparison.OrdinalIgnoreCase)||name.Equals("SearchHost",StringComparison.OrdinalIgnoreCase);}}catch(ArgumentException){}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}return shellActive;}
    static readonly TextFormatFlags Left=TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix;
    static readonly TextFormatFlags Right=Left|TextFormatFlags.Right;
    internal static Color RowColor(DpsSignal.Row row,DpsOptions options){return ColorTranslator.FromHtml(!options.RoleColors||row.Role==0?options.Color:row.Role==3?options.TankColor:row.Role==2?options.HealerColor:options.DamageColor);}
    internal static long MaximumDamage(DpsSignal.Snapshot snapshot){long maximum=1;foreach(var row in snapshot.Rows)maximum=Math.Max(maximum,row.Damage);return maximum;}
    internal static long MaximumTotal(DpsSignal.Snapshot snapshot,string view){long maximum=1;foreach(var row in snapshot.Rows)maximum=Math.Max(maximum,DpsSignal.Total(row,view));return maximum;}
    internal static string Percent(double? value){return value.HasValue?value.Value.ToString("0.0",CultureInfo.InvariantCulture)+"%":"—";}
    internal static Rectangle TabBounds(int width){float scale=Math.Max(.8f,Math.Min(3f,width/520f));int pad=(int)(12*Math.Max(.8f,Math.Min(3f,width/380f)));return new Rectangle(pad,(int)(28*scale),width-pad*2,Math.Max(24,(int)(23*scale)));}
    internal static int HeaderHeight(int width){return TabBounds(width).Bottom+4;}
    internal static int FooterHeight(int width,int height){int room=height-HeaderHeight(width)-42;return room<18?0:Math.Min(room,(int)(25*Math.Max(.8f,Math.Min(3f,width/380f))));}
    internal static string TabView(int width,Point point){Rectangle bounds=TabBounds(width);if(!bounds.Contains(point))return null;int index=Math.Min(2,(point.X-bounds.Left)*3/bounds.Width);return index==0?"damage":index==1?"healing":"received";}
    static void PaintTabs(Graphics g,Graphics mask,int width,DpsOptions options,string language,Font font){Rectangle bounds=TabBounds(width);g.SmoothingMode=SmoothingMode.AntiAlias;
        for(int i=0;i<3;i++){int left=bounds.Left+bounds.Width*i/3,right=bounds.Left+bounds.Width*(i+1)/3;string view=i==0?"damage":i==1?"healing":"received";bool selected=options.View==view;
            using(var path=FpsDesign.Round(left+1,bounds.Top+1,right-left-2,bounds.Height-2,6))using(var brush=new SolidBrush(selected?Color.FromArgb(180,48,73,73):Color.FromArgb(100,15,20,27)))g.FillPath(brush,path);
            if(selected)using(var pen=new Pen(Color.FromArgb(230,103,199,176),2))g.DrawLine(pen,left+8,bounds.Bottom-2,right-8,bounds.Bottom-2);
            TextRenderer.DrawText(mask,UiLanguage.Text(i==0?"Damage":i==1?"Heals":"Received",language),font,new Rectangle(left+2,bounds.Top,right-left-4,bounds.Height-2),selected?Color.White:Color.FromArgb(160,160,160),Left|TextFormatFlags.HorizontalCenter);
        }
    }
    static string Amount(double value,int width,Font font){string full=value.ToString("N0",CultureInfo.InvariantCulture);if(TextRenderer.MeasureText(full,font,Size.Empty,TextFormatFlags.NoPadding).Width<=width)return full;return value>=1000000?(value/1000000).ToString("0.#",CultureInfo.InvariantCulture)+"M":value>=1000?(value/1000).ToString("0.#",CultureInfo.InvariantCulture)+"K":full;}
    internal static string DisplayName(DpsSignal.Row row,DpsSignal.Snapshot snapshot,DpsOptions options,string language){return row.Actor==snapshot.SelfActor&&(options.ShowSelfAsYou||row.Name==null)?UiLanguage.Text("You",language):row.Name??string.Format(UiLanguage.Text("Player {0}",language),row.Actor);}
    internal static Bitmap Render(int width,int height,DpsSignal.Snapshot snapshot,DpsOptions options,bool editing,string language,DpsPaintWorkspace workspace,IDictionary<uint,double> ratios=null,bool includeBars=true){
        float scale=Math.Max(.8f,Math.Min(3f,width/380f));workspace.Prepare(width,height,Math.Max(.8f,Math.Min(3f,width/520f)));var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
        int pad=(int)(12*scale),titleHeight=TabBounds(width).Top,header=HeaderHeight(width),footer=FooterHeight(width,height),line=Math.Max(21,(height-header-footer)/(options.MaxRows+1));
        using(var g=Graphics.FromImage(bitmap)){
            if(editing)using(var target=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(target,0,0,width,height);
            if(options.Background){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=FpsDesign.Round(.5f,.5f,width-1,height-1,10*scale))using(var brush=new SolidBrush(Color.FromArgb(options.BackgroundOpacity,9,12,17)))g.FillPath(brush,path);}
            if(editing)using(var pen=new Pen(Color.FromArgb(160,103,199,176))){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,1,1,width-3,height-3);}
            var mg=workspace.Pixels.MaskGraphics;mg.Clear(Color.Black);
            string title=options.View=="damage"?"DPS":options.View=="healing"?"Healing done":"Healing received";
            TextRenderer.DrawText(mg,UiLanguage.Text(title,language)+" · Alpha",workspace.Body,new Rectangle(pad,3,(int)(width*.5),titleHeight-3),Color.White,Left);
            string scope=UiLanguage.Text(options.Scope=="self"?"Self":options.Scope=="party"?"Party":"Nearby players",language);
            TextRenderer.DrawText(mg,scope+" · "+snapshot.Duration.ToString("0.0",CultureInfo.InvariantCulture)+" s",workspace.Small,new Rectangle(pad,3,width-pad*2,titleHeight-3),Color.White,Right);
            PaintTabs(g,mg,width,options,language,workspace.Small);
            int available=width-pad*2,nameWidth=(int)(available*.30),damageWidth=(int)(available*.22),dpsWidth=(int)(available*.16),critWidth=(int)(available*.16),rateWidth=available-nameWidth-damageWidth-dpsWidth-critWidth;
            int amountX=pad+nameWidth,rateX=amountX+damageWidth,critX=rateX+dpsWidth,chanceX=critX+critWidth;
            int y=header;TextRenderer.DrawText(mg,UiLanguage.Text("Name",language),workspace.Small,new Rectangle(pad,y,nameWidth,line),Color.White,Left);
            TextRenderer.DrawText(mg,UiLanguage.Text(options.View=="damage"?"Damage":"Healing",language),workspace.Small,new Rectangle(amountX,y,damageWidth,line),Color.White,Right);
            TextRenderer.DrawText(mg,options.View=="damage"?"DPS":"HPS",workspace.Small,new Rectangle(rateX,y,dpsWidth,line),Color.White,Right);
            TextRenderer.DrawText(mg,UiLanguage.Text("Crit. %",language),workspace.Small,new Rectangle(critX,y,critWidth,line),Color.White,Right);
            TextRenderer.DrawText(mg,UiLanguage.Text("Crit. rate",language),workspace.Small,new Rectangle(chanceX,y,rateWidth,line),Color.White,Right);y+=line;
            if(snapshot.NeedsIdentification){TextRenderer.DrawText(mg,UiLanguage.Text("Dash once to identify your character",language),workspace.Small,new Rectangle(pad,y,width-pad*2,line-3),Color.White,Left);y+=line;}
            int count=Math.Min(options.MaxRows-(snapshot.NeedsIdentification?1:0),snapshot.Rows.Count);
            for(int i=0;i<count&&y+line<=height-footer;i++,y+=line){var row=snapshot.Rows[i];
                string name=(i+1)+". "+DisplayName(row,snapshot,options,language);TextRenderer.DrawText(mg,name,workspace.Body,new Rectangle(pad,y,nameWidth,line-3),Color.White,Left);
                TextRenderer.DrawText(mg,Amount(DpsSignal.Total(row,options.View),damageWidth-4,workspace.Body),workspace.Body,new Rectangle(amountX,y,damageWidth,line-3),Color.White,Right);
                TextRenderer.DrawText(mg,Amount(options.View=="healing"?row.Hps:options.View=="received"?row.ReceivedHps:row.Dps,dpsWidth-4,workspace.Body),workspace.Body,new Rectangle(rateX,y,dpsWidth,line-3),Color.White,Right);
                TextRenderer.DrawText(mg,Percent(DpsSignal.CriticalShare(row,options.View)),workspace.Body,new Rectangle(critX,y,critWidth,line-3),Color.White,Right);
                TextRenderer.DrawText(mg,Percent(DpsSignal.CriticalRate(row,options.View)),workspace.Body,new Rectangle(chanceX,y,rateWidth,line-3),Color.White,Right);
                int barWidth=width-pad*2,barY=y+line-3;using(var track=new SolidBrush(Color.FromArgb(100,0,0,0)))g.FillRectangle(track,pad,barY,barWidth,2);
            }
            if(count==0&&!snapshot.NeedsIdentification)TextRenderer.DrawText(mg,UiLanguage.Text(snapshot.Status,language),workspace.Small,new Rectangle(pad,y,width-pad*2,Math.Max(20,height-footer-y)),Color.White,Left);
            string note=UiLanguage.Text(options.View=="damage"?"Partial damage · identified DoT included":"Raw healing · supported skills only",language);
            if(footer>0)TextRenderer.DrawText(mg,note,workspace.Small,new Rectangle(pad,height-footer,width-pad*2,footer),Color.White,Left);
            // GDI glyph coverage converted to alpha, avoiding ClearType fringes
            // and resizing an existing image. Same crisp text path as FPS.
            var mask=workspace.Pixels.Mask;var data=mask.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            try{var pixels=workspace.Pixels.Pixels;int length=data.Stride*height;Marshal.Copy(data.Scan0,pixels,0,length);for(int p=0;p<length;p+=4){byte alpha=Math.Max(pixels[p],Math.Max(pixels[p+1],pixels[p+2]));pixels[p]=pixels[p+1]=pixels[p+2]=238;pixels[p+3]=alpha;}Marshal.Copy(pixels,0,data.Scan0,length);}finally{mask.UnlockBits(data);}g.DrawImageUnscaled(mask,0,0);if(includeBars)PaintBars(g,width,height,snapshot,options,ratios);
        }return bitmap;
    }
    static void PaintBars(Graphics g,int width,int height,DpsSignal.Snapshot snapshot,DpsOptions options,IDictionary<uint,double> ratios){g.SmoothingMode=SmoothingMode.None;float scale=Math.Max(.8f,Math.Min(3f,width/380f));int pad=(int)(12*scale),header=HeaderHeight(width),footer=FooterHeight(width,height),line=Math.Max(21,(height-header-footer)/(options.MaxRows+1));int y=header+line+(snapshot.NeedsIdentification?line:0);long maximum=MaximumTotal(snapshot,options.View);
        for(int i=0;i<Math.Min(options.MaxRows-(snapshot.NeedsIdentification?1:0),snapshot.Rows.Count)&&y+line<=height-footer;i++,y+=line){var row=snapshot.Rows[i];double ratio=DpsSignal.Total(row,options.View)/(double)maximum,value;if(ratios!=null&&ratios.TryGetValue(row.Actor,out value))ratio=value;using(var color=new SolidBrush(RowColor(row,options)))g.FillRectangle(color,pad,y+line-3,(int)((width-pad*2)*Math.Max(0,Math.Min(1,ratio))),2);}
    }
    internal static Bitmap Compose(DpsPaintWorkspace workspace,DpsSignal.Snapshot snapshot,DpsOptions options,IDictionary<uint,double> ratios){var bitmap=(Bitmap)workspace.Panel.Clone();using(var g=Graphics.FromImage(bitmap))PaintBars(g,bitmap.Width,bitmap.Height,snapshot,options,ratios);return bitmap;}
    internal static Rectangle Resize(Rectangle start,Point delta,int hit){bool left=hit==10||hit==13||hit==16,right=hit==11||hit==14||hit==17,top=hit==12||hit==13||hit==14,bottom=hit==15||hit==16||hit==17;int w=Math.Max(260,Math.Min(1600,start.Width+(left?-delta.X:right?delta.X:0))),h=Math.Max(110,Math.Min(900,start.Height+(top?-delta.Y:bottom?delta.Y:0)));return new Rectangle(left?start.Right-w:start.Left,top?start.Bottom-h:start.Top,w,h);}
    internal static Rectangle Constrain(Rectangle bounds,Rectangle area){int width=Math.Min(bounds.Width,area.Width),height=Math.Min(bounds.Height,area.Height);return new Rectangle(Math.Max(area.Left,Math.Min(area.Right-width,bounds.Left)),Math.Max(area.Top,Math.Min(area.Bottom-height,bounds.Top)),width,height);}
    internal static Rectangle UsableBounds(Rectangle bounds){var screen=Screen.FromRectangle(bounds);Rectangle full=screen.Bounds,working=screen.WorkingArea;double now=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
        if(now>=areaExpires||full!=lastScreen||working!=lastWorking){safeArea=working;lastScreen=full;lastWorking=working;areaExpires=now+1;
            // Windows returns the full monitor as WorkingArea for autohide.
            // Reserve its real appbar thickness so the tray remains reachable.
            for(uint edge=0;edge<4;edge++){var data=new AppBarData{Size=(uint)Marshal.SizeOf(typeof(AppBarData)),Edge=edge,Bounds=new NativeRect{Left=full.Left,Top=full.Top,Right=full.Right,Bottom=full.Bottom}};IntPtr bar=SHAppBarMessage(11,ref data);NativeRect native;
                if(bar==IntPtr.Zero||!GetWindowRect(bar,out native))continue;int thickness=Math.Max(2,Math.Min(256,edge==0||edge==2?native.Right-native.Left:native.Bottom-native.Top));
                if(edge==0&&safeArea.Left==full.Left)safeArea=Rectangle.FromLTRB(full.Left+thickness,safeArea.Top,safeArea.Right,safeArea.Bottom);
                if(edge==1&&safeArea.Top==full.Top)safeArea=Rectangle.FromLTRB(safeArea.Left,full.Top+thickness,safeArea.Right,safeArea.Bottom);
                if(edge==2&&safeArea.Right==full.Right)safeArea=Rectangle.FromLTRB(safeArea.Left,safeArea.Top,full.Right-thickness,safeArea.Bottom);
                if(edge==3&&safeArea.Bottom==full.Bottom)safeArea=Rectangle.FromLTRB(safeArea.Left,safeArea.Top,safeArea.Right,full.Bottom-thickness);
            }
        }return Constrain(bounds,safeArea);
    }
    internal static DpsSignal.Snapshot Sample(DpsOptions options=null){var s=new DpsSignal.Snapshot{SelfActor=2,Duration=24.5,Status="In combat",Active=true};s.Rows.Add(new DpsSignal.Row{Actor=1,Role=1,Name="Player One",HighestHit=5400,TaggedDamageEvents=20,BackHits=12,FrontHits=6,DoubleHits=3,PerfectHits=5,BackCriticalHits=4,FrontCriticalHits=2,BackDoubleCriticalHits=2,FrontDoubleCriticalHits=1,BackPerfectHits=3,FrontPerfectHits=2,Damage=38600,Dps=1576,DamageEvents=20,Criticals=6,DamagePrimary=35000,DamageCritical=15000,Healing=4000,Hps=163,HealingEvents=5,HealingCriticals=1,HealingPrimary=4000,HealingCritical=1000,HealingReceived=22000,ReceivedHps=898,ReceivedEvents=12,ReceivedCriticals=3,ReceivedPrimary=22000,ReceivedCritical=8000});s.Rows.Add(new DpsSignal.Row{Actor=2,Role=2,Name="Player Two",HighestHit=3600,TaggedDamageEvents=18,BackHits=10,FrontHits=5,DoubleHits=2,PerfectHits=3,BackCriticalHits=3,FrontCriticalHits=1,BackDoubleCriticalHits=1,BackPerfectHits=2,FrontPerfectHits=1,Damage=27100,Dps=1106,DamageEvents=18,Criticals=4,DamagePrimary=26000,DamageCritical=9000,Healing=43000,Hps=1755,HealingEvents=15,HealingCriticals=5,HealingPrimary=38000,HealingCritical=19000,HealingReceived=13000,ReceivedHps=531,ReceivedEvents=10,ReceivedCriticals=2,ReceivedPrimary=13000,ReceivedCritical=4000});s.Rows.Add(new DpsSignal.Row{Actor=3,Role=3,Name="Player Three",HighestHit=3000,TaggedDamageEvents=12,BackHits=4,FrontHits=8,DoubleHits=1,PerfectHits=2,BackCriticalHits=1,FrontCriticalHits=1,FrontDoubleCriticalHits=1,BackPerfectHits=1,FrontPerfectHits=1,Damage=20300,Dps=829,DamageEvents=12,Criticals=2,DamagePrimary=20000,DamageCritical=6000,Healing=12000,Hps=490,HealingEvents=8,HealingCriticals=2,HealingPrimary=10000,HealingCritical=3000,HealingReceived=24000,ReceivedHps=980,ReceivedEvents=14,ReceivedCriticals=3,ReceivedPrimary=22000,ReceivedCritical=9000});DpsSignal.SortRows(s.Rows,s.SelfActor,options!=null&&options.PinSelfFirst,options==null?"damage":options.View);return s;}
}

// Only this small input surface accepts clicks while the main layered widget
// keeps WS_EX_TRANSPARENT. It has no timer and cannot activate over the game.
internal sealed class DpsTabInput:Form {
    readonly LayeredImage image=new LayeredImage();readonly Action<Point> select;Size uploaded;Point presented;bool pressed,disposed;int pressedTab;
    internal DpsTabInput(Form parent,Action<Point> clicked){Owner=parent;select=clicked;Text="Aion 2 Helper - Combat tabs";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;Cursor=Cursors.Hand;}
    internal void Sync(Rectangle bounds,bool show){if(!show){HideInput();return;}if(Bounds!=bounds)Bounds=bounds;bool resized=uploaded!=bounds.Size;if(resized){using(var b=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(b))g.Clear(Color.FromArgb(1,0,0,0));image.Upload(b);}uploaded=bounds.Size;}
        if(resized||!Visible||presented!=bounds.Location){image.Present(Handle,bounds.X,bounds.Y,255);presented=bounds.Location;}if(!Visible)Show();
    }
    internal void HideInput(){pressed=false;Capture=false;if(Visible)Hide();}
    internal void Raise(){if(Visible)OverlayNative.RaiseWithoutFocus(Handle);}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40020;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){pressed=true;pressedTab=Math.Min(2,e.X*3/Math.Max(1,Width));Capture=true;}base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){bool click=pressed&&e.Button==MouseButtons.Left&&ClientRectangle.Contains(e.Location)&&pressedTab==Math.Min(2,e.X*3/Math.Max(1,Width));pressed=false;Capture=false;if(click)select(e.Location);base.OnMouseUp(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)pressed=false;base.OnMouseCaptureChanged(e);}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;image.Dispose();}base.Dispose(disposing);}
}

internal sealed class DpsOverlay:Form {
    readonly EnergyOverlay owner;readonly DpsSignal signal;readonly bool preview;readonly LayeredImage image=new LayeredImage();readonly DpsPaintWorkspace workspace=new DpsPaintWorkspace();readonly Timer timer=new Timer();readonly DpsMotion motion=new DpsMotion();DpsSignal.Snapshot cached;double nextPoll;DpsOptions options=new DpsOptions();bool editing,closing,rendering,dragging,valid,disposed;int hit,imageX=int.MinValue,imageY,imageAlpha=-1;Rectangle dragStart;Point mouseStart;long revision=-1,raise;double duration=-1;string language,status;
    readonly DpsTabInput tabs;
    internal DpsOverlay(EnergyOverlay parent,DpsSignal reader,bool previewMode){owner=parent;signal=reader;preview=previewMode;Text="Aion 2 Helper - DPS";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;tabs=new DpsTabInput(this,delegate(Point p){SelectTab(PointToClient(tabs.PointToScreen(p)));});timer.Tick+=delegate{UpdateWidget();};FormClosing+=delegate{closing=true;tabs.HideInput();};}
    internal void Configure(DpsOptions value,bool locked){bool changed=!DpsOptions.Same(options,value)||editing==locked||language!=owner.Language;options=value.Copy();options.Normalize();editing=!locked;language=owner.Language;if(Bounds!=options.Bounds)Bounds=options.Bounds;if(changed)valid=false;
        nextPoll=0;int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);if(preview)return;if(!options.Enabled){timer.Stop();motion.Reset();cached=null;tabs.HideInput();Hide();return;}timer.Interval=options.RefreshIntervalMs;timer.Start();UpdateWidget();}
    internal void SelectTab(Point point){string view=DpsDesign.TabView(Width,point);if(view==null||!options.Enabled||view==owner.Configuration.Dps.View)return;var cfg=owner.Configuration;cfg.Dps.View=view;owner.ApplyConfiguration(cfg);}
    internal string ReadingStatus {get{return signal.Read((DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds).Status;}}
    internal object WindowStatus {get{return new{visible=Visible,opacity=motion.Opacity,x=Left,y=Top,width=Width,height=Height};}}
    void UpdateWidget(){if(closing||rendering||!options.Enabled)return;rendering=true;try{long tick=Stopwatch.GetTimestamp();double now=tick/(double)Stopwatch.Frequency;
        if(preview||cached==null||now>=nextPoll){double utc=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;cached=signal.Read(utc);motion.Set(cached,options,now,utc);nextPoll=now+options.RefreshIntervalMs/1000.0;}
        bool shell=!preview&&DpsDesign.ShellUiActive();bool barsChanged=motion.Step(now,editing||preview,options);var s=cached;double stamp=Math.Round(s.Duration,1);bool panelChanged=!valid||revision!=s.Revision||duration!=stamp||status!=s.Status;bool changed=panelChanged||barsChanged;byte alpha=(byte)Math.Round(motion.Opacity*255);
        if(changed&&(alpha>0||preview)&&!shell){if(panelChanged)workspace.ReplacePanel(DpsDesign.Render(Width,Height,s,options,editing,language,workspace,null,false));using(var bitmap=DpsDesign.Compose(workspace,s,options,motion.Ratios))image.Upload(bitmap);revision=s.Revision;duration=stamp;status=s.Status;valid=true;}
        else if(changed)valid=false;
        if((alpha==0||shell)&&!preview){if(Visible)Hide();imageAlpha=-1;}
        else {if(changed||Left!=imageX||Top!=imageY||imageAlpha!=alpha||!Visible&&!preview){image.Present(Handle,Left,Top,alpha);imageX=Left;imageY=Top;imageAlpha=alpha;}if(!Visible&&!preview)Show();if(!preview&&!owner.TrayMenuOpen&&(tick-raise)/(double)Stopwatch.Frequency>=1){OverlayNative.RaiseWithoutFocus(Handle);raise=tick;}}
        if(!preview){Rectangle tabBounds=DpsDesign.TabBounds(Width);tabBounds.Offset(Left,Top);tabs.Sync(tabBounds,Visible&&alpha>0&&!shell&&!owner.TrayMenuOpen&&!dragging);if(tick==raise)tabs.Raise();double wait=nextPoll-now;if(motion.Animating&&!shell)wait=Math.Min(wait,.033);else if(options.AutoHide&&!editing&&now<motion.HideDeadline)wait=Math.Min(wait,motion.HideDeadline-now);timer.Interval=Math.Max(1,Math.Min(options.RefreshIntervalMs,(int)Math.Ceiling(wait*1000)));}
    }finally{rendering=false;}}
    void SaveGeometry(){var c=owner.Configuration;c.Dps.Bounds=Bounds;owner.ApplyConfiguration(c);}
    internal void VerifyTabs(){var original=owner.Configuration;var cfg=original.Copy();cfg.Locked=true;cfg.Dps.Enabled=true;cfg.Dps.View="damage";owner.ApplyConfiguration(cfg);Configure(cfg.Dps,true);Rectangle area=DpsDesign.TabBounds(Width);
        for(int i=0;i<3;i++){string view=i==0?"damage":i==1?"healing":"received";Point point=new Point(area.Left+area.Width*(2*i+1)/6,area.Top+area.Height/2);if(DpsDesign.TabView(Width,point)!=view)throw new Exception("Tab geometry disagrees with its view");SelectTab(point);var expected=cfg.Copy();expected.Dps.View=view;if(!ConfigurationHistory.Same(owner.Configuration,expected))throw new Exception("Widget tab must preserve lock, geometry and other modules");}
        SelectTab(new Point(area.Left,area.Bottom+10));if(owner.Configuration.Dps.View!="received")throw new Exception("Clicks outside tabs must not change view");owner.UndoConfiguration();if(owner.Configuration.Dps.View!="healing")throw new Exception("Widget tab must support undo");owner.RedoConfiguration();if(owner.Configuration.Dps.View!="received")throw new Exception("Widget tab must support redo");
        Rectangle screen=area;screen.Offset(Left,Top);tabs.Sync(screen,true);if(tabs.Bounds!=screen||(OverlayNative.GetWindowLong(tabs.Handle,-20)&0x08000000)==0||(OverlayNative.GetWindowLong(Handle,-20)&0x20)==0||tabs.ShowInTaskbar)throw new Exception("Only the tab strip may receive input without activating or appearing in taskbar");
        IntPtr pointParam=new IntPtr((area.Height/2<<16)|(area.Width/6));OverlayNative.SendMessage(tabs.Handle,0x201,new IntPtr(1),pointParam);OverlayNative.SendMessage(tabs.Handle,0x202,IntPtr.Zero,pointParam);if(owner.Configuration.Dps.View!="damage")throw new Exception("Native tab mouse click did not switch view");if(OverlayNative.SendMessage(tabs.Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()!=3)throw new Exception("Tab clicks must not activate over the game");
        tabs.Sync(screen,false);if(tabs.Visible)throw new Exception("Hidden widget must hide its input strip");cfg.Dps.Enabled=false;owner.ApplyConfiguration(cfg);Configure(cfg.Dps,true);SelectTab(new Point(area.Left+area.Width/2,area.Top+2));if(owner.Configuration.Dps.View!="damage")throw new Exception("Disabled widget must ignore tab input");owner.ApplyConfiguration(original);
    }
    internal void VerifyCache(){Configure(new DpsOptions{Enabled=true},true);UpdateWidget();int uploads=image.Uploads;for(int i=0;i<1000;i++)UpdateWidget();if(image.Uploads!=uploads)throw new Exception("Idle DPS must not redraw unchanged data");for(int i=0;i<50;i++){Left++;UpdateWidget();}if(image.Uploads!=uploads)throw new Exception("Moving DPS must reuse bitmap");var o=options.Copy();o.Bounds=new Rectangle(30,40,420,250);Configure(o,true);UpdateWidget();if(image.Uploads!=uploads+1)throw new Exception("Resize must redraw DPS once");using(var process=Process.GetCurrentProcess()){uint before=OverlayNative.GetGuiResources(process.Handle,0);for(int i=0;i<200;i++){signal.ResetEncounter();UpdateWidget();}if(OverlayNative.GetGuiResources(process.Handle,0)>before+3)throw new Exception("DPS GDI resources must remain bounded");}}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;closing=true;timer.Stop();timer.Dispose();tabs.Dispose();image.Dispose();workspace.Dispose();}base.Dispose(disposing);}
    protected override void WndProc(ref Message m){
        if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenTrayMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}var p=PointToClient(new Point((short)(m.LParam.ToInt64()&0xffff),(short)((m.LParam.ToInt64()>>16)&0xffff)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){dragging=true;dragStart=Bounds;mouseStart=Cursor.Position;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){var delta=new Point(Cursor.Position.X-mouseStart.X,Cursor.Position.Y-mouseStart.Y);Bounds=DpsDesign.UsableBounds(hit==2?new Rectangle(dragStart.X+delta.X,dragStart.Y+delta.Y,dragStart.Width,dragStart.Height):DpsDesign.Resize(dragStart,delta,hit));valid=false;UpdateWidget();m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;SaveGeometry();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x46){var p=(OverlayNative.WindowPos)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.WindowPos));if((p.Flags&1)==0){p.Width=Math.Max(260,Math.Min(1600,p.Width));p.Height=Math.Max(110,Math.Min(900,p.Height));Marshal.StructureToPtr(p,m.LParam,false);}m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x24){var p=(OverlayNative.MinMaxInfo)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.MinMaxInfo));p.MinTrackSize=new OverlayNative.Point(260,110);p.MaxTrackSize=new OverlayNative.Point(1600,900);Marshal.StructureToPtr(p,m.LParam,false);m.Result=IntPtr.Zero;return;}base.WndProc(ref m);
    }
}
