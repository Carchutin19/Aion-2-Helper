using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

internal sealed class OdyleOptions {
    internal static readonly bool Available=false;
    public FpsOptions Widget = new FpsOptions { Color = "#FFFFFF", Bounds = new Rectangle(30,180,152,44) };
    public bool ShowMaximum = true;

    internal OdyleOptions Copy() { return new OdyleOptions { Widget=Widget.Copy(), ShowMaximum=ShowMaximum }; }
    internal static bool Same(OdyleOptions a, OdyleOptions b) {
        return a.ShowMaximum==b.ShowMaximum && FpsOptions.Same(a.Widget,b.Widget);
    }
    internal void Normalize() {
        if (Widget==null) Widget=new OdyleOptions().Widget;
        Widget.Normalize();
    }
    internal static OdyleOptions Read(Dictionary<string,object> settings) {
        var options=new OdyleOptions();
        object value;
        if (settings.TryGetValue("odyleEnergy",out value)) {
            try { options=NativeHudFile.Json().ConvertToType<OdyleOptions>(value)??options; }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
        options.Normalize();
        return options;
    }
}

internal sealed class OdyleLedger {
    internal const int StandardMaximum=560, MemberMaximum=840, Recharge=15, IntervalMinutes=180;
    static readonly long IntervalTicks=TimeSpan.FromMinutes(IntervalMinutes).Ticks;
    internal sealed class Entry {
        public string Name;
        public int Server, Maximum=StandardMaximum;
        public int? Energy;
        public long NextUtcTicks;

        internal Reading Read(DateTime now) {
            if (!Energy.HasValue) return new Reading();
            if (Energy.Value>=Maximum) return new Reading { Energy=Maximum };
            long elapsed=now.Ticks-NextUtcTicks;
            long pulses=elapsed<0?0:elapsed/IntervalTicks+1;
            int current=(int)Math.Min(Maximum, Energy.Value+pulses*Recharge);
            if (current==Maximum) return new Reading { Energy=current };
            long next=NextUtcTicks+pulses*IntervalTicks;
            int remaining=(Maximum-current+Recharge-1)/Recharge;
            return new Reading { Energy=current, NextUtc=new DateTime(next,DateTimeKind.Utc),
                FullUtc=new DateTime(next+(remaining-1)*IntervalTicks,DateTimeKind.Utc) };
        }
    }
    internal struct Reading {
        internal int? Energy;
        internal DateTime? NextUtc, FullUtc;
    }

    readonly string path;
    readonly bool preview;
    readonly List<Entry> entries=new List<Entry>();
    bool unreadable;
    internal string Error { get; private set; }
    internal int Revision { get; private set; }
    internal Entry Active { get; private set; }
    internal IEnumerable<Entry> Entries { get { return entries; } }

    internal OdyleLedger(string root,bool preview) {
        this.preview=preview;
        path=Path.Combine(root,"odyle-characters.json");
        if (preview || !File.Exists(path)) return;
        try {
            if (new FileInfo(path).Length>128*1024) throw new InvalidDataException();
            var saved=NativeHudFile.Json().Deserialize<List<Entry>>(File.ReadAllText(path));
            if (saved==null || saved.Count>128) throw new InvalidDataException();
            foreach (var entry in saved) {
                if (entry==null || !ValidIdentity(entry.Name,entry.Server) || Find(entry.Name,entry.Server)!=null
                    || !ValidBalance(entry.Energy,entry.Maximum)
                    || (entry.Energy<entry.Maximum ? entry.NextUtcTicks<=0 || entry.NextUtcTicks>DateTime.MaxValue.Ticks-100*IntervalTicks : entry.NextUtcTicks!=0))
                    throw new InvalidDataException();
                entries.Add(entry);
            }
        } catch (Exception error) {
            if (!FileError(error)) throw;
            unreadable=true;
            entries.Clear();
            Error="Could not load saved Odyle energy.";
        }
    }
    static bool FileError(Exception error) {
        return error is IOException || error is InvalidDataException || error is UnauthorizedAccessException
            || error is ArgumentException || error is InvalidOperationException || error is System.Security.SecurityException;
    }
    static bool ValidIdentity(string name,int server) {
        return !string.IsNullOrWhiteSpace(name) && name.Length<=64 && name.Trim()==name && !name.Any(char.IsControl)
            && server>=1000 && server<3000;
    }
    static bool ValidBalance(int? energy,int maximum) {
        return (maximum==StandardMaximum || maximum==MemberMaximum) && (!energy.HasValue || energy>=0 && energy<=maximum);
    }
    Entry Find(string name,int server) {
        return entries.Find(x=>x.Server==server && string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase));
    }
    internal void Observe(string name,int server) {
        if (Active!=null && Active.Server==server && string.Equals(Active.Name,name,StringComparison.OrdinalIgnoreCase)) return;
        if (Active==null && name==null) return;
        Active=name==null?null:Find(name,server);
        if (Active==null && ValidIdentity(name,server)) {
            Active=new Entry { Name=name,Server=server };
            if (Replace(null,Active)) return;
        }
        Revision++;
    }
    internal bool Save(string name,int server,int energy,int maximum,int nextMinutes,DateTime now) {
        if (!ValidIdentity(name,server) || !ValidBalance(energy,maximum) || nextMinutes<1 || nextMinutes>IntervalMinutes) return false;
        var entry=new Entry { Name=name,Server=server,Energy=energy,Maximum=maximum,
            NextUtcTicks=energy<maximum?now.AddMinutes(nextMinutes).Ticks:0 };
        return Replace(Find(name,server),entry);
    }
    internal bool Spend(Entry entry,int amount,DateTime now) {
        var reading=entry.Read(now);
        if (amount<=0 || !reading.Energy.HasValue || reading.Energy.Value<amount) return false;
        return Replace(entry,new Entry { Name=entry.Name,Server=entry.Server,Maximum=entry.Maximum,
            Energy=reading.Energy.Value-amount,NextUtcTicks=(reading.NextUtc??now.AddMinutes(IntervalMinutes)).Ticks });
    }
    internal bool Remove(Entry entry) { return Replace(entry,null); }
    bool Replace(Entry previous,Entry replacement) {
        if (unreadable) return false;
        var next=new List<Entry>(entries);
        if (previous!=null && !next.Remove(previous)) return false;
        if (replacement!=null) next.Add(replacement);
        if (next.Count>128) return false;
        try {
            if (!preview) {
                VisualTweaks.NoLinks(path);
                NativeHotbar.Atomic(path,Encoding.UTF8.GetBytes(NativeHudFile.Json().Serialize(next)));
            }
        } catch (Exception error) {
            if (!FileError(error)) throw;
            Error="Could not save Odyle energy.";
            return false;
        }
        entries.Clear();
        entries.AddRange(next);
        if (Active!=null) Active=Find(Active.Name,Active.Server)??new Entry { Name=Active.Name,Server=Active.Server };
        Error=null;
        Revision++;
        return true;
    }
}

internal static class OdyleDesign {
    // Draw the supplied image through a silhouette so the existing widget background remains visible.
    static readonly Lazy<Bitmap> Gem=new Lazy<Bitmap>(LoadGem);
    static Bitmap LoadGem() {
        using (var stream=typeof(OdyleDesign).Assembly.GetManifestResourceStream("OdyleGem.jpg"))
        using (var source=Image.FromStream(stream)) {
            var gem=new Bitmap(128,128,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g=Graphics.FromImage(gem))
            using (var outline=new GraphicsPath()) {
                outline.AddPolygon(new[] { new Point(16,116),new Point(17,83),new Point(30,44),new Point(34,38),
                    new Point(61,26),new Point(97,0),new Point(109,1),new Point(111,7),new Point(115,1),
                    new Point(120,17),new Point(117,44),new Point(108,64),new Point(106,83),new Point(92,94),
                    new Point(63,109),new Point(23,122),new Point(17,120) });
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.SetClip(outline);
                g.DrawImage(source,new Rectangle(0,0,128,128));
            }
            return gem;
        }
    }
    internal static string Value(int energy,int maximum,bool showMaximum) {
        return energy.ToString("N0",CultureInfo.CurrentCulture)
            +(showMaximum?" / "+maximum.ToString("N0",CultureInfo.CurrentCulture):"");
    }
    internal static Bitmap Render(Size size,int energy,int maximum,OdyleOptions options) {
        var bitmap=FpsDesign.Render(size.Width,size.Height,"",options.Widget,false);
        using (var g=Graphics.FromImage(bitmap))
        using (var painter=new CrispOverlayText()) {
            int icon=Math.Max(8,Math.Min(size.Height*2/3,size.Width/4));
            int gap=Math.Max(3,icon/5), available=Math.Max(1,size.Width-icon-gap-8);
            int fontSize=Math.Max(6,(int)Math.Round(size.Height*.52));
            string value=Value(energy,maximum,options.ShowMaximum);
            int width;
            using (var font=new Font("Segoe UI",fontSize,FontStyle.Regular,GraphicsUnit.Pixel))
                width=TextRenderer.MeasureText(value,font,Size.Empty,TextFormatFlags.NoPadding).Width+2;
            if (width>available) fontSize=Math.Max(4,fontSize*available/width);
            width=Math.Min(width,available);
            int left=(size.Width-width-gap-icon)/2;
            painter.Draw(g,value,fontSize,false,ColorTranslator.FromHtml(options.Widget.Color),new Rectangle(left,0,width,size.Height),1,true);
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.DrawImage(Gem.Value,new Rectangle(left+width+gap,(size.Height-icon)/2,icon,icon));
        }
        return bitmap;
    }
}

internal sealed class OdyleWidget : IDisposable {
    readonly MovableOverlayWindow window=new MovableOverlayWindow("Odyle Energy");
    OdyleOptions shown;
    int lastEnergy,lastMaximum;
    string key;
    long revision;
    Func<Size,Bitmap> paint;
    internal OdyleWidget(EnergyOverlay owner) {
        window.SnapOwner=owner;
        window.LimitResize=size=>new Size(Math.Max(48,Math.Min(1600,size.Width)),Math.Max(20,Math.Min(400,size.Height)));
        window.Moved=bounds=>{
            var config=owner.Configuration;
            config.Odyle.Widget.Bounds=bounds;
            owner.ApplyConfiguration(config);
        };
    }
    internal void Maintain(OdyleOptions options,OdyleLedger.Entry active,DateTime now,bool editing,bool hidden) {
        if (!OdyleOptions.Available || !options.Widget.Enabled || hidden) { window.HideOverlay(); return; }
        var reading=active==null?new OdyleLedger.Reading():active.Read(now);
        if (!editing && !reading.Energy.HasValue) { window.HideOverlay(); return; }
        int value=reading.Energy??325, maximum=active==null?OdyleLedger.StandardMaximum:active.Maximum;
        if (shown!=options || lastEnergy!=value || lastMaximum!=maximum) {
            shown=options;
            lastEnergy=value;
            lastMaximum=maximum;
            key=(++revision).ToString(CultureInfo.InvariantCulture);
            paint=size=>OdyleDesign.Render(size,value,maximum,options);
        }
        window.Render(options.Widget.Bounds,window.SnapOwner.HudOpacity(),editing,key,paint);
    }
    public void Dispose() { window.Dispose(); }
}
