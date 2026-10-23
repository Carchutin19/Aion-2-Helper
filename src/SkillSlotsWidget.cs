using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
 public bool Enabled=false,Blur=true,GroupMode=true,Snap=true,ShowKey=true,ShowCountdown=true;
 public bool? HideNativeHotbar=null;public string NativeHotbarPath="";
 public int X=650,Y=700,Scale=100,Size=76,Gap=10,Columns=12,SnapDistance=14,Opacity=100,Tint=18;
 public List<SkillSlotPlacement> Placements=new List<SkillSlotPlacement>();
 public List<SkillSlotAssignment> KnownSlots=new List<SkillSlotAssignment>();
 internal SkillSlotsOptions Copy(){var o=(SkillSlotsOptions)MemberwiseClone();o.Placements=Placements.Select(p=>p.Copy()).ToList();o.KnownSlots=KnownSlots.Select(p=>p.Copy()).ToList();return o;}
 internal static int Clamp(int n,int lo,int hi){return Math.Max(lo,Math.Min(hi,n));}
 internal void Normalize(){NativeHotbarPath=(NativeHotbarPath??"").Trim();if(NativeHotbarPath.Length>1024)NativeHotbarPath="";X=Clamp(X,-100000,100000);Y=Clamp(Y,-100000,100000);Scale=Clamp(Scale,40,250);Size=Clamp(Size,40,160);Gap=Clamp(Gap,0,100);Columns=Clamp(Columns,1,24);SnapDistance=Clamp(SnapDistance,2,40);Opacity=Clamp(Opacity,10,100);Tint=Clamp(Tint,0,85);if(KnownSlots==null)KnownSlots=new List<SkillSlotAssignment>();KnownSlots=KnownSlots.Where(p=>p!=null&&p.Index>=0&&p.Index<48&&p.Skill>0).GroupBy(p=>p.Index).Select(p=>p.First()).OrderBy(p=>p.Index).Take(48).ToList();if(Placements==null)Placements=new List<SkillSlotPlacement>();Placements=Placements.Where(p=>p!=null&&p.Slot>=0&&p.Slot<48).GroupBy(p=>p.Slot).Select(p=>p.First()).Take(48).ToList();foreach(var p in Placements){p.X=Clamp(p.X,-10000,10000);p.Y=Clamp(p.Y,-10000,10000);p.Scale=Clamp(p.Scale,40,250);p.Key=(p.Key??"").Trim();if(p.Key.Length>8)p.Key=p.Key.Substring(0,8);}}
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
 internal static Point Snapped(Rectangle moving,IEnumerable<Rectangle> others,int gap,int distance){int bestX=distance+1,bestY=distance+1,dx=0,dy=0;foreach(var r in others){int[] xs={r.Left-moving.Left,r.Right-moving.Right,r.Right+gap-moving.Left,r.Left-gap-moving.Right,r.Left+r.Width/2-moving.Left-moving.Width/2};int[] ys={r.Top-moving.Top,r.Bottom-moving.Bottom,r.Bottom+gap-moving.Top,r.Top-gap-moving.Bottom,r.Top+r.Height/2-moving.Top-moving.Height/2};if(moving.Bottom+distance+gap>=r.Top&&moving.Top-distance-gap<=r.Bottom)foreach(int x in xs)if(Math.Abs(x)<bestX){bestX=Math.Abs(x);dx=x;}if(moving.Right+distance+gap>=r.Left&&moving.Left-distance-gap<=r.Right)foreach(int y in ys)if(Math.Abs(y)<bestY){bestY=Math.Abs(y);dy=y;}}return new Point(moving.X+dx,moving.Y+dy);}
}

internal sealed class SkillSlotAssets:IDisposable {
 Stream resource;ZipArchive zip;Dictionary<string,string> tokens;readonly Dictionary<uint,Bitmap> cache=new Dictionary<uint,Bitmap>();readonly Queue<uint> order=new Queue<uint>();
 internal Bitmap Icon(uint skill){uint id=skill/10000;Bitmap b;if(cache.TryGetValue(id,out b))return b;if(tokens==null){resource=typeof(SkillSlotAssets).Assembly.GetManifestResourceStream("SkillSlotIcons.zip");tokens=new Dictionary<string,string>();if(resource!=null&&resource.Length<6000000){zip=new ZipArchive(resource,ZipArchiveMode.Read);var entry=zip.GetEntry("skill_icon_tokens.json");if(entry!=null&&entry.Length<200000)using(var reader=new StreamReader(entry.Open()))tokens=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(reader.ReadToEnd());}}string token;if(zip!=null&&tokens.TryGetValue(id.ToString(),out token)&&token.Length<80&&!token.Contains("/")&&!token.Contains("\\")){var entry=zip.GetEntry(token+".png");if(entry!=null&&entry.Length<200000)using(var input=entry.Open())using(var loaded=Image.FromStream(input)){if(loaded.Width<=256&&loaded.Height<=256)b=new Bitmap(loaded);}}if(cache.Count>=64){uint old=order.Dequeue();var image=cache[old];if(image!=null)image.Dispose();cache.Remove(old);}cache[id]=b;order.Enqueue(id);return b;}
 internal int Cached {get{return cache.Count;}}
 public void Dispose(){foreach(var b in cache.Values)if(b!=null)b.Dispose();cache.Clear();if(zip!=null)zip.Dispose();if(resource!=null)resource.Dispose();}
}
internal sealed class SkillSlotPainter:IDisposable {
 internal readonly SkillSlotAssets Assets=new SkillSlotAssets();readonly CrispOverlayText counter=new CrispOverlayText(),key=new CrispOverlayText();
 internal static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 internal Bitmap Render(int size,uint skill,string label,bool cooling,double filled,string text,bool chain,bool editing,int tint){var bitmap=new Bitmap(size,size,PixelFormat.Format32bppPArgb);float s=size/100f;using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;using(var plate=Round(new RectangleF(1*s,1*s,98*s,98*s),10*s)){using(var brush=new LinearGradientBrush(new RectangleF(0,0,size,size),Color.FromArgb(Math.Min(230,tint*255/100+12),248,252,255),Color.FromArgb(tint*190/100,231,242,250),90f))g.FillPath(brush,plate);using(var pen=new Pen(chain?Color.FromArgb(230,126,242,213):Color.FromArgb(115,242,249,255),(chain?1.8f:.8f)*s))g.DrawPath(pen,plate);}
 var iconRect=new RectangleF(13*s,13*s,74*s,74*s);using(var clip=Round(iconRect,5*s)){var previous=g.Save();g.SetClip(clip);var icon=Assets.Icon(skill);if(icon!=null){g.DrawImage(icon,iconRect);if(cooling){using(var brush=new SolidBrush(Color.FromArgb(185,8,20,34)))g.FillRectangle(brush,iconRect);if(filled>0){g.SetClip(new RectangleF(iconRect.X,iconRect.Y,iconRect.Width,(float)(iconRect.Height*Math.Min(1,filled))),CombineMode.Intersect);g.DrawImage(icon,iconRect);}}}else{using(var brush=new SolidBrush(Color.FromArgb(180,27,52,70)))g.FillRectangle(brush,iconRect);using(var pen=new Pen(Color.FromArgb(110,205,239,255),2*s)){g.DrawLine(pen,35*s,50*s,65*s,50*s);g.DrawLine(pen,50*s,35*s,50*s,65*s);}}g.Restore(previous);}
 var outline=new TextOutlineOptions{Enabled=true,Width=Math.Max(1,(int)Math.Round(1.5*s)),Opacity=75};if(cooling)counter.Draw(g,text,Math.Max(11,(int)(28*s)),true,Color.White,new Rectangle((int)(8*s),(int)(29*s),(int)(84*s),(int)(42*s)),1,true,outline);
 if(!string.IsNullOrEmpty(label))key.Draw(g,label,Math.Max(9,(int)(13*s)),true,Color.FromArgb(237,244,252),new Rectangle((int)(5*s),(int)(2*s),(int)(90*s),(int)(18*s)),0,true,outline);
 if(editing){using(var pen=new Pen(Color.FromArgb(130,104,221,209),Math.Max(1,s))){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,1,1,size-3,size-3);}using(var pen=new Pen(Color.FromArgb(210,194,236,245),Math.Max(1,s)))for(int n=0;n<3;n++)g.DrawLine(pen,size-6-(n*4)*s,size-4,size-4,size-6-(n*4)*s);}}
 return bitmap;}
 public void Dispose(){counter.Dispose();key.Dispose();Assets.Dispose();}
}

internal sealed class SkillSlotsOverlay:IDisposable {
 readonly EnergyOverlay owner;readonly SkillSlotsSignal signal;readonly bool preview;internal SkillSlotsOptions Options=new SkillSlotsOptions();internal bool Editing;readonly Dictionary<int,SkillSlotWindow> windows=new Dictionary<int,SkillSlotWindow>();internal readonly SkillSlotPainter Painter=new SkillSlotPainter();internal List<SkillSlotsSignal.Slot> Current=new List<SkillSlotsSignal.Slot>();SkillSlotsSignal.View lastView;string hideReason;bool test;double testStart;internal int NextInterval=1000;internal string StatusKey="";long lastRevision=-1;
 internal static readonly uint[] ExampleSkills={15210000,15280000,15400000,15200000,15310000,15110000,15320000,15390000,15040000,15060000,15010000};
 internal static List<SkillSlotsSignal.Slot> Examples(){return ExampleSkills.Select((id,n)=>new SkillSlotsSignal.Slot{Index=n,Skill=id}).ToList();}
 internal SkillSlotsOverlay(EnergyOverlay owner,SkillSlotsSignal signal,bool preview){this.owner=owner;this.signal=signal;this.preview=preview;}
 internal void Configure(SkillSlotsOptions o,bool locked){Options=o.Copy();Options.Normalize();Editing=!locked;signal.Configure(!preview&&Options.Enabled);var view=signal.Read(DpsHistory.UtcNow());if(view!=null)Options.Remember(view.Slots);}
 internal void Test(){test=true;testStart=DpsHistory.UtcNow();Maintain();}
 internal static double LoopSeconds(double elapsed){return Math.Max(0,12-Math.Max(0,elapsed)%13);}
 internal double TestSeconds(double now){return test?LoopSeconds(now-testStart):0;}
 internal void StopTest(){test=false;Maintain();}
 internal bool Testing {get{return test;}}
 internal static string Counter(SkillSlotsSignal.Cooldown cd,double now,bool numbers){double remaining=SkillSlotsSignal.Remaining(cd,now);return cd==null||remaining<=0?null:numbers?Math.Ceiling(remaining).ToString():"CD";}
 internal void Maintain(){Maintain(DpsHistory.UtcNow());}
 internal void Maintain(double now){var v=signal.Read(now);if(v!=null&&!object.ReferenceEquals(v,lastView))Options.Remember(v.Slots);lastView=v;bool sample=test||Options.Enabled&&Editing&&v==null&&Options.KnownSlots.Count>0;bool shell=!preview&&DpsDesign.ShellUiActive();bool show=(Options.Enabled||test)&&(v!=null||sample)&&!shell;NextInterval=1000;if(!show){hideReason=!Options.Enabled&&!test?"disabled":shell?"Windows shell":"waiting for own bar or stale transport";foreach(var w in windows.Values)w.Conceal();StatusKey="hidden/"+hideReason;return;}hideReason=null;Current=sample?(test?(v!=null?v.Slots:Options.KnownSlots.Count>0?Options.EditableSlots(null):Examples()):Options.EditableSlots(null)):v.Slots;
 // Materialise missing placements together. Only the active set is compacted;
 // existing manual positions are never changed by cooldown or icon updates.
 int ordinal=0;foreach(var slot in Current)Options.Ensure(slot.Index,ordinal++);
 var active=new HashSet<int>();int coolCount=0,tickKey=17,labelOrdinal=0;foreach(var slot in Current){int displayOrdinal=labelOrdinal++;var p=Options.Placement(slot.Index,0);if(!p.Visible)continue;active.Add(slot.Index);SkillSlotWindow window;if(!windows.TryGetValue(slot.Index,out window)){windows[slot.Index]=window=new SkillSlotWindow(this,owner,slot.Index);}
 uint id=sample?slot.Skill:SkillSlotsSignal.DisplaySkill(v,slot,now);if(!sample&&SkillSlotsSignal.IsFireContext(slot)&&v.ContextTarget!=0&&v.FireMarkUntil>now&&SkillSlotsSignal.Remaining(SkillSlotsSignal.For(v,slot.Skill),now)>0)NextInterval=50;var cd=sample?null:SkillSlotsSignal.For(v,id);string counter=sample?null:Counter(cd,now,Options.ShowCountdown);bool cooling=sample?test:counter!=null;string text=counter??"";double fill=0;if(sample&&cooling){double seconds=TestSeconds(now);cooling=seconds>0;text=cooling?(Options.ShowCountdown?Math.Ceiling(seconds).ToString():"CD"):"";fill=1-seconds/12;}else if(cooling){double seconds=SkillSlotsSignal.Remaining(cd,now);fill=cd.Peak>0?1-Math.Min(1,seconds*1000/cd.Peak):0;}
 if(cooling){coolCount++;NextInterval=50;}unchecked{tickKey=tickKey*31+text.GetHashCode();}string label=Options.ShowKey?SkillSlotsOptions.Label(p,displayOrdinal):"";window.UpdateSlot(Options.Rect(p),id,label,cooling,fill,text,!sample&&id!=slot.Skill,Editing,Options);}
 if(test)NextInterval=50;foreach(var pair in windows)if(!active.Contains(pair.Key))pair.Value.Conceal();lastRevision=v==null?-1:v.Revision;StatusKey=show+"/"+sample+"/"+lastRevision+"/"+coolCount+"/"+tickKey;
 }
 internal object Status {get{double now=DpsHistory.UtcNow();var v=lastView;return new{enabled=Options.Enabled,editing=Editing,testing=test,hideReason,sourceRevision=lastRevision,visibleSlots=windows.Values.Count(w=>w.Visible),uploads=windows.Values.Sum(w=>w.Uploads),cachedIcons=Painter.Assets.Cached,showCountdown=Options.ShowCountdown,cooldownSource="0238 own cast + 4738 correction, milliseconds",receiver=signal.Diagnostic(now),contextTarget=v==null?(uint?)null:v.ContextTarget,fireMarkRemaining=v==null?(double?)null:Math.Max(0,v.FireMarkUntil-now),slots=v==null?null:v.Slots.Select((s,n)=>{uint id=SkillSlotsSignal.DisplaySkill(v,s,now);var cd=SkillSlotsSignal.For(v,id);return new{number=n+1,label=SkillSlotsOptions.Label(Options.Placement(s.Index,n),n),slot=s.Index,skill=s.Skill,displayedSkill=id,cooldownKnown=cd!=null,remainingMilliseconds=cd==null?(uint?)null:cd.Raw,remainingSeconds=cd==null?(double?)null:Math.Round(SkillSlotsSignal.Remaining(cd,now),3),source=cd==null?null:cd.FromCast?"own cast":"server correction",visible=windows.ContainsKey(s.Index)&&windows[s.Index].Visible};}).ToArray(),timer=false};}}
 internal void Drag(int index,SkillSlotsOptions start,Point delta,bool resize){Options=start.Copy();var p=Options.Placements.Find(v=>v.Slot==index);if(p==null)return;if(resize){if(Options.GroupMode)Options.Scale=SkillSlotsOptions.Clamp(start.Scale+delta.X,40,250);else p.Scale=SkillSlotsOptions.Clamp(p.Scale+delta.X,40,250);}else if(Options.GroupMode){Options.X+=delta.X;Options.Y+=delta.Y;}else{double s=Options.Scale/100.0;var r=Options.Rect(p);r.Offset(delta);if(Options.Snap){var others=Current.Where(v=>v.Index!=index).Select((v,n)=>Options.Rect(Options.Placement(v.Index,n)));r.Location=SkillSlotsOptions.Snapped(r,others,(int)Math.Round(Options.Gap*s),Options.SnapDistance);}p.X=(int)Math.Round((r.X-Options.X)/s);p.Y=(int)Math.Round((r.Y-Options.Y)/s);}Maintain();}
 internal void Commit(){var cfg=owner.Configuration;cfg.SkillSlots=Options.Copy();owner.ApplyConfiguration(cfg);}
 public void Dispose(){foreach(var w in windows.Values)w.Dispose();windows.Clear();Painter.Dispose();}
}
internal sealed class SkillSlotWindow:Form {
 readonly SkillSlotsOverlay service;readonly EnergyOverlay owner;readonly int index;readonly LayeredImage image=new LayeredImage();OverlayGlassLayer glass;string key;bool editing,dragging,resizing;Point startMouse,presented;SkillSlotsOptions start;byte alpha;internal int Uploads {get{return image.Uploads;}}
 internal SkillSlotWindow(SkillSlotsOverlay service,EnergyOverlay owner,int index){this.service=service;this.owner=owner;this.index=index;Text="Aion 2 Helper - Skill "+(index+1);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;}
 internal void UpdateSlot(Rectangle bounds,uint skill,string label,bool cooling,double fill,string countdown,bool chain,bool edit,SkillSlotsOptions o){bool changedStyle=editing!=edit;editing=edit;Bounds=bounds;if(changedStyle||!IsHandleCreated){int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);}string next=Width+"/"+skill+"/"+label+"/"+cooling+"/"+(int)(fill*Width)+"/"+countdown+"/"+chain+"/"+editing+"/"+o.Tint;if(next!=key){using(var b=service.Painter.Render(Width,skill,label,cooling,fill,countdown,chain,editing,o.Tint))image.Upload(b);key=next;alpha=0;}
 byte a=(byte)(o.Opacity*255/100);if(o.Blur){if(glass==null)glass=new OverlayGlassLayer();int pad=Math.Max(1,(int)(Width*.01));glass.Sync(new Rectangle(Left+pad,Top+pad,Width-2*pad,Height-2*pad),a,Handle,true,Width*.10f);}else if(glass!=null){glass.Dispose();glass=null;}
 if(!Visible||alpha!=a||presented!=Location){image.Present(Handle,Left,Top,a);alpha=a;presented=Location;}if(!Visible){Show();OverlayNative.RaiseWithoutFocus(Handle);}}
 internal void Conceal(){if(Visible)Hide();if(glass!=null){glass.Dispose();glass=null;}}
 protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if((m.Msg==0x205||m.Msg==0xa5)&&editing){owner.OpenSkillSlotsSettings();m.Result=IntPtr.Zero;return;}if(m.Msg==0x84){m.Result=new IntPtr(editing?1:-1);return;}if(m.Msg==0x201&&editing){startMouse=Cursor.Position;start=service.Options.Copy();var pt=PointToClient(startMouse);resizing=pt.X>Width-14&&pt.Y>Height-14;dragging=true;Capture=true;m.Result=IntPtr.Zero;return;}if(m.Msg==0x200&&dragging){var mouse=Cursor.Position;service.Drag(index,start,new Point(mouse.X-startMouse.X,mouse.Y-startMouse.Y),resizing);m.Result=IntPtr.Zero;return;}if((m.Msg==0x202||m.Msg==0x215)&&dragging){dragging=false;Capture=false;service.Commit();m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
 protected override void Dispose(bool disposing){if(disposing){image.Dispose();if(glass!=null)glass.Dispose();}base.Dispose(disposing);}
}
