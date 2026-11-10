using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;

// Reference glyph matching for the native item quickbar. Match the fixed key glyphs
// rather than the menu background. Only scores leave the sampler.
internal sealed class NativeHudVisibility : IDisposable {
    public sealed class Layout {
        public int Width {get;set;}
        public int Height {get;set;}
        public int Left {get;set;}
        public int Top {get;set;}
        public int[] Offsets {get;set;}
        public byte[][] Glyphs {get;set;}
    }

    const int GlyphWidth=10, GlyphHeight=7, GlyphSize=GlyphWidth*GlyphHeight, Padding=2;
    sealed class Reading {
        internal IntPtr Window;
        internal int Generation;
        internal int? Matches;
        internal bool SearchComplete;
    }
    readonly Layout layout;
    readonly byte[] pixels;
    readonly int stripWidth;
    readonly double[][] templates;
    readonly object gate=new object();
    IntPtr window, workerWindow;
    IntPtr memory, bitmap, original, bits;
    IntPtr scanMemory,scanBitmap,scanOriginal,scanBits;
    byte[] scanPixels;
    int left,right,top,scanWidth,scanHeight;
    readonly int[] bright=new int[8],dark=new int[8];
    double nextSample,nextSearch;
    bool searching,busy,disposed;
    Reading pending;
    int shownSamples,missingSamples,generation;
    bool armed,hidden,watching;
    internal bool Calibrated {get{return layout!=null;}}
    internal int? Matches {get;private set;}

    internal NativeHudVisibility(string root,bool preview) {
        if(preview)return;
        string file=Path.Combine(root,"native-hud-calibration.json");
        try {
            var info=new FileInfo(file);
            string reference;
            if(info.Exists&&info.Length<=24000)reference=File.ReadAllText(file);
            else using(var input=typeof(NativeHudVisibility).Assembly.GetManifestResourceStream("NativeKeyGlyphs.json")){
                if(input==null)return;
                using(var reader=new StreamReader(input))reference=reader.ReadToEnd();
            }
            var candidate=new JavaScriptSerializer().Deserialize<Layout>(reference);
            if(candidate==null||candidate.Width<640||candidate.Width>7680||
               candidate.Height<480||candidate.Height>4320||candidate.Left<0||candidate.Top<0||
               candidate.Top>candidate.Height-GlyphHeight||candidate.Offsets==null||candidate.Offsets.Length!=8||
               candidate.Glyphs==null||candidate.Glyphs.Length!=candidate.Offsets.Length)return;
            var prepared=new double[candidate.Offsets.Length][];
            int right=0;
            for(int i=0;i<candidate.Offsets.Length;i++){
                int x=candidate.Offsets[i];
                if(x<right||x>512||candidate.Glyphs[i]==null||candidate.Glyphs[i].Length!=GlyphSize)return;
                right=x+GlyphWidth;
                prepared[i]=Normalize(candidate.Glyphs[i]);
                if(prepared[i]==null)return;
            }
            if(candidate.Offsets[0]!=0||candidate.Offsets[4]-candidate.Offsets[3]<GlyphWidth+2*Padding||
               candidate.Left<Padding||candidate.Left>candidate.Width-right-Padding)return;
            stripWidth=right;
            pixels=new byte[(stripWidth+2*Padding)*GlyphHeight*4];
            templates=prepared;
            layout=candidate;
            ResetPosition();
            for(int i=0;i<templates.Length;i++){
                bright[i]=Extreme(candidate.Glyphs[i],true);
                dark[i]=Extreme(candidate.Glyphs[i],false);
            }
        }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }

    internal bool Update(double now,bool check) {
        if(!Calibrated)return false;
        if(!check){
            if(watching){generation++;watching=false;lock(gate)pending=null;}
            hidden=searching=false;shownSamples=missingSamples=0;nextSample=nextSearch=0;Matches=null;
            return false;
        }
        IntPtr target=GetForegroundWindow();
        watching=true;
        if(target!=window){
            window=target;armed=hidden=searching=false;shownSamples=missingSamples=0;
            generation++;
            nextSearch=nextSample=0;Matches=null;
        }
        lock(gate){
            if(pending!=null){
                if(pending.Window==window&&pending.Generation==generation)ApplyReading(pending);
                pending=null;
            }
            if(!busy&&!disposed&&window!=IntPtr.Zero&&now>=nextSample){
                bool search=missingSamples>=2&&now>=nextSearch;
                if(search)nextSearch=now+(hidden?2:.75);
                nextSample=now+.25;
                busy=true;
                int epoch=generation;
                ThreadPool.QueueUserWorkItem(delegate{ReadWindow(target,search,epoch);});
            }
        }
        return hidden;
    }

    void ApplyReading(Reading value) {
        Matches=value.Matches;
        if(Matches>=4){
            if(shownSamples<2)shownSamples++;
            armed|=shownSamples>=2;
        }else shownSamples=0;
        if(Matches.HasValue&&Matches<4){
            if(missingSamples<3)missingSamples++;
            searching|=value.SearchComplete;
            hidden=Matches<=1&&armed&&searching&&missingSamples>=3;
        }else{
            missingSamples=0;
            searching=hidden=false;
        }
    }

    void ResetPosition(){left=layout.Left;right=left+layout.Offsets[4];top=layout.Top;}

    void ReadWindow(IntPtr target,bool search,int epoch) {
        var result=new Reading{Window=target,Generation=epoch};
        try{Sample(target,search,result);}
        catch(System.ComponentModel.Win32Exception){}
        finally{
            lock(gate){
                if(disposed)ReleaseSurfaces();
                else pending=result;
                busy=false;
            }
        }
    }

    static int Extreme(byte[] glyph,bool maximum) {
        int best=0;
        for(int i=1;i<glyph.Length;i++)if(maximum?glyph[i]>glyph[best]:glyph[i]<glyph[best])best=i;
        return best;
    }

    static double[] Normalize(byte[] glyph) {
        double total=0,squares=0;
        foreach(byte value in glyph){total+=value;squares+=value*value;}
        double spread=Math.Sqrt(squares-total*total/GlyphSize);
        if(spread<8)return null;
        var result=new double[GlyphSize];
        for(int i=0;i<GlyphSize;i++)result[i]=(glyph[i]-total/GlyphSize)/spread;
        return result;
    }

    void Sample(IntPtr target,bool search,Reading result) {
        if(target!=GetForegroundWindow())return;
        if(target!=workerWindow){workerWindow=target;ResetPosition();}
        Rect size;
        var origin=new OverlayNative.Point();
        if(!GetClientRect(target,out size)||size.Right<640||size.Bottom<480||
           size.Right>7680||size.Bottom>4320||!ClientToScreen(target,ref origin))return;
        int width=size.Right,height=size.Bottom;
        if((long)width*height>8294400)return;
        if(scanWidth!=width||scanHeight!=height){
            ReleaseSurface(ref scanMemory,ref scanBitmap,ref scanOriginal,ref scanBits);scanPixels=null;
            scanWidth=width;scanHeight=height;
            left=Math.Max(Padding,Math.Min(left,width-stripWidth-Padding));
            right=left+layout.Offsets[4];top=Math.Min(top,height-GlyphHeight);
        }
        IntPtr screen=OverlayNative.GetDC(IntPtr.Zero);
        if(screen==IntPtr.Zero)return;
        try {
            int paddedWidth=stripWidth+2*Padding,group=layout.Offsets[4];
            if(memory==IntPtr.Zero&&!CreateSurface(screen,paddedWidth,GlyphHeight,out memory,out bitmap,out original,out bits))return;
            if(right==left+group){
                if(!ReadPixels(memory,bits,screen,origin.X+left-Padding,origin.Y+top,paddedWidth,GlyphHeight,pixels))return;
            }else{
                int firstWidth=layout.Offsets[3]+GlyphWidth+2*Padding;
                if(!BitBlt(memory,0,0,firstWidth,GlyphHeight,screen,origin.X+left-Padding,origin.Y+top,0x00cc0020)||
                   !BitBlt(memory,group,0,paddedWidth-group,GlyphHeight,screen,origin.X+right-Padding,origin.Y+top,0x00cc0020))return;
                GdiFlush();
                Marshal.Copy(bits,pixels,0,pixels.Length);
            }
            int total;
            int matches=CountMatches(pixels,Padding*4,paddedWidth*4,0,8,Padding,out total);
            if(search&&matches<4&&width*height<=8294400){
                if(scanMemory==IntPtr.Zero&&CreateSurface(screen,width,height,out scanMemory,out scanBitmap,out scanOriginal,out scanBits))
                    scanPixels=new byte[width*height*4];
                if(scanPixels!=null&&ReadPixels(scanMemory,scanBits,screen,origin.X,origin.Y,width,height,scanPixels)){
                    result.SearchComplete=true;
                    int x,secondX,y;
                    if(Locate(scanPixels,width,height,top,out x,out secondX,out y,out matches)){
                        left=x;right=secondX;top=y;total=1;
                    }
                }
            }
            if(target==GetForegroundWindow())result.Matches=total==0?(int?)null:matches;
        }finally{OverlayNative.ReleaseDC(IntPtr.Zero,screen);}
    }

    static bool ReadPixels(IntPtr memory,IntPtr bits,IntPtr screen,int x,int y,int width,int height,byte[] output) {
        if(!BitBlt(memory,0,0,width,height,screen,x,y,0x00cc0020))return false;
        GdiFlush();
        Marshal.Copy(bits,output,0,output.Length);
        return true;
    }

    int CountMatches(byte[] data,int start,int stride,int begin,int end,int padding,out int total) {
        int matches=0;total=0;
        for(int i=begin;i<end;i++){
            int p=start+(layout.Offsets[i]-layout.Offsets[begin])*4;
            int light;
            if(GlyphScore(data,p,stride,i,out light)>=.82)matches++;
            else for(int shift=-padding;shift<=padding;shift++){
                int unused;
                if(shift!=0&&GlyphScore(data,p+4*shift,stride,i,out unused)>=.82){matches++;break;}
            }
            total+=light;
        }
        return matches;
    }

    double GlyphScore(byte[] data,int start,int stride,int index,out int light) {
        double dot=0,sum=0,squares=0;
        var glyph=templates[index];
        for(int row=0;row<GlyphHeight;row++)for(int column=0;column<GlyphWidth;column++){
            int p=start+row*stride+column*4;
            int value=(29*data[p]+150*data[p+1]+77*data[p+2])>>8;
            sum+=value;squares+=value*value;
            dot+=glyph[row*GlyphWidth+column]*value;
        }
        light=(int)sum;
        double spread=Math.Sqrt(squares-sum*sum/GlyphSize);
        return spread>=8?dot/spread:0;
    }

    bool Locate(byte[] data,int width,int height,int startY,out int x,out int secondX,out int y,out int count) {
        x=secondX=y=count=0;
        int stride=width*4;
        int[] brightBytes=new int[8],darkBytes=new int[8];
        for(int i=0;i<templates.Length;i++){
            int group=i<4?0:4;
            int offset=(layout.Offsets[i]-layout.Offsets[group])*4;
            brightBytes[i]=offset+PixelOffset(bright[i],stride);
            darkBytes[i]=offset+PixelOffset(dark[i],stride);
        }
        int maxY=height-GlyphHeight;
        for(int step=0;step<=2*maxY;step++){
            int row=startY+((step&1)==0?-(step/2):(step+1)/2);
            if(row<0||row>maxY)continue;
            int first=FindGroup(data,row*stride,stride,width,0,1,2,Padding,brightBytes,darkBytes);
            if(first<0)continue;
            int next=FindGroup(data,row*stride,stride,width,4,4,6,first+layout.Offsets[3]+GlyphWidth,brightBytes,darkBytes);
            if(next<0)continue;
            int unused;
            count=CountMatches(data,row*stride+first*4,stride,0,4,Padding,out unused)+
                CountMatches(data,row*stride+next*4,stride,4,8,Padding,out unused);
            if(count>=4){x=first;secondX=next;y=row;return true;}
        }
        return false;
    }

    int FindGroup(byte[] data,int line,int stride,int width,int group,int probe1,int probe2,int start,int[] brightBytes,int[] darkBytes) {
        int groupWidth=layout.Offsets[group+3]-layout.Offsets[group]+GlyphWidth;
        for(int x=start;x<=width-groupWidth-Padding;x++){
            int at=line+x*4;
            if(Probe(data,at,stride,group,probe1,brightBytes,darkBytes)&&
               Probe(data,at,stride,group,probe2,brightBytes,darkBytes))return x;
        }
        return -1;
    }

    bool Probe(byte[] data,int at,int stride,int group,int index,int[] brightBytes,int[] darkBytes) {
        for(int shift=-Padding;shift<=Padding;shift++){
            int offset=at+shift*4;
            if(data[offset+brightBytes[index]]-data[offset+darkBytes[index]]<70)continue;
            int light;
            if(GlyphScore(data,offset+(layout.Offsets[index]-layout.Offsets[group])*4,stride,index,out light)>=.91)return true;
        }
        return false;
    }

    static int PixelOffset(int i,int stride){return i/GlyphWidth*stride+i%GlyphWidth*4+1;}

    static bool CreateSurface(IntPtr screen,int width,int height,out IntPtr memory,out IntPtr bitmap,out IntPtr original,out IntPtr bits) {
        bitmap=original=bits=IntPtr.Zero;
        memory=OverlayNative.CreateCompatibleDC(screen);
        if(memory==IntPtr.Zero)return false;
        var info=new BitmapInfo{Size=40,Width=width,Height=-height,Planes=1,Bits=32};
        bitmap=CreateDIBSection(memory,ref info,0,out bits,IntPtr.Zero,0);
        if(bitmap!=IntPtr.Zero)original=OverlayNative.SelectObject(memory,bitmap);
        if(original!=IntPtr.Zero&&original!=new IntPtr(-1))return true;
        ReleaseSurface(ref memory,ref bitmap,ref original,ref bits);
        return false;
    }

    static void ReleaseSurface(ref IntPtr memory,ref IntPtr bitmap,ref IntPtr original,ref IntPtr bits) {
        if(original!=IntPtr.Zero&&original!=new IntPtr(-1))OverlayNative.SelectObject(memory,original);
        if(bitmap!=IntPtr.Zero)OverlayNative.DeleteObject(bitmap);
        if(memory!=IntPtr.Zero)OverlayNative.DeleteDC(memory);
        memory=bitmap=original=bits=IntPtr.Zero;
    }
    void ReleaseSurfaces(){
        ReleaseSurface(ref memory,ref bitmap,ref original,ref bits);
        ReleaseSurface(ref scanMemory,ref scanBitmap,ref scanOriginal,ref scanBits);
        scanPixels=null;
    }
    public void Dispose(){lock(gate){disposed=true;if(!busy)ReleaseSurfaces();}}

    internal static void Verify(string root) {
        var fixture=new Layout{Width=720,Height=480,Left=51,Top=416,Offsets=new[]{0,31,62,93,124,155,186,217},Glyphs=new byte[8][]};
        var random=new Random(23);
        for(int i=0;i<fixture.Glyphs.Length;i++){
            fixture.Glyphs[i]=new byte[GlyphSize];
            random.NextBytes(fixture.Glyphs[i]);
        }
        string folder=Path.Combine(root,"hud-visibility-fixture-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,"native-hud-calibration.json");
        try{
            using(var defaults=new NativeHudVisibility(folder,false))if(!defaults.Calibrated)throw new Exception("Public build must contain its HUD reference without a private calibration file");
            File.WriteAllText(file,new JavaScriptSerializer().Serialize(fixture));
            using(var detector=new NativeHudVisibility(folder,false)){
                int x,secondX,y,count;
                var scene=new byte[fixture.Width*fixture.Height*4];
                if(!detector.Calibrated||detector.Locate(scene,fixture.Width,fixture.Height,fixture.Top,out x,out secondX,out y,out count))
                    throw new Exception("An empty screen must not produce a quickbar");
                const int newX=109,newY=279,gap=75;
                for(int i=0;i<fixture.Glyphs.Length;i++)for(int row=0;row<GlyphHeight;row++)for(int column=0;column<GlyphWidth;column++){
                    int p=((newY+row)*fixture.Width+newX+fixture.Offsets[i]+column+(i<4?0:gap))*4;
                    scene[p]=scene[p+1]=scene[p+2]=fixture.Glyphs[i][row*GlyphWidth+column];
                }
                if(!detector.Locate(scene,fixture.Width,fixture.Height,fixture.Top,out x,out secondX,out y,out count)||
                   Math.Abs(x-newX)>Padding||Math.Abs(secondX-newX-fixture.Offsets[4]-gap)>Padding||y!=newY||count!=8)
                    throw new Exception("Changing HUD preset must allow the quickbar to be reacquired at its new position");
                detector.ApplyReading(new Reading{Matches=8});detector.ApplyReading(new Reading{Matches=8});
                for(int i=0;i<3;i++)detector.ApplyReading(new Reading{Matches=0});
                if(detector.hidden)throw new Exception("Losing a calibrated location alone must not hide the widgets");
                detector.ApplyReading(new Reading{Matches=0,SearchComplete=true});
                if(!detector.hidden)throw new Exception("Confirmed quickbar absence must hide gameplay widgets");
                detector.ApplyReading(new Reading{Matches=count,SearchComplete=true});
                if(detector.hidden)throw new Exception("Rediscovered quickbar must restore the widgets");
            }
        }finally{File.Delete(file);Directory.Delete(folder);}
    }

    [StructLayout(LayoutKind.Sequential)] struct Rect {internal int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct BitmapInfo {
        internal int Size,Width,Height;
        internal short Planes,Bits;
        internal int Compression,ImageSize,XPels,YPels,Used,Important;
    }
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window,ref OverlayNative.Point point);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr pixels,IntPtr section,uint offset);
    [DllImport("gdi32.dll")] static extern bool GdiFlush();
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst,int x,int y,int w,int h,IntPtr src,int sx,int sy,uint op);
}

internal sealed class NativeHudFade {
    const double Duration=.18;
    double started,from=1,target=1,value=1;
    internal byte Alpha {get;private set;}
    internal bool Animating {get{return value!=target;}}
    internal NativeHudFade(){Alpha=255;}
    internal bool Step(double now,bool hide,bool animate) {
        byte previous=Alpha;
        double next=hide?0:1;
        if(!animate){
            value=from=target=next;
        }else{
            double amount=Math.Max(0,Math.Min(1,(now-started)/Duration));
            value=from+(target-from)*amount*amount*(3-2*amount);
            if(target!=next){from=value;target=next;started=now;}
        }
        Alpha=(byte)Math.Round(value*255);
        return previous!=Alpha;
    }
    internal byte Apply(byte opacity){return (byte)((opacity*Alpha+127)/255);}

    internal static void Verify(){
        var fade=new NativeHudFade();
        fade.Step(1,true,true);
        fade.Step(1.09,true,true);
        if(fade.Alpha!=127&&fade.Alpha!=128||!fade.Animating||fade.Apply(160)<79||fade.Apply(160)>81)
            throw new Exception("Native menu must fade relative to the configured opacity");
        byte half=fade.Alpha;
        fade.Step(1.09,false,true);
        if(fade.Alpha!=half)throw new Exception("Reversing the fade cannot jump in opacity");
        fade.Step(1.3,false,true);
        if(fade.Alpha!=255||fade.Animating)throw new Exception("Closing the menu restores the widget opacity");
        fade.Step(2,true,true);
        fade.Step(2.2,true,true);
        if(fade.Alpha!=0||fade.Animating)throw new Exception("An open native menu must fully hide the widgets");
        fade.Step(3,false,false);
        if(fade.Alpha!=255||fade.Animating)throw new Exception("Edit mode or focus loss cancels the menu fade");
    }
}
