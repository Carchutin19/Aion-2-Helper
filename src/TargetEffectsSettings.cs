using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

internal sealed class TargetEffectsSettingsPane:StackPanel {
    readonly Dictionary<string,FrameworkElement> fields=new Dictionary<string,FrameworkElement>();
    readonly EnergyOverlay owner;
    readonly List<Action<BuffOverlayOptions>> reloaders=new List<Action<BuffOverlayOptions>>();
    readonly Image preview=new Image{Height=70,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,10,0,6)};
    BuffOverlayOptions previous;string language;bool loading;
    internal TargetEffectsSettingsPane(EnergyOverlay owner){
        this.owner=owner;Margin=new Thickness(0,18,0,0);
        var title=SettingsVisual.Text("TARGET BUFFS & DEBUFFS",11,"#BBD8FF");title.Margin=new Thickness(2,0,0,10);Children.Add(title);
        var panel=new StackPanel();Children.Add(SettingsVisual.Card(panel,new Thickness(16,10,16,10)));
        var hint=SettingsVisual.Text("Follows the same locked or unlocked target as its health bar. Hidden when that bar is hidden. Unlock widgets in General to move this group; its offsets stay relative to the target bar.",12,"#A7ADBA");hint.Margin=new Thickness(0,4,0,12);panel.Children.Add(hint);
        Toggle(panel,"Show target effects",o=>o.Enabled,(o,v)=>o.Enabled=v);
        Toggle(panel,"Show buffs",o=>o.ShowBuffs,(o,v)=>o.ShowBuffs=v);
        Toggle(panel,"Show debuffs",o=>o.ShowDebuffs,(o,v)=>o.ShowDebuffs=v);
        Toggle(panel,"Blue buff / red debuff borders",o=>o.TypeBorders,(o,v)=>o.TypeBorders=v);
        Choice(panel,"Effect arrangement",new[]{"Together","Buffs right, debuffs left","Buffs left, debuffs right","Buffs above, debuffs below","Debuffs above, buffs below"},o=>o.LayoutMode,(o,v)=>o.LayoutMode=v);
        Choice(panel,"Combined effect order",new[]{"Buffs first","Debuffs first"},o=>o.DebuffsFirst?1:0,(o,v)=>o.DebuffsFirst=v==1);
        Choice(panel,"Effect style",new[]{"Glass","Compact"},o=>o.Style,(o,v)=>o.Style=v);
        Choice(panel,"Buff growth direction",new[]{"Right","Left","Down","Up"},o=>o.LayoutMode==0?o.Grow:o.Direction(false),(o,v)=>{if(o.LayoutMode==0)o.Grow=v;else o.BuffGrow=v;});
        Choice(panel,"Debuff growth direction",new[]{"Right","Left","Down","Up"},o=>o.Direction(true),(o,v)=>o.DebuffGrow=v);
        Toggle(panel,"Show buff names",o=>o.Names,(o,v)=>o.Names=v);
        Toggle(panel,"Show buff timer inside the icon",o=>o.ShowTimer,(o,v)=>o.ShowTimer=v);
        Toggle(panel,"Amber pulse in the last 4 seconds",o=>o.WarnBeforeExpiry,(o,v)=>o.WarnBeforeExpiry=v);
        Toggle(panel,"Backdrop blur",o=>o.Blur,(o,v)=>o.Blur=v);
        panel.Children.Add(preview);
        Number(panel,"Buff group scale","%",50,180,o=>o.Scale,(o,v)=>o.Scale=v);
        Number(panel,"Buff spacing","px",0,40,o=>o.Gap,(o,v)=>o.Gap=v);
        Number(panel,"Maximum visible effects","",1,64,o=>o.MaximumIcons,(o,v)=>o.MaximumIcons=v);
        Number(panel,"Widget opacity","%",10,100,o=>o.Opacity,(o,v)=>o.Opacity=v);
        Number(panel,"Target bar horizontal offset","px",-10000,10000,o=>o.X,(o,v)=>o.X=v);
        Number(panel,"Target bar vertical offset","px",-10000,10000,o=>o.Y,(o,v)=>o.Y=v);
    }
    void Row(StackPanel panel,string label,FrameworkElement control){
        fields[label]=control;var row=new Grid{MinHeight=42};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var text=SettingsVisual.Text(label,13);text.VerticalAlignment=VerticalAlignment.Center;text.Margin=new Thickness(0,0,12,0);row.Children.Add(text);
        Grid.SetColumn(control,1);row.Children.Add(control);panel.Children.Add(row);AutomationProperties.SetName(control,label);
    }
    void Change(Action<BuffOverlayOptions> change){if(loading)return;var cfg=owner.Configuration;change(cfg.EnemyHealth.Effects);owner.ApplyConfiguration(cfg);}
    void Toggle(StackPanel panel,string label,Func<BuffOverlayOptions,bool> get,Action<BuffOverlayOptions,bool> set){var field=new CheckBox{VerticalAlignment=VerticalAlignment.Center};Row(panel,label,field);field.Click+=delegate{Change(o=>set(o,field.IsChecked==true));};reloaders.Add(o=>field.IsChecked=get(o));}
    void Choice(StackPanel panel,string label,string[] names,Func<BuffOverlayOptions,int> get,Action<BuffOverlayOptions,int> set){var field=new ComboBox{Width=255};foreach(string name in names)field.Items.Add(new ComboBoxItem{Content=name});Row(panel,label,field);field.SelectionChanged+=delegate{if(field.SelectedIndex>=0)Change(o=>set(o,field.SelectedIndex));};reloaders.Add(o=>{field.SelectedIndex=get(o);if(label=="Debuff growth direction")field.IsEnabled=o.LayoutMode!=0;if(label=="Combined effect order")field.IsEnabled=o.LayoutMode==0;});}
    void Number(StackPanel panel,string label,string unit,int min,int max,Func<BuffOverlayOptions,int> get,Action<BuffOverlayOptions,int> set){var field=new SettingsNumber(label,min,max,0,1);var side=new StackPanel{Orientation=Orientation.Horizontal};side.Children.Add(field);var suffix=SettingsVisual.Text(unit,11,"#8994A6");suffix.Margin=new Thickness(6,0,0,0);side.Children.Add(suffix);Row(panel,label,side);field.Changed+=delegate{Change(o=>set(o,(int)field.Value));};reloaders.Add(o=>field.Value=get(o));}
    internal void Reload(){
        var options=owner.Configuration.EnemyHealth.Effects;
        if(previous!=null&&BuffOverlayOptions.Same(previous,options)&&language==owner.Language)return;
        loading=true;try{foreach(var reload in reloaders)reload(options);using(var image=owner.TargetBuffs.Preview(options,owner.Language))preview.Source=SettingsVisual.Bitmap(image);preview.Opacity=options.Opacity/100.0;previous=options.Copy();language=owner.Language;UiLanguage.Apply(this,language);}finally{loading=false;}
    }
    internal void VerifyControls(){
        var original=owner.Configuration;
        try{
            ((ComboBox)fields["Effect arrangement"]).SelectedIndex=original.EnemyHealth.Effects.LayoutMode==3?4:3;Reload();
            if(owner.Configuration.EnemyHealth.Effects.LayoutMode==original.EnemyHealth.Effects.LayoutMode||!BuffOverlayOptions.Same(original.BuffOverlay,owner.Configuration.BuffOverlay))throw new Exception("Target arrangement changes only target effects");
            ((ComboBox)fields["Buff growth direction"]).SelectedIndex=2;((ComboBox)fields["Debuff growth direction"]).SelectedIndex=3;Reload();
            if(owner.Configuration.EnemyHealth.Effects.BuffGrow!=2||owner.Configuration.EnemyHealth.Effects.DebuffGrow!=3)throw new Exception("Independent target directions persist");
            ((ComboBox)fields["Effect arrangement"]).SelectedIndex=0;Reload();((ComboBox)fields["Combined effect order"]).SelectedIndex=1;Reload();
            if(!owner.Configuration.EnemyHealth.Effects.DebuffsFirst||fields["Debuff growth direction"].IsEnabled||!fields["Combined effect order"].IsEnabled)throw new Exception("Combined target effect order follows its setting");
            var borders=(CheckBox)fields["Blue buff / red debuff borders"];borders.IsChecked=!original.EnemyHealth.Effects.TypeBorders;borders.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Reload();
            if(owner.Configuration.EnemyHealth.Effects.TypeBorders==original.EnemyHealth.Effects.TypeBorders)throw new Exception("Target border toggle must persist");
            var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();
            var restored=serializer.Deserialize<EnemyHealthOptions>(serializer.Serialize(owner.Configuration.EnemyHealth));restored.Normalize();
            if(!BuffOverlayOptions.Same(restored.Effects,owner.Configuration.EnemyHealth.Effects))throw new Exception("Target effects survive preference serialization");
        }finally{owner.ApplyConfiguration(original);Reload();}
    }

}
