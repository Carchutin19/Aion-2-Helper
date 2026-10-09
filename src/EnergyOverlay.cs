using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Threading;
using Timer=System.Windows.Forms.Timer;

internal static class OverlayNative {
    [StructLayout(LayoutKind.Sequential)] internal struct Point {internal int X,Y;internal Point(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)] internal struct Size {internal int W,H;internal Size(int w,int h){W=w;H=h;}}
    [StructLayout(LayoutKind.Sequential)] internal struct MinMaxInfo {internal Point Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;}
    [StructLayout(LayoutKind.Sequential)] internal struct WindowPos {internal IntPtr Window,After;internal int X,Y,Width,Height;internal uint Flags;}
    [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct Blend {internal byte Operation,Flags,Alpha,Format;}
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dst,ref Point position,ref Size size,IntPtr src,ref Point origin,uint key,ref Blend blend,uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr h,IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr h,int index);
    [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr h,int index,int value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int ht,uint flags);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")] internal static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] internal static extern uint GetGuiResources(IntPtr process,uint flags);
    internal static void Present(IntPtr window,Bitmap bitmap,int x,int y,byte opacity=255) {
        IntPtr screen=GetDC(IntPtr.Zero),memory=CreateCompatibleDC(screen),hb=IntPtr.Zero,old=IntPtr.Zero;
        try {
            hb=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,hb);
            var position=new Point(x,y);var size=new Size(bitmap.Width,bitmap.Height);var origin=new Point(0,0);var blend=new Blend{Alpha=opacity,Format=1};
            if(!UpdateLayeredWindow(window,screen,ref position,ref size,memory,ref origin,0,ref blend,2))throw new Win32Exception(Marshal.GetLastWin32Error());
        }finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(hb!=IntPtr.Zero)DeleteObject(hb);DeleteDC(memory);ReleaseDC(IntPtr.Zero,screen);}
    }
    internal static void RaiseWithoutFocus(IntPtr h){SetWindowPos(h,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010);}
}

// Retain the native image/DC across opacity and position updates.
internal sealed class LayeredImage:IDisposable {
    readonly IntPtr memory=OverlayNative.CreateCompatibleDC(IntPtr.Zero);
    IntPtr bitmap,original;int width,height;
    internal int Uploads,Presentations;
    internal void Upload(Bitmap image){
        IntPtr next=image.GetHbitmap(Color.FromArgb(0)),previous=OverlayNative.SelectObject(memory,next);
        if(bitmap==IntPtr.Zero)original=previous;else OverlayNative.DeleteObject(bitmap);
        bitmap=next;width=image.Width;height=image.Height;Uploads++;
    }
    internal void Present(IntPtr window,int x,int y,byte opacity){
        var position=new OverlayNative.Point(x,y);var size=new OverlayNative.Size(width,height);var origin=new OverlayNative.Point(0,0);var blend=new OverlayNative.Blend{Alpha=opacity,Format=1};
        if(!OverlayNative.UpdateLayeredWindow(window,IntPtr.Zero,ref position,ref size,memory,ref origin,0,ref blend,2))throw new Win32Exception(Marshal.GetLastWin32Error());Presentations++;
    }
    public void Dispose(){if(original!=IntPtr.Zero)OverlayNative.SelectObject(memory,original);if(bitmap!=IntPtr.Zero){OverlayNative.DeleteObject(bitmap);bitmap=IntPtr.Zero;}OverlayNative.DeleteDC(memory);}
}

internal sealed class LatestStatusWriter:IDisposable {
    readonly string path;readonly object gate=new object();object pending;bool running,closed;
    internal LatestStatusWriter(string file){path=file;}
    internal void Publish(object value){lock(gate){if(closed)return;pending=value;if(running)return;running=true;ThreadPool.QueueUserWorkItem(delegate{var serializer=new JavaScriptSerializer();
            while(true){object next;lock(gate){next=pending;pending=null;if(next==null){running=false;Monitor.PulseAll(gate);return;}}
                try{File.WriteAllText(path,serializer.Serialize(next));}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        });}}
    public void Dispose(){lock(gate){closed=true;while(running)Monitor.Wait(gate);}}
}

// Monotonic time keeps holds and animation independent of packet/frame frequency.
internal sealed class BarMotion {
    internal const double FullHoldSeconds=1.5,FadeSeconds=.2,SmoothingSeconds=.07;
    internal double Value,Opacity;internal double? FullSince;
    internal bool Animating;double desiredOpacity,targetValue;
    bool started,hasValue;double lastTime;
    double lastTarget;bool hasTarget;
    internal void Step(double now,bool editing,bool fresh,double target,uint energy,uint maximum,EnergyBarOptions options=null,double targetAgeSeconds=-1){
        options=options??new EnergyBarOptions();double hold=options.HoldSeconds,fade=options.Fade?options.FadeSeconds:0,smooth=options.Smooth?options.SmoothingSeconds:0;
        double dt=started?Math.Max(0,now-lastTime):0;lastTime=now;started=true;
        if(!options.Enabled){Opacity=0;FullSince=null;hasValue=false;Animating=false;return;}
        target=Math.Max(0,Math.Min(1,target));
        double smoothDt=fresh&&hasTarget&&target!=lastTarget&&targetAgeSeconds>=0?Math.Min(dt,targetAgeSeconds):dt;
        if(fresh){lastTarget=target;hasTarget=true;if(!hasValue||smooth<=0){Value=target;hasValue=true;}else{Value+=(target-Value)*(1-Math.Exp(-smoothDt/smooth));if(Math.Abs(Value-target)<.0001)Value=target;}}
        bool full=options.AutoHide&&!editing&&fresh&&maximum>0&&energy==maximum;
        if(full){if(!FullSince.HasValue)FullSince=now;}else FullSince=null;
        bool show=!full||now-FullSince.Value<hold;
        double previousDesired=desiredOpacity;desiredOpacity=show?1:0;targetValue=target;
        if(editing){Opacity=1;Animating=fresh&&Math.Abs(Value-targetValue)>=.0001;return;}
        // A delayed frame only fades for the elapsed time after the hold expires.
        double fadeTime=show?dt:Math.Min(dt,Math.Max(0,now-FullSince.Value-hold));
        if(show&&previousDesired!=desiredOpacity&&targetAgeSeconds>=0)fadeTime=Math.Min(fadeTime,targetAgeSeconds);
        Opacity=fade<=0?(show?1:0):Math.Max(0,Math.Min(1,Opacity+(show?1:-1)*fadeTime/fade));
        Animating=(fresh&&Math.Abs(Value-targetValue)>=.0001)||Math.Abs(Opacity-desiredOpacity)>.00001;
    }
    internal static void Verify(){
        var motion=new BarMotion();motion.Step(0,true,true,.4,40,100);
        motion.Step(.1,false,true,1,100,100);motion.Step(1.599,false,true,1,100,100);
        if(motion.Opacity!=1)throw new Exception("Full energy must remain visible for 1.5 seconds");
        motion.Step(1.6,false,true,.7,70,100);if(motion.FullSince.HasValue||motion.Opacity!=1)throw new Exception("Spending cancels pending hide");
        motion.Step(2,false,true,1,100,100);motion.Step(3.49,false,true,1,100,100);motion.Step(3.55,false,true,1,100,100);
        if(motion.Opacity<.7||motion.Opacity>.8)throw new Exception("Fade starts after hold, rather than abruptly hiding");
        double fading=motion.Opacity;motion.Step(3.57,false,true,.8,80,100);
        if(motion.Opacity<=fading||motion.Opacity>=1||motion.FullSince.HasValue)throw new Exception("Spending smoothly reverses fade out");
        motion.Step(3.8,false,true,1,100,100);motion.Step(5.6,false,true,1,100,100);if(motion.Opacity!=0)throw new Exception("Complete fade hides window");
        motion.Step(5.65,false,true,.5,50,100);if(motion.Opacity<=0||motion.Opacity>=1||motion.Value<=.5||motion.Value>=1)throw new Exception("Fade in and energy smoothing on spend");
        motion.Step(5.7,true,true,.5,50,100);if(motion.Opacity!=1)throw new Exception("Editing must be immediately accessible");
        motion.Step(6,false,true,1,100,100);motion.Step(8,false,true,1,100,100);motion.Step(8.05,false,false,1,100,100);
        if(motion.Opacity<=0||motion.FullSince.HasValue)throw new Exception("Missing signal must show unknown state, rather than hide as full");
        var first=new BarMotion();first.Step(0,false,false,0,0,100);first.Step(.1,false,true,.6,60,100);if(first.Value!=.6)throw new Exception("First real reading must not animate from invented zero");
        var slow=new BarMotion();var fast=new BarMotion();slow.Step(0,true,true,1,100,100);fast.Step(0,true,true,1,100,100);
        for(int i=1;i<=10;i++)slow.Step(i*.02,true,true,.3,30,100);
        for(int i=1;i<=20;i++)fast.Step(i*.01,true,true,.3,30,100);
        if(Math.Abs(slow.Value-fast.Value)>.00001||slow.Value<=.3||slow.Value>=.35)throw new Exception("Frame-independent smoothing without overshoot");
        fast.Step(.25,true,true,.8,80,100);if(fast.Value<=.3||fast.Value>=.8)throw new Exception("Recovery must also be smoothed");
        var options=new EnergyBarOptions{AutoHide=false};var persistent=new BarMotion();
        persistent.Step(0,false,true,1,100,100,options);persistent.Step(100,false,true,1,100,100,options);
        if(persistent.Opacity!=1||persistent.FullSince.HasValue)throw new Exception("Disabled automatic hiding must stay visible at full energy indefinitely");
        options.AutoHide=true;persistent.Step(101,false,true,1,100,100,options);persistent.Step(102.49,false,true,1,100,100,options);
        if(persistent.Opacity!=1)throw new Exception("Enabling hide must begin a new complete hold");
        persistent.Step(103,false,true,1,100,100,options);if(persistent.Opacity!=0)throw new Exception("Enabled auto hide must use normal full-energy behavior");
        options.AutoHide=false;persistent.Step(103.1,false,true,1,100,100,options);persistent.Step(103.3,false,true,1,100,100,options);
        if(persistent.Opacity!=1)throw new Exception("Disabling hide must reveal an already hidden full bar");
        options.Fade=false;options.Smooth=false;options.AutoHide=true;options.HoldSeconds=0;
        persistent.Step(104,false,true,1,100,100,options);if(persistent.Opacity!=0)throw new Exception("Disabled fade and zero hold hide immediately");
        persistent.Step(104.01,false,true,.2,20,100,options);if(persistent.Value!=.2||persistent.Opacity!=1)throw new Exception("Disabled smoothing/fade snap to the actual reading without division by zero");
        options.Fade=true;options.FadeSeconds=0;options.Smooth=true;options.SmoothingSeconds=0;
        persistent.Step(104.02,false,true,.8,80,100,options);if(persistent.Value!=.8||persistent.Opacity!=1)throw new Exception("Zero animation durations must remain safe");
        options.Enabled=false;persistent.Step(105,true,true,.8,80,100,options);
        if(persistent.Opacity!=0||persistent.FullSince.HasValue)throw new Exception("Disabled energy bar must hide immediately even when widgets are unlocked");
        options.AutoHide=false;persistent.Step(106,false,false,0,0,100,options);
        if(persistent.Opacity!=0)throw new Exception("Missing data or disabled auto hide must never reveal a disabled bar");
        options.Enabled=true;persistent.Step(107,false,true,.6,60,100,options);
        if(persistent.Opacity!=1||persistent.Value!=.6)throw new Exception("Re-enabling must resume with the actual current energy");
        var idle=new BarMotion();idle.Step(0,true,true,.8,80,100);idle.Step(10,false,true,.3,30,100,new EnergyBarOptions(),0);
        if(idle.Value!=.8||!idle.Animating)throw new Exception("Waking from idle must begin smoothing from the packet arrival rather than snapping over idle time");
        idle.Step(10.02,false,true,.3,30,100,new EnergyBarOptions(),.02);if(idle.Value>=.8||idle.Value<=.3)throw new Exception("Adaptive timer must preserve the animated transition");
    }
}

internal static class BarDesign {
    internal const int MinimumWidth=40,MinimumHeight=3,DefaultWidth=320,DefaultHeight=4,MaximumWidth=32767,MaximumHeight=300;
    internal const int GlowPadding=5;
    static readonly Color LowEnergy=Color.FromArgb(160,45,45),MediumEnergy=Color.FromArgb(184,84,22),HighEnergy=Color.FromArgb(40,112,68);
    static readonly double[] GlowKernel=CreateGlowKernel();
    static double[] CreateGlowKernel(){var kernel=new double[2*GlowPadding+1];double sum=0;for(int d=-GlowPadding;d<=GlowPadding;d++){double weight=Math.Exp(-d*d/(2*1.5*1.5));kernel[d+GlowPadding]=weight;sum+=weight;}for(int i=0;i<kernel.Length;i++)kernel[i]/=sum;return kernel;}
    internal sealed class GlowWorkspace {
        internal double[] Horizontal=new double[0],Vertical=new double[0];internal byte[] Pixels=new byte[0];int lastFilled=-1,lastColorHeight=-1;
        internal void Prepare(int width,int height,int filled,int colorHeight){
            int pad=GlowPadding;if(Horizontal.Length!=width){Horizontal=new double[width];lastFilled=-1;}if(Vertical.Length!=height){Vertical=new double[height];lastColorHeight=-1;}
            if(Pixels.Length!=width*height*4)Pixels=new byte[width*height*4];else Array.Clear(Pixels,0,Pixels.Length);
            if(lastFilled!=filled){Array.Clear(Horizontal,0,Horizontal.Length);for(int x=0;x<filled+2*pad;x++)for(int d=-pad;d<=pad;d++)if(x-d>=pad&&x-d<pad+filled)Horizontal[x]+=GlowKernel[d+pad];lastFilled=filled;}
            if(lastColorHeight!=colorHeight){Array.Clear(Vertical,0,Vertical.Length);for(int y=0;y<colorHeight+2*pad;y++)for(int d=-pad;d<=pad;d++)if(y-d>=pad&&y-d<pad+colorHeight)Vertical[y]+=GlowKernel[d+pad];lastColorHeight=colorHeight;}
        }
    }
    static Color Mix(Color from,Color to,double amount){return Color.FromArgb((int)Math.Round(from.R+(to.R-from.R)*amount),(int)Math.Round(from.G+(to.G-from.G)*amount),(int)Math.Round(from.B+(to.B-from.B)*amount));}
    internal static Color EnergyColor(double ratio,EnergyBarOptions options=null){
        Color low=options==null?LowEnergy:options.LowPaint,medium=options==null?MediumEnergy:options.MediumPaint,high=options==null?HighEnergy:options.HighPaint;
        if(options!=null&&!options.DynamicColors)return medium;
        if(ratio<=.25)return low;
        if(ratio<.35)return Mix(low,medium,(ratio-.25)/.1);
        if(ratio<=.65)return medium;
        if(ratio<.75)return Mix(medium,high,(ratio-.65)/.1);
        return high;
    }
    static Color EmissionColor(double ratio,EnergyBarOptions options=null){
        Color color=EnergyColor(ratio,options);double scale=255.0/Math.Max(1,(int)Math.Max(color.R,Math.Max(color.G,color.B)));
        return Color.FromArgb((int)Math.Round(color.R*scale),(int)Math.Round(color.G*scale),(int)Math.Round(color.B*scale));
    }
    internal static int HitTest(int x,int y,int width,int height,int edge) {
        int verticalEdge=Math.Max(1,Math.Min(edge,(height-1)/2));
        bool left=x<edge,right=x>=width-edge,top=y<verticalEdge,bottom=y>=height-verticalEdge;
        if(left&&top)return 13;if(right&&top)return 14;if(left&&bottom)return 16;if(right&&bottom)return 17;
        if(left)return 10;if(right)return 11;if(top)return 12;if(bottom)return 15;return 2;
    }
    internal static Rectangle ResizeBounds(Rectangle start,System.Drawing.Point delta,int hit){
        bool left=hit==10||hit==13||hit==16,right=hit==11||hit==14||hit==17,top=hit==12||hit==13||hit==14,bottom=hit==15||hit==16||hit==17;
        int width=Math.Max(MinimumWidth,Math.Min(MaximumWidth,start.Width+(left?-delta.X:right?delta.X:0)));
        int height=Math.Max(MinimumHeight,Math.Min(MaximumHeight,start.Height+(top?-delta.Y:bottom?delta.Y:0)));
        return new Rectangle(left?start.Right-width:start.Left,top?start.Bottom-height:start.Top,width,height);
    }
    internal static Bitmap Render(int width,int height,double ratio,bool editing,bool fresh,float dpiScale,EnergyBarOptions options=null) {
        var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(bitmap)){
            // Pixel-aligned shapes remain crisp even at the minimum 3-pixel height.
            g.SmoothingMode=SmoothingMode.None;g.PixelOffsetMode=PixelOffsetMode.Default;g.Clear(Color.Transparent);
            // Layered windows only hit-test nontransparent pixels. This near-invisible
            // editing surface also makes the empty part of the bar draggable.
            if(editing)using(var brush=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(brush,0,0,width,height);
            int baseline=Math.Max(1,height/4),colorHeight=height-baseline;
            // The remaining track is a thinner dark line; there is no full-height
            // black rectangle behind the colored stroke.
            using(var brush=new SolidBrush(Color.FromArgb(options==null?170:options.TrackOpacity,0,0,0)))g.FillRectangle(brush,0,colorHeight,width,baseline);
            int filled=(int)Math.Round(width*Math.Max(0,Math.Min(1,ratio)));
            if(fresh&&filled>0)using(var brush=new SolidBrush(EnergyColor(ratio,options)))g.FillRectangle(brush,0,0,filled,colorHeight);
            if(!fresh)using(var brush=new SolidBrush(Color.FromArgb(170,167,173,181))){int dot=Math.Max(1,(int)Math.Round(dpiScale));for(int x=0;x<width;x+=dot*4)g.FillRectangle(brush,x,0,Math.Min(dot*2,width-x),colorHeight);}
            if(editing){
                using(var brush=new SolidBrush(Color.FromArgb(255,100,110,115))){g.FillRectangle(brush,0,0,1,height);g.FillRectangle(brush,width-1,0,1,height);}
            }
        }return bitmap;
    }
    internal static Bitmap RenderEmissive(int width,int height,double ratio,bool editing,bool fresh,float dpiScale,EnergyBarOptions options=null,GlowWorkspace workspace=null){
        int pad=GlowPadding,outerWidth=width+2*pad,outerHeight=height+2*pad;
        var bitmap=new Bitmap(outerWidth,outerHeight,PixelFormat.Format32bppPArgb);
        int filled=(int)Math.Round(width*Math.Max(0,Math.Min(1,ratio))),colorHeight=height-Math.Max(1,height/4);
        bool emission=options==null||options.Emissive;
        if(emission&&fresh&&filled>0){
            // Blur only the colored fill, never the empty track. A separable,
            // finite Gaussian creates a soft halo with transparent outer edges.
            workspace=workspace??new GlowWorkspace();workspace.Prepare(outerWidth,outerHeight,filled,colorHeight);var horizontal=workspace.Horizontal;var vertical=workspace.Vertical;
            Color glowColor=EmissionColor(ratio,options);int red=glowColor.R,green=glowColor.G,blue=glowColor.B;
            var data=bitmap.LockBits(new Rectangle(0,0,outerWidth,outerHeight),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
            try{var pixels=workspace.Pixels;
                for(int y=0;y<colorHeight+2*pad;y++)for(int x=0;x<filled+2*pad;x++){
                    int alpha=Math.Min(255,(int)Math.Round(200*(options==null?1:options.GlowPercent/100.0)*horizontal[x]*vertical[y]));int offset=y*data.Stride+x*4;
                    pixels[offset]=(byte)((blue*alpha+127)/255);pixels[offset+1]=(byte)((green*alpha+127)/255);pixels[offset+2]=(byte)((red*alpha+127)/255);pixels[offset+3]=(byte)alpha;
                }
                Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
            }finally{bitmap.UnlockBits(data);}
        }
        using(var g=Graphics.FromImage(bitmap)){
            if(editing)using(var brush=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(brush,0,0,outerWidth,outerHeight);
            using(var core=Render(width,height,ratio,editing,fresh,dpiScale,options))g.DrawImageUnscaled(core,GlowPadding,GlowPadding);
            if(emission&&fresh&&filled>0){
                // Flight-meter reference: saturated colored edges around a thin,
                // almost-white luminous filament, rather than a flat dark fill.
                Color light=EmissionColor(ratio,options);
                using(var brush=new SolidBrush(Mix(EnergyColor(ratio,options),light,.45)))g.FillRectangle(brush,pad,pad,filled,colorHeight);
                int filament=Math.Max(1,colorHeight/3),filamentY=pad+(colorHeight-filament)/2;
                using(var brush=new SolidBrush(Mix(light,Color.White,.8)))g.FillRectangle(brush,pad+(filled>2?1:0),filamentY,filled-(filled>2?2:0),filament);
                if(editing)using(var brush=new SolidBrush(Color.FromArgb(255,100,110,115))){g.FillRectangle(brush,pad,pad,1,height);g.FillRectangle(brush,pad+width-1,pad,1,height);}
            }
        }
        return bitmap;
    }
    internal static void Verify(string root) {
        if(HitTest(1,1,320,20,5)!=13||HitTest(319,19,320,20,5)!=17||HitTest(160,1,320,20,5)!=12||HitTest(160,19,320,20,5)!=15||HitTest(160,10,320,20,5)!=2)throw new Exception("Resize handles");
        if(HitTest(160,0,320,3,6)!=12||HitTest(160,1,320,3,6)!=2||HitTest(160,2,320,3,6)!=15||HitTest(1,1,320,3,6)!=10)throw new Exception("Minimum-height drag/resize targets");
        if(HitTest(225,5,454,13,6)!=12||HitTest(225,6,454,13,6)!=2||HitTest(225,7,454,13,6)!=15)throw new Exception("Three-pixel core must remain movable/resizable inside halo padding");
        var bounds=new Rectangle(740,981,444,4);
        var thinner=ResizeBounds(bounds,new System.Drawing.Point(0,-100),15);if(thinner.Height!=3||thinner.Left!=740||thinner.Width!=444||thinner.Top!=981)throw new Exception("Bottom edge minimum without length jump");
        var longer=ResizeBounds(bounds,new System.Drawing.Point(200,0),11);if(longer.Height!=4||longer.Width!=644)throw new Exception("Length resize must preserve thickness");
        var corner=ResizeBounds(bounds,new System.Drawing.Point(500,500),13);if(corner.Right!=bounds.Right||corner.Bottom!=bounds.Bottom||corner.Size!=new System.Drawing.Size(40,3))throw new Exception("Corner anchors/minimum");
        string output=Path.Combine(root,"designs","implemented");Directory.CreateDirectory(output);
        foreach(int height in new[]{3,4,8,64})using(var bar=Render(320,height,.65,false,true,1)){
            bar.Save(Path.Combine(output,"slim-v2-320x"+height+".png"),ImageFormat.Png);
            Color orange=bar.GetPixel(0,0),empty=bar.GetPixel(280,0),black=bar.GetPixel(280,height-1);
            if(orange.A!=255||orange.R!=184||orange.G!=84||orange.B!=22||empty.A!=0||black.A!=170||black.R!=0)throw new Exception("Slim design: no margins/panel, dark orange fill, black baseline");
        }
        using(var editing=Render(640,4,.65,true,true,1)){editing.Save(Path.Combine(output,"slim-v2-edit.png"),ImageFormat.Png);if(editing.GetPixel(500,1).A==0)throw new Exception("Empty segment must accept editing clicks");}
        using(var hidpi=Render(640,8,.65,false,true,2))hidpi.Save(Path.Combine(output,"slim-v2-200dpi.png"),ImageFormat.Png);
        using(var empty=Render(320,3,0,false,true,1))if(empty.GetPixel(10,0).A!=0||empty.GetPixel(10,2).A!=170)throw new Exception("Zero energy baseline");
        foreach(double ratio in new[]{.2,.5,.9})using(var bar=Render(425,3,ratio,false,true,1))bar.Save(Path.Combine(output,"dark-energy-"+(int)(ratio*100)+".png"),ImageFormat.Png);
        foreach(double ratio in new[]{.2,.5,.9})using(var bar=RenderEmissive(425,3,ratio,false,true,1)){
            bar.Save(Path.Combine(output,"emissive-energy-"+(int)(ratio*100)+".png"),ImageFormat.Png);
            if(bar.Width!=435||bar.Height!=13||bar.GetPixel(20,4).A==0||bar.GetPixel(420,1).A!=0||bar.GetPixel(0,0).A!=0)throw new Exception("Halo extent/transparent empty track");
            Color core=bar.GetPixel(20,5),edge=bar.GetPixel(20,6);if(core.A!=255||core.R<200||core.G<200||core.B<200||core.R+core.G+core.B<=edge.R+edge.G+edge.B)throw new Exception("Flight-style emission must have a brighter near-white filament and colored edge");
        }
        EnergyOverlay.VerifyNativeSizing(root);
        BarMotion.Verify();
        EnergyBarOptions.Verify();
        var custom=new EnergyBarOptions{Emissive=false,DynamicColors=false,MediumColor="#123456",TrackOpacity=80};
        using(var flat=RenderEmissive(320,4,.9,false,true,1,custom))if(flat.GetPixel(20,4).A!=0||flat.GetPixel(20,5).ToArgb()!=ColorTranslator.FromHtml("#123456").ToArgb()||flat.GetPixel(310,8).A!=80)throw new Exception("Settings must control emission, fixed color and dark track opacity without changing geometry");
        File.WriteAllText(Path.Combine(root,"ui-test.txt"),"PASS: enabled/disabled bar persistence and migration; disabled bar stays hidden even unlocked or without data; configurable emission/colors/track; native minimum size; hold/fade reversal and frame-independent smoothing; adaptive timer preserves animation after idle; fade-only bitmap reuse, zero redraws when unchanged, no GDI handle growth after 2000 redraws.");
    }
}

internal sealed class EnergyOverlay:Form {
    readonly string root;readonly DashSignal signal;readonly Recorder recorder;readonly Timer timer=new Timer();readonly NotifyIcon tray=new NotifyIcon();
    readonly Icon helperIcon;
    readonly LayeredImage image=new LayeredImage();readonly BarDesign.GlowWorkspace glowWorkspace=new BarDesign.GlowWorkspace();readonly LatestStatusWriter statusWriter;
    int imageWidth=-1,imageHeight,imageFilled,imageColor,imageX=int.MinValue,imageY,imageAlpha=-1,readingQueued;bool imageEditing,imageFresh;float imageScale;
    double nextStatusWrite;
    readonly BarMotion motion=new BarMotion();readonly Stopwatch animationClock=Stopwatch.StartNew();double lastPoll=-2,lastRaise=-1;
    readonly ToolStripMenuItem stateItem=new ToolStripMenuItem("Waiting for data");readonly ToolStripMenuItem editItem=new ToolStripMenuItem();
    internal readonly WidgetLock InteractionLock=new WidgetLock();
    readonly ConfigurationHistory configurationHistory=new ConfigurationHistory();bool restoringConfiguration;
    bool locked {get{return InteractionLock.Locked;}set{InteractionLock.Locked=value;}}
    string language="en";
    internal string Language {get{return language;}}
    EnergyBarOptions options=new EnergyBarOptions();HelperSettings settingsWindow;HelperTrayMenu trayMenu;
    internal event Action SettingsChanged;
    bool closing,ready,rendering;double lastSaved;string status="No data · dash once to start";string startError="";
    string renderKey="",statusKey="";bool preview;double previewRatio;
    System.Drawing.Size initialSize=new System.Drawing.Size(BarDesign.DefaultWidth,BarDesign.DefaultHeight);
    bool resizing;int resizeHit;Rectangle resizeStart;System.Drawing.Point resizeMouseStart;
    Rectangle CoreBounds {
        get{return new Rectangle(Left+BarDesign.GlowPadding,Top+BarDesign.GlowPadding,Math.Max(1,Width-2*BarDesign.GlowPadding),Math.Max(1,Height-2*BarDesign.GlowPadding));}
        set{Bounds=new Rectangle(value.X-BarDesign.GlowPadding,value.Y-BarDesign.GlowPadding,Math.Max(BarDesign.MinimumWidth,Math.Min(BarDesign.MaximumWidth,value.Width))+2*BarDesign.GlowPadding,Math.Max(BarDesign.MinimumHeight,Math.Min(BarDesign.MaximumHeight,value.Height))+2*BarDesign.GlowPadding);}
    }
    System.Drawing.Size CoreSize {get{return CoreBounds.Size;}set{CoreBounds=new Rectangle(CoreBounds.Location,value);}}
    internal EnergyOverlay(string folder,bool previewMode=false) {
        root=folder;preview=previewMode;previewRatio=.65;signal=new DashSignal(root);recorder=new Recorder(root,false);recorder.OnSegment=signal.Consume;recorder.OnCaptureStarted=signal.Reset;statusWriter=new LatestStatusWriter(Path.Combine(root,"live-status.json"));signal.OnReading=QueueReading;
        helperIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath)??(Icon)SystemIcons.Application.Clone();Icon=helperIcon;
        Text="Aion 2 Helper - Energy";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;
        // Enforce the slim window's own limits in native sizing messages.
        CoreSize=new System.Drawing.Size(BarDesign.DefaultWidth,BarDesign.DefaultHeight);MinimumSize=System.Drawing.Size.Empty;MaximumSize=new System.Drawing.Size(BarDesign.MaximumWidth+2*BarDesign.GlowPadding,BarDesign.MaximumHeight+2*BarDesign.GlowPadding);
        var area=Screen.PrimaryScreen.WorkingArea;CoreBounds=new Rectangle(area.Left+(area.Width-CoreSize.Width)/2,area.Top+area.Height-240,CoreSize.Width,CoreSize.Height);
        LoadSettings();
        if(preview)locked=false;
        configurationHistory.Observe(Configuration);
        var menu=new ContextMenuStrip();menu.Items.Add("Settings",null,delegate{OpenSettings();});
        editItem.Click+=delegate{ToggleLock();};menu.Items.Add(editItem);menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit Aion 2 Helper",null,delegate{Close();});ContextMenuStrip=menu;
        menu.Opening+=delegate(object sender,CancelEventArgs e){e.Cancel=true;if(!closing)BeginInvoke(new Action(delegate{OpenTrayMenu(Cursor.Position);}));};
        tray.Icon=helperIcon;tray.Text="Aion 2 Helper";tray.Visible=!preview;tray.DoubleClick+=delegate{OpenSettings();};tray.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right&&!closing)BeginInvoke(new Action(delegate{OpenTrayMenu(Cursor.Position);}));};
        InteractionLock.Changed+=delegate{if(ready){ApplyLock();UpdateBar();SaveSettings();}};
        // Before handle creation WinForms can still clamp Size to the frame's
        // minimum. Apply the requested dimensions once native handlers are active.
        Shown+=delegate{CoreSize=initialSize;ready=true;ApplyLock();if(!preview)recorder.MaintainAsync(options.Enabled);UpdateBar();SaveSettings();};
        Resize+=delegate{if(ready)RenderNow();};Move+=delegate{if(ready&&Visible&&!rendering)RenderNow();};
        timer.Interval=250;timer.Tick+=delegate{try{double now=animationClock.Elapsed.TotalSeconds;if(!preview&&now-lastPoll>=2){lastPoll=now;recorder.MaintainAsync(options.Enabled);}startError=recorder.LastStartError;UpdateBar();if(Visible&&now-lastRaise>=1){lastRaise=now;OverlayNative.RaiseWithoutFocus(Handle);}}catch(Exception ex){status="Error: "+ex.Message;stateItem.Text=status;}};timer.Start();
        FormClosing+=delegate{closing=true;if(trayMenu!=null&&!trayMenu.IsDisposed)trayMenu.Close();if(settingsWindow!=null&&!settingsWindow.IsDisposed)settingsWindow.Close();timer.Stop();recorder.Dispose();timer.Dispose();SaveSettings();statusWriter.Dispose();image.Dispose();tray.Visible=false;tray.Dispose();helperIcon.Dispose();};
    }
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80)&~0x40000;cp.Style&=~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    void LoadSettings(){try{var cfg=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"overlay-settings.json")));signal.Maximum=Convert.ToUInt32(cfg["maximum"]);if(signal.Maximum==0)signal.Maximum=113900;
        bool migrate=!cfg.ContainsKey("design")||Convert.ToString(cfg["design"])!="slim-v2";int previousHeight=cfg.ContainsKey("height")?Convert.ToInt32(cfg["height"]):BarDesign.DefaultHeight;
        if(cfg.ContainsKey("width"))initialSize=new System.Drawing.Size(Math.Max(BarDesign.MinimumWidth,Math.Min(BarDesign.MaximumWidth,Convert.ToInt32(cfg["width"]))),migrate?BarDesign.DefaultHeight:Math.Max(BarDesign.MinimumHeight,Math.Min(BarDesign.MaximumHeight,previousHeight)));CoreSize=initialSize;
        var position=new System.Drawing.Point(Convert.ToInt32(cfg["x"]),Convert.ToInt32(cfg["y"])+(migrate?(previousHeight-initialSize.Height)/2:0));if(Screen.AllScreens.Any(s=>s.WorkingArea.IntersectsWith(new Rectangle(position,initialSize))))CoreBounds=new Rectangle(position,initialSize);
        if(cfg.ContainsKey("locked"))locked=Convert.ToBoolean(cfg["locked"]);options=EnergyBarOptions.Read(cfg);language=UiLanguage.Read(cfg);
    }catch{}}
    void SaveSettings(){if(!restoringConfiguration)configurationHistory.Observe(Configuration);if(!preview){var core=CoreBounds;File.WriteAllText(Path.Combine(root,"overlay-settings.json"),new JavaScriptSerializer().Serialize(new {maximum=signal.Maximum,x=core.X,y=core.Y,width=core.Width,height=core.Height,locked=locked,field="008D/u32/kind3",validated=true,design="slim-v2",palette="dark-energy-v1",emissive=options.Emissive,language=language,energyBar=options}));}if(SettingsChanged!=null)SettingsChanged();}
    void ApplyLock(){int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,locked?style|0x20:style&~0x20);editItem.Text=UiLanguage.Text(locked?"Unlock":"Lock",language);renderKey="";}
    void ToggleLock(){EndResize();locked=!locked;}
    internal void ToggleWidgetsLock(){ToggleLock();}
    internal string ReadingStatus {get{return UiLanguage.Text(options.Enabled?status:"Energy Bar disabled",language);}}
    internal EnergyConfiguration Configuration {get{return new EnergyConfiguration{Options=options.Copy(),Bounds=CoreBounds,Locked=locked,Maximum=signal.Maximum,Language=language};}}
    internal bool CanUndoConfiguration {get{return configurationHistory.CanUndo;}}
    internal bool CanRedoConfiguration {get{return configurationHistory.CanRedo;}}
    internal void UndoConfiguration(){RestoreConfiguration(false);}
    internal void RedoConfiguration(){RestoreConfiguration(true);}
    void RestoreConfiguration(bool redo){
        EndResize();EnergyConfiguration cfg;if(!(redo?configurationHistory.Redo(out cfg):configurationHistory.Undo(out cfg)))return;
        restoringConfiguration=true;try{ApplyConfiguration(cfg);}finally{restoringConfiguration=false;}
    }
    internal void ApplyConfiguration(EnergyConfiguration cfg){
        bool wasEnabled=options.Enabled;EndResize();language=UiLanguage.Normalize(cfg.Language);options=cfg.Options.Copy();options.Normalize();signal.Maximum=Math.Max(1u,cfg.Maximum);CoreBounds=cfg.Bounds;locked=cfg.Locked;
        if(!preview&&wasEnabled!=options.Enabled){recorder.MaintainAsync(options.Enabled);lastPoll=animationClock.Elapsed.TotalSeconds;}
        ApplyLock();renderKey="";statusKey="";UpdateBar();SaveSettings();
    }
    internal bool Calibrate(){var reading=signal.Current;if(!IsFresh||reading==null||reading.Value==0)return false;signal.Maximum=reading.Value;renderKey="";UpdateBar();SaveSettings();return true;}
    internal void OpenSettings(){
        if(trayMenu!=null&&!trayMenu.IsDisposed)trayMenu.Close();
        if(settingsWindow==null||settingsWindow.IsDisposed)settingsWindow=new HelperSettings(this);
        settingsWindow.Show();if(settingsWindow.WindowState==System.Windows.WindowState.Minimized)settingsWindow.WindowState=System.Windows.WindowState.Normal;settingsWindow.Activate();
    }
    void OpenTrayMenu(System.Drawing.Point position){
        if(closing)return;if(trayMenu!=null&&!trayMenu.IsDisposed)trayMenu.Close();var popup=new HelperTrayMenu(this);trayMenu=popup;popup.Closed+=delegate{if(trayMenu==popup)trayMenu=null;};popup.ShowAt(position);
    }
    void BeginResize(int hit,System.Drawing.Point cursor){resizeHit=hit;resizeStart=CoreBounds;resizeMouseStart=cursor;resizing=true;Capture=true;}
    void ResizeTo(System.Drawing.Point cursor){if(!resizing)return;CoreBounds=BarDesign.ResizeBounds(resizeStart,new System.Drawing.Point(cursor.X-resizeMouseStart.X,cursor.Y-resizeMouseStart.Y),resizeHit);}
    void EndResize(){if(!resizing)return;resizing=false;Capture=false;SaveSettings();renderKey="";RenderNow();}
    void QueueReading(DashSignal.Reading reading){
        if(closing||!IsHandleCreated||Interlocked.CompareExchange(ref readingQueued,1,0)!=0)return;
        try{BeginInvoke(new Action(delegate{Interlocked.Exchange(ref readingQueued,0);if(!closing)UpdateBar();}));}catch(InvalidOperationException){Interlocked.Exchange(ref readingQueued,0);}
    }
    bool Fresh(DashSignal.Snapshot snapshot,double now){return preview||(recorder.Active&&snapshot.Reading!=null&&now-snapshot.LastTraffic<=4);}
    bool IsFresh {get{return Fresh(signal.ReadSnapshot(),(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds);}}
    double Ratio {get{var r=signal.Current;return preview?previewRatio:r==null?0:(double)r.Value/signal.Maximum;}}
    void UpdateBar(){
        if(closing)return;var snapshot=signal.ReadSnapshot();var r=snapshot.Reading;double utcNow=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds,now=animationClock.Elapsed.TotalSeconds;bool fresh=Fresh(snapshot,utcNow);double ratio=preview?previewRatio:r==null?0:(double)r.Value/signal.Maximum;
        string nextStatus=!options.Enabled?"Energy Bar disabled":fresh?ratio.ToString("P0")+(ratio>1?" · recalibrate the maximum":""):(startError.Length>0?startError:"No data · dash once to start");
        if(nextStatus!=status)status=nextStatus;string localized=ReadingStatus;
        if(stateItem.Text!=localized){stateItem.Text=localized;string tooltip="Aion 2 Helper · "+localized;tray.Text=tooltip.Substring(0,Math.Min(63,tooltip.Length));}
        motion.Step(now,!locked,fresh,ratio,preview?(uint)(previewRatio*100):r==null?0:r.Value,preview?100:signal.Maximum,options,preview?-1:fresh&&r!=null?Math.Max(0,utcNow-r.Timestamp):0);
        bool show=options.Enabled&&motion.Opacity>0;
        if(show&&!Visible){RenderNow(true);Show();OverlayNative.RaiseWithoutFocus(Handle);}else if(!show&&Visible)Hide();
        if(Visible)RenderNow();
        if(!preview&&now>=nextStatusWrite){nextStatusWrite=now+.5;string meta=Width+"/"+Height+"/"+Left+"/"+Top+"/"+locked+"/"+fresh+"/"+Visible+"/"+status+"/"+snapshot.Errors;
            if(meta!=statusKey||(r!=null&&r.Timestamp>lastSaved)){if(r!=null)lastSaved=r.Timestamp;statusKey=meta;WriteStatus(snapshot,fresh);}}
        int interval=motion.Animating?16:options.Enabled?250:1000;
        if(!motion.Animating&&motion.FullSince.HasValue&&motion.Opacity>0){double remaining=options.HoldSeconds-(now-motion.FullSince.Value);if(remaining>0)interval=Math.Min(interval,Math.Max(1,(int)Math.Ceiling(remaining*1000)));}
        if(timer.Interval!=interval)timer.Interval=interval;
    }
    void WriteStatus(DashSignal.Snapshot snapshot,bool fresh){var core=CoreBounds;var r=snapshot.Reading;statusWriter.Publish(new {ts=r==null?0:r.Timestamp,actor=r==null?0:r.Actor,value=r==null?0:r.Value,maximum=signal.Maximum,percentage=(r==null?0:(double)r.Value/signal.Maximum)*100,displayedPercentage=motion.Value*100,opacity=motion.Opacity,enabled=options.Enabled,autoHide=options.AutoHide,fullHoldSeconds=options.HoldSeconds,fadeSeconds=options.Fade?options.FadeSeconds:0,smoothingSeconds=options.Smooth?options.SmoothingSeconds:0,samples=snapshot.Samples,errors=snapshot.Errors,fresh=fresh,editing=options.Enabled&&!locked,visible=Visible,width=core.Width,height=core.Height,x=core.X,y=core.Y,status=status,style="slim-v2",emissive=options.Emissive});}
    void RenderNow(bool includeHidden=false){
        if(!ready||closing||rendering||!options.Enabled||(!Visible&&!includeHidden))return;rendering=true;
        try {float scale=1;try{scale=OverlayNative.GetDpiForWindow(Handle)/96f;}catch(EntryPointNotFoundException){}double ratio=motion.Value;byte alpha=(byte)Math.Round(motion.Opacity*255);
            var core=CoreBounds;bool fresh=IsFresh;int filled=(int)Math.Round(Math.Max(0,Math.Min(1,ratio))*core.Width),color=BarDesign.EnergyColor(ratio,options).ToArgb();
            bool changed=renderKey.Length==0||imageWidth!=core.Width||imageHeight!=core.Height||imageFilled!=filled||imageColor!=color||imageEditing!=!locked||imageFresh!=fresh||imageScale!=scale;
            if(changed){using(var bitmap=BarDesign.RenderEmissive(core.Width,core.Height,ratio,!locked,fresh,scale,options,glowWorkspace))image.Upload(bitmap);
                imageWidth=core.Width;imageHeight=core.Height;imageFilled=filled;imageColor=color;imageEditing=!locked;imageFresh=fresh;imageScale=scale;renderKey="valid";}
            if(changed||includeHidden||imageX!=Left||imageY!=Top||imageAlpha!=alpha){image.Present(Handle,Left,Top,alpha);imageX=Left;imageY=Top;imageAlpha=alpha;}
        }finally{rendering=false;}
    }
    protected override void WndProc(ref Message m){
        // Resize ourselves instead of using WS_THICKFRAME, whose native dragging
        // forcibly restores a 39-pixel frame even with a smaller tracking limit.
        if(m.Msg==0xa1&&!locked&&m.WParam.ToInt32()>=10&&m.WParam.ToInt32()<=17){BeginResize(m.WParam.ToInt32(),Cursor.Position);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x200&&resizing){ResizeTo(Cursor.Position);m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2)&&resizing){EndResize();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x215&&resizing){resizing=false;SaveSettings();}
        if(m.Msg==0x20&&!locked){int hit=(int)(m.LParam.ToInt64()&0xffff);Cursor cursor=null;
            if(hit==10||hit==11)cursor=Cursors.SizeWE;else if(hit==12||hit==15)cursor=Cursors.SizeNS;else if(hit==13||hit==17)cursor=Cursors.SizeNWSE;else if(hit==14||hit==16)cursor=Cursors.SizeNESW;
            if(cursor!=null){OverlayNative.SetCursor(cursor.Handle);m.Result=new IntPtr(1);return;}}
        if(m.Msg==0x46){ // DefWindowProc otherwise reapplies the system frame minimum.
            var position=(OverlayNative.WindowPos)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.WindowPos));
            if((position.Flags&0x0001)==0){int padding=2*BarDesign.GlowPadding;position.Width=Math.Max(BarDesign.MinimumWidth+padding,Math.Min(BarDesign.MaximumWidth+padding,position.Width));position.Height=Math.Max(BarDesign.MinimumHeight+padding,Math.Min(BarDesign.MaximumHeight+padding,position.Height));Marshal.StructureToPtr(position,m.LParam,false);}
            m.Result=IntPtr.Zero;return;
        }
        if(m.Msg==0x24){ // Override Windows' thick-frame minimum (about 39 px).
            base.WndProc(ref m);var limits=(OverlayNative.MinMaxInfo)Marshal.PtrToStructure(m.LParam,typeof(OverlayNative.MinMaxInfo));
            int padding=2*BarDesign.GlowPadding;limits.MinTrackSize=new OverlayNative.Point(BarDesign.MinimumWidth+padding,BarDesign.MinimumHeight+padding);limits.MaxTrackSize=new OverlayNative.Point(BarDesign.MaximumWidth+padding,BarDesign.MaximumHeight+padding);
            Marshal.StructureToPtr(limits,m.LParam,false);m.Result=IntPtr.Zero;return;
        }
        if(m.Msg==0x83&&m.WParam!=IntPtr.Zero){m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84&&ready){if(locked){m.Result=new IntPtr(-1);return;}long position=m.LParam.ToInt64();var point=PointToClient(new System.Drawing.Point((short)(position&0xffff),(short)((position>>16)&0xffff)));m.Result=new IntPtr(BarDesign.HitTest(point.X,point.Y,Width,Height,6));return;}
        if(m.Msg==0xa5&&!locked){OpenTrayMenu(Cursor.Position);return;}
        if(m.Msg==0x232){SaveSettings();renderKey="";RenderNow();}
        if(m.Msg==0x2e0){renderKey="";} // Re-render after Windows applies per-monitor DPI bounds.
        base.WndProc(ref m);
    }
    internal static void VerifyNativeSizing(string root){
        using(var window=new EnergyOverlay(root,true)){
            window.initialSize=new System.Drawing.Size(444,4);window.Show();Application.DoEvents();
            if(window.CoreSize.Height!=4||window.ClientSize.Height!=14)throw new Exception("Startup must preserve slim height plus halo after Show");
            IntPtr handle=window.Handle;IntPtr buffer=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(OverlayNative.MinMaxInfo)));
            try{Marshal.StructureToPtr(new OverlayNative.MinMaxInfo(),buffer,false);OverlayNative.SendMessage(handle,0x24,IntPtr.Zero,buffer);
                var limits=(OverlayNative.MinMaxInfo)Marshal.PtrToStructure(buffer,typeof(OverlayNative.MinMaxInfo));
                if(limits.MinTrackSize.Y!=13||limits.MinTrackSize.X!=50||limits.MaxTrackSize.X<=3000)throw new Exception("Native tracking limits including halo");
                window.CoreSize=new System.Drawing.Size(444,3);Application.DoEvents();if(window.CoreSize.Height!=3||window.ClientSize.Height!=13)throw new Exception("Real window/client minimum height with halo: window="+window.Size+", client="+window.ClientSize);
                if((OverlayNative.GetWindowLong(handle,-16)&0x40000)!=0)throw new Exception("System sizing frame must be absent");
                window.BeginResize(11,new System.Drawing.Point(0,0));window.ResizeTo(new System.Drawing.Point(200,0));window.EndResize();window.Activate();OverlayNative.RaiseWithoutFocus(handle);Application.DoEvents();
                if(window.CoreSize.Height!=3||window.CoreSize.Width!=644)throw new Exception("Resize/focus/topmost must preserve slim thickness");
                window.BeginResize(15,new System.Drawing.Point(0,0));window.ResizeTo(new System.Drawing.Point(0,-100));window.EndResize();Application.DoEvents();
                if(window.CoreSize.Height!=3||window.CoreSize.Width!=644||window.Capture)throw new Exception("Thickness resize minimum and mouse release");
                var stable=window.CoreBounds;for(int i=0;i<20;i++){window.renderKey="";window.RenderNow();}if(window.CoreBounds!=stable)throw new Exception("Repeated halo drawing must not grow or shift window geometry");
                var cfg=window.Configuration;cfg.Options.Enabled=false;window.ApplyConfiguration(cfg);Application.DoEvents();
                if(window.Visible||window.motion.Opacity!=0)throw new Exception("Disabling must hide the actual overlay window");
                ((ToolStripMenuItem)window.ContextMenuStrip.Items[1]).PerformClick();Application.DoEvents();
                if(window.Visible)throw new Exception("Global locking must not reveal a disabled widget");
                window.OpenSettings();Application.DoEvents();
                if(!window.settingsWindow.IsVisible||window.Visible)throw new Exception("Settings must remain available with energy bar disabled");
                cfg=window.Configuration;cfg.Options.Enabled=true;cfg.Options.AutoHide=false;cfg.Options.Fade=false;window.ApplyConfiguration(cfg);Application.DoEvents();
                if(!window.Visible||window.CoreBounds!=stable||!window.settingsWindow.IsVisible)throw new Exception("Re-enabling must restore the existing bar geometry while Settings stays open");
                int uploads=window.image.Uploads,presentations=window.image.Presentations;window.motion.Opacity=.5;window.RenderNow();
                if(window.image.Uploads!=uploads||window.image.Presentations!=presentations+1)throw new Exception("Fade-only updates must reuse the existing emissive bitmap");
                window.motion.Opacity=1;window.RenderNow();uploads=window.image.Uploads;presentations=window.image.Presentations;
                for(int i=0;i<100;i++)window.RenderNow();if(window.image.Uploads!=uploads||window.image.Presentations!=presentations)throw new Exception("Unchanged bar must neither redraw nor upload");
                using(var process=Process.GetCurrentProcess()){
                    uint before=OverlayNative.GetGuiResources(process.Handle,0);for(int i=0;i<2000;i++){window.motion.Value=.2+(i%701)/1000.0;window.RenderNow();}
                    uint after=OverlayNative.GetGuiResources(process.Handle,0);if(after>before+2)throw new Exception("Repeated native image replacement must not leak GDI handles: "+before+" -> "+after);
                }
            }finally{Marshal.FreeHGlobal(buffer);window.Close();}
        }
    }
}
