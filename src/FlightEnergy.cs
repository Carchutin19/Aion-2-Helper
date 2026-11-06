using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Web.Script.Serialization;

internal sealed class FlightEnergyOptions {
    public EnergyBarOptions Bar=DefaultBar();
    public bool RechargeOnly=true;
    public Rectangle Bounds=new Rectangle(710,935,BarDesign.DefaultWidth,BarDesign.DefaultHeight);

    internal static EnergyBarOptions DefaultBar(){return new EnergyBarOptions{Enabled=false,CurrentText=true,MaximumText=true};}
    internal FlightEnergyOptions Copy(){return new FlightEnergyOptions{Bar=Bar.Copy(),RechargeOnly=RechargeOnly,Bounds=Bounds};}
    internal void Normalize(){
        if(Bar==null)Bar=DefaultBar();
        Bar.Normalize();
        Bounds=new Rectangle(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y)),
            Math.Max(BarDesign.MinimumWidth,Math.Min(BarDesign.MaximumWidth,Bounds.Width)),Math.Max(BarDesign.MinimumHeight,Math.Min(BarDesign.MaximumHeight,Bounds.Height)));
    }
    internal static FlightEnergyOptions Read(Dictionary<string,object> config){
        var result=new FlightEnergyOptions();object data;
        if(config.TryGetValue("flightEnergy",out data)){
            try{result=new JavaScriptSerializer().ConvertToType<FlightEnergyOptions>(data)??result;}
            catch(ArgumentException){}catch(InvalidOperationException){}
        }
        result.Normalize();return result;
    }
    internal static bool Same(FlightEnergyOptions a,FlightEnergyOptions b){return a.RechargeOnly==b.RechargeOnly&&a.Bounds==b.Bounds&&EnergyBarOptions.Same(a.Bar,b.Bar);}
}

internal sealed class FlightEnergyOverlay:IDisposable {
    internal const uint UnitsPerPoint=100, PreviewMaximum=2500;
    readonly EnergyOverlay owner;
    readonly MovableOverlayWindow window=new MovableOverlayWindow("Flight Energy");
    readonly ProgressTextOverlay text=new ProgressTextOverlay();
    readonly BarMotion motion=new BarMotion();
    readonly BarDesign.GlowWorkspace glow=new BarDesign.GlowWorkspace();
    FlightEnergyOptions options;
    PlayerVitalSignal.FlightReading live;
    internal int NextInterval {get;private set;}
    internal string StatusKey {get{return live.Current+"/"+live.Maximum+"/"+live.Recharging+"/"+window.Visible+"/"+(options!=null&&options.Bar.Enabled);}}
    internal object Status {get{return new{current=live.Current,maximum=live.Maximum,unitsPerPoint=UnitsPerPoint,recharging=live.Recharging,visible=window.Visible};}}
    internal string ReadingStatus {get{
        if(options==null||!options.Bar.Enabled)return "Flight Energy disabled";
        if(!live.Known)return "Waiting for flight energy data";
        return (live.Current/UnitsPerPoint).ToString(CultureInfo.InvariantCulture)+" / "+(live.Maximum/UnitsPerPoint).ToString(CultureInfo.InvariantCulture);
    }}

    internal FlightEnergyOverlay(EnergyOverlay owner){
        this.owner=owner;
        NextInterval=1000;
        window.LimitResize=delegate(Size size){
            var core=CoreBounds(new Rectangle(Point.Empty,size));
            core.Width=Math.Max(BarDesign.MinimumWidth,Math.Min(BarDesign.MaximumWidth,core.Width));
            core.Height=Math.Max(BarDesign.MinimumHeight,Math.Min(BarDesign.MaximumHeight,core.Height));
            return WindowBounds(core).Size;
        };
        window.Moved=delegate(Rectangle bounds){var config=owner.Configuration;config.FlightEnergy.Bounds=CoreBounds(bounds);owner.ApplyConfiguration(config);};
    }
    static Rectangle WindowBounds(Rectangle core){core.Inflate(BarDesign.GlowPadding,BarDesign.GlowPadding);return core;}
    static Rectangle CoreBounds(Rectangle window){window.Inflate(-BarDesign.GlowPadding,-BarDesign.GlowPadding);return window;}
    internal void Configure(){window.InvalidateImage();}
    internal static bool ShowReading(PlayerVitalSignal.FlightReading reading,FlightEnergyOptions settings){
        if(!reading.Known)return false;
        if(!settings.RechargeOnly)return true;
        return reading.Recharging&&(reading.Current<reading.Maximum||settings.Bar.AutoHide);
    }
    internal void Maintain(FlightEnergyOptions settings,PlayerVitalSignal.FlightReading reading,bool editing,double now,double utc){
        options=settings;live=reading;
        var bar=settings.Bar;
        bool sample=editing&&!reading.Known;
        uint current=sample?PreviewMaximum*65:reading.Current,maximum=sample?PreviewMaximum*UnitsPerPoint:reading.Maximum;
        double ratio=maximum==0?0:(double)current/maximum;
        bool show=ShowReading(reading,settings);
        // A new session should not fade in with the previous character's fill.
        if(motion.Opacity==0&&!editing&&show)motion.SnapValue(ratio);
        motion.Step(now,editing,sample||reading.Known,ratio,current,maximum,bar,
            reading.Known?Math.Max(0,utc-reading.Time):-1,show);
        byte alpha=(byte)Math.Round(motion.Opacity*255);
        if(!bar.Enabled||alpha==0||owner.WidgetsSuppressed||DpsDesign.ShellUiActive()){
            window.HideOverlay();text.Clear();
        }else{
            var core=settings.Bounds;double value=motion.Value;
            string key=Math.Round(value*core.Width)+"/"+BarDesign.EnergyColor(value,bar).ToArgb()+"/"+editing;
            window.Render(WindowBounds(core),alpha,editing,key,delegate(Size size){
                int pad=2*BarDesign.GlowPadding;
                return BarDesign.RenderEmissive(size.Width-pad,size.Height-pad,value,editing,true,1,bar,glow);
            });
            text.Update(CoreBounds(window.PresentedBounds),bar,sample||reading.Known,current/UnitsPerPoint,maximum/UnitsPerPoint,alpha);
        }
        NextInterval=!bar.Enabled?1000:!owner.WidgetsSuppressed&&motion.Animating&&(alpha>0||show||editing)?16:250;
    }
    public void Dispose(){text.Dispose();window.Dispose();}
}
