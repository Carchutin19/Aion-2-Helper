using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

internal sealed class EnergyBarOptions {
    public bool Enabled=true, AutoHide=true, Fade=true, Smooth=true, Emissive=true, DynamicColors=true;
    public double HoldSeconds=1.5, FadeSeconds=.2, SmoothingSeconds=.07;
    public int GlowPercent=100, TrackOpacity=170;
    public string LowColor="#A02D2D", MediumColor="#B85416", HighColor="#287044";
    string lowCache,mediumCache,highCache;Color lowPaint,mediumPaint,highPaint;
    static Color CachedColor(string value,ref string cached,ref Color color){if(cached!=value){color=ColorTranslator.FromHtml(value);cached=value;}return color;}
    internal Color LowPaint {get{return CachedColor(LowColor,ref lowCache,ref lowPaint);}}
    internal Color MediumPaint {get{return CachedColor(MediumColor,ref mediumCache,ref mediumPaint);}}
    internal Color HighPaint {get{return CachedColor(HighColor,ref highCache,ref highPaint);}}
    internal EnergyBarOptions Copy(){return (EnergyBarOptions)MemberwiseClone();}
    internal static bool ValidColor(string value){int hex;return value!=null&&value.Length==7&&value[0]=='#'&&int.TryParse(value.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out hex);}
    internal void Normalize(){
        HoldSeconds=Finite(HoldSeconds,1.5,0,30);FadeSeconds=Finite(FadeSeconds,.2,0,2);SmoothingSeconds=Finite(SmoothingSeconds,.07,0,1);
        GlowPercent=Math.Max(0,Math.Min(200,GlowPercent));TrackOpacity=Math.Max(0,Math.Min(255,TrackOpacity));
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
    internal EnergyConfiguration Copy(){return new EnergyConfiguration{Options=Options.Copy(),Bounds=Bounds,Locked=Locked,Maximum=Maximum,Language=Language};}
}

// Session-only history: 100 undo steps, with independent immutable snapshots.
// Recording happens only when preferences are saved, never on energy packets.
internal sealed class ConfigurationHistory {
    const int MaximumSteps=100;
    readonly List<EnergyConfiguration> states=new List<EnergyConfiguration>();int cursor=-1;
    internal bool CanUndo {get{return cursor>0;}}
    internal bool CanRedo {get{return cursor>=0&&cursor<states.Count-1;}}
    internal int UndoCount {get{return Math.Max(0,cursor);}}
    internal static bool Same(EnergyConfiguration a,EnergyConfiguration b){
        var x=a.Options;var y=b.Options;
        return UiLanguage.Normalize(a.Language)==UiLanguage.Normalize(b.Language)&&a.Bounds==b.Bounds&&a.Locked==b.Locked&&a.Maximum==b.Maximum&&x.Enabled==y.Enabled&&x.AutoHide==y.AutoHide&&x.Fade==y.Fade&&x.Smooth==y.Smooth&&x.Emissive==y.Emissive&&x.DynamicColors==y.DynamicColors&&x.HoldSeconds==y.HoldSeconds&&x.FadeSeconds==y.FadeSeconds&&x.SmoothingSeconds==y.SmoothingSeconds&&x.GlowPercent==y.GlowPercent&&x.TrackOpacity==y.TrackOpacity&&string.Equals(x.LowColor,y.LowColor,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.MediumColor,y.MediumColor,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.HighColor,y.HighColor,StringComparison.OrdinalIgnoreCase);
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
