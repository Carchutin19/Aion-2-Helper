using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

internal sealed class SupportSettingsPane:StackPanel {
 readonly EnergyOverlay owner;readonly CheckBox enabled=new CheckBox();readonly TextBlock status=SettingsVisual.Text("",11,"#84B8AB");readonly Button folder;bool loading;
 internal SupportSettingsPane(EnergyOverlay owner){this.owner=owner;var title=SettingsVisual.Text("SUPPORT & DIAGNOSTICS",10,"#929CAD");title.Margin=new Thickness(2,24,0,9);Children.Add(title);var body=new StackPanel();Children.Add(SettingsVisual.Card(body,new Thickness(18,16,18,16)));
  var row=new Grid{MinHeight=38};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.Children.Add(SettingsVisual.Text("Play with diagnostics"));Grid.SetColumn(enabled,1);row.Children.Add(enabled);body.Children.Add(row);
  enabled.Click+=delegate{if(loading)return;var cfg=owner.Configuration;cfg.SupportLogging=enabled.IsChecked==true;owner.ApplyConfiguration(cfg);};
  Add(body,"Only enable this if something is not working. Reproduce the problem while it is on, then turn it off to finish the report. For a connection or login problem, enable it before fully restarting Aion.");
  Add(body,"Send support-report.zip from the Helper folder privately to support, together with what failed and the steps you followed. No upload is automatic. Your settings are preserved.");
  Add(body,"The report contains version, driver, connection counters and overlay/detection state, which may include character/server identifiers. It does not record packet contents, chat or session keys. Recording stops after 15 minutes or 8 MiB. Turn it off and on to start a new report; one previous ZIP is kept.");
  status.Margin=new Thickness(0,10,0,10);body.Children.Add(status);folder=SettingsVisual.Button("Show support report",delegate{ShowReport();});folder.HorizontalAlignment=HorizontalAlignment.Left;body.Children.Add(folder);Reload();
 }
 static void Add(StackPanel body,string text){var item=SettingsVisual.Text(text,11,"#929CAD");item.Margin=new Thickness(0,8,0,0);body.Children.Add(item);}
 void ShowReport(){string path=owner.SupportReportPath;if(!File.Exists(path))return;try{Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+path+"\""){UseShellExecute=true});}catch(System.ComponentModel.Win32Exception){status.Text=UiLanguage.Text("Could not open the report folder.",owner.Language);}}
 internal void Reload(){loading=true;try{enabled.IsChecked=owner.Configuration.SupportLogging;folder.IsEnabled=File.Exists(owner.SupportReportPath)&&!owner.Configuration.SupportLogging&&!SupportDiagnostics.Enabled&&!SupportDiagnostics.Failed;status.Text=UiLanguage.Text(SupportDiagnostics.Failed?"Could not write the diagnostic report. Check that the Helper folder is writable.":SupportDiagnostics.State,owner.Language);UiLanguage.Apply(this,owner.Language);}finally{loading=false;}}
}
