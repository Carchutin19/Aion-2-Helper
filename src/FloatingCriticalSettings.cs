using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Image=System.Windows.Controls.Image;
using PixelFormat=System.Drawing.Imaging.PixelFormat;

// Uses the existing floating-text renderer and frame cadence. No extra timer.
internal sealed class FloatingCriticalSettings:StackPanel,IDisposable {
 readonly EnergyOverlay owner;readonly Window window;readonly CheckBox separate=new CheckBox(),independent=new CheckBox();
 readonly StackPanel options=new StackPanel(),position=new StackPanel();readonly Image preview=new Image{Height=150,Stretch=Stretch.Uniform};
 readonly List<Action<FloatingTextStyle>> reloaders=new List<Action<FloatingTextStyle>>();readonly Button test;readonly ComboBox direction=new ComboBox{Width=160};
 FloatingTextOptions shown;Bitmap glyph;bool loading;string language;
 internal FloatingCriticalSettings(EnergyOverlay owner,Window window){this.owner=owner;this.window=window;
  var title=SettingsVisual.Text("CRITICAL HITS",10,"#929CAD");title.Margin=new Thickness(2,23,0,9);Children.Add(title);
  var card=new StackPanel();Children.Add(SettingsVisual.Card(card,new Thickness(17,12,17,12)));
  AddSwitch(card,"Separate critical damage",separate,delegate(FloatingTextOptions o){o.SeparateCriticalDamage=separate.IsChecked==true;});
  card.Children.Add(SettingsVisual.Text("Give confirmed outgoing critical hits their own appearance. Each hit is displayed once; !! still marks criticals.",11,"#929CAD"));
  card.Children.Add(options);options.Children.Add(preview);
  test=SettingsVisual.Button("Test critical hits",delegate{owner.FloatingText.ToggleTest(3);});options.Children.Add(test);
  Number(options,"Critical text size","px",12,64,1,o=>o.FontSize,(o,v)=>o.FontSize=(int)v);
  Number(options,"Text scale","%",50,200,5,o=>o.Scale,(o,v)=>o.Scale=(int)v);
  Pick(options,"Critical text color",o=>o.Color,(o,v)=>o.Color=v);Pick(options,"Outline color",o=>o.OutlineColor,(o,v)=>o.OutlineColor=v);
  Number(options,"Outline thickness","px",0,6,1,o=>o.OutlineWidth,(o,v)=>o.OutlineWidth=(int)v);Number(options,"Text opacity","%",0,100,1,o=>o.Opacity,(o,v)=>o.Opacity=(int)v);
  AddSwitch(options,"Use an independent critical widget",independent,delegate(FloatingTextOptions o){o.CriticalOwnWidget=independent.IsChecked==true;});
  options.Children.Add(SettingsVisual.Text("Off: criticals share the damage-dealt anchor, movement and collision spacing, with their own appearance. On: move and scale the critical widget independently; criticals are removed from the normal damage widget.",11,"#929CAD"));
  options.Children.Add(position);
  Number(position,"Spawn position X","px",-100000,100000,1,o=>o.X,(o,v)=>o.X=(int)v);Number(position,"Spawn position Y","px",-100000,100000,1,o=>o.Y,(o,v)=>o.Y=(int)v);
  var row=Row("Movement direction");direction.Items.Add("Up");direction.Items.Add("Down");Grid.SetColumn(direction,1);row.Children.Add(direction);position.Children.Add(row);direction.SelectionChanged+=delegate{if(!loading)Change(o=>o.CriticalDamage.Up=direction.SelectedIndex==0);};
  Number(position,"Time on screen","s",.4M,6,.05M,o=>(decimal)o.Duration,(o,v)=>o.Duration=(double)v);
  Number(position,"Travel distance","px",0,400,5,o=>o.Travel,(o,v)=>o.Travel=(int)v);Number(position,"Horizontal spread","%",0,160,5,o=>o.Spread,(o,v)=>o.Spread=(int)v);Number(position,"Vertical spacing","px",0,40,1,o=>o.Spacing,(o,v)=>o.Spacing=(int)v);
  position.Children.Add(SettingsVisual.Text("Unlock widgets in General to move or resize the critical widget on screen.",11,"#929CAD"));
 }
 static Grid Row(string text){var r=new Grid{MinHeight=38,Margin=new Thickness(0,5,0,5)};r.ColumnDefinitions.Add(new ColumnDefinition());r.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});r.Children.Add(SettingsVisual.Text(text));return r;}
 void AddSwitch(StackPanel host,string text,CheckBox box,Action<FloatingTextOptions> edit){var row=Row(text);Grid.SetColumn(box,1);row.Children.Add(box);host.Children.Add(row);box.Click+=delegate{Change(edit);};}
 void Change(Action<FloatingTextOptions> edit){if(loading)return;var cfg=owner.Configuration;edit(cfg.FloatingText);cfg.FloatingText.Normalize();owner.ApplyConfiguration(cfg);}
 void Number(StackPanel host,string label,string unit,decimal min,decimal max,decimal step,Func<FloatingTextStyle,decimal> read,Action<FloatingTextStyle,decimal> write){var row=Row(label);var stack=new StackPanel{Orientation=Orientation.Horizontal};var n=new SettingsNumber(label,min,max,unit=="s"?2:0,step);stack.Children.Add(n);var suffix=SettingsVisual.Text(unit,11,"#929CAD");suffix.Margin=new Thickness(8,0,0,0);stack.Children.Add(suffix);Grid.SetColumn(stack,1);row.Children.Add(stack);host.Children.Add(row);n.Changed+=delegate{Change(o=>write(o.CriticalDamage,n.Value));};reloaders.Add(o=>n.Value=read(o));}
 void Pick(StackPanel host,string label,Func<FloatingTextStyle,string> read,Action<FloatingTextStyle,string> write){var row=Row(label);Button button=null;button=SettingsVisual.Button("",delegate{var picker=new HelperColorPicker(read(owner.Configuration.FloatingText.CriticalDamage),label,owner.Language){Owner=window};if(picker.ShowDialog()==true)Change(o=>write(o.CriticalDamage,picker.SelectedHex));});button.MinWidth=130;Grid.SetColumn(button,1);row.Children.Add(button);host.Children.Add(row);reloaders.Add(o=>{button.Content=read(o);button.Foreground=SettingsVisual.Brush(read(o));});}
 internal void Reload(FloatingTextOptions cfg){string label=UiLanguage.Text(owner.FloatingText.Testing?"Stop test":"Test critical hits",owner.Language);if(!object.Equals(test.Content,label))test.Content=label;if(shown!=null&&language==owner.Language&&FloatingTextOptions.Same(shown,cfg))return;language=owner.Language;loading=true;try{separate.IsChecked=cfg.SeparateCriticalDamage;independent.IsChecked=cfg.CriticalOwnWidget;options.Visibility=cfg.SeparateCriticalDamage?Visibility.Visible:Visibility.Collapsed;position.Visibility=cfg.CriticalOwnWidget?Visibility.Visible:Visibility.Collapsed;direction.Items[0]=UiLanguage.Text("Up",owner.Language);direction.Items[1]=UiLanguage.Text("Down",owner.Language);direction.SelectedIndex=cfg.CriticalDamage.Up?0:1;foreach(var reload in reloaders)reload(cfg.CriticalDamage);if(shown==null||!FloatingTextOptions.Same(shown,cfg)){if(glyph!=null)glyph.Dispose();glyph=FloatingTextMotion.Glyph("12 480!!",cfg.CriticalDamage);shown=cfg.Copy();}test.Content=UiLanguage.Text(owner.FloatingText.Testing?"Stop test":"Test critical hits",owner.Language);}finally{loading=false;}}
 internal void Paint(double time){if(Visibility!=Visibility.Visible||options.Visibility!=Visibility.Visible||glyph==null)return;var cfg=owner.Configuration.FloatingText;var style=cfg.CriticalOwnWidget?cfg.CriticalDamage.Copy():cfg.InlineCritical();double age=time%(style.Duration+.35);style.Travel=Math.Min(80,style.Travel*style.Scale/100);style.Scale=100;using(var image=new Bitmap(620,150,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(image))using(var a=new ImageAttributes()){float y=style.Up?110:30;FloatingTextMotion.Sprite(g,glyph,310-glyph.Width/2f,y-glyph.Height/2f+FloatingTextMotion.Offset(age,style),FloatingTextMotion.Alpha(age,style.Duration)*style.Opacity/100,a,new ColorMatrix());preview.Source=SettingsVisual.Bitmap(image);}}
 public void Dispose(){preview.Source=null;if(glyph!=null){glyph.Dispose();glyph=null;}}
}
