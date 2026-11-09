using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

internal sealed class BuffOverlayOptions {
    internal const int WarningSeconds=4;
    public bool Enabled,Blur=true,Names=true,Own=true,Party=true,ShowTimer=true,WarnBeforeExpiry=true;
    public int X=780,Y=670,Scale=100,Gap=5,Grow=3,Opacity=100;
    public int Style;
    internal BuffOverlayOptions Copy(){return (BuffOverlayOptions)MemberwiseClone();}
    internal void Normalize(){
        X=SkillSlotsOptions.Clamp(X,-100000,100000);Y=SkillSlotsOptions.Clamp(Y,-100000,100000);
        Scale=SkillSlotsOptions.Clamp(Scale,50,180);Gap=SkillSlotsOptions.Clamp(Gap,0,40);
        Grow=SkillSlotsOptions.Clamp(Grow,0,3);Opacity=SkillSlotsOptions.Clamp(Opacity,10,100);
        Style=SkillSlotsOptions.Clamp(Style,0,1);
    }
    internal static BuffOverlayOptions Read(Dictionary<string,object> settings){
        object raw;var result=new BuffOverlayOptions();
        if(settings.TryGetValue("buffOverlay",out raw)&&raw is Dictionary<string,object>)
            try{result=new JavaScriptSerializer().ConvertToType<BuffOverlayOptions>(raw)??result;}
            catch(ArgumentException){}catch(InvalidOperationException){}
        result.Normalize();
        return result;
    }
    internal static bool Same(BuffOverlayOptions a,BuffOverlayOptions b){
        return a.Enabled==b.Enabled&&a.Blur==b.Blur&&a.Names==b.Names&&a.Own==b.Own&&a.Party==b.Party&&
            a.Style==b.Style&&a.ShowTimer==b.ShowTimer&&a.WarnBeforeExpiry==b.WarnBeforeExpiry&&
            a.X==b.X&&a.Y==b.Y&&a.Scale==b.Scale&&a.Gap==b.Gap&&a.Grow==b.Grow&&a.Opacity==b.Opacity;
    }
    internal bool Vertical {get{return Grow>=2;}}
    internal bool Reverse {get{return Grow==1||Grow==3;}}
    internal bool Compact {get{return Style==1;}}
    internal Size Card {get{return new Size((Names?152:44)*Scale/100,44*Scale/100);}}
    internal int Spacing {get{return Gap*Scale/100;}}
    internal Rectangle Layout(int count){
        var card=Card;
        int w=Vertical?card.Width:count*card.Width+(count-1)*Spacing;
        int h=Vertical?count*card.Height+(count-1)*Spacing:card.Height;
        return new Rectangle(X-(Grow==1?w-card.Width:0),Y-(Grow==3?h-card.Height:0),w,h);
    }
}

internal sealed class BuffOverlay : IDisposable {
    readonly EnergyOverlay owner;
    readonly CombatOverlayState signal;
    readonly MovableOverlayWindow window=new MovableOverlayWindow("Buffs");
    readonly SkillSlotAssets icons=new SkillSlotAssets();
    readonly CrispOverlayText caption=new CrispOverlayText(),counter=new CrispOverlayText();
    internal int NextInterval {get;private set;}
    bool testing,disposed;
    double testStart,nextFrame;
    BuffOverlayOptions previous;
    bool wasEditing,wasHidden,wasConnected;
    string previousLanguage;
    internal bool Testing {get{return testing;}}
    internal BuffOverlay(EnergyOverlay owner,CombatOverlayState signal){
        this.owner=owner;this.signal=signal;
        window.SnapOwner=owner;window.ResizeAllowed=false;
        window.Moved=delegate(Rectangle moved){
            var cfg=owner.Configuration;
            var options=cfg.BuffOverlay;
            options.X=moved.X+(options.Grow==1?moved.Width-options.Card.Width:0);
            options.Y=moved.Y+(options.Grow==3?moved.Height-options.Card.Height:0);
            owner.ApplyConfiguration(cfg);
        };
    }
    internal void ToggleTest(){testing=!testing;testStart=DpsHistory.UtcNow();nextFrame=0;}
    internal void StopTest(){testing=false;nextFrame=0;}
    internal void Maintain(BuffOverlayOptions o,bool editing,bool hidden,bool connected,double now,string language){
        if(disposed)return;
        if(!testing&&(!o.Enabled||hidden||!connected&&!editing)){NextInterval=1000;window.HideOverlay();wasHidden=true;return;}
        if(o==previous&&!wasHidden&&editing==wasEditing&&connected==wasConnected&&language==previousLanguage&&now<nextFrame)return;
        previous=o;previousLanguage=language;wasHidden=false;wasEditing=editing;wasConnected=connected;nextFrame=now+.125;
        NextInterval=1000;
        var effects=connected?signal.ReadBuffs(now,o.Own,o.Party):new CombatOverlayState.Effect[0];
        if(testing||editing&&effects.Length==0){
            double elapsed=testing?(now-testStart)%14:0;
            effects=new[]{
                new CombatOverlayState.Effect{Skill=15310000,Ends=now+12-elapsed%12},
                new CombatOverlayState.Effect{Skill=15200000,Ends=now+4-elapsed%4,Party=true},
                new CombatOverlayState.Effect{Skill=15400000,Ends=0}};
        }
        if(effects.Length==0){NextInterval=250;window.HideOverlay();return;}
        var layout=o.Layout(effects.Length);
        var key=new StringBuilder().Append(layout.Size).Append('/').Append(o.Scale).Append('/').Append(o.Names)
            .Append('/').Append(o.Style).Append('/').Append(o.ShowTimer).Append('/').Append(o.WarnBeforeExpiry)
            .Append('/').Append(o.Grow).Append('/').Append(o.Spacing).Append('/').Append(language).Append('/').Append(editing);
        foreach(var effect in effects){
            int left=Seconds(effect,now);
            key.Append('/').Append(effect.Skill).Append('/').Append(effect.Party);
            if(o.ShowTimer)key.Append('/').Append(left);
            else if(o.WarnBeforeExpiry)key.Append('/').Append(Ending(left));
        }
        NextInterval=125;
        window.Render(layout,(byte)Math.Round(o.Opacity*255/100.0),editing,key.ToString(),
            size=>Paint(size,o,effects,now,language,editing),o.Blur&&!o.Compact);
    }

    static int Seconds(CombatOverlayState.Effect effect,double now){
        return effect.Ends==0?-1:Math.Max(0,(int)Math.Ceiling(effect.Ends-now));
    }
    static bool Ending(int seconds){return seconds>=0&&seconds<=BuffOverlayOptions.WarningSeconds;}
    static string TimeLabel(int seconds){
        if(seconds<0)return "";
        if(seconds>=3600)return (seconds/3600).ToString(CultureInfo.InvariantCulture)+"h";
        if(seconds>=60)return (seconds/60).ToString(CultureInfo.InvariantCulture)+":"+(seconds%60).ToString("00",CultureInfo.InvariantCulture);
        return seconds.ToString(CultureInfo.InvariantCulture);
    }
    internal Bitmap Preview(BuffOverlayOptions o,string language){
        var sample=o.Copy();
        sample.Grow=0;
        sample.Scale=Math.Min(sample.Scale,100);
        var effects=new[]{
            new CombatOverlayState.Effect{Skill=15310000,Ends=12},
            new CombatOverlayState.Effect{Skill=15110000,Ends=3,Party=true}};
        return Paint(sample.Layout(effects.Length).Size,sample,effects,0,language,false,false);
    }
    Bitmap Paint(Size size,BuffOverlayOptions o,CombatOverlayState.Effect[] effects,double now,string language,bool editing,bool updateBackdrop=true){
        var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
        if(updateBackdrop)window.BackdropTiles=new Rectangle[effects.Length];
        using(var g=Graphics.FromImage(image)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            var cell=o.Card;
            int pad=Math.Max(1,(int)Math.Round(cell.Height*(o.Compact?.055:.15)));
            var outline=new TextOutlineOptions{Enabled=true,Width=1,Opacity=85};
            for(int i=0;i<effects.Length;i++){
                int index=o.Reverse?effects.Length-1-i:i;
                int x=o.Vertical?0:index*(cell.Width+o.Spacing);
                int y=o.Vertical?index*(cell.Height+o.Spacing):0;
                var rect=new Rectangle(x,y,cell.Width,cell.Height);
                var plate=new Rectangle(x+1,y+1,cell.Width-2,cell.Height-2);
                if(updateBackdrop)window.BackdropTiles[i]=plate;
                var effect=effects[i];
                int seconds=Seconds(effect,now);
                bool ending=o.WarnBeforeExpiry&&Ending(seconds);
                using(var shape=SkillSlotPainter.Round(plate,cell.Height*.14f)){
                    if(o.Compact)SkillSlotPainter.PaintCompactPlate(g,shape,plate);
                    else{
                        using(var fill=new LinearGradientBrush(plate,Color.FromArgb(124,241,251,255),Color.FromArgb(37,195,225,234),90f))
                            g.FillPath(fill,shape);
                        using(var edge=new Pen(effect.Party?Color.FromArgb(205,158,240,220):Color.FromArgb(195,235,248,255)))
                            g.DrawPath(edge,shape);
                    }
                }
                var iconRect=new Rectangle(x+pad,y+pad,cell.Height-2*pad,cell.Height-2*pad);
                var icon=icons.Icon(effect.Skill);
                if(icon!=null){
                    var state=g.Save();
                    using(var clip=SkillSlotPainter.Round(iconRect,Math.Max(2,cell.Height*(o.Compact?.105f:.08f)))){g.SetClip(clip);g.DrawImage(icon,iconRect);}
                    g.Restore(state);
                }else caption.Draw(g,"?",Math.Max(11,cell.Height/3),true,Color.White,iconRect,1,true,outline);
                if(ending){
                    var warning=new Rectangle(x+2,y+2,cell.Height-4,cell.Height-4);
                    using(var rim=SkillSlotPainter.Round(warning,cell.Height*.12f))
                    using(var red=new Pen(Color.FromArgb(255,255,63,63),Math.Max(2,cell.Height*.055f)))g.DrawPath(red,rim);
                }
                string time=o.ShowTimer?TimeLabel(seconds):"";
                if(time.Length>0){
                    int font=Math.Max(6,Math.Min(cell.Height/3,(int)(iconRect.Width/(time.Length*.66))));
                    counter.Draw(g,time,font,true,Color.White,iconRect,1,true,outline);
                }
                if(o.Names){
                    int labelPadding=Math.Max(4,cell.Height/9);
                    int left=x+cell.Height+labelPadding,w=rect.Right-left-labelPadding;
                    caption.Draw(g,SkillCatalogue.Shared.Name(effect.Skill,language),Math.Max(8,cell.Height/4),true,Color.White,
                        new Rectangle(left,y,w,cell.Height),0,true,outline);
                }
            }
            if(editing)using(var border=new Pen(Color.FromArgb(155,104,221,209))){
                border.DashStyle=DashStyle.Dot;g.DrawRectangle(border,0,0,size.Width-1,size.Height-1);
            }
        }
        return image;
    }
    internal void VerifyAppearance(){
        var options=new BuffOverlayOptions{Grow=0,Names=true};
        var effects=new[]{new CombatOverlayState.Effect{Skill=15310000,Ends=4}};
        var size=options.Layout(effects.Length).Size;
        using(var glass=Paint(size,options,effects,0,"es",false)){
            options.Style=1;
            using(var compact=Paint(size,options,effects,0,"es",false)){
                if(glass.GetPixel(80,4).ToArgb()==compact.GetPixel(80,4).ToArgb()||options.Layout(1).Size!=size)
                    throw new Exception("Buff style changes appearance while retaining placement");
                options.ShowTimer=false;
                using(var noTimer=Paint(size,options,effects,0,"es",false)){
                    bool changed=false;
                    for(int y=6;y<38&&!changed;y++)for(int x=6;x<38&&!changed;x++)
                        changed=compact.GetPixel(x,y).ToArgb()!=noTimer.GetPixel(x,y).ToArgb();
                    if(!changed||compact.GetPixel(2,22).ToArgb()!=noTimer.GetPixel(2,22).ToArgb())
                        throw new Exception("The timer draws inside the icon independently of the expiry border");
                    options.WarnBeforeExpiry=false;
                    using(var noBorder=Paint(size,options,effects,0,"es",false)){
                        Color red=noTimer.GetPixel(2,22),normal=noBorder.GetPixel(2,22);
                        if(red.R<220||red.G>=100||normal.ToArgb()==red.ToArgb())
                            throw new Exception("The red expiry border is optional");
                    }
                }
            }
        }
        options.Names=false;
        if(options.Card.Width!=options.Card.Height)throw new Exception("Names can be hidden to keep only the square buttons");
        effects=new[]{effects[0],effects[0]};
        using(Paint(options.Layout(2).Size,options,effects,0,"es",false)){
            if(window.BackdropTiles[0].Right>=window.BackdropTiles[1].Left)
                throw new Exception("Buff backdrops must leave the gap between tiles clear");
        }
    }
    public void Dispose(){if(disposed)return;disposed=true;window.Dispose();caption.Dispose();counter.Dispose();icons.Dispose();}
}
