using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Image = System.Windows.Controls.Image;

internal sealed class CombatLogSettingsPane : ScrollViewer {
    readonly EnergyOverlay owner;
    readonly CombatLogPainter painter = new CombatLogPainter();
    readonly StackPanel content = new StackPanel { Margin = new Thickness(0,0,12,0) };
    readonly Image preview = new Image { Height = 192, Stretch = Stretch.Uniform };
    readonly CheckBox enabled = new CheckBox(), blur = new CheckBox(), time = new CheckBox(), tags = new CheckBox();
    readonly CheckBox autoHide = new CheckBox(), newestAtBottom = new CheckBox();
    readonly ComboBox format = new ComboBox { Width = 225, Height = 32 };
    readonly SettingsNumber opacity = new SettingsNumber("Background opacity",0,100,0,1);
    readonly SettingsNumber width = new SettingsNumber("Panel width",CombatLogOptions.MinWidth,CombatLogOptions.MaxWidth,0,1);
    readonly SettingsNumber height = new SettingsNumber("Panel height",CombatLogOptions.MinHeight,CombatLogOptions.MaxHeight,0,1);
    readonly SettingsNumber idleSeconds = new SettingsNumber("Hide after inactivity",1,300,0,1);
    readonly StackPanel idleOptions = new StackPanel();
    CombatLogOptions shown;
    string language;
    bool loading, released;

    internal CombatLogSettingsPane(EnergyOverlay owner) {
        this.owner=owner;
        Content=content;
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;
        SetResourceReference(StyleProperty,"SlimScroll");

        var group=Group("PREVIEW");
        group.Children.Add(preview);
        group.Children.Add(SettingsVisual.Text("Sample combat events",11,"#8994A6"));

        group=Group("BEHAVIOR");
        Row(group,"Enable combat log",enabled);
        Row(group,"Line format",format);
        foreach (var name in new[]{"Icon, name and damage","Icon and damage","Name and damage"})
            format.Items.Add(new ComboBoxItem{Content=name});
        Row(group,"Show timestamps",time);
        Row(group,"Show hit label",tags);
        Row(group,"New entries at the bottom",newestAtBottom);
        Row(group,"Auto-hide when inactive",autoHide);
        group.Children.Add(idleOptions);
        NumberRow(idleOptions,"Hide after inactivity","s",idleSeconds);
        group.Children.Add(SettingsVisual.Text("Shows your supported outgoing hits while Aion is in focus. Recent hits are kept for this session.",11,"#8994A6"));
        group.Children.Add(SettingsVisual.Text("Use the bar on the right to review earlier hits. The log stays visible while you scroll.",11,"#8994A6"));

        group=Group("APPEARANCE");
        Row(group,"Backdrop blur",blur);
        NumberRow(group,"Background opacity","%",opacity);

        group=Group("POSITION & SIZE");
        NumberRow(group,"Panel width","px",width);
        NumberRow(group,"Panel height","px",height);
        group.Children.Add(SettingsVisual.Text("Unlock widgets in General to move or resize.",11,"#8994A6"));

        enabled.Click+=delegate{Apply(o=>o.Enabled=enabled.IsChecked==true);};
        blur.Click+=delegate{Apply(o=>o.Blur=blur.IsChecked==true);};
        time.Click+=delegate{Apply(o=>o.ShowTime=time.IsChecked==true);};
        tags.Click+=delegate{Apply(o=>o.ShowHitType=tags.IsChecked==true);};
        newestAtBottom.Click+=delegate{Apply(o=>o.NewestAtBottom=newestAtBottom.IsChecked==true);};
        autoHide.Click+=delegate{Apply(o=>o.AutoHide=autoHide.IsChecked==true);};
        idleSeconds.Changed+=delegate{Apply(o=>o.IdleSeconds=(int)idleSeconds.Value);};
        format.SelectionChanged+=delegate{if(format.SelectedIndex>=0)Apply(o=>o.Format=format.SelectedIndex);};
        opacity.Changed+=delegate{Apply(o=>o.BackgroundOpacity=(int)opacity.Value);};
        width.Changed+=delegate{Apply(o=>o.Bounds.Width=(int)width.Value);};
        height.Changed+=delegate{Apply(o=>o.Bounds.Height=(int)height.Value);};
        Reload();
    }

    StackPanel Group(string title) {
        var heading=SettingsVisual.Text(title,11,"#929CAD");
        heading.Margin=new Thickness(2,14,0,8);
        content.Children.Add(heading);
        var group=new StackPanel();
        content.Children.Add(SettingsVisual.Card(group,new Thickness(16,10,16,10)));
        return group;
    }

    static void Row(StackPanel group,string label,FrameworkElement control) {
        var row=new Grid{MinHeight=40};
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var text=SettingsVisual.Text(label);
        text.VerticalAlignment=VerticalAlignment.Center;
        row.Children.Add(text);
        AutomationProperties.SetName(control,label);
        Grid.SetColumn(control,1);
        control.VerticalAlignment=VerticalAlignment.Center;
        row.Children.Add(control);
        group.Children.Add(row);
    }
    static void NumberRow(StackPanel group,string label,string unit,SettingsNumber number) {
        var panel=new StackPanel{Orientation=Orientation.Horizontal};
        panel.Children.Add(number);
        var suffix=SettingsVisual.Text(unit,11,"#8994A6");
        suffix.Margin=new Thickness(6,0,0,0);
        suffix.VerticalAlignment=VerticalAlignment.Center;
        panel.Children.Add(suffix);
        Row(group,label,panel);
    }

    void Apply(Action<CombatLogOptions> change) {
        if (loading||released) return;
        var cfg=owner.Configuration;
        change(cfg.CombatLog);
        owner.ApplyConfiguration(cfg);
    }

    internal void Reload() {
        if (released) return;
        var o=owner.Configuration.CombatLog;
        if (shown!=null&&CombatLogOptions.Same(o,shown)&&language==owner.Language) return;
        loading=true;
        try {
            language=owner.Language;
            enabled.IsChecked=o.Enabled;blur.IsChecked=o.Blur;time.IsChecked=o.ShowTime;tags.IsChecked=o.ShowHitType;
            autoHide.IsChecked=o.AutoHide;newestAtBottom.IsChecked=o.NewestAtBottom;idleSeconds.Value=o.IdleSeconds;
            idleOptions.Visibility=o.AutoHide?Visibility.Visible:Visibility.Collapsed;
            format.SelectedIndex=o.Format;opacity.Value=o.BackgroundOpacity;width.Value=o.Bounds.Width;height.Value=o.Bounds.Height;
            using (var bitmap=painter.Render(o.Bounds.Size,o,CombatLogPainter.Sample,language,true,false))
                preview.Source=SettingsVisual.Bitmap(bitmap);
            shown=o;
            UiLanguage.Apply(this,language);
        } finally { loading=false; }
    }

    internal void VerifySizingAndAutoHide() {
        ((TextBox)width.Children[1]).Text="420";width.Commit();
        ((TextBox)height.Children[1]).Text="166";height.Commit();
        autoHide.IsChecked=true;autoHide.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        newestAtBottom.IsChecked=true;newestAtBottom.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        ((TextBox)idleSeconds.Children[1]).Text="12";idleSeconds.Commit();
        var o=owner.Configuration.CombatLog;
        if(o.Bounds.Width!=420||o.Bounds.Height!=166||!o.AutoHide||!o.NewestAtBottom||o.IdleSeconds!=12||idleOptions.Visibility!=Visibility.Visible)
            throw new Exception("Combat log size and timeout must update through the settings controls");
    }

    internal void Release() {
        if (released) return;
        released=true;
        preview.Source=null;
        painter.Dispose();
    }
}
