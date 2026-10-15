using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Forms;

internal sealed class TextOutlineOptions {
    public bool Enabled=false;
    public int Width=1,Opacity=100;
    internal TextOutlineOptions Copy(){return (TextOutlineOptions)MemberwiseClone();}
    internal void Normalize(){Width=Math.Max(1,Math.Min(8,Width));Opacity=Math.Max(0,Math.Min(100,Opacity));}
    internal int Padding {get{return Enabled&&Opacity>0?Width:0;}}
    internal string Key {get{return Enabled+"/"+Width+"/"+Opacity;}}
    internal static bool Same(TextOutlineOptions a,TextOutlineOptions b){return a==null||b==null?a==b:a.Enabled==b.Enabled&&a.Width==b.Width&&a.Opacity==b.Opacity;}
}

internal sealed class EnemyNameOptions {
    public bool Enabled=true,Bold=true,Marker=true;
    public int FontSize=21,Position=0,OffsetX=-5,OffsetY=-12,LayoutScalePercent=100;
    public string Color="#FFA992";
    public TextOutlineOptions Outline=new TextOutlineOptions();
    internal EnemyNameOptions Copy(){var copy=(EnemyNameOptions)MemberwiseClone();copy.Outline=Outline==null?new TextOutlineOptions():Outline.Copy();return copy;}
    internal void Normalize(){if(Outline==null)Outline=new TextOutlineOptions();Outline.Normalize();LayoutScalePercent=Math.Max(25,Math.Min(900,LayoutScalePercent));FontSize=Math.Max(10,Math.Min(192,FontSize));Position=Math.Max(0,Math.Min(5,Position));OffsetX=Math.Max(-1500,Math.Min(1500,OffsetX));OffsetY=Math.Max(-1500,Math.Min(1500,OffsetY));if(!EnergyBarOptions.ValidColor(Color))Color="#FFA992";}
}

// An independent click-through text surface avoids clipping offsets to the bar
// window, and shares visibility/opacity without adding a capture or a timer.
internal sealed class EnemyNameOverlay:Form {
    readonly LayeredImage image=new LayeredImage();readonly CrispOverlayText crisp=new CrispOverlayText();string key;Rectangle presented;byte alpha;bool disposed;long nextRaise;
    internal EnemyNameOverlay(){FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;Text="Aion 2 Helper - Enemy name";}
    internal static Rectangle CaptionBounds(Rectangle core,EnemyNameOptions options){double scale=options.LayoutScalePercent/100.0;int height=options.FontSize+(int)Math.Round(12*scale);int x=core.X+options.OffsetX,y=(options.Position<3?core.Y-height:core.Bottom+(int)Math.Round(8*scale))+options.OffsetY;var bounds=new Rectangle(x,y,core.Width+(int)Math.Round(10*scale),height);int pad=options.Outline.Padding;bounds.Inflate(pad,pad);return bounds;}
    internal static void Draw(Graphics g,Rectangle bounds,EnemyNameOptions options,string name,bool locked,CrispOverlayText workspace=null){
        g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        int padding=options.Outline.Padding;bounds.Inflate(-padding,-padding);
        float scale=options.LayoutScalePercent/100f;int reserve=locked&&options.Position<3?(int)Math.Round(65*scale):0;var text=new RectangleF(bounds.X+20*scale,bounds.Y,Math.Max(20,bounds.Width-25*scale-reserve),bounds.Height);
        if(options.Marker)using(var font=new Font("Segoe UI",options.FontSize,options.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var format=new StringFormat{Alignment=options.Position%3==0?StringAlignment.Near:options.Position%3==1?StringAlignment.Center:StringAlignment.Far,FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter}){
            var measured=g.MeasureString(name,font,new SizeF(text.Width,text.Height),format);float marker=options.Position%3==0?bounds.X+5*scale:options.Position%3==1?text.X+(text.Width-measured.Width)/2-15*scale:text.Right-measured.Width-15*scale;float center=bounds.Y+(options.FontSize+7)/2f;float diamond=6*scale;
            var color=ColorTranslator.FromHtml(options.Color);if(options.Marker)using(var brush=new SolidBrush(color))g.FillPolygon(brush,new[]{new PointF(marker,center-diamond),new PointF(marker+diamond,center),new PointF(marker,center+diamond),new PointF(marker-diamond,center)});if(options.Marker)using(var brush=new SolidBrush(Color.FromArgb(255,25,29,31)))g.FillPolygon(brush,new[]{new PointF(marker,center-diamond/3),new PointF(marker+diamond/3,center),new PointF(marker,center+diamond/3),new PointF(marker-diamond/3,center)});
        }
        bool own=workspace==null;if(own)workspace=new CrispOverlayText();try{workspace.Draw(g,name,options.FontSize,options.Bold,ColorTranslator.FromHtml(options.Color),Rectangle.Round(text),options.Position%3,false,options.Outline);}finally{if(own)workspace.Dispose();}
    }
    internal void Update(Rectangle core,EnemyNameOptions options,string name,bool locked,byte opacity){if(disposed)return;if(!options.Enabled||opacity==0||string.IsNullOrEmpty(name)||DpsDesign.ShellUiActive()){Clear();return;}var bounds=CaptionBounds(core,options);if(Bounds!=bounds)Bounds=bounds;string next=name+"/"+locked+"/"+bounds.Size+"/"+options.FontSize+"/"+options.Position+"/"+options.Color+"/"+options.Bold+"/"+options.Marker+"/"+options.LayoutScalePercent+"/"+options.Outline.Key;if(key!=next){key=next;using(var bitmap=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);Draw(g,new Rectangle(Point.Empty,bounds.Size),options,name,locked,crisp);image.Upload(bitmap);}alpha=0;}if(alpha!=opacity||presented!=bounds||!Visible){image.Present(Handle,Left,Top,opacity);alpha=opacity;presented=bounds;}if(!Visible){Show();nextRaise=0;}long now=Stopwatch.GetTimestamp();if(now>=nextRaise){nextRaise=now+Stopwatch.Frequency;OverlayNative.RaiseWithoutFocus(Handle);}}
    internal void Clear(){if(Visible)Hide();}
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x20|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;crisp.Dispose();image.Dispose();}base.Dispose(disposing);}
}
