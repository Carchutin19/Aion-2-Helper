using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Web.Script.Serialization;

// Effect IDs are not skill IDs. Metadata is immutable and loaded once; never
// perform network requests, classification guesses or archive scans per frame.
internal static class EffectCatalogue {
    internal sealed class Entry {
        public string icon {get;set;}public string kind {get;set;}public string en {get;set;}public string es {get;set;}public string de {get;set;}public string fr {get;set;}
        public int stacks=1;
        public bool hidden {get;set;}
        internal string Name(string language){
            string name=language=="es"?es:language=="de"?de:language=="fr"?fr:en;
            return string.IsNullOrEmpty(name)?en:name;
        }
    }
    static readonly Dictionary<string,Entry> entries=Load();
    static Dictionary<string,Entry> Load(){
        using(var stream=typeof(EffectCatalogue).Assembly.GetManifestResourceStream("EffectCatalogue.json")){
            if(stream==null)return new Dictionary<string,Entry>();
            using(var reader=new StreamReader(stream))return new JavaScriptSerializer{MaxJsonLength=5000000}.Deserialize<Dictionary<string,Entry>>(reader.ReadToEnd());
        }
    }
    internal static Entry Find(uint id){Entry entry;return entries.TryGetValue(id.ToString(System.Globalization.CultureInfo.InvariantCulture),out entry)?entry:null;}
    internal static bool Visible(uint id){var e=Find(id);return e!=null&&!e.hidden&&(e.kind=="B"||e.kind=="D");}
    internal static bool Harmful(uint id){var e=Find(id);return e!=null&&e.kind=="D";}
    internal static string Name(uint id,uint skill,string language){var e=Find(id);string name=e==null?null:e.Name(language);return !string.IsNullOrEmpty(name)?name:skill!=0?SkillCatalogue.Shared.Name(skill,language):"#"+id;}
}

internal sealed class EffectIconAssets:IDisposable {
    readonly Dictionary<string,Bitmap> cache=new Dictionary<string,Bitmap>();
    readonly Queue<string> order=new Queue<string>();
    Stream stream;ZipArchive archive;
    internal Bitmap Icon(uint id){
        var entry=EffectCatalogue.Find(id);if(entry==null||string.IsNullOrEmpty(entry.icon))return null;
        string token=entry.icon;if(token.Length>100||token.Contains("/")||token.Contains("\\"))return null;
        Bitmap bitmap;if(cache.TryGetValue(token,out bitmap))return bitmap;
        if(archive==null){stream=typeof(EffectIconAssets).Assembly.GetManifestResourceStream("EffectIcons.zip");if(stream!=null)archive=new ZipArchive(stream,ZipArchiveMode.Read);}
        var file=archive==null?null:archive.GetEntry(token+".png");
        if(file!=null&&file.Length<=200000)using(var input=file.Open())using(var image=Image.FromStream(input))if(image.Width<=256&&image.Height<=256){
            bitmap=new Bitmap(Math.Min(96,image.Width),Math.Min(96,image.Height),System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using(var graphics=Graphics.FromImage(bitmap)){graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,new Rectangle(0,0,bitmap.Width,bitmap.Height));}
        }
        if(cache.Count>=128){string oldest=order.Dequeue();var old=cache[oldest];if(old!=null)old.Dispose();cache.Remove(oldest);}
        cache[token]=bitmap;order.Enqueue(token);return bitmap;
    }
    public void Dispose(){foreach(var image in cache.Values)if(image!=null)image.Dispose();cache.Clear();if(archive!=null)archive.Dispose();if(stream!=null)stream.Dispose();}
}
