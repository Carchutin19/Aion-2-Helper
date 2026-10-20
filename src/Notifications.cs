using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class NotificationOptions {
    public bool Enabled=true,PartyInvites=true,DungeonInvites=true,ShugoFestival=true,ShugoOpening=true,Sound=true,Fade=true,Simplified=false,IconGlow=true;
    public int BackgroundOpacity=210,FontSize=18,Volume=80,GlowPercent=65;
    public double DurationSeconds=6,FadeSeconds=.25;
    public string Color="#EEEEEE";
    public Rectangle Bounds=new Rectangle(740,120,440,90);
    public RiftOptions Rift=new RiftOptions();
    internal NotificationOptions Copy(){var o=(NotificationOptions)MemberwiseClone();o.Rift=(Rift??new RiftOptions()).Copy();return o;}
    internal bool NeedsCapture {get{return Enabled&&(PartyInvites||DungeonInvites)||Rift.NeedsIdentity(Enabled);}}
    internal static bool SameAppearance(NotificationOptions a,NotificationOptions b){return a.Simplified==b.Simplified&&a.IconGlow==b.IconGlow&&a.GlowPercent==b.GlowPercent&&a.BackgroundOpacity==b.BackgroundOpacity&&a.FontSize==b.FontSize&&a.Color==b.Color&&a.Bounds.Size==b.Bounds.Size;}
    internal void Normalize(){if(Rift==null)Rift=new RiftOptions();Rift.Normalize();BackgroundOpacity=Math.Max(0,Math.Min(255,BackgroundOpacity));FontSize=Math.Max(10,Math.Min(48,FontSize));Volume=Math.Max(0,Math.Min(100,Volume));GlowPercent=Math.Max(0,Math.Min(100,GlowPercent));DurationSeconds=Finite(DurationSeconds,6,1,60);FadeSeconds=Finite(FadeSeconds,.25,0,2);if(!EnergyBarOptions.ValidColor(Color))Color="#EEEEEE";Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(260,Math.Min(1600,Bounds.Width)),Math.Max(60,Math.Min(500,Bounds.Height)));}
    static double Finite(double value,double fallback,double min,double max){return double.IsNaN(value)||double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));}
    internal static NotificationOptions Read(Dictionary<string,object> data){object value;var o=new NotificationOptions();if(data.TryGetValue("notifications",out value))try{o=new JavaScriptSerializer().ConvertToType<NotificationOptions>(value)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(NotificationOptions a,NotificationOptions b){return RiftOptions.Same(a.Rift,b.Rift)&&SameAppearance(a,b)&&a.Enabled==b.Enabled&&a.PartyInvites==b.PartyInvites&&a.DungeonInvites==b.DungeonInvites&&a.ShugoFestival==b.ShugoFestival&&a.ShugoOpening==b.ShugoOpening&&a.Sound==b.Sound&&a.Fade==b.Fade&&a.Volume==b.Volume&&a.DurationSeconds==b.DurationSeconds&&a.FadeSeconds==b.FadeSeconds&&a.Bounds==b.Bounds;}
}

// Hourly :00 schedule requested by the user. UTC keeps the reminder independent
// of DST and PC timezone changes; Global EU/NA use whole-hour UTC offsets.
// The one-minute window allows timer jitter, but never replays an old reminder.
internal sealed class ShugoSchedule {
    long lastHour,lastOpeningHour;
    internal bool Due(DateTime utc){if(utc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC required");if(utc.Minute!=55)return false;long hour=utc.Ticks/TimeSpan.TicksPerHour;if(hour<=lastHour)return false;lastHour=hour;return true;}
    internal bool DueOpening(DateTime utc){if(utc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC required");if(utc.Minute!=0)return false;long hour=utc.Ticks/TimeSpan.TicksPerHour;if(hour<=lastOpeningHour)return false;lastOpeningHour=hour;return true;}
    internal static int NextCheck(DateTime utc){return Math.Max(1000,60000-utc.Second*1000-utc.Millisecond);}
    internal static bool GameOpen(){bool found=false;foreach(var process in Process.GetProcessesByName("AION2"))using(process){try{if(!process.HasExited)found=true;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}return found;}
}

// Incoming 0992 normal and 0E97 dungeon-group invitation layouts observed on
// the Global client. Roster/join and recruitment listings never trigger alerts.
internal sealed class NotificationSignal {
    internal sealed class Notice {internal readonly string Name;internal readonly bool Dungeon;internal Notice(string name,bool dungeon){Name=name;Dungeon=dungeon;}}
    readonly object gate=new object();readonly Queue<Notice> pending=new Queue<Notice>();
    readonly Dictionary<string,double> requests=new Dictionary<string,double>();readonly List<string> expired=new List<string>();
    volatile bool enabled,partyInvites,dungeonInvites,hasPending;string lastName;double lastTime,lastAccepted,decodeDelayMs;long received;string lastInviter;
    static readonly UTF8Encoding strictUtf8=new UTF8Encoding(false,true);
    internal bool HasPending {get{return hasPending;}}
    internal Action OnPending;
    internal double LastAccepted {get{lock(gate)return lastAccepted;}}
    internal bool DetectionReady {get{return true;}}
    internal long Received {get{lock(gate)return received;}}
    internal object Status {get{lock(gate)return new{ready=DetectionReady,received=received,lastInviter=lastInviter,lastTimestamp=lastTime,acceptedUtc=lastAccepted,decodeDelayMs=decodeDelayMs,queued=pending.Count};}}
    internal void Configure(NotificationOptions o){lock(gate){
        bool changed=partyInvites!=o.PartyInvites||dungeonInvites!=o.DungeonInvites;
        enabled=o.Enabled;partyInvites=o.PartyInvites;dungeonInvites=o.DungeonInvites;
        if(!enabled){pending.Clear();hasPending=false;lastName=null;requests.Clear();}
        else if(changed){
            int count=pending.Count;for(int i=0;i<count;i++){var notice=pending.Dequeue();if(notice.Dungeon?dungeonInvites:partyInvites)pending.Enqueue(notice);}hasPending=pending.Count>0;
            expired.Clear();foreach(var entry in requests)if(entry.Key[0]=='D'?!dungeonInvites:!partyInvites)expired.Add(entry.Key);foreach(string old in expired)requests.Remove(old);
            if(!partyInvites)lastName=null;
        }
    }}
    internal static bool Invitation(byte[] data,int start,int end,out string name,out string request){
        name=request=null;if(data==null||start<0||end<start||end>data.Length||end-start<55)return false;
        bool dungeon=data[start]==0x0e&&data[start+1]==0x97;
        if(!dungeon&&(data[start]!=9||data[start+1]!=0x92))return false;
        // The observed dungeon-group invitation adds one byte before the sender.
        // Keep its exact layout separate from rosters and recruitment listings.
        int shift=dungeon?1:0;if(end-start<55+shift||end-start>126+shift||dungeon&&data[start+10]!=0)return false;
        int count=data[start+30+shift],tail=start+31+shift+count;if(count<1||count>72||tail+23!=end||data[start+16+shift]==0&&data[start+17+shift]==0||data[tail]!=data[start+16+shift]||data[tail+1]!=data[start+17+shift])return false;
        for(int p=tail+2;p<tail+6;p++)if(data[p]!=0)return false;if(data[tail+14]!=1)return false;
        ulong party=BitConverter.ToUInt64(data,start+2),stamp=BitConverter.ToUInt64(data,tail+15),sender=0;for(int p=0;p<6;p++)sender|=(ulong)data[start+10+shift+p]<<(8*p);if(party==0||sender==0||stamp==0)return false;
        try{name=strictUtf8.GetString(data,start+31+shift,count);}catch(DecoderFallbackException){return false;}if(name.Length>24||string.IsNullOrWhiteSpace(name)||name.Trim()!=name)return false;foreach(char c in name)if(char.IsControl(c))return false;
        request=(dungeon?"D":"P")+party.ToString("X16")+sender.ToString("X12")+stamp.ToString("X16");return true;
    }
    internal void Consume(byte[] data,int start,int end,double timestamp,string flow){if(!enabled||!partyInvites&&!dungeonInvites)return;if(data==null||start<0||end<start||end>data.Length||end-start<2||!((data[start]==9&&data[start+1]==0x92)||(data[start]==0x0e&&data[start+1]==0x97))||double.IsNaN(timestamp)||double.IsInfinity(timestamp)||timestamp<0)return;bool dungeon=data[start]==0x0e;if(dungeon?!dungeonInvites:!partyInvites)return;string name,key;if(!Invitation(data,start,end,out name,out key))return;lock(gate){if(!enabled||(dungeon?!dungeonInvites:!partyInvites))return;expired.Clear();foreach(var entry in requests)if(timestamp-entry.Value>120)expired.Add(entry.Key);foreach(string old in expired)requests.Remove(old);if(requests.ContainsKey(key))return;if(requests.Count>=64){string oldest=null;double earliest=double.MaxValue;foreach(var entry in requests)if(entry.Value<earliest){earliest=entry.Value;oldest=entry.Key;}requests.Remove(oldest);}requests[key]=timestamp;received++;lastInviter=name;lastTime=timestamp;lastAccepted=DpsHistory.UtcNow();decodeDelayMs=Math.Max(0,(lastAccepted-timestamp)*1000);if(pending.Count>=8)pending.Dequeue();pending.Enqueue(new Notice(name,dungeon));hasPending=true;}var wake=OnPending;if(wake!=null)wake();}
    internal void Reset(){lock(gate){pending.Clear();hasPending=false;lastName=null;lastTime=lastAccepted=decodeDelayMs=0;lastInviter=null;requests.Clear();}}
    internal bool EnqueueValidatedInvitation(string name,double timestamp){if(string.IsNullOrWhiteSpace(name)||name.Length>64||double.IsNaN(timestamp)||double.IsInfinity(timestamp)||timestamp<0)return false;foreach(char c in name)if(char.IsControl(c))return false;lock(gate){if(!enabled||!partyInvites||name==lastName&&timestamp-lastTime<2)return false;lastName=name;lastTime=timestamp;if(pending.Count>=8)pending.Dequeue();pending.Enqueue(new Notice(name,false));hasPending=true;return true;}}
    internal Notice TakeNotice(){if(!hasPending)return null;lock(gate){if(pending.Count==0)return null;var value=pending.Dequeue();hasPending=pending.Count>0;return value;}}
    internal string Take(){var notice=TakeNotice();return notice==null?null:notice.Name;}
}

internal sealed class NotificationMotion {
    internal double Opacity;internal int TickInterval=16;double started,initial;bool active;
    internal bool Active {get{return active;}}
    internal void Show(double now){initial=Opacity;started=now;active=true;}
    internal bool Step(double now,NotificationOptions o,bool editing){TickInterval=16;if(editing){Opacity=1;return false;}if(!active){Opacity=0;return false;}double fade=o.Fade?o.FadeSeconds:0,elapsed=Math.Max(0,now-started),rise=fade*(1-initial);if(elapsed<rise)Opacity=initial+elapsed/fade;else if(elapsed<rise+o.DurationSeconds){Opacity=1;TickInterval=Math.Max(16,(int)Math.Ceiling((rise+o.DurationSeconds-elapsed)*1000));}else if(fade>0&&elapsed<rise+o.DurationSeconds+fade)Opacity=1-(elapsed-rise-o.DurationSeconds)/fade;else {Opacity=0;active=false;}return active;}
    internal void Clear(){active=false;Opacity=0;}
}

internal sealed class NotificationAudio:IDisposable {
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern uint mciSendString(string command,StringBuilder result,int length,IntPtr callback);
    readonly string file,alias="aionnotice"+Guid.NewGuid().ToString("N");bool opened;
    internal string Error {get;private set;}
    internal NotificationAudio(string root){file=Path.Combine(root,"assets","sounds","party-invite.mp3");}
    internal void Play(int volume){Error=null;if(!File.Exists(file)){Error="Notification sound file is missing.";return;}if(!opened){if(mciSendString("open \""+file+"\" type mpegvideo alias "+alias,null,0,IntPtr.Zero)!=0){Error="Notification sound could not be played.";return;}opened=true;}mciSendString("stop "+alias,null,0,IntPtr.Zero);mciSendString("setaudio "+alias+" volume to "+(Math.Max(0,Math.Min(100,volume))*10),null,0,IntPtr.Zero);if(mciSendString("play "+alias+" from 0",null,0,IntPtr.Zero)!=0)Error="Notification sound could not be played.";}
    internal void Stop(){if(opened)mciSendString("stop "+alias,null,0,IntPtr.Zero);}
    internal void Close(){if(opened){mciSendString("close "+alias,null,0,IntPtr.Zero);opened=false;}}
    public void Dispose(){Close();}
}

// Embedded artwork is decoded, trimmed and reduced once per category. Glow is
// made from the alpha silhouette, never baked into the text/black gradient.
internal static class NotificationArtwork {
    sealed class Asset {internal Bitmap Icon,Glow;}
    static readonly Dictionary<string,Asset> cache=new Dictionary<string,Asset>();
    static Asset Get(string key){lock(cache){Asset asset;if(cache.TryGetValue(key,out asset))return asset;
        using(var stream=typeof(NotificationArtwork).Assembly.GetManifestResourceStream("Notification."+key+".png")){
            if(stream==null)throw new InvalidOperationException("Missing embedded notification icon: "+key);
            using(var original=new Bitmap(stream))using(var source=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb)){
                using(var g=Graphics.FromImage(source))g.DrawImageUnscaled(original,0,0);
                var data=source.LockBits(new Rectangle(0,0,source.Width,source.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var pixels=new byte[data.Stride*source.Height];int left=source.Width,top=source.Height,right=0,bottom=0;
                try{Marshal.Copy(data.Scan0,pixels,0,pixels.Length);for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if(pixels[y*data.Stride+x*4+3]>32){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}finally{source.UnlockBits(data);}
                if(left>right||top>bottom)throw new InvalidOperationException("Empty notification icon: "+key);
                var icon=new Bitmap(384,384,PixelFormat.Format32bppPArgb);float ratio=Math.Min(380f/(right-left+1),380f/(bottom-top+1));var dest=new RectangleF((384-(right-left+1)*ratio)/2,(384-(bottom-top+1)*ratio)/2,(right-left+1)*ratio,(bottom-top+1)*ratio);
                using(var g=Graphics.FromImage(icon)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(source,dest,new Rectangle(left,top,right-left+1,bottom-top+1),GraphicsUnit.Pixel);}
                asset=new Asset{Icon=icon,Glow=MakeGlow(icon,key=="dungeon")};cache.Add(key,asset);return asset;
            }
        }
    }}
    static Bitmap MakeGlow(Bitmap icon,bool stone){
        const int n=160;var glow=new Bitmap(n,n,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(glow))g.DrawImage(icon,new Rectangle(16,16,128,128));
        var area=new Rectangle(0,0,n,n);var data=glow.LockBits(area,ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);var pixels=new byte[n*n*4];var alpha=new float[n*n];var scratch=new float[n*n];
        try{Marshal.Copy(data.Scan0,pixels,0,pixels.Length);for(int i=0;i<alpha.Length;i++)alpha[i]=pixels[i*4+3];
            for(int pass=0;pass<3;pass++){for(int y=0;y<n;y++)for(int x=0;x<n;x++){float sum=0;for(int k=Math.Max(0,x-5);k<=Math.Min(n-1,x+5);k++)sum+=alpha[y*n+k];scratch[y*n+x]=sum/11;}for(int y=0;y<n;y++)for(int x=0;x<n;x++){float sum=0;for(int k=Math.Max(0,y-5);k<=Math.Min(n-1,y+5);k++)sum+=scratch[k*n+x];alpha[y*n+x]=sum/11;}}
            for(int i=0;i<alpha.Length;i++){int a=Math.Min(stone?125:190,(int)(alpha[i]*(stone?.8:1.2)));pixels[i*4]=(byte)((stone?170:220)*a/255);pixels[i*4+1]=(byte)((stone?220:235)*a/255);pixels[i*4+2]=(byte)((stone?235:100)*a/255);pixels[i*4+3]=(byte)a;}Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
        }finally{glow.UnlockBits(data);}return glow;
    }
    internal static Bitmap Icon(string key){return Get(key).Icon;}
    internal static void Draw(Graphics g,string key,Rectangle bounds,NotificationOptions o){var asset=Get(key);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        if(o.IconGlow&&o.GlowPercent>0){var dest=bounds;dest.Inflate(Math.Max(1,bounds.Width/8),Math.Max(1,bounds.Height/8));using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=o.GlowPercent/100f;attributes.SetColorMatrix(matrix);g.DrawImage(asset.Glow,dest,0,0,asset.Glow.Width,asset.Glow.Height,GraphicsUnit.Pixel,attributes);}}
        g.DrawImage(asset.Icon,bounds);
    }
}

internal static class NotificationDesign {
    internal sealed class Layout {internal Size Size;internal Rectangle Title,Body,Icon,Background;internal int BodySize,TitleSize,Padding;internal string TitleText,BodyText,Artwork;internal bool Rich;}
    static string Title(string name,string language,bool sample,bool shugo,bool dungeon,bool rift){return UiLanguage.Text(sample?"Notification":rift?"Spacetime Rift":shugo?"Shugo Festival":dungeon?"Dungeon party invitation":"Party invitation",language);}
    static string Body(string name,string language,bool sample,bool shugo,bool opening,bool dungeon,bool rift,int lead,bool closing){return sample?UiLanguage.Text("This is a sample notification.",language):rift?(closing?UiLanguage.Text("The portal has closed. Entry is no longer available.",language):opening?UiLanguage.Text("The portal is now open. You have 10 minutes to enter.",language):string.Format(UiLanguage.Text("Spacetime Rift opens in {0} minutes.",language),lead)):shugo?UiLanguage.Text(opening?"You can now sign up for Shugo Festival.":"Shugo Festival starts in 5 minutes.",language):string.Format(UiLanguage.Text(dungeon?"{0} is inviting you to a dungeon party.":"{0} is inviting you to a party.",language),name);}
    static string Wrap(string text,Font font,int width){
        var output=new StringBuilder();var line=new StringBuilder();var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine;
        foreach(string word in text.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){
            string candidate=line.Length==0?word:line+" "+word;
            if(line.Length>0&&TextRenderer.MeasureText(candidate,font,Size.Empty,flags).Width>width){output.AppendLine(line.ToString());line.Clear();}
            if(TextRenderer.MeasureText(word,font,Size.Empty,flags).Width<=width){if(line.Length>0)line.Append(' ');line.Append(word);continue;}
            var elements=System.Globalization.StringInfo.GetTextElementEnumerator(word);
            while(elements.MoveNext()){string element=elements.GetTextElement();if(line.Length>0&&TextRenderer.MeasureText(line.ToString()+element,font,Size.Empty,flags).Width>width){output.AppendLine(line.ToString());line.Clear();}line.Append(element);}
        }return output.Append(line).ToString();
    }
    internal static Layout Measure(int maximumWidth,int maximumHeight,string name,string language,NotificationOptions o,bool editing,FpsDesign.Workspace workspace,bool shugo=false,bool opening=false,bool dungeon=false,bool rift=false,int lead=5,bool riftClosing=false){
        bool sample=editing||name==null&&!shugo&&!rift;string title=Title(name,language,sample,shugo,dungeon,rift),text=Body(name,language,sample,shugo,opening,dungeon,rift,lead,riftClosing);
        if(!o.Simplified)return MeasureRich(maximumWidth,maximumHeight,title,text,o,workspace,rift?"rift":shugo?"shugo":dungeon?"dungeon":"party");
        int limit=Math.Max(80,Math.Min(maximumWidth,o.Bounds.Width));double scale=o.Bounds.Height/90.0;int font=Math.Max(10,Math.Min(144,(int)Math.Round(o.FontSize*scale)));
        var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak;Layout result=null;
        for(int attempt=0;attempt<20;attempt++){
            int pad=Math.Max(8,(int)Math.Round(18.0*font/o.FontSize)),top=Math.Max(6,(int)Math.Round(10.0*font/o.FontSize)),gap=Math.Max(4,(int)Math.Round(5.0*font/o.FontSize));pad=Math.Min(pad,(limit-20)/2);int titleSize=Math.Max(10,(int)Math.Round(font*.75));int room=Math.Max(20,limit-pad*2);
            var titleFont=workspace.Font(titleSize,true);var bodyFont=workspace.Font(font,false);
            int natural=Math.Max(TextRenderer.MeasureText(title,titleFont,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine).Width,TextRenderer.MeasureText(text,bodyFont,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine).Width);
            int inner=Math.Min(room,natural+2);string wrappedTitle=Wrap(title,titleFont,inner),wrappedBody=Wrap(text,bodyFont,inner);var titleMeasure=TextRenderer.MeasureText(wrappedTitle,titleFont,new Size(inner,int.MaxValue),flags);var bodyMeasure=TextRenderer.MeasureText(wrappedBody,bodyFont,new Size(inner,int.MaxValue),flags);
            int titleHeight=titleMeasure.Height+2,bodyHeight=bodyMeasure.Height+2;result=new Layout{Size=new Size(inner+pad*2,titleHeight+bodyHeight+top*2+gap),Title=new Rectangle(pad,top,inner,titleHeight),Body=new Rectangle(pad,top+titleHeight+gap,inner,bodyHeight),BodySize=font,TitleSize=titleSize,Padding=pad,TitleText=wrappedTitle,BodyText=wrappedBody};
            if(result.Size.Height<=maximumHeight||font==10)break;font=Math.Max(10,(int)Math.Floor(font*.9));
        }return result;
    }
    static Layout MeasureRich(int maximumWidth,int maximumHeight,string title,string text,NotificationOptions o,FpsDesign.Workspace workspace,string artwork){
        int limit=Math.Max(80,Math.Min(maximumWidth,o.Bounds.Width));double scale=o.Bounds.Height/90.0;int font=Math.Max(10,Math.Min(144,(int)Math.Round(o.FontSize*scale)));
        int pad=Math.Max(4,Math.Min(limit/12,(int)Math.Round(10*scale))),gap=Math.Max(4,(int)Math.Round(12*scale));int icon=Math.Max(24,Math.Min(limit/3,(int)Math.Round(64*scale)));gap=Math.Min(gap,limit/10);int room=Math.Max(20,limit-2*pad-icon-gap);Layout result=null;
        for(int attempt=0;attempt<30;attempt++){
            int titleSize=Math.Max(10,(int)Math.Round(font*.75));var titleFont=workspace.Font(titleSize,true);var bodyFont=workspace.Font(font,false);var single=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine;var wrap=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak;
            int natural=Math.Max(TextRenderer.MeasureText(title,titleFont,Size.Empty,single).Width,TextRenderer.MeasureText(text,bodyFont,Size.Empty,single).Width);int inner=Math.Min(room,natural+4);string wrappedTitle=Wrap(title,titleFont,inner),wrappedBody=Wrap(text,bodyFont,inner);
            int titleHeight=TextRenderer.MeasureText(wrappedTitle,titleFont,new Size(inner,int.MaxValue),wrap).Height+2,bodyHeight=TextRenderer.MeasureText(wrappedBody,bodyFont,new Size(inner,int.MaxValue),wrap).Height+2,lineGap=Math.Max(3,(int)Math.Round(4*scale));int textHeight=titleHeight+lineGap+bodyHeight;int height=Math.Max(icon,textHeight)+2*pad,x=pad+icon+gap,y=(height-textHeight)/2;
            result=new Layout{Rich=true,Artwork=artwork,Size=new Size(x+inner+pad,height),Title=new Rectangle(x,y,inner,titleHeight),Body=new Rectangle(x,y+titleHeight+lineGap,inner,bodyHeight),Icon=new Rectangle(pad,(height-icon)/2,icon,icon),Background=new Rectangle(pad+icon/2,Math.Max(1,y-pad/2),icon/2+gap+inner+pad,Math.Min(height-2,textHeight+pad)),BodySize=font,TitleSize=titleSize,Padding=pad,TitleText=wrappedTitle,BodyText=wrappedBody};
            if(height<=maximumHeight||font==10)break;font=Math.Max(10,(int)Math.Floor(font*.85));
        }return result;
    }
    internal static Bitmap RenderFitted(int maximumWidth,int maximumHeight,string name,string language,NotificationOptions o,bool editing,FpsDesign.Workspace workspace,bool shugo=false,bool opening=false,bool dungeon=false,bool rift=false,int lead=5,bool riftClosing=false){var layout=Measure(maximumWidth,maximumHeight,name,language,o,editing,workspace,shugo,opening,dungeon,rift,lead,riftClosing);return RenderLayout(layout.Size.Width,layout.Size.Height,layout,o,editing,workspace);}
    internal static Bitmap Render(int width,int height,string name,string language,NotificationOptions o,bool editing,FpsDesign.Workspace workspace){
        return Render(width,height,name,language,o,editing,workspace,false);
    }
    internal static Bitmap Render(int width,int height,string name,string language,NotificationOptions o,bool editing,FpsDesign.Workspace workspace,bool shugo,bool opening=false,bool dungeon=false,bool rift=false,int lead=5,bool riftClosing=false){
        var layout=Measure(width,height,name,language,o,editing,workspace,shugo,opening,dungeon,rift,lead,riftClosing);return RenderLayout(width,height,layout,o,editing,workspace);
    }
    static Bitmap RenderLayout(int width,int height,Layout layout,NotificationOptions o,bool editing,FpsDesign.Workspace workspace){
        workspace.Prepare(width,height);var image=new Bitmap(width,height,PixelFormat.Format32bppPArgb);var mask=workspace.Mask;var mg=workspace.MaskGraphics;mg.Clear(Color.Black);
        using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;
            if(editing)using(var target=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(target,0,0,width,height);
            int offsetX=width/2-layout.Size.Width/2,offsetY=height/2-layout.Size.Height/2;var title=layout.Title;title.Offset(offsetX,offsetY);var body=layout.Body;body.Offset(offsetX,offsetY);
            float radius=Math.Min(12,layout.Size.Height/4f);
            if(layout.Rich){var band=layout.Background;band.Offset(offsetX,offsetY);using(var background=new LinearGradientBrush(band,Color.Transparent,Color.Transparent,LinearGradientMode.Horizontal)){background.InterpolationColors=new ColorBlend{Colors=new[]{Color.FromArgb(0,0,0,0),Color.FromArgb(o.BackgroundOpacity,0,0,0),Color.FromArgb(o.BackgroundOpacity,0,0,0),Color.FromArgb(0,0,0,0)},Positions=new[]{0f,.12f,.45f,1f}};using(var shape=FpsDesign.Round(band.X,band.Y,band.Width,band.Height,Math.Min(12,band.Height/4f)))g.FillPath(background,shape);}var icon=layout.Icon;icon.Offset(offsetX,offsetY);NotificationArtwork.Draw(g,layout.Artwork,icon,o);}
            else {using(var shape=FpsDesign.Round(offsetX+2,offsetY+2,layout.Size.Width-4,layout.Size.Height-4,radius))using(var background=new SolidBrush(Color.FromArgb(o.BackgroundOpacity,0,0,0)))g.FillPath(background,shape);
                using(var shape=FpsDesign.Round(offsetX+2.5f,offsetY+2.5f,layout.Size.Width-5,layout.Size.Height-5,radius))using(var pen=new Pen(Color.FromArgb(35,255,255,255)))g.DrawPath(pen,shape);}
            var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak;
            TextRenderer.DrawText(mg,layout.TitleText,workspace.Font(layout.TitleSize,true),title,Color.FromArgb(175,175,175),flags);
            TextRenderer.DrawText(mg,layout.BodyText,workspace.Font(layout.BodySize,false),body,Color.White,flags);
            var data=mask.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);try{var pixels=workspace.Pixels;int length=data.Stride*height;var tint=ColorTranslator.FromHtml(o.Color);Marshal.Copy(data.Scan0,pixels,0,length);for(int p=0;p<length;p+=4){byte alpha=Math.Max(pixels[p],Math.Max(pixels[p+1],pixels[p+2]));pixels[p]=tint.B;pixels[p+1]=tint.G;pixels[p+2]=tint.R;pixels[p+3]=alpha;}Marshal.Copy(pixels,0,data.Scan0,length);}finally{mask.UnlockBits(data);}g.DrawImageUnscaled(mask,0,0);
            if(editing)using(var pen=new Pen(Color.FromArgb(140,145,162,163))){pen.DashStyle=DashStyle.Dot;using(var shape=FpsDesign.Round(.5f,.5f,width-1,height-1,12))g.DrawPath(pen,shape);}
        }return image;
    }
    internal static Rectangle Geometry(Rectangle start,int x,int y,int width,int height){if(height!=start.Height)width=(int)Math.Round(width*(double)height/start.Height);width=Math.Max(260,Math.Min(1600,width));height=Math.Max(60,Math.Min(500,height));return new Rectangle(x+start.Width/2-width/2,y+start.Height/2-height/2,width,height);}
    internal static Rectangle Resize(Rectangle start,Point delta,int hit){bool left=hit==10||hit==13||hit==16,right=hit==11||hit==14||hit==17,top=hit==12||hit==13||hit==14,bottom=hit==15||hit==16||hit==17;int width=Math.Max(260,Math.Min(1600,start.Width+2*(left?-delta.X:right?delta.X:0))),height=Math.Max(60,Math.Min(500,start.Height+2*(top?-delta.Y:bottom?delta.Y:0)));return new Rectangle(start.X+start.Width/2-width/2,start.Y+start.Height/2-height/2,width,height);}
}

internal sealed class NotificationOverlay:Form {
    readonly EnergyOverlay owner;readonly NotificationSignal signal;readonly NotificationAudio audio;readonly LayeredImage image=new LayeredImage();readonly FpsDesign.Workspace workspace=new FpsDesign.Workspace();readonly NotificationMotion motion=new NotificationMotion();readonly Stopwatch clock=Stopwatch.StartNew();readonly Timer animation=new Timer();
    NotificationOptions options=new NotificationOptions();string name,language;bool editing,valid,disposed,dragging,isShugo,isShugoOpening,isDungeon,isRift,isRiftOpening,isRiftClosing;int riftMinutes=5;readonly Queue<RiftAlertLedger.Notice> pendingRifts=new Queue<RiftAlertLedger.Notice>();sealed class PendingNotice {internal string Player;internal bool Sound,Shugo,Opening,Dungeon;internal double At;}readonly Queue<PendingNotice> pendingNotices=new Queue<PendingNotice>();Rectangle dragStart;Point mouseStart;int hit,wakeQueued;byte shownAlpha;Point shownLocation;double nextMaintenance,lastRaise,dispatchedUtc,firstVisibleUtc,dispatchDelayMs;readonly bool preview;
    readonly ShugoSchedule shugoSchedule=new ShugoSchedule();readonly Timer hourly=new Timer();long shugoReminders;DateTime lastShugoUtc;
    internal NotificationOverlay(EnergyOverlay parent,string root,NotificationSignal source,bool previewMode){owner=parent;signal=source;preview=previewMode;audio=new NotificationAudio(root);signal.OnPending=Wake;Text="Aion 2 Helper - Notifications";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;animation.Interval=16;animation.Tick+=delegate{Pulse();};hourly.Tick+=delegate{CheckShugo();};}
    void CheckShugo(){hourly.Stop();if(disposed||preview||!options.Enabled||!options.ShugoFestival&&!options.ShugoOpening)return;DateTime utc=DateTime.UtcNow;bool advance=options.ShugoFestival&&shugoSchedule.Due(utc),opening=options.ShugoOpening&&shugoSchedule.DueOpening(utc);if((advance||opening)&&!editing&&ShugoSchedule.GameOpen()){shugoReminders++;lastShugoUtc=utc;ShowNotice(null,true,true,true,opening);}hourly.Interval=ShugoSchedule.NextCheck(DateTime.UtcNow);hourly.Start();}
    void Wake(){if(!IsHandleCreated||IsDisposed||System.Threading.Interlocked.CompareExchange(ref wakeQueued,1,0)!=0)return;try{BeginInvoke((Action)delegate{System.Threading.Interlocked.Exchange(ref wakeQueued,0);if(!disposed)Pulse();});}catch(InvalidOperationException){System.Threading.Interlocked.Exchange(ref wakeQueued,0);}}
    internal string AudioError {get{return audio.Error;}}
    internal object WindowStatus {get{return new{x=Left,y=Top,width=Width,height=Height,autoSize=true,simplified=options.Simplified,iconGlow=options.IconGlow,glowPercent=options.GlowPercent,maximumWidth=options.Bounds.Width,scalePercent=options.Bounds.Height/90.0*100,enabled=options.Enabled,partyInvites=options.PartyInvites,dungeonInvites=options.DungeonInvites,activeDungeonInvitation=isDungeon,activeRiftNotice=isRift,activeRiftClosing=isRift&&isRiftClosing,shugoFestival=options.ShugoFestival,shugoOpening=options.ShugoOpening,shugoReminders=shugoReminders,lastShugoUtc=lastShugoUtc==DateTime.MinValue?null:lastShugoUtc.ToString("o"),sound=options.Sound,visible=Visible,opacity=motion.Opacity,editing=editing,animationTimer=animation.Enabled,audioError=audio.Error,dispatchedUtc=dispatchedUtc,firstVisibleUtc=firstVisibleUtc,dispatchDelayMs=dispatchDelayMs,firstVisibleDelayMs=firstVisibleUtc==0?0:Math.Max(0,(firstVisibleUtc-dispatchedUtc)*1000)};}}
    internal void Configure(NotificationOptions value,bool locked){
        bool stopInvite=(options.PartyInvites&&!value.PartyInvites&&!isShugo&&!isDungeon&&!isRift)||(options.DungeonInvites&&!value.DungeonInvites&&!isShugo&&isDungeon)||(options.ShugoFestival&&!value.ShugoFestival&&isShugo&&!isShugoOpening)||(options.ShugoOpening&&!value.ShugoOpening&&isShugoOpening);
        if(options.Rift.Region!=value.Rift.Region){pendingRifts.Clear();stopInvite|=isRift;}
        stopInvite|=isRift&&(isRiftClosing?!value.Rift.Closing:isRiftOpening?!value.Rift.Opening:!value.Rift.Warning);
        bool changed=!NotificationOptions.SameAppearance(options,value)||language!=owner.Language||editing==locked;
        bool timing=options.Fade!=value.Fade||options.FadeSeconds!=value.FadeSeconds||options.DurationSeconds!=value.DurationSeconds;
        options=value.Copy();options.Normalize();signal.Configure(options);language=owner.Language;editing=!locked;
        Rectangle target=editing?DpsDesign.UsableBounds(options.Bounds):FittedBounds(Size);if(target!=Bounds)Bounds=target;
        if(changed)valid=false;
        if(stopInvite){motion.Clear();animation.Stop();audio.Close();if(Visible)Hide();}
        if(!options.Sound)audio.Close();
        if(!options.Enabled){pendingRifts.Clear();pendingNotices.Clear();hourly.Stop();motion.Clear();animation.Stop();audio.Close();if(Visible)Hide();return;}
        if(!options.ShugoFestival&&!options.ShugoOpening)hourly.Stop();else if(!preview)CheckShugo();
        if(timing)animation.Stop();
        int style=OverlayNative.GetWindowLong(Handle,-20),next=editing?style&~0x20:style|0x20;if(next!=style)OverlayNative.SetWindowLong(Handle,-20,next);
        nextMaintenance=0;Pulse();
    }
    internal void Test(){if(!options.Enabled)return;ShowNotice(null,true);}
    internal void TestShugo(){if(!options.Enabled||!options.ShugoFestival)return;ShowNotice(null,true,true,true);}
    internal void TestShugoOpening(){if(!options.Enabled||!options.ShugoOpening)return;ShowNotice(null,true,true,true,true);}
    internal void VerifyRiftNative(){var o=new NotificationOptions{Sound=false,Fade=false};Configure(o,true);ShowNotice("Example",false);ShowRift(true,5);if(isRift||pendingRifts.Count!=1)throw new Exception("Rift alerts queue behind an active invitation");motion.Clear();nextMaintenance=0;Maintain(clock.Elapsed.TotalSeconds+1);if(!isRift||!isRiftOpening||pendingRifts.Count!=0)throw new Exception("Queued Rift opening follows the previous notice");o.Rift.Opening=false;Configure(o,true);if(motion.Opacity!=0)throw new Exception("Disabling Rift opening dismisses its active alert");ShowNotice("Example",false);o.Rift.Warning=false;Configure(o,true);if(motion.Opacity!=1||isRift)throw new Exception("Rift switches preserve normal party alerts");motion.Clear();o.Rift.Opening=true;Configure(o,true);ShowRift(true,5);ShowNotice(null,false,true,true,true);if(!isRift||pendingNotices.Count!=1)throw new Exception("Shugo must queue when Rift wins the same-hour race");motion.Clear();nextMaintenance=0;Maintain(clock.Elapsed.TotalSeconds+2);if(isRift||!isShugo||pendingNotices.Count!=0)throw new Exception("Queued Shugo opening follows Rift without being lost");motion.Clear();ShowRift(false,0,null,true);if(!isRiftClosing)throw new Exception("Closing uses the shared normal notification renderer");o.Rift.Closing=false;Configure(o,true);if(motion.Opacity!=0)throw new Exception("Closing switch dismisses only the closing notice");}
    internal void VerifyAutoSize(){
        var o=new NotificationOptions{Sound=false,Fade=false,FontSize=10};Configure(o,true);Test();Size small=Size;CheckAutoSize(small.Width<260&&small.Height<90,"Short locked notice must shrink below the old fixed minimum");
        ShowNotice(new string('W',48),false,true,false,false,true);CheckAutoSize(Width>small.Width||Height>small.Height,"New longer message must expand native bounds");CheckAutoSize(options.Bounds==o.Bounds,"Automatic fitting must not overwrite the saved reference geometry");
        Configure(o,false);CheckAutoSize(Size==DpsDesign.UsableBounds(o.Bounds).Size,"Unlocked sample keeps reference area resizable");o.PartyInvites=o.DungeonInvites=o.ShugoFestival=o.ShugoOpening=false;Configure(o,true);Test();CheckAutoSize(motion.Opacity==1&&!isShugo&&!isDungeon&&name==null,"Generic test remains independent of disabled categories");
    }
    static void CheckAutoSize(bool ok,string text){if(!ok)throw new Exception(text);}
    internal void VerifyDungeonNative(){
        var o=new NotificationOptions{Sound=false,Fade=false};Configure(o,true);ShowNotice("Example",false,true,false,false,true);
        int uploads=image.Uploads;o.PartyInvites=false;Configure(o,true);if(!isDungeon||motion.Opacity!=1||image.Uploads!=uploads)throw new Exception("Normal invitation switch must preserve active dungeon notice and cached glyphs");
        o.DungeonInvites=false;Configure(o,true);if(motion.Opacity!=0||Visible||animation.Enabled)throw new Exception("Dungeon switch must dismiss active dungeon notice");
        o.PartyInvites=true;o.DungeonInvites=true;Configure(o,true);ShowNotice("Example",false);o.DungeonInvites=false;Configure(o,true);if(isDungeon||motion.Opacity!=1)throw new Exception("Dungeon switch must preserve active normal invitation");
        o.PartyInvites=false;Configure(o,true);if(motion.Opacity!=0)throw new Exception("Normal switch must dismiss active normal invitation");
    }
    internal void VerifyShugoNative(){var o=new NotificationOptions{Sound=false,Fade=false,ShugoFestival=true};Configure(o,true);TestShugo();if(!isShugo||motion.Opacity!=1)throw new Exception("Shugo uses the notification renderer");o.PartyInvites=false;Configure(o,true);if(motion.Opacity!=1)throw new Exception("Disabling party alerts must preserve an active Shugo reminder");o.ShugoFestival=false;Configure(o,true);TestShugo();if(motion.Opacity!=0||animation.Enabled)throw new Exception("Disabling Shugo must clear its notice and block its test");o.ShugoOpening=true;Configure(o,true);TestShugoOpening();if(!isShugoOpening||motion.Opacity!=1)throw new Exception("Opening must work with advance notice disabled");o.ShugoFestival=true;Configure(o,true);o.ShugoFestival=false;Configure(o,true);if(motion.Opacity!=1)throw new Exception("Disabling advance must preserve opening notice");o.ShugoOpening=false;Configure(o,true);TestShugoOpening();if(motion.Opacity!=0)throw new Exception("Opening disabled blocks and clears only its own notice");o.Enabled=false;Configure(o,true);if(hourly.Enabled)throw new Exception("Disabled notifications stop the hourly timer");}
    internal void VerifyNative(){if(Owner!=null)throw new Exception("Notifications must be independent of energy visibility");var o=new NotificationOptions{Enabled=true,Sound=false,Fade=false};Configure(o,true);Test();int uploads=image.Uploads;for(int i=0;i<500;i++)Pulse();if(image.Uploads!=uploads)throw new Exception("Notification hold/fade must reuse the uploaded bitmap");VerifyRenderCache(o);int style=OverlayNative.GetWindowLong(Handle,-20);if((style&0x20)==0||(style&0x08000000)==0||ShowInTaskbar||OverlayNative.SendMessage(Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()!=3)throw new Exception("Locked notification must pass clicks through, avoid activation and taskbar entry");motion.Clear();Pulse();VerifyWake();o.PartyInvites=false;Configure(o,true);if(Visible||animation.Enabled||motion.Opacity!=0)throw new Exception("Disabling invitations must dismiss the active invitation and stop animation");o.PartyInvites=true;Configure(o,true);motion.Clear();Pulse();if(animation.Enabled)throw new Exception("Idle notification must stop its animation timer");VerifyFadeTimer(o);Configure(o,false);if((OverlayNative.GetWindowLong(Handle,-20)&0x20)!=0||animation.Enabled)throw new Exception("Editable sample needs input but no animation or sound");o.Enabled=false;Configure(o,true);if(animation.Enabled||Visible)throw new Exception("Disabled notification must stop and hide");}
    void VerifyRenderCache(NotificationOptions original){
        int uploads=image.Uploads,presents=image.Presentations;for(int i=0;i<500;i++)Maintain(i*.016);if(image.Uploads!=uploads||image.Presentations!=presents)throw new Exception("Unchanged hold must reuse its native image and presentation");
        var changed=original.Copy();changed.Volume=30;changed.Sound=true;changed.DurationSeconds=30;changed.Bounds.X+=10;Configure(changed,true);if(image.Uploads!=uploads)throw new Exception("Audio, timing and moving must preserve notification glyphs");
        changed.Color="#123456";Configure(changed,true);if(image.Uploads!=uploads+1)throw new Exception("Text color change must redraw once");Configure(original,true);
        using(var process=Process.GetCurrentProcess()){uint before=OverlayNative.GetGuiResources(process.Handle,0);var resize=original.Copy();for(int i=0;i<120;i++){resize.Bounds.Width=440+i%3;Configure(resize,true);}Configure(original,true);uint after=OverlayNative.GetGuiResources(process.Handle,0);if(after>before+3)throw new Exception("Notification fonts, bitmaps and DCs must stay bounded after resizing");}
    }
    void VerifyFadeTimer(NotificationOptions original){
        var timed=original.Copy();timed.Fade=true;timed.FadeSeconds=.06;timed.DurationSeconds=1;timed.Sound=false;Configure(timed,true);Test();double deadline=clock.Elapsed.TotalSeconds+1.8;bool partial=false;
        while(clock.Elapsed.TotalSeconds<deadline&&animation.Enabled){Application.DoEvents();if(motion.Opacity>0&&motion.Opacity<1)partial=true;System.Threading.Thread.Sleep(5);}
        if(!partial||animation.Enabled||motion.Opacity!=0)throw new Exception("Fade and hold must complete without the shared polling timer");Configure(original,true);
    }
    void VerifyWake(){
        byte[] invitation=new byte[59];invitation[0]=9;invitation[1]=0x92;invitation[2]=1;invitation[10]=2;invitation[16]=0x15;invitation[17]=5;invitation[30]=5;Array.Copy(Encoding.UTF8.GetBytes("Edeln"),0,invitation,31,5);invitation[36]=0x15;invitation[37]=5;invitation[50]=1;invitation[51]=1;
        var worker=new System.Threading.Thread(delegate(){signal.Consume(invitation,0,invitation.Length,DpsHistory.UtcNow(),"wake-test");});worker.Start();worker.Join();double before=dispatchedUtc;Application.DoEvents();if(name!="Edeln"||dispatchedUtc<=before||dispatchDelayMs>1000)throw new Exception("Incoming capture event must wake the UI without a polling tick");
    }
    void ShowNotice(string player,bool sound,bool pulse=true,bool shugo=false,bool opening=false,bool dungeon=false){if(!editing&&motion.Active&&isRift&&(player!=null||shugo||dungeon)){if(pendingNotices.Count<4)pendingNotices.Enqueue(new PendingNotice{Player=player,Sound=sound,Shugo=shugo,Opening=opening,Dungeon=dungeon,At=DpsHistory.UtcNow()});return;}name=player;isRift=isRiftOpening=isRiftClosing=false;isDungeon=dungeon;isShugo=shugo;isShugoOpening=opening;valid=false;dispatchedUtc=DpsHistory.UtcNow();firstVisibleUtc=0;dispatchDelayMs=shugo||signal.LastAccepted==0?0:Math.Max(0,(dispatchedUtc-signal.LastAccepted)*1000);motion.Show(clock.Elapsed.TotalSeconds);animation.Stop();if(sound&&options.Sound&&!preview)audio.Play(options.Volume);if(pulse)Pulse();}
    internal void CancelRiftNotices(){pendingRifts.Clear();if(isRift){motion.Clear();animation.Stop();audio.Close();if(Visible)Hide();}}
    internal void ShowRift(bool opening,int minutes,DateTimeOffset? scheduledOpening=null,bool closing=false){if(!options.Enabled||(closing?!options.Rift.Closing:opening?!options.Rift.Opening:!options.Rift.Warning))return;if(motion.Active&&!editing){if(pendingRifts.Count<4)pendingRifts.Enqueue(new RiftAlertLedger.Notice{Opening=opening,Closing=closing,Minutes=minutes,At=scheduledOpening??DateTimeOffset.MinValue});return;}ShowNotice(null,true,false);isRift=true;isRiftOpening=opening;isRiftClosing=closing;riftMinutes=minutes;dispatchDelayMs=0;Pulse();}
    // The shared energy timer can tick at 60 Hz. Idle notifications need no
    // decoding lock, clock or shell lookup; fades have their own short timer.
    internal void Maintain(double now){
        if(disposed||!options.Enabled||!editing&&!motion.Active&&!signal.HasPending&&pendingRifts.Count==0&&pendingNotices.Count==0)return;
        if(animation.Enabled&&animation.Interval==16&&!signal.HasPending)return;
        if(now<nextMaintenance)return;nextMaintenance=now+.25;if(!editing&&!motion.Active&&pendingRifts.Count>0){var notice=pendingRifts.Dequeue();DateTimeOffset utc=DateTimeOffset.UtcNow;if(notice.At!=DateTimeOffset.MinValue){if(notice.Closing?utc>notice.At.AddMinutes(11):notice.Opening?utc>notice.At.AddMinutes(1):utc>=notice.At)return;if(!notice.Opening&&!notice.Closing)notice.Minutes=Math.Max(1,(int)Math.Ceiling((notice.At-utc).TotalMinutes));}ShowRift(notice.Opening,notice.Minutes,notice.At,notice.Closing);return;}if(!editing&&!motion.Active&&pendingNotices.Count>0){var item=pendingNotices.Dequeue();bool allowed=item.Shugo?(item.Opening?options.ShugoOpening:options.ShugoFestival):item.Dungeon?options.DungeonInvites:options.PartyInvites;if(allowed&&DpsHistory.UtcNow()-item.At<60)ShowNotice(item.Player,item.Sound,true,item.Shugo,item.Opening,item.Dungeon);return;}Pulse();
    }
    void Schedule(bool active){
        if(!active){if(animation.Enabled)animation.Stop();return;}
        int interval=motion.TickInterval;
        if(!animation.Enabled){animation.Interval=interval;animation.Start();}
        else if(interval==16||animation.Interval==16){if(animation.Interval!=interval)animation.Interval=interval;}
        // Keep the one-shot hold deadline. Resetting it on every maintenance
        // tick kills/recreates a native timer without changing its due time.
    }
    internal void Pulse(){
        if(disposed||!options.Enabled)return;
        if(!editing&&!motion.Active&&!signal.HasPending){if(Visible)Hide();return;}
        var invite=signal.TakeNotice();if(invite!=null){ShowNotice(invite.Name,true,false,false,false,invite.Dungeon);if(signal.HasPending)Wake();}
        double now=clock.Elapsed.TotalSeconds;bool active=motion.Step(now,options,editing);Schedule(active);
        bool shell=!preview&&DpsDesign.ShellUiActive();byte alpha=shell?(byte)0:(byte)Math.Round(motion.Opacity*255);
        if(alpha==0){if(Visible)Hide();return;}
        if(!valid){var reference=DpsDesign.UsableBounds(options.Bounds);var area=Screen.FromRectangle(reference).WorkingArea;var layout=NotificationDesign.Measure(reference.Width,editing?reference.Height:area.Height,name,language,options,editing,workspace,isShugo,isRift?isRiftOpening:isShugoOpening,isDungeon,isRift,riftMinutes,isRiftClosing);Bounds=editing?reference:FittedBounds(layout.Size);using(var bitmap=NotificationDesign.Render(Width,Height,name,language,options,editing,workspace,isShugo,isRift?isRiftOpening:isShugoOpening,isDungeon,isRift,riftMinutes,isRiftClosing))image.Upload(bitmap);valid=true;shownAlpha=0;}
        if(alpha!=shownAlpha||shownLocation!=Location||!preview&&!Visible){image.Present(Handle,Left,Top,alpha);shownAlpha=alpha;shownLocation=Location;}
        if(!preview&&!Visible)Show();if(firstVisibleUtc==0&&dispatchedUtc>0)firstVisibleUtc=DpsHistory.UtcNow();
        if(!preview&&now-lastRaise>=1){OverlayNative.RaiseWithoutFocus(Handle);lastRaise=now;}
    }
    Rectangle FittedBounds(Size size){var reference=options.Bounds;return DpsDesign.UsableBounds(new Rectangle(reference.X+reference.Width/2-size.Width/2,reference.Y+reference.Height/2-size.Height/2,size.Width,size.Height));}
    void SaveGeometry(){var c=owner.Configuration;c.Notifications.Bounds=Bounds;owner.ApplyConfiguration(c);}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenTrayMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}Point p=PointToClient(new Point((short)(m.LParam.ToInt64()&0xffff),(short)((m.LParam.ToInt64()>>16)&0xffff)));m.Result=new IntPtr(BarDesign.HitTest(p.X,p.Y,Width,Height,6));return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){dragging=true;dragStart=Bounds;mouseStart=Cursor.Position;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){Point delta=new Point(Cursor.Position.X-mouseStart.X,Cursor.Position.Y-mouseStart.Y);Rectangle previous=Bounds;Bounds=DpsDesign.UsableBounds(hit==2?new Rectangle(dragStart.X+delta.X,dragStart.Y+delta.Y,dragStart.Width,dragStart.Height):NotificationDesign.Resize(dragStart,delta,hit));if(Size!=previous.Size)valid=false;Pulse();m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;SaveGeometry();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x46){var p=(OverlayNative.WindowPos)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.WindowPos));if((p.Flags&1)==0){p.Width=Math.Max(editing?260:80,Math.Min(1600,p.Width));p.Height=Math.Max(editing?60:20,Math.Min(editing?500:Screen.FromRectangle(options.Bounds).WorkingArea.Height,p.Height));Marshal.StructureToPtr(p,m.LParam,false);}}
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;signal.OnPending=null;hourly.Stop();hourly.Dispose();animation.Stop();animation.Dispose();audio.Dispose();image.Dispose();workspace.Dispose();}base.Dispose(disposing);}
}
