using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Drawing=System.Drawing;

internal sealed class OdyleEnergySettingsPane : Grid {
    readonly EnergyOverlay owner;
    readonly HelperSettings parent;
    readonly StackPanel roster=new StackPanel();
    readonly TextBlock active=SettingsVisual.Text(""), message=SettingsVisual.Text("",12,"#A5C9EF");
    readonly TextBox name=new TextBox { Width=210 }, server=new TextBox { Width=210 }, balance=new TextBox { Width=210 };
    readonly ComboBox maximum=new ComboBox { Width=210, Height=34 };
    readonly SettingsNumber next=new SettingsNumber("Minutes until next recharge",1,OdyleLedger.IntervalMinutes,0,1)
        { Value=OdyleLedger.IntervalMinutes };
    readonly Image preview=new Image { MaxHeight=62, MaxWidth=320, HorizontalAlignment=HorizontalAlignment.Center };
    readonly Button useActive;
    readonly List<Action<OdyleOptions>> reloaders=new List<Action<OdyleOptions>>();
    readonly List<Action<DateTime>> clocks=new List<Action<DateTime>>();
    OdyleOptions shown;
    string language,formMessage;
    int revision=-1;
    DateTime nextRefresh;

    internal OdyleEnergySettingsPane(EnergyOverlay owner,HelperSettings parent) {
        this.owner=owner;
        this.parent=parent;
        var panel=new StackPanel { Margin=new Thickness(0,0,12,12) };
        var scroll=new ScrollViewer { Content=panel, VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
        scroll.SetResourceReference(FrameworkElement.StyleProperty,"SlimScroll");
        Children.Add(scroll);

        var widget=Group(panel,"OVERLAY");
        Toggle(widget,"Show current character's Odyle energy",o=>o.Widget.Enabled,(o,v)=>o.Widget.Enabled=v);
        Toggle(widget,"Show Odyle maximum",o=>o.ShowMaximum,(o,v)=>o.ShowMaximum=v);
        widget.Children.Add(preview);
        var note=SettingsVisual.Text("Sample value · not a game reading",11,"#929CAD");
        note.HorizontalAlignment=HorizontalAlignment.Center;
        widget.Children.Add(note);

        var characters=Group(panel,"CHARACTERS");
        characters.Children.Add(active);
        UiLanguage.PreserveDataText(active);
        var help=SettingsVisual.Text("Enter the rechargeable balance shown in AION 2. The Helper estimates +15 every 3 hours, even while closed. Update the balance after spending or claiming energy.",12,"#A5C9EF");
        help.Margin=new Thickness(0,8,0,10);
        characters.Children.Add(help);
        characters.Children.Add(roster);

        var editor=Group(panel,"ODYLE BALANCE");
        useActive=SettingsVisual.Button("Use current character",delegate { Fill(owner.Odyle.Active); });
        editor.Children.Add(useActive);
        Row(editor,"Character name",name);
        Row(editor,"Server ID",server);
        Row(editor,"Rechargeable balance",balance);
        maximum.Items.Add("560");
        maximum.Items.Add("840");
        maximum.SelectedIndex=0;
        Row(editor,"Recharge limit",maximum);
        Row(editor,"Minutes until next recharge",next);
        editor.Children.Add(SettingsVisual.Text("Use the in-game countdown if available. The default starts a new 3-hour interval.",11,"#929CAD"));
        var save=SettingsVisual.Button("Save balance",Save);
        save.Margin=new Thickness(0,8,0,0);
        editor.Children.Add(save);
        message.Margin=new Thickness(0,6,0,0);
        UiLanguage.PreserveDataText(message);
        editor.Children.Add(message);

        var appearance=Group(panel,"APPEARANCE");
        var color=SettingsVisual.Button("Choose color",delegate {
            var picker=new HelperColorPicker(owner.OdyleOptions.Widget.Color,"Text color",owner.Language){Owner=this.parent};
            if (picker.ShowDialog()==true) Apply(o=>o.Widget.Color=picker.SelectedHex);
        });
        Row(appearance,"Text color",color);
        reloaders.Add(o=>{color.Content=o.Widget.Color;color.Foreground=SettingsVisual.Brush(o.Widget.Color);});
        Toggle(appearance,"Dark background",o=>o.Widget.Background,(o,v)=>o.Widget.Background=v);
        Number(appearance,"Background opacity",0,100,o=>Math.Round(o.Widget.BackgroundOpacity*100m/255),
            (o,v)=>o.Widget.BackgroundOpacity=(int)Math.Round(v*255/100m));
        Number(appearance,"Background softness",0,24,o=>o.Widget.Softness,(o,v)=>o.Widget.Softness=v);
        var position=Group(panel,"POSITION & SIZE");
        Number(position,"Horizontal position",-100000,100000,o=>o.Widget.Bounds.X,(o,v)=>{var b=o.Widget.Bounds;b.X=v;o.Widget.Bounds=b;});
        Number(position,"Vertical position",-100000,100000,o=>o.Widget.Bounds.Y,(o,v)=>{var b=o.Widget.Bounds;b.Y=v;o.Widget.Bounds=b;});
        Number(position,"Width",48,1600,o=>o.Widget.Bounds.Width,(o,v)=>{var b=o.Widget.Bounds;b.Width=v;o.Widget.Bounds=b;});
        Number(position,"Height",20,400,o=>o.Widget.Bounds.Height,(o,v)=>{var b=o.Widget.Bounds;b.Height=v;o.Widget.Bounds=b;});
        position.Children.Add(SettingsVisual.Text("Unlock widgets in General to move or resize.",11,"#929CAD"));
        Reload();
    }

    static StackPanel Group(StackPanel parent,string title) {
        var heading=SettingsVisual.Text(title,11,"#929CAD");
        heading.Margin=new Thickness(2,14,0,8);
        parent.Children.Add(heading);
        var content=new StackPanel();
        parent.Children.Add(SettingsVisual.Card(content,new Thickness(16,10,16,10)));
        return content;
    }
    static void Row(StackPanel group,string label,FrameworkElement control) {
        var row=new Grid { MinHeight=40 };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var text=SettingsVisual.Text(label);
        text.Margin=new Thickness(0,0,12,0);
        row.Children.Add(text);
        control.VerticalAlignment=VerticalAlignment.Center;
        AutomationProperties.SetName(control,label);
        Grid.SetColumn(control,1);
        row.Children.Add(control);
        group.Children.Add(row);
    }
    void Apply(Action<OdyleOptions> update) {
        var config=owner.Configuration;
        update(config.Odyle);
        owner.ApplyConfiguration(config);
    }
    void Toggle(StackPanel panel,string label,Func<OdyleOptions,bool> read,Action<OdyleOptions,bool> set) {
        var control=new CheckBox();
        Row(panel,label,control);
        control.Click+=delegate { Apply(o=>set(o,control.IsChecked==true)); };
        reloaders.Add(o=>control.IsChecked=read(o));
    }
    void Number(StackPanel panel,string label,int min,int max,Func<OdyleOptions,decimal> read,Action<OdyleOptions,int> set) {
        var control=new SettingsNumber(label,min,max,0,1);
        Row(panel,label,control);
        control.Changed+=delegate { Apply(o=>set(o,(int)control.Value)); };
        reloaders.Add(o=>control.Value=read(o));
    }
    string T(string key) { return UiLanguage.Text(key,owner.Language); }
    void SetMessage(string key) {
        formMessage=key;
        message.Text=T(owner.Odyle.Error??formMessage??"");
    }
    void Fill(OdyleLedger.Entry entry) {
        if (entry==null) return;
        var now=DateTime.UtcNow;
        var reading=entry.Read(now);
        name.Text=entry.Name;
        server.Text=entry.Server.ToString(CultureInfo.InvariantCulture);
        balance.Text=reading.Energy.HasValue?reading.Energy.Value.ToString(CultureInfo.InvariantCulture):"";
        maximum.SelectedIndex=entry.Maximum==OdyleLedger.MemberMaximum?1:0;
        next.Value=reading.NextUtc.HasValue?Math.Max(1,Math.Ceiling((decimal)(reading.NextUtc.Value-now).TotalMinutes)):OdyleLedger.IntervalMinutes;
        SetMessage(null);
        balance.BringIntoView();
        balance.Focus();
        balance.SelectAll();
    }
    void Save() {
        next.Commit();
        int serverId,energy;
        int limit=maximum.SelectedIndex==1?OdyleLedger.MemberMaximum:OdyleLedger.StandardMaximum;
        bool saved=int.TryParse(server.Text,out serverId) && int.TryParse(balance.Text,out energy)
            && owner.Odyle.Save(name.Text.Trim(),serverId,energy,limit,(int)next.Value,DateTime.UtcNow);
        SetMessage(saved?"Odyle balance saved.":"Check the name, server ID and balance.");
        UpdateRoster();
    }
    void RebuildRoster() {
        var ledger=owner.Odyle;
        active.Text=ledger.Active==null?T("Enter a character to detect it automatically."):
            string.Format(T("Current character · {0} · Server {1}"),ledger.Active.Name,ledger.Active.Server);
        useActive.IsEnabled=ledger.Active!=null;
        roster.Children.Clear();
        clocks.Clear();
        foreach (var entry in ledger.Entries.OrderByDescending(e=>e==ledger.Active).ThenBy(e=>e.Name).ThenBy(e=>e.Server)) {
            var card=new StackPanel();
            var title=SettingsVisual.Text(string.Format(T("{0} · Server {1}"),entry.Name,entry.Server));
            UiLanguage.PreserveDataText(title);
            card.Children.Add(title);
            var info=SettingsVisual.Text("",12,"#A5C9EF");
            UiLanguage.PreserveDataText(info);
            card.Children.Add(info);
            var buttons=new WrapPanel { Margin=new Thickness(0,6,0,0) };
            card.Children.Add(buttons);
            AddButton(buttons,"Edit balance",delegate { Fill(entry); });
            var spend40=AddButton(buttons,"−40",delegate { Spend(entry,40); });
            var spend80=AddButton(buttons,"−80",delegate { Spend(entry,80); });
            AddButton(buttons,"Remove",delegate {
                if (MessageBox.Show(parent,string.Format(T("Remove the saved Odyle balance for {0}?"),entry.Name),T("Odyle Energy"),
                    MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
                bool removed=ledger.Remove(entry);
                SetMessage(removed?null:"Could not save Odyle energy.");
                UpdateRoster();
            });
            clocks.Add(now=>{
                var reading=entry.Read(now);
                spend40.IsEnabled=reading.Energy>=40;
                spend80.IsEnabled=reading.Energy>=80;
                if (!reading.Energy.HasValue) { info.Text=T("Enter a balance to start tracking."); return; }
                string value=OdyleDesign.Value(reading.Energy.Value,entry.Maximum,true);
                info.Text=reading.FullUtc.HasValue
                    ?value+" · "+string.Format(T("Estimated full · {0}"),reading.FullUtc.Value.ToLocalTime().ToString("g",CultureInfo.GetCultureInfo(owner.Language)))
                    :value+" · "+T("At maximum");
            });
            var border=SettingsVisual.Card(card,new Thickness(12,8,12,8));
            border.Margin=new Thickness(0,0,0,6);
            roster.Children.Add(border);
        }
        UiLanguage.Apply(roster,owner.Language);
        revision=ledger.Revision;
    }
    static Button AddButton(Panel group,string label,Action action) {
        var button=SettingsVisual.Button(label,action);
        button.Margin=new Thickness(0,0,5,4);
        group.Children.Add(button);
        return button;
    }
    void Spend(OdyleLedger.Entry entry,int amount) {
        bool spent=owner.Odyle.Spend(entry,amount,DateTime.UtcNow);
        SetMessage(spent?"Odyle balance saved.":"Could not save Odyle energy.");
        UpdateRoster();
    }
    internal void UpdateRoster() {
        var now=DateTime.UtcNow;
        if (revision!=owner.Odyle.Revision) { RebuildRoster(); nextRefresh=DateTime.MinValue; }
        if (now<nextRefresh) return;
        nextRefresh=now.AddSeconds(1);
        foreach (var refresh in clocks) refresh(now);
    }
    internal void Reload() {
        var options=owner.OdyleOptions;
        bool translate=language!=owner.Language;
        if (shown==null || !OdyleOptions.Same(shown,options)) {
            foreach (var update in reloaders) update(options);
            var size=options.Widget.Bounds.Size;
            using (var bitmap=OdyleDesign.Render(new Drawing.Size(Math.Min(320,size.Width),Math.Min(62,size.Height)),325,560,options))
                preview.Source=SettingsVisual.Bitmap(bitmap);
            shown=options.Copy();
        }
        if (translate) {
            language=owner.Language;
            revision=-1;
            SetMessage(formMessage);
            UiLanguage.Apply(this,language);
        }
        UpdateRoster();
    }
}
