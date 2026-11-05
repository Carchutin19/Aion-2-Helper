using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;

internal sealed class PerformanceSettingsPane : StackPanel {
    readonly EnergyOverlay owner;
    readonly Window window;
    readonly VisualTweaks service;
    readonly DispatcherTimer poll=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
    readonly DispatcherTimer gamePoll=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
    readonly ComboBox cap=new ComboBox{MinWidth=170,MinHeight=34};
    readonly SettingsNumber custom=new SettingsNumber("Custom FPS limit",20,500,0,1);
    readonly TextBlock path=SettingsVisual.Text("",11,"#929CAD"), configStatus=SettingsVisual.Text("",12,"#E4B986"),
        testStatus=SettingsVisual.Text("",12,"#E4B986"), results=SettingsVisual.Text("",13);
    readonly Button apply,restore,measure;
    readonly int[] limits={0,60,90,120,-1};
    VisualTweaks.PerformanceState state;
    string configMessage="";
    bool loading;

    internal PerformanceSettingsPane(EnergyOverlay owner,Window window,VisualTweaks service){
        this.owner=owner;this.window=window;this.service=service;
        foreach(var label in new[]{"Use game settings","60 FPS","90 FPS","120 FPS","Custom"})
            cap.Items.Add(new ComboBoxItem{Content=label});
        var test=Card("Frame pacing test");
        Note(test,"Run the same route twice to compare FPS and slow frames. Return to Aion 2 after starting the test and keep it in focus.");
        measure=Button(Actions(test),"Measure for 60 seconds",ToggleTest,true);
        test.Children.Add(testStatus);test.Children.Add(results);
        UiLanguage.PreserveDataText(testStatus);UiLanguage.PreserveDataText(results);UiLanguage.PreserveDataText(measure);
        poll.Tick+=delegate{RefreshTest();};
        gamePoll.Tick+=delegate{RefreshAfterGameChange();};
        IsVisibleChanged+=delegate{if(IsVisible)gamePoll.Start();else gamePoll.Stop();};
        window.Closed+=delegate{poll.Stop();gamePoll.Stop();};

        var settings=Card("FPS limit");
        Note(settings,"Set a limit to try steadier frame pacing. Close Aion 2 to apply it, then relaunch and measure again.");
        var controls=Actions(settings);
        cap.Margin=new Thickness(0,0,8,6);controls.Children.Add(cap);AutomationProperties.SetName(cap,"FPS limit");
        custom.Value=120;controls.Children.Add(custom);
        cap.SelectionChanged+=delegate{if(!loading)UpdateActions();};
        custom.Changed+=delegate{if(!loading)UpdateActions();};
        var files=Actions(settings);
        Button(files,"Browse Engine.ini",Browse);Button(files,"Refresh status",Refresh);
        settings.Children.Add(path);settings.Children.Add(configStatus);
        UiLanguage.PreserveDataText(path);UiLanguage.PreserveDataText(configStatus);
        var actions=Actions(settings);
        apply=Button(actions,"Apply FPS limit",delegate{custom.Commit();Change(()=>service.ApplyFrameLimit(Desired,state.Snapshot));},true);
        restore=Button(actions,"Restore previous FPS limit",delegate{Change(service.RestoreFrameLimit);});
        Note(settings,"This changes only the FPS limit in Engine.ini. Visual effects are configured separately.");
    }
    int Desired{get{int i=cap.SelectedIndex;return i<0?0:limits[i]<0?(int)custom.Value:limits[i];}}
    StackPanel Card(string title){
        var panel=new StackPanel();panel.Children.Add(SettingsVisual.Text(title,18));
        var card=SettingsVisual.Card(panel,new Thickness(18,16,18,16));card.Margin=new Thickness(0,0,0,14);Children.Add(card);return panel;
    }
    static WrapPanel Actions(StackPanel parent){var panel=new WrapPanel{Margin=new Thickness(0,8,0,4)};parent.Children.Add(panel);return panel;}
    static void Note(StackPanel parent,string key){var text=SettingsVisual.Text(key,11,"#929CAD");text.Margin=new Thickness(0,8,0,4);parent.Children.Add(text);}
    static Button Button(Panel parent,string key,Action click,bool primary=false){
        var button=SettingsVisual.Button(key,click,primary);button.Margin=new Thickness(0,0,8,6);parent.Children.Add(button);return button;
    }
    void Browse(){
        var dialog=new Microsoft.Win32.OpenFileDialog{Title=T("Select the game's Engine.ini"),Filter="Engine.ini|Engine.ini",CheckFileExists=true};
        if(service.ConfigPath.Length>0&&Directory.Exists(Path.GetDirectoryName(service.ConfigPath)))dialog.InitialDirectory=Path.GetDirectoryName(service.ConfigPath);
        if(dialog.ShowDialog(window)==true){try{service.ChooseFile(dialog.FileName);ReadConfig(false);}catch(Exception e){ShowConfig(e.Message);}}
    }
    string T(string key){return UiLanguage.Text(key,owner.Language);}
    void ShowConfig(string message){configMessage=message;configStatus.Text=T(message);}
    void Change(Action action){
        try{action();ReadConfig(false);ShowConfig("FPS limit updated. Relaunch Aion 2 to test it.");}
        catch(Exception e){Refresh();ShowConfig(e.Message);}
    }
    void UpdateActions(){
        custom.Visibility=cap.SelectedIndex==4?Visibility.Visible:Visibility.Collapsed;
        apply.IsEnabled=state!=null&&state.CanApply&&HasPendingLimit;
        restore.IsEnabled=state!=null&&state.CanRestore;
    }
    bool HasPendingLimit{get{return state!=null&&cap.SelectedIndex>=0&&state.Override!=(Desired==0?null:Desired.ToString(CultureInfo.InvariantCulture));}}
    internal void Refresh(){ReadConfig(true);}
    void RefreshAfterGameChange(){if(state!=null&&state.Snapshot!=null&&state.GameRunning!=service.GameRunning)Refresh();}
    void ReadConfig(bool keepSelection){
        bool pending=keepSelection&&path.Text==service.ConfigPath&&(HasPendingLimit||custom.IsKeyboardFocusWithin);
        loading=true;
        try{
            state=service.InspectPerformance();path.Text=service.ConfigPath;
            int current,index=0;
            if(state.Override!=null){
                if(int.TryParse(state.Override,NumberStyles.Integer,CultureInfo.InvariantCulture,out current)&&current>=20&&current<=500){
                    index=Array.IndexOf(limits,current);
                    if(index<0){index=4;if(!pending)custom.Value=current;}
                }else index=-1;
            }
            if(!pending)cap.SelectedIndex=index;
            if(index<0&&state.Message.Length==0)ShowConfig("An existing FPS override uses a value outside this control.");
            else ShowConfig(state.Message);
        }finally{loading=false;UpdateActions();}
    }
    internal void Open(){Refresh();Translate();}
    void ToggleTest(){
        string status;FpsBenchmarkResult previous,latest;
        if(owner.PerformanceTestState(out status,out previous,out latest))owner.CancelPerformanceTest();
        else owner.BeginPerformanceTest();
        RefreshTest();
    }
    string Result(string label,FpsBenchmarkResult result){
        return result==null?"":string.Format(T("{0} · {1:0.0} FPS average · p99 {2:0.0} ms · {3} frames over 50 ms"),T(label),result.Average,result.P99,result.SlowFrames);
    }
    void RefreshTest(){
        string status;FpsBenchmarkResult previous,latest;
        bool active=owner.PerformanceTestState(out status,out previous,out latest);
        if(active)poll.Start();else poll.Stop();
        measure.Content=T(active?"Cancel performance test":"Measure for 60 seconds");
        testStatus.Text=T(status);
        results.Text=latest==null?"":(previous==null?"":Result("Previous test",previous)+Environment.NewLine)+Result("Latest test",latest);
    }
    internal void VerifyLimitAfterGameCloses(Action<bool> setRunning){
        setRunning(true);ReadConfig(false);
        cap.SelectedIndex=4;custom.Value=130;UpdateActions();
        if(apply.IsEnabled)throw new Exception("Applying the FPS limit must remain blocked while Aion is open");
        setRunning(false);RefreshAfterGameChange();
        if(!apply.IsEnabled||Desired!=130)throw new Exception("Closing Aion must enable apply without losing the selected limit");
        Refresh();
        if(!apply.IsEnabled||Desired!=130)throw new Exception("Refreshing status must preserve the selected limit");
    }
    internal void Translate(){ShowConfig(configMessage);RefreshTest();UiLanguage.Apply(this,owner.Language);}
}
