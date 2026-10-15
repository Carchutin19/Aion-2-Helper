using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// GDI grid-fits small glyphs to the final physical pixel size. Convert its RGB
// coverage to a grayscale alpha mask, so transparent overlays have no coloured
// ClearType fringes. The workspace is reused and redrawn only with changed text.
internal sealed class CrispOverlayText:IDisposable {
    Bitmap mask,shadow;Graphics graphics;byte[] pixels,coverage,horizontal;Font font;int width,height,size;bool bold;
    void Prepare(int w,int h,int fontSize,bool weight){if(mask==null||width!=w||height!=h){if(graphics!=null)graphics.Dispose();if(mask!=null)mask.Dispose();if(shadow!=null)shadow.Dispose();width=w;height=h;mask=new Bitmap(w,h,PixelFormat.Format32bppArgb);shadow=new Bitmap(w,h,PixelFormat.Format32bppArgb);graphics=Graphics.FromImage(mask);pixels=new byte[w*h*4];coverage=horizontal=null;}if(font==null||size!=fontSize||bold!=weight){if(font!=null)font.Dispose();size=fontSize;bold=weight;font=new Font("Segoe UI",fontSize,weight?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}}
    // Two bounded max-filter passes expand the existing crisp alpha mask.
    // Buffers are reused; the owning text surface calls Draw only on a visual change.
    void Outline(int stride,int radius,int opacity){
        int count=width*height;if(coverage==null||coverage.Length!=count){coverage=new byte[count];horizontal=new byte[count];}
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)coverage[y*width+x]=pixels[y*stride+x*4+3];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){byte value=0;for(int k=Math.Max(0,x-radius);k<=Math.Min(width-1,x+radius);k++)value=Math.Max(value,coverage[y*width+k]);horizontal[y*width+x]=value;}
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){byte value=0;for(int k=Math.Max(0,y-radius);k<=Math.Min(height-1,y+radius);k++)value=Math.Max(value,horizontal[k*width+x]);int at=y*stride+x*4;pixels[at]=pixels[at+1]=pixels[at+2]=0;pixels[at+3]=(byte)((value*opacity+50)/100);}
    }
    internal void Draw(Graphics destination,string text,int fontSize,bool weight,Color color,Rectangle bounds,int alignment,bool centered=false,TextOutlineOptions outline=null){
        if(bounds.Width<=0||bounds.Height<=0||string.IsNullOrEmpty(text))return;
        int pad=outline==null?0:outline.Padding;Prepare(bounds.Width+2*pad,bounds.Height+2*pad,fontSize,weight);graphics.Clear(Color.Black);
        var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|(centered?TextFormatFlags.VerticalCenter:TextFormatFlags.Top)|(alignment==1?TextFormatFlags.HorizontalCenter:alignment==2?TextFormatFlags.Right:TextFormatFlags.Left);
        TextRenderer.DrawText(graphics,text,font,new Rectangle(pad,pad,bounds.Width,bounds.Height),Color.White,flags);
        var area=new Rectangle(0,0,width,height);var data=mask.LockBits(area,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);int length=data.Stride*height,stride=data.Stride;
        try{Marshal.Copy(data.Scan0,pixels,0,length);for(int y=0;y<height;y++)for(int x=0;x<width;x++){int at=y*stride+x*4;byte alpha=(byte)((pixels[at]+pixels[at+1]+pixels[at+2]+1)/3);pixels[at]=color.B;pixels[at+1]=color.G;pixels[at+2]=color.R;pixels[at+3]=alpha;}Marshal.Copy(pixels,0,data.Scan0,length);}finally{mask.UnlockBits(data);}
        var shade=shadow.LockBits(area,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        try{if(pad>0)Outline(stride,pad,outline.Opacity);else for(int at=0;at<length;at+=4){pixels[at]=pixels[at+1]=pixels[at+2]=0;pixels[at+3]=(byte)(pixels[at+3]*.85);}Marshal.Copy(pixels,0,shade.Scan0,length);}finally{shadow.UnlockBits(shade);}
        destination.DrawImageUnscaled(shadow,bounds.X-pad,bounds.Y-pad+(pad==0?1:0));destination.DrawImageUnscaled(mask,bounds.X-pad,bounds.Y-pad);
    }
    public void Dispose(){if(graphics!=null)graphics.Dispose();if(mask!=null)mask.Dispose();if(shadow!=null)shadow.Dispose();if(font!=null)font.Dispose();graphics=null;mask=shadow=null;font=null;pixels=coverage=horizontal=null;}
}
