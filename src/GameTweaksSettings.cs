using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Threading.Tasks;

internal sealed class GameTweaksPane:ScrollViewer {
    readonly EnergyOverlay owner;readonly Window window;readonly IntroTweaks tweaks;readonly StackPanel content=new StackPanel();readonly VisualTweaksPane visuals;readonly Button[] tabs=new Button[2];
    readonly TextBlock folder=SettingsVisual.Text("",11,"#A7ADBA"),status=SettingsVisual.Text("",12,"#E4B986");
    readonly CheckBox skip=new CheckBox();readonly Button apply,restore,detect;bool refreshing,started,detecting,disposed;string statusMessage="";IntroTweaks.State state;
    internal GameTweaksPane(EnergyOverlay helper,Window settings,IntroTweaks service=null,VisualTweaks visualService=null){owner=helper;window=settings;tweaks=service??new IntroTweaks(AppDomain.CurrentDomain.BaseDirectory);
        var root=new StackPanel{Margin=new Thickness(0,0,12,0)};Content=root;VerticalScrollBarVisibility=ScrollBarVisibility.Auto;HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;SetResourceReference(StyleProperty,"SlimScroll");
        var navigation=new Grid{Margin=new Thickness(0,0,0,14)};root.Children.Add(navigation);for(int n=0;n<2;n++){int selected=n;navigation.ColumnDefinitions.Add(new ColumnDefinition());var tab=SettingsVisual.Button(n==0?"Startup":"Visuals",delegate{SelectVisuals(selected==1);});tabs[n]=tab;Grid.SetColumn(tab,n);navigation.Children.Add(tab);}root.Children.Add(content);visuals=new VisualTweaksPane(owner,window,visualService){Visibility=Visibility.Collapsed};root.Children.Add(visuals);tabs[0].Background=SettingsVisual.Brush("#3067C7B0");
        content.Children.Add(SettingsVisual.Text("STARTUP",11,"#929CAD"));var body=new StackPanel();content.Children.Add(SettingsVisual.Card(body,new Thickness(18,16,18,16)));
        body.Children.Add(SettingsVisual.Text("Skip the NCSOFT intro",18));AddText(body,"Shorten startup using our own one-frame black video. The expected game file stays present so playback can finish normally. No external mod is needed. This changes a game file; it does not improve gameplay FPS.",12);
        var switchRow=new Grid{Margin=new Thickness(0,16,0,14)};switchRow.ColumnDefinitions.Add(new ColumnDefinition());switchRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});switchRow.Children.Add(SettingsVisual.Text("Skip intro"));Grid.SetColumn(skip,1);switchRow.Children.Add(skip);body.Children.Add(switchRow);AutomationProperties.SetName(skip,"Skip intro");skip.Click+=delegate{if(!refreshing)UpdateActions();};
        AddText(body,"GAME FOLDER",10);body.Children.Add(folder);AddText(body,"The helper detects the game installation automatically. Choose a folder manually only if detection fails or you want another installation.",11);var folderActions=Actions(body);detect=Button(folderActions,"Detect game folder",DetectFolder);Button(folderActions,"Browse folder",ChooseFolder);window.Closed+=delegate{disposed=true;};

        status.Margin=new Thickness(0,16,0,14);status.LineHeight=17;body.Children.Add(status);var actions=Actions(body);apply=Button(actions,"Apply change",delegate{Act(delegate{if(skip.IsChecked==true)tweaks.Apply();else tweaks.Restore();});},true);restore=Button(actions,"Restore original",delegate{Act(tweaks.Restore);});var check=Button(actions,"Refresh status",Refresh);check.ToolTip="Checks the installed video and backup without changing game files.";
        AddText(body,"A verified copy of the original video is kept outside the game folder. Restore original puts it back. Refresh status only checks the installation and backup; game updates may restore the intro.",11);
        AddText(body,"Changes apply only when you click Apply change or Restore original. Settings undo/redo does not alter game files. This tweak stays applied when the helper is closed.",11);
    }
    static StackPanel Actions(StackPanel parent){var row=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,14)};parent.Children.Add(row);return row;}
    static Button Button(StackPanel parent,string text,Action action,bool primary=false){var button=SettingsVisual.Button(text,action,primary);button.Margin=new Thickness(0,0,8,0);parent.Children.Add(button);return button;}
    static void AddText(StackPanel parent,string text,double size){var block=SettingsVisual.Text(text,size,"#929CAD");block.Margin=new Thickness(0,8,0,8);block.LineHeight=17;parent.Children.Add(block);}
    void ChooseFolder(){using(var dialog=new System.Windows.Forms.FolderBrowserDialog{Description=UiLanguage.Text("Select the Aion 2 installation folder",owner.Language),SelectedPath=tweaks.GameFolder})if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)Act(delegate{tweaks.ChooseFolder(dialog.SelectedPath);});}
    void Act(Action action){try{action();Refresh();}catch(Exception e){statusMessage=e.Message;status.Text=UiLanguage.Text(statusMessage,owner.Language);}}
    void DetectFolder(){if(detecting||disposed)return;detecting=true;detect.IsEnabled=false;string before=tweaks.GameFolder;statusMessage="Looking for the game installation...";status.Text=UiLanguage.Text(statusMessage,owner.Language);
        // One worker per search; no polling and no drive-wide scans on the UI thread.
        Task.Factory.StartNew<string>(new Func<string>(IntroTweaks.FindGameFolder)).ContinueWith(delegate(Task<string> task){string found=task.IsFaulted?"":task.Result;if(task.IsFaulted)task.Exception.Handle(delegate(Exception error){return true;});
            try{window.Dispatcher.BeginInvoke(new Action(delegate{if(disposed)return;detecting=false;detect.IsEnabled=true;
                if(tweaks.GameFolder!=before){Refresh();return;}if(found.Length!=0)Act(delegate{tweaks.ChooseFolder(found);});else{Refresh();statusMessage="Game folder not found. Select it manually.";status.Text=UiLanguage.Text(statusMessage,owner.Language);}
            }));}catch(InvalidOperationException){}
        });
    }
    internal void Open(){bool discover=!started&&!GameInstallation.IsValid(tweaks.GameFolder);started=true;Refresh();if(discover)DetectFolder();}
    internal void Refresh(){refreshing=true;try{state=tweaks.Inspect();skip.IsChecked=state.Active;folder.Text=tweaks.GameFolder.Length==0?UiLanguage.Text("No game folder selected",owner.Language):tweaks.GameFolder;statusMessage=detecting?"Looking for the game installation...":state.Message;status.Text=UiLanguage.Text(statusMessage,owner.Language);UpdateActions();}finally{refreshing=false;}}
    void UpdateActions(){if(state==null)return;bool desired=skip.IsChecked==true;skip.IsEnabled=state.CanApply||state.CanRestore;apply.IsEnabled=desired!=state.Active&&(desired?state.CanApply:state.CanRestore);restore.IsEnabled=state.CanRestore;}
    internal void SelectVisuals(bool active=true){content.Visibility=active?Visibility.Collapsed:Visibility.Visible;visuals.Visibility=active?Visibility.Visible:Visibility.Collapsed;for(int n=0;n<2;n++)tabs[n].Background=SettingsVisual.Brush((n==1)==active?"#3067C7B0":"#102F3542");if(active)visuals.Open();Translate();ScrollToTop();}
    internal void VerifyVisuals(){SelectVisuals();visuals.Verify();}
    internal void Translate(){if(state!=null){status.Text=UiLanguage.Text(statusMessage,owner.Language);if(tweaks.GameFolder.Length==0)folder.Text=UiLanguage.Text("No game folder selected",owner.Language);}visuals.Translate();UiLanguage.Apply(this,owner.Language);}
}
