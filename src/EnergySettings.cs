using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

internal sealed class EnergyBarOptions {
    public bool Enabled=true, AutoHide=true, Fade=true, Smooth=true, Emissive=true, DynamicColors=true;
    public double HoldSeconds=1.5, FadeSeconds=.2, SmoothingSeconds=.07;
    public int GlowPercent=100, TrackOpacity=170,ScalePercent=100;public ProgressScaleReference ScaleReference;
    public string LowColor="#A02D2D", MediumColor="#B85416", HighColor="#287044";
    public bool CurrentText=false,MaximumText=false,PercentageText=false;
    public int CurrentPosition=0,MaximumPosition=2,PercentagePosition=1,TextSize=14;
    public int CurrentOffsetY=0,MaximumOffsetY=0,PercentageOffsetY=0;
    public string TextColor="#EEEEEE";public TextOutlineOptions TextOutline=new TextOutlineOptions();
    string lowCache,mediumCache,highCache;Color lowPaint,mediumPaint,highPaint;
    static Color CachedColor(string value,ref string cached,ref Color color){if(cached!=value){color=ColorTranslator.FromHtml(value);cached=value;}return color;}
    internal Color LowPaint {get{return CachedColor(LowColor,ref lowCache,ref lowPaint);}}
    internal Color MediumPaint {get{return CachedColor(MediumColor,ref mediumCache,ref mediumPaint);}}
    internal Color HighPaint {get{return CachedColor(HighColor,ref highCache,ref highPaint);}}
    internal EnergyBarOptions Copy(){var copy=(EnergyBarOptions)MemberwiseClone();copy.ScaleReference=ScaleReference==null?null:ScaleReference.Copy();copy.TextOutline=TextOutline==null?new TextOutlineOptions():TextOutline.Copy();return copy;}
    internal static bool ValidColor(string value){int hex;return value!=null&&value.Length==7&&value[0]=='#'&&int.TryParse(value.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out hex);}
    internal void Normalize(){
        if(TextOutline==null)TextOutline=new TextOutlineOptions();TextOutline.Normalize();ScalePercent=Math.Max(50,Math.Min(300,ScalePercent));HoldSeconds=Finite(HoldSeconds,1.5,0,30);FadeSeconds=Finite(FadeSeconds,.2,0,2);SmoothingSeconds=Finite(SmoothingSeconds,.07,0,1);
        GlowPercent=Math.Max(0,Math.Min(200,GlowPercent));TrackOpacity=Math.Max(0,Math.Min(255,TrackOpacity));
        CurrentPosition=Math.Max(0,Math.Min(5,CurrentPosition));MaximumPosition=Math.Max(0,Math.Min(5,MaximumPosition));PercentagePosition=Math.Max(0,Math.Min(5,PercentagePosition));TextSize=Math.Max(10,Math.Min(144,TextSize));if(!ValidColor(TextColor))TextColor="#EEEEEE";
        CurrentOffsetY=Math.Max(-600,Math.Min(600,CurrentOffsetY));MaximumOffsetY=Math.Max(-600,Math.Min(600,MaximumOffsetY));PercentageOffsetY=Math.Max(-600,Math.Min(600,PercentageOffsetY));
        if(!ValidColor(LowColor))LowColor="#A02D2D";if(!ValidColor(MediumColor))MediumColor="#B85416";if(!ValidColor(HighColor))HighColor="#287044";
    }
    static double Finite(double value,double fallback,double min,double max){return double.IsNaN(value)||double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));}
    internal static EnergyBarOptions Read(Dictionary<string,object> cfg){
        object data;var options=new EnergyBarOptions();if(cfg.TryGetValue("energyBar",out data)){
            try{options=new System.Web.Script.Serialization.JavaScriptSerializer().ConvertToType<EnergyBarOptions>(data)??options;}catch{}
        }else if(cfg.ContainsKey("emissive"))options.Emissive=Convert.ToBoolean(cfg["emissive"]);
        options.Normalize();return options;
    }
    internal static void Verify(){
        var legacy=Read(new Dictionary<string,object>{{"width",425},{"height",4},{"emissive",true}});
        if(!legacy.Enabled||!legacy.AutoHide||legacy.HoldSeconds!=1.5||legacy.FadeSeconds!=.2||!legacy.Emissive)throw new Exception("Legacy settings must preserve existing defaults");
        legacy.Enabled=false;legacy.AutoHide=false;legacy.HoldSeconds=2.25;legacy.LowColor="#123456";legacy.Emissive=false;
        var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();var restored=Read(serializer.Deserialize<Dictionary<string,object>>(serializer.Serialize(new {energyBar=legacy})));
        if(restored.Enabled||restored.AutoHide||restored.Emissive||restored.HoldSeconds!=2.25||restored.LowColor!="#123456")throw new Exception("Settings must survive saving/reopening, including a disabled energy bar");
        var previous=Read(serializer.Deserialize<Dictionary<string,object>>("{\"energyBar\":{\"AutoMaximum\":false,\"LowColor\":\"#123456\",\"Enabled\":false}}"));if(previous.LowColor!="#123456"||previous.Enabled)throw new Exception("Retired calibration switch must not reset other preferences");
        restored.LowColor="bad";restored.FadeSeconds=double.NaN;restored.GlowPercent=999;restored.Normalize();
        if(restored.LowColor!="#A02D2D"||restored.FadeSeconds!=.2||restored.GlowPercent!=200)throw new Exception("Invalid settings must have safe drawing/animation limits");
    }
}

// Shared by every overlay widget, including future helper modules.
internal sealed class WidgetLock {
    bool locked;
    internal event Action Changed;
    internal bool Locked {get{return locked;}set{if(locked==value)return;locked=value;if(Changed!=null)Changed();}}
}

internal sealed class EnergyConfiguration {
    internal EnergyBarOptions Options;
    internal Rectangle Bounds;
    internal bool Locked;
    internal uint Maximum;
    internal string Language="en";
    internal FpsOptions Fps=new FpsOptions();
    internal FpsOptions Ping=FpsOptions.PingDefaults();
    internal DpsOptions Dps=new DpsOptions();
    internal NotificationOptions Notifications=new NotificationOptions();
    internal PetOptions Pets=new PetOptions();
    internal StaggerOptions Stagger=new StaggerOptions();
    internal EnemyHealthOptions EnemyHealth=new EnemyHealthOptions(),EnemyTarget=EnemyHealthOptions.TargetDefaults();
    internal PlayerVitalOptions PlayerHealth=PlayerVitalOptions.Defaults(false),PlayerMana=PlayerVitalOptions.Defaults(true);
    internal EnergyConfiguration Copy(){return new EnergyConfiguration{Options=Options.Copy(),Bounds=Bounds,Locked=Locked,Maximum=Maximum,Language=Language,Fps=Fps.Copy(),Ping=Ping.Copy(),Dps=Dps.Copy(),Notifications=Notifications.Copy(),Pets=Pets.Copy(),Stagger=Stagger.Copy(),EnemyHealth=EnemyHealth.Copy(),EnemyTarget=EnemyTarget.Copy(),PlayerHealth=PlayerHealth.Copy(),PlayerMana=PlayerMana.Copy()};}
}

// Display metadata is separate from settings undo and game data. Keep the last
// user-edited layout as the reference to avoid rounding drift on monitor swaps.
internal sealed class WidgetDisplayLayout {
    internal sealed class Area {public string Name;public Rectangle Bounds,Working;public bool Primary;}
    internal sealed class State {public int Version=8;public Area[] Screens;public Rectangle Energy,Fps,Ping,Dps,Notifications,Stagger,EnemyHealth,EnemyTarget,PlayerHealth,PlayerMana;public double HealthDisplayScale=1,ManaDisplayScale=1;public int NotificationFontSize;public Rectangle RiftCounter;public int RiftScale=100;public Rectangle Pets,PetAtlas;public int PetScale=100;}
    internal State Reference;
    internal static Area[] Screens(){var screens=Screen.AllScreens;var result=new Area[screens.Length];for(int n=0;n<screens.Length;n++)result[n]=new Area{Name=screens[n].DeviceName,Bounds=screens[n].Bounds,Working=screens[n].WorkingArea,Primary=screens[n].Primary};return result;}
    internal static bool Valid(Area[] areas){if(areas==null||areas.Length==0||areas.Length>32)return false;foreach(var a in areas)if(a==null||string.IsNullOrEmpty(a.Name)||a.Bounds.Width<1||a.Bounds.Height<1||a.Bounds.Width>65535||a.Bounds.Height>65535||a.Working.Width<1||a.Working.Height<1||!a.Bounds.Contains(a.Working))return false;return true;}
    internal static bool Same(Area[] a,Area[] b){if(!Valid(a)||!Valid(b)||a.Length!=b.Length)return false;foreach(var old in a){bool found=false;foreach(var next in b)if(old.Name==next.Name&&old.Bounds==next.Bounds&&old.Working==next.Working&&old.Primary==next.Primary){found=true;break;}if(!found)return false;}return true;}
    internal static Area Primary(Area[] areas){foreach(var area in areas)if(area.Primary)return area;return areas[0];}
    static Area Source(Rectangle bounds,Area[] areas){Area best=Primary(areas);long largest=0;foreach(var area in areas){var part=Rectangle.Intersect(bounds,area.Bounds);long size=(long)Math.Max(0,part.Width)*Math.Max(0,part.Height);if(size>largest){largest=size;best=area;}}return best;}
    static Area Destination(Area source,Area[] areas){foreach(var area in areas)if(area.Name==source.Name)return area;return Primary(areas);}
    static int Round(double value){return (int)Math.Max(-100000,Math.Min(100000,Math.Round(value)));}
    static Rectangle Fit(Rectangle bounds,Area area,int minWidth,int minHeight,int maxWidth,int maxHeight){var usable=area.Working;int width=Math.Min(usable.Width,Math.Max(minWidth,Math.Min(maxWidth,bounds.Width))),height=Math.Min(usable.Height,Math.Max(minHeight,Math.Min(maxHeight,bounds.Height)));return new Rectangle(Math.Max(usable.Left,Math.Min(usable.Right-width,bounds.X)),Math.Max(usable.Top,Math.Min(usable.Bottom-height,bounds.Y)),width,height);}
    internal static Rectangle Project(Rectangle bounds,Area[] old,Area[] current,int minWidth,int minHeight,int maxWidth,int maxHeight){var source=Source(bounds,old);var target=Destination(source,current);double sx=(double)target.Bounds.Width/source.Bounds.Width,sy=(double)target.Bounds.Height/source.Bounds.Height;var scaled=new Rectangle(target.Bounds.Left+Round(((double)bounds.X-source.Bounds.Left)*sx),target.Bounds.Top+Round(((double)bounds.Y-source.Bounds.Top)*sy),Math.Max(1,Round(bounds.Width*sx)),Math.Max(1,Round(bounds.Height*sy)));return Fit(scaled,target,minWidth,minHeight,maxWidth,maxHeight);}
    internal static Rectangle Recover(Rectangle bounds,Area[] current,int minWidth,int minHeight,int maxWidth,int maxHeight){var area=Source(bounds,current);return Fit(bounds,area,minWidth,minHeight,maxWidth,maxHeight);}
    static PlayerVitalOptions ProjectVital(PlayerVitalOptions o,Area[] old,Area[] current){var result=o.Copy();var source=Source(o.Bounds,old);var target=Destination(source,current);result.Bounds=Project(o.Bounds,old,current,80,6,1600,80);result.DisplayScale=Math.Max(.25,Math.Min(4,o.DisplayScale*Math.Min((double)target.Bounds.Width/source.Bounds.Width,(double)target.Bounds.Height/source.Bounds.Height)));return result;}
    static RiftCounterOptions RecoverRift(RiftCounterOptions options,Area[] current){var result=options.Copy();var actual=result.ActualBounds;var fit=Recover(actual,current,1,1,10000,10000);if(fit.Width<actual.Width||fit.Height<actual.Height)result.ScalePercent=Math.Max(50,(int)Math.Floor(result.ScalePercent*Math.Min((double)fit.Width/actual.Width,(double)fit.Height/actual.Height)));var b=result.Bounds;b.X=fit.X+fit.Width/2-b.Width/2;b.Y=fit.Y+fit.Height/2-b.Height/2;result.Bounds=b;return result;}
    static RiftCounterOptions ProjectRift(RiftCounterOptions options,Area[] old,Area[] current){var result=options.Copy();var original=options.ActualBounds;var source=Source(original,old);var target=Destination(source,current);var projected=Project(original,old,current,1,1,10000,10000);result.ScalePercent=Math.Max(50,Math.Min(300,Round(result.ScalePercent*Math.Min((double)target.Bounds.Width/source.Bounds.Width,(double)target.Bounds.Height/source.Bounds.Height))));var b=result.Bounds;b.X=projected.X+projected.Width/2-b.Width/2;b.Y=projected.Y+projected.Height/2-b.Height/2;result.Bounds=b;return RecoverRift(result,current);}
    static PetOptions RecoverPet(PetOptions options,Area[] current){var o=options.Copy();var actual=o.Scaled;var fit=Recover(actual,current,1,1,10000,10000);if(fit.Width<actual.Width||fit.Height<actual.Height)o.Scale=Math.Max(50,(int)Math.Floor(o.Scale*Math.Min((double)fit.Width/actual.Width,(double)fit.Height/actual.Height)));var b=o.Bounds;b.X=fit.X+fit.Width/2-b.Width/2;b.Y=fit.Y+fit.Height/2-43;o.Bounds=b;o.AtlasBounds=Recover(o.AtlasBounds,current,700,480,1800,1200);return o;}
    static PetOptions ProjectPet(PetOptions options,Area[] old,Area[] current){var o=options.Copy();var projected=Project(o.Scaled,old,current,1,1,10000,10000);var from=Source(o.Scaled,old);var to=Destination(from,current);o.Scale=Math.Max(50,Math.Min(200,Round(o.Scale*Math.Min((double)to.Bounds.Width/from.Bounds.Width,(double)to.Bounds.Height/from.Bounds.Height))));var b=o.Bounds;b.X=projected.X+projected.Width/2-b.Width/2;b.Y=projected.Y+projected.Height/2-43;o.Bounds=b;o.AtlasBounds=Project(o.AtlasBounds,old,current,700,480,1800,1200);return RecoverPet(o,current);}
    internal static EnergyConfiguration Transform(EnergyConfiguration cfg,Area[] old,Area[] current){var result=cfg.Copy();result.Pets=ProjectPet(cfg.Pets,old,current);result.Notifications.Rift.Counter=ProjectRift(cfg.Notifications.Rift.Counter,old,current);result.PlayerHealth=ProjectVital(cfg.PlayerHealth,old,current);result.PlayerMana=ProjectVital(cfg.PlayerMana,old,current);result.Bounds=Project(cfg.Bounds,old,current,40,3,32767,300);result.Fps.Bounds=Project(cfg.Fps.Bounds,old,current,48,20,1600,400);result.Ping.Bounds=Project(cfg.Ping.Bounds,old,current,48,20,1600,400);result.Stagger.Bounds=Project(cfg.Stagger.Bounds,old,current,40,3,32767,300);result.EnemyHealth.Bounds=Project(cfg.EnemyHealth.Bounds,old,current,80,3,2400,80);result.EnemyTarget.Bounds=Project(cfg.EnemyTarget.Bounds,old,current,80,3,2400,80);result.Dps.Bounds=Project(cfg.Dps.Bounds,old,current,260,110,1600,900);result.Notifications.Bounds=Project(cfg.Notifications.Bounds,old,current,260,60,1600,500);var source=Source(cfg.Notifications.Bounds,old);var target=Destination(source,current);double scale=Math.Min((double)target.Bounds.Width/source.Bounds.Width,(double)target.Bounds.Height/source.Bounds.Height);result.Notifications.FontSize=Math.Max(10,Math.Min(48,Round(cfg.Notifications.FontSize*scale)));return result;}
    internal void Capture(EnergyConfiguration cfg,Area[] screens){Reference=new State{Screens=screens,Energy=cfg.Bounds,Fps=cfg.Fps.Bounds,Ping=cfg.Ping.Bounds,Dps=cfg.Dps.Bounds,Notifications=cfg.Notifications.Bounds,Stagger=cfg.Stagger.Bounds,EnemyHealth=cfg.EnemyHealth.Bounds,EnemyTarget=cfg.EnemyTarget.Bounds,PlayerHealth=cfg.PlayerHealth.Bounds,PlayerMana=cfg.PlayerMana.Bounds,HealthDisplayScale=cfg.PlayerHealth.DisplayScale,ManaDisplayScale=cfg.PlayerMana.DisplayScale,NotificationFontSize=cfg.Notifications.FontSize,RiftCounter=cfg.Notifications.Rift.Counter.Bounds,RiftScale=cfg.Notifications.Rift.Counter.ScalePercent,Pets=cfg.Pets.Bounds,PetScale=cfg.Pets.Scale,PetAtlas=cfg.Pets.AtlasBounds};}
    internal static EnergyConfiguration Recover(EnergyConfiguration cfg,Area[] current){var result=cfg.Copy();result.Pets=RecoverPet(cfg.Pets,current);result.Notifications.Rift.Counter=RecoverRift(cfg.Notifications.Rift.Counter,current);result.Bounds=Recover(cfg.Bounds,current,40,3,32767,300);result.Fps.Bounds=Recover(cfg.Fps.Bounds,current,48,20,1600,400);result.Ping.Bounds=Recover(cfg.Ping.Bounds,current,48,20,1600,400);result.Stagger.Bounds=Recover(cfg.Stagger.Bounds,current,40,3,32767,300);result.EnemyHealth.Bounds=Recover(cfg.EnemyHealth.Bounds,current,80,3,2400,80);result.EnemyTarget.Bounds=Recover(cfg.EnemyTarget.Bounds,current,80,3,2400,80);result.PlayerHealth.Bounds=Recover(cfg.PlayerHealth.Bounds,current,80,6,1600,80);result.PlayerMana.Bounds=Recover(cfg.PlayerMana.Bounds,current,80,6,1600,80);result.Dps.Bounds=Recover(cfg.Dps.Bounds,current,260,110,1600,900);result.Notifications.Bounds=Recover(cfg.Notifications.Bounds,current,260,60,1600,500);return result;}
    internal void Read(Dictionary<string,object> settings){object value;if(!settings.TryGetValue("displayLayout",out value))return;try{var saved=new System.Web.Script.Serialization.JavaScriptSerializer().ConvertToType<State>(value);if(saved!=null&&(saved.Version==1||saved.Version==2||saved.Version==3||saved.Version==4||saved.Version==5||saved.Version==6||saved.Version==7||saved.Version==8)&&(saved.Version<4||saved.EnemyHealth.Width>0&&saved.EnemyHealth.Height>0)&&(saved.Version<5||saved.EnemyTarget.Width>0&&saved.EnemyTarget.Height>0)&&(saved.Version<6||saved.PlayerHealth.Width>0&&saved.PlayerMana.Width>0&&saved.PlayerHealth.Height>0&&saved.PlayerMana.Height>0&&saved.HealthDisplayScale>0&&saved.HealthDisplayScale<=4&&saved.ManaDisplayScale>0&&saved.ManaDisplayScale<=4)&&(saved.Version<7||saved.RiftCounter.Width>=260&&saved.RiftCounter.Height==82&&saved.RiftScale>=50&&saved.RiftScale<=300)&&(saved.Version<8||saved.Pets.Width>=320&&saved.Pets.Height==86&&saved.PetScale>=50&&saved.PetScale<=200&&saved.PetAtlas.Width>=700&&saved.PetAtlas.Height>=480)&&Valid(saved.Screens)&&(saved.Version<3||(saved.Stagger.Width>0&&saved.Stagger.Height>0))&&(saved.Version==1||(saved.Ping.Width>0&&saved.Ping.Height>0))&&saved.Energy.Width>0&&saved.Energy.Height>0&&saved.Fps.Width>0&&saved.Fps.Height>0&&saved.Dps.Width>0&&saved.Dps.Height>0&&saved.Notifications.Width>0&&saved.Notifications.Height>0&&saved.NotificationFontSize>=10&&saved.NotificationFontSize<=48)Reference=saved;}catch{}}
    internal EnergyConfiguration Adapt(EnergyConfiguration cfg,Area[] current){if(Reference==null){var result=cfg.Copy();result.Pets=RecoverPet(cfg.Pets,current);result.Notifications.Rift.Counter=RecoverRift(cfg.Notifications.Rift.Counter,current);result.Bounds=Recover(cfg.Bounds,current,40,3,32767,300);result.Fps.Bounds=Recover(cfg.Fps.Bounds,current,48,20,1600,400);result.Ping.Bounds=Recover(cfg.Ping.Bounds,current,48,20,1600,400);result.Stagger.Bounds=Recover(cfg.Stagger.Bounds,current,40,3,32767,300);result.EnemyHealth.Bounds=Recover(cfg.EnemyHealth.Bounds,current,80,3,2400,80);result.EnemyTarget.Bounds=Recover(cfg.EnemyTarget.Bounds,current,80,3,2400,80);result.PlayerHealth.Bounds=Recover(cfg.PlayerHealth.Bounds,current,80,6,1600,80);result.PlayerMana.Bounds=Recover(cfg.PlayerMana.Bounds,current,80,6,1600,80);result.Dps.Bounds=Recover(cfg.Dps.Bounds,current,260,110,1600,900);result.Notifications.Bounds=Recover(cfg.Notifications.Bounds,current,260,60,1600,500);Capture(result,current);return result;}if(Reference.Version==1){Reference.Ping=Project(cfg.Ping.Bounds,current,Reference.Screens,48,20,1600,400);Reference.Version=2;}var baseline=cfg.Copy();if(Reference.Version<3){Reference.Stagger=Project(cfg.Stagger.Bounds,current,Reference.Screens,40,3,32767,300);Reference.Version=3;}if(Reference.Version<4){Reference.EnemyHealth=Project(cfg.EnemyHealth.Bounds,current,Reference.Screens,80,3,2400,80);Reference.Version=4;}if(Reference.Version<5){Reference.EnemyTarget=Project(cfg.EnemyTarget.Bounds,current,Reference.Screens,80,3,2400,80);Reference.Version=5;}if(Reference.Version<6){var health=ProjectVital(cfg.PlayerHealth,current,Reference.Screens);var mana=ProjectVital(cfg.PlayerMana,current,Reference.Screens);Reference.PlayerHealth=health.Bounds;Reference.PlayerMana=mana.Bounds;Reference.HealthDisplayScale=health.DisplayScale;Reference.ManaDisplayScale=mana.DisplayScale;Reference.Version=6;}if(Reference.Version<7){var rift=ProjectRift(cfg.Notifications.Rift.Counter,current,Reference.Screens);Reference.RiftCounter=rift.Bounds;Reference.RiftScale=rift.ScalePercent;Reference.Version=7;}baseline.Notifications.Rift.Counter.Bounds=Reference.RiftCounter;baseline.Notifications.Rift.Counter.ScalePercent=Reference.RiftScale;baseline.PlayerHealth.Bounds=Reference.PlayerHealth;baseline.PlayerMana.Bounds=Reference.PlayerMana;baseline.PlayerHealth.DisplayScale=Reference.HealthDisplayScale;baseline.PlayerMana.DisplayScale=Reference.ManaDisplayScale;baseline.EnemyTarget.Bounds=Reference.EnemyTarget;baseline.EnemyHealth.Bounds=Reference.EnemyHealth;baseline.Stagger.Bounds=Reference.Stagger;if(Reference.Version<8){var p=ProjectPet(cfg.Pets,current,Reference.Screens);Reference.Pets=p.Bounds;Reference.PetScale=p.Scale;Reference.PetAtlas=p.AtlasBounds;Reference.Version=8;}baseline.Pets.Bounds=Reference.Pets;baseline.Pets.Scale=Reference.PetScale;baseline.Pets.AtlasBounds=Reference.PetAtlas;baseline.Bounds=Reference.Energy;baseline.Ping.Bounds=Reference.Ping;baseline.Fps.Bounds=Reference.Fps;baseline.Dps.Bounds=Reference.Dps;baseline.Notifications.Bounds=Reference.Notifications;baseline.Notifications.FontSize=Reference.NotificationFontSize;return Same(Reference.Screens,current)?Recover(baseline,current):Transform(baseline,Reference.Screens,current);}
    internal static void Verify(){
        var large=new[]{new Area{Name="screen",Bounds=new Rectangle(0,0,2560,1440),Working=new Rectangle(0,0,2560,1400),Primary=true}};
        var small=new[]{new Area{Name="screen",Bounds=new Rectangle(0,0,1920,1080),Working=new Rectangle(0,0,1920,1040),Primary=true}};
        var original=new EnergyConfiguration{Options=new EnergyBarOptions(),Bounds=new Rectangle(800,1200,800,8),Maximum=138500};original.Fps.Bounds=new Rectangle(2068,12,112,44);original.Dps.Bounds=new Rectangle(1600,960,480,320);original.Notifications.Bounds=new Rectangle(960,160,640,120);original.Notifications.FontSize=24;
        var layout=new WidgetDisplayLayout();layout.Capture(original,large);var changed=layout.Adapt(original,small);
        if(changed.Ping.Bounds!=new Rectangle(22,60,84,33))throw new Exception("Ping must scale independently with the other widgets");
        if(changed.Bounds!=new Rectangle(600,900,600,6)||changed.Fps.Bounds!=new Rectangle(1551,9,84,33)||changed.Dps.Bounds!=new Rectangle(1200,720,360,240)||changed.Notifications.Bounds!=new Rectangle(720,120,480,90)||changed.Notifications.FontSize!=18)throw new Exception("All widgets must proportionally adapt from 1440p to 1080p");
        if(!ConfigurationHistory.Same(original,layout.Adapt(changed,large)))throw new Exception("Returning to original resolution must restore exact reference without rounding drift");
        var legacyLayout=new WidgetDisplayLayout();legacyLayout.Capture(original,large);legacyLayout.Reference.Version=1;legacyLayout.Reference.Ping=Rectangle.Empty;var migrated=legacyLayout.Adapt(original,small);if(migrated.Ping.Bounds!=original.Ping.Bounds||migrated.Fps.Bounds!=changed.Fps.Bounds||legacyLayout.Reference.Version!=8)throw new Exception("Four-widget metadata must migrate without moving the new ping widget or losing FPS projection");
        var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();var reopened=new WidgetDisplayLayout();reopened.Read(serializer.Deserialize<Dictionary<string,object>>(serializer.Serialize(new{displayLayout=layout.Reference})));if(!ConfigurationHistory.Same(changed,reopened.Adapt(original,small)))throw new Exception("Layout reference must survive restart");
        var left=new[]{new Area{Name="left",Bounds=new Rectangle(-2560,0,2560,1440),Working=new Rectangle(-2560,0,2560,1400),Primary=true}};var moved=Project(new Rectangle(2068,12,112,44),large,left,48,20,1600,400);if(moved!=new Rectangle(-492,12,112,44))throw new Exception("Replacing monitor must preserve relative placement, including negative origins");
        var dual=new[]{large[0],new Area{Name="second",Bounds=new Rectangle(2560,0,1920,1080),Working=new Rectangle(2560,0,1920,1040)}};var repositioned=new[]{new Area{Name="screen",Bounds=new Rectangle(-2560,0,2560,1440),Working=new Rectangle(-2560,0,2560,1400)},new Area{Name="second",Bounds=new Rectangle(0,0,1920,1080),Working=new Rectangle(0,0,1920,1040),Primary=true}};
        if(Project(new Rectangle(2760,100,112,44),dual,repositioned,48,20,1600,400)!=new Rectangle(200,100,112,44))throw new Exception("Widgets must remain on their matching monitor when screens are rearranged");
        var legacy=new WidgetDisplayLayout().Adapt(original,small);if(!small[0].Working.Contains(legacy.Fps.Bounds)||!small[0].Working.Contains(legacy.Dps.Bounds))throw new Exception("Legacy inaccessible positions must be recovered without guessed resolution");
        var extreme=Project(new Rectangle(2200,1300,2000,900),large,small,260,110,1600,900);if(!small[0].Working.Contains(extreme))throw new Exception("Scaled widgets must stay usable above the taskbar");
        var invalid=new WidgetDisplayLayout();invalid.Read(serializer.Deserialize<Dictionary<string,object>>("{\"displayLayout\":{\"Version\":1,\"Screens\":[{\"Name\":\"bad\",\"Bounds\":{\"Width\":0,\"Height\":0}}]}}"));if(invalid.Reference!=null)throw new Exception("Invalid reference must not divide by zero");
        var history=new ConfigurationHistory();history.Observe(original);var edit=original.Copy();edit.Options.LowColor="#123456";history.Observe(edit);history.AdaptDisplays(large,small);EnergyConfiguration undo;if(!history.Undo(out undo)||!ConfigurationHistory.Same(undo,changed))throw new Exception("Undo geometry must be adapted to the current display without losing other settings");
    }
    internal static void VerifyPersistence(string root){
        string folder=System.IO.Path.Combine(root,".local-tools","display-layout-test");System.IO.Directory.CreateDirectory(System.IO.Path.Combine(folder,"protocol"));System.IO.File.Copy(System.IO.Path.Combine(root,"protocol","sync-opcodes.json"),System.IO.Path.Combine(folder,"protocol","sync-opcodes.json"),true);
        var screens=Screens();var source=new[]{new Area{Name=screens[0].Name,Bounds=new Rectangle(0,0,2560,1600),Working=new Rectangle(0,0,2560,1560),Primary=true}};
        var cfg=new EnergyConfiguration{Options=new EnergyBarOptions{Enabled=false},Bounds=new Rectangle(800,1200,640,8),Maximum=120000,Locked=true};cfg.Notifications.Enabled=false;cfg.Fps.Bounds=new Rectangle(2069,10,112,44);
        var layout=new WidgetDisplayLayout();layout.Capture(cfg,source);var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();string path=System.IO.Path.Combine(folder,"overlay-settings.json");System.IO.File.WriteAllText(path,serializer.Serialize(new{maximum=cfg.Maximum,x=cfg.Bounds.X,y=cfg.Bounds.Y,width=cfg.Bounds.Width,height=cfg.Bounds.Height,design="slim-v2",locked=true,energyBar=cfg.Options,fps=cfg.Fps,ping=cfg.Ping,dps=cfg.Dps,notifications=cfg.Notifications,displayLayout=layout.Reference}));
        var expected=layout.Adapt(cfg,screens);using(var owner=new EnergyOverlay(folder)){if(!ConfigurationHistory.Same(owner.Configuration,expected))throw new Exception("Actual startup must restore proportional geometry for all widgets");owner.Show();Application.DoEvents();owner.Close();}
        var retained=new WidgetDisplayLayout();retained.Read(serializer.Deserialize<Dictionary<string,object>>(System.IO.File.ReadAllText(path)));if(retained.Reference==null||!Same(retained.Reference.Screens,source))throw new Exception("Startup/shutdown must retain original reference rather than accumulate rounding");
        using(var reopened=new EnergyOverlay(folder)){if(!ConfigurationHistory.Same(reopened.Configuration,expected))throw new Exception("Reopened proportional layout must match first startup");}
    }
}

// Session-only history: 100 undo steps, with independent immutable snapshots.
// Recording happens only when preferences are saved, never on energy packets.
internal sealed class ConfigurationHistory {
    const int MaximumSteps=100;
    readonly List<EnergyConfiguration> states=new List<EnergyConfiguration>();int cursor=-1;
    internal bool CanUndo {get{return cursor>0;}}
    internal bool CanRedo {get{return cursor>=0&&cursor<states.Count-1;}}
    internal int UndoCount {get{return Math.Max(0,cursor);}}
    internal void AdaptDisplays(WidgetDisplayLayout.Area[] old,WidgetDisplayLayout.Area[] current){for(int n=0;n<states.Count;n++)states[n]=WidgetDisplayLayout.Transform(states[n],old,current);}
    internal static bool Same(EnergyConfiguration a,EnergyConfiguration b){
        var x=a.Options;var y=b.Options;
        return PlayerVitalOptions.Same(a.PlayerHealth,b.PlayerHealth)&&PlayerVitalOptions.Same(a.PlayerMana,b.PlayerMana)&&new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(a.EnemyTarget)==new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(b.EnemyTarget)&&new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(a.EnemyHealth)==new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(b.EnemyHealth)&&StaggerOptions.Same(a.Stagger,b.Stagger)&&ProgressBarText.Same(x,y)&&NotificationOptions.Same(a.Notifications,b.Notifications)&&PetOptions.Same(a.Pets,b.Pets)&&DpsOptions.Same(a.Dps,b.Dps)&&FpsOptions.Same(a.Fps,b.Fps)&&FpsOptions.Same(a.Ping,b.Ping)&&UiLanguage.Normalize(a.Language)==UiLanguage.Normalize(b.Language)&&a.Bounds==b.Bounds&&a.Locked==b.Locked&&a.Maximum==b.Maximum&&x.Enabled==y.Enabled&&x.AutoHide==y.AutoHide&&x.Fade==y.Fade&&x.Smooth==y.Smooth&&x.Emissive==y.Emissive&&x.DynamicColors==y.DynamicColors&&x.HoldSeconds==y.HoldSeconds&&x.FadeSeconds==y.FadeSeconds&&x.SmoothingSeconds==y.SmoothingSeconds&&x.GlowPercent==y.GlowPercent&&x.TrackOpacity==y.TrackOpacity&&string.Equals(x.LowColor,y.LowColor,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.MediumColor,y.MediumColor,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.HighColor,y.HighColor,StringComparison.OrdinalIgnoreCase);
    }
    internal void Observe(EnergyConfiguration current){
        if(cursor>=0&&Same(states[cursor],current))return;
        if(cursor<states.Count-1)states.RemoveRange(cursor+1,states.Count-cursor-1);
        states.Add(current.Copy());cursor=states.Count-1;if(states.Count>MaximumSteps+1){states.RemoveAt(0);cursor--;}
    }
    internal bool Undo(out EnergyConfiguration state){state=null;if(!CanUndo)return false;state=states[--cursor].Copy();return true;}
    internal bool Redo(out EnergyConfiguration state){state=null;if(!CanRedo)return false;state=states[++cursor].Copy();return true;}
    internal static void Verify(){
        var baseline=new EnergyConfiguration{Options=new EnergyBarOptions(),Bounds=new Rectangle(710,971,484,4),Locked=true,Maximum=113900};var history=new ConfigurationHistory();history.Observe(baseline);
        EnergyConfiguration value;if(history.CanUndo||history.CanRedo||history.Undo(out value)||history.Redo(out value))throw new Exception("Empty undo/redo must do nothing");
        var changed=baseline.Copy();changed.Options.LowColor="#123456";changed.Options.AutoHide=false;changed.Bounds=new Rectangle(600,900,300,3);changed.Maximum=120000;changed.Locked=false;history.Observe(changed);
        if(!history.Undo(out value)||!Same(value,baseline)||!history.CanRedo)throw new Exception("Undo must restore all preferences/geometry/maximum/lock");
        history.Observe(value);if(!history.CanRedo)throw new Exception("Saving identical preferences must preserve redo");
        value.Options.LowColor="#FFFFFF";if(!history.Redo(out value)||!Same(value,changed))throw new Exception("History snapshots must not alias mutable preferences");
        changed.Options.LowColor="#000000";if(!history.Undo(out value)||!Same(value,baseline))throw new Exception("Recording must copy the source");
        var branch=baseline.Copy();branch.Options.GlowPercent=150;history.Observe(branch);if(history.CanRedo||history.UndoCount!=1)throw new Exception("New changes after undo must clear redo");
        history=new ConfigurationHistory();history.Observe(baseline);for(uint i=1;i<=150;i++){var step=baseline.Copy();step.Maximum=baseline.Maximum+i;history.Observe(step);}
        if(history.UndoCount!=100)throw new Exception("History must remain bounded");for(int i=0;i<100;i++)if(!history.Undo(out value))throw new Exception("Bounded undo");if(value.Maximum!=baseline.Maximum+50||history.CanUndo)throw new Exception("Oldest retained snapshot");
    }
}
