using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal sealed class StaggerScreenOptions {
 public bool Enabled=false;
 public string TextColor="#FFE3AC",EffectColor="#EF865F";
 internal StaggerScreenOptions Copy(){return (StaggerScreenOptions)MemberwiseClone();}
 internal void Normalize(){if(!EnergyBarOptions.ValidColor(TextColor))TextColor="#FFE3AC";if(!EnergyBarOptions.ValidColor(EffectColor))EffectColor="#EF865F";}
}

// A zero received on a selected, positively observed gauge creates an event in
// StaggerSignal. This gate never turns missing data or an estimated refill into
// a stagger, and never replays the current event when the option is enabled.
internal sealed class StaggerScreenGate {
 bool armed;long seen;
 internal void Configure(bool value,StaggerSignal.Sample sample){if(armed!=value){armed=value;if(sample!=null)seen=Math.Max(seen,sample.BurstSerial);}}
 internal bool Take(StaggerSignal.Sample sample,double utc){
  if(sample==null||sample.BurstSerial<=seen)return false;seen=sample.BurstSerial;
  return armed&&sample.ConfirmedTarget&&sample.Current==0&&StaggerSignal.DisplayAvailable(sample,utc)&&utc-sample.BurstAt>=-.25&&utc-sample.BurstAt<=.75;
 }
}

// Procedural screen-space artwork. No frames or game assets from the reference
// video are shipped: broken shock rings, streaks, flare and glyphs are drawn.
internal sealed class StaggerScreenPainter:IDisposable {
 internal const double Duration=1.18;
 readonly GraphicsPath text=new GraphicsPath();readonly ImageAttributes attributes=new ImageAttributes();
 readonly ColorMatrix matrix=new ColorMatrix();readonly Bitmap halo,flare;readonly RectangleF textBounds;
 Bitmap scene;internal int Paints;bool released;
 internal StaggerScreenPainter(){
  FontFamily font;try{font=new FontFamily("Candara Light");}catch(ArgumentException){font=new FontFamily("Segoe UI");}
  using(font)using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()){text.AddString("Staggered",font,(int)FontStyle.Regular,100,PointF.Empty,format);}
  textBounds=text.GetBounds();using(var shift=new Matrix()){shift.Translate(-textBounds.Left-textBounds.Width/2,-textBounds.Top-textBounds.Height/2);text.Transform(shift);}
  halo=Texture(256,256,false);flare=Texture(768,96,true);
 }
 static Bitmap Texture(int width,int height,bool line){
  var image=new Bitmap(width,height,PixelFormat.Format32bppPArgb);var pixels=new byte[width*height*4];
  for(int y=0;y<height;y++)for(int x=0;x<width;x++){
   double dx=(x+.5-width/2.0)/(width/2.0),dy=(y+.5-height/2.0)/(height/2.0);
   double a=line?Math.Pow(Math.Max(0,1-Math.Abs(dx)),.55)*Math.Exp(-dy*dy*6):Math.Pow(Math.Max(0,1-Math.Sqrt(dx*dx+dy*dy)),2.2);
   int p=(y*width+x)*4;byte alpha=(byte)Math.Round(Math.Min(1,a)*255);pixels[p]=pixels[p+1]=pixels[p+2]=pixels[p+3]=alpha;
  }
  var data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{image.UnlockBits(data);}return image;
 }
 static float Clamp(double value){return (float)Math.Max(0,Math.Min(1,value));}
 static float Ease(double value){float x=Clamp(value);return 1-(1-x)*(1-x)*(1-x);}
 static Color Mix(Color a,Color b,float amount){return Color.FromArgb((int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));}
 static Color Alpha(Color color,double alpha){return Color.FromArgb((int)Math.Round(255*Clamp(alpha)),color);}
 void Sprite(Graphics g,Bitmap image,RectangleF bounds,Color tint,double opacity){if(opacity<=.001)return;matrix.Matrix00=tint.R/255f;matrix.Matrix11=tint.G/255f;matrix.Matrix22=tint.B/255f;matrix.Matrix33=Clamp(opacity);attributes.SetColorMatrix(matrix);g.DrawImage(image,new[]{new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom)},new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel,attributes);}
 static PointF Polar(float cx,float cy,float radius,double angle){double rad=angle*Math.PI/180;return new PointF(cx+radius*(float)Math.Cos(rad),cy+radius*(float)Math.Sin(rad));}
 void Ring(Graphics g,float cx,float cy,float radius,float unit,float angle,double opacity,Color color){
  var bounds=new RectangleF(cx-radius,cy-radius,radius*2,radius*2);
  // Deliberately uneven cuts and tapered fragments rather than a dashed circle.
  float[] starts={-8,28,83,134,188,231,287,328},lengths={18,37,17,34,20,39,23,13};
  for(int i=0;i<starts.Length;i++)for(int part=0;part<8;part++){
   float taper=(float)Math.Sin(Math.PI*(part+.5)/8),start=starts[i]+angle+lengths[i]*part/8;
   using(var glow=new Pen(Alpha(color,opacity*.13*taper),unit*(12+30*taper)))g.DrawArc(glow,bounds,start,lengths[i]/8+.3f);
   using(var core=new Pen(Alpha(Mix(color,Color.FromArgb(255,241,206),.72f),opacity*.88*taper),unit*(2+6*taper)))g.DrawArc(core,bounds,start,lengths[i]/8+.25f);
  }
 }
 internal void Paint(Bitmap frame,double seconds,StaggerScreenOptions options){
  if(released)throw new ObjectDisposedException("StaggerScreenPainter");Paints++;
  // Soft full-screen layers use a bounded surface; glyphs stay native-sized.
  // This avoids rasterizing enormous anti-aliased arcs on high-DPI displays.
  double scale=Math.Min(1,Math.Min(600.0/frame.Height,2560.0/frame.Width));
  if(scale>=1){PaintScene(frame,seconds,options,true);return;}
  int width=Math.Max(1,(int)Math.Round(frame.Width*scale)),height=Math.Max(1,(int)Math.Round(frame.Height*scale));
  if(scene==null||scene.Width!=width||scene.Height!=height){if(scene!=null)scene.Dispose();scene=new Bitmap(width,height,PixelFormat.Format32bppPArgb);}
  PaintScene(scene,seconds,options,false);
  using(var g=Graphics.FromImage(frame)){
   g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(scene,new Rectangle(0,0,frame.Width,frame.Height));
   if(seconds>=0&&seconds<Duration){g.CompositingMode=CompositingMode.SourceOver;g.SmoothingMode=SmoothingMode.AntiAlias;DrawText(g,frame.Width,frame.Height,seconds,options);}
  }
 }
 void PaintScene(Bitmap frame,double seconds,StaggerScreenOptions options,bool drawText){
  using(var g=Graphics.FromImage(frame)){
   g.CompositingMode=CompositingMode.SourceCopy;g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceOver;
   if(seconds<0||seconds>=Duration)return;
   g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.InterpolationMode=InterpolationMode.HighQualityBilinear;
   float w=frame.Width,h=frame.Height,cx=w/2,cy=h/2,unit=h/1080f;
   Color color=ColorTranslator.FromHtml(options.EffectColor),hot=Mix(color,Color.FromArgb(255,244,211),.83f);
   double intro=Clamp(seconds/.045),tail=1-Clamp((seconds-.82)/.36),flash=intro*Math.Exp(-Math.Max(0,seconds-.11)*12),ring=intro*(1-Clamp((seconds-.17)/.22));
   // Soft mist and the long coral horizontal lens flare are independently faded.
   Sprite(g,halo,new RectangleF(cx-h*.8f,cy-h*.8f,h*1.6f,h*1.6f),hot,flash*.78);
   float band=h*(.018f+.09f*Clamp(flash));double flareAlpha=(flash*.82+.10*tail)*intro;
   Sprite(g,flare,new RectangleF(0,cy-band,w,band*2),color,flareAlpha);
   Sprite(g,flare,new RectangleF(0,cy-h*.006f,w,h*.012f),hot,flash*.86);
   // Radial glints accelerate outwards with the opening shock wave.
   if(ring>.001){
    float radius=h*(.28f+.85f*Clamp(seconds/.28));
    Ring(g,cx,cy,radius,unit,(float)(seconds*35),ring,color);
    using(var ghost=new Pen(Alpha(hot,ring*.17),Math.Max(.5f,unit)))g.DrawArc(ghost,cx-radius*1.10f,cy-radius*1.10f,radius*2.20f,radius*2.20f,22+(float)seconds*12,302);
    for(int i=0;i<28;i++){
     double angle=i*137.508+11;float progress=(float)((i*17%29)/29.0),a=radius*(.28f+progress*.35f),b=a+h*(.12f+(i%5)*.035f);
     PointF begin=Polar(cx,cy,a,angle),end=Polar(cx,cy,b,angle),side=Polar(cx,cy,a,angle+.50);
     using(var brush=new SolidBrush(Alpha(i%4==0?color:hot,ring*(.14+(i%4)*.05))))g.FillPolygon(brush,new[]{begin,end,side});
     using(var ray=new Pen(Alpha(hot,ring*.22),unit*(i%4==0?3:1.4f)))g.DrawLine(ray,begin,end);
    }
   }
   // Smaller residual motes disperse after the large opening ring has gone.
   for(int i=0;i<22;i++){
    double angle=i*137.508;float travel=(float)seconds,dist=h*(.04f+travel*(.16f+(i%5)*.035f));PointF point=Polar(cx,cy,dist,angle);double alpha=intro*tail*(1-Clamp((seconds-.14)/.85))*(.16+(i%4)*.04);float size=unit*(1.2f+(i%3));
    using(var mote=new SolidBrush(Alpha(hot,alpha)))g.FillEllipse(mote,point.X-size/2,point.Y-size/2,size,size);
   }
   float textWidth=h*.30f*TextScale(seconds);double textAlpha=intro*tail;
   Sprite(g,halo,new RectangleF(cx-textWidth*.85f,cy-h*.20f,textWidth*1.7f,h*.40f),color,textAlpha*.23);
   if(drawText)DrawText(g,w,h,seconds,options);
  }
 }
 static float TextScale(double seconds){return seconds<.10?3.3f-1.1f*Clamp(seconds/.10):seconds<.18?2.2f-1.6f*Ease((seconds-.10)/.08):seconds<.34?.60f+.40f*Ease((seconds-.18)/.16):1;}
 void DrawText(Graphics g,float width,float height,double seconds,StaggerScreenOptions options){
  float scale=height*.30f*TextScale(seconds)/textBounds.Width;double opacity=Clamp(seconds/.045)*(1-Clamp((seconds-.82)/.36));Color color=ColorTranslator.FromHtml(options.EffectColor),letters=ColorTranslator.FromHtml(options.TextColor);
  var state=g.Save();g.TranslateTransform(width/2,height/2);g.ScaleTransform(scale,scale);
  for(int n=3;n>=1;n--)using(var pen=new Pen(Alpha(color,opacity*(.045+.025*(4-n))),n*2.1f)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,text);}
  using(var brush=new SolidBrush(Alpha(letters,opacity)))g.FillPath(brush,text);g.Restore(state);
 }
 public void Dispose(){if(released)return;released=true;if(scene!=null)scene.Dispose();text.Dispose();attributes.Dispose();halo.Dispose();flare.Dispose();}
}

internal sealed class StaggerScreenEffect:Form {
 [StructLayout(LayoutKind.Sequential)]struct Rect {internal int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr window,ref Point point);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 readonly StaggerScreenGate gate=new StaggerScreenGate();readonly Func<StaggerSignal.Sample> read;
 readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly Stopwatch clock=Stopwatch.StartNew();readonly bool isolated;
 StaggerScreenOptions options=new StaggerScreenOptions();StaggerScreenPainter painter;LayeredImage image;Bitmap frame;
 bool active,testing,disposed;double started;uint actor;string flow;long epoch;int gamePid;
 internal long Revision;internal int Tests,Triggers,Frames;internal bool Active {get{return active;}}internal bool Testing {get{return active&&testing;}}
 internal StaggerScreenEffect(Func<StaggerSignal.Sample> source,bool preview){read=source;isolated=preview;Text="Aion 2 Helper - Staggered screen effect";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;timer.Tick+=delegate{Tick();};}
 internal void Configure(StaggerScreenOptions value,bool barEnabled,StaggerSignal.Sample sample){if(options.Enabled!=value.Enabled||options.TextColor!=value.TextColor||options.EffectColor!=value.EffectColor)Revision++;options=value.Copy();options.Normalize();gate.Configure(barEnabled&&options.Enabled&&!isolated,sample);if(!barEnabled||!options.Enabled){if(!testing)Stop();}}
 internal void Observe(StaggerSignal.Sample sample,double utc){
  if(active&&!testing&&!Context(sample,utc))Stop();
  if(!gate.Take(sample,utc))return;Rectangle area;int pid;if(!GameArea(out area,out pid)||!Foreground(pid))return;
  actor=sample.Actor;flow=sample.Flow;epoch=sample.Epoch;gamePid=pid;Triggers++;Play(area,false);
 }
 internal void Test(Rectangle area){Tests++;gamePid=0;Play(isolated?new Rectangle(-20000,-20000,600,412):area,true);}
 bool Context(StaggerSignal.Sample sample,double utc){return sample!=null&&sample.ConfirmedTarget&&sample.Actor==actor&&sample.Flow==flow&&sample.Epoch==epoch&&StaggerSignal.Fresh(sample,utc)&&StaggerSignal.Available(sample);}
 static bool Foreground(int pid){uint foreground;GetWindowThreadProcessId(GetForegroundWindow(),out foreground);return foreground==(uint)pid;}
 static bool GameArea(out Rectangle area,out int pid){area=Rectangle.Empty;pid=0;foreach(var game in Process.GetProcessesByName("AION2"))try{IntPtr window=game.MainWindowHandle;Rect r;Point origin=Point.Empty;if(window==IntPtr.Zero||IsIconic(window)||!Foreground(game.Id)||!GetClientRect(window,out r)||!ClientToScreen(window,ref origin))continue;int width=r.Right-r.Left,height=r.Bottom-r.Top;if(width<200||height<200||width>8192||height>8192||(long)width*height>16777216)continue;area=new Rectangle(origin.X,origin.Y,width,height);pid=game.Id;return true;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}finally{game.Dispose();}return false;}
 void Play(Rectangle area,bool test){Stop();if(area.Width<1||area.Height<1||area.Width>8192||area.Height>8192||(long)area.Width*area.Height>16777216)return;testing=test;active=true;Revision++;Bounds=area;started=clock.Elapsed.TotalSeconds;frame=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppPArgb);image=new LayeredImage();if(painter==null)painter=new StaggerScreenPainter();timer.Interval=(long)area.Width*area.Height>3000000?33:16;Tick();timer.Start();}
 void Tick(){if(!active||disposed)return;double elapsed=clock.Elapsed.TotalSeconds-started;if(elapsed>=StaggerScreenPainter.Duration||!testing&&(!Foreground(gamePid)||!Context(read(),DpsHistory.UtcNow()))){Stop();return;}painter.Paint(frame,elapsed,options);Frames++;image.Upload(frame);image.Present(Handle,Left,Top,255);if(!Visible){Show();OverlayNative.RaiseWithoutFocus(Handle);}}
 internal void Stop(){timer.Stop();if(active)Revision++;active=false;testing=false;if(Visible)Hide();if(frame!=null){frame.Dispose();frame=null;}if(image!=null){image.Dispose();image=null;}}
 internal object Status {get{return new{enabled=options.Enabled,active,testing,tests=Tests,triggers=Triggers,frames=Frames,text="Staggered",textColor=options.TextColor,effectColor=options.EffectColor,fixedPosition="game viewport center",duration=StaggerScreenPainter.Duration};}}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle=(p.ExStyle|0x80000|0x80|0x08000000|0x20)&~0x40000;return p;}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
 protected override void Dispose(bool disposing){if(disposing&&!disposed){Stop();disposed=true;timer.Dispose();if(painter!=null)painter.Dispose();}base.Dispose(disposing);}
}
