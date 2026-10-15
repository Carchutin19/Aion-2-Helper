using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

internal sealed class UpdateCancellation {
    volatile bool cancelled;readonly object gate=new object();HttpWebRequest request;
    internal void Cancel(){cancelled=true;lock(gate)if(request!=null)request.Abort();}
    internal void Check(){if(cancelled)throw new OperationCanceledException();}
    internal void Request(HttpWebRequest value){lock(gate){Check();request=value;}}
    internal void Clear(){lock(gate)request=null;}
}
internal sealed class UpdateRelease {
    internal Version Version;internal string Tag,Name,AssetName,Url,Sha256,ChecksumUrl;internal long Size;
}
internal sealed class UpdatePreferences {public bool CheckOnStartup=true;}
internal sealed class UpdateSnapshot {
    internal string State="Unchecked",Error="";internal UpdateRelease Release;internal DateTime CheckedUtc;
}
internal sealed class UpdateManager {
    internal const string Repository="Carchutin19/Aion-2-Helper";
    internal const string LatestUrl="https://api.github.com/repos/"+Repository+"/releases/latest";
    internal const long MaximumArchive=256L*1024*1024;
    static Version InstalledVersion(){var v=typeof(UpdateManager).Assembly.GetName().Version;return new Version(v.Major,v.Minor,Math.Max(0,v.Build));}
    internal static readonly Version CurrentVersion=InstalledVersion();
    internal static string CurrentLabel {get{return Label(CurrentVersion);}}
    internal static string Label(Version v){return "v"+v.Major+"."+v.Minor+"."+Math.Max(0,v.Build);}
    internal static readonly UpdateManager Shared=new UpdateManager(AppDomain.CurrentDomain.BaseDirectory);
    readonly object gate=new object();readonly string preferencesPath;UpdateSnapshot snapshot=new UpdateSnapshot();bool checking;
    internal event Action Changed;internal UpdatePreferences Preferences;
    internal UpdateManager(string root){preferencesPath=Path.Combine(root,"update-settings.json");Preferences=new UpdatePreferences();try{if(File.Exists(preferencesPath)&&new FileInfo(preferencesPath).Length<4096)Preferences=new JavaScriptSerializer().Deserialize<UpdatePreferences>(File.ReadAllText(preferencesPath))??Preferences;}catch(Exception e){if(!(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException))throw;}}
    internal UpdateSnapshot Read(){lock(gate)return new UpdateSnapshot{State=snapshot.State,Error=snapshot.Error,Release=snapshot.Release,CheckedUtc=snapshot.CheckedUtc};}
    internal void StartupPreference(bool enabled){Preferences.CheckOnStartup=enabled;File.WriteAllText(preferencesPath,new JavaScriptSerializer().Serialize(Preferences),new UTF8Encoding(false));}
    void Publish(UpdateSnapshot next){lock(gate)snapshot=next;var callback=Changed;if(callback!=null)callback();}
    internal void CheckAsync(UpdateCancellation cancellation=null){lock(gate){if(checking)return;checking=true;}Publish(new UpdateSnapshot{State="Checking"});ThreadPool.QueueUserWorkItem(delegate{var next=new UpdateSnapshot{CheckedUtc=DateTime.UtcNow};try{var token=cancellation??new UpdateCancellation();UpdateRelease release;using(var timeout=new Timer(delegate{token.Cancel();},null,7000,Timeout.Infinite))release=Parse(DownloadText(LatestUrl,2*1024*1024,token),CurrentVersion);next.Release=release;next.State=release.Version>CurrentVersion?"Available":"Current";}catch(Exception ex){next.State="Failed";next.Error=Error(ex);}finally{lock(gate)checking=false;}Publish(next);});}
    internal static string Error(Exception ex){if(ex is OperationCanceledException)return "Update check cancelled.";var web=ex as WebException;var response=web==null?null:web.Response as HttpWebResponse;if(response!=null&&(response.StatusCode==HttpStatusCode.Forbidden||(int)response.StatusCode==429))return "GitHub request limit reached. Please try again later.";if(web!=null)return "Could not connect to GitHub. Check your connection and try again.";if(ex is UnauthorizedAccessException)return "Windows denied access. Restart the helper as administrator and try again.";return ex.Message;}
    internal static Version ParseVersion(string tag){if(tag==null||!Regex.IsMatch(tag,@"^v?\d+\.\d+\.\d+$"))throw new InvalidDataException("Unsupported release version.");Version version;if(!Version.TryParse(tag.TrimStart('v'),out version))throw new InvalidDataException("Invalid release version.");return version;}
    internal static UpdateRelease Parse(string source,Version current){var row=new JavaScriptSerializer{MaxJsonLength=2*1024*1024}.Deserialize<Dictionary<string,object>>(source);object data;if(row==null||!row.ContainsKey("draft")||!row.ContainsKey("prerelease")||Convert.ToBoolean(row["draft"])||Convert.ToBoolean(row["prerelease"]))throw new InvalidDataException("No published release is available.");string tag=Convert.ToString(row["tag_name"]);var version=ParseVersion(tag);if(tag!="v"+version.ToString(3))throw new InvalidDataException("Unsupported release tag.");string name="Aion-2-Helper-"+tag+"-windows-x64.zip",checksum=name.Substring(0,name.Length-4)+".sha256";var result=new UpdateRelease{Version=version,Tag=tag,Name=Convert.ToString(row["name"]),AssetName=name};if(!row.TryGetValue("assets",out data))throw new InvalidDataException("Release has no Windows download.");var assets=data as System.Collections.IEnumerable;if(assets==null)throw new InvalidDataException("Invalid release assets.");int matches=0;
        foreach(var item in assets){var asset=item as Dictionary<string,object>;if(asset==null)continue;string file=Convert.ToString(asset["name"]);if(file!=name&&file!=checksum)continue;string url=Convert.ToString(asset["browser_download_url"]);if(url!="https://github.com/"+Repository+"/releases/download/"+tag+"/"+file)throw new InvalidDataException("Unexpected update download location.");if(file==name){matches++;result.Url=url;result.Size=Convert.ToInt64(asset["size"]);object digest;if(asset.TryGetValue("digest",out digest)){string value=Convert.ToString(digest);if(value.StartsWith("sha256:",StringComparison.Ordinal)&&Regex.IsMatch(value.Substring(7),"^[0-9a-fA-F]{64}$"))result.Sha256=value.Substring(7).ToLowerInvariant();}}else result.ChecksumUrl=url;}
        if(matches!=1||result.Size<=0||result.Size>MaximumArchive)throw new InvalidDataException("No supported Windows package is available.");if(result.Sha256==null&&result.ChecksumUrl==null)throw new InvalidDataException("The release has no SHA-256 checksum.");return result;
    }
    static bool TrustedRedirect(Uri uri){if(uri.Scheme!="https"||uri.Port!=443||uri.UserInfo.Length!=0)return false;return uri.Host=="release-assets.githubusercontent.com"||uri.Host=="objects.githubusercontent.com"||uri.Host=="github-releases.githubusercontent.com"||(uri.Host=="github.com"&&uri.AbsolutePath.StartsWith("/"+Repository+"/releases/download/",StringComparison.Ordinal));}
    static HttpWebResponse Open(string url,UpdateCancellation cancellation){var uri=new Uri(url);bool api=url==LatestUrl;for(int redirect=0;redirect<5;redirect++){cancellation.Check();if(!api&&!TrustedRedirect(uri))throw new InvalidDataException("Untrusted update redirect.");ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(uri);request.UserAgent="Aion2Helper/"+CurrentLabel;request.Accept=api?"application/vnd.github+json":"application/octet-stream";request.Timeout=6000;request.ReadWriteTimeout=12000;request.AllowAutoRedirect=false;cancellation.Request(request);var response=(HttpWebResponse)request.GetResponse();int status=(int)response.StatusCode;if(status>=300&&status<400){string location=response.Headers["Location"];response.Dispose();if(api||string.IsNullOrEmpty(location))throw new InvalidDataException("Unexpected GitHub response.");uri=new Uri(uri,location);continue;}if(status!=200){response.Dispose();throw new InvalidDataException("Unexpected download response.");}return response;}throw new InvalidDataException("Too many update redirects.");}
    internal static string DownloadText(string url,int maximum,UpdateCancellation cancellation){try{using(var response=Open(url,cancellation))using(var input=response.GetResponseStream())using(var output=new MemoryStream()){var buffer=new byte[8192];int n;while((n=input.Read(buffer,0,buffer.Length))>0){cancellation.Check();if(output.Length+n>maximum)throw new InvalidDataException("Update metadata is too large.");output.Write(buffer,0,n);}return new UTF8Encoding(false,true).GetString(output.ToArray());}}finally{cancellation.Clear();}}
    internal static string Hash(string path){using(var hash=SHA256.Create())using(var input=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(input)).Replace("-","").ToLowerInvariant();}
    internal static void Download(UpdateRelease release,string destination,UpdateCancellation cancellation,Action<long,long> progress){long total=0;try{using(var response=Open(release.Url,cancellation))using(var input=response.GetResponseStream())using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None)){if(response.ContentLength>0&&response.ContentLength!=release.Size)throw new InvalidDataException("Download size does not match the release.");var buffer=new byte[65536];int n;var clock=Stopwatch.StartNew();long last=0;while((n=input.Read(buffer,0,buffer.Length))>0){cancellation.Check();if(clock.Elapsed.TotalMinutes>10||total+n>release.Size||total+n>MaximumArchive)throw new InvalidDataException("Download limit exceeded.");output.Write(buffer,0,n);total+=n;if(clock.ElapsedMilliseconds-last>=100){last=clock.ElapsedMilliseconds;progress(total,release.Size);}}}if(total!=release.Size)throw new InvalidDataException("The update download is incomplete.");progress(total,total);cancellation.Check();string expected=release.Sha256;if(expected==null){string text=DownloadText(release.ChecksumUrl,4096,cancellation).Trim();var match=Regex.Match(text,@"^([0-9a-fA-F]{64})\s+\*?"+Regex.Escape(release.AssetName)+@"$");if(!match.Success)throw new InvalidDataException("Invalid release checksum.");expected=match.Groups[1].Value;}if(!string.Equals(Hash(destination),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update checksum failed. The download was not installed.");}finally{cancellation.Clear();}}
}
