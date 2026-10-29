using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;

internal static class NextSettingsVerification {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool x,string m){if(!x)throw new Exception(m);}
 internal static void Run(string root){
  var legacy=FloatingTextOptions.Read(new Dictionary<string,object>());Check(!legacy.SeparateCriticalDamage&&!legacy.CriticalOwnWidget&&!legacy.Enabled,"Existing settings keep critical separation off");
  var o=new FloatingTextOptions{Enabled=true,SeparateCriticalDamage=true,CriticalOwnWidget=true};o.CriticalDamage.Color="#FF55AA";o.CriticalDamage.FontSize=48;
  Check(o.Channel(0,true)==3&&o.Channel(0,false)==0&&o.Channel(1,true)==1&&o.Channel(2,true)==2,"Only own confirmed critical damage moves, once, to its independent channel");
  o.CriticalOwnWidget=false;Check(o.Channel(0,true)==0&&o.InlineCritical().X==o.DamageDealt.X&&o.InlineCritical().Color=="#FF55AA","Shared criticals keep normal anchor with separate appearance");
  var json=new JavaScriptSerializer();var cfg=FloatingTextOptions.Read(new Dictionary<string,object>{{"floatingText",json.DeserializeObject(json.Serialize(o))}});Check(FloatingTextOptions.Same(cfg,o),"All critical preferences survive serialization");
  var copy=o.Copy();copy.CriticalDamage.X+=20;Check(copy.CriticalDamage.X!=o.CriticalDamage.X,"Critical style copies cannot mutate saved preferences");
  using(var normal=FloatingTextMotion.Glyph("12 480!!",o.DamageDealt))using(var critical=FloatingTextMotion.Glyph("12 480!!",o.CriticalDamage))Check(critical.Width>normal.Width&&critical.Height>normal.Height,"Custom critical size reaches the real glyph renderer");
  using(var owner=new EnergyOverlay(root,true)){
   var c=owner.Configuration;c.FloatingText=o;c.FloatingText.Enabled=true;c.FloatingText.CriticalOwnWidget=true;owner.ApplyConfiguration(c);
   owner.FloatingText.ToggleTest(3);var windows=(FloatingTextWindow[])typeof(FloatingTextService).GetField("windows",Private).GetValue(owner.FloatingText);
   Check(windows.Length==4&&windows[3].HasEntries&&!windows[0].HasEntries,"Independent critical preview emits no duplicate in the normal widget");owner.FloatingText.StopTest();
   c=owner.Configuration;c.FloatingText.CriticalOwnWidget=false;owner.ApplyConfiguration(c);owner.FloatingText.ToggleTest(3);Check(windows[0].HasEntries&&!windows[3].HasEntries,"Shared critical preview uses the normal widget only");owner.FloatingText.StopTest();
   c=owner.Configuration;var before=c.Copy();c.SupportLogging=true;owner.ApplyConfiguration(c);Check(owner.Configuration.SupportLogging&&!SupportDiagnostics.Enabled,"Preview configuration is isolated from diagnostic startup");owner.UndoConfiguration();Check(!owner.Configuration.SupportLogging,"Diagnostic preference supports undo");
   var a=o.DamageDealt.Copy();var b=o.CriticalDamage.Copy();using(var mixed=new FloatingTextWindow(owner,0,true)){mixed.Configure(a,true,b);mixed.Add(1200,false,1);mixed.Add(12000,true,1);var initial=mixed.Positions(1);var later=mixed.Positions(1.1);Check(initial.Length==2&&!initial[0].IntersectsWith(initial[1]),"Shared critical glyphs participate in normal collision placement");Check(Math.Abs((later[0].Y-initial[0].Y)-(later[1].Y-initial[1].Y))<.01,"Shared criticals use the same motion rate even with different text scales");mixed.Clear();}
   // Render unlocked edit references with no impacts, then lock while idle.
   c=owner.Configuration;c.FloatingText.Enabled=true;c.FloatingText.SeparateCriticalDamage=true;c.FloatingText.CriticalOwnWidget=true;owner.ApplyConfiguration(c);owner.FloatingText.Configure(c.FloatingText,false);
   var surface=typeof(FloatingTextWindow).GetField("surface",Private);for(int i=0;i<4;i++)Check(surface.GetValue(windows[i])!=null,"Each enabled unlocked reference, including independent criticals, renders an edit surface");
   owner.FloatingText.Configure(c.FloatingText,true);for(int i=0;i<4;i++)Check(surface.GetValue(windows[i])==null&&!windows[i].HasEntries,"Relocking disposes idle edit surfaces immediately without Done, new hits or another render tick");
   string folder=Path.Combine(root,"designs");Directory.CreateDirectory(folder);
   foreach(string lang in UiLanguageMore.Codes){Check(UiLanguage.Normalize(lang)==lang,"Supported locale retained: "+lang);c=owner.Configuration;c.Language=lang;c.FloatingText.SeparateCriticalDamage=true;c.FloatingText.CriticalOwnWidget=true;owner.ApplyConfiguration(c);
    var window=new Window{Width=730,Height=1100};SettingsVisual.Theme(window);var pane=new FloatingCriticalSettings(owner,window){Margin=new Thickness(20)};window.Content=pane;pane.Reload(owner.Configuration.FloatingText);UiLanguage.Apply(window,lang);pane.Paint(.3);
    try{Check(window.FlowDirection==(lang=="ar"?FlowDirection.RightToLeft:FlowDirection.LeftToRight),"RTL changes only for Arabic");SettingsVisual.RenderPreview(window,Path.Combine(folder,"critical-settings-"+lang+".png"));}finally{pane.Dispose();window.Close();}
    window=new Window{Width=770,Height=650};SettingsVisual.Theme(window);window.Content=new SupportSettingsPane(owner){Margin=new Thickness(20)};UiLanguage.Apply(window,lang);try{SettingsVisual.RenderPreview(window,Path.Combine(folder,"support-settings-"+lang+".png"));}finally{window.Close();}
   }
   var label=new TextBlock{Text="Settings"};foreach(string lang in new[]{"de","fr","ar","es","en"}){UiLanguage.Apply(label,lang);Check(label.Text==UiLanguage.Text("Settings",lang),"Repeated live language changes retain original label");}
   foreach(string lang in new[]{"de","fr","ar"})Check(UiLanguageMore.Count(lang)>=250&&UiLanguage.Text("Separate critical damage",lang)!="Separate critical damage"&&UiLanguage.Text("Show support report",lang)!="Show support report","New controls translated in "+lang);
  }
  File.WriteAllText(Path.Combine(root,"next-settings-test.txt"),"PASS: legacy defaults, critical serialization/deep copy/appearance, no duplicate independent/shared critical preview, real mixed collision and matching movement, undo/isolation of opt-in logs, five-language rendering, Arabic RTL, repeat language switching and explicit preview-locale coverage. No live capture or regular Helper launch.");
 }
}
