using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class SkillSlotPlacement {
 public int Slot,X,Y,Scale=100;public string Key="";public bool Visible=true;
 internal SkillSlotPlacement Copy(){return (SkillSlotPlacement)MemberwiseClone();}
}
internal sealed class SkillSlotAssignment {
 public int Index;public uint Skill,Alternative,Third;
 internal SkillSlotAssignment Copy(){return (SkillSlotAssignment)MemberwiseClone();}
 internal SkillSlotsSignal.Slot Slot(){return new SkillSlotsSignal.Slot{Index=Index,Skill=Skill,Alternative=Alternative,Third=Third};}
}
internal sealed class SkillSlotsOptions {
 public bool Enabled=false,Blur=true,GroupMode=true,Snap=true,ShowKey=true,ShowCountdown=true,AnimateKeyPresses,FlashWhenReady;
 public int Style;
 // Legacy native-file preferences are retained for compatibility, never applied.
 public bool? HideNativeHotbar=null;public string NativeHotbarPath="";public string PreparedHudFile="";
 public int X=650,Y=700,Scale=100,Size=76,Gap=10,Columns=12,SnapDistance=14,Opacity=100,Tint=18;
 public List<SkillSlotPlacement> Placements=new List<SkillSlotPlacement>();
 public List<SkillSlotAssignment> KnownSlots=new List<SkillSlotAssignment>();
 internal SkillSlotsOptions Copy(){var o=(SkillSlotsOptions)MemberwiseClone();o.Placements=Placements.Select(p=>p.Copy()).ToList();o.KnownSlots=KnownSlots.Select(p=>p.Copy()).ToList();return o;}
 internal static int Clamp(int n,int lo,int hi){return Math.Max(lo,Math.Min(hi,n));}
 internal void Normalize(){PreparedHudFile=(PreparedHudFile??"").Trim();if(PreparedHudFile.Length>1024)PreparedHudFile="";NativeHotbarPath=(NativeHotbarPath??"").Trim();if(NativeHotbarPath.Length>1024)NativeHotbarPath="";X=Clamp(X,-100000,100000);Y=Clamp(Y,-100000,100000);Scale=Clamp(Scale,40,250);Size=Clamp(Size,40,160);Gap=Clamp(Gap,0,100);Columns=Clamp(Columns,1,24);SnapDistance=Clamp(SnapDistance,2,40);Opacity=Clamp(Opacity,10,100);Tint=Clamp(Tint,0,85);Style=Clamp(Style,0,1);if(KnownSlots==null)KnownSlots=new List<SkillSlotAssignment>();KnownSlots=KnownSlots.Where(p=>p!=null&&p.Index>=0&&p.Index<48&&p.Skill>0).GroupBy(p=>p.Index).Select(p=>p.First()).OrderBy(p=>p.Index).Take(48).ToList();if(Placements==null)Placements=new List<SkillSlotPlacement>();Placements=Placements.Where(p=>p!=null&&p.Slot>=0&&p.Slot<48).GroupBy(p=>p.Slot).Select(p=>p.First()).Take(48).ToList();foreach(var p in Placements){p.X=Clamp(p.X,-10000,10000);p.Y=Clamp(p.Y,-10000,10000);p.Scale=Clamp(p.Scale,40,250);p.Key=(p.Key??"").Trim();if(p.Key.Length>8)p.Key=p.Key.Substring(0,8);}}
 internal void Remember(List<SkillSlotsSignal.Slot> slots){if(KnownSlots.Count==slots.Count&&KnownSlots.Select((p,n)=>p.Index==slots[n].Index&&p.Skill==slots[n].Skill&&p.Alternative==slots[n].Alternative&&p.Third==slots[n].Third).All(v=>v))return;KnownSlots=slots.Select(p=>new SkillSlotAssignment{Index=p.Index,Skill=p.Skill,Alternative=p.Alternative,Third=p.Third}).ToList();}
 internal List<SkillSlotsSignal.Slot> EditableSlots(SkillSlotsSignal.View live){return live==null?KnownSlots.Select(p=>p.Slot()).ToList():live.Slots;}
 internal void AutoLabels(IEnumerable<int> slots){int n=0;foreach(int slot in slots)Ensure(slot,n++).Key="";}
 internal static string Label(SkillSlotPlacement p,int ordinal){return string.IsNullOrEmpty(p.Key)?(ordinal+1).ToString():p.Key;}
 internal static SkillSlotsOptions Read(Dictionary<string,object> cfg){var o=new SkillSlotsOptions();object value;if(cfg.TryGetValue("skillSlots",out value))try{o=new JavaScriptSerializer().ConvertToType<SkillSlotsOptions>(value)??o;}catch(ArgumentException){}catch(InvalidOperationException){}o.Normalize();return o;}
 internal static bool Same(SkillSlotsOptions a,SkillSlotsOptions b){return new JavaScriptSerializer().Serialize(a)==new JavaScriptSerializer().Serialize(b);}
 internal SkillSlotPlacement Placement(int index,int ordinal){var p=Placements.Find(v=>v.Slot==index);return p??new SkillSlotPlacement{Slot=index,X=ordinal%Columns*(Size+Gap),Y=ordinal/Columns*(Size+Gap)};}
 internal Rectangle Rect(SkillSlotPlacement p){double s=Scale/100.0;int size=Math.Max(20,(int)Math.Round(Size*s*p.Scale/100.0));return new Rectangle(X+(int)Math.Round(p.X*s),Y+(int)Math.Round(p.Y*s),size,size);}
 internal SkillSlotPlacement Ensure(int index,int ordinal){var p=Placements.Find(v=>v.Slot==index);if(p==null){p=Placement(index,ordinal);Placements.Add(p);}return p;}
 internal void Arrange(IEnumerable<int> slots){int n=0;foreach(int slot in slots){var p=Ensure(slot,n);p.X=n%Columns*(Size+Gap);p.Y=n/Columns*(Size+Gap);p.Scale=100;n++;}}
 internal static Point Snapped(Rectangle moving,IEnumerable<Rectangle> others,int gap,int distance){
  bool x,y;
  return WidgetAlignment.Find(moving,others,gap,distance,out x,out y);
 }
}

internal sealed class SkillSlotAssets:IDisposable {
 Stream resource;ZipArchive zip;Dictionary<string,string> tokens;readonly Dictionary<uint,Bitmap> cache=new Dictionary<uint,Bitmap>();readonly Queue<uint> order=new Queue<uint>();
 internal Bitmap Icon(uint skill){uint id=skill/10000;Bitmap b;if(cache.TryGetValue(id,out b))return b;if(tokens==null){resource=typeof(SkillSlotAssets).Assembly.GetManifestResourceStream("SkillSlotIcons.zip");tokens=new Dictionary<string,string>();if(resource!=null&&resource.Length<6000000){zip=new ZipArchive(resource,ZipArchiveMode.Read);var entry=zip.GetEntry("skill_icon_tokens.json");if(entry!=null&&entry.Length<200000)using(var reader=new StreamReader(entry.Open()))tokens=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(reader.ReadToEnd());}}string token;if(zip!=null&&tokens.TryGetValue(id.ToString(),out token)&&token.Length<80&&!token.Contains("/")&&!token.Contains("\\")){var entry=zip.GetEntry(token+".png");if(entry!=null&&entry.Length<200000)using(var input=entry.Open())using(var loaded=Image.FromStream(input)){if(loaded.Width<=256&&loaded.Height<=256)b=new Bitmap(loaded);}}if(cache.Count>=64){uint old=order.Dequeue();var image=cache[old];if(image!=null)image.Dispose();cache.Remove(old);}cache[id]=b;order.Enqueue(id);return b;}
 internal int Cached {get{return cache.Count;}}
 public void Dispose(){foreach(var b in cache.Values)if(b!=null)b.Dispose();cache.Clear();if(zip!=null)zip.Dispose();if(resource!=null)resource.Dispose();}
}
internal sealed class SkillSlotPainter:IDisposable {
 internal struct IconFrame {
  internal uint Skill;
  internal bool Cooling,Unavailable,ReactiveReady,Compact;
  internal double Fill,RevealShade;
  internal int ReactivePulse,ReadyFlash;
 }
 internal readonly SkillSlotAssets Assets=new SkillSlotAssets();readonly CrispOverlayText counter=new CrispOverlayText(),key=new CrispOverlayText();
 internal static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 internal static void PaintCompactPlate(Graphics g,GraphicsPath shape,RectangleF bounds){
  using(var fill=new SolidBrush(Color.FromArgb(255,12,12,14)))g.FillPath(fill,shape);
  using(var edge=new Pen(Color.FromArgb(240,0,0,0),Math.Max(1,bounds.Height*.014f)))g.DrawPath(edge,shape);
 }
 internal Bitmap Render(int size,uint skill,string label,bool cooling,double filled,string text,bool chain,bool editing,int tint,bool compact=false){return Render(size,new IconFrame{Skill=skill,Cooling=cooling,Fill=filled,Compact=compact},label,text,chain,editing,tint,false,new IconFrame(),1);}
 internal Bitmap Render(int size,IconFrame current,string label,string text,bool chain,bool editing,int tint,bool pressed,IconFrame outgoing,double slide){
 var bitmap=new Bitmap(size,size,PixelFormat.Format32bppPArgb);float s=size/100f;using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
 using(var plate=Round(new RectangleF(1*s,1*s,98*s,98*s),(current.Compact?13:10)*s)){
  if(!current.Compact){
   using(var brush=new LinearGradientBrush(new RectangleF(0,0,size,size),Color.FromArgb(Math.Min(230,tint*255/100+12),248,252,255),Color.FromArgb(tint*190/100,231,242,250),90f))g.FillPath(brush,plate);
  }else PaintCompactPlate(g,plate,new RectangleF(0,0,size,size));
  if(pressed||current.ReactiveReady){
   int greenGlow=15+190*current.ReactivePulse/12,greenEdge=36+219*current.ReactivePulse/12;
   using(var rim=Round(new RectangleF(4*s,4*s,92*s,92*s),8*s))
   using(var glow=new Pen(pressed?Color.FromArgb(205,255,190,31):Color.FromArgb(greenGlow,53,244,75),5.4f*s))g.DrawPath(glow,rim);
   using(var highlight=new Pen(pressed?Color.FromArgb(255,255,243,154):Color.FromArgb(greenEdge,173,255,170),3.2f*s))g.DrawPath(highlight,plate);
  }else if(chain||!current.Compact){
   using(var pen=new Pen(chain?Color.FromArgb(230,126,242,213):Color.FromArgb(115,242,249,255),(chain?1.8f:.8f)*s))g.DrawPath(pen,plate);
  }
 }
 float inset=current.Compact?3.5f:13;
 var iconRect=new RectangleF(inset*s,inset*s,(100-2*inset)*s,(100-2*inset)*s);
 using(var clip=Round(iconRect,(current.Compact?10:5)*s)){
  var saved=g.Save();g.SetClip(clip);
  if(outgoing.Skill!=0&&slide<1){
   float offset=(float)(iconRect.Width*slide);
   var old=iconRect;old.Offset(offset,0);PaintIcon(g,old,outgoing,s);
   iconRect.Offset(offset-iconRect.Width,0);
  }
  PaintIcon(g,iconRect,current,s);g.Restore(saved);
 }
 if(current.ReadyFlash>0){
  float phase=1-current.ReadyFlash/16f;
  int strength=(int)(210*(1-phase));
  var saved=g.Save();
  using(var shape=Round(new RectangleF(2*s,2*s,96*s,96*s),9*s)){
   g.SetClip(shape);
   using(var rim=new Pen(Color.FromArgb(strength,255,218,105),4*s))g.DrawPath(rim,shape);
   float center=size*(-.2f+1.4f*phase);
   using(var glow=new LinearGradientBrush(new PointF(center-17*s,0),new PointF(center+17*s,12*s),Color.Transparent,Color.Transparent)){
    glow.InterpolationColors=new ColorBlend{Colors=new[]{Color.Transparent,Color.FromArgb(strength,255,237,171),Color.Transparent},Positions=new[]{0f,.5f,1f}};
    glow.WrapMode=WrapMode.TileFlipX;
    var stripe=new PointF[]{new PointF(center-17*s,0),new PointF(center+17*s,0),new PointF(center+52*s,size),new PointF(center+18*s,size)};
    g.FillPolygon(glow,stripe);
   }
  }
  g.Restore(saved);
 }
 var outline=new TextOutlineOptions{Enabled=true,Width=Math.Max(1,(int)Math.Round(1.5*s)),Opacity=75};if(current.Cooling)counter.Draw(g,text,Math.Max(11,(int)((current.Compact?36:28)*s)),true,Color.White,new Rectangle((int)(8*s),(int)(29*s),(int)(84*s),(int)(42*s)),1,true,outline);
 if(!string.IsNullOrEmpty(label))key.Draw(g,label,Math.Max(9,(int)((current.Compact?18:13)*s)),true,Color.FromArgb(237,244,252),new Rectangle((int)(5*s),(int)(2*s),(int)(90*s),(int)(18*s)),0,true,outline);
 if(editing){using(var pen=new Pen(Color.FromArgb(130,104,221,209),Math.Max(1,s))){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,1,1,size-3,size-3);}using(var pen=new Pen(Color.FromArgb(210,194,236,245),Math.Max(1,s)))for(int n=0;n<3;n++)g.DrawLine(pen,size-6-(n*4)*s,size-4,size-4,size-6-(n*4)*s);}}
 return bitmap;}
 void PaintIcon(Graphics g,RectangleF rect,IconFrame frame,float scale){
  var icon=Assets.Icon(frame.Skill);
  if(icon==null){
   using(var brush=new SolidBrush(Color.FromArgb(180,27,52,70)))g.FillRectangle(brush,rect);
   float x=rect.X+rect.Width/2,y=rect.Y+rect.Height/2,arm=15*scale;
   using(var pen=new Pen(Color.FromArgb(110,205,239,255),2*scale)){g.DrawLine(pen,x-arm,y,x+arm,y);g.DrawLine(pen,x,y-arm,x,y+arm);}
  }else{
   g.DrawImage(icon,rect);
   if(frame.Cooling&&frame.Compact){
    float sweep=360*(float)(1-Math.Max(0,Math.Min(1,frame.Fill)));
    using(var brush=new SolidBrush(Color.FromArgb(174,0,0,0))){
     var circle=rect;circle.Inflate(rect.Width*.25f,rect.Height*.25f);
     if(sweep>0)g.FillPie(brush,circle.X,circle.Y,circle.Width,circle.Height,-90+sweep*-1,sweep);
    }
   }else if(frame.Cooling){
    using(var brush=new SolidBrush(Color.FromArgb(185,8,20,34)))g.FillRectangle(brush,rect);
    if(frame.Fill>0){var saved=g.Save();g.SetClip(new RectangleF(rect.X,rect.Y,rect.Width,(float)(rect.Height*Math.Min(1,frame.Fill))),CombineMode.Intersect);g.DrawImage(icon,rect);g.Restore(saved);}
   }
  }
  int shade=frame.Unavailable?155:(int)Math.Round(155*frame.RevealShade);
  if(shade>0&&!frame.Cooling)using(var brush=new SolidBrush(Color.FromArgb(shade,7,11,17)))g.FillRectangle(brush,rect);
 }
 public void Dispose(){counter.Dispose();key.Dispose();Assets.Dispose();}
}

internal sealed class SkillSlotMotion {
 internal const double SlideSeconds=.16;
 internal const double RevealSeconds=.12;
 internal const double PulseSeconds=.9;
 internal static int ReadyPulse(double elapsed){return (int)Math.Round(6+6*Math.Cos(2*Math.PI*elapsed/PulseSeconds));}
 internal static double CastFlashRemaining(double castAt,double physicalAt,double now){
  if(castAt<=0||now<castAt||physicalAt>0&&physicalAt>=castAt-.25)return 0;
  return Math.Max(0,castAt+.12-now);
 }
 SkillSlotPainter.IconFrame current;
 internal SkillSlotPainter.IconFrame Outgoing {get;private set;}
 internal double RevealShade {get;private set;}
 double start,revealUntil;
 internal double Advance(SkillSlotPainter.IconFrame next,double now,bool allowSlide){
  if(!allowSlide)Outgoing=new SkillSlotPainter.IconFrame();
  else if(current.Skill!=0&&current.Skill/10000!=next.Skill/10000){Outgoing=current;start=now;}
  if(!allowSlide||!next.ReactiveReady)revealUntil=0;
  else if(current.Unavailable)revealUntil=now+RevealSeconds;
  RevealShade=Math.Max(0,Math.Min(1,(revealUntil-now)/RevealSeconds));
  current=next;
  if(Outgoing.Skill==0)return 1;
  double progress=Math.Max(0,Math.Min(1,(now-start)/SlideSeconds));
  if(progress>=1)Outgoing=new SkillSlotPainter.IconFrame();
  return progress*progress*(3-2*progress);
 }
 internal void Reset(){current=Outgoing=new SkillSlotPainter.IconFrame();revealUntil=RevealShade=0;}
}

internal static class SkillSlotKey {
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 internal static bool Down(int key){return key!=0&&GetAsyncKeyState(key)<0;}
 internal static int Parse(string label){
  if(string.IsNullOrEmpty(label))return 0;
  if(label.Length==1){char key=char.ToUpperInvariant(label[0]);return key>='A'&&key<='Z'||key>='0'&&key<='9'?key:0;}
  switch(label.ToUpperInvariant()){
   case "M1":return 1;
   case "M2":return 2;
   case "M3":return 4;
   case "M4":return 5;
   case "M5":return 6;
   case "SPACE":return 32;
  }
  int number;
  return (label[0]=='F'||label[0]=='f')&&int.TryParse(label.Substring(1),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out number)&&number>=1&&number<=24?111+number:0;
 }
}

internal sealed class SkillSlotsOverlay:IDisposable {
 readonly EnergyOverlay owner;readonly SkillSlotsSignal signal;readonly bool preview;internal SkillSlotsOptions Options=new SkillSlotsOptions();internal bool Editing;readonly Dictionary<int,SkillSlotWindow> windows=new Dictionary<int,SkillSlotWindow>();internal readonly SkillSlotPainter Painter=new SkillSlotPainter();internal List<SkillSlotsSignal.Slot> Current=new List<SkillSlotsSignal.Slot>();SkillSlotsSignal.View lastView;string hideReason;bool test;double testStart;internal int NextInterval=1000;internal string StatusKey="";long lastRevision=-1;
 internal static readonly uint[] ExampleSkills={15210000,15280000,15400000,15200000,15310000,15110000,15320000,15390000,15040000,15060000,15010000};
 internal static List<SkillSlotsSignal.Slot> Examples(){return ExampleSkills.Select((id,n)=>new SkillSlotsSignal.Slot{Index=n,Skill=id}).ToList();}
 readonly Timer pressTimer;readonly StaggerSignal stagger;
 internal SkillSlotsOverlay(EnergyOverlay owner,SkillSlotsSignal signal,bool preview,StaggerSignal stagger=null){
  this.owner=owner;this.signal=signal;this.preview=preview;this.stagger=stagger;
  if(!preview){pressTimer=new Timer();pressTimer.Tick+=PollPresses;}
 }
 void SetPressPolling(bool enabled,bool foreground){
  if(pressTimer==null)return;
  if(enabled){
   int interval=foreground?16:125;
   if(pressTimer.Interval!=interval)pressTimer.Interval=interval;
  }
  pressTimer.Enabled=enabled;
 }
 void PollPresses(object sender,EventArgs e){
  bool foreground=FloatingTextService.GameForeground();
  double now=DpsHistory.UtcNow();
  bool plain=Unmodified(foreground);
  bool changed=false;
  foreach(var w in windows.Values)
   if(w.Visible&&w.HasPressBinding)changed|=w.IsPressed(plain)!=w.PresentedPhysicalPress;
  if(changed)Maintain(now);
  else SetPressPolling(true,foreground);
 }
 static bool Unmodified(bool foreground){return foreground&&!SkillSlotKey.Down(16)&&!SkillSlotKey.Down(17)&&!SkillSlotKey.Down(18);}
 internal void Configure(SkillSlotsOptions o,bool locked){Options=o.Copy();Options.Normalize();Editing=!locked;signal.Configure(!preview&&Options.Enabled,Options.AnimateKeyPresses);var view=signal.Read(DpsHistory.UtcNow());if(view!=null)Options.Remember(view.Slots);}
 internal void Test(){test=true;testStart=DpsHistory.UtcNow();Maintain();}
 internal static double LoopSeconds(double elapsed){return Math.Max(0,12-Math.Max(0,elapsed)%13);}
 internal double TestSeconds(double now){return test?LoopSeconds(now-testStart):0;}
 internal void StopTest(){test=false;Maintain();}
 internal bool Testing {get{return test;}}
 internal static string Counter(SkillSlotsSignal.Cooldown cd,double now,bool numbers){double remaining=SkillSlotsSignal.Remaining(cd,now);return cd==null||remaining<=0?null:numbers?Math.Ceiling(remaining).ToString():"CD";}
 internal void Maintain(){Maintain(DpsHistory.UtcNow());}
 internal void Maintain(double now){
  var view=signal.Read(now);
  bool sameCharacter=view!=null&&lastView!=null&&view.Flow==lastView.Flow&&view.Self==lastView.Self&&view.Map==lastView.Map;
  if(view!=null&&!object.ReferenceEquals(view,lastView))Options.Remember(view.Slots);
  lastView=view;
  bool sample=test||Options.Enabled&&Editing&&view==null&&Options.KnownSlots.Count>0;
  bool shell=!preview&&DpsDesign.ShellUiActive();
  bool show=(Options.Enabled||test)&&(view!=null||sample)&&!shell&&!owner.WidgetsSuppressed&&(Editing||test||owner.CombatWidgetOpacity(Options.Opacity/100.0)>0);
  NextInterval=1000;
  if(!show){
   hideReason=!Options.Enabled&&!test?"disabled":owner.WidgetsSuppressed?"Aion is not in the foreground":shell?"Windows shell":"waiting for own bar or stale transport";
   SetPressPolling(false,false);
   foreach(var window in windows.Values)window.Conceal();
   StatusKey="hidden/"+hideReason;return;
  }
  hideReason=null;
  Current=sample?(test?(view!=null?view.Slots:Options.KnownSlots.Count>0?Options.EditableSlots(null):Examples()):Options.EditableSlots(null)):view.Slots;
  int ordinal=0;
  foreach(var slot in Current)Options.Ensure(slot.Index,ordinal++);
  bool watchKeys=Options.AnimateKeyPresses&&!preview&&!sample&&!Editing;
  bool foreground=watchKeys&&FloatingTextService.GameForeground();
  bool noModifiers=Unmodified(foreground),poll=false;
  var active=new HashSet<int>();int coolCount=0,tickKey=17,labelOrdinal=0;
  foreach(var slot in Current){
   int displayOrdinal=labelOrdinal++;
   var placement=Options.Placement(slot.Index,0);
   if(!placement.Visible)continue;
   active.Add(slot.Index);
   SkillSlotWindow window;
   if(!windows.TryGetValue(slot.Index,out window))windows[slot.Index]=window=new SkillSlotWindow(this,owner,slot.Index);
   uint id=sample?slot.Skill:SkillSlotsSignal.DisplaySkill(view,slot,now);
   if(!sample&&id!=slot.Skill&&(view.FireMarkUntil>now||view.StunUntil>now||view.HeartGoreUntil>now||view.EvasionUntil>now))NextInterval=Math.Min(NextInterval,50);
   var cd=sample?null:SkillSlotsSignal.For(view,id);
   string counter=sample?null:Counter(cd,now,Options.ShowCountdown);
   bool sampleUnavailable=sample&&test&&displayOrdinal==1;
   bool sampleReactive=sample&&test&&displayOrdinal==2;
   bool cooling=sample?test&&!sampleUnavailable&&!sampleReactive:counter!=null;
   bool? requirement=sample?null:SkillSlotsSignal.Requirement(view,slot,id,now,stagger);
   string text=counter??"";
   double fill=0;
   if(sample&&cooling){
    double seconds=TestSeconds(now);
    cooling=seconds>0;
    text=cooling?(Options.ShowCountdown?Math.Ceiling(seconds).ToString():"CD"):"";
    fill=1-seconds/12;
   }else if(cooling){
    double seconds=SkillSlotsSignal.Remaining(cd,now);
    fill=cd.Peak>0?1-Math.Min(1,seconds*1000/cd.Peak):0;
   }
   if(cooling){coolCount++;NextInterval=Math.Min(NextInterval,50);}
   if(requirement==true&&!cooling)NextInterval=Math.Min(NextInterval,40);
   unchecked{tickKey=tickKey*31+text.GetHashCode();tickKey=tickKey*31+(requirement==null?0:requirement.Value?1:2);}
   bool physical=window.ObservePress(placement.Key,watchKeys&&noModifiers,now);
   double castFlash=foreground&&!physical?SkillSlotMotion.CastFlashRemaining(slot.CastAt,window.LastPhysicalPress,now):0;
   if(castFlash>0)NextInterval=Math.Min(NextInterval,Math.Max(1,(int)Math.Ceiling(castFlash*1000)));
   bool pressed=physical||castFlash>0;
   poll|=watchKeys&&window.HasPressBinding;
   string label=Options.ShowKey?SkillSlotsOptions.Label(placement,displayOrdinal):"";
   var frame=new SkillSlotPainter.IconFrame{Skill=id,Cooling=cooling,Fill=fill,Compact=Options.Style==1,
    Unavailable=sampleUnavailable||!cooling&&requirement==false,
    ReactiveReady=!cooling&&(requirement==true||sampleReactive)};
   if(window.UpdateSlot(Options.Rect(placement),slot.Skill,frame,label,text,!sample&&id!=slot.Skill,Editing,Options,pressed,now,sameCharacter&&!sample,test))
    NextInterval=Math.Min(NextInterval,16);
  }
  SetPressPolling(poll,foreground);
  if(test)NextInterval=Math.Min(NextInterval,50);
  foreach(var pair in windows)if(!active.Contains(pair.Key))pair.Value.Conceal();
  lastRevision=view==null?-1:view.Revision;StatusKey=show+"/"+sample+"/"+lastRevision+"/"+coolCount+"/"+tickKey;
 }
 internal object Status {get{double now=DpsHistory.UtcNow();var v=lastView;return new{enabled=Options.Enabled,editing=Editing,testing=test,hideReason,sourceRevision=lastRevision,visibleSlots=windows.Values.Count(w=>w.Visible),uploads=windows.Values.Sum(w=>w.Uploads),cachedIcons=Painter.Assets.Cached,showCountdown=Options.ShowCountdown,cooldownSource="0238 own cast + 4738 correction, milliseconds",receiver=signal.Diagnostic(now),contextTarget=v==null?(uint?)null:v.ContextTarget,fireMarkRemaining=v==null?(double?)null:Math.Max(0,v.FireMarkUntil-now),slots=v==null?null:v.Slots.Select((s,n)=>{uint id=SkillSlotsSignal.DisplaySkill(v,s,now);var cd=SkillSlotsSignal.For(v,id);return new{number=n+1,label=SkillSlotsOptions.Label(Options.Placement(s.Index,n),n),slot=s.Index,skill=s.Skill,displayedSkill=id,lastOwnCastAt=s.CastAt>0?(double?)s.CastAt:null,cooldownKnown=cd!=null,remainingMilliseconds=cd==null?(uint?)null:cd.Raw,remainingSeconds=cd==null?(double?)null:Math.Round(SkillSlotsSignal.Remaining(cd,now),3),source=cd==null?null:cd.FromCast?"own cast":"server correction",requirement=SkillSlotsSignal.Requirement(v,s,id,now,stagger),visible=windows.ContainsKey(s.Index)&&windows[s.Index].Visible};}).ToArray(),timer=false};}}
 internal void Drag(int index,SkillSlotsOptions start,Point delta,bool resize){
  Options=start.Copy();
  var p=Options.Placements.Find(v=>v.Slot==index);
  if(p==null)return;
  if(resize){
   if(Options.GroupMode)Options.Scale=SkillSlotsOptions.Clamp(start.Scale+delta.X,40,250);
   else p.Scale=SkillSlotsOptions.Clamp(p.Scale+delta.X,40,250);
  }else if(Options.GroupMode){
   Options.X+=delta.X;
   Options.Y+=delta.Y;
   if(owner.WidgetSnapEnabled){
    var group=Rectangle.Empty;
    for(int i=0;i<Current.Count;i++){
     var placement=Options.Placement(Current[i].Index,i);
     if(!placement.Visible)continue;
     var card=Options.Rect(placement);
     group=group.IsEmpty?card:Rectangle.Union(group,card);
    }
    if(!group.IsEmpty){
     var aligned=owner.SnapWidget(null,group,true);
     Options.X+=aligned.X-group.X;
     Options.Y+=aligned.Y-group.Y;
    }
   }
  }else{
   double scale=Options.Scale/100.0;
   var bounds=Options.Rect(p);
   bounds.Offset(delta);
   bool localX=false,localY=false;
   if(Options.Snap){
    var others=Current.Where(v=>v.Index!=index).Select((v,n)=>Options.Rect(Options.Placement(v.Index,n)));
    bounds.Location=WidgetAlignment.Find(bounds,others,(int)Math.Round(Options.Gap*scale),Options.SnapDistance,out localX,out localY);
   }
   bounds=owner.SnapWidget(null,bounds,true,!localX,!localY);
   p.X=(int)Math.Round((bounds.X-Options.X)/scale);
   p.Y=(int)Math.Round((bounds.Y-Options.Y)/scale);
  }
  Maintain();
 }
 internal void Commit(){var cfg=owner.Configuration;cfg.SkillSlots=Options.Copy();owner.ApplyConfiguration(cfg);}
 public void Dispose(){if(pressTimer!=null)pressTimer.Dispose();foreach(var w in windows.Values)w.Dispose();windows.Clear();Painter.Dispose();}
}
internal sealed class SkillSlotWindow:Form,IWidgetSnapTarget {
    Rectangle IWidgetSnapTarget.SnapBounds {get{return Bounds;}}
 readonly SkillSlotsOverlay service;readonly EnergyOverlay owner;readonly int index;readonly LayeredImage image=new LayeredImage();OverlayGlassLayer glass;string key;bool editing,dragging,resizing;Point startMouse,presented;SkillSlotsOptions start;byte alpha;internal int Uploads {get{return image.Uploads;}}
 internal SkillSlotWindow(SkillSlotsOverlay service,EnergyOverlay owner,int index){this.service=service;this.owner=owner;this.index=index;Text="Aion 2 Helper - Skill "+(index+1);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;}
 readonly SkillSlotMotion motion=new SkillSlotMotion();
 uint assignment;
 string pressLabel;
 int pressKey;
 double reactiveStarted,readyStarted=-1;
 uint coolingSkill;
 bool wasReactive;
 internal bool HasPressBinding {get{return pressKey!=0;}}
 internal bool PresentedPhysicalPress {get;private set;}
 internal double LastPhysicalPress {get;private set;}
 internal bool ObservePress(string label,bool readKeys,double now){
  if(label!=pressLabel){pressLabel=label;pressKey=SkillSlotKey.Parse(label);LastPhysicalPress=0;PresentedPhysicalPress=false;}
  bool down=IsPressed(readKeys);
  if(down||PresentedPhysicalPress)LastPhysicalPress=now;
  PresentedPhysicalPress=down;
  return down;
 }
 internal bool IsPressed(bool readKeys){return readKeys&&SkillSlotKey.Down(pressKey);}
 internal bool UpdateSlot(Rectangle bounds,uint assigned,SkillSlotPainter.IconFrame frame,string label,string countdown,bool chain,bool edit,SkillSlotsOptions o,bool pressed,double now,bool allowSlide,bool test=false){
  bool changedStyle=editing!=edit;editing=edit;
  if(Bounds!=bounds)Bounds=bounds;
  if(changedStyle||!IsHandleCreated){int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);}
  double slide=motion.Advance(frame,now,Visible&&allowSlide&&assignment==assigned);
  bool allowFlash=o.FlashWhenReady&&(test||allowSlide&&Visible&&!edit)&&assignment==assigned;
  if(!allowFlash||frame.Cooling||frame.Unavailable||coolingSkill!=0&&coolingSkill!=frame.Skill)readyStarted=-1;
  else if(coolingSkill==frame.Skill&&frame.Skill!=0)readyStarted=now;
  coolingSkill=frame.Cooling?frame.Skill:0;
  double flash=readyStarted<0?0:Math.Max(0,1-(now-readyStarted)/.66);
  frame.ReadyFlash=pressed?0:(int)Math.Ceiling(flash*16);
  assignment=assigned;
  var outgoing=motion.Outgoing;
  frame.RevealShade=motion.RevealShade;
  if(frame.ReactiveReady&&!wasReactive)reactiveStarted=now;
  wasReactive=frame.ReactiveReady;
  frame.ReactivePulse=wasReactive&&!pressed?SkillSlotMotion.ReadyPulse(now-reactiveStarted):0;
  string next=Width+"/"+frame.Skill+"/"+label+"/"+frame.Cooling+"/"+(int)(frame.Fill*Width)+"/"+countdown+"/"+chain+"/"+editing+"/"+o.Tint+"/"+pressed+"/"+frame.Unavailable+"/"+frame.ReactiveReady+"/"+frame.ReactivePulse+"/"+(int)Math.Round(frame.RevealShade*155)+"/"+outgoing.Skill+"/"+(int)Math.Round(slide*Width)+"/"+frame.Compact+"/"+frame.ReadyFlash;
  if(next!=key){using(var b=service.Painter.Render(Width,frame,label,countdown,chain,editing,o.Tint,pressed,outgoing,slide))image.Upload(b);key=next;alpha=0;}
  double configured=o.Opacity/100.0;byte a=(byte)Math.Round((test||edit?configured:owner.CombatWidgetOpacity(configured))*255);if(o.Blur&&o.Style==0){if(glass==null)glass=new OverlayGlassLayer();int pad=Math.Max(1,(int)(Width*.01));glass.Sync(new Rectangle(Left+pad,Top+pad,Width-2*pad,Height-2*pad),a,Handle,true,Width*.10f);}else if(glass!=null){glass.Dispose();glass=null;}
  if(!Visible||alpha!=a||presented!=Location){image.Present(Handle,Left,Top,a);alpha=a;presented=Location;}if(!Visible){Show();OverlayNative.RaiseWithoutFocus(Handle);}
  return outgoing.Skill!=0||frame.RevealShade>0||flash>0;
 }
 internal void Conceal(){motion.Reset();wasReactive=false;readyStarted=-1;coolingSkill=0;PresentedPhysicalPress=false;LastPhysicalPress=0;if(Visible)Hide();if(glass!=null){glass.Dispose();glass=null;}}
 protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if((m.Msg==0x205||m.Msg==0xa5)&&editing){owner.OpenSkillSlotsSettings();m.Result=IntPtr.Zero;return;}if(m.Msg==0x84){m.Result=new IntPtr(editing?1:-1);return;}if(m.Msg==0x201&&editing){startMouse=Cursor.Position;start=service.Options.Copy();var pt=PointToClient(startMouse);resizing=pt.X>Width-14&&pt.Y>Height-14;dragging=true;Capture=true;m.Result=IntPtr.Zero;return;}if(m.Msg==0x200&&dragging){var mouse=Cursor.Position;service.Drag(index,start,new Point(mouse.X-startMouse.X,mouse.Y-startMouse.Y),resizing);m.Result=IntPtr.Zero;return;}if((m.Msg==0x202||m.Msg==0x215)&&dragging){dragging=false;Capture=false;service.Commit();m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
 protected override void Dispose(bool disposing){if(disposing){image.Dispose();if(glass!=null)glass.Dispose();}base.Dispose(disposing);}
}
