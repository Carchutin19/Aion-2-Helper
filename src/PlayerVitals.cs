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

internal sealed class VitalTextOptions {
    public bool Enabled=true,Bold=true;
    public int FontSize=16,Position=3,OffsetX,OffsetY;
    public string Color="#EEEAE1";
    public TextOutlineOptions Outline=new TextOutlineOptions{Enabled=true,Width=1,Opacity=85};
    internal VitalTextOptions Copy(){var c=(VitalTextOptions)MemberwiseClone();c.Outline=Outline==null?new TextOutlineOptions():Outline.Copy();return c;}
    internal void Normalize(){FontSize=Math.Max(10,Math.Min(96,FontSize));Position=Math.Max(0,Math.Min(5,Position));OffsetX=Math.Max(-600,Math.Min(600,OffsetX));OffsetY=Math.Max(-600,Math.Min(600,OffsetY));if(!EnergyBarOptions.ValidColor(Color))Color="#EEEAE1";if(Outline==null)Outline=new TextOutlineOptions();Outline.Normalize();}
}

internal sealed class PlayerShieldOptions {
    public bool Enabled=true,Emissive=true;
    public string Color="#E5B447";
    public int Opacity=100,GlowPercent=65,PreviewPercent=25;
    internal PlayerShieldOptions Copy(){return (PlayerShieldOptions)MemberwiseClone();}
    internal void Normalize(){if(!EnergyBarOptions.ValidColor(Color))Color="#E5B447";Opacity=Math.Max(0,Math.Min(100,Opacity));GlowPercent=Math.Max(0,Math.Min(200,GlowPercent));PreviewPercent=Math.Max(0,Math.Min(100,PreviewPercent));}
}

// Appearance preferences are independent from received character resources.
internal sealed class PlayerVitalOptions {
    public EnergyBarOptions Bar=new EnergyBarOptions{Enabled=false,AutoHide=false,DynamicColors=false,LowColor="#83D52B",MediumColor="#83D52B",HighColor="#83D52B",GlowPercent=65,TrackOpacity=165};
    public Rectangle Bounds=new Rectangle(500,850,420,12);
    public bool Border=true,MaximumBesideCurrent=true;
    public int BorderWidth=1,BorderOpacity=85,Opacity=100,PreviewPercent=80;
    public double DisplayScale=1;
    public string BorderColor="#B4A68D";
    public PlayerShieldOptions Shield=new PlayerShieldOptions();
    public VitalTextOptions Label=new VitalTextOptions(),Current=new VitalTextOptions{FontSize=24,OffsetX=40},Maximum=new VitalTextOptions{Bold=false,Color="#CFCBBF"},Percentage=new VitalTextOptions{Position=5};
    internal static PlayerVitalOptions Defaults(bool mana){var o=new PlayerVitalOptions();if(mana){o.Bar.LowColor=o.Bar.MediumColor=o.Bar.HighColor="#E94ABC";o.Bounds=new Rectangle(980,850,420,12);o.PreviewPercent=72;}return o;}
    internal PlayerVitalOptions Copy(){return new PlayerVitalOptions{Bar=Bar.Copy(),Bounds=Bounds,Border=Border,MaximumBesideCurrent=MaximumBesideCurrent,BorderWidth=BorderWidth,BorderOpacity=BorderOpacity,Opacity=Opacity,PreviewPercent=PreviewPercent,DisplayScale=DisplayScale,BorderColor=BorderColor,Shield=Shield==null?new PlayerShieldOptions():Shield.Copy(),Label=Label.Copy(),Current=Current.Copy(),Maximum=Maximum.Copy(),Percentage=Percentage.Copy()};}
    internal void Normalize(){if(Shield==null)Shield=new PlayerShieldOptions();Shield.Normalize();if(Bar==null)Bar=new EnergyBarOptions{Enabled=false};Bar.Normalize();Bar.ScaleReference=null;Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),Math.Max(80,Math.Min(1600,Bounds.Width)),Math.Max(6,Math.Min(80,Bounds.Height)));BorderWidth=Math.Max(1,Math.Min(4,BorderWidth));BorderOpacity=Math.Max(0,Math.Min(100,BorderOpacity));Opacity=Math.Max(0,Math.Min(100,Opacity));PreviewPercent=Math.Max(0,Math.Min(100,PreviewPercent));if(double.IsNaN(DisplayScale)||double.IsInfinity(DisplayScale))DisplayScale=1;DisplayScale=Math.Max(.25,Math.Min(4,DisplayScale));if(!EnergyBarOptions.ValidColor(BorderColor))BorderColor="#B4A68D";if(Label==null)Label=new VitalTextOptions();if(Current==null)Current=new VitalTextOptions();if(Maximum==null)Maximum=new VitalTextOptions();if(Percentage==null)Percentage=new VitalTextOptions();foreach(var text in Texts)text.Normalize();}
    internal VitalTextOptions Text(int i){return i==0?Label:i==1?Current:i==2?Maximum:Percentage;}
    internal VitalTextOptions[] Texts {get{return new[]{Label,Current,Maximum,Percentage};}}
    internal static PlayerVitalOptions Read(Dictionary<string,object> data,bool mana){var o=Defaults(mana);object value;if(data.TryGetValue(mana?"playerMana":"playerHealth",out value))try{o=new JavaScriptSerializer().ConvertToType<PlayerVitalOptions>(value)??o;}catch{}o.Normalize();return o;}
    internal static bool Same(PlayerVitalOptions a,PlayerVitalOptions b){return new JavaScriptSerializer().Serialize(a)==new JavaScriptSerializer().Serialize(b);}
    internal double Factor {get{return Bar.ScalePercent/100.0;}}
    internal Rectangle EffectiveBounds {get{int w=(int)Math.Round(Bounds.Width*Factor),h=Math.Max(6,(int)Math.Round(Bounds.Height*Factor));return new Rectangle(Bounds.X+Bounds.Width/2-w/2,Bounds.Y+Bounds.Height/2-h/2,w,h);}}
}

internal static class PlayerVitalDesign {
    internal sealed class Caption {internal Rectangle Bounds;internal VitalTextOptions Options;internal string Text;internal int Size,Outline,Index;}
    internal sealed class Layout {internal Rectangle Core,Surface;internal readonly List<Caption> Captions=new List<Caption>();internal double Scale;}
    internal sealed class Surface:IDisposable {internal Bitmap Bitmap;internal Rectangle Core;public void Dispose(){if(Bitmap!=null)Bitmap.Dispose();}}
    // Four bounded caption workspaces belong to the bar, rather than to an
    // animation frame. Each is resized when its actual glyph bounds change.
    internal sealed class Workspace:IDisposable {
        Bitmap measurement;Graphics graphics;readonly Font[] fonts=new Font[4];readonly int[] sizes=new int[4];readonly bool[] weights=new bool[4];
        readonly CrispOverlayText[] captions={new CrispOverlayText(),new CrispOverlayText(),new CrispOverlayText(),new CrispOverlayText()};
        readonly string[] measuredText=new string[4];readonly int[] measuredWidth=new int[4];
        sealed class Raster {internal bool Valid,Bold;internal string Text,Color;internal int Width,Height,Size,Padding,Opacity;}
        readonly Raster[] raster={new Raster(),new Raster(),new Raster(),new Raster()};
        internal Graphics Measurement {get{if(graphics==null){measurement=new Bitmap(1,1);graphics=Graphics.FromImage(measurement);}return graphics;}}
        internal Font FontFor(int index,int size,bool bold){if(fonts[index]==null||sizes[index]!=size||weights[index]!=bold){if(fonts[index]!=null)fonts[index].Dispose();fonts[index]=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);sizes[index]=size;weights[index]=bold;measuredText[index]=null;}return fonts[index];}
        internal int TextWidth(int index,string text,int size,bool bold){var font=FontFor(index,size,bold);if(measuredText[index]!=text){measuredWidth[index]=TextRenderer.MeasureText(Measurement,text,font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine).Width+3;measuredText[index]=text;}return measuredWidth[index];}
        internal void DrawCaption(Graphics destination,Caption caption){int i=caption.Index;var cached=raster[i];var o=caption.Options;int pad=o.Outline.Enabled&&o.Outline.Opacity>0?Math.Min(8,caption.Outline):0;
            // Very large user-selected fonts still render at their requested
            // size, but their glyph buffers must not become a permanent cache.
            bool retain=(long)(caption.Bounds.Width+2*pad)*(caption.Bounds.Height+2*pad)<=65536;if(!retain)cached.Valid=false;
            if(!cached.Valid||cached.Text!=caption.Text||cached.Color!=o.Color||cached.Width!=caption.Bounds.Width||cached.Height!=caption.Bounds.Height||cached.Size!=caption.Size||cached.Bold!=o.Bold||cached.Padding!=pad||cached.Opacity!=o.Outline.Opacity){
                var outline=o.Outline.Copy();outline.Width=Math.Min(8,caption.Outline);captions[i].Draw(destination,caption.Text,caption.Size,o.Bold,ColorTranslator.FromHtml(o.Color),caption.Bounds,0,false,outline);
                cached.Valid=retain;cached.Text=caption.Text;cached.Color=o.Color;cached.Width=caption.Bounds.Width;cached.Height=caption.Bounds.Height;cached.Size=caption.Size;cached.Bold=o.Bold;cached.Padding=pad;cached.Opacity=o.Outline.Opacity;if(!retain)captions[i].Dispose();
            }else captions[i].Present(destination,caption.Bounds,pad);
        }
        internal void Clear(){for(int i=0;i<4;i++){captions[i].Dispose();raster[i].Valid=false;measuredText[i]=null;if(fonts[i]!=null){fonts[i].Dispose();fonts[i]=null;}}}
        public void Dispose(){Clear();if(graphics!=null){graphics.Dispose();graphics=null;}if(measurement!=null){measurement.Dispose();measurement=null;}}
    }
    static Color Mix(Color a,Color b,double f){return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*f),(int)Math.Round(a.G+(b.G-a.G)*f),(int)Math.Round(a.B+(b.B-a.B)*f));}
    static int Metric(int value,double scale){return (int)Math.Round(value*scale);}
    static Rectangle CaptionArea(Caption caption){var area=caption.Bounds;int pad=(caption.Options.Outline.Enabled?Math.Min(8,caption.Outline):0)+2;area.Inflate(pad,pad);return area;}
    internal static Layout Measure(PlayerVitalOptions o,bool mana,double ratio,uint? actualValue=null,uint? actualMaximum=null,Workspace workspace=null){
        var work=workspace??new Workspace();try{
        var result=new Layout{Core=new Rectangle(Point.Empty,o.EffectiveBounds.Size),Scale=o.Factor*o.DisplayScale};int glow=Math.Max(8,Metric(10,result.Scale));result.Surface=result.Core;result.Surface.Inflate(glow,glow);
        uint maximum=actualMaximum??(mana?5000u:15000u),current=actualValue??(uint)Math.Round(maximum*Math.Max(0,Math.Min(1,ratio)));double textRatio=actualValue.HasValue&&maximum>0?(double)current/maximum:ratio;string[] words={mana?"MP":"HP",current.ToString("N0",CultureInfo.InvariantCulture).Replace(","," "),"/ "+maximum.ToString("N0",CultureInfo.InvariantCulture).Replace(","," "),(100*textRatio).ToString("0",CultureInfo.InvariantCulture)+"%"};var texts=o.Texts;int row=0;
        foreach(var text in texts)if(text.Enabled&&text.Position>=3)row=Math.Max(row,Math.Max(6,Math.Min(384,Metric(text.FontSize,result.Scale)))+8);
        Caption actual=null;
        // Measure on the same bitmap DPI as CrispOverlayText, not the desktop DC:
        // monitor scaling must not inflate the caption width a second time.
        for(int i=0;i<texts.Length;i++){var t=texts[i];if(!t.Enabled)continue;int size=Math.Max(6,Math.Min(384,Metric(t.FontSize,result.Scale))),height=size+8;int width=work.TextWidth(i,words[i],size,t.Bold);
            int x=t.Position%3==0?0:t.Position%3==1?(result.Core.Width-width)/2:result.Core.Width-width;int y=t.Position<3?-height-5:result.Core.Height+5+row-height;x+=Metric(t.OffsetX,result.Scale);y+=Metric(t.OffsetY,result.Scale);
            if(i==2&&o.MaximumBesideCurrent&&actual!=null){x=actual.Bounds.Right+Metric(8+t.OffsetX,result.Scale);y=actual.Bounds.Bottom-height+Metric(t.OffsetY,result.Scale);}
            else if(i==2&&o.MaximumBesideCurrent&&!o.Current.Enabled&&t.Position%3==0)x=Math.Max(x,Metric(o.Current.OffsetX,result.Scale));
            var caption=new Caption{Bounds=new Rectangle(x,y,width,height),Options=t,Text=words[i],Size=size,Outline=Math.Max(1,Math.Min(24,Metric(t.Outline.Width,result.Scale))),Index=i};
            bool above=t.Position<3;int gap=Math.Max(2,Metric(3,result.Scale));
            // Keep authored positions when they fit. Narrow bars wrap captions instead
            // of shrinking their chosen fonts or persisting automatic adjustments.
            if(i==2&&o.MaximumBesideCurrent&&actual!=null){
                above=o.Current.Position<3;
                if(caption.Bounds.Right>result.Core.Right){
                    caption.Bounds=new Rectangle(Math.Max(0,Math.Min(actual.Bounds.X,result.Core.Width-width)),above?actual.Bounds.Top-height-gap:actual.Bounds.Bottom+gap,width,height);
                }
            }
            for(int attempt=0;attempt<result.Captions.Count;attempt++){
                var area=CaptionArea(caption);int shift=0;
                foreach(var prior in result.Captions){var occupied=CaptionArea(prior);if(!area.IntersectsWith(occupied))continue;shift=above?Math.Min(shift,occupied.Top-gap-area.Bottom):Math.Max(shift,occupied.Bottom+gap-area.Top);}
                if(shift==0)break;caption.Bounds.Offset(0,shift);
            }
            result.Captions.Add(caption);if(i==1)actual=caption;
        }foreach(var caption in result.Captions)result.Surface=Rectangle.Union(result.Surface,CaptionArea(caption));return result;
        }finally{if(workspace==null)work.Dispose();}
    }
    internal static Surface Render(PlayerVitalOptions o,bool mana,double ratio,bool editing,uint? actual=null,uint? maximum=null,double shieldRatio=0,Workspace workspace=null){
        var work=workspace??new Workspace();try{
        ratio=Math.Max(0,Math.Min(1,ratio));var layout=Measure(o,mana,ratio,actual,maximum,work);int width=Math.Max(1,Math.Min(8192,layout.Surface.Width)),height=Math.Max(1,Math.Min(2048,layout.Surface.Height));var result=new Surface{Bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb),Core=new Rectangle(-layout.Surface.X,-layout.Surface.Y,layout.Core.Width,layout.Core.Height)};
        using(var g=Graphics.FromImage(result.Bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(-layout.Surface.X,-layout.Surface.Y);if(editing)using(var brush=new SolidBrush(Color.FromArgb(1,0,0,0)))g.FillRectangle(brush,layout.Surface);
            var core=layout.Core;var color=BarDesign.EnergyColor(ratio,o.Bar);int border=o.Border?Math.Min(Math.Max(1,core.Height/3),Math.Max(1,Metric(o.BorderWidth,layout.Scale))):0;float inset=Math.Max(1,Math.Min(core.Height/3f,Math.Max(2,border+1)));var inner=new RectangleF(inset,inset,Math.Max(1,core.Width-2*inset),Math.Max(1,core.Height-2*inset));
            if(o.Bar.Emissive&&ratio>0&&o.Bar.GlowPercent>0)for(int i=Math.Max(2,Metric(8,layout.Scale));i>=1;i--){var halo=new RectangleF(-i,-i,(float)(core.Width*ratio)+2*i,core.Height+2*i);int alpha=Math.Max(1,Math.Min(45,(int)(o.Bar.GlowPercent*.12/(1+i*.3))));using(var brush=new SolidBrush(Color.FromArgb(alpha,color)))using(var shape=FpsDesign.Round(halo.X,halo.Y,halo.Width,halo.Height,halo.Height/2))g.FillPath(brush,shape);}
            using(var track=new SolidBrush(Color.FromArgb(o.Bar.TrackOpacity,12,15,16)))using(var shape=FpsDesign.Round(0,0,core.Width,core.Height,core.Height/2f))g.FillPath(track,shape);
            if(ratio>0){float filled=(float)(inner.Width*ratio);using(var clip=FpsDesign.Round(inner.X,inner.Y,inner.Width,inner.Height,inner.Height/2)){var saved=g.Save();g.SetClip(clip);using(var fill=new LinearGradientBrush(inner,Mix(color,Color.White,.18),Mix(color,Color.Black,.22),LinearGradientMode.Vertical))using(var shape=FpsDesign.Round(inner.X,inner.Y,filled,inner.Height,inner.Height/2))g.FillPath(fill,shape);if(o.Bar.Emissive)using(var filament=new Pen(Mix(color,Color.White,.8),Math.Max(1,inner.Height/4))){filament.StartCap=filament.EndCap=LineCap.Round;if(filled>2)g.DrawLine(filament,inner.X+1,inner.Y+inner.Height/2,inner.X+filled-1,inner.Y+inner.Height/2);}g.Restore(saved);}}
            // Independent capacity layer over HP; no separate frame, maximum or
            // extra width. Clip the emissive strokes to the same capsule as HP.
            if(!mana&&o.Shield.Enabled&&o.Shield.Opacity>0&&shieldRatio>0){
                float filled=(float)(inner.Width*Math.Max(0,Math.Min(1,shieldRatio)));var gold=ColorTranslator.FromHtml(o.Shield.Color);int a=o.Shield.Opacity*255/100;
                using(var clip=FpsDesign.Round(inner.X,inner.Y,inner.Width,inner.Height,inner.Height/2)){var saved=g.Save();g.SetClip(clip);
                    using(var fill=new LinearGradientBrush(inner,Color.FromArgb(a,Mix(gold,Color.White,.22)),Color.FromArgb(a,Mix(gold,Color.Black,.18)),LinearGradientMode.Vertical))g.FillRectangle(fill,inner.X,inner.Y,filled,inner.Height);
                    if(o.Shield.Emissive&&o.Shield.GlowPercent>0){var glowState=g.Save();g.SetClip(new RectangleF(inner.X,inner.Y,filled,inner.Height),CombineMode.Intersect);for(int i=3;i>=1;i--)using(var glow=new Pen(Color.FromArgb(Math.Min(a,(int)(a*o.Shield.GlowPercent/200.0/(i+2))),Mix(gold,Color.White,.8)),Math.Max(1,inner.Height*i/3)))g.DrawLine(glow,inner.X,inner.Y+inner.Height/2,inner.X+filled,inner.Y+inner.Height/2);g.Restore(glowState);}
                    g.Restore(saved);
                }
            }
            if(o.Border)using(var pen=new Pen(Color.FromArgb(o.BorderOpacity*255/100,ColorTranslator.FromHtml(o.BorderColor)),border))using(var shape=FpsDesign.Round(border/2f,border/2f,core.Width-border,core.Height-border,(core.Height-border)/2f))g.DrawPath(pen,shape);
            foreach(var caption in layout.Captions)work.DrawCaption(g,caption);
            if(editing)using(var pen=new Pen(Color.FromArgb(100,170,180,185))){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,-2,-2,core.Width+4,core.Height+4);}
        }return result;
        }finally{if(workspace==null)work.Dispose();}
    }
}

internal sealed class PlayerVitalOverlay:Form {
    readonly BarMotion motion=new BarMotion();readonly PlayerVitalDesign.Workspace workspace=new PlayerVitalDesign.Workspace();string identityFlow;uint identityActor;bool identitySet,identityKnown,hasData;uint? currentValue,maximumValue,paintedCurrent,paintedMaximum;uint shieldValue;int paintedFill,paintedColor,paintedShield;bool paintedEditing;EnergyBarOptions shieldVisible;
    readonly EnergyOverlay owner;readonly bool mana,preview;readonly LayeredImage image=new LayeredImage();PlayerVitalOptions options;bool editing,released,dragging,valid;string key;Rectangle original,coreInSurface;Point mouse,presented;int hit;long nextRaise;byte alpha;
    internal PlayerVitalOverlay(EnergyOverlay owner,bool mana,bool preview){this.owner=owner;this.mana=mana;this.preview=preview;options=PlayerVitalOptions.Defaults(mana);FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;Text="Aion 2 Helper - Player "+(mana?"MP":"HP")+" preview";}
    internal object Status {get{return new{enabled=options.Bar.Enabled,visible=Visible,editing=editing,visualPreviewOnly=editing&&!hasData,hasGameData=hasData,current=currentValue,maximum=maximumValue,shield=mana?(uint?)null:shieldValue,dataState=owner.PlayerVitals.Diagnostics(mana,DpsHistory.UtcNow()),timer=false,uploads=image.Uploads};}}
    internal string StatusKey {get{return options.Bar.Enabled+"/"+Visible+"/"+editing+"/"+hasData+"/"+currentValue+"/"+maximumValue+"/"+shieldValue;}}
    internal int NextInterval {get{if(!options.Bar.Enabled)return int.MaxValue;if(motion.Animating)return 16;if(motion.FullSince.HasValue&&motion.Opacity>0){double remaining=options.Bar.HoldSeconds-(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency-motion.FullSince.Value);if(remaining>0)return Math.Min(250,Math.Max(1,(int)Math.Ceiling(remaining*1000)));}return 250;}}
    internal void Configure(PlayerVitalOptions next,bool locked){options=next.Copy();options.Normalize();shieldVisible=options.Bar.Copy();shieldVisible.AutoHide=false;editing=!locked;int style=OverlayNative.GetWindowLong(Handle,-20);OverlayNative.SetWindowLong(Handle,-20,editing?style&~0x20:style|0x20);var appearance=options.Copy();appearance.Bounds=new Rectangle(Point.Empty,appearance.Bounds.Size);appearance.Opacity=100;string nextKey=new JavaScriptSerializer().Serialize(appearance);if(nextKey!=key){key=nextKey;valid=false;workspace.Clear();}Maintain();}
    internal void Maintain(){if(released)return;double utc=DpsHistory.UtcNow(),now=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;var sample=owner.PlayerVitals.Read(mana,utc);hasData=sample!=null;currentValue=hasData?(uint?)sample.Current:null;maximumValue=hasData?(uint?)sample.Maximum:null;shieldValue=hasData&&!mana?sample.Shield:0;double shieldRatio=mana||!options.Shield.Enabled?0:hasData?(double)shieldValue/sample.Maximum:editing?options.Shield.PreviewPercent/100.0:0;
        double ratio=hasData?(double)sample.Current/sample.Maximum:options.PreviewPercent/100.0;uint actual=hasData?sample.Current:(uint)Math.Round(ratio*100),max=hasData?sample.Maximum:100;string nextFlow=hasData?sample.Flow:null;uint nextActor=hasData?sample.Actor:0;
        if(!identitySet||identityKnown!=hasData||identityFlow!=nextFlow||identityActor!=nextActor){identitySet=true;identityKnown=hasData;identityFlow=nextFlow;identityActor=nextActor;motion.SnapValue(ratio);}motion.Step(now,editing,hasData||editing,ratio,actual,max,shieldRatio>0&&options.Shield.Opacity>0?shieldVisible:options.Bar,hasData?Math.Max(0,utc-sample.Time):0);
        byte opacity=(byte)Math.Round(motion.Opacity*options.Opacity*255/100);if(!options.Bar.Enabled||opacity==0||DpsDesign.ShellUiActive()){if(Visible)Hide();return;}
        var core=options.EffectiveBounds;int fill=(int)Math.Round(motion.Value*core.Width),color=BarDesign.EnergyColor(motion.Value,options.Bar).ToArgb(),shield=(int)Math.Round(Math.Min(1,shieldRatio)*core.Width);
        if(!valid||paintedFill!=fill||paintedColor!=color||paintedCurrent!=currentValue||paintedMaximum!=maximumValue||paintedEditing!=editing||paintedShield!=shield){using(var surface=PlayerVitalDesign.Render(options,mana,motion.Value,editing,currentValue,maximumValue,shieldRatio,workspace)){coreInSurface=surface.Core;Bounds=new Rectangle(core.X-coreInSurface.X,core.Y-coreInSurface.Y,surface.Bitmap.Width,surface.Bitmap.Height);image.Upload(surface.Bitmap);}valid=true;paintedFill=fill;paintedColor=color;paintedCurrent=currentValue;paintedMaximum=maximumValue;paintedEditing=editing;paintedShield=shield;alpha=0;}
        var at=new Point(core.X-coreInSurface.X,core.Y-coreInSurface.Y);if(Location!=at)Location=at;if(alpha!=opacity||presented!=at||!preview&&!Visible){image.Present(Handle,Left,Top,opacity);alpha=opacity;presented=at;}if(!preview&&!Visible)Show();long tick=Stopwatch.GetTimestamp();if(!preview&&tick>=nextRaise){nextRaise=tick+Stopwatch.Frequency;OverlayNative.RaiseWithoutFocus(Handle);}
    }
    internal void VerifySafety(){var o=PlayerVitalOptions.Defaults(mana);o.Bar.Enabled=true;Configure(o,true);if(Visible||image.Uploads!=0)throw new Exception("Locked bar cannot fabricate game values");Configure(o,false);int count=image.Uploads;for(int i=0;i<1000;i++)Maintain();if(image.Uploads!=count)throw new Exception("Static editing samples must not redraw on unchanged ticks");Configure(o,true);if(Visible)throw new Exception("Locking clears unknown sample");o.Bar.Enabled=false;Configure(o,false);if(Visible)throw new Exception("Disabled sample stays hidden");
        owner.PlayerVitals.Configure(true);var profile=EnemyTargetVerification.Player(13351,"Charlotta",12683,12683,true);owner.PlayerVitals.Consume(profile,0,profile.Length,DpsHistory.UtcNow(),"A");o.Bar.Enabled=true;o.Bar.Fade=o.Bar.Smooth=o.Bar.AutoHide=false;Configure(o,true);if(!hasData||!currentValue.HasValue||maximumValue!=(mana?4591u:12683u)||image.Uploads<=count)throw new Exception("Locked live bars must render actual resources");count=image.Uploads;for(int i=0;i<1000;i++)Maintain();if(image.Uploads!=count)throw new Exception("Unchanged live resources must reuse bitmap");
        if(!mana){
            o.Bar.AutoHide=true;o.Bar.HoldSeconds=0;Configure(o,true);Maintain();if(motion.Opacity!=0)throw new Exception("Full HP respects automatic hiding before shield");
            double time=DpsHistory.UtcNow();var status=PlayerShieldVerification.Status(13351,671);var tick=PlayerShieldVerification.Tick(13351,671,2935);owner.PlayerVitals.Consume(status,0,status.Length,time,"A");owner.PlayerVitals.Consume(tick,0,tick.Length,time,"A");Maintain();if(shieldValue!=2935||motion.Opacity!=1||motion.FullSince.HasValue)throw new Exception("Active shield reveals full HP without changing actual HP");count=image.Uploads;for(int i=0;i<1000;i++)Maintain();if(image.Uploads!=count)throw new Exception("Unchanged shield must reuse bitmap");
            o.Shield.Enabled=false;Configure(o,true);Maintain();if(motion.Opacity!=0)throw new Exception("Disabled shield cannot override full HP auto-hide");o.Shield.Enabled=true;Configure(o,true);var removal=PlayerShieldVerification.Remove(13351,671);owner.PlayerVitals.Consume(removal,0,removal.Length,DpsHistory.UtcNow(),"A");Maintain();if(shieldValue!=0||motion.Opacity!=0)throw new Exception("Removal immediately clears shield and restores full HP auto-hide");
        }
        owner.PlayerVitals.Reset();Maintain();if(hasData||currentValue.HasValue||Visible)throw new Exception("Transport reset removes live values immediately");owner.PlayerVitals.Configure(false);
    }
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x80000|0x80|0x08000000)&~0x40000;return cp;}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if((m.Msg==0xa5||m.Msg==0x205)&&editing){owner.OpenTrayMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x84){if(!editing){m.Result=new IntPtr(-1);return;}var p=PointToClient(new Point((short)(m.LParam.ToInt64()&65535),(short)((m.LParam.ToInt64()>>16)&65535)));m.Result=new IntPtr(coreInSurface.Contains(p)?BarDesign.HitTest(p.X-coreInSurface.X,p.Y-coreInSurface.Y,coreInSurface.Width,coreInSurface.Height,3):2);return;}
        if(m.Msg==0xa1&&editing){hit=m.WParam.ToInt32();if(hit==2||hit>=10&&hit<=17){original=options.Bounds;mouse=Cursor.Position;dragging=true;Capture=true;m.Result=IntPtr.Zero;return;}}
        if(m.Msg==0x200&&dragging){var delta=new Point(Cursor.Position.X-mouse.X,Cursor.Position.Y-mouse.Y);if(hit==2)options.Bounds=new Rectangle(original.X+delta.X,original.Y+delta.Y,original.Width,original.Height);else{var start=options.Copy();start.Bounds=original;var effective=start.EffectiveBounds;var resized=BarDesign.ResizeBounds(effective,delta,hit);int w=Math.Max(80,Math.Min(1600,(int)Math.Round(resized.Width/options.Factor))),h=Math.Max(6,Math.Min(80,(int)Math.Round(resized.Height/options.Factor)));options.Bounds=new Rectangle(resized.X+resized.Width/2-w/2,resized.Y+resized.Height/2-h/2,w,h);}if(hit!=2)valid=false;Maintain();m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0xa2||m.Msg==0x215)&&dragging){dragging=false;Capture=false;var c=owner.Configuration;if(mana)c.PlayerMana=options.Copy();else c.PlayerHealth=options.Copy();owner.ApplyConfiguration(c);m.Result=IntPtr.Zero;return;}base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing&&!released){released=true;workspace.Dispose();image.Dispose();}base.Dispose(disposing);}
}
