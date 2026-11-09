using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class CharacterProgressOptions {
    public FpsOptions Power = new FpsOptions { Color = "#FFFFFF", Bounds = new Rectangle(30, 130, 112, 44) };
    public bool AlternatePower;
    public EnergyBarOptions Experience = new EnergyBarOptions {
        Enabled = false, AutoHide = false, DynamicColors = false, HighColor = "#FFAD35",
        TextColor = "#FFE5AA", TextSize = 13, CurrentText = true, MaximumText = true, PercentageText = true,
        HoldSeconds = 5, FadeSeconds = .25, GlowPercent = 60
    };
    public bool HideAtCap;
    public Rectangle ExperienceBounds = new Rectangle(740, 1010, 440, 4);

    internal CharacterProgressOptions Copy() {
        var copy = (CharacterProgressOptions)MemberwiseClone();
        copy.Power = Power.Copy();
        copy.Experience = Experience.Copy();
        return copy;
    }

    internal static bool Same(CharacterProgressOptions a, CharacterProgressOptions b) {
        var x=a.Experience; var y=b.Experience;
        return FpsOptions.Same(a.Power,b.Power) && a.AlternatePower==b.AlternatePower
            && a.ExperienceBounds==b.ExperienceBounds && a.HideAtCap==b.HideAtCap
            && x.Enabled==y.Enabled && x.AutoHide==y.AutoHide && x.HoldSeconds==y.HoldSeconds
            && x.Fade==y.Fade && x.FadeSeconds==y.FadeSeconds && x.Emissive==y.Emissive
            && x.HighColor==y.HighColor && x.GlowPercent==y.GlowPercent && x.TrackOpacity==y.TrackOpacity
            && ProgressBarText.Same(x,y);
    }

    internal void Normalize() {
        var defaults = new CharacterProgressOptions();
        Power = Power ?? defaults.Power;
        Experience = Experience ?? defaults.Experience;
        if (!EnergyBarOptions.ValidColor(Power.Color)) Power.Color = defaults.Power.Color;
        Power.Normalize();
        Experience.Normalize();
        Experience.DynamicColors = false;
        ExperienceBounds = new Rectangle(
            Math.Max(-100000, Math.Min(100000, ExperienceBounds.X)), Math.Max(-100000, Math.Min(100000, ExperienceBounds.Y)),
            Math.Max(120, Math.Min(2400, ExperienceBounds.Width)), Math.Max(3, Math.Min(60, ExperienceBounds.Height)));
    }

    internal static CharacterProgressOptions Read(Dictionary<string,object> settings) {
        var options = new CharacterProgressOptions();
        object value;
        if (settings.TryGetValue("characterProgress", out value)) {
            try { options = new JavaScriptSerializer().ConvertToType<CharacterProgressOptions>(value) ?? options; }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
        options.Normalize();
        return options;
    }
}

internal sealed class CharacterProgressSignal {
    // Exp (character_levels), EU/NA client 2.0.9.0, Steam build 26262663.
    // https://aion2.gg/database/tables/character-levels
    internal const int LevelCap = 45;
    static readonly uint[] thresholds = {
        0, 1133, 1712, 2589, 3915, 5919, 8950, 13532, 20461, 30937, 46777,
        59434, 75516, 95950, 121913, 154901, 196815, 250071, 317737, 403713,
        512953, 651753, 828109, 1052186, 1336895, 1698643, 2158276, 2742280,
        3484308, 4427121, 5625048, 7147120, 9081047, 11538271, 14660392, 18627321,
        23667655, 30071844, 38208931, 48547819, 61684287, 78375330, 99582772,
        126528698, 160765875
    };
    static readonly byte[] powerTail = { 5,1,0,0,2,0,0,3,0,0,4,0,0,5,0,0 };
    static readonly Encoding utf8 = new UTF8Encoding(false, true);
    internal static uint Maximum(int level) { return level > 0 && level < thresholds.Length ? thresholds[level] : 0; }

    internal sealed class Reading {
        internal uint Actor;
        internal int Level;
        internal ulong? Primary, Secondary, Xp;
        internal double Activity;
        internal Reading Copy() { return (Reading)MemberwiseClone(); }
    }

    readonly object gate = new object();
    string flow, name;
    int server;
    Reading value;
    ulong? pendingXp;
    uint loadingActor;
    ulong? loadingPrimary,loadingSecondary;
    double pendingAt;
    internal Func<double> ReceiptClock;

    internal Reading Read() { lock (gate) return value; }
    internal void Reset() { lock (gate) Clear(); }
    void Clear() { flow = name = null; server = 0; value = null; pendingXp = null; loadingActor=0; loadingPrimary=loadingSecondary=null; }

    internal static bool Profile(byte[] b, int start, int end, out Reading reading, out string name, out int server) {
        reading = null;
        server = 0;
        uint actor;
        byte role;
        if (!DpsSignal.PlayerIdentity(b, start, end, true, out actor, out name, out role)) return false;
        int p = start + 2;
        uint unused;
        if (!DpsSignal.Var(b, ref p, end, out unused)) return false;
        for (int i = p; i < p + 16 && i < end - 30; i++) {
            int n = b[i], header = i + 1 + n + 2;
            if (n < 2 || n > 24 || header > end - 29) continue;
            uint code = BitConverter.ToUInt32(b, header);
            if (code < 5 || code > 36 || (b[header+4] != 1 && b[header+4] != 2)) continue;
            string candidate;
            try { candidate = utf8.GetString(b, i + 1, n); }
            catch (DecoderFallbackException) { continue; }
            if (candidate != name) continue;
            uint level = BitConverter.ToUInt32(b, header+5);
            if (level == 0 || level > 200 || BitConverter.ToUInt32(b, header+17) != level) continue;
            reading = new Reading { Actor = actor, Level = (int)level };
            server = BitConverter.ToUInt16(b, header-2);
            uint primary = BitConverter.ToUInt32(b, header+9);
            if (primary == BitConverter.ToUInt32(b, header+13)) reading.Primary = primary;
            ulong xp = BitConverter.ToUInt64(b, header+21);
            uint maximum = Maximum(reading.Level);
            if (maximum > 0 && xp < maximum) reading.Xp = xp;
            int tail = end - 2 * powerTail.Length;
            if (tail >= header + 45 && HasPowerTail(b, tail)) {
                ulong power = BitConverter.ToUInt64(b, tail-16);
                if (power <= long.MaxValue && BitConverter.ToUInt64(b, tail-8) == power) reading.Secondary = power;
            }
            return true;
        }
        return false;
    }

    static bool HasPowerTail(byte[] data, int start) {
        // Two lists of five ordered entries. Each entry has a one-byte ID
        // followed by two payload bytes, which can differ between the lists.
        for (int offset = 0; offset < 2 * powerTail.Length; offset += powerTail.Length) {
            int list = start + offset;
            if (data[list] != 5) return false;
            for (int id = 1; id <= 5; id++)
                if (data[list+1+(id-1)*3] != id) return false;
        }
        return true;
    }

    internal bool Consume(byte[] b, int start, int end, string key, double now) {
        if (end-start < 2) return false;
        int op = (b[start]<<8) | b[start+1], length = end-start, p = start+2;
        if (op != 0x2036 && op != 0x3336 && op != 0x1536 && op != 0x1d56 && op != 0x5636 && op != 0x078d && op != 0x088d) return false;
        if (ReceiptClock != null) now = ReceiptClock();
        lock (gate) {
            if(op==0x2036){
                uint actor;if(!DashSignal.LoginActor(b,start,end,out actor))return false;
                Clear();flow=key;loadingActor=actor;return true;
            }
            if (op == 0x3336) {
                Reading profile;
                string profileName;
                int profileServer;
                if (!Profile(b, start, end, out profile, out profileName, out profileServer)) {
                    if (flow != key || value == null) return false;
                    Clear();
                    return true;
                }
                if(loadingActor!=0&&(flow!=key||loadingActor!=profile.Actor))return false;
                bool same = value != null && value.Actor == profile.Actor && flow == key && name == profileName && server == profileServer;
                profile.Activity = !same || profile.Level != value.Level || profile.Xp.HasValue && (!value.Xp.HasValue || profile.Xp > value.Xp) ? now : value.Activity;
                if (same && !profile.Secondary.HasValue) profile.Secondary = value.Secondary;
                if(flow==key&&loadingActor==profile.Actor){
                    if(!profile.Primary.HasValue)profile.Primary=loadingPrimary;
                    if(!profile.Secondary.HasValue)profile.Secondary=loadingSecondary;
                }
                loadingActor=0;loadingPrimary=loadingSecondary=null;
                flow = key;
                name = profileName;
                server = profileServer;
                pendingXp = null;
                value = profile;
                return true;
            }
            if(flow!=key)return false;
            if (op == 0x1536) { Clear(); return true; }
            if (op == 0x1d56 && length == 10) {
                uint power = BitConverter.ToUInt32(b,p);
                if(power!=BitConverter.ToUInt32(b,p+4))return false;
                if(value==null){if(loadingActor!=0)loadingPrimary=power;return false;}
                if(value.Primary==power)return false;
                var next = value.Copy();
                next.Primary = power;
                value = next;
                return true;
            }
            if (op == 0x5636 && length == 18) {
                ulong power = BitConverter.ToUInt64(b,p);
                if(power>long.MaxValue||power!=BitConverter.ToUInt64(b,p+8))return false;
                if(value==null){if(loadingActor!=0)loadingSecondary=power;return false;}
                if(value.Secondary==power)return false;
                var next = value.Copy();
                next.Secondary = power;
                value = next;
                return true;
            }
            if(value==null)return false;
            if (op == 0x078d && length == 19) {
                ulong xp = BitConverter.ToUInt64(b,p), gain = BitConverter.ToUInt64(b,p+8);
                uint limit = Maximum(value.Level);
                if (limit == 0 || xp >= thresholds[LevelCap-1] || gain > long.MaxValue) return false;
                bool rollover = gain > 0 && value.Xp.HasValue && gain >= limit-value.Xp.Value
                    && xp < value.Xp.Value + gain;
                pendingXp = xp;
                pendingAt = now;
                if (rollover || xp >= limit) return false;
                if (value.Xp == xp) return false;
                var next = value.Copy();
                next.Xp = xp;
                if (gain > 0) next.Activity = now;
                value = next;
                return true;
            }
            if (op == 0x088d && length == 18) {
                uint level = BitConverter.ToUInt32(b,p);
                if (level == 0 || level > 200 || level != BitConverter.ToUInt32(b,p+4) || level == value.Level) return false;
                var next = value.Copy();
                next.Level = (int)level;
                next.Xp = pendingXp.HasValue && now >= pendingAt && now-pendingAt <= .25 && pendingXp.Value < Maximum(next.Level)
                    ? pendingXp : null;
                next.Activity = now;
                pendingXp = null;
                value = next;
                return true;
            }
            return false;
        }
    }

    internal static void Verify() {
        var signal = new CharacterProgressSignal();
        var profile = new byte[160];
        profile[0]=0x33; profile[1]=0x36; profile[2]=12; profile[3]=6;
        Buffer.BlockCopy(Encoding.UTF8.GetBytes("Tester"),0,profile,4,6);
        profile[12]=18; profile[16]=1;
        profile[17]=10; profile[21]=3; profile[25]=3; profile[29]=10;
        Buffer.BlockCopy(BitConverter.GetBytes(4468ul),0,profile,112,8);
        Buffer.BlockCopy(BitConverter.GetBytes(4468ul),0,profile,120,8);
        Buffer.BlockCopy(powerTail,0,profile,128,16);
        Buffer.BlockCopy(powerTail,0,profile,144,16);
        profile[146]=3;
        if (!signal.Consume(profile,0,profile.Length,"own",1) || signal.Read().Primary!=3
            || signal.Read().Secondary!=4468 || signal.Read().Level!=10 || signal.Read().Xp!=0)
            throw new InvalidOperationException("Initial own level, both powers and zero XP must reach the overlay.");
        var power=new byte[] {0x1d,0x56,5,0,0,0,5,0,0,0};
        if (signal.Consume(power,0,power.Length,"foreign",2) || !signal.Consume(power,0,power.Length,"own",2)
            || signal.Read().Primary!=5)
            throw new InvalidOperationException("Only the current character's flow can change Combat Power.");
        var xp=new byte[19];
        xp[0]=0x07; xp[1]=0x8d; xp[18]=1;
        Buffer.BlockCopy(BitConverter.GetBytes(678ul),0,xp,2,8);
        Buffer.BlockCopy(BitConverter.GetBytes(339ul),0,xp,10,8);
        if (!signal.Consume(xp,0,xp.Length,"own",3) || signal.Read().Xp!=678 || Maximum(10)!=46777
            || signal.Consume(xp,0,xp.Length,"own",4) || signal.Read().Activity!=3)
            throw new InvalidOperationException("An XP update supplies the authoritative total and a duplicate does not restart the hold.");
        Array.Clear(xp,2,8);
        Buffer.BlockCopy(BitConverter.GetBytes(46099ul),0,xp,10,8);
        var level=new byte[18]; level[0]=0x08; level[1]=0x8d; level[2]=level[6]=11;
        if (signal.Consume(xp,0,xp.Length,"own",5) || signal.Read().Xp!=678
            || !signal.Consume(level,0,level.Length,"own",5) || signal.Read().Level!=11 || signal.Read().Xp!=0)
            throw new InvalidOperationException("A rollover commits the new level and XP together.");
        signal.Reset();
        if (signal.Read()!=null || signal.Consume(power,0,power.Length,"own",6))
            throw new InvalidOperationException("A reset revokes all readings until a new own profile.");
        signal.Consume(profile,0,profile.Length,"own",7);
        var login=DashSignal.LoginFixture(34);
        signal.Consume(login,0,login.Length,"own",8);
        var secondary=new byte[18];secondary[0]=0x56;secondary[1]=0x36;
        Buffer.BlockCopy(BitConverter.GetBytes(81318ul),0,secondary,2,8);
        Buffer.BlockCopy(BitConverter.GetBytes(81318ul),0,secondary,10,8);
        signal.Consume(secondary,0,secondary.Length,"own",8.1);
        signal.Consume(profile,0,profile.Length,"own",8.2);
        if(signal.Read()!=null)throw new InvalidOperationException("Power arriving during login must not be displayed under the previous character.");
        var nextProfile=(byte[])profile.Clone();nextProfile[2]=34;Array.Clear(nextProfile,112,48);
        signal.Consume(nextProfile,0,nextProfile.Length,"own",9);
        if(signal.Read()==null||signal.Read().Actor!=34||signal.Read().Secondary!=81318)
            throw new InvalidOperationException("The confirmed character receives its own power update staged during login.");
    }
}

internal static class CharacterProgressDesign {
    internal static Bitmap Power(Size size, ulong? value, bool crossed, FpsOptions options) {
        var bitmap = FpsDesign.Render(size.Width, size.Height, "", options, false);
        using (var g = Graphics.FromImage(bitmap))
        using (var text = new CrispOverlayText())
        using (var brush = new SolidBrush(ColorTranslator.FromHtml(options.Color))) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int icon = Math.Max(8, Math.Min(size.Height / 2, size.Width / 4));
            int fontSize = Math.Max(8, (int)Math.Round(size.Height * .52));
            int gap = Math.Max(3, icon / 4), available = size.Width-icon-gap-8;
            string number = value.HasValue ? value.Value.ToString("N0", CultureInfo.CurrentCulture) : "—";
            int width;
            using (var font = new Font("Segoe UI", fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                width = TextRenderer.MeasureText(number, font, Size.Empty, TextFormatFlags.NoPadding).Width + 2;
            if (width > available) fontSize = Math.Max(6, fontSize*available/width);
            width = Math.Min(width, available);
            int left = (size.Width-width-gap-icon)/2;
            text.Draw(g, number, fontSize, false, brush.Color, new Rectangle(left,0,width,size.Height), 1, true);
            g.TranslateTransform(left+width+gap+icon/2f,size.Height/2f);
            g.ScaleTransform(icon/20f,icon/20f);
            Sword(g,brush);
            if (crossed) { g.ScaleTransform(-1,1); Sword(g,brush); }
        }
        return bitmap;
    }

    static void Sword(Graphics g, Brush brush) {
        g.FillPolygon(brush,new[] { new PointF(-6,4),new PointF(5,-7),new PointF(9,-9),new PointF(7,-5),new PointF(-4,6) });
        using (var pen = new Pen(brush,1.7f)) {
            g.DrawLine(pen,-8,2,-2,8);
            g.DrawLine(pen,-5,5,-9,9);
        }
    }

    internal static Bitmap Experience(Size size, int level, ulong xp, uint max, EnergyBarOptions options, string language) {
        double ratio = max == 0 ? 1 : (double)xp/max;
        string label = (UiLanguage.Normalize(language) == "es" ? "Nv. " : "Lv. ") + level;
        if (max == 0) label += "  ·  " + UiLanguage.Text("Maximum level", language);
        else {
            if (options.CurrentText) label += "   " + xp.ToString("N0");
            if (options.MaximumText) label += (options.CurrentText ? " / " : "   ") + max.ToString("N0");
            if (options.CurrentText || options.MaximumText) label += " XP";
        }
        using (var bar = BarDesign.RenderEmissive(size.Width,size.Height,ratio,false,true,1,options))
        using (var text = new CrispOverlayText()) {
            int row = options.TextSize+8, pad = BarDesign.GlowPadding;
            var bitmap = new Bitmap(bar.Width,bar.Height+row,PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap)) {
                g.DrawImageUnscaled(bar,0,0);
                var color = ColorTranslator.FromHtml(options.TextColor);
                int y = bar.Height-pad;
                int percent = options.PercentageText && max > 0 ? Math.Min(size.Width/3,options.TextSize*6) : 0;
                text.Draw(g,label,options.TextSize,true,color,new Rectangle(pad,y,size.Width-percent,row),0,true,options.TextOutline);
                if (percent > 0)
                    text.Draw(g,ratio.ToString("P2"),options.TextSize,false,color,new Rectangle(pad+size.Width-percent,y,percent,row),2,true,options.TextOutline);
            }
            return bitmap;
        }
    }
}

internal sealed class MovableOverlayWindow : Form,IWidgetSnapTarget {
    internal EnergyOverlay SnapOwner;
    internal int SnapInset;
    internal bool ResizeAllowed=true;
    Rectangle IWidgetSnapTarget.SnapBounds { get { return SnapOwner==null?Rectangle.Empty:SnapArea(presented); } }
    Rectangle SnapArea(Rectangle bounds) {
        bounds.Inflate(-SnapInset,-SnapInset);
        return bounds;
    }
    Rectangle MovedBounds(Point delta) {
        var bounds=start;
        bounds.Offset(delta);
        if(SnapOwner==null)return bounds;
        var area=SnapArea(bounds);
        var aligned=SnapOwner.SnapWidget(this,area);
        bounds.Offset(aligned.X-area.X,aligned.Y-area.Y);
        return bounds;
    }
    internal const int ResizeGrip = 24;
    readonly LayeredImage image = new LayeredImage();
    OverlayGlassLayer backdrop;
    List<OverlayGlassLayer> tileBackdrops;
    internal Rectangle[] BackdropTiles;
    string imageKey;
    Func<Size,Bitmap> paint;
    Size requestedSize, paintedSize;
    Rectangle presented;
    byte alpha;
    bool dragging, interactive, resizing, blur;
    Point pointer;
    Rectangle start;
    internal Action Clicked;
    internal Func<Size,Rectangle> ScrollArea;
    internal Action<int> Scrolled;
    internal Action<double> ScrollPosition;
    bool scrolling;
    internal bool IsScrolling {get{return scrolling;}}
    internal Rectangle PresentedBounds {get{return presented;}}
    internal Action<Rectangle> Moved;
    internal Func<Size,Size> LimitResize;

    internal MovableOverlayWindow(string title) {
        Text = "Aion 2 Helper - " + title;
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
        get { var p = base.CreateParams; p.ExStyle |= 0x80000 | 0x80 | 0x08000000; return p; }
    }
    internal void InvalidateImage() { imageKey = null; }
    internal void HideOverlay() {
        if(scrolling){scrolling=false;Capture=false;}
        if (Visible) Hide();
        HideBackdrops();
    }
    void HideBackdrops() {
        if(backdrop!=null&&backdrop.Visible)backdrop.Hide();
        if(tileBackdrops!=null)foreach(var tile in tileBackdrops)if(tile.Visible)tile.Hide();
    }
    void SyncBackdrop() {
        if(!blur){HideBackdrops();return;}
        if(BackdropTiles!=null) {
            if(tileBackdrops==null)tileBackdrops=new List<OverlayGlassLayer>();
            for(int i=0;i<BackdropTiles.Length;i++){
                if(i==tileBackdrops.Count)tileBackdrops.Add(new OverlayGlassLayer());
                var card=BackdropTiles[i];
                card.Offset(presented.Location);
                tileBackdrops[i].Sync(card,alpha,Handle,true,card.Height*.14f);
            }
            for(int i=BackdropTiles.Length;i<tileBackdrops.Count;i++)
                if(tileBackdrops[i].Visible)tileBackdrops[i].Hide();
        } else {
            if (backdrop==null) backdrop=new OverlayGlassLayer();
            backdrop.Sync(presented,alpha,Handle,true,12);
        }
    }
    internal void Render(Rectangle bounds, byte opacity, bool editing, string key, Func<Size,Bitmap> render, bool useBlur=false) {
        if (opacity == 0) { HideOverlay(); return; }
        blur=useBlur;
        interactive = editing;
        if (!dragging) start = bounds;
        var position = dragging ? presented.Location : bounds.Location;
        paint=render;
        Size size=dragging&&resizing&&LimitResize!=null ? requestedSize : bounds.Size;
        bool changed = key != imageKey || size != paintedSize;
        if (changed) {
            PaintSize(size);
            imageKey = key;
        }
        if (!Visible || changed || opacity != alpha || position != presented.Location) {
            image.Present(Handle,position.X,position.Y,opacity);
            presented.Location = position;
            alpha = opacity;
        }
        if (!Visible) { Show(); OverlayNative.RaiseWithoutFocus(Handle); }
        SyncBackdrop();
    }
    void PaintSize(Size size) {
        using (var bitmap=paint(size)) {
            image.Upload(bitmap);
            presented.Size=bitmap.Size;
        }
        paintedSize=size;
    }
    bool OverResizeGrip(Point position) {
        return ResizeAllowed&&presented.Contains(position) && position.X>=presented.Right-ResizeGrip && position.Y>=presented.Bottom-ResizeGrip;
    }
    Size DragSize(Point delta) {
        var size=start.Size+(Size)delta;
        return LimitResize==null?size:LimitResize(size);
    }
    void BeginDrag(Point position) {
        pointer=position;
        resizing=OverResizeGrip(position);
        requestedSize=start.Size;
        dragging=true;
    }
    void MoveDrag(Point position) {
        var delta=position-(Size)pointer;
        if (!resizing) {
            presented.Location=MovedBounds(delta).Location;
        } else {
            if (LimitResize==null) return;
            requestedSize=DragSize(delta);
            if (requestedSize==paintedSize) return;
            PaintSize(requestedSize);
        }
        image.Present(Handle,presented.X,presented.Y,alpha);
        SyncBackdrop();
    }
    Rectangle EndDrag(Point position,bool captureLost) {
        var delta=position-(Size)pointer;
        var next=start;
        if (resizing) next.Size=captureLost&&LimitResize!=null?requestedSize:DragSize(delta);
        else next=captureLost?presented:MovedBounds(delta);
        dragging=false;
        return next;
    }
    Rectangle ScrollBounds(){
        var area=ScrollArea==null?Rectangle.Empty:ScrollArea(presented.Size);
        if(!area.IsEmpty)area.Offset(presented.Location);
        return area;
    }
    void ScrollToPointer(){
        var area=ScrollBounds();
        if(area.Height>0&&ScrollPosition!=null)ScrollPosition(Math.Max(0,Math.Min(1,(Cursor.Position.Y-area.Top)/(double)Math.Max(1,area.Height-1))));
    }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
        if (m.Msg == 0x84) { m.Result = new IntPtr(interactive || Clicked!=null || ScrollBounds().Contains(Cursor.Position) ? 1 : -1); return; }
        if(m.Msg==0x20&&ScrollBounds().Contains(Cursor.Position)){OverlayNative.SetCursor(Cursors.Hand.Handle);m.Result=new IntPtr(1);return;}
        if(m.Msg==0x20A&&Scrolled!=null&&ScrollBounds().Contains(Cursor.Position)){
            Scrolled((short)(m.WParam.ToInt64()>>16));return;
        }
        if(m.Msg==0x201&&ScrollPosition!=null&&ScrollBounds().Contains(Cursor.Position)){
            scrolling=true;Capture=true;ScrollToPointer();return;
        }
        if(m.Msg==0x200&&scrolling){ScrollToPointer();return;}
        if((m.Msg==0x202||m.Msg==0x215)&&scrolling){scrolling=false;Capture=false;return;}
        if (m.Msg == 0x20 && interactive && (dragging ? resizing : OverResizeGrip(Cursor.Position))) {
            OverlayNative.SetCursor(Cursors.SizeNWSE.Handle);
            m.Result=new IntPtr(1);
            return;
        }
        if (m.Msg == 0x201 && interactive) {
            BeginDrag(Cursor.Position);
            Capture = true;
            return;
        }
        if (m.Msg == 0x200 && dragging) {
            MoveDrag(Cursor.Position);
            return;
        }
        if ((m.Msg == 0x202 || m.Msg == 0x215) && dragging) {
            var next=EndDrag(Cursor.Position,m.Msg==0x215);
            Capture = false;
            if (Moved != null) Moved(next);
            return;
        }
        if (m.Msg == 0x202 && Clicked!=null && !interactive) {
            Clicked();
            return;
        }
        base.WndProc(ref m);
    }
    internal static void VerifyLiveResize() {
        using(var window=new MovableOverlayWindow("Resize verification")) {
            var bounds=new Rectangle(-20000,-20000,570,250);
            window.LimitResize=CombatLogOptions.LimitSize;
            Func<Size,Bitmap> draw=size=>new Bitmap(size.Width,size.Height);
            window.Render(bounds,255,true,"sample",draw);
            var corner=new Point(bounds.Right-18,bounds.Bottom-18);
            window.BeginDrag(corner);
            window.MoveDrag(new Point(corner.X-450,corner.Y-200));
            var compact=new Size(CombatLogOptions.MinWidth,CombatLogOptions.MinHeight);
            var resized=window.presented;
            if(resized.Location!=bounds.Location||resized.Size!=compact)
                throw new Exception("Dragging the visible grip must resize immediately and respect the minimum");
            window.Render(bounds,255,true,"refreshed sample",draw);
            resized=window.presented;
            if(resized.Size!=compact||window.EndDrag(Point.Empty,true)!=new Rectangle(bounds.Location,compact))
                throw new Exception("Refresh and capture loss must keep the last preview size");
        }
    }
    protected override void Dispose(bool disposing) {
        if(disposing){
            if(backdrop!=null)backdrop.Dispose();
            if(tileBackdrops!=null)foreach(var tile in tileBackdrops)tile.Dispose();
            image.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class CharacterProgressWidgets : IDisposable {
    readonly EnergyOverlay owner;
    readonly MovableOverlayWindow power = new MovableOverlayWindow("Combat Power");
    readonly MovableOverlayWindow xp = new MovableOverlayWindow("Experience");
    double alpha, lastTick;
    CharacterProgressSignal.Reading reading;
    CharacterProgressOptions powerOptions,xpOptions;
    Func<Size,Bitmap> paintPower,paintXp;
    ulong? powerNumber,xpNumber;
    int xpLevel;
    string xpLanguage;
    string powerKey,xpKey;
    long powerRevision,xpRevision;
    string statusPowerKey,statusXpKey;
    bool statusPowerVisible,statusXpVisible;
    internal int NextInterval { get; private set; }
    internal string StatusKey { get; private set; }
    internal object Status {
        get { return new { level=reading==null?0:reading.Level, current=reading==null?null:reading.Xp,
            maximum=reading==null?0:CharacterProgressSignal.Maximum(reading.Level), primary=reading==null?null:reading.Primary,
            secondary=reading==null?null:reading.Secondary, powerVisible=power.Visible, experienceVisible=xp.Visible }; }
    }
    internal CharacterProgressWidgets(EnergyOverlay owner) {
        this.owner = owner;
        power.Clicked = delegate { Change(o=>o.AlternatePower=!o.AlternatePower); };
        power.SnapOwner=xp.SnapOwner=owner;
        power.Moved = bounds => Change(o=>o.Power.Bounds=bounds);
        xp.Moved = bounds => Change(o=>o.ExperienceBounds=bounds);
    }
    void Change(Action<CharacterProgressOptions> update) {
        var config = owner.Configuration;
        update(config.CharacterProgress);
        owner.ApplyConfiguration(config);
    }
    internal void Configure() { powerOptions=xpOptions=null;power.InvalidateImage();xp.InvalidateImage(); }
    internal void Maintain(CharacterProgressOptions o, CharacterProgressSignal.Reading live, bool editing, double now, string language) {
        reading = live;
        NextInterval = 1000;
        if (!o.Power.Enabled && !o.Experience.Enabled) {
            if (power.Visible) power.Hide();
            if (xp.Visible) xp.Hide();
            lastTick = now;
            alpha = 0;
            StatusKey = "disabled";
            return;
        }
        bool shell = (o.Power.Enabled || o.Experience.Enabled) && (owner.WidgetsSuppressed || DpsDesign.ShellUiActive());
        ulong? number = live == null ? null : o.AlternatePower ? live.Secondary : live.Primary;
        if (editing && !number.HasValue) number = o.AlternatePower ? 1248ul : 120ul;
        byte powerOpacity = o.Power.Enabled && !shell && (editing || live != null) ? (byte)255 : (byte)0;
        if(powerOptions!=o||powerNumber!=number){
            powerOptions=o;powerNumber=number;powerKey=(++powerRevision).ToString(CultureInfo.InvariantCulture);
            paintPower=size=>CharacterProgressDesign.Power(size,powerNumber,powerOptions.AlternatePower,powerOptions.Power);
        }
        power.Render(o.Power.Bounds,powerOpacity,editing,powerKey,paintPower);

        int level = editing ? 10 : live == null ? 0 : live.Level;
        ulong? current = editing ? 28208ul : live == null ? null : live.Xp;
        uint max = CharacterProgressSignal.Maximum(level);
        bool cap = level == CharacterProgressSignal.LevelCap;
        bool known = cap || max > 0 && current.HasValue;
        double dt = Math.Max(0,now-lastTick);
        lastTick = now;
        double deadline = live == null ? now : live.Activity+o.Experience.HoldSeconds;
        bool active = editing || (!cap || !o.HideAtCap) && (!o.Experience.AutoHide || now < deadline);
        if (!o.Experience.Enabled || shell || !known) alpha = 0;
        else if (editing || !o.Experience.Fade || o.Experience.FadeSeconds <= 0) alpha = active ? 1 : 0;
        else {
            if (active && live != null) dt = Math.Min(dt,Math.Max(0,now-live.Activity));
            if (!active && cap && o.HideAtCap && live != null) dt = Math.Min(dt,Math.Max(0,now-live.Activity));
            if (!active && o.Experience.AutoHide && !(cap && o.HideAtCap)) dt = Math.Min(dt,Math.Max(0,now-deadline));
            alpha = Math.Max(0,Math.Min(1,alpha+(active?dt:-dt)/o.Experience.FadeSeconds));
            if (alpha != (active?1:0)) NextInterval = 16;
        }
        if (o.Experience.Enabled && known && !editing && o.Experience.AutoHide && active && now < deadline)
            NextInterval = Math.Min(NextInterval,Math.Max(1,(int)Math.Ceiling((deadline-now)*1000)));
        if(xpOptions!=o||xpLevel!=level||xpNumber!=current||xpLanguage!=language){
            xpOptions=o;xpLevel=level;xpNumber=current;xpLanguage=language;xpKey=(++xpRevision).ToString(CultureInfo.InvariantCulture);
            paintXp=size=>CharacterProgressDesign.Experience(size,xpLevel,xpNumber.GetValueOrDefault(),CharacterProgressSignal.Maximum(xpLevel),xpOptions.Experience,xpLanguage);
        }
        xp.Render(o.ExperienceBounds,(byte)Math.Round(alpha*255),editing,xpKey,paintXp);
        if(StatusKey=="disabled"||statusPowerKey!=powerKey||statusXpKey!=xpKey||statusPowerVisible!=power.Visible||statusXpVisible!=xp.Visible){
            statusPowerKey=powerKey;statusXpKey=xpKey;statusPowerVisible=power.Visible;statusXpVisible=xp.Visible;
            StatusKey=powerKey+"/"+statusPowerVisible+"/"+xpKey+"/"+statusXpVisible;
        }
    }
    public void Dispose() { power.Dispose(); xp.Dispose(); }
}
