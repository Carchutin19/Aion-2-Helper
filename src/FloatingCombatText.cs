using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class FloatingTextStyle {
 public bool Enabled=true,Up=true;
 public int X=1080,Y=460,Scale=100,FontSize=32,Opacity=100,OutlineWidth=2,Travel=105,Spacing=12,Spread=100;
 public double Duration=1.35;
 public string Color="#F3D69A",OutlineColor="#191B23";
 internal static bool Same(FloatingTextStyle a,FloatingTextStyle b){return a.Enabled==b.Enabled&&a.Up==b.Up&&a.X==b.X&&a.Y==b.Y&&a.Scale==b.Scale&&a.FontSize==b.FontSize&&a.Opacity==b.Opacity&&a.OutlineWidth==b.OutlineWidth&&a.Travel==b.Travel&&a.Spacing==b.Spacing&&a.Spread==b.Spread&&a.Duration==b.Duration&&a.Color==b.Color&&a.OutlineColor==b.OutlineColor;}
 internal FloatingTextStyle Copy(){return (FloatingTextStyle)MemberwiseClone();}
 internal void Normalize(){X=Clamp(X,-100000,100000);Y=Clamp(Y,-100000,100000);Scale=Clamp(Scale,50,200);FontSize=Clamp(FontSize,12,64);Opacity=Clamp(Opacity,0,100);OutlineWidth=Clamp(OutlineWidth,0,6);Travel=Clamp(Travel,0,400);Spacing=Clamp(Spacing,0,40);Spread=Clamp(Spread,0,160);Duration=double.IsNaN(Duration)||double.IsInfinity(Duration)?1.35:Math.Max(.4,Math.Min(6,Duration));if(!EnergyBarOptions.ValidColor(Color))Color="#F3D69A";if(!EnergyBarOptions.ValidColor(OutlineColor))OutlineColor="#191B23";}
 static int Clamp(int x,int lo,int hi){return Math.Max(lo,Math.Min(hi,x));}
 internal static FloatingTextStyle Defaults(int kind){return kind==1?new FloatingTextStyle{X=820,Y=540,Up=false,Travel=75,Color="#EF887F",OutlineColor="#291C25",Duration=1.45}:kind==2?new FloatingTextStyle{X=960,Y=610,Travel=110,Color="#82DFC0",OutlineColor="#132B28",Duration=1.6}:new FloatingTextStyle();}
 internal Rectangle Anchor {get{return new Rectangle(X-80,Y-32,160,64);}}
}
internal sealed class FloatingTextOptions {
 public bool Enabled=false;
 public FloatingTextStyle DamageDealt=FloatingTextStyle.Defaults(0),DamageTaken=FloatingTextStyle.Defaults(1),HealingReceived=FloatingTextStyle.Defaults(2);
 internal FloatingTextStyle Style(int kind){return kind==0?DamageDealt:kind==1?DamageTaken:HealingReceived;}
 internal void Set(int kind,FloatingTextStyle style){if(kind==0)DamageDealt=style;else if(kind==1)DamageTaken=style;else HealingReceived=style;}
 internal bool NeedsCapture {get{return Enabled&&(DamageDealt.Enabled||DamageTaken.Enabled||HealingReceived.Enabled);}}
 internal FloatingTextOptions Copy(){return new FloatingTextOptions{Enabled=Enabled,DamageDealt=DamageDealt.Copy(),DamageTaken=DamageTaken.Copy(),HealingReceived=HealingReceived.Copy()};}
 internal void Normalize(){for(int i=0;i<3;i++){if(Style(i)==null)Set(i,FloatingTextStyle.Defaults(i));Style(i).Normalize();}}
 internal static FloatingTextOptions Read(Dictionary<string,object> cfg){var o=new FloatingTextOptions();object value;if(cfg.TryGetValue("floatingText",out value))try{o=new JavaScriptSerializer().ConvertToType<FloatingTextOptions>(value)??o;}catch(ArgumentException){}catch(InvalidOperationException){}o.Normalize();return o;}
 internal static bool Same(FloatingTextOptions a,FloatingTextOptions b){return new JavaScriptSerializer().Serialize(a)==new JavaScriptSerializer().Serialize(b);}
 internal static string Name(int kind){return kind==0?"Damage dealt":kind==1?"Damage taken":"Healing received";}
}

// Separate from meter accounting/scope. Only exact decoded impacts involving
// our flow-scoped identity are eligible. Never infer damage/healing from HP.
internal sealed class FloatingTextSignal {
 internal sealed class Impact {internal int Kind;internal uint Amount;internal bool Critical;internal double Time;internal long Epoch;}
 sealed class Flow {internal uint Self,Map;internal string Name,IdentitySource;internal double Traffic;internal readonly Dictionary<uint,Parent> Parents=new Dictionary<uint,Parent>();}
 sealed class Parent {internal uint Owner;internal double At;}
 readonly object gate=new object();readonly Dictionary<string,Flow> flows=new Dictionary<string,Flow>();readonly Queue<Impact> pending=new Queue<Impact>();
 internal Func<double> ReceiptClock;
 string active;bool enabled,isolated;readonly bool[] channels=new bool[3];long epoch,accepted,dropped,rejected;internal Action Changed;
 internal bool NeedsIdentity {get{lock(gate)return enabled&&active==null;}}
 static readonly Impact[] Empty=new Impact[0];
 internal long Epoch {get{lock(gate)return epoch;}}
 internal void Configure(FloatingTextOptions options,bool isolated){lock(gate){this.isolated=isolated;bool next=!isolated&&options.NeedsCapture;if(next!=enabled){enabled=next;pending.Clear();epoch++;}if(isolated)Clear();for(int i=0;i<3;i++)channels[i]=options.Style(i).Enabled;}}
 // A shared received bar is valid identity evidence only on its exact flow.
 // Called from the decoder after SkillSignal consumes the same frame, never
 // from a UI timer that could reintroduce pre-reset or pre-map state.
 internal void Adopt(SkillSlotsSignal.View own,string frameFlow,double now){if(own==null||own.Self==0||own.Flow!=frameFlow)return;lock(gate){if(isolated||active!=null)return;Flow f;if(!flows.TryGetValue(own.Flow,out f)){if(flows.Count>=4)return;flows[own.Flow]=f=new Flow();}if(f.Map!=0&&own.Map!=f.Map)return;f.Map=own.Map;f.Self=own.Self;f.IdentitySource="confirmed shared hotbar";f.Traffic=now;active=own.Flow;pending.Clear();epoch++;}}
 void Clear(){flows.Clear();pending.Clear();active=null;epoch++;}
 internal void Reset(){lock(gate)Clear();Notify();}
 void Notify(){var changed=Changed;if(changed!=null)changed();}
 internal Impact[] Drain(double now){lock(gate){if(pending.Count==0)return Empty;var result=new List<Impact>();while(pending.Count>0){var x=pending.Dequeue();if(enabled&&channels[x.Kind]&&x.Epoch==epoch&&now>=x.Time-.25&&now-x.Time<=2)result.Add(x);else dropped++;}return result.ToArray();}}
 internal void Consume(byte[] b,int s,int e,double t,string key){
  if(b==null||s<0||e>b.Length||e-s<2||e-s>65536||string.IsNullOrEmpty(key)||double.IsNaN(t)||double.IsInfinity(t))return;if(ReceiptClock!=null){t=ReceiptClock();if(double.IsNaN(t)||double.IsInfinity(t))return;}bool notify=false;
  lock(gate){if(isolated)return;int op=b[s]*256+b[s+1];Flow f;if(!flows.TryGetValue(key,out f)){if(op!=0x3336&&op!=0x2136)return;if(flows.Count>=4){string oldest=null;double stamp=double.MaxValue;foreach(var pair in flows)if(pair.Key!=active&&pair.Value.Traffic<stamp){oldest=pair.Key;stamp=pair.Value.Traffic;}if(oldest==null)return;flows.Remove(oldest);}flows[key]=f=new Flow();}
   if(t<f.Traffic)return;if(f.Traffic>0&&t-f.Traffic>30){f.Self=0;f.Name=null;f.Parents.Clear();if(active==key){active=null;pending.Clear();epoch++;notify=true;}}f.Traffic=t;
   if(op==0x2136&&e-s==48){uint map=BitConverter.ToUInt32(b,s+6);if(map==0||map>=10000000)return;if(f.Map!=0&&f.Map!=map){f.Self=0;f.Name=null;f.Parents.Clear();if(active==key){active=null;pending.Clear();epoch++;notify=true;}}f.Map=map;}
   else if(op==0x3336){uint actor;string name;byte role;if(DpsSignal.PlayerIdentity(b,s,e,true,out actor,out name,out role)){if(active!=key||f.Self!=actor||f.Name!=name){pending.Clear();epoch++;f.Parents.Clear();}f.Self=actor;f.Name=name;f.IdentitySource="received own profile";active=key;notify=true;}}
   else if(active==key&&f.Self!=0){
    if(op==0x4136){uint actor,parent;if(DpsSignal.EffectParent(b,s,e,out actor,out parent)){if(f.Parents.Count<128||f.Parents.ContainsKey(actor))f.Parents[actor]=new Parent{Owner=parent,At=t};}}
    else if(op==0x4236){int p=s+2;uint actor;if(DpsSignal.Var(b,ref p,e,out actor)&&e-p==2&&b[p]==0){f.Parents.Remove(actor);if(actor==f.Self){f.Self=0;active=null;pending.Clear();epoch++;notify=true;}}}
    else if(enabled&&(op==0x0438||op==0x0538)){
     DpsSignal.Hit hit;bool heal=false,ok;
     if(op==0x0538){heal=DpsSignal.HealingTick(b,s,e,out hit);ok=heal||DpsSignal.Periodic(b,s,e,out hit);}
     else{ok=DpsSignal.Direct(b,s,e,out hit);if(ok)heal=DpsSignal.HealingSkill(hit.Skill);}
     if(!ok){rejected++;return;}if(SkillCatalogue.Shared.IsNonDamage(hit.Skill)&&!heal)return;
     // Unknown direct impact types are not silently labelled damage. Periodic
     // readers already reject unconfirmed effect families and shield capacity.
     if(op==0x0438&&!heal&&!hit.CriticalKnown){rejected++;return;}
     uint source=hit.Source;Parent owner;if(f.Parents.TryGetValue(source,out owner)&&t>=owner.At&&t-owner.At<=120)source=owner.Owner;
     int kind=heal?(hit.Target==f.Self?2:-1):source==f.Self&&hit.Target!=f.Self?0:hit.Target==f.Self&&source!=f.Self?1:-1;
     if(kind>=0&&channels[kind]){if(pending.Count>=128){pending.Dequeue();dropped++;}pending.Enqueue(new Impact{Kind=kind,Amount=hit.Amount,Critical=hit.CriticalKnown&&hit.Critical,Time=t,Epoch=epoch});accepted++;notify=true;}
    }
   }
  }if(notify)Notify();
 }
 internal object Status {get{lock(gate){Flow f;return new{enabled,ownActor=active!=null&&flows.TryGetValue(active,out f)?f.Self:0,identitySource=active!=null&&flows.TryGetValue(active,out f)?f.IdentitySource:null,epoch,pending=pending.Count,accepted,dropped,rejected,source="decoded own impacts; announced healing, not effective healing"};}}}
}

internal static class FloatingTextMotion {
 internal static double Alpha(double age,double duration){if(age<0||age>=duration)return 0;double p=age/duration;return Math.Min(1,p/.09)*Math.Min(1,(1-p)/.34);}
 internal static float Offset(double age,FloatingTextStyle o){double p=Math.Max(0,Math.Min(1,age/o.Duration));return (o.Up?-1:1)*(float)(o.Travel*o.Scale/100.0*p);}
 internal static string Number(uint amount,int kind,bool critical){string n=amount.ToString("N0",CultureInfo.InvariantCulture).Replace(',',' ');return (kind==1?"−":kind==2?"+":"")+n+(critical?"!!":"");}
 internal static RectangleF Separate(RectangleF desired,IList<RectangleF> occupied,bool up,float gap){for(int pass=0;pass<=occupied.Count;pass++){bool moved=false;foreach(var other in occupied){var padded=other;padded.Inflate(gap,gap);if(!padded.IntersectsWith(desired))continue;desired.Y=up?padded.Bottom: padded.Top-desired.Height;moved=true;}if(!moved)return desired;}return desired;}
 internal static Bitmap Glyph(string value,FloatingTextStyle o){
  float size=o.FontSize*o.Scale/100f,edge=o.OutlineWidth*o.Scale/100f;using(var path=new GraphicsPath()){
   FontFamily family;try{family=new FontFamily("Bahnschrift");}catch(ArgumentException){family=new FontFamily("Segoe UI");}
   using(family)using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()){format.FormatFlags|=StringFormatFlags.NoWrap;path.AddString(value,family,(int)(family.IsStyleAvailable(FontStyle.Bold)?FontStyle.Bold:FontStyle.Regular),size,PointF.Empty,format);}
   var box=path.GetBounds();int pad=(int)Math.Ceiling(edge+5);using(var move=new Matrix()){move.Translate(pad-box.Left,pad-box.Top);path.Transform(move);}var image=new Bitmap(Math.Max(1,(int)Math.Ceiling(box.Width)+pad*2),Math.Max(1,(int)Math.Ceiling(box.Height)+pad*2),PixelFormat.Format32bppPArgb);
   using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;if(edge>0)using(var pen=new Pen(ColorTranslator.FromHtml(o.OutlineColor),edge*2)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,path);}using(var brush=new SolidBrush(ColorTranslator.FromHtml(o.Color)))g.FillPath(brush,path);}return image;
  }
 }
 internal static void Sprite(Graphics g,Bitmap glyph,float x,float y,double opacity,ImageAttributes attributes,ColorMatrix matrix){if(opacity<=0)return;matrix.Matrix33=(float)Math.Max(0,Math.Min(1,opacity));attributes.SetColorMatrix(matrix);g.DrawImage(glyph,new[]{new PointF(x,y),new PointF(x+glyph.Width,y),new PointF(x,y+glyph.Height)},new RectangleF(0,0,glyph.Width,glyph.Height),GraphicsUnit.Pixel,attributes);}
}


// A persistent top-down premultiplied DIB is painted directly. Presentation
// reads only the tight text rectangle: no per-frame GetHbitmap or full-screen
// bitmap conversion, and no per-frame native bitmap/DC allocation.
internal sealed class FloatingTextSurface:IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct Info {internal uint Size;internal int Width,Height;internal ushort Planes,Bits;internal uint Compression,ImageSize;internal int X,Y;internal uint Used,Important;}
 [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref Info info,uint usage,out IntPtr pixels,IntPtr section,uint offset);
 [DllImport("gdi32.dll")]static extern bool PatBlt(IntPtr dc,int x,int y,int width,int height,uint operation);
 [DllImport("gdi32.dll")]static extern bool GdiFlush();
 internal readonly IntPtr dc;IntPtr bitmap,previous;internal readonly Bitmap Pixels;internal readonly Graphics Draw;bool disposed;
 internal FloatingTextSurface(int width,int height){dc=OverlayNative.CreateCompatibleDC(IntPtr.Zero);if(dc==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();IntPtr bits;var info=new Info{Size=40,Width=width,Height=-height,Planes=1,Bits=32};bitmap=CreateDIBSection(dc,ref info,0,out bits,IntPtr.Zero,0);if(bitmap==IntPtr.Zero){OverlayNative.DeleteDC(dc);throw new System.ComponentModel.Win32Exception();}previous=OverlayNative.SelectObject(dc,bitmap);try{Pixels=new Bitmap(width,height,width*4,PixelFormat.Format32bppPArgb,bits);Draw=Graphics.FromImage(Pixels);Draw.Clear(Color.Transparent);Draw.CompositingMode=CompositingMode.SourceOver;Draw.InterpolationMode=InterpolationMode.HighQualityBilinear;Draw.PixelOffsetMode=PixelOffsetMode.Half;}catch{if(Pixels!=null)Pixels.Dispose();OverlayNative.SelectObject(dc,previous);OverlayNative.DeleteObject(bitmap);OverlayNative.DeleteDC(dc);throw;}}
 internal void Clear(Rectangle dirty){if(!dirty.IsEmpty)PatBlt(dc,dirty.X,dirty.Y,dirty.Width,dirty.Height,0x00000042);}
 internal void FlushNative(){GdiFlush();}
 internal void Present(IntPtr window,Rectangle crop,Point location){Draw.Flush(FlushIntention.Sync);GdiFlush();var pos=new OverlayNative.Point(location.X+crop.X,location.Y+crop.Y);var size=new OverlayNative.Size(crop.Width,crop.Height);var origin=new OverlayNative.Point(crop.X,crop.Y);var blend=new OverlayNative.Blend{Alpha=255,Format=1};if(!OverlayNative.UpdateLayeredWindow(window,IntPtr.Zero,ref pos,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
 public void Dispose(){if(disposed)return;disposed=true;Draw.Dispose();Pixels.Dispose();OverlayNative.SelectObject(dc,previous);OverlayNative.DeleteObject(bitmap);OverlayNative.DeleteDC(dc);}
}

internal sealed class FloatingGlyphPainter:IDisposable {
 readonly FontFamily family;readonly StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone();readonly GraphicsPath path=new GraphicsPath();readonly Matrix move=new Matrix();SolidBrush fill;Pen edge;FloatingTextStyle style;internal int Rasterizations;
 internal FloatingGlyphPainter(){try{family=new FontFamily("Bahnschrift");}catch(ArgumentException){family=new FontFamily("Segoe UI");}format.FormatFlags|=StringFormatFlags.NoWrap;}
 internal void Configure(FloatingTextStyle next){if(style!=null&&FloatingTextStyle.Same(style,next))return;style=next.Copy();if(fill!=null)fill.Dispose();if(edge!=null)edge.Dispose();fill=new SolidBrush(ColorTranslator.FromHtml(next.Color));edge=next.OutlineWidth==0?null:new Pen(ColorTranslator.FromHtml(next.OutlineColor),next.OutlineWidth*next.Scale/50f){LineJoin=LineJoin.Round};}
 internal Bitmap Render(string text){Rasterizations++;path.Reset();path.AddString(text,family,(int)(family.IsStyleAvailable(FontStyle.Bold)?FontStyle.Bold:FontStyle.Regular),style.FontSize*style.Scale/100f,PointF.Empty,format);var box=path.GetBounds();int pad=(int)Math.Ceiling(style.OutlineWidth*style.Scale/100f+5);move.Reset();move.Translate(pad-box.Left,pad-box.Top);path.Transform(move);var b=new Bitmap(Math.Max(1,(int)Math.Ceiling(box.Width)+pad*2),Math.Max(1,(int)Math.Ceiling(box.Height)+pad*2),PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;if(edge!=null)g.DrawPath(edge,path);g.FillPath(fill,path);}return b;}
 public void Dispose(){if(fill!=null)fill.Dispose();if(edge!=null)edge.Dispose();family.Dispose();format.Dispose();path.Dispose();move.Dispose();}
}

// Four lazily cached quarter-pixel phases keep slow motion smooth while the
// hot path is a native premultiplied-alpha blend. Native handles are bounded
// by the visible-entry cap and are owned/disposed by the individual impact.
internal sealed class FloatingNativeGlyph:IDisposable {
 [DllImport("msimg32.dll",SetLastError=true)]static extern bool AlphaBlend(IntPtr target,int x,int y,int width,int height,IntPtr source,int sx,int sy,int sw,int sh,OverlayNative.Blend blend);
 sealed class Tile:IDisposable {internal IntPtr Dc,Bitmap,Previous;internal Tile(Bitmap source,float phase){Dc=OverlayNative.CreateCompatibleDC(IntPtr.Zero);try{if(phase==0)Bitmap=source.GetHbitmap(Color.FromArgb(0));else using(var shifted=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(shifted)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(source,new RectangleF(0,phase,source.Width,source.Height),new RectangleF(0,0,source.Width,source.Height),GraphicsUnit.Pixel);}Bitmap=shifted.GetHbitmap(Color.FromArgb(0));}Previous=OverlayNative.SelectObject(Dc,Bitmap);}catch{if(Bitmap!=IntPtr.Zero)OverlayNative.DeleteObject(Bitmap);OverlayNative.DeleteDC(Dc);throw;}}public void Dispose(){OverlayNative.SelectObject(Dc,Previous);OverlayNative.DeleteObject(Bitmap);OverlayNative.DeleteDC(Dc);}}
 readonly Bitmap source;readonly Tile[] tiles=new Tile[4];internal int Width {get{return source.Width;}}internal int Height {get{return source.Height;}}
 internal FloatingNativeGlyph(Bitmap source){this.source=source;}
 internal void Draw(FloatingTextSurface surface,float x,float y,double opacity){int top=(int)Math.Floor(y),phase=(int)Math.Round((y-top)*4);if(phase==4){top++;phase=0;}if(tiles[phase]==null)tiles[phase]=new Tile(source,phase*.25f);var blend=new OverlayNative.Blend{Alpha=(byte)Math.Max(0,Math.Min(255,Math.Round(opacity*255))),Format=1};if(!AlphaBlend(surface.dc,(int)Math.Round(x),top,Width,Height,tiles[phase].Dc,0,0,Width,Height,blend))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
 public void Dispose(){foreach(var tile in tiles)if(tile!=null)tile.Dispose();source.Dispose();}
}

internal sealed class FloatingTextWindow:Form {
 internal sealed class Entry:IDisposable {internal FloatingNativeGlyph Glyph;internal double At;internal float X,Y;internal RectangleF Rect(double now,FloatingTextStyle style){return new RectangleF(X,Y+FloatingTextMotion.Offset(now-At,style),Glyph.Width,Glyph.Height);}public void Dispose(){Glyph.Dispose();}}
 readonly EnergyOverlay owner;readonly int kind;readonly bool isolated;readonly List<Entry> entries=new List<Entry>(16);readonly FloatingGlyphPainter glyphs=new FloatingGlyphPainter();readonly List<RectangleF> occupied=new List<RectangleF>(16);
 FloatingTextStyle style=FloatingTextStyle.Defaults(0);FloatingTextSurface surface;Rectangle canvas,dirtyRect;bool editing,dirty,dragging,disposed;int hit,startScale,serial,allocations,presentations,visualDrops;long presentedPixels;Point startMouse;FloatingTextStyle startStyle;Bitmap editGlyph;float trail;
 internal FloatingTextWindow(EnergyOverlay owner,int kind,bool isolated){this.owner=owner;this.kind=kind;this.isolated=isolated;Text="Aion 2 Helper - "+FloatingTextOptions.Name(kind);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;}
 internal bool HasEntries {get{return entries.Count>0;}}
 internal long CanvasPixels {get{return (long)canvas.Width*canvas.Height;}}
 internal void Configure(FloatingTextStyle next,bool locked){bool changed=!FloatingTextStyle.Same(style,next);if(changed)Clear();style=next.Copy();dirty|=editing==locked||changed;editing=!locked;glyphs.Configure(style);float scale=style.Scale/100f;int width=Math.Min(1200,Math.Max(480,(int)(style.FontSize*scale*20+100)));trail=Math.Max(240,Math.Min(800,(style.FontSize*scale+style.OutlineWidth*scale*2+style.Spacing+12)*6));int leading=(int)(style.Travel*scale)+100,height=leading+(int)trail+100;var nextCanvas=new Rectangle(style.X-width/2,style.Y-(style.Up?leading:(int)trail+100),width,height);if(canvas.Size!=nextCanvas.Size)ReleaseSurface();canvas=nextCanvas;if(editGlyph!=null&&changed){editGlyph.Dispose();editGlyph=null;}if(editing)Bounds=canvas;int flags=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?flags&~0x20:flags|0x20);}
 void Expire(double now){for(int n=entries.Count-1;n>=0;n--)if(now-entries[n].At>=style.Duration){entries[n].Dispose();entries.RemoveAt(n);dirty=true;}}
 void RemoveOldest(){entries[0].Dispose();entries.RemoveAt(0);visualDrops++;}
 internal void Add(uint amount,bool critical,double at){Expire(at);var glyph=new FloatingNativeGlyph(glyphs.Render(FloatingTextMotion.Number(amount,kind,critical)));while(entries.Count>=16)RemoveOldest();RectangleF placed;for(;;){occupied.Clear();foreach(var item in entries)occupied.Add(item.Rect(at,style));int columns=Math.Max(1,Math.Min(3,(canvas.Width-40)/(glyph.Width+style.Spacing)));float pitch=Math.Min((glyph.Width+style.Spacing+2)*style.Spread/100f,Math.Max(0,(canvas.Width-glyph.Width)/2f-20));placed=RectangleF.Empty;float best=float.MaxValue;for(int candidate=0;candidate<columns;candidate++){int slot=(serial+candidate)%columns;float lane=columns==3?(slot==0?0:slot==1?-pitch:pitch):columns==2?(slot==0?-pitch/2:pitch/2):0;var desired=new RectangleF(canvas.Width/2f-glyph.Width/2f+lane,style.Y-canvas.Top-glyph.Height/2f,glyph.Width,glyph.Height);var p=FloatingTextMotion.Separate(desired,occupied,style.Up,Math.Max(.5f,style.Spacing));float shift=Math.Abs(p.Y-desired.Y);if(p.Top>=2&&p.Bottom<=canvas.Height-2&&shift<best){placed=p;best=shift;}}if(!placed.IsEmpty||entries.Count==0)break;RemoveOldest();}if(placed.IsEmpty){glyph.Dispose();visualDrops++;return;}serial++;entries.Add(new Entry{Glyph=glyph,At=at,X=placed.X,Y=placed.Y});dirty=true;}
 internal void Clear(){foreach(var x in entries)x.Dispose();entries.Clear();dirty=true;if(Visible)Hide();}
 internal void ReleaseSurface(){if(surface!=null){surface.Dispose();surface=null;}dirtyRect=Rectangle.Empty;}
 internal void Render(double now,bool show,bool testing){Expire(now);if(!show||entries.Count==0&&!editing){if(Visible)Hide();return;}if(!dirty&&entries.Count==0)return;dirty=false;if(surface==null){surface=new FloatingTextSurface(canvas.Width,canvas.Height);allocations++;}var g=surface.Draw;surface.Clear(dirtyRect);RectangleF union=RectangleF.Empty;
  foreach(var x in entries){double age=now-x.At;var pos=x.Rect(now,style);float alpha=(float)(FloatingTextMotion.Alpha(age,style.Duration)*style.Opacity/100);if(alpha<=0)continue;union=union.IsEmpty?pos:RectangleF.Union(union,pos);x.Glyph.Draw(surface,pos.X,pos.Y,alpha);}
  if(editing){surface.FlushNative();var anchor=new Rectangle(canvas.Width/2-95,style.Y-canvas.Top-35,190,70);using(var fill=new SolidBrush(Color.FromArgb(25,15,19,26)))g.FillRectangle(fill,anchor);using(var pen=new Pen(Color.FromArgb(160,ColorTranslator.FromHtml(style.Color)))){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,anchor);}using(var font=new Font("Segoe UI",9))using(var brush=new SolidBrush(Color.FromArgb(230,ColorTranslator.FromHtml(style.Color))))g.DrawString(UiLanguage.Text(FloatingTextOptions.Name(kind),owner.Language),font,brush,anchor.Left+8,anchor.Top+3);if(entries.Count==0){if(editGlyph==null)editGlyph=glyphs.Render(FloatingTextMotion.Number(kind==0?12480u:kind==1?2180u:4360u,kind,false));g.DrawImageUnscaled(editGlyph,canvas.Width/2-editGlyph.Width/2,style.Y-canvas.Top-editGlyph.Height/2+7);}using(var pen=new Pen(Color.FromArgb(190,Color.White),2))g.DrawLine(pen,anchor.Right-13,anchor.Bottom-4,anchor.Right-4,anchor.Bottom-13);union=new RectangleF(0,0,canvas.Width,canvas.Height);}
  if(union.IsEmpty){dirtyRect=Rectangle.Empty;if(Visible)Hide();return;}union.Inflate(2,2);var crop=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(union.Left),(int)Math.Floor(union.Top),(int)Math.Ceiling(union.Right),(int)Math.Ceiling(union.Bottom)),new Rectangle(Point.Empty,canvas.Size));dirtyRect=crop;if(crop.IsEmpty)return;surface.Present(Handle,crop,canvas.Location);presentations++;presentedPixels+=(long)crop.Width*crop.Height;if(!Visible&&!isolated){Show();OverlayNative.RaiseWithoutFocus(Handle);}
 }
 internal object Status {get{return new{visible=Visible,editing,active=entries.Count,uploads=allocations,surfaceAllocations=allocations,presentations,presentedPixels,glyphRasterizations=glyphs.Rasterizations,visualDrops,x=style.X,y=style.Y};}}
 internal RectangleF[] Positions(double now){var result=new RectangleF[entries.Count];for(int i=0;i<result.Length;i++)result[i]=entries[i].Rect(now,style);return result;}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle=(p.ExStyle|0x80000|0x80|0x08000000|0x20)&~0x40000;return p;}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if(m.Msg==0x84){var mouse=Cursor.Position;var anchor=new Rectangle(style.X-95,style.Y-35,190,70);m.Result=new IntPtr(!editing||!anchor.Contains(mouse)?-1:mouse.X>anchor.Right-20&&mouse.Y>anchor.Bottom-20?17:2);return;}if((m.Msg==0xa1||m.Msg==0x201)&&editing){hit=m.Msg==0xa1?m.WParam.ToInt32():2;startMouse=Cursor.Position;startStyle=style.Copy();startScale=style.Scale;dragging=true;Capture=true;m.Result=IntPtr.Zero;return;}if(m.Msg==0x200&&dragging){var next=startStyle.Copy();int dx=Cursor.Position.X-startMouse.X,dy=Cursor.Position.Y-startMouse.Y;if(hit==2){next.X+=dx;next.Y+=dy;}else next.Scale=Math.Max(50,Math.Min(200,startScale+(int)Math.Round((dx+dy)*.35)));Configure(next,false);Render(0,true,false);m.Result=IntPtr.Zero;return;}if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;var cfg=owner.Configuration;cfg.FloatingText.Set(kind,style.Copy());owner.ApplyConfiguration(cfg);m.Result=IntPtr.Zero;return;}base.WndProc(ref m);}
 protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;Clear();ReleaseSurface();if(editGlyph!=null)editGlyph.Dispose();glyphs.Dispose();}base.Dispose(disposing);}
}

// Per-timer high-resolution pacing where supported, with one pending UI delivery.
// No global timer-resolution request or spinning; fully cancelled while idle.
internal sealed class FloatingFramePump:IDisposable {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWaitableTimerExW(IntPtr attributes,string name,uint flags,uint access);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetWaitableTimer(IntPtr timer,ref long due,int period,IntPtr callback,IntPtr argument,bool resume);
 [DllImport("kernel32.dll")]static extern bool CancelWaitableTimer(IntPtr timer);
 sealed class NativeWait:System.Threading.WaitHandle {internal NativeWait(IntPtr h){SafeWaitHandle=new Microsoft.Win32.SafeHandles.SafeWaitHandle(h,true);}}
 readonly Control dispatcher;readonly Action tick,delivery;readonly System.Threading.AutoResetEvent wake=new System.Threading.AutoResetEvent(false);System.Threading.Thread worker;volatile bool enabled,disposed;int queued;internal string Mode="idle";
 internal bool Enabled {get{return enabled;}}
 internal FloatingFramePump(Control dispatcher,Action tick){this.dispatcher=dispatcher;this.tick=tick;delivery=Deliver;}
 void Work(){NativeWait scheduled=null;try{IntPtr handle=IntPtr.Zero;try{handle=CreateWaitableTimerExW(IntPtr.Zero,null,2,0x00100002);Mode="high-resolution waitable timer";if(handle==IntPtr.Zero){handle=CreateWaitableTimerExW(IntPtr.Zero,null,0,0x00100002);Mode="waitable timer fallback";}}catch(EntryPointNotFoundException){}if(handle!=IntPtr.Zero)scheduled=new NativeWait(handle);else Mode="event timeout fallback";System.Threading.WaitHandle[] waits=scheduled==null?null:new System.Threading.WaitHandle[]{wake,scheduled};
  while(!disposed){if(!enabled){wake.WaitOne();continue;}long due=-10000;bool armed=scheduled!=null&&SetWaitableTimer(handle,ref due,16,IntPtr.Zero,IntPtr.Zero,false);while(enabled&&!disposed){if(armed){if(System.Threading.WaitHandle.WaitAny(waits)==0)break;}else if(wake.WaitOne(16))break;if(enabled&&!disposed)Queue();}if(armed)CancelWaitableTimer(handle);}
 }finally{if(scheduled!=null)scheduled.Dispose();wake.Dispose();}}
 void Queue(){if(!enabled||disposed||System.Threading.Interlocked.CompareExchange(ref queued,1,0)!=0)return;try{dispatcher.BeginInvoke(delivery);}catch(InvalidOperationException){System.Threading.Interlocked.Exchange(ref queued,0);}}
 void Deliver(){try{if(enabled&&!disposed)tick();}finally{System.Threading.Interlocked.Exchange(ref queued,0);}}
 internal void Start(){if(enabled||disposed)return;enabled=true;if(worker==null){worker=new System.Threading.Thread(Work){IsBackground=true,Name="Floating text frames"};worker.Start();}wake.Set();}
 internal void Stop(){if(!enabled||disposed)return;enabled=false;wake.Set();}
 public void Dispose(){if(disposed)return;enabled=false;disposed=true;if(worker==null)wake.Dispose();else{wake.Set();worker.Join(200);}}
}

internal sealed class FloatingTextService:IDisposable {
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 readonly FloatingTextWindow[] windows=new FloatingTextWindow[3];readonly FloatingTextSignal signal;readonly Stopwatch clock=Stopwatch.StartNew();readonly FloatingFramePump timer;readonly bool isolated;FloatingTextOptions options=new FloatingTextOptions();bool locked=true,disposed;long epoch;int test=-1;double nextTest;
 internal FloatingTextService(EnergyOverlay owner,FloatingTextSignal signal,bool isolated){this.signal=signal;this.isolated=isolated;for(int i=0;i<3;i++)windows[i]=new FloatingTextWindow(owner,i,isolated);timer=new FloatingFramePump(windows[0],delegate{Maintain(true);});}
 internal bool Testing {get{return test>=0;}}
 internal void Configure(FloatingTextOptions next,bool value){options=next.Copy();options.Normalize();locked=value;signal.Configure(options,isolated);for(int i=0;i<3;i++){windows[i].Configure(options.Style(i),locked);if(!options.Enabled||!options.Style(i).Enabled)windows[i].ReleaseSurface();}Maintain();}
 internal void ToggleTest(int kind){if(test==kind){StopTest();return;}StopTest();test=kind;nextTest=0;Maintain();}
 internal void StopTest(){test=-1;for(int i=0;i<3;i++)windows[i].Clear();Maintain();}
 static uint foregroundPid;static bool foregroundGame;static double foregroundChecked;
 internal static bool GameForeground(){uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);double now=DpsHistory.UtcNow();if(pid==foregroundPid&&now-foregroundChecked<2)return foregroundGame;foregroundPid=pid;foregroundChecked=now;foregroundGame=false;try{using(var p=Process.GetProcessById((int)pid))return foregroundGame=string.Equals(p.ProcessName,"AION2",StringComparison.OrdinalIgnoreCase);}catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(System.ComponentModel.Win32Exception){return false;}}
 double nextForeground;bool foreground;
 internal void Maintain(){Maintain(false);}
 void Maintain(bool drawFrame){if(disposed)return;if(!options.NeedsCapture&&test<0){timer.Stop();for(int i=0;i<3;i++)windows[i].Clear();signal.Drain(DpsHistory.UtcNow());return;}double utc=DpsHistory.UtcNow(),now=clock.Elapsed.TotalSeconds;long current=signal.Epoch;if(epoch!=current){epoch=current;for(int i=0;i<3;i++)windows[i].Clear();}var impacts=signal.Drain(utc);bool existing=false;for(int i=0;i<3;i++)existing|=windows[i].HasEntries;if(test<0&&locked&&impacts.Length==0&&!existing){timer.Stop();return;}if(now>=nextForeground){foreground=!isolated&&GameForeground();nextForeground=now+.15;}
  foreach(var x in impacts)if(foreground&&test<0&&options.Enabled&&options.Style(x.Kind).Enabled)windows[x.Kind].Add(x.Amount,x.Critical,now-Math.Min(.15,Math.Max(0,utc-x.Time)));
  if(test>=0&&now>=nextTest){windows[test].Add(test==0?12480u:test==1?2180u:4360u,((int)(now*10)%2)==0,now);nextTest=now+.48;}
  bool animate=test>=0;for(int i=0;i<3;i++)animate|=windows[i].HasEntries;if(animate&&!drawFrame){if(!timer.Enabled)timer.Start();return;}animate=test>=0;for(int i=0;i<3;i++){bool show=test==i||options.Enabled&&options.Style(i).Enabled&&(!locked||foreground);if(!show)windows[i].Clear();windows[i].Render(now,show,test==i);animate|=show&&windows[i].HasEntries;}if(animate){if(!timer.Enabled)timer.Start();}else timer.Stop();
 }
 internal object Status {get{return new{enabled=options.Enabled,testing=Testing,testCategory=test,timer=timer.Enabled,pacing=timer.Mode,signal=signal.Status,damageDealt=windows[0].Status,damageTaken=windows[1].Status,healingReceived=windows[2].Status};}}
 public void Dispose(){if(disposed)return;disposed=true;timer.Stop();timer.Dispose();foreach(var w in windows)w.Dispose();}
}
