using System;
using System.Collections.Generic;
using System.Linq;
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
    public int Style,LayoutMode;
    public int BuffGrow=-1,DebuffGrow=-1;
    public bool DebuffsFirst;
    public bool ShowBuffs=true,ShowDebuffs=true,TypeBorders=true;
    public int MaximumIcons=24;
    internal static BuffOverlayOptions TargetDefaults(){return new BuffOverlayOptions{Enabled=true,Names=false,X=0,Y=28,Grow=0,Scale=85,LayoutMode=1};}
    internal BuffOverlayOptions Copy(){return (BuffOverlayOptions)MemberwiseClone();}
    internal void Normalize(){
        X=SkillSlotsOptions.Clamp(X,-100000,100000);Y=SkillSlotsOptions.Clamp(Y,-100000,100000);
        Scale=SkillSlotsOptions.Clamp(Scale,50,180);Gap=SkillSlotsOptions.Clamp(Gap,0,40);
        Grow=SkillSlotsOptions.Clamp(Grow,0,3);Opacity=SkillSlotsOptions.Clamp(Opacity,10,100);
        Style=SkillSlotsOptions.Clamp(Style,0,1);LayoutMode=SkillSlotsOptions.Clamp(LayoutMode,0,4);MaximumIcons=SkillSlotsOptions.Clamp(MaximumIcons,1,64);if(LayoutMode>=3)Grow=Grow%2;
        BuffGrow=SkillSlotsOptions.Clamp(BuffGrow,-1,3);DebuffGrow=SkillSlotsOptions.Clamp(DebuffGrow,-1,3);
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
        return a.DebuffsFirst==b.DebuffsFirst&&a.BuffGrow==b.BuffGrow&&a.DebuffGrow==b.DebuffGrow&&a.ShowBuffs==b.ShowBuffs&&a.ShowDebuffs==b.ShowDebuffs&&a.TypeBorders==b.TypeBorders&&a.MaximumIcons==b.MaximumIcons&&a.LayoutMode==b.LayoutMode&&a.Enabled==b.Enabled&&a.Blur==b.Blur&&a.Names==b.Names&&a.Own==b.Own&&a.Party==b.Party&&
            a.Style==b.Style&&a.ShowTimer==b.ShowTimer&&a.WarnBeforeExpiry==b.WarnBeforeExpiry&&
            a.X==b.X&&a.Y==b.Y&&a.Scale==b.Scale&&a.Gap==b.Gap&&a.Grow==b.Grow&&a.Opacity==b.Opacity;
    }
    internal bool Vertical {get{return Grow>=2;}}
    internal bool Reverse {get{return Grow==1||Grow==3;}}
    internal bool Compact {get{return Style==1;}}
    internal Size Card {get{return new Size((Names?152:44)*Scale/100,44*Scale/100);}}
    internal int Spacing {get{return Gap*Scale/100;}}
    internal CombatOverlayState.Effect[] Arrange(CombatOverlayState.Effect[] effects){
        var visible=effects.Where(e=>e.Harmful?ShowDebuffs:ShowBuffs);
        return LayoutMode==0?visible.OrderBy(e=>e.Harmful==DebuffsFirst?0:1).ThenBy(e=>e.Id).ToArray():visible.ToArray();
    }
    internal int Direction(bool harmful){
        int direction=harmful?DebuffGrow:BuffGrow;if(direction>=0)return direction;
        if(LayoutMode==1||LayoutMode==2)return (LayoutMode==1?harmful:!harmful)?1:0;
        return Grow;
    }
    internal Rectangle Geometry(CombatOverlayState.Effect[] effects,out Rectangle[] cells){
        cells=new Rectangle[effects.Length];var size=Card;int stepX=size.Width+Spacing,stepY=size.Height+Spacing;
        int buff=0,debuff=0;Rectangle bounds=Rectangle.Empty,buffBounds=Rectangle.Empty,debuffBounds=Rectangle.Empty;
        for(int i=0;i<effects.Length;i++){
            bool bad=effects[i].Harmful;int n=bad?debuff++:buff++;int x=0,y=0;
            if(LayoutMode!=0){int direction=Direction(bad);x=direction>=2?0:(direction==1?-n:n)*stepX;y=direction<2?0:(direction==3?-n:n)*stepY;}
            else{int index=Reverse?effects.Length-1-i:i;x=Vertical?0:index*stepX;y=Vertical?index*stepY:0;}
            cells[i]=new Rectangle(x,y,size.Width,size.Height);
            if(bad)debuffBounds=debuff==1?cells[i]:Rectangle.Union(debuffBounds,cells[i]);else buffBounds=buff==1?cells[i]:Rectangle.Union(buffBounds,cells[i]);
            bounds=i==0?cells[i]:Rectangle.Union(bounds,cells[i]);
        }
        if(LayoutMode!=0&&effects.Length>0){
            bounds=Rectangle.Empty;
            for(int i=0;i<cells.Length;i++){
                bool bad=effects[i].Harmful;Rectangle group=bad?debuffBounds:buffBounds;
                if(LayoutMode==1||LayoutMode==2){bool left=LayoutMode==1?bad:!bad;cells[i].Offset(left?-Spacing-group.Right:-group.Left,0);}
                else {bool below=LayoutMode==3?bad:!bad;cells[i].Offset(0,below?size.Height+Spacing-group.Top:size.Height-group.Bottom);}
                bounds=i==0?cells[i]:Rectangle.Union(bounds,cells[i]);
            }
        }
        if(effects.Length==0)bounds=new Rectangle(0,0,size.Width,size.Height);
        if(LayoutMode==0){if(Grow==1)bounds.Offset(-bounds.Width+size.Width,0);if(Grow==3)bounds.Offset(0,-bounds.Height+size.Height);}
        for(int i=0;i<cells.Length;i++){
            if(LayoutMode==0){cells[i].Offset(-bounds.Left,-bounds.Top);if(Grow==1)cells[i].Offset(-bounds.Width+size.Width,0);if(Grow==3)cells[i].Offset(0,-bounds.Height+size.Height);}
            else cells[i].Offset(-bounds.Left,-bounds.Top);
        }
        bounds.Offset(X,Y);return bounds;
    }
    internal Rectangle Layout(int count){
        var card=Card;
        int w=Vertical?card.Width:count*card.Width+(count-1)*Spacing;
        int h=Vertical?count*card.Height+(count-1)*Spacing:card.Height;
        return new Rectangle(X-(Grow==1?w-card.Width:0),Y-(Grow==3?h-card.Height:0),w,h);
    }
}

internal sealed class BuffOverlay : IDisposable {
    internal sealed class TargetAnchor {internal Rectangle Bounds;internal uint Actor;internal string Flow;internal byte Opacity;}
    readonly bool target;
    TargetAnchor anchor,lastAnchor;
    Point originOffset;
    readonly EffectIconAssets effectIcons=new EffectIconAssets();
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
    internal BuffOverlay(EnergyOverlay owner,CombatOverlayState signal,bool target=false){
        this.target=target;
        this.owner=owner;this.signal=signal;
        window.SnapOwner=owner;window.ResizeAllowed=false;
        window.Moved=delegate(Rectangle moved){
            var cfg=owner.Configuration;
            var options=target?cfg.EnemyHealth.Effects:cfg.BuffOverlay;
            options.X=moved.X-originOffset.X-(target&&anchor!=null?anchor.Bounds.X:0);
            options.Y=moved.Y-originOffset.Y-(target&&anchor!=null?anchor.Bounds.Y:0);
            owner.ApplyConfiguration(cfg);
        };
    }
    internal void ToggleTest(){testing=!testing;testStart=DpsHistory.UtcNow();nextFrame=0;}
    internal void StopTest(){testing=false;nextFrame=0;}
    internal void Maintain(BuffOverlayOptions o,bool editing,bool hidden,bool connected,double now,string language,TargetAnchor targetAnchor=null){
        if(disposed)return;
        anchor=targetAnchor;
        if(target)signal.WatchTarget(anchor==null?0:anchor.Actor,anchor==null?null:anchor.Flow);
        if(!testing&&(!o.Enabled||hidden||!connected&&!editing)){NextInterval=1000;window.HideOverlay();wasHidden=true;return;}
        if(target&&!testing&&!editing&&(anchor==null||anchor.Opacity==0||anchor.Actor==0)){window.HideOverlay();NextInterval=250;wasHidden=true;return;}
        if((!target||SameAnchor(anchor,lastAnchor))&&!window.SnapOwner.HudOpacityChanged&&o==previous&&!wasHidden&&editing==wasEditing&&connected==wasConnected&&language==previousLanguage&&now<nextFrame)return;
        lastAnchor=anchor;previous=o;previousLanguage=language;wasHidden=false;wasEditing=editing;wasConnected=connected;nextFrame=now+.125;
        NextInterval=1000;
        var effects=connected&&(!target||anchor!=null&&anchor.Actor!=0)?signal.ReadEffects(target&&anchor!=null?anchor.Actor:0,target&&anchor!=null?anchor.Flow:null,now,o.Own,o.Party,o.ShowBuffs,o.ShowDebuffs,o.MaximumIcons):new CombatOverlayState.Effect[0];
        if(testing||editing&&effects.Length==0){
            double elapsed=testing?(now-testStart)%14:0;
            effects=new[]{
                new CombatOverlayState.Effect{Skill=15310000,Ends=now+12-elapsed%12},
                new CombatOverlayState.Effect{Id=100000301,Skill=15200000,Harmful=true,Ends=now+4-elapsed%4,Party=true},
                new CombatOverlayState.Effect{Skill=15400000,Ends=0}};
        }
        effects=o.Arrange(effects);
        if(effects.Length==0){NextInterval=250;window.HideOverlay();return;}
        Rectangle[] positions;var layout=o.Geometry(effects,out positions);originOffset=new Point(layout.X-o.X,layout.Y-o.Y);
        if(target&&anchor!=null)layout.Offset(anchor.Bounds.X,anchor.Bounds.Y);
        var key=new StringBuilder().Append(layout.Size).Append('/').Append(o.Scale).Append('/').Append(o.Names)
            .Append('/').Append(o.TypeBorders).Append('/').Append(o.LayoutMode).Append('/').Append(o.Style).Append('/').Append(o.ShowTimer).Append('/').Append(o.WarnBeforeExpiry)
            .Append('/').Append(o.Grow).Append('/').Append(o.BuffGrow).Append('/').Append(o.DebuffGrow).Append('/').Append(o.Spacing).Append('/').Append(language).Append('/').Append(editing);
        bool expiring=false;
        foreach(var effect in effects){
            int left=Seconds(effect,now);
            expiring|=o.WarnBeforeExpiry&&Ending(left);
            key.Append('/').Append(effect.Id).Append('/').Append(effect.Skill).Append('/').Append(effect.Stacks).Append('/').Append(effect.Harmful).Append('/').Append(effect.Party);
            if(o.ShowTimer)key.Append('/').Append(left);
            else if(o.WarnBeforeExpiry)key.Append('/').Append(Ending(left));
        }
        if(expiring)key.Append("/expiry-pulse/").Append((int)Math.Round(ExpiryPulse(now)*16));
        NextInterval=expiring?63:125;nextFrame=now+NextInterval/1000.0;
        window.Render(layout,target&&anchor!=null&&!editing?(byte)Math.Round(o.Opacity*anchor.Opacity/100.0):window.SnapOwner.HudOpacity((byte)Math.Round(o.Opacity*255/100.0)),editing,key.ToString(),
            size=>Paint(size,o,effects,now,language,editing),o.Blur&&!o.Compact);
    }

    static bool SameAnchor(TargetAnchor a,TargetAnchor b){return a==null?b==null:b!=null&&a.Actor==b.Actor&&a.Flow==b.Flow&&a.Bounds==b.Bounds&&a.Opacity==b.Opacity;}
    internal static int Seconds(CombatOverlayState.Effect effect,double now){
        // Aura renewals still expire/remove in CombatOverlayState. Suppress only
        // their repetitive countdown and expiry pulse, not their real lifetime.
        return effect.Ends==0||EffectCatalogue.Sustained(effect.Id)?-1:Math.Max(0,(int)Math.Ceiling(effect.Ends-now));
    }
    static double ExpiryPulse(double now){return .5+.5*Math.Cos(now*Math.PI*2/1.2);}
    static bool Ending(int seconds){return seconds>=0&&seconds<=BuffOverlayOptions.WarningSeconds;}
    internal static string StackLabel(int stacks){return "x"+stacks.ToString(CultureInfo.InvariantCulture);}
    static string TimeLabel(int seconds){
        if(seconds<0)return "";
        if(seconds>=3600)return (seconds/3600).ToString(CultureInfo.InvariantCulture)+"h";
        if(seconds>=60)return (seconds/60).ToString(CultureInfo.InvariantCulture)+":"+(seconds%60).ToString("00",CultureInfo.InvariantCulture);
        return seconds.ToString(CultureInfo.InvariantCulture);
    }
    internal Bitmap Preview(BuffOverlayOptions o,string language){
        var sample=o.Copy();
        sample.Scale=Math.Min(sample.Scale,100);
        var effects=new[]{
            new CombatOverlayState.Effect{Skill=15310000,Ends=12},
            new CombatOverlayState.Effect{Id=100000301,Skill=15110000,Harmful=true,Ends=3,Stacks=3,Party=true}};
        effects=sample.Arrange(effects);Rectangle[] cells;return Paint(sample.Geometry(effects,out cells).Size,sample,effects,0,language,false,false);
    }
    Bitmap Paint(Size size,BuffOverlayOptions o,CombatOverlayState.Effect[] effects,double now,string language,bool editing,bool updateBackdrop=true){
        var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
        if(updateBackdrop)window.BackdropTiles=new Rectangle[effects.Length];
        using(var g=Graphics.FromImage(image)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            var cell=o.Card;Rectangle[] positions;o.Geometry(effects,out positions);
            int pad=Math.Max(1,(int)Math.Round(cell.Height*(o.Compact?.055:.15)));
            var outline=new TextOutlineOptions{Enabled=true,Width=1,Opacity=85};
            for(int i=0;i<effects.Length;i++){
                int x=positions[i].X,y=positions[i].Y;
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
                var icon=effectIcons.Icon(effect.Id)??icons.Icon(effect.Skill);
                if(icon!=null){
                    var state=g.Save();
                    using(var clip=SkillSlotPainter.Round(iconRect,Math.Max(2,cell.Height*(o.Compact?.105f:.08f)))){g.SetClip(clip);g.DrawImage(icon,iconRect);}
                    g.Restore(state);
                }else caption.Draw(g,"◇",Math.Max(11,cell.Height/3),true,Color.White,iconRect,1,true,outline);
                if(o.TypeBorders){
                    using(var rim=SkillSlotPainter.Round(plate,cell.Height*.14f))
                    using(var edge=new Pen(effect.Harmful?Color.FromArgb(255,245,65,75):Color.FromArgb(255,65,161,255),Math.Max(1f,cell.Height*.028f)))g.DrawPath(edge,rim);
                }
                if(ending){
                    // Expiry lives inside the icon. The outside blue/red rim
                    // continues to identify the effect type throughout its life.
                    double pulse=ExpiryPulse(now);
                    using(var clip=SkillSlotPainter.Round(iconRect,Math.Max(2,cell.Height*(o.Compact?.105f:.08f))))
                    using(var tint=new SolidBrush(Color.FromArgb(12+(int)Math.Round(pulse*26),255,185,55)))g.FillPath(tint,clip);
                    int inset=Math.Max(2,(int)Math.Round(cell.Height*.065));
                    var warning=new Rectangle(iconRect.X+inset,iconRect.Y+inset,iconRect.Width-inset*2,iconRect.Height-inset*2);
                    using(var rim=SkillSlotPainter.Round(warning,cell.Height*.08f))
                    using(var amber=new Pen(Color.FromArgb(95+(int)Math.Round(pulse*140),255,195,75),Math.Max(1,cell.Height*.025f)))g.DrawPath(amber,rim);
                }
                string time=o.ShowTimer?TimeLabel(seconds):"";
                if(time.Length>0){
                    int font=Math.Max(6,Math.Min(cell.Height/3,(int)(iconRect.Width/(time.Length*.66))));
                    counter.Draw(g,time,font,true,ending?Color.FromArgb(255,210,117):Color.White,iconRect,1,true,outline);
                }
                if(effect.Stacks>1){string stacks=StackLabel(effect.Stacks);counter.Draw(g,stacks,Math.Max(7,Math.Min(cell.Height/4,(int)(cell.Height/(stacks.Length*.7)))),true,Color.White,new Rectangle(iconRect.Left,iconRect.Bottom-cell.Height/3,iconRect.Width,cell.Height/3),2,true,outline);}
                if(o.Names){
                    int labelPadding=Math.Max(4,cell.Height/9);
                    int left=x+cell.Height+labelPadding,w=rect.Right-left-labelPadding;
                    caption.Draw(g,EffectCatalogue.Name(effect.Id,effect.Skill,language),Math.Max(8,cell.Height/4),true,Color.White,
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
        var options=new BuffOverlayOptions{Grow=0,Names=true,TypeBorders=false};
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
                        bool warningChangesIcon=false;
                        for(int y=5;y<38&&!warningChangesIcon;y++)for(int x=5;x<38&&!warningChangesIcon;x++)
                            warningChangesIcon=noTimer.GetPixel(x,y).ToArgb()!=noBorder.GetPixel(x,y).ToArgb();
                        if(!warningChangesIcon||noTimer.GetPixel(1,22).ToArgb()!=noBorder.GetPixel(1,22).ToArgb())
                            throw new Exception("The optional amber expiry pulse stays inside the icon");
                    }
                }
            }
        }
        VerifyExpiryAppearance();
        options.Names=false;
        if(options.Card.Width!=options.Card.Height)throw new Exception("Names can be hidden to keep only the square buttons");
        effects=new[]{effects[0],effects[0]};
        using(Paint(options.Layout(2).Size,options,effects,0,"es",false)){
            if(window.BackdropTiles[0].Right>=window.BackdropTiles[1].Left)
                throw new Exception("Buff backdrops must leave the gap between tiles clear");
        }
    }
    void VerifyExpiryAppearance(){
        var options=new BuffOverlayOptions{Names=false,Grow=0,TypeBorders=true,ShowTimer=false,Style=1};
        var effects=new[]{new CombatOverlayState.Effect{Skill=15310000,Ends=3},new CombatOverlayState.Effect{Skill=15310000,Ends=3,Harmful=true}};
        Rectangle[] cells;var size=options.Geometry(effects,out cells).Size;
        using(var warning=Paint(size,options,effects,0,"en",false,false)){
            options.WarnBeforeExpiry=false;
            using(var normal=Paint(size,options,effects,0,"en",false,false)){
                for(int i=0;i<2;i++){
                    int x=cells[i].X;Color type=warning.GetPixel(x+1,22);
                    if(type.ToArgb()!=normal.GetPixel(x+1,22).ToArgb()||(i==0?type.B<=type.R:type.R<=type.B))
                        throw new Exception("Expiry must preserve both thin blue and red type borders");
                    Color amber=warning.GetPixel(x+5,22);
                    if(amber.R<200||amber.G<130||amber.B>=amber.G)
                        throw new Exception("Both effect types need a distinct amber expiry cue");
                }
            }
            options.WarnBeforeExpiry=true;
            using(var low=Paint(size,options,effects,.6,"en",false,false)){
                if(low.GetPixel(5,22).ToArgb()==warning.GetPixel(5,22).ToArgb())throw new Exception("Expiry icon pulse must animate");
            }
        }
    }
    internal Bitmap ExpiryPreview(){
        var options=new BuffOverlayOptions{Names=false,Grow=0,Style=1};
        var effects=new[]{new CombatOverlayState.Effect{Skill=15310000,Ends=12},new CombatOverlayState.Effect{Skill=15310000,Ends=3},new CombatOverlayState.Effect{Skill=15310000,Ends=3,Harmful=true}};
        Rectangle[] cells;return Paint(options.Geometry(effects,out cells).Size,options,effects,0,"es",false,false);
    }
    public void Dispose(){if(disposed)return;disposed=true;window.Dispose();caption.Dispose();counter.Dispose();icons.Dispose();effectIcons.Dispose();}
}
