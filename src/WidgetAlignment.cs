using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal interface IWidgetSnapTarget {
    Rectangle SnapBounds { get; }
}

internal static class WidgetAlignment {
    internal const int Distance = 12;

    internal static Point Find(Rectangle moving,IEnumerable<Rectangle> targets,int gap,int distance,out bool snappedX,out bool snappedY) {
        int bestX=distance+1,bestY=distance+1,dx=0,dy=0;
        foreach(var target in targets) {
            if(target.Width<=0||target.Height<=0)continue;
            if(moving.Bottom+distance+gap>=target.Top&&moving.Top-distance-gap<=target.Bottom)
                Closest(target.Left,moving.Left,target.Right,moving.Right,gap,ref bestX,ref dx);
            if(moving.Right+distance+gap>=target.Left&&moving.Left-distance-gap<=target.Right)
                Closest(target.Top,moving.Top,target.Bottom,moving.Bottom,gap,ref bestY,ref dy);
        }
        snappedX=bestX<=distance;
        snappedY=bestY<=distance;
        return new Point(moving.X+dx,moving.Y+dy);
    }

    static void Closest(int near,int start,int far,int end,int gap,ref int best,ref int offset) {
        Consider(near-start,ref best,ref offset);
        Consider(far-end,ref best,ref offset);
        Consider(far+gap-start,ref best,ref offset);
        Consider(near-gap-end,ref best,ref offset);
        Consider(near+(far-near)/2-start-(end-start)/2,ref best,ref offset);
    }

    static void Consider(int delta,ref int best,ref int offset) {
        int distance=Math.Abs(delta);
        if(distance>=best)return;
        best=distance;
        offset=delta;
    }

    internal static IEnumerable<Rectangle> VisibleTargets(Form moving,bool skipHotbar) {
        foreach(Form window in Application.OpenForms) {
            var target=window as IWidgetSnapTarget;
            if(target!=null&&window!=moving&&window.Visible&&!(skipHotbar&&window is SkillSlotWindow))
                yield return target.SnapBounds;
        }
    }

    internal static void Verify() {
        var target=new Rectangle(-340,180,100,50);
        bool x,y;
        var aligned=Find(new Rectangle(-245,182,40,20),new[]{target},0,Distance,out x,out y);
        if(aligned!=new Point(-240,180)||!x||!y)
            throw new Exception("Global snap must align an adjacent edge and a shared top on a secondary display");
        aligned=Find(new Rectangle(-243,236,40,20),new[]{target},0,Distance,out x,out y);
        if(aligned!=new Point(-240,230)||!x||!y)
            throw new Exception("Global snap must also align stacked widgets");
        aligned=Find(new Rectangle(-235,180,40,20),new[]{target},5,Distance,out x,out y);
        if(aligned!=new Point(-235,180)||!x||!y)
            throw new Exception("Exact hotbar alignment must reserve both axes even with zero movement");
        aligned=Find(new Rectangle(-227,182,40,20),new[]{target},0,Distance,out x,out y);
        if(aligned!=new Point(-227,182)||x||y)
            throw new Exception("Widgets outside the snap radius must stay at the raw position");
        if(WidgetGrid.Alpha(0)!=0||WidgetGrid.Alpha(100)!=255||WidgetGrid.Alpha(WidgetGrid.DefaultOpacity)!=140)
            throw new Exception("Grid opacity must cover the full range and preserve the default center-line alpha");
        foreach(var size in new[]{new Size(1920,1080),new Size(900,1600)}) {
            using(var grid=WidgetGrid.Draw(size)) {
                int cx=size.Width/2,cy=size.Height/2;
                int step=(int)Math.Round(Math.Min(size.Width,size.Height)/48.0);
                int minor=grid.GetPixel(cx+step,cy+3).A;
                if(minor==0||grid.GetPixel(cx,cy+3).A<=minor||grid.GetPixel(cx+3,cy).A<=minor||
                    grid.GetPixel(cx+3,cy+3).A!=0)
                    throw new Exception("Grid must be transparent with stronger center axes in landscape and portrait");
            }
        }
    }
}

internal sealed class WidgetGrid : IDisposable {
    internal const int DefaultOpacity=55;
    readonly List<GridWindow> windows=new List<GridWindow>();
    WidgetDisplayLayout.Area[] screens;
    int opacity;

    internal static int ClampOpacity(int value){return Math.Max(0,Math.Min(100,value));}
    internal static byte Alpha(int percent){return (byte)((ClampOpacity(percent)*255+50)/100);}

    internal void Sync(bool visible,WidgetDisplayLayout.Area[] areas,int percent) {
        if(!visible||percent==0){Dispose();return;}
        byte alpha=Alpha(percent);
        if(screens!=null&&WidgetDisplayLayout.Same(screens,areas)){
            if(opacity!=percent)foreach(var window in windows)window.Present(alpha);
            opacity=percent;
            return;
        }
        Dispose();
        IntPtr below=LowestWidget();
        foreach(var area in areas) {
            var window=new GridWindow(area.Bounds);
            windows.Add(window);
            window.ShowGrid(below,alpha);
        }
        screens=areas;
        opacity=percent;
    }

    public void Dispose() {
        foreach(var window in windows)window.Dispose();
        windows.Clear();
        screens=null;
    }

    [DllImport("user32.dll")] static extern IntPtr GetTopWindow(IntPtr parent);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);

    static IntPtr LowestWidget() {
        var widgets=new HashSet<IntPtr>();
        foreach(Form form in Application.OpenForms)
            if(form is IWidgetSnapTarget&&form.Visible&&form.IsHandleCreated&&form.TopMost)widgets.Add(form.Handle);
        var lowest=IntPtr.Zero;
        for(var window=GetTopWindow(IntPtr.Zero);window!=IntPtr.Zero&&widgets.Count>0;window=GetWindow(window,2))
            if(widgets.Remove(window))lowest=window;
        return lowest;
    }

    internal static Bitmap Draw(Size size) {
        int cx=size.Width/2,cy=size.Height/2;
        int step=Math.Max(12,(int)Math.Round(Math.Min(size.Width,size.Height)/48.0));
        var bitmap=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(bitmap))
        using(var minor=new Pen(Color.FromArgb(80,235,245,255)))
        using(var axis=new Pen(Color.FromArgb(255,235,245,255),2)) {
            for(int x=cx%step;x<size.Width;x+=step)
                if(x!=cx)g.DrawLine(minor,x,0,x,size.Height-1);
            for(int y=cy%step;y<size.Height;y+=step)
                if(y!=cy)g.DrawLine(minor,0,y,size.Width-1,y);
            g.DrawLine(axis,cx,0,cx,size.Height-1);
            g.DrawLine(axis,0,cy,size.Width-1,cy);
        }
        return bitmap;
    }

    sealed class GridWindow : Form {
        readonly Rectangle area;
        LayeredImage image=new LayeredImage();

        internal GridWindow(Rectangle bounds) {
            area=bounds;
            FormBorderStyle=FormBorderStyle.None;
            AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.Manual;
            ShowInTaskbar=false;
            TopMost=true;
            Bounds=bounds;
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get { var cp=base.CreateParams;cp.ExStyle|=0x80000|0x80|0x20|0x08000000;return cp; }
        }

        internal void ShowGrid(IntPtr below,byte alpha) {
            using(var bitmap=Draw(area.Size))image.Upload(bitmap);
            Present(alpha);
            Show();
            if(below!=IntPtr.Zero)OverlayNative.SetWindowPos(Handle,below,0,0,0,0,0x0013);
        }

        internal void Present(byte alpha){image.Present(Handle,area.X,area.Y,alpha);}

        protected override void Dispose(bool disposing) {
            if(disposing&&image!=null){image.Dispose();image=null;}
            base.Dispose(disposing);
        }
    }
}
