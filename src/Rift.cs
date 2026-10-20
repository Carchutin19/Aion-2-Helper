using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

// Independently implemented calendar. Global community timetable reference:
// https://shugo.gg/timers (reviewed 2026-10-20). Scheduled, not a live spawn signal.
internal sealed class RiftOptions {
    public bool Opening=true,Warning=true,Closing=true;
    public int LeadMinutes=5;
    public string Region="auto";
    public RiftCounterOptions Counter=new RiftCounterOptions();
    internal RiftOptions Copy(){var o=(RiftOptions)MemberwiseClone();o.Counter=(Counter??new RiftCounterOptions()).Copy();return o;}
    internal void Normalize(){if(Region!="auto"&&RiftCalendar.Zone(Region)==null)Region="auto";LeadMinutes=Math.Max(1,Math.Min(10,LeadMinutes));if(Counter==null)Counter=new RiftCounterOptions();Counter.Normalize();}
    internal bool NeedsIdentity(bool notifications){return Region=="auto"&&(Counter.Enabled||notifications&&(Opening||Warning||Closing));}
    internal static bool Same(RiftOptions a,RiftOptions b){return a.Region==b.Region&&a.Opening==b.Opening&&a.Warning==b.Warning&&a.Closing==b.Closing&&a.LeadMinutes==b.LeadMinutes&&RiftCounterOptions.Same(a.Counter,b.Counter);}
}
internal sealed class RiftCounterOptions {
    public bool Enabled=true,IconGlow=true,Outline=true;
    public int ScalePercent=100,FontSize=24,GlowPercent=65,BackgroundOpacity=210,Opacity=100,OutlineWidth=1;
    public string Color="#EAD9FF";
    public Rectangle Bounds=new Rectangle(790,230,340,82);
    internal RiftCounterOptions Copy(){return (RiftCounterOptions)MemberwiseClone();}
    internal void Normalize(){ScalePercent=Math.Max(50,Math.Min(300,ScalePercent));FontSize=Math.Max(12,Math.Min(48,FontSize));GlowPercent=Math.Max(0,Math.Min(100,GlowPercent));BackgroundOpacity=Math.Max(0,Math.Min(255,BackgroundOpacity));Opacity=Math.Max(0,Math.Min(100,Opacity));OutlineWidth=Math.Max(1,Math.Min(4,OutlineWidth));if(!EnergyBarOptions.ValidColor(Color))Color="#EAD9FF";Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(260,Math.Min(900,Bounds.Width)),82);}
    internal Rectangle ActualBounds {get{int w=(int)Math.Round(Bounds.Width*ScalePercent/100.0),h=(int)Math.Round(Bounds.Height*ScalePercent/100.0);return new Rectangle(Bounds.X+Bounds.Width/2-w/2,Bounds.Y+Bounds.Height/2-h/2,w,h);}}
    internal static bool Same(RiftCounterOptions a,RiftCounterOptions b){return SameAppearance(a,b)&&a.Enabled==b.Enabled&&a.Bounds==b.Bounds&&a.Opacity==b.Opacity;}
    internal static bool SameAppearance(RiftCounterOptions a,RiftCounterOptions b){return a.ScalePercent==b.ScalePercent&&a.FontSize==b.FontSize&&a.GlowPercent==b.GlowPercent&&a.IconGlow==b.IconGlow&&a.BackgroundOpacity==b.BackgroundOpacity&&a.Color==b.Color&&a.Outline==b.Outline&&a.OutlineWidth==b.OutlineWidth&&a.Bounds.Size==b.Bounds.Size;}
}
internal static class RiftCalendar {
    internal static readonly string[] Keys={"auto","eu","nae","naw","sa","asia"};
    internal static readonly string[] Labels={"Automatic detection","Europe","North America East","North America West","South America","Asia"};
    internal static string RegionForServer(int server){if(server<1000||server>2999||server%100<1||server%100>50)return null;switch(server/100%10){case 1:return "nae";case 2:return "naw";case 3:return "eu";case 4:return "sa";case 5:return "asia";default:return null;}}
    static readonly Dictionary<string,TimeZoneInfo> zones=LoadZones();
    static Dictionary<string,TimeZoneInfo> LoadZones(){var found=new Dictionary<string,TimeZoneInfo>();string[] ids={"W. Europe Standard Time","Eastern Standard Time","Pacific Standard Time","E. South America Standard Time","Korea Standard Time"};for(int i=0;i<ids.Length;i++)try{found[Keys[i+1]]=TimeZoneInfo.FindSystemTimeZoneById(ids[i]);}catch(TimeZoneNotFoundException){}catch(InvalidTimeZoneException){}return found;}
    internal static TimeZoneInfo Zone(string region){TimeZoneInfo zone;return region!=null&&zones.TryGetValue(region,out zone)?zone:null;}
    internal static string Label(string region,string language){for(int n=1;n<Keys.Length;n++)if(Keys[n]==region)return UiLanguage.Text(Labels[n],language);return UiLanguage.Text("Waiting for character server",language);}
    internal static List<DateTimeOffset> Openings(DateTimeOffset now,TimeZoneInfo zone){var times=new List<DateTimeOffset>();if(zone==null)return times;DateTime day=TimeZoneInfo.ConvertTime(now,zone).Date;
        for(int offset=-1;offset<=2;offset++)for(int slot=0;slot<8;slot++){DateTime wall=DateTime.SpecifyKind(day.AddDays(offset).AddHours(2+3*slot),DateTimeKind.Unspecified);if(zone.IsInvalidTime(wall))continue;TimeSpan[] shifts=zone.IsAmbiguousTime(wall)?zone.GetAmbiguousTimeOffsets(wall):new[]{zone.GetUtcOffset(wall)};foreach(var shift in shifts)times.Add(new DateTimeOffset(wall,shift).ToUniversalTime());}times.Sort();return times;}
    internal sealed class Window {internal DateTimeOffset Opening,Closing;}
    internal static Window Active(DateTimeOffset now,string region){foreach(var t in Openings(now,Zone(region)))if(t<=now&&now<t.AddMinutes(10))return new Window{Opening=t,Closing=t.AddMinutes(10)};return null;}
    internal static DateTimeOffset? Next(DateTimeOffset now,string region){foreach(var t in Openings(now,Zone(region)))if(t>now)return t;return null;}
    internal static string Remaining(DateTimeOffset close,DateTimeOffset now){int seconds=Math.Max(0,(int)Math.Ceiling((close-now).TotalSeconds));return (seconds/60).ToString("00",CultureInfo.InvariantCulture)+":"+(seconds%60).ToString("00",CultureInfo.InvariantCulture);}
}
// Validated own profile only. Never derives a region from an IP, NPC or party member.
internal sealed class RiftIdentity {
    readonly object gate=new object();int server;double timestamp;volatile bool enabled;
    internal int Server {get{lock(gate)return server;}}
    internal void Configure(bool value){lock(gate){if(enabled==value)return;enabled=value;server=0;timestamp=0;}}
    internal void Reset(){lock(gate){server=0;timestamp=0;}}
    internal void Consume(byte[] b,int start,int end,double time,string flow){if(!enabled||b==null||start<0||end>b.Length||end-start<128||end-start>65536||b[start]!=0x33||b[start+1]!=0x36||string.IsNullOrEmpty(flow)||double.IsNaN(time)||double.IsInfinity(time))return;lock(gate){if(!enabled||time<timestamp)return;
        EnemyTargetSignal.Player player;if(!EnemyTargetSignal.PlayerState(b,start,end,true,out player))return;int at=start+2;uint actor;if(!DpsSignal.Var(b,ref at,end,out actor))return;byte[] name=Encoding.UTF8.GetBytes(player.Name);
        for(int p=at;p<Math.Min(at+16,end-12);p++){if(b[p]!=name.Length||p+1+name.Length+7>end)continue;bool same=true;for(int n=0;n<name.Length;n++)if(b[p+1+n]!=name[n]){same=false;break;}if(!same)continue;int after=p+1+name.Length;uint role=BitConverter.ToUInt32(b,after+2);if(role<5||role>36||(b[after+6]!=1&&b[after+6]!=2))continue;server=BitConverter.ToUInt16(b,after);timestamp=time;return;}
    }}
}
internal sealed class RiftAlertLedger {
    readonly HashSet<string> delivered=new HashSet<string>();readonly Queue<string> order=new Queue<string>();
    readonly string path;internal string Error {get;private set;}
    internal RiftAlertLedger(string path=null){this.path=path;if(path==null)return;try{if(!File.Exists(path)||new FileInfo(path).Length>16384)return;var saved=new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(path));if(saved==null)return;foreach(string key in saved){if(key==null||key.Length>100||delivered.Contains(key))continue;delivered.Add(key);order.Enqueue(key);while(order.Count>64)delivered.Remove(order.Dequeue());}}catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is InvalidOperationException))throw;Error=ex.Message;}}
    void Save(){if(path==null)return;try{string temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(order.ToArray()));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);Error=null;}catch(Exception ex){if(!(ex is IOException||ex is UnauthorizedAccessException))throw;Error=ex.Message;}}
    internal sealed class Notice {internal bool Opening,Closing;internal int Minutes;internal DateTimeOffset At;}
    bool Claim(string region,DateTimeOffset at,string type){string key=region+"/"+at.UtcDateTime.Ticks+"/"+type;if(delivered.Contains(key))return false;delivered.Add(key);order.Enqueue(key);while(order.Count>64)delivered.Remove(order.Dequeue());Save();return true;}
    internal Notice Poll(DateTimeOffset now,string region,RiftOptions options,bool enabled){if(!enabled||RiftCalendar.Zone(region)==null)return null;
        foreach(var at in RiftCalendar.Openings(now,RiftCalendar.Zone(region))){double until=(at-now).TotalSeconds;double closedAge=(now-at.AddMinutes(10)).TotalSeconds;
            if(options.Closing&&closedAge>=0&&closedAge<60&&Claim(region,at,"close"))return new Notice{Closing=true,At=at};
            bool opening=until<=0&&until> -60;if(opening){if(options.Opening&&Claim(region,at,"open"))return new Notice{Opening=true,At=at};}
            else if(options.Warning&&until>0&&until<=options.LeadMinutes*60&&Claim(region,at,"warning"))return new Notice{Minutes=Math.Max(1,(int)Math.Ceiling(until/60)),At=at};
        }return null;}
}
