using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Threading.Tasks;

internal sealed class VisualTweaksPane:StackPanel {
    readonly EnergyOverlay owner;readonly Window window;readonly VisualTweaks service;
    readonly ComboBox files=new ComboBox{MinHeight=38,HorizontalAlignment=HorizontalAlignment.Stretch};
    readonly TextBlock path=SettingsVisual.Text("",11,"#A7ADBA"),status=SettingsVisual.Text("",12,"#E4B986");
    readonly CheckBox[] effects=new CheckBox[VisualTweaks.Keys.Length];readonly Button apply,restore,detect;VisualTweaks.State state;byte[] snapshot;bool loading,started,disposed,searching;string message="";
    internal VisualTweaksPane(EnergyOverlay helper,Window settings,VisualTweaks tweaks=null){owner=helper;window=settings;service=tweaks??new VisualTweaks(AppDomain.CurrentDomain.BaseDirectory);window.Closed+=delegate{disposed=true;};
        var card=new StackPanel();Children.Add(SettingsVisual.Card(card,new Thickness(18,16,18,16)));card.Children.Add(SettingsVisual.Text("Visual effects",18));
        Note(card,"Enable an effect to use the game's settings. Disable it to add a zero override to Engine.ini. With an unmodified INI, all effects default to enabled.");
        var warning=SettingsVisual.Text("Changes with the game open are not guaranteed to take effect. Close Aion 2 before applying, then relaunch it to check the result.",12,"#E4B986");warning.Margin=new Thickness(0,10,0,12);card.Children.Add(warning);
        string[] names={"Depth of Field","Chromatic Aberration","Film Grain","Motion Blur","Selection outline"};string[] descriptions={"Blurs scenery outside the focus distance.","Adds colored fringes around edges.","Adds the cinematic film grain effect.","Blurs movement of the camera and objects.","Highlights selected targets, with or without target lock. Disabling this also disables other highlights that use Custom Depth."};
        for(int n=0;n<effects.Length;n++){var row=new Grid{Margin=new Thickness(0,8,0,8)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var text=new StackPanel();text.Margin=new Thickness(0,0,12,0);text.Children.Add(SettingsVisual.Text(names[n],14));Note(text,descriptions[n]);row.Children.Add(text);var toggle=new CheckBox{IsChecked=true,VerticalAlignment=VerticalAlignment.Center};effects[n]=toggle;Grid.SetColumn(toggle,1);row.Children.Add(toggle);AutomationProperties.SetName(toggle,names[n]);toggle.Click+=delegate{if(!loading)UpdateActions();};card.Children.Add(row);}
        Note(card,"Enabled means no helper disable override, not a guarantee that the game renders the effect. Game graphics settings and supported Unreal variables still apply.");
        var config=new StackPanel();var outer=SettingsVisual.Card(config,new Thickness(18,16,18,16));outer.Margin=new Thickness(0,14,0,0);Children.Add(outer);config.Children.Add(SettingsVisual.Text("Configuration file",16));Note(config,"Engine.ini is detected under LocalAppData/AION2 for Steam and other launchers. If several files exist, select the profile you use. Browse manually if detection fails.");
        files.Margin=new Thickness(0,8,0,8);config.Children.Add(files);files.SelectionChanged+=delegate{if(!loading&&files.SelectedItem!=null)Act(delegate{service.ChooseFile(Convert.ToString(files.SelectedItem));});};UiLanguage.PreserveDataText(files);UiLanguage.PreserveDataText(path);config.Children.Add(path);
        var find=Actions(config);detect=AddButton(find,"Detect configuration files",Discover);AddButton(find,"Browse Engine.ini",Browse);status.Margin=new Thickness(0,12,0,8);config.Children.Add(status);
        var actions=Actions(config);apply=AddButton(actions,"Apply visual changes",delegate{var desired=new bool[effects.Length];for(int i=0;i<effects.Length;i++)desired[i]=effects[i].IsChecked==true;Act(delegate{service.Apply(desired,snapshot);},"Visual settings saved. Relaunch Aion 2 to check the result.");},true);
        restore=AddButton(actions,"Restore previous visual change",delegate{Act(service.Restore,"Previous visual settings restored. Relaunch Aion 2 to check the result.");});AddButton(actions,"Refresh status",Refresh);
        Note(config,"Only these visual settings are edited. Other settings are preserved. Verified backups stay outside the game configuration folder; Engine.ini remains writable. Restore undoes the last visual change while preserving unrelated edits.");
        Note(config,"Nothing is applied until you click Apply visual changes. Game updates may rewrite the INI; Refresh status reads the file without changing it. Settings undo/redo does not edit game files.");
    }
    static void Note(StackPanel parent,string text){var t=SettingsVisual.Text(text,11,"#929CAD");t.Margin=new Thickness(0,6,0,6);t.LineHeight=16;parent.Children.Add(t);}
    static WrapPanel Actions(StackPanel parent){var p=new WrapPanel{Margin=new Thickness(0,8,0,4)};parent.Children.Add(p);return p;}
    static Button AddButton(Panel row,string text,Action action,bool primary=false){var b=SettingsVisual.Button(text,action,primary);b.Margin=new Thickness(0,0,8,8);row.Children.Add(b);return b;}
    void Browse(){var dialog=new Microsoft.Win32.OpenFileDialog{Title=UiLanguage.Text("Select the game's Engine.ini",owner.Language),Filter="Engine.ini|Engine.ini",CheckFileExists=true};if(service.ConfigPath.Length!=0&&Directory.Exists(Path.GetDirectoryName(service.ConfigPath)))dialog.InitialDirectory=Path.GetDirectoryName(service.ConfigPath);if(dialog.ShowDialog(window)==true)Act(delegate{service.ChooseFile(dialog.FileName);});}
    void Act(Action action,string success=null){try{action();Refresh();if(success!=null)SetMessage(success);}catch(Exception e){SetMessage(e.Message);}}
    void SetMessage(string text){message=text;status.Text=UiLanguage.Text(text,owner.Language);}
    internal void Open(){Refresh();if(!started){started=true;Discover();}}
    internal void Refresh(){loading=true;try{state=service.Inspect();snapshot=state.Snapshot;for(int i=0;i<effects.Length;i++)effects[i].IsChecked=state.Enabled[i];path.Text=service.ConfigPath;SetMessage(state.Message);UpdateActions();}catch(Exception e){snapshot=null;apply.IsEnabled=restore.IsEnabled=false;SetMessage(e.Message);}finally{loading=false;}}
    void UpdateActions(){bool dirty=false;if(state!=null)for(int i=0;i<effects.Length;i++)dirty|=(effects[i].IsChecked==true)!=state.Enabled[i];apply.IsEnabled=state!=null&&state.CanApply&&snapshot!=null&&dirty;restore.IsEnabled=state!=null&&state.CanRestore;}
    void Discover(){if(searching||disposed)return;searching=true;detect.IsEnabled=false;SetMessage("Looking for Engine.ini...");Task.Factory.StartNew<string[]>(delegate{return VisualTweaks.FindFiles();}).ContinueWith(delegate(Task<string[]> task){var found=task.IsFaulted?new string[0]:task.Result;if(task.IsFaulted)task.Exception.Handle(delegate(Exception error){return true;});try{window.Dispatcher.BeginInvoke(new Action(delegate{if(disposed)return;searching=false;detect.IsEnabled=true;loading=true;files.Items.Clear();foreach(string p in found)files.Items.Add(p);files.SelectedItem=service.ConfigPath;loading=false;
            if(found.Length==1&&!service.Inspect().Found)Act(delegate{service.ChooseFile(found[0]);});else{Refresh();if(!state.Found)SetMessage(found.Length>1?"Several Engine.ini files found. Select the profile you use.":"Engine.ini was not found. Choose the file manually.");}
        }));}catch(InvalidOperationException){}});}
    internal void Translate(){status.Text=UiLanguage.Text(message,owner.Language);UiLanguage.Apply(this,owner.Language);}
    internal void Verify(){Refresh();if(effects.Length!=5||snapshot==null||!state.Found)throw new Exception("Visual menu reads five independent effects");for(int i=0;i<effects.Length;i++){bool value=effects[i].IsChecked==true;effects[i].IsChecked=!value;UpdateActions();if(!apply.IsEnabled)throw new Exception("Visual choices need explicit apply");effects[i].IsChecked=value;UpdateActions();if(apply.IsEnabled)throw new Exception("Unchanged visual choices must not write");}}
}
