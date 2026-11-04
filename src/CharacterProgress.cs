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
    double pendingAt;
    internal Func<double> ReceiptClock;

    internal Reading Read() { lock (gate) return value; }
    internal void Reset() { lock (gate) Clear(); }
    void Clear() { flow = name = null; server = 0; value = null; pendingXp = null; }

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
        if (op != 0x3336 && op != 0x1536 && op != 0x1d56 && op != 0x5636 && op != 0x078d && op != 0x088d) return false;
        if (ReceiptClock != null) now = ReceiptClock();
        lock (gate) {
            if (op == 0x3336) {
                Reading profile;
                string profileName;
                int profileServer;
                if (!Profile(b, start, end, out profile, out profileName, out profileServer)) {
                    if (flow != key || value == null) return false;
                    Clear();
                    return true;
                }
                bool same = value != null && value.Actor == profile.Actor && flow == key && name == profileName && server == profileServer;
                profile.Activity = !same || profile.Level != value.Level || profile.Xp.HasValue && (!value.Xp.HasValue || profile.Xp > value.Xp) ? now : value.Activity;
                if (same && !profile.Secondary.HasValue) profile.Secondary = value.Secondary;
                flow = key;
                name = profileName;
                server = profileServer;
                pendingXp = null;
                value = profile;
                return true;
            }
            if (value == null || flow != key) return false;
            if (op == 0x1536) { Clear(); return true; }
            if (op == 0x1d56 && length == 10) {
                uint power = BitConverter.ToUInt32(b,p);
                if (power != BitConverter.ToUInt32(b,p+4) || value.Primary == power) return false;
                var next = value.Copy();
                next.Primary = power;
                value = next;
                return true;
            }
            if (op == 0x5636 && length == 18) {
                ulong power = BitConverter.ToUInt64(b,p);
                if (power > long.MaxValue || power != BitConverter.ToUInt64(b,p+8) || value.Secondary == power) return false;
                var next = value.Copy();
                next.Secondary = power;
                value = next;
                return true;
            }
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

internal sealed class CharacterProgressWindow : Form {
    readonly LayeredImage image = new LayeredImage();
    readonly bool power;
    string imageKey;
    Rectangle presented;
    byte alpha;
    bool dragging, interactive, resizing;
    Point pointer;
    Rectangle start;
    internal Action Clicked;
    internal Action<Rectangle> Moved;

    internal CharacterProgressWindow(bool power) {
        this.power = power;
        Text = power ? "Aion 2 Helper - Combat Power" : "Aion 2 Helper - Experience";
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
    internal void Render(Rectangle bounds, byte opacity, bool editing, string key, Func<Bitmap> render) {
        if (opacity == 0) { if (Visible) Hide(); return; }
        interactive = editing;
        if (!dragging) start = bounds;
        var position = dragging ? presented.Location : bounds.Location;
        bool changed = key != imageKey;
        if (changed) {
            using (var bitmap = render()) {
                image.Upload(bitmap);
                presented.Size = bitmap.Size;
            }
            imageKey = key;
        }
        if (!Visible || changed || opacity != alpha || position != presented.Location) {
            image.Present(Handle,position.X,position.Y,opacity);
            presented.Location = position;
            alpha = opacity;
        }
        if (!Visible) { Show(); OverlayNative.RaiseWithoutFocus(Handle); }
    }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
        if (m.Msg == 0x84) { m.Result = new IntPtr(interactive || power ? 1 : -1); return; }
        if (m.Msg == 0x201 && interactive) {
            pointer = Cursor.Position;
            var point = PointToClient(pointer);
            resizing = point.X >= Width-12 && point.Y >= Height-12;
            dragging = true;
            Capture = true;
            return;
        }
        if (m.Msg == 0x200 && dragging && !resizing) {
            var delta = Cursor.Position - (Size)pointer;
            image.Present(Handle,start.X+delta.X,start.Y+delta.Y,alpha);
            presented.Location = new Point(start.X+delta.X,start.Y+delta.Y);
            return;
        }
        if ((m.Msg == 0x202 || m.Msg == 0x215) && dragging) {
            var delta = Cursor.Position - (Size)pointer;
            var next = start;
            if (resizing) { next.Width += delta.X; next.Height += delta.Y; }
            else next.Offset(delta);
            dragging = false;
            Capture = false;
            if (Moved != null) Moved(next);
            return;
        }
        if (m.Msg == 0x202 && power && !interactive) {
            if (Clicked != null) Clicked();
            return;
        }
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing) { if (disposing) image.Dispose(); base.Dispose(disposing); }
}

internal sealed class CharacterProgressWidgets : IDisposable {
    readonly EnergyOverlay owner;
    readonly CharacterProgressWindow power = new CharacterProgressWindow(true);
    readonly CharacterProgressWindow xp = new CharacterProgressWindow(false);
    double alpha, lastTick;
    CharacterProgressSignal.Reading reading;
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
        power.Moved = bounds => Change(o=>o.Power.Bounds=bounds);
        xp.Moved = bounds => Change(o=>o.ExperienceBounds=bounds);
    }
    void Change(Action<CharacterProgressOptions> update) {
        var config = owner.Configuration;
        update(config.CharacterProgress);
        owner.ApplyConfiguration(config);
    }
    internal void Configure() { power.InvalidateImage(); xp.InvalidateImage(); }
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
        bool shell = (o.Power.Enabled || o.Experience.Enabled) && DpsDesign.ShellUiActive();
        ulong? number = live == null ? null : o.AlternatePower ? live.Secondary : live.Primary;
        if (editing && !number.HasValue) number = o.AlternatePower ? 1248ul : 120ul;
        byte powerOpacity = o.Power.Enabled && !shell && (editing || live != null) ? (byte)255 : (byte)0;
        string powerKey = number + "/" + o.AlternatePower + "/" + o.Power.Bounds.Size;
        power.Render(o.Power.Bounds,powerOpacity,editing,powerKey,()=>CharacterProgressDesign.Power(o.Power.Bounds.Size,number,o.AlternatePower,o.Power));

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
        string xpKey = level + "/" + current + "/" + o.ExperienceBounds.Size + "/" + language;
        xp.Render(o.ExperienceBounds,(byte)Math.Round(alpha*255),editing,xpKey,
            ()=>CharacterProgressDesign.Experience(o.ExperienceBounds.Size,level,current.GetValueOrDefault(),max,o.Experience,language));
        StatusKey = powerKey + "/" + power.Visible + "/" + xpKey + "/" + xp.Visible;
    }
    public void Dispose() { power.Dispose(); xp.Dispose(); }
}
