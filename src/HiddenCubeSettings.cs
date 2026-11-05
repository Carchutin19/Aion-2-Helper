using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

internal sealed class HiddenCubeSettingsPane:ScrollViewer {
    readonly EnergyOverlay owner;
    readonly Window window;
    readonly HiddenCubePainter painter=new HiddenCubePainter();
    readonly Image preview=new Image{Height=130,Stretch=Stretch.Uniform};
    readonly StackPanel content=new StackPanel{Margin=new Thickness(0,0,12,0)};
    readonly List<Action<HiddenCubeOptions>> refresh=new List<Action<HiddenCubeOptions>>();
    HiddenCubeOptions shown;
    string language;
    bool loading,released;
    internal HiddenCubeSettingsPane(EnergyOverlay owner,Window window){
        this.owner=owner;this.window=window;
        Content=content;
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;
        SetResourceReference(StyleProperty,"SlimScroll");
        var group=Group("PREVIEW");
        group.Children.Add(preview);
        group.Children.Add(SettingsVisual.Text("Sample hidden cube",11,"#8994A6"));
        group=Group("BEHAVIOR");
        Toggle(group,"Enable Hidden Cube Radar",o=>o.Enabled,(o,v)=>o.Enabled=v);
        group.Children.Add(SettingsVisual.Text("Shows an alert when the game reports a supported nearby cube.",11,"#8994A6"));
        Toggle(group,"Fade",o=>o.Fade,(o,v)=>o.Fade=v);
        Number(group,"Fade duration","ms",0,1500,o=>o.FadeMs,(o,v)=>o.FadeMs=v,10);
        group=Group("APPEARANCE");
        Toggle(group,"Backdrop blur",o=>o.Blur,(o,v)=>o.Blur=v);
        Number(group,"Background opacity","%",0,100,o=>o.BackgroundOpacity,(o,v)=>o.BackgroundOpacity=v);
        Number(group,"Widget opacity","%",0,100,o=>o.Opacity,(o,v)=>o.Opacity=v);
        var color=SettingsVisual.Button("Accent color",delegate{
            var picker=new HelperColorPicker(owner.Configuration.HiddenCube.Accent,"Accent color",owner.Language){Owner=window};
            if(picker.ShowDialog()==true)Apply(o=>o.Accent=picker.SelectedHex);
        });
        Row(group,"Accent color",color);
        refresh.Add(o=>{color.Content=o.Accent;color.Foreground=SettingsVisual.Brush(o.Accent);});
        group=Group("POSITION & SIZE");
        Number(group,"Horizontal position","px",-100000,100000,o=>o.Bounds.X,(o,v)=>o.Bounds.X=v);
        Number(group,"Vertical position","px",-100000,100000,o=>o.Bounds.Y,(o,v)=>o.Bounds.Y=v);
        Number(group,"Panel width","px",HiddenCubeOptions.MinWidth,HiddenCubeOptions.MaxWidth,o=>o.Bounds.Width,(o,v)=>o.Bounds.Width=v);
        Number(group,"Panel height","px",HiddenCubeOptions.MinHeight,HiddenCubeOptions.MaxHeight,o=>o.Bounds.Height,(o,v)=>o.Bounds.Height=v);
        group.Children.Add(SettingsVisual.Text("Unlock widgets in General to move or resize.",11,"#8994A6"));
        Reload();
    }
    void Apply(Action<HiddenCubeOptions> edit){
        if(loading||released)return;
        var cfg=owner.Configuration;
        edit(cfg.HiddenCube);owner.ApplyConfiguration(cfg);
    }
    StackPanel Group(string heading){
        var text=SettingsVisual.Text(heading,11,"#929CAD");
        text.Margin=new Thickness(2,14,0,8);content.Children.Add(text);
        var panel=new StackPanel();content.Children.Add(SettingsVisual.Card(panel,new Thickness(16,10,16,10)));
        return panel;
    }
    static void Row(StackPanel group,string name,FrameworkElement control){
        var row=new Grid{MinHeight=40};
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var label=SettingsVisual.Text(name);label.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(label);
        AutomationProperties.SetName(control,name);Grid.SetColumn(control,1);control.VerticalAlignment=VerticalAlignment.Center;
        row.Children.Add(control);group.Children.Add(row);
    }
    void Toggle(StackPanel group,string name,Func<HiddenCubeOptions,bool> get,Action<HiddenCubeOptions,bool> set){
        var box=new CheckBox();Row(group,name,box);
        box.Click+=delegate{Apply(o=>set(o,box.IsChecked==true));};refresh.Add(o=>box.IsChecked=get(o));
    }
    void Number(StackPanel group,string name,string unit,int min,int max,Func<HiddenCubeOptions,int> get,Action<HiddenCubeOptions,int> set,int step=1){
        var field=new SettingsNumber(name,min,max,0,step);
        var panel=new StackPanel{Orientation=Orientation.Horizontal};
        var suffix=SettingsVisual.Text(unit,11,"#8994A6");
        suffix.Margin=new Thickness(6,0,0,0);suffix.VerticalAlignment=VerticalAlignment.Center;
        panel.Children.Add(field);panel.Children.Add(suffix);Row(group,name,panel);
        field.Changed+=delegate{Apply(o=>set(o,(int)field.Value));};refresh.Add(o=>field.Value=get(o));
    }
    internal void Reload(){
        if(released)return;
        var o=owner.Configuration.HiddenCube;
        if(shown!=null&&HiddenCubeOptions.Same(shown,o)&&language==owner.Language)return;
        loading=true;
        try{
            language=owner.Language;
            foreach(var action in refresh)action(o);
            using(var b=painter.Render(o.Bounds.Size,o,HiddenCubePainter.Sample,language,false))
                preview.Source=SettingsVisual.Bitmap(b);
            preview.Opacity=o.Opacity/100.0;
            shown=o;UiLanguage.Apply(this,language);
        }finally{loading=false;}
    }
    internal void Release(){
        if(released)return;released=true;
        preview.Source=null;painter.Dispose();
    }
}
