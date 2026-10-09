using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Drawing=System.Drawing;

internal sealed class FpsSettingsPane:Grid {
    readonly EnergyOverlay owner;readonly Window window;readonly CheckBox enabled=new CheckBox(),background=new CheckBox();
    readonly TextBlock activation=SettingsVisual.Text("Disabled",12,"#A7ADBA"),reading=SettingsVisual.Text("",11,"#8994A6");
    readonly Image preview=new Image{Height=60,Stretch=System.Windows.Media.Stretch.None};readonly StackPanel controls=new StackPanel();readonly Border previewCard;readonly ScrollViewer scroller;readonly Button color,permission;
    readonly Dictionary<string,SettingsNumber> numbers=new Dictionary<string,SettingsNumber>();string colorHex="#00FF63",loadedLanguage;FpsOptions loadedOptions,previewOptions;bool loading;
    internal FpsSettingsPane(EnergyOverlay helper,Window settings){owner=helper;window=settings;
        RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
        var activationRow=Row("Enable FPS counter");Children.Add(activationRow);var switches=new StackPanel{Orientation=Orientation.Horizontal};activation.Margin=new Thickness(0,0,10,0);switches.Children.Add(activation);switches.Children.Add(enabled);Grid.SetColumn(switches,1);activationRow.Children.Add(switches);AutomationProperties.SetName(enabled,"Enable FPS counter");enabled.Click+=delegate{if(loading)return;var cfg=owner.Configuration;cfg.Fps.Enabled=enabled.IsChecked==true;owner.ApplyConfiguration(cfg);};
        var previewStack=new StackPanel();var previewLabel=SettingsVisual.Text("PREVIEW",10,"#929CAD");previewLabel.Margin=new Thickness(0,0,0,8);previewStack.Children.Add(previewLabel);previewStack.Children.Add(preview);var previewNote=SettingsVisual.Text("Sample value · not a game reading",11,"#8994A6");previewNote.HorizontalAlignment=HorizontalAlignment.Center;previewNote.Margin=new Thickness(0,5,0,0);previewStack.Children.Add(previewNote);previewCard=SettingsVisual.Card(previewStack,new Thickness(18,12,18,12));Grid.SetRow(previewCard,1);Children.Add(previewCard);
        scroller=new ScrollViewer{Content=controls,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};scroller.SetResourceReference(FrameworkElement.StyleProperty,"SlimScroll");controls.Margin=new Thickness(0,0,12,0);Grid.SetRow(scroller,3);Children.Add(scroller);
        var group=Group("MEASUREMENT");Number(group,"RefreshInterval","Refresh interval","ms",100,2000,50);var measurementGroup=group;group=Group("APPEARANCE");var row=Row("Text color");group.Children.Add(row);color=SettingsVisual.Button("",PickColor);color.Width=142;Grid.SetColumn(color,1);row.Children.Add(color);AutomationProperties.SetName(color,"Choose color: FPS text");
        row=Row("Dark background","A soft black background with transparent edges.");group.Children.Add(row);Grid.SetColumn(background,1);row.Children.Add(background);AutomationProperties.SetName(background,"Dark background");background.Click+=delegate{Changed();};
        Number(group,"Opacity","Background opacity","%",0,100,1);Number(group,"Softness","Background softness","px",0,24,1);
        group=Group("GEOMETRY");Number(group,"Scale","Scale","%",50,400,5);Number(group,"Width","Width","px",48,1600,1);Number(group,"Height","Height","px",20,400,1);Number(group,"X","Horizontal position","px",-100000,100000,1);Number(group,"Y","Vertical position","px",-100000,100000,1);
        var reset=SettingsVisual.Button("Reset FPS appearance",delegate{var cfg=owner.Configuration;bool active=cfg.Fps.Enabled;var location=cfg.Fps.Bounds.Location;int interval=cfg.Fps.RefreshIntervalMs;cfg.Fps=new FpsOptions{Enabled=active,RefreshIntervalMs=interval};cfg.Fps.Bounds=new Drawing.Rectangle(location,cfg.Fps.Bounds.Size);owner.ApplyConfiguration(cfg);});reset.HorizontalAlignment=HorizontalAlignment.Left;reset.Margin=new Thickness(0,8,0,12);group.Children.Add(reset);
        group=measurementGroup;var measurement=new Grid{Margin=new Thickness(0,0,0,14)};measurement.ColumnDefinitions.Add(new ColumnDefinition());measurement.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});Grid.SetRow(measurement,2);Children.Add(measurement);reading.Margin=new Thickness(0,0,12,0);measurement.Children.Add(reading);
        permission=SettingsVisual.Button("Start measurement as administrator",owner.AllowFpsAdministrator,true);permission.HorizontalAlignment=HorizontalAlignment.Left;permission.Margin=new Thickness(0);Grid.SetColumn(permission,1);measurement.Children.Add(permission);
        var note=SettingsVisual.Text("Default: 250 ms. Lower intervals update the number more often.\nWindows may deliver new readings about once per second.",11,"#8994A6");note.Margin=new Thickness(0,0,0,13);group.Children.Add(note);
        foreach(var pair in numbers){string key=pair.Key;pair.Value.Changed+=delegate{if(loading)return;if(key=="Scale"){var cfg=owner.Configuration;double scale=(double)numbers[key].Value/100;cfg.Fps.Bounds=new Drawing.Rectangle(cfg.Fps.Bounds.Location,new Drawing.Size((int)Math.Round(112*scale),(int)Math.Round(44*scale)));owner.ApplyConfiguration(cfg);}else Changed();};}
        Reload();
    }
    static Grid Row(string title,string note=null){var row=new Grid{MinHeight=57,Margin=new Thickness(0,5,0,5)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var labels=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,20,0)};labels.Children.Add(SettingsVisual.Text(title));if(note!=null){var hint=SettingsVisual.Text(note,11,"#8994A6");hint.Margin=new Thickness(0,5,0,0);hint.MaxWidth=330;labels.Children.Add(hint);}row.Children.Add(labels);return row;}
    StackPanel Group(string title){var label=SettingsVisual.Text(title,11,"#929CAD");label.Margin=new Thickness(2,5,0,9);controls.Children.Add(label);var group=new StackPanel();controls.Children.Add(SettingsVisual.Card(group,new Thickness(16,3,16,3)));return group;}
    void Number(StackPanel group,string key,string label,string unit,decimal minimum,decimal maximum,decimal step){var row=Row(label);group.Children.Add(row);var side=new StackPanel{Orientation=Orientation.Horizontal};Grid.SetColumn(side,1);row.Children.Add(side);var field=new SettingsNumber(label,minimum,maximum,0,step);side.Children.Add(field);var units=SettingsVisual.Text(unit,11,"#8994A6");units.Width=28;units.Margin=new Thickness(7,0,0,0);side.Children.Add(units);numbers[key]=field;}
    void Changed(){if(loading)return;var cfg=owner.Configuration;cfg.Fps.Background=background.IsChecked==true;cfg.Fps.Color=colorHex;cfg.Fps.BackgroundOpacity=(int)Math.Round((double)numbers["Opacity"].Value*255/100);cfg.Fps.Softness=(int)numbers["Softness"].Value;cfg.Fps.RefreshIntervalMs=(int)numbers["RefreshInterval"].Value;cfg.Fps.Bounds=new Drawing.Rectangle((int)numbers["X"].Value,(int)numbers["Y"].Value,(int)numbers["Width"].Value,(int)numbers["Height"].Value);owner.ApplyConfiguration(cfg);}
    void PickColor(){var picker=new HelperColorPicker(colorHex,"FPS text",owner.Language){Owner=window};if(picker.ShowDialog()==true){colorHex=picker.SelectedHex;Changed();}}
    internal void Reload(){var o=owner.Configuration.Fps;if(loadedOptions!=null&&FpsOptions.Same(o,loadedOptions)&&loadedLanguage==owner.Language){UpdateStatus();return;}loading=true;try{loadedOptions=o.Copy();loadedLanguage=owner.Language;enabled.IsChecked=o.Enabled;background.IsChecked=o.Background;activation.Text=o.Enabled?"Enabled":"Disabled";colorHex=o.Color;
        var swatch=new StackPanel{Orientation=Orientation.Horizontal};swatch.Children.Add(new Border{Width=17,Height=17,CornerRadius=new CornerRadius(5),Background=SettingsVisual.Brush(o.Color),Margin=new Thickness(0,0,9,0)});swatch.Children.Add(SettingsVisual.Text(o.Color.ToUpperInvariant(),12,"#D1D6E0"));color.Content=swatch;
        numbers["Opacity"].Value=(decimal)(o.BackgroundOpacity*100.0/255);numbers["Softness"].Value=o.Softness;numbers["RefreshInterval"].Value=o.RefreshIntervalMs;numbers["Scale"].Value=(decimal)(o.Bounds.Height/44.0*100);numbers["Width"].Value=o.Bounds.Width;numbers["Height"].Value=o.Bounds.Height;numbers["X"].Value=o.Bounds.X;numbers["Y"].Value=o.Bounds.Y;
        previewCard.IsEnabled=scroller.IsEnabled=o.Enabled;previewCard.Opacity=scroller.Opacity=o.Enabled?1:.4;numbers["Opacity"].IsEnabled=numbers["Softness"].IsEnabled=o.Background;UpdateStatus();
        if(previewOptions==null||previewOptions.Bounds.Size!=o.Bounds.Size||previewOptions.Background!=o.Background||previewOptions.Color!=o.Color||previewOptions.BackgroundOpacity!=o.BackgroundOpacity||previewOptions.Softness!=o.Softness){using(var image=FpsDesign.Render(Math.Min(420,o.Bounds.Width),Math.Min(100,o.Bounds.Height),"144 FPS",o,false))preview.Source=SettingsVisual.Bitmap(image);preview.Height=Math.Min(100,o.Bounds.Height);previewOptions=o.Copy();}UiLanguage.Apply(this,owner.Language);
    }finally{loading=false;}}
    internal void UpdateStatus(){string text=owner.FpsStatus;if(reading.Text!=text)reading.Text=text;var visibility=owner.FpsNeedsAdministrator?Visibility.Visible:Visibility.Collapsed;if(permission.Visibility!=visibility)permission.Visibility=visibility;}
    internal void Verify(){var original=owner.Configuration;var cfg=original.Copy();cfg.Fps.Enabled=true;cfg.Fps.Color="#00CC77";cfg.Fps.BackgroundOpacity=93;cfg.Fps.Softness=12;cfg.Fps.RefreshIntervalMs=750;cfg.Fps.Bounds=new Drawing.Rectangle(50,60,168,66);owner.ApplyConfiguration(cfg);Reload();
        if(!previewCard.IsEnabled||!scroller.IsEnabled||numbers["Width"].Value!=168||colorHex!="#00CC77"||numbers["RefreshInterval"].Value!=750)throw new Exception("FPS controls must load actual preferences");owner.UndoConfiguration();if(!ConfigurationHistory.Same(original,owner.Configuration))throw new Exception("FPS undo must preserve energy/language");owner.RedoConfiguration();if(!ConfigurationHistory.Same(cfg,owner.Configuration))throw new Exception("FPS redo must restore its complete module");cfg.Fps.Enabled=false;owner.ApplyConfiguration(cfg);Reload();if(!enabled.IsEnabled||scroller.IsEnabled||previewCard.IsEnabled)throw new Exception("FPS activation stays editable while module controls are disabled");owner.ApplyConfiguration(original);}
}

internal static class FpsVerification {
    internal static int VerifyStandby(string root){
        var games=new FpsGameDiscovery();games.Update();if(games.Ids.Count!=0)return 2;
        var output=new System.Text.StringBuilder();
        try{using(var monitor=new FpsMonitor(root))for(int cycle=0;cycle<2;cycle++){
            monitor.Configure(true);monitor.Tick();var field=typeof(FpsMonitor).GetField("worker",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var owned=(System.Diagnostics.Process)field.GetValue(monitor);
            if(owned==null)throw new Exception("Standby worker did not start");
            using(var worker=System.Diagnostics.Process.GetProcessById(owned.Id)){
                var wait=System.Diagnostics.Stopwatch.StartNew();while(monitor.Status!="Waiting for Aion 2"&&wait.Elapsed.TotalSeconds<5)System.Threading.Thread.Sleep(100);
                if(monitor.Status!="Waiting for Aion 2"||monitor.Value.HasValue||monitor.NeedsAdministrator)throw new Exception("No game must wait without FPS or a permission prompt");
                worker.Refresh();double before=worker.TotalProcessorTime.TotalMilliseconds;System.Threading.Thread.Sleep(4000);worker.Refresh();output.AppendLine("cycle="+cycle+"; standby CPU ms="+(worker.TotalProcessorTime.TotalMilliseconds-before)+"; working set="+worker.WorkingSet64);
                if(worker.HasExited||monitor.Value.HasValue)throw new Exception("Standby must remain ready without invented FPS");
                monitor.Configure(false);if(!worker.WaitForExit(6000))throw new Exception("Disabled measurement worker must exit");
                if(field.GetValue(monitor)!=null)throw new Exception("Disabled measurement must release the process reference");
            }
            if(cycle==1){monitor.Configure(true);monitor.Tick();var current=(System.Diagnostics.Process)field.GetValue(monitor);using(var child=System.Diagnostics.Process.GetProcessById(current.Id)){
                var wait=System.Diagnostics.Stopwatch.StartNew();while(monitor.Status!="Waiting for Aion 2"&&wait.Elapsed.TotalSeconds<5)System.Threading.Thread.Sleep(100);
                child.Kill();child.WaitForExit();wait.Restart();while((field.GetValue(monitor)!=null||monitor.Status!="FPS measurement unavailable")&&wait.Elapsed.TotalSeconds<5)System.Threading.Thread.Sleep(100);
                var pipe=typeof(FpsMonitor).GetField("pipe",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(field.GetValue(monitor)!=null||pipe.GetValue(monitor)!=null||monitor.Status!="FPS measurement unavailable")throw new Exception("Unexpected worker exit must release its pipe and process reference");
            }}
        }File.WriteAllText(Path.Combine(root,"fps-standby-test.txt"),"PASS: two enable/disable cycles without a game; no invented FPS or permission prompt; workers exited; unexpected worker exit released its pipe and process reference.\r\n"+output);return 0;
        }catch(Exception ex){File.WriteAllText(Path.Combine(root,"fps-standby-test.txt"),"FAIL: "+ex+"\r\n"+output);return 1;}
    }
    internal static int VerifyRefresh(string root){
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var arrival=typeof(FpsMonitor).GetField("lastMessage",flags);var process=typeof(FpsMonitor).GetField("worker",flags);
        int slow=0,fast=0,workerId=0;long previous=0;bool changed=false;bool stable=true;
        using(var monitor=new FpsMonitor(root)){monitor.Configure(true,2000);var watch=System.Diagnostics.Stopwatch.StartNew();
            while(watch.Elapsed.TotalSeconds<12){monitor.Tick();var worker=process.GetValue(monitor) as System.Diagnostics.Process;if(worker!=null){if(workerId==0)workerId=worker.Id;else if(worker.Id!=workerId)stable=false;}
                if(!changed&&watch.Elapsed.TotalSeconds>=8){monitor.Configure(true,100);changed=true;}
                long stamp=(long)arrival.GetValue(monitor);if(stamp!=previous&&monitor.Value.HasValue){previous=stamp;if(changed)fast++;else slow++;}System.Threading.Thread.Sleep(50);
            }monitor.Configure(false);
        }
        bool pass=stable&&workerId!=0&&slow>=2&&fast>=20;
        File.WriteAllText(Path.Combine(root,"fps-refresh-test.txt"),(pass?"PASS":"FAIL")+": live 2000 ms to 100 ms without restarting worker; slow samples="+slow+", fast samples="+fast+", same worker="+stable);return pass?0:1;
    }
    internal static int Live(string root){
        var samples=new List<int>();var trace=new System.Text.StringBuilder();
        using(var monitor=new FpsMonitor(root)){monitor.Configure(true);var watch=System.Diagnostics.Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<16){monitor.Tick();int? value=monitor.Value;if(value.HasValue)samples.Add(value.Value);trace.AppendLine(watch.Elapsed.TotalSeconds.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" | "+monitor.Status+" | "+(value.HasValue?value.Value.ToString():"unknown"));System.Threading.Thread.Sleep(250);}monitor.Configure(false);}
        File.WriteAllText(Path.Combine(root,"fps-live-test.txt"),(samples.Count>0?"PASS: real Aion 2 FPS samples: "+samples.Count:"NO DATA: real-game validation pending")+"\r\n"+trace);return samples.Count>0?0:1;
    }
    internal static void Run(string root){FpsEtw.Verify();string output=Path.Combine(root,"designs");Directory.CreateDirectory(output);var o=new FpsOptions();var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();o.Color="#123456";o.Enabled=true;o.RefreshIntervalMs=750;o.Bounds=new Drawing.Rectangle(50,60,144,60);var restored=FpsOptions.Read(serializer.Deserialize<Dictionary<string,object>>(serializer.Serialize(new{fps=o})));if(!FpsOptions.Same(o,restored)||FpsOptions.Read(new Dictionary<string,object>()).Enabled)throw new Exception("FPS save/load and migration");if(FpsOptions.Read(new Dictionary<string,object>()).RefreshIntervalMs!=250||FpsOptions.Read(serializer.Deserialize<Dictionary<string,object>>("{\"fps\":{\"Enabled\":true}}" )).RefreshIntervalMs!=250)throw new Exception("Legacy FPS refresh default");var invalid=new FpsOptions{RefreshIntervalMs=0};invalid.Normalize();if(invalid.RefreshIntervalMs!=100)throw new Exception("FPS refresh lower bound");invalid.RefreshIntervalMs=999999;invalid.Normalize();if(invalid.RefreshIntervalMs!=2000)throw new Exception("FPS refresh upper bound");
        using(var small=FpsDesign.Render(76,30,"83 FPS",new FpsOptions(),false))small.Save(Path.Combine(output,"fps-widget-small.png"));
        foreach(int height in new[]{20,44,88}){o.Bounds=new Drawing.Rectangle(30,30,(int)(height*112/44.0),height);using(var bitmap=FpsDesign.Render(o.Bounds.Width,height,"144 FPS",o,false))bitmap.Save(Path.Combine(output,"fps-widget-"+height+".png"));}
        o.Background=false;using(var plain=FpsDesign.Render(112,44,"144 FPS",o,false))if(plain.GetPixel(0,0).A!=0||plain.GetPixel(4,4).A!=0)throw new Exception("Background off must stay transparent");
        if(FpsDesign.ResizeBounds(new Drawing.Rectangle(10,20,112,44),new Drawing.Point(1000,1000),13)!=new Drawing.Rectangle(74,44,48,20))throw new Exception("FPS resize must preserve opposite corner and own minimum");
        using(var owner=new EnergyOverlay(root,true)){using(var widget=new FpsOverlay(owner,root,true))widget.VerifyCache();var pane=new FpsSettingsPane(owner,new System.Windows.Window());pane.Verify();var cfg=owner.Configuration;cfg.Fps.Enabled=true;owner.ApplyConfiguration(cfg);var settings=new HelperSettings(owner);try{settings.VerifyFps(root);}finally{settings.Close();}}
        File.WriteAllText(Path.Combine(root,"fps-test.txt"),"PASS: bounded real presentation timestamps; CSV quoting; PID/swap-chain isolation; invalid/out-of-order filtering; long-pause reset; no invented FPS; migration/save/load; transparent background off; minimum resizing; full-module undo/redo and disabled UI; localized FPS settings preview.");
    }
}
