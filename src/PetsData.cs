using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Script.Serialization;

// Original implementation. See PET-ASSETS.md for independently licensed data,
// original game-art ownership, pinned provenance and MIT protocol references.
internal sealed class PetOptions {
    public bool Enabled=true,Widget=true,Blur=true,Outline=false,Glow=false;
    public int Scale=100,FontSize=14,WhiteOpacity=38,Opacity=100,MaxCards=5,OutlineWidth=1;
    public double Seconds=5;
    public string TextColor="#1D4E89",AccentColor="#2C78B8";
    public Rectangle Bounds=new Rectangle(40,330,380,86),AtlasBounds=new Rectangle(280,170,1060,680);
    public int AtlasWorld=0;
    internal PetOptions Copy(){return (PetOptions)MemberwiseClone();}
    internal void Normalize(){Scale=Clamp(Scale,50,200);FontSize=Clamp(FontSize,10,24);WhiteOpacity=Clamp(WhiteOpacity,5,90);Opacity=Clamp(Opacity,10,100);MaxCards=Clamp(MaxCards,1,7);OutlineWidth=Clamp(OutlineWidth,1,3);Seconds=double.IsNaN(Seconds)||double.IsInfinity(Seconds)?5:Math.Max(1,Math.Min(30,Seconds));if(!EnergyBarOptions.ValidColor(TextColor))TextColor="#1D4E89";if(!EnergyBarOptions.ValidColor(AccentColor))AccentColor="#2C78B8";Bounds=new Rectangle(Clamp(Bounds.X,-100000,100000),Clamp(Bounds.Y,-100000,100000),Clamp(Bounds.Width,320,800),86);AtlasBounds=new Rectangle(Clamp(AtlasBounds.X,-100000,100000),Clamp(AtlasBounds.Y,-100000,100000),Clamp(AtlasBounds.Width,700,1800),Clamp(AtlasBounds.Height,480,1200));}
    static int Clamp(int n,int low,int high){return Math.Max(low,Math.Min(high,n));}
    internal Rectangle Scaled {get{int w=Bounds.Width*Scale/100,h=86*Scale/100;return new Rectangle(Bounds.X+Bounds.Width/2-w/2,Bounds.Y+43-h/2,w,h);}}
    internal static bool Same(PetOptions a,PetOptions b){return a.Enabled==b.Enabled&&a.Widget==b.Widget&&a.Blur==b.Blur&&a.Outline==b.Outline&&a.Glow==b.Glow&&a.Scale==b.Scale&&a.FontSize==b.FontSize&&a.WhiteOpacity==b.WhiteOpacity&&a.Opacity==b.Opacity&&a.MaxCards==b.MaxCards&&a.OutlineWidth==b.OutlineWidth&&a.Seconds==b.Seconds&&a.TextColor==b.TextColor&&a.AccentColor==b.AccentColor&&a.Bounds==b.Bounds&&a.AtlasBounds==b.AtlasBounds&&a.AtlasWorld==b.AtlasWorld;}
    internal static PetOptions Read(Dictionary<string,object> data){object raw;var o=new PetOptions();if(data.TryGetValue("pets",out raw))try{o=new JavaScriptSerializer().ConvertToType<PetOptions>(raw)??o;}catch(ArgumentException){}catch(InvalidOperationException){}o.Normalize();return o;}
}
#pragma warning disable 0649 // Public DTO fields are populated by JavaScriptSerializer.
internal sealed class PetDefinition {public int Id;public string Name,Genus;public PetDropper[] Droppers;}
internal sealed class PetDropper {public int NpcCode,Level;public string Name;}
internal sealed class PetAcquisition {public int Id;public bool Verified;public string Source,Kind;public string[] Facts;}
internal sealed class PetWorld {public string name,key;public double[] sectors;internal int Id;}
internal sealed class PetPoint {internal int Pet,World,Npc;internal string Variant,Area;internal double X,Y;}
internal sealed class PetMapRoot {public Dictionary<string,PetWorld> worlds;public Dictionary<string,PetMapNpc[]> pets;public string fetched;}
internal sealed class PetMapNpc {public int npc;public string name;public PetMapRegion[] worlds;}
internal sealed class PetMapRegion {public int world;public string area;public double[][] points;}
#pragma warning restore 0649
internal sealed class PetAssets:IDisposable {
    readonly object gate=new object();ZipArchive archive;Stream file;
    readonly Dictionary<int,Bitmap> portraits=new Dictionary<int,Bitmap>();readonly Dictionary<int,System.Windows.Media.Imaging.BitmapSource> images=new Dictionary<int,System.Windows.Media.Imaging.BitmapSource>();
    internal readonly Dictionary<int,PetDefinition> Pets=new Dictionary<int,PetDefinition>();
    internal readonly Dictionary<int,PetWorld> Worlds=new Dictionary<int,PetWorld>();
    internal readonly Dictionary<int,List<PetPoint>> Locations=new Dictionary<int,List<PetPoint>>();
    internal readonly Dictionary<int,PetAcquisition> Sources=new Dictionary<int,PetAcquisition>();
    internal string Error,Date;internal bool Ready {get{return archive!=null&&Error==null;}}
    internal PetAssets(string root){try{
        file=typeof(PetAssets).Assembly.GetManifestResourceStream("PetAssets.zip");if(file==null){string path=Path.Combine(root,"assets/pets/pets.zip");if(!File.Exists(path)){Error="Pet asset pack is missing";return;}file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);}archive=new ZipArchive(file,ZipArchiveMode.Read,false);
        if(archive.Entries.Count>512)throw new InvalidDataException("Invalid pet asset pack");
        var serializer=new JavaScriptSerializer{MaxJsonLength=4000000};var definitions=serializer.Deserialize<PetDefinition[]>(Text("data/pet-catalog.json"));
        if(definitions==null||definitions.Length>512)throw new InvalidDataException("Invalid pet catalogue");
        foreach(var p in definitions){if(p.Id<=0||string.IsNullOrEmpty(p.Name)||Pets.ContainsKey(p.Id))throw new InvalidDataException("Invalid pet definition");Pets.Add(p.Id,p);}
        var maps=serializer.Deserialize<PetMapRoot>(Text("data/maps/spawns.json"));Date=maps.fetched;
        foreach(var pair in maps.worlds){int id;if(!int.TryParse(pair.Key,out id)||id<=0||maps.worlds.Count>16)throw new InvalidDataException("Invalid pet world");var w=pair.Value;if(w.sectors==null||w.sectors.Length!=5||w.sectors[2]<=0||w.sectors[3]<=0||w.sectors[4]<=0||string.IsNullOrEmpty(w.key)||w.key.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'))throw new InvalidDataException("Invalid map geometry");w.Id=id;Worlds.Add(id,w);}
        int count=0;foreach(var pet in maps.pets){int id;if(!int.TryParse(pet.Key,out id)||!Pets.ContainsKey(id))continue;var points=new List<PetPoint>();foreach(var npc in pet.Value)foreach(var region in npc.worlds){PetWorld world;if(!Worlds.TryGetValue(region.world,out world))continue;foreach(var xy in region.points){if(++count>100000||xy.Length!=2)throw new InvalidDataException("Invalid map points");double x=(xy[0]-world.sectors[0])/(world.sectors[2]*world.sectors[4]),y=1-(xy[1]-world.sectors[1])/(world.sectors[3]*world.sectors[4]);if(double.IsNaN(x)||double.IsNaN(y)||x<0||x>1||y<0||y>1)continue;points.Add(new PetPoint{Pet=id,World=region.world,Npc=npc.npc,Variant=npc.name,Area=region.area,X=x,Y=y});}}Locations[id]=points;}
        foreach(var source in serializer.Deserialize<PetAcquisition[]>(Text("data/pet-sources.json")))if(Pets.ContainsKey(source.Id))Sources[source.Id]=source;
    }catch(Exception ex){if(!(ex is IOException||ex is ArgumentException||ex is InvalidOperationException||ex is InvalidDataException||ex is NullReferenceException||ex is KeyNotFoundException))throw;Error=ex.Message;if(archive!=null){archive.Dispose();archive=null;}if(file!=null){file.Dispose();file=null;}}}
    string Text(string name){var entry=archive.GetEntry(name);if(entry==null||entry.Length>4000000)throw new InvalidDataException("Missing pet data: "+name);using(var r=new StreamReader(entry.Open()))return r.ReadToEnd();}
    internal string Name(int id){PetDefinition p;return Pets.TryGetValue(id,out p)?p.Name:"Pet #"+id;}
    internal int[] Maps(int id){List<PetPoint> points;return Locations.TryGetValue(id,out points)?points.Select(p=>p.World).Distinct().OrderBy(n=>n).ToArray():new int[0];}
    internal Bitmap Portrait(int id){lock(gate){Bitmap found;if(portraits.TryGetValue(id,out found))return found;if(!Ready)return null;var entry=archive.GetEntry("data/pets/"+id+".png");if(entry==null||entry.Length>1000000)return null;using(var stream=entry.Open())using(var image=Image.FromStream(stream)){if(image.Width>256||image.Height>256)return null;found=new Bitmap(image);}portraits[id]=found;return found;}}
    internal System.Windows.Media.Imaging.BitmapSource PortraitSource(int id){lock(gate){System.Windows.Media.Imaging.BitmapSource image;if(images.TryGetValue(id,out image))return image;var bitmap=Portrait(id);if(bitmap==null)return null;image=SettingsVisual.Bitmap(bitmap);images[id]=image;return image;}}
    internal Bitmap Map(int id){byte[] bytes;lock(gate){PetWorld world;if(!Ready||!Worlds.TryGetValue(id,out world))return null;var entry=archive.GetEntry("data/maps/"+world.key+".png");if(entry==null||entry.Length>16000000)return null;using(var s=entry.Open())using(var memory=new MemoryStream()){s.CopyTo(memory);bytes=memory.ToArray();}}using(var stream=new MemoryStream(bytes,false))using(var image=Image.FromStream(stream)){if((long)image.Width*image.Height>40000000)throw new InvalidDataException("Map bitmap too large");return new Bitmap(image);}}
    public void Dispose(){lock(gate){foreach(var b in portraits.Values)b.Dispose();portraits.Clear();images.Clear();if(archive!=null)archive.Dispose();if(file!=null)file.Dispose();archive=null;file=null;}}
}
internal sealed class PetProgress {
    internal int Level,Current,Goal,Remaining;internal long Total;
    internal static PetProgress From(long total){if(total<0)return null;int level=total<5?0:total<30?1:total<105?2:3;int basis=level==0?0:level==1?5:level==2?30:105,goal=level==0?5:level==1?25:level==2?75:0;return new PetProgress{Total=total,Level=level,Current=level==3?0:(int)(total-basis),Goal=goal,Remaining=level==3?0:goal-(int)(total-basis)};}
}
