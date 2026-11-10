using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Script.Serialization;

internal sealed class CombatLogOptions {
    internal const int Capacity = 128;
    internal const int MinWidth=300, MaxWidth=1000, MinHeight=114, MaxHeight=560;
    public bool Enabled, Blur, AutoHide, NewestAtBottom, ShowTime = true, ShowHitType = true;
    public int Format, BackgroundOpacity = 78, IdleSeconds = 30;
    public Rectangle Bounds = new Rectangle(28, 360, 570, 250);

    internal bool ShowIcon { get { return Format != 2; } }
    internal bool ShowName { get { return Format != 1; } }
    internal CombatLogOptions Copy() { return (CombatLogOptions)MemberwiseClone(); }
    internal static bool Same(CombatLogOptions a, CombatLogOptions b) {
        return a.Enabled == b.Enabled && a.Blur == b.Blur && a.AutoHide==b.AutoHide && a.IdleSeconds==b.IdleSeconds
            && a.ShowTime == b.ShowTime && a.ShowHitType == b.ShowHitType && a.NewestAtBottom == b.NewestAtBottom
            && a.Format == b.Format && a.BackgroundOpacity == b.BackgroundOpacity && a.Bounds == b.Bounds;
    }
    internal void Normalize() {
        Format = Math.Max(0, Math.Min(2, Format));
        BackgroundOpacity = Math.Max(0, Math.Min(100, BackgroundOpacity));
        IdleSeconds = Math.Max(1,Math.Min(300,IdleSeconds));
        Bounds = new Rectangle(new Point(Math.Max(-100000, Math.Min(100000, Bounds.X)), Math.Max(-100000, Math.Min(100000, Bounds.Y))),
            LimitSize(Bounds.Size));
    }
    internal static Size LimitSize(Size size) {
        return new Size(Math.Max(MinWidth,Math.Min(MaxWidth,size.Width)),Math.Max(MinHeight,Math.Min(MaxHeight,size.Height)));
    }
    internal bool ShowForActivity(CombatLogEntry[] entries,bool editing,double now) {
        return editing||!AutoHide||entries.Length!=0&&now-entries[entries.Length-1].Time<IdleSeconds;
    }
    internal static CombatLogOptions Read(Dictionary<string,object> root) {
        var options = new CombatLogOptions();
        object value;
        if (root.TryGetValue("combatLog", out value)) {
            try { options = new JavaScriptSerializer().ConvertToType<CombatLogOptions>(value) ?? options; }
            catch (ArgumentException) {}
            catch (InvalidOperationException) {}
        }
        options.Normalize();
        return options;
    }
}

internal struct CombatLogEntry {
    internal readonly double Time;
    internal readonly uint Skill, Amount;
    internal readonly FloatingHitLabel Label;
    internal readonly long Sequence;
    internal double AppearedAt;
    internal CombatLogEntry(double time, uint skill, uint amount, FloatingHitLabel label, long sequence=0) {
        Time=time; Skill=skill; Amount=amount; Label=label; Sequence=sequence; AppearedAt=0;
    }
}

internal static class CombatLogMotion {
    const double FadeSeconds=.20;
    internal const double HighlightSeconds=.65;
    internal static byte Opacity(double age) {
        return (byte)Math.Round(64+191*Math.Max(0,Math.Min(1,age/FadeSeconds)));
    }
    internal static byte Highlight(double age) {
        return (byte)Math.Round(230*Math.Max(0,1-Math.Max(0,age)/HighlightSeconds));
    }
    internal static void MarkArrivals(CombatLogEntry[] previous, CombatLogEntry[] recent, double now) {
        int old=previous.Length-1;
        for(int i=recent.Length-1;i>=0;i--){
            while(old>=0&&previous[old].Sequence>recent[i].Sequence)old--;
            recent[i].AppearedAt=old>=0&&previous[old].Sequence==recent[i].Sequence?previous[old].AppearedAt:now;
        }
    }
}

internal sealed class CombatLogPainter : IDisposable {
    const int RowHeight=34;
    internal static int VisibleRows(int height){return Math.Max(1,(height-54)/RowHeight);}
    internal static Rectangle ScrollTrack(Size size){return new Rectangle(size.Width-13,42,11,size.Height-68);}
    static readonly Color Text = Color.FromArgb(233, 241, 246), Muted = Color.FromArgb(163, 183, 195),
        Mint = Color.FromArgb(139, 219, 194), Gold = Color.FromArgb(255, 207, 131);
    internal static readonly CombatLogEntry[] Sample = {
        new CombatLogEntry(1793916495,13010000,842,FloatingHitLabel.Front),
        new CombatLogEntry(1793916496,13350000,2486,FloatingHitLabel.Critical|FloatingHitLabel.Back),
        new CombatLogEntry(1793916497,13210000,1102,FloatingHitLabel.None),
        new CombatLogEntry(1793916498,13220000,3240,FloatingHitLabel.Perfect|FloatingHitLabel.Back)
    };
    readonly SkillSlotAssets icons = new SkillSlotAssets();
    readonly CrispOverlayText title = new CrispOverlayText(), badge = new CrispOverlayText(),
        timestamp = new CrispOverlayText(), skillName = new CrispOverlayText(),
        hitLabel = new CrispOverlayText(), amount = new CrispOverlayText();
    readonly Dictionary<long,Bitmap> rows=new Dictionary<long,Bitmap>();
    readonly Queue<long> rowOrder=new Queue<long>();
    readonly ImageAttributes rowAlpha=new ImageAttributes();
    readonly ColorMatrix alphaMatrix=new ColorMatrix();
    int cachedWidth,cachedFormat;
    bool cachedTime,cachedLabels,cachedSample;
    string cachedLanguage;
    internal int RasterizedRows {get;private set;}

    void PrepareRows(int width,CombatLogOptions options,string language,bool sample){
        if(cachedWidth==width&&cachedFormat==options.Format&&cachedTime==options.ShowTime&&cachedLabels==options.ShowHitType&&cachedLanguage==language&&cachedSample==sample)return;
        ClearRows();
        cachedWidth=width;cachedFormat=options.Format;cachedTime=options.ShowTime;cachedLabels=options.ShowHitType;cachedLanguage=language;cachedSample=sample;
    }
    void ClearRows(){
        foreach(var row in rows.Values)row.Dispose();
        rows.Clear();rowOrder.Clear();
    }
    Bitmap RowPixels(long key,CombatLogEntry entry,int width,CombatLogOptions options,string language){
        Bitmap pixels;
        if(rows.TryGetValue(key,out pixels))return pixels;
        if(rows.Count==32){
            long oldest=rowOrder.Dequeue();rows[oldest].Dispose();rows.Remove(oldest);
        }
        pixels=new Bitmap(width-18,RowHeight,PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(pixels)){
            g.TranslateTransform(-9,0);
            PaintRow(g,entry,width,options,language);
        }
        rows.Add(key,pixels);rowOrder.Enqueue(key);RasterizedRows++;
        return pixels;
    }

    internal Bitmap Render(Size size, CombatLogOptions options, CombatLogEntry[] entries, string language, bool sample, bool editing, double now=0,int historyOffset=0,bool scrolling=false) {
        var image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(image)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var panel = SkillSlotPainter.Round(new RectangleF(.5f,.5f,size.Width-1,size.Height-1),12))
            using (var fill = new SolidBrush(Color.FromArgb((int)Math.Round(options.BackgroundOpacity*255/100.0),5,8,12)))
            using (var border = new Pen(Color.FromArgb(editing ? 200 : 130,101,135,143))) {
                g.FillPath(fill,panel);
                g.DrawPath(border,panel);
            }
            title.Draw(g,UiLanguage.Text("Combat Log",language),16,true,Text,new Rectangle(14,6,size.Width-(sample?125:28),28),0,true);
            if (sample) badge.Draw(g,UiLanguage.Text("SAMPLE",language),11,true,Mint,new Rectangle(size.Width-110,6,96,28),2,true);
            using (var line = new Pen(Color.FromArgb(75,156,184,192))) g.DrawLine(line,12,36,size.Width-12,36);
            int capacity=VisibleRows(size.Height),maxOffset=Math.Max(0,entries.Length-capacity);
            int rowWidth=size.Width-(maxOffset>0?9:0);
            PrepareRows(rowWidth,options,language,sample);
            int offset=Math.Max(0,Math.Min(historyOffset,maxOffset));
            int end=entries.Length-offset,first=Math.Max(0,end-capacity);
            if (entries.Length==0)
                skillName.Draw(g,UiLanguage.Text("Waiting for your first hit",language),13,false,Muted,new Rectangle(14,44,size.Width-28,26),0,true);
            int visible=end-first;
            int top=options.NewestAtBottom?size.Height-13-visible*RowHeight:41;
            for (int row=0;row<visible;row++) {
                int i=options.NewestAtBottom?first+row:end-1-row;
                double age=now-entries[i].AppearedAt;
                byte opacity=sample?(byte)255:CombatLogMotion.Opacity(age);
                byte highlight=i!=entries.Length-1?(byte)0:sample?(byte)210:CombatLogMotion.Highlight(age);
                var entry=entries[i];
                int y=top+row*RowHeight;
                if((row&1)!=0)
                    using(var tint=new SolidBrush(Color.FromArgb(32*opacity/255,101,145,153)))g.FillRectangle(tint,10,y,rowWidth-20,33);
                var pixels=RowPixels(sample?i:entry.Sequence,entry,rowWidth,options,language);
                if(opacity==255)g.DrawImageUnscaled(pixels,9,y);
                else{
                    alphaMatrix.Matrix33=opacity/255f;rowAlpha.SetColorMatrix(alphaMatrix);
                    g.DrawImage(pixels,new Rectangle(9,y,pixels.Width,RowHeight),0,0,pixels.Width,RowHeight,GraphicsUnit.Pixel,rowAlpha);
                }
                if(highlight>0)using(var path=SkillSlotPainter.Round(new RectangleF(9,y+1,rowWidth-18,31),5))
                    using(var edge=new Pen(Color.FromArgb(highlight,255,204,84),1.5f))g.DrawPath(edge,path);
            }
            if(maxOffset>0){
                var area=ScrollTrack(size);
                int thumb=Math.Min(area.Height,Math.Max(14,area.Height*capacity/entries.Length));
                double position=offset/(double)maxOffset;
                if(options.NewestAtBottom)position=1-position;
                using(var hit=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(hit,area);
                using(var track=new SolidBrush(Color.FromArgb(35,163,183,195)))g.FillRectangle(track,area.X+4,area.Y,3,area.Height);
                using(var grip=new SolidBrush(Color.FromArgb(scrolling?175:95,163,183,195)))g.FillRectangle(grip,area.X+3,area.Y+(int)Math.Round((area.Height-thumb)*position),5,thumb);
            }
            if (editing) using (var grip = new SolidBrush(Color.FromArgb(105,66,105,105)))
            using (var line = new Pen(Mint,1.5f)) {
                int corner=MovableOverlayWindow.ResizeGrip;
                g.FillRectangle(grip,size.Width-corner,size.Height-corner,corner-4,corner-4);
                g.DrawLine(line,size.Width-17,size.Height-7,size.Width-7,size.Height-17);
                g.DrawLine(line,size.Width-12,size.Height-7,size.Width-7,size.Height-12);
            }
        }
        return image;
    }

    void PaintRow(Graphics g, CombatLogEntry entry, int width, CombatLogOptions o, string language) {
        int x=14;
        if (o.ShowTime) {
            var stamp=DpsHistory.LocalTime(entry.Time).ToString("HH:mm:ss",CultureInfo.InvariantCulture);
            timestamp.Draw(g,stamp,11,false,Muted,new Rectangle(x,0,55,32),0,true);
            x+=58;
        }
        if (o.ShowIcon) {
            var icon=icons.Icon(entry.Skill);
            if(icon!=null)g.DrawImage(icon,x,4,24,24);
            else using(var edge=new Pen(Color.FromArgb(100,133,161,173)))g.DrawRectangle(edge,x,4,23,23);
            x+=33;
        }
        string label=o.ShowHitType && entry.Label!=FloatingHitLabel.None ? FloatingTextMotion.Label(entry.Label,language) : "";
        var color=(entry.Label&FloatingHitLabel.Critical)!=0 ? Gold : (entry.Label&FloatingHitLabel.Perfect)!=0 ? Mint : Text;
        int amountWidth=90, nameArea=width-14-amountWidth-x;
        if (o.ShowName) {
            string name=SkillCatalogue.Shared.Name(entry.Skill,language);
            skillName.Draw(g,name,13,true,Text,new Rectangle(x,label.Length==0?6:-2,nameArea,19),0,true);
        }
        if (label.Length!=0)
            hitLabel.Draw(g,label,11,false,color,new Rectangle(x,o.ShowName?15:8,nameArea,16),0,true);
        amount.Draw(g,entry.Amount.ToString("N0",CultureInfo.CurrentCulture),16,true,color,new Rectangle(width-14-amountWidth,0,amountWidth,32),2,true);
    }

    public void Dispose() {
        ClearRows();rowAlpha.Dispose();
        icons.Dispose(); title.Dispose(); badge.Dispose(); timestamp.Dispose();
        skillName.Dispose(); hitLabel.Dispose(); amount.Dispose();
    }
}

internal sealed class CombatLogOverlay : IDisposable {
    readonly EnergyOverlay owner;
    readonly FloatingTextSignal signal;
    readonly CombatLogPainter painter = new CombatLogPainter();
    readonly MovableOverlayWindow window = new MovableOverlayWindow("Combat Log");
    readonly Stopwatch clock = Stopwatch.StartNew();
    CombatLogEntry[] entries = new CombatLogEntry[0];
    long revision = -1;
    string language;
    CombatLogOptions displayed;
    long visual;
    bool wasSample, wasEditing, wasAnimating,wasScrolling;
    CombatLogOptions lastOptions;
    double nextMaintenance;
    long anchor;
    int historyOffset;
    double lastScroll=double.NegativeInfinity;
    internal int NextInterval { get; private set; }

    internal CombatLogOverlay(EnergyOverlay owner, FloatingTextSignal signal) {
        this.owner=owner;
        this.signal=signal;
        window.LimitResize=CombatLogOptions.LimitSize;
        window.ScrollArea=size=>entries.Length>CombatLogPainter.VisibleRows(size.Height)?CombatLogPainter.ScrollTrack(size):Rectangle.Empty;
        window.Scrolled=delta=>{
            int steps=Math.Max(1,Math.Abs(delta)/120)*3;
            Scroll(historyOffset+(delta>0?1:-1)*(displayed.NewestAtBottom?1:-1)*steps);
        };
        window.ScrollPosition=position=>{
            int max=MaxOffset(displayed.Bounds.Height);
            Scroll((int)Math.Round(max*(displayed.NewestAtBottom?1-position:position)));
        };
        window.SnapOwner=owner;
        window.Moved=delegate(Rectangle bounds) {
            var cfg=owner.Configuration;
            cfg.CombatLog.Bounds=bounds;
            owner.ApplyConfiguration(cfg);
        };
    }
    int MaxOffset(int height){return Math.Max(0,entries.Length-CombatLogPainter.VisibleRows(height));}
    void Scroll(int offset){
        if(displayed==null)return;
        historyOffset=Math.Max(0,Math.Min(offset,MaxOffset(displayed.Bounds.Height)));
        anchor=historyOffset==0?0:entries[entries.Length-1-historyOffset].Sequence;
        lastScroll=clock.Elapsed.TotalSeconds;
        visual++;
    }

    internal void Maintain(CombatLogOptions o, bool locked, bool isolated) {
        bool editing=!locked;
        if(!o.Enabled||isolated){NextInterval=1000;nextMaintenance=0;window.HideOverlay();return;}
        double now=clock.Elapsed.TotalSeconds;
        if(!owner.HudOpacityChanged&&now<nextMaintenance&&ReferenceEquals(lastOptions,o)&&wasEditing==editing&&language==owner.Language)return;
        lastOptions=o;
        bool game=o.Enabled&&!isolated&&FloatingTextService.GameForeground();
        bool show=o.Enabled&&!isolated&&(game||editing)&&!owner.WidgetsSuppressed&&!DpsDesign.ShellUiActive();
        NextInterval=250;
        nextMaintenance=now+.25;
        if (!show) { window.HideOverlay(); return; }
        NextInterval=125;
        nextMaintenance=now+.125;
        var recent=signal.ReadCombatLog(ref revision);
        if (recent!=null) {
            CombatLogMotion.MarkArrivals(entries,recent,now);entries=recent;
            if(anchor!=0){
                int index=Array.FindIndex(entries,entry=>entry.Sequence==anchor);
                historyOffset=index<0?MaxOffset(o.Bounds.Height):entries.Length-1-index;
            }
        }
        int offset=Math.Min(historyOffset,MaxOffset(o.Bounds.Height));
        var track=CombatLogPainter.ScrollTrack(o.Bounds.Size);track.Offset(o.Bounds.Location);
        bool hovering=window.Visible&&MaxOffset(o.Bounds.Height)>0&&track.Contains(System.Windows.Forms.Cursor.Position);
        bool scrolling=window.IsScrolling||hovering||now-lastScroll<5;
        if (!o.ShowForActivity(entries,editing,DpsHistory.UtcNow())&&!scrolling) {
            if(historyOffset!=0){historyOffset=0;anchor=0;visual++;}
            NextInterval=250;nextMaintenance=now+.25;
            window.HideOverlay();return;
        }
        bool sample=editing&&entries.Length==0;
        bool animating=offset==0&&entries.Length!=0&&now-entries[entries.Length-1].AppearedAt<CombatLogMotion.HighlightSeconds;
        if(animating){NextInterval=40;nextMaintenance=now+.04;}
        if (recent!=null||animating||wasAnimating||displayed==null||!CombatLogOptions.Same(displayed,o)||language!=owner.Language||sample!=wasSample||editing!=wasEditing||scrolling!=wasScrolling) {
            displayed=o.Copy(); language=owner.Language; wasSample=sample; wasEditing=editing; visual++;
        }
        wasAnimating=animating;wasScrolling=scrolling;
        window.Render(o.Bounds,owner.HudOpacity(),editing,visual.ToString(CultureInfo.InvariantCulture),
            size=>painter.Render(size,o,sample?CombatLogPainter.Sample:entries,language,sample,editing,now,offset,scrolling),o.Blur);
    }

    public void Dispose() { window.Dispose(); painter.Dispose(); }
}
