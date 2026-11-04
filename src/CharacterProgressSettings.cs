using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Drawing = System.Drawing;

internal sealed class CharacterProgressSettingsPane : ScrollViewer {
    readonly EnergyOverlay owner;
    readonly Window window;
    readonly bool power;
    readonly StackPanel content = new StackPanel { Margin = new Thickness(0,0,12,0) };
    readonly Image preview = new Image { Height = 54, Stretch = Stretch.Uniform };
    readonly List<Action<CharacterProgressOptions>> refresh = new List<Action<CharacterProgressOptions>>();
    CharacterProgressOptions shown;
    string language;

    internal CharacterProgressSettingsPane(EnergyOverlay owner, Window window, bool power) {
        this.owner = owner;
        this.window = window;
        this.power = power;
        Content = content;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        SetResourceReference(StyleProperty,"SlimScroll");

        var group = Group("PREVIEW");
        group.Children.Add(preview);
        var note = SettingsVisual.Text("Sample value · not a game reading",11,"#8994A6");
        note.HorizontalAlignment = HorizontalAlignment.Center;
        group.Children.Add(note);

        group = Group("BEHAVIOR");
        if (power) {
            Toggle(group,"Show Combat Power",o=>o.Power.Enabled,(o,v)=>o.Power.Enabled=v);
            Toggle(group,"Show the second value",o=>o.AlternatePower,(o,v)=>o.AlternatePower=v);
            group.Children.Add(SettingsVisual.Text("Click the widget while locked to switch between both values.",11,"#8994A6"));
        } else {
            Toggle(group,"Show experience bar",o=>o.Experience.Enabled,(o,v)=>o.Experience.Enabled=v);
            Toggle(group,"Hide when no XP is gained",o=>o.Experience.AutoHide,(o,v)=>o.Experience.AutoHide=v);
            Number(group,"Time visible after gaining XP","s",1,30,o=>(decimal)o.Experience.HoldSeconds,(o,v)=>o.Experience.HoldSeconds=(double)v);
            Toggle(group,"Fade",o=>o.Experience.Fade,(o,v)=>o.Experience.Fade=v);
            Number(group,"Fade duration","ms",0,2000,o=>(decimal)(o.Experience.FadeSeconds*1000),(o,v)=>o.Experience.FadeSeconds=(double)v/1000,25);
            Toggle(group,"Hide at maximum level",o=>o.HideAtCap,(o,v)=>o.HideAtCap=v);
        }

        group = Group("APPEARANCE");
        if (power) {
            Color(group,"Text and icon color",o=>o.Power.Color,(o,v)=>o.Power.Color=v);
            Toggle(group,"Dark background",o=>o.Power.Background,(o,v)=>o.Power.Background=v);
            Number(group,"Background opacity","%",0,100,o=>o.Power.BackgroundOpacity*100m/255,(o,v)=>o.Power.BackgroundOpacity=(int)Math.Round(v*255/100));
            Number(group,"Background softness","px",0,24,o=>o.Power.Softness,(o,v)=>o.Power.Softness=(int)v);
        } else {
            Color(group,"Bar color",o=>o.Experience.HighColor,(o,v)=>o.Experience.HighColor=v);
            Color(group,"Text color",o=>o.Experience.TextColor,(o,v)=>o.Experience.TextColor=v);
            Number(group,"Text size","px",10,80,o=>o.Experience.TextSize,(o,v)=>o.Experience.TextSize=(int)v);
            Toggle(group,"Show current value",o=>o.Experience.CurrentText,(o,v)=>o.Experience.CurrentText=v);
            Toggle(group,"Show maximum value",o=>o.Experience.MaximumText,(o,v)=>o.Experience.MaximumText=v);
            Toggle(group,"Show percentage",o=>o.Experience.PercentageText,(o,v)=>o.Experience.PercentageText=v);
            Toggle(group,"Glow",o=>o.Experience.Emissive,(o,v)=>o.Experience.Emissive=v);
            Number(group,"Glow intensity","%",0,200,o=>o.Experience.GlowPercent,(o,v)=>o.Experience.GlowPercent=(int)v);
            Number(group,"Track opacity","%",0,100,o=>o.Experience.TrackOpacity*100m/255,(o,v)=>o.Experience.TrackOpacity=(int)Math.Round(v*255/100));
        }

        group = Group("POSITION & SIZE");
        Number(group,"Horizontal position","px",-100000,100000,o=>Bounds(o).X,(o,v)=>{var b=Bounds(o);b.X=(int)v;Bounds(o,b);});
        Number(group,"Vertical position","px",-100000,100000,o=>Bounds(o).Y,(o,v)=>{var b=Bounds(o);b.Y=(int)v;Bounds(o,b);});
        Number(group,"Width","px",power?48:120,power?1600:2400,o=>Bounds(o).Width,(o,v)=>{var b=Bounds(o);b.Width=(int)v;Bounds(o,b);});
        Number(group,"Height","px",power?20:3,power?400:60,o=>Bounds(o).Height,(o,v)=>{var b=Bounds(o);b.Height=(int)v;Bounds(o,b);});
        group.Children.Add(SettingsVisual.Text("Unlock widgets in General to move or resize.",11,"#8994A6"));
        Reload();
    }

    Drawing.Rectangle Bounds(CharacterProgressOptions o) { return power ? o.Power.Bounds : o.ExperienceBounds; }
    void Bounds(CharacterProgressOptions o, Drawing.Rectangle b) { if (power) o.Power.Bounds=b; else o.ExperienceBounds=b; }
    void Apply(Action<CharacterProgressOptions> edit) {
        var cfg=owner.Configuration;
        edit(cfg.CharacterProgress);
        owner.ApplyConfiguration(cfg);
    }
    StackPanel Group(string title) {
        var heading=SettingsVisual.Text(title,11,"#929CAD");
        heading.Margin=new Thickness(2,14,0,8);
        content.Children.Add(heading);
        var panel=new StackPanel();
        content.Children.Add(SettingsVisual.Card(panel,new Thickness(16,10,16,10)));
        return panel;
    }
    void Row(StackPanel group, string label, FrameworkElement control) {
        var row=new Grid { MinHeight=40 };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var text=SettingsVisual.Text(label);
        text.VerticalAlignment=VerticalAlignment.Center;
        text.Margin=new Thickness(0,0,12,0);
        row.Children.Add(text);
        Grid.SetColumn(control,1);
        row.Children.Add(control);
        group.Children.Add(row);
    }
    void Toggle(StackPanel group,string label,Func<CharacterProgressOptions,bool> read,Action<CharacterProgressOptions,bool> write) {
        var box=new CheckBox { VerticalAlignment=VerticalAlignment.Center };
        AutomationProperties.SetName(box,label);
        Row(group,label,box);
        box.Click+=delegate { Apply(o=>write(o,box.IsChecked==true)); };
        refresh.Add(o=>box.IsChecked=read(o));
    }
    void Number(StackPanel group,string label,string unit,int min,int max,Func<CharacterProgressOptions,decimal> read,Action<CharacterProgressOptions,decimal> write,int step=1) {
        var panel=new StackPanel { Orientation=Orientation.Horizontal, VerticalAlignment=VerticalAlignment.Center };
        var field=new SettingsNumber(label,min,max,0,step);
        panel.Children.Add(field);
        var suffix=SettingsVisual.Text(unit,11,"#8994A6");
        suffix.Margin=new Thickness(6,0,0,0);
        suffix.VerticalAlignment=VerticalAlignment.Center;
        panel.Children.Add(suffix);
        Row(group,label,panel);
        field.Changed+=delegate { Apply(o=>write(o,field.Value)); };
        refresh.Add(o=>field.Value=read(o));
    }
    void Color(StackPanel group,string label,Func<CharacterProgressOptions,string> read,Action<CharacterProgressOptions,string> write) {
        var button=SettingsVisual.Button("Choose color",delegate {
            var picker=new HelperColorPicker(read(owner.Configuration.CharacterProgress),label,owner.Language) { Owner=window };
            if (picker.ShowDialog()==true) Apply(o=>write(o,picker.SelectedHex));
        });
        Row(group,label,button);
        refresh.Add(o=>{button.Content=read(o);button.Foreground=SettingsVisual.Brush(read(o));});
    }
    internal void Reload() {
        var options=owner.Configuration.CharacterProgress;
        if (shown!=null && CharacterProgressOptions.Same(shown,options) && language==owner.Language) return;
        foreach (var action in refresh) action(options);
        language=owner.Language;
        var b=Bounds(options);
        var size=new Drawing.Size(Math.Min(power?380:490,b.Width),Math.Min(72,b.Height));
        using (var bitmap=power
            ? CharacterProgressDesign.Power(size,options.AlternatePower?1248ul:120ul,options.AlternatePower,options.Power)
            : CharacterProgressDesign.Experience(size,10,28208,46777,options.Experience,language))
            preview.Source=SettingsVisual.Bitmap(bitmap);
        shown=options;
        UiLanguage.Apply(this,language);
    }
}
