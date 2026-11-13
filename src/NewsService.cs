using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;

internal sealed class AionNewsCacheEntry {
    public string Url {get;set;}
    public string Etag {get;set;}
    public string Modified {get;set;}
    public DateTime Checked {get;set;}
    internal byte[] Data;
    public string Payload {get{return Data==null?null:Convert.ToBase64String(Data);}set{Data=value==null?null:Convert.FromBase64String(value);}}
    public string ContentType {get;set;}
}
internal sealed class AionNewsState {
    public string Region {get;set;}
    public Dictionary<string,string> Seen {get;set;}
}
internal sealed class AionNewsFeed {
    internal List<AionNewsItem> Items=new List<AionNewsItem>();
    internal bool Loading,Failed;
    internal DateTime Checked,Attempted;
    internal string Language;
    internal string Error;
}

// Independent from capture and overlay rendering. Work runs only while settings
// are open; transport, image decode and file IO never run on the dispatcher.
internal sealed class AionNewsService {
    static readonly object instancesGate=new object();
    static AionNewsService instance;
    internal static AionNewsService Current {get{lock(instancesGate)return instance??(instance=new AionNewsService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Aion2Helper","news")));}}
    readonly string folder;
    readonly object gate=new object(),diskGate=new object();
    readonly SemaphoreSlim requests=new SemaphoreSlim(1,1),pictures=new SemaphoreSlim(2,2),imageTransport=new SemaphoreSlim(2,2);
    readonly Dictionary<string,AionNewsFeed> feeds=new Dictionary<string,AionNewsFeed>();
    readonly Dictionary<string,BitmapSource> images=new Dictionary<string,BitmapSource>();
    readonly Dictionary<string,Task<BitmapSource>> imageJobs=new Dictionary<string,Task<BitmapSource>>();
    readonly Dictionary<string,CancellationToken> imageTokens=new Dictionary<string,CancellationToken>();
    readonly Dictionary<string,DateTime> imageFailures=new Dictionary<string,DateTime>();
    readonly Dictionary<string,DateTime> hostBackoff=new Dictionary<string,DateTime>();
    AionNewsState state=new AionNewsState{Region="global",Seen=new Dictionary<string,string>()};
    internal readonly AionNewsSource Sources;
    DateTime lastRequest;
    bool loaded;
    int stateVersion;
    internal event Action Changed;
    internal AionNewsService(string path){folder=path;Sources=ReadSources();}
    internal string Region {get{lock(gate)return state.Region;}}
    internal static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
    AionNewsSource ReadSources(){
        try{
            string path=Path.Combine(folder,"sources.json");if(!File.Exists(path)||new FileInfo(path).Length>8192)return AionNewsSource.Default;
            var s=new JavaScriptSerializer().Deserialize<AionNewsSource>(File.ReadAllText(path));if(s==null)return AionNewsSource.Default;
            foreach(string url in new[]{s.GlobalApi,s.KoreaApi,s.PromotionApi,s.OfficialWeb})if(!AionNewsSource.Allowed(new Uri(url),false))return AionNewsSource.Default;
            return s;
        }catch(Exception e){if(!Expected(e))throw;return AionNewsSource.Default;}
    }
    internal static bool Expected(Exception e){return e is IOException||e is UnauthorizedAccessException||e is System.Security.SecurityException||e is ArgumentException||e is InvalidOperationException||e is FormatException||e is WebException||e is OperationCanceledException||e is NotSupportedException;}
    void Notify(){var handler=Changed;if(handler!=null)handler();}
    internal bool IsUnread(AionNewsItem item){lock(gate){string revision;return !state.Seen.TryGetValue(item.Key,out revision)||revision!=item.Revision;}}
    internal bool HasUnread {get{lock(gate){foreach(var feed in feeds.Values)foreach(var item in feed.Items)if(IsUnread(item))return true;return false;}}}
    internal AionNewsFeed Snapshot(string region,string language,string kind){lock(gate){AionNewsFeed f;if(!feeds.TryGetValue(FeedKey(region,language,kind),out f))return new AionNewsFeed();return new AionNewsFeed{Items=new List<AionNewsItem>(f.Items),Loading=f.Loading,Failed=f.Failed,Checked=f.Checked,Language=f.Language,Error=f.Error};}}
    static string FeedKey(string region,string language,string kind){return kind=="helper"?"helper:en:helper":region+":"+(region=="kr"?"ko":AionNewsSource.Language(language))+":"+kind;}
    internal void SelectRegion(string region){lock(gate)state.Region=region=="kr"?"kr":"global";SaveState();}
    internal void MarkRead(AionNewsItem item){lock(gate)state.Seen[item.Key]=item.Revision;SaveState();Notify();}
    internal void MarkAll(string region,string language,string selectedKind=null){lock(gate){foreach(string kind in selectedKind=="helper"?new[]{"helper"}:new[]{"news","patch","events"}){AionNewsFeed feed;if(feeds.TryGetValue(FeedKey(region,language,kind),out feed))foreach(var item in feed.Items)state.Seen[item.Key]=item.Revision;}}SaveState();Notify();}
    void SaveState(){
        string json;int version;lock(gate){if(state.Seen.Count>2500){var trimmed=new Dictionary<string,string>();foreach(var feed in feeds.Values)foreach(var item in feed.Items){string revision;if(state.Seen.TryGetValue(item.Key,out revision))trimmed[item.Key]=revision;}state.Seen=trimmed;}json=new JavaScriptSerializer().Serialize(state);version=++stateVersion;}
        Task.Run(delegate{try{lock(diskGate){if(version!=stateVersion)return;Write("state.json",Encoding.UTF8.GetBytes(json));}}catch(Exception e){if(!Expected(e))throw;}});
    }
    internal async Task Initialize(){
        await Task.Run(delegate{lock(gate){if(loaded)return;loaded=true;try{Directory.CreateDirectory(folder);string file=Path.Combine(folder,"state.json");if(File.Exists(file)&&new FileInfo(file).Length<524288){var s=new JavaScriptSerializer().Deserialize<AionNewsState>(File.ReadAllText(file));if(s!=null&&s.Seen!=null){state=s;state.Region=state.Region=="kr"?"kr":"global";}}}catch(Exception e){if(!Expected(e))throw;}}}).ConfigureAwait(false);
    }
    internal async Task Refresh(string region,string language,bool force){
        await Initialize().ConfigureAwait(false);
        await RefreshFeed(region,language,"helper",force).ConfigureAwait(false);
        foreach(string kind in new[]{"news","patch","events"})await RefreshFeed(region,language,kind,force).ConfigureAwait(false);
        await Task.Run(new Action(TrimCache)).ConfigureAwait(false);
    }
    internal async Task RefreshFeed(string region,string language,string kind,bool force){
        string key=FeedKey(region,language,kind);AionNewsFeed feed;
        lock(gate){if(!feeds.TryGetValue(key,out feed)){feed=new AionNewsFeed();feeds[key]=feed;}if(feed.Loading||DateTime.UtcNow-feed.Attempted<TimeSpan.FromSeconds(30)||!force&&!feed.Failed&&DateTime.UtcNow-feed.Checked<TimeSpan.FromMinutes(15))return;feed.Loading=true;feed.Attempted=DateTime.UtcNow;}
        Notify();
        string url=kind=="helper"?UpdateManager.LatestUrl:Sources.Feed(region,language,kind);
        try{
            var saved=await Task.Run(delegate{return Read(url);}).ConfigureAwait(false);
            if(saved!=null){bool corrupt=false;try{var cached=ParseFeed(saved.Data,region,language,kind);lock(gate){feed.Items=cached;feed.Checked=saved.Checked;feed.Language=kind=="helper"?"en":region=="kr"?"ko":AionNewsSource.Language(language);}Notify();}catch(Exception e){if(!Expected(e))throw;corrupt=true;}if(corrupt)await Invalidate(url).ConfigureAwait(false);}
            var entry=await Fetch(url,force?TimeSpan.Zero:TimeSpan.FromMinutes(15),2097152,false,CancellationToken.None).ConfigureAwait(false);
            var items=await Task.Run(delegate{return ParseFeed(entry.Data,region,language,kind);}).ConfigureAwait(false);
            lock(gate){feed.Items=items;feed.Checked=entry.Checked;feed.Failed=false;feed.Language=kind=="helper"?"en":region=="kr"?"ko":AionNewsSource.Language(language);}
        }catch(Exception e){if(!Expected(e))throw;if(e is FormatException||e is ArgumentException)Forget(url);lock(gate){feed.Failed=true;feed.Error=e.GetType().Name+": "+e.Message;}}
        finally{lock(gate)feed.Loading=false;Notify();}
    }
    List<AionNewsItem> ParseFeed(byte[] data,string region,string language,string kind){string json=Encoding.UTF8.GetString(data);return kind=="helper"?AionNewsJson.HelperRelease(json):AionNewsJson.Feed(json,region,language,kind,Sources);}
    internal async Task<string> Article(AionNewsItem item,CancellationToken cancellation){
        if(item.Kind=="helper"){cancellation.ThrowIfCancellationRequested();return item.Html??"";}
        if(item.Kind=="events")return "";
        string url=Sources.Article(item);AionNewsCacheEntry entry=null;
        try{entry=await Fetch(url,TimeSpan.FromHours(24),2097152,false,cancellation).ConfigureAwait(false);}
        catch(Exception e){if(cancellation.IsCancellationRequested||!Expected(e))throw;}
        if(entry==null)entry=await Task.Run(delegate{return Read(url);},cancellation).ConfigureAwait(false);if(entry==null)throw new WebException("Article unavailable");
        return await Task.Run(delegate{return AionNewsJson.Body(Encoding.UTF8.GetString(entry.Data));},cancellation).ConfigureAwait(false);
    }
    internal Task<BitmapSource> Image(string url,int width,CancellationToken cancellation){
        Uri address;if(String.IsNullOrEmpty(url)||!Uri.TryCreate(url,UriKind.Absolute,out address)||!AionNewsSource.Allowed(address,true))return Task.FromResult<BitmapSource>(null);
        string key=url+"@"+width;lock(gate){BitmapSource found;Task<BitmapSource> job;DateTime failed;CancellationToken prior;if(images.TryGetValue(key,out found))return Task.FromResult(found);if(imageFailures.TryGetValue(key,out failed)&&DateTime.UtcNow-failed<TimeSpan.FromMinutes(10))return Task.FromResult<BitmapSource>(null);if(imageJobs.TryGetValue(key,out job)&&imageTokens.TryGetValue(key,out prior)&&!prior.IsCancellationRequested)return job;job=LoadImage(key,url,width,cancellation);imageJobs[key]=job;imageTokens[key]=cancellation;return job;}
    }
    internal static bool CardImage(BitmapSource image){return image!=null&&image.PixelWidth>=48&&image.PixelHeight>=40&&(long)image.PixelWidth<=5L*image.PixelHeight&&(long)image.PixelHeight<=4L*image.PixelWidth;}
    internal async Task<BitmapSource> Thumbnail(AionNewsItem item,int width,CancellationToken cancellation){
        if(item.Kind=="events")return await Image(item.Image,width,cancellation).ConfigureAwait(false);
        // Official feeds list article decorations before their actual illustrations.
        // Check a bounded set of cached thumbnail candidates; never fetch full articles for cards.
        var urls=item.Thumbnails!=null&&item.Thumbnails.Count>0?item.Thumbnails:new List<string>{item.Image};
        for(int i=0;i<Math.Min(4,urls.Count);i++){cancellation.ThrowIfCancellationRequested();var picture=await Image(urls[i],width,cancellation).ConfigureAwait(false);if(CardImage(picture))return picture;}
        return null;
    }
    async Task<BitmapSource> LoadImage(string key,string url,int width,CancellationToken cancellation){
        await Task.Delay(1).ConfigureAwait(false);BitmapSource source=null;bool acquired=false;
        try{
            await pictures.WaitAsync(cancellation).ConfigureAwait(false);acquired=true;
            AionNewsCacheEntry entry=null;
            try{entry=await Fetch(url,TimeSpan.FromDays(7),4194304,true,cancellation).ConfigureAwait(false);}
            catch(Exception e){if(cancellation.IsCancellationRequested||!Expected(e))throw;}
            if(entry==null)entry=await Task.Run(delegate{return Read(url);},cancellation).ConfigureAwait(false);if(entry==null)throw new WebException("Image unavailable");
            source=await Task.Run(delegate{
                using(var stream=new MemoryStream(entry.Data,false)){
                    var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.IgnoreColorProfile,BitmapCacheOption.None);
                    var frame=decoder.Frames[0];if(frame.PixelWidth<1||frame.PixelHeight<1||(long)frame.PixelWidth*frame.PixelHeight>32000000||frame.PixelWidth>16000||frame.PixelHeight>16000)return null;
                    int targetWidth=Math.Min(frame.PixelWidth,Math.Max(160,Math.Min(1000,width))),maxHeight=width<=320?240:1600;
                    targetWidth=Math.Max(1,Math.Min(targetWidth,(int)((long)maxHeight*frame.PixelWidth/Math.Max(1,frame.PixelHeight))));
                    stream.Position=0;var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.CreateOptions=BitmapCreateOptions.IgnoreColorProfile;image.DecodePixelWidth=targetWidth;image.StreamSource=stream;image.EndInit();image.Freeze();return (BitmapSource)image;
                }
            },cancellation).ConfigureAwait(false);
        }catch(Exception e){if(!Expected(e)&&!(e is System.Runtime.InteropServices.COMException)&&!(e is FileFormatException)&&!(e is OverflowException))throw;}
        finally{if(acquired)pictures.Release();lock(gate){CancellationToken current;if(imageTokens.TryGetValue(key,out current)&&current==cancellation){imageJobs.Remove(key);imageTokens.Remove(key);}if(source!=null){long bytes=(long)source.PixelWidth*source.PixelHeight*4;foreach(var saved in images.Values)bytes+=(long)saved.PixelWidth*saved.PixelHeight*4;if(images.Count>=36||bytes>25165824)images.Clear();images[key]=source;}else if(!cancellation.IsCancellationRequested)imageFailures[key]=DateTime.UtcNow;}}
        return source;
    }
    internal async Task<AionNewsCacheEntry> Fetch(string url,TimeSpan age,int maximum,bool image,CancellationToken cancellation){
        Uri uri=new Uri(url);if(!AionNewsSource.TransportAllowed(uri,image))throw new InvalidOperationException("Unsupported news source");
        var cached=await Task.Run(delegate{return Read(url);},cancellation).ConfigureAwait(false);
        if(cached!=null&&DateTime.UtcNow-cached.Checked<age)return cached;
        var transport=image?imageTransport:requests;
        await transport.WaitAsync(cancellation).ConfigureAwait(false);
        try{
            DateTime blocked;lock(gate)if(hostBackoff.TryGetValue(uri.Host,out blocked)&&blocked>DateTime.UtcNow)throw new WebException("News source temporarily unavailable");
            if(!image){double pause=(lastRequest.AddSeconds(1)-DateTime.UtcNow).TotalMilliseconds;if(pause>0)await Task.Delay((int)Math.Ceiling(pause),cancellation).ConfigureAwait(false);lastRequest=DateTime.UtcNow;}
            for(int redirect=0;redirect<4;redirect++){
                ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
                var request=(HttpWebRequest)WebRequest.Create(uri);request.Method="GET";request.AllowAutoRedirect=false;request.Timeout=8000;request.ReadWriteTimeout=8000;request.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;request.UserAgent="Aion2Helper-News/1.0";request.Accept=image?"image/png,image/jpeg,image/webp,image/*;q=0.8":"application/json";request.UseDefaultCredentials=false;request.CookieContainer=null;
                if(cached!=null){if(!String.IsNullOrEmpty(cached.Etag))request.Headers[HttpRequestHeader.IfNoneMatch]=cached.Etag;DateTime date;if(DateTime.TryParse(cached.Modified,out date))request.IfModifiedSince=date;}
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation)){
                    timeout.CancelAfter(8000);
                    using(timeout.Token.Register(request.Abort)){
                        HttpWebResponse response;
                        try{response=(HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);}
                        catch(WebException error){
                            response=error.Response as HttpWebResponse;
                            if(response!=null&&(int)response.StatusCode==304&&cached!=null){}
                            else {if(response!=null){int status=(int)response.StatusCode;int seconds;string retry=response.Headers["Retry-After"];if(!Int32.TryParse(retry,out seconds))seconds=300;lock(gate)hostBackoff[uri.Host]=DateTime.UtcNow.AddSeconds(status==429||status==503?Math.Max(60,Math.Min(3600,seconds)):60);response.Dispose();}throw;}
                        }
                        using(response){
                            int status=(int)response.StatusCode;
                            if(status==304&&cached!=null){cached.Checked=DateTime.UtcNow;await Store(cached).ConfigureAwait(false);return cached;}
                            if(status>=300&&status<400){Uri next;if(!Uri.TryCreate(uri,response.Headers["Location"],out next)||!AionNewsSource.TransportAllowed(next,image))throw new WebException("Unsupported redirect");uri=next;continue;}
                            string type=(response.ContentType??"").ToLowerInvariant();
                            if(response.ContentLength>maximum||image&&!type.StartsWith("image/")||!image&&!type.Contains("json"))throw new FormatException("Unsupported news response");
                            using(var input=response.GetResponseStream())using(var output=new MemoryStream()){
                                byte[] buffer=new byte[16384];int n;
                                while((n=await input.ReadAsync(buffer,0,buffer.Length,timeout.Token).ConfigureAwait(false))>0){if(output.Length+n>maximum)throw new FormatException("News response too large");output.Write(buffer,0,n);}
                                var result=new AionNewsCacheEntry{Url=url,Data=output.ToArray(),Checked=DateTime.UtcNow,Etag=response.Headers["ETag"],Modified=response.Headers["Last-Modified"],ContentType=type};
                                await Store(result).ConfigureAwait(false);return result;
                            }
                        }
                    }
                }
            }
            throw new WebException("Too many redirects");
        }finally{transport.Release();}
    }
    Task Store(AionNewsCacheEntry entry){return Task.Run(delegate{try{Write(Hash(entry.Url)+".json",Encoding.UTF8.GetBytes(new JavaScriptSerializer{MaxJsonLength=8388608}.Serialize(entry)));TrimCache();}catch(Exception e){if(!Expected(e))throw;}});}
    void Forget(string url){Invalidate(url);}
    Task Invalidate(string url){return Task.Run(delegate{try{lock(diskGate){string path=Path.Combine(folder,Hash(url)+".json");if(File.Exists(path))File.Delete(path);}}catch(Exception e){if(!Expected(e))throw;}});}
    AionNewsCacheEntry Read(string url){
        try{lock(diskGate){string file=Path.Combine(folder,Hash(url)+".json");if(!File.Exists(file)||new FileInfo(file).Length>8388608)return null;var value=new JavaScriptSerializer{MaxJsonLength=8388608}.Deserialize<AionNewsCacheEntry>(File.ReadAllText(file));return value!=null&&value.Url==url&&value.Data!=null?value:null;}}
        catch(Exception e){if(!Expected(e))throw;return null;}
    }
    void Write(string name,byte[] bytes){lock(diskGate){Directory.CreateDirectory(folder);string path=Path.Combine(folder,name),temp=path+".tmp";File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}}
    void TrimCache(){
        try{lock(diskGate){var files=new DirectoryInfo(folder).GetFiles("*.json");System.Array.Sort(files,delegate(FileInfo a,FileInfo b){return b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc);});long total=0;foreach(var f in files){if(f.Name=="state.json"||f.Name=="sources.json")continue;total+=f.Length;if(total>67108864||DateTime.UtcNow-f.LastWriteTimeUtc>TimeSpan.FromDays(30))f.Delete();}}}catch(Exception e){if(!Expected(e))throw;}
    }
}

internal static class AionNewsAssembly {
    internal static void Register(){AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){if(new System.Reflection.AssemblyName(e.Name).Name!="HtmlAgilityPack")return null;using(var input=typeof(AionNewsAssembly).Assembly.GetManifestResourceStream("News.HtmlAgilityPack.dll")){if(input==null)return null;using(var data=new MemoryStream()){input.CopyTo(data);return System.Reflection.Assembly.Load(data.ToArray());}}};}
}
