using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Drawing=System.Drawing;

// This UI loads only when Settings is opened. The game overlay keeps its existing
// WinForms/native drawing path and does not run a second animation loop.
internal static class SettingsVisual {
    internal static readonly Color Accent=Color.FromRgb(103,199,176);
    internal static Brush Brush(string hex){return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));}
    internal static void Theme(Window window){
        using(var stream=typeof(SettingsVisual).Assembly.GetManifestResourceStream("SettingsTheme.xaml"))window.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
        window.FontFamily=new FontFamily("Segoe UI");window.FontSize=13;window.Foreground=Brush("#F0F1F5");window.Background=Brush("#00000000");
        // Keep the chosen alpha when inactive too. The system acrylic material
        // can replace it with a solid surface and add a light native frame.
        window.WindowStyle=WindowStyle.None;window.AllowsTransparency=true;window.ResizeMode=ResizeMode.CanResizeWithGrip;window.WindowStartupLocation=WindowStartupLocation.CenterScreen;
        System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(window);
    }
    internal static TextBlock Text(string text,double size=13,string color=null){return new TextBlock{Text=text,FontSize=size,Foreground=Brush(color??"#F0F1F5"),TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};}
    internal static Button Button(string text,Action action,bool primary=false){var b=new Button{Content=text};if(primary)b.SetResourceReference(FrameworkElement.StyleProperty,"PrimaryButton");b.Click+=delegate{action();};AutomationProperties.SetName(b,text);return b;}
    internal static Border Card(UIElement content,Thickness padding){return new Border{CornerRadius=new CornerRadius(14),Background=Brush("#162E3542"),BorderBrush=Brush("#18FFFFFF"),BorderThickness=new Thickness(1),Padding=padding,Child=content,Margin=new Thickness(0,0,0,14)};}
    internal static Border Shell(UIElement content){return new Border{CornerRadius=new CornerRadius(16),Background=Brush("#F2191C23"),BorderThickness=new Thickness(0),Child=content};}
    internal static BitmapSource Bitmap(Drawing.Bitmap bitmap){
        var rect=new Drawing.Rectangle(0,0,bitmap.Width,bitmap.Height);var data=bitmap.LockBits(rect,Drawing.Imaging.ImageLockMode.ReadOnly,Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try{var image=BitmapSource.Create(bitmap.Width,bitmap.Height,96,96,PixelFormats.Pbgra32,null,data.Scan0,data.Stride*data.Height,data.Stride);image.Freeze();return image;}finally{bitmap.UnlockBits(data);}
    }
    internal static string Hex(Color color){return "#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");}
    internal static void RenderPreview(Window window,string path){
        var root=(FrameworkElement)window.Content;root.Measure(new Size(window.Width,window.Height));root.Arrange(new Rect(0,0,window.Width,window.Height));root.UpdateLayout();
        var image=new RenderTargetBitmap((int)window.Width,(int)window.Height,96,96,PixelFormats.Pbgra32);var backdrop=new DrawingVisual();using(var dc=backdrop.RenderOpen())dc.DrawRectangle(Brush("#FF20232B"),null,new Rect(0,0,window.Width,window.Height));image.Render(backdrop);image.Render(root);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var output=File.Create(path))encoder.Save(output);
    }
    internal static Color Hsv(double hue,double saturation,double value){
        hue=((hue%360)+360)%360;saturation=Math.Max(0,Math.Min(1,saturation));value=Math.Max(0,Math.Min(1,value));
        double c=value*saturation,x=c*(1-Math.Abs((hue/60)%2-1)),m=value-c,r=0,g=0,b=0;
        if(hue<60){r=c;g=x;}else if(hue<120){r=x;g=c;}else if(hue<180){g=c;b=x;}else if(hue<240){g=x;b=c;}else if(hue<300){r=x;b=c;}else{r=c;b=x;}
        return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));
    }
    internal static void ToHsv(Color color,out double hue,out double saturation,out double value){
        double r=color.R/255.0,g=color.G/255.0,b=color.B/255.0,min=Math.Min(r,Math.Min(g,b));value=Math.Max(r,Math.Max(g,b));double delta=value-min;
        saturation=value==0?0:delta/value;hue=delta==0?0:value==r?60*((g-b)/delta%6):value==g?60*((b-r)/delta+2):60*((r-g)/delta+4);if(hue<0)hue+=360;
    }
    internal static void Verify(string root){
        ConfigurationHistory.Verify();NpcapSupport.Verify();
        foreach(string hex in new[]{"#000000","#FFFFFF","#A02D2D","#B85416","#287044","#00FFFF","#FF00FF","#FFFF00","#123456"}){
            var c=(Color)ColorConverter.ConvertFromString(hex);double h,s,v;ToHsv(c,out h,out s,out v);if(Hex(Hsv(h,s,v))!=hex)throw new Exception("HSV round-trip: "+hex);
        }
        decimal parsed;if(!SettingsNumber.TryParse("1,5",out parsed)||parsed!=1.5M||SettingsNumber.TryParse("NaN",out parsed))throw new Exception("Settings numeric input");SettingsNumber.Verify();
        using(var owner=new EnergyOverlay(root,true)){
            var before=owner.Configuration;var settings=new HelperSettings(owner);try{
                var actual=settings.ReadConfiguration();if(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(before.Options)!=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(actual.Options)||before.Bounds!=actual.Bounds||before.Maximum!=actual.Maximum)throw new Exception("Settings must preserve all energy preferences");
                settings.VerifyHistory(root);settings.VerifyLayout();string designs=Path.Combine(root,"designs");Directory.CreateDirectory(designs);RenderPreview(settings,Path.Combine(designs,"settings-current.png"));var picker=new HelperColorPicker("#B85416","Medium energy");try{if(picker.SelectedHex!="#B85416")throw new Exception("Picker initial color");RenderPreview(picker,Path.Combine(designs,"color-picker-current.png"));picker.VerifyColorInputs();}finally{picker.Close();}
                var trayMenu=new HelperTrayMenu(owner);try{trayMenu.Verify();RenderPreview(trayMenu,Path.Combine(designs,"tray-menu-current.png"));}finally{trayMenu.Close();}
            }finally{settings.Close();}
            var reopened=new HelperSettings(owner);try{reopened.VerifyReopenedHistory(before);}finally{reopened.Close();}
            var commandMenu=new HelperTrayMenu(owner);try{commandMenu.VerifyLockCommand(owner);}finally{if(!commandMenu.IsDisposed)commandMenu.Close();}
        }
        using(var owner=new EnergyOverlay(root,true)){var settings=new HelperSettings(owner);try{settings.VerifyLanguages(root);}finally{settings.Close();}}
        VerifyLanguagePersistence(root);
        using(var owner=new EnergyOverlay(root,true)){bool installed=false;var missing=new NpcapSupport(delegate{return installed;},delegate{});var settings=new HelperSettings(owner,missing);try{settings.VerifyNpcap(root,missing,delegate{installed=true;});}finally{settings.Close();}}
        File.WriteAllText(Path.Combine(root,"settings-test.txt"),"PASS: HSV/HEX round-trip, decimal input without rounding unedited fields, settings preserve all options/geometry/maximum, every tab has controls, picker HEX/RGB validation; disabled module blocks controls/preview but retains its activation switch; bounded undo/redo, immutable snapshots, branch replacement, no-op saves preserve redo; overlay restoration, buttons and history across Settings reopening; tray menu screen bounds and shared lock command; live EN/ES selection, localized energy settings/tray/color picker, language undo/redo, independent general settings, persistence/restart and legacy/invalid language fallback.");
    }
    static void VerifyLanguagePersistence(string root){
        if(UiLanguage.Read(new Dictionary<string,object>())!="en"||UiLanguage.Read(new Dictionary<string,object>{{"language","invalid"}})!="en")throw new Exception("Language migration/fallback");
        // A disabled, isolated instance exercises the real save/load path without capture.
        string folder=Path.Combine(root,".local-tools","language-settings-test");Directory.CreateDirectory(Path.Combine(folder,"protocol"));File.Copy(Path.Combine(root,"protocol","sync-opcodes.json"),Path.Combine(folder,"protocol","sync-opcodes.json"),true);
        var cfg=new EnergyConfiguration{Options=new EnergyBarOptions{Enabled=false,LowColor="#123456"},Bounds=new Drawing.Rectangle(100,100,320,4),Maximum=120000,Locked=true,Language="es"};
        var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();string path=Path.Combine(folder,"overlay-settings.json");
        File.WriteAllText(path,serializer.Serialize(new{maximum=cfg.Maximum,x=100,y=100,width=320,height=4,design="slim-v2",locked=true,energyBar=cfg.Options}));
        using(var owner=new EnergyOverlay(folder)){if(owner.Language!="en")throw new Exception("Legacy language default");owner.ApplyConfiguration(cfg);}
        using(var reopened=new EnergyOverlay(folder)){if(!ConfigurationHistory.Same(cfg,reopened.Configuration))throw new Exception("Language/preferences must survive restart");}
    }
}

internal sealed class SettingsNumber : Grid {
    readonly TextBox input=new TextBox();readonly decimal minimum,maximum,increment;readonly int decimals;
    decimal number;bool writing,dirty;internal event Action Changed;
    internal decimal Value {get{return number;}set{number=Math.Max(minimum,Math.Min(maximum,value));Write();}}
    internal SettingsNumber(string name,decimal min,decimal max,int precision,decimal step){
        minimum=min;maximum=max;decimals=precision;increment=step;Width=152;Height=34;
        ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(30)});ColumnDefinitions.Add(new ColumnDefinition());ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(30)});
        var minus=SettingsVisual.Button("−",delegate{Commit();Set(number-increment);});minus.Padding=new Thickness(0);minus.BorderThickness=new Thickness(0);AutomationProperties.SetName(minus,"Decrease "+name);Children.Add(minus);
        input.TextAlignment=TextAlignment.Center;input.Padding=new Thickness(4,6,4,6);SetColumn(input,1);Children.Add(input);AutomationProperties.SetName(input,name);
        var plus=SettingsVisual.Button("+",delegate{Commit();Set(number+increment);});plus.Padding=new Thickness(0);plus.BorderThickness=new Thickness(0);SetColumn(plus,2);AutomationProperties.SetName(plus,"Increase "+name);Children.Add(plus);
        input.LostKeyboardFocus+=delegate{Commit();};input.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Enter){Commit();e.Handled=true;}else if(e.Key==Key.Escape){Write();e.Handled=true;}else if(e.Key==Key.Up){Commit();Set(number+increment);e.Handled=true;}else if(e.Key==Key.Down){Commit();Set(number-increment);e.Handled=true;}};
        input.TextChanged+=delegate{if(!writing)dirty=true;};
        Value=min;
    }
    internal static bool TryParse(string text,out decimal value){return decimal.TryParse(text.Trim().Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out value);}
    void Write(){writing=true;input.Text=number.ToString(decimals==0?"0":"0."+new string('0',decimals),CultureInfo.InvariantCulture);dirty=false;writing=false;}
    void Set(decimal value){value=Math.Round(Math.Max(minimum,Math.Min(maximum,value)),decimals);if(value==number){Write();return;}number=value;Write();if(Changed!=null)Changed();}
    internal void Commit(){if(writing||!dirty)return;decimal value;if(TryParse(input.Text,out value))Set(value);else Write();}
    internal static void Verify(){var field=new SettingsNumber("Opacity",0,100,0,1);int edits=0;field.Changed+=delegate{edits++;};decimal precise=170M*100/255;field.Value=precise;field.Commit();if(field.Value!=precise||edits!=0)throw new Exception("Unedited numbers must retain their exact stored value");field.input.Text="42";field.Commit();if(field.Value!=42||edits!=1)throw new Exception("Edited number must commit once");field.input.Text="invalid";field.Commit();if(field.Value!=42||edits!=1)throw new Exception("Invalid numeric edits must not enter history");}
}

// Independent WPF window, with stable per-pixel alpha and opaque text/controls.
internal sealed class HelperSettings : Window {
    readonly EnergyOverlay overlay;readonly Dictionary<string,CheckBox> switches=new Dictionary<string,CheckBox>();readonly Dictionary<string,SettingsNumber> numbers=new Dictionary<string,SettingsNumber>();
    readonly Dictionary<string,Button> colors=new Dictionary<string,Button>();readonly Dictionary<string,string> colorValues=new Dictionary<string,string>();
    readonly StackPanel[] pages=new StackPanel[3];readonly Button[] tabs=new Button[3];readonly Grid pageHost=new Grid();readonly Grid segmented=new Grid{Margin=new Thickness(0,0,0,16)};readonly ScrollViewer scroller=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Border previewCard;
    readonly Image previewImage=new Image{Height=20,Stretch=Stretch.None,HorizontalAlignment=HorizontalAlignment.Center};readonly Slider previewSlider=new Slider{Minimum=0,Maximum=100,Value=65,IsMoveToPointEnabled=true};
    readonly TextBlock previewValue=SettingsVisual.Text("65 %",12,"#A7ADBA"),status=SettingsVisual.Text("",11,"#8E97A6");
    readonly TextBlock widgetStatus=SettingsVisual.Text("",11);
    readonly DispatcherTimer refresh=new DispatcherTimer();readonly BarDesign.GlowWorkspace previewGlow=new BarDesign.GlowWorkspace();readonly TextBlock activation=SettingsVisual.Text("Enabled",12,"#A7ADBA");
    readonly Button undo,redo;
    readonly Button[] sections=new Button[5];readonly FpsSettingsPane fpsPage;readonly DpsSettingsPane dpsPage;readonly NotificationSettingsPane notificationPage;readonly ComboBox languagePicker=new ComboBox{Width=190,Height=38};
    readonly NpcapSupport npcSupport;readonly TextBlock npcMessage=SettingsVisual.Text("",13,"#E4B986");Border npcCard;Button npcDownload;
    readonly TextBlock sectionTitle=SettingsVisual.Text("Energy Bar",27),sectionSubtitle=SettingsVisual.Text("Energy bar for dash and sprint",13,"#929AA8");
    readonly Grid activationRow=new Grid{Margin=new Thickness(0,18,0,0)};readonly StackPanel generalPage=new StackPanel();bool generalSelected;
    bool loading;DateTime noticeUntil;internal bool IsDisposed {get;private set;}
    internal HelperSettings(EnergyOverlay owner,NpcapSupport dependency=null){
        overlay=owner;npcSupport=dependency??NpcapSupport.Current;Title="Aion 2 Helper · Settings";SettingsVisual.Theme(this);Width=980;Height=816;MinWidth=880;MinHeight=650;
        var work=SystemParameters.WorkArea;Width=Math.Min(Width,work.Width-24);Height=Math.Min(Height,work.Height-24);MinWidth=Math.Min(MinWidth,Width);MinHeight=Math.Min(MinHeight,Height);
        using(var icon=owner.Icon.ToBitmap())Icon=SettingsVisual.Bitmap(icon);
        var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(190)});layout.ColumnDefinitions.Add(new ColumnDefinition());Content=SettingsVisual.Shell(layout);
        BuildSidebar(layout);
        var body=new Grid{Margin=new Thickness(28,20,28,14)};Grid.SetColumn(body,1);layout.Children.Add(body);
        foreach(var size in new[]{GridLength.Auto,GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto})body.RowDefinitions.Add(new RowDefinition{Height=size});
        var header=new Grid{Margin=new Thickness(0,0,0,18)};body.Children.Add(header);header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        header.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});header.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var titles=new StackPanel();titles.Children.Add(sectionTitle);sectionSubtitle.Margin=new Thickness(0,6,0,0);titles.Children.Add(sectionSubtitle);header.Children.Add(titles);
        var tools=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Top};Grid.SetColumn(tools,1);header.Children.Add(tools);
        var minimize=SettingsVisual.Button("−",delegate{WindowState=WindowState.Minimized;});minimize.Width=30;minimize.Height=30;minimize.Padding=new Thickness(0);minimize.Background=SettingsVisual.Brush("#102F3542");AutomationProperties.SetName(minimize,"Minimize Settings");tools.Children.Add(minimize);
        var close=SettingsVisual.Button("×",delegate{Close();});close.Width=30;close.Height=30;close.FontSize=18;close.Margin=new Thickness(8,0,0,0);close.Padding=new Thickness(0);close.Background=SettingsVisual.Brush("#102F3542");AutomationProperties.SetName(close,"Close Settings");tools.Children.Add(close);
        titles.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.ButtonState==MouseButtonState.Pressed)DragMove();};
        activationRow.ColumnDefinitions.Add(new ColumnDefinition());activationRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});Grid.SetRow(activationRow,1);Grid.SetColumnSpan(activationRow,2);header.Children.Add(activationRow);activationRow.Children.Add(SettingsVisual.Text("Enable Energy Bar",13));
        var activationControls=new StackPanel{Orientation=Orientation.Horizontal};Grid.SetColumn(activationControls,1);activationRow.Children.Add(activationControls);activation.Margin=new Thickness(0,0,10,0);activationControls.Children.Add(activation);var enabled=new CheckBox();switches["Enabled"]=enabled;AutomationProperties.SetName(enabled,"Enable Energy Bar");activationControls.Children.Add(enabled);
        BuildPreview(body);
        Grid.SetRow(segmented,2);body.Children.Add(segmented);
        string[] names={"Behavior","Appearance","Position & size"};for(int n=0;n<3;n++){int tab=n;segmented.ColumnDefinitions.Add(new ColumnDefinition());tabs[n]=SettingsVisual.Button(names[n],delegate{SelectTab(tab);});tabs[n].Margin=new Thickness(n==0?0:4,0,n==2?0:4,0);Grid.SetColumn(tabs[n],n);segmented.Children.Add(tabs[n]);pages[n]=new StackPanel{Margin=new Thickness(0,0,12,0)};}
        scroller.Content=pageHost;scroller.SetResourceReference(FrameworkElement.StyleProperty,"SlimScroll");Grid.SetRow(scroller,3);body.Children.Add(scroller);foreach(var page in pages)pageHost.Children.Add(page);
        BuildBehavior();BuildAppearance();BuildPosition();
        BuildGeneral(body);
        fpsPage=new FpsSettingsPane(overlay,this);Grid.SetRow(fpsPage,1);Grid.SetRowSpan(fpsPage,3);body.Children.Add(fpsPage);
        dpsPage=new DpsSettingsPane(overlay,this);Grid.SetRow(dpsPage,1);Grid.SetRowSpan(dpsPage,3);body.Children.Add(dpsPage);
        notificationPage=new NotificationSettingsPane(overlay,this);Grid.SetRow(notificationPage,1);Grid.SetRowSpan(notificationPage,3);body.Children.Add(notificationPage);
        var footer=new Grid{Margin=new Thickness(0,14,0,0)};Grid.SetRow(footer,4);footer.ColumnDefinitions.Add(new ColumnDefinition());footer.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});body.Children.Add(footer);
        var historyActions=new StackPanel{Orientation=Orientation.Horizontal};footer.Children.Add(historyActions);
        undo=SettingsVisual.Button("↶  Undo",overlay.UndoConfiguration);undo.ToolTip="Undo the last change · Ctrl+Z";undo.Margin=new Thickness(0,0,8,0);historyActions.Children.Add(undo);
        redo=SettingsVisual.Button("↷  Redo",overlay.RedoConfiguration);redo.ToolTip="Redo the undone change · Ctrl+Y";historyActions.Children.Add(redo);
        var done=SettingsVisual.Button("Done",delegate{Close();},true);done.MinWidth=85;Grid.SetColumn(done,1);footer.Children.Add(done);
        foreach(var control in switches.Values)control.Click+=delegate{Changed();};foreach(var control in numbers.Values)control.Changed+=Changed;
        previewSlider.ValueChanged+=delegate{UpdatePreview();};SizeChanged+=delegate{UpdatePreview();};
        overlay.SettingsChanged+=Reload;Closed+=delegate{IsDisposed=true;refresh.Stop();refresh.Tick-=RefreshStatus;overlay.SettingsChanged-=Reload;previewImage.Source=null;notificationPage.Release();};
        refresh.Interval=TimeSpan.FromMilliseconds(400);refresh.Tick+=RefreshStatus;Loaded+=delegate{refresh.Start();UpdatePreview();};
        KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Handled)return;if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Z){overlay.UndoConfiguration();e.Handled=true;}else if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Y){overlay.RedoConfiguration();e.Handled=true;}else if(e.Key==Key.Escape){Close();e.Handled=true;}};Reload();SelectTab(0);SelectSection(true);
    }
    void BuildSidebar(Grid layout){
        var nav=new Grid{Margin=new Thickness(0)};var shell=new Border{Background=SettingsVisual.Brush("#40262A34"),CornerRadius=new CornerRadius(16,0,0,16),BorderBrush=SettingsVisual.Brush("#14FFFFFF"),BorderThickness=new Thickness(0,0,1,0),Child=nav};layout.Children.Add(shell);
        nav.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});nav.RowDefinitions.Add(new RowDefinition());nav.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var brand=new StackPanel{Margin=new Thickness(24,27,20,24)};
        var image=new Image{Width=48,Height=48,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,14)};
        string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","aion-2-helper.png");if(File.Exists(path)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.UriSource=new Uri(path);bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=96;bitmap.EndInit();bitmap.Freeze();image.Source=bitmap;}brand.Children.Add(image);
        brand.Children.Add(SettingsVisual.Text("Aion 2 Helper",19));brand.Children.Add(new TextBlock{Text="SETTINGS",FontSize=10,Foreground=SettingsVisual.Brush("#818A9B"),Margin=new Thickness(0,8,0,0)});nav.Children.Add(brand);
        var section=new StackPanel{Margin=new Thickness(12,12,12,0)};Grid.SetRow(section,1);nav.Children.Add(section);
        for(int n=0;n<5;n++){int module=n;var button=SettingsVisual.Button(n==0?"◉   General":n==1?"◉   Energy Bar":n==2?"◉   FPS Counter":n==3?"◉   DPS Meter":"◉   Notifications",delegate{SelectModule(module);});button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(12,11,12,11);button.Margin=new Thickness(0,0,0,7);sections[n]=button;section.Children.Add(button);}
        var general=new StackPanel{Margin=new Thickness(22,20,18,25)};Grid.SetRow(general,2);nav.Children.Add(general);
        general.Children.Add(new Border{Height=1,Background=SettingsVisual.Brush("#16FFFFFF"),Margin=new Thickness(0,0,0,15)});
        general.Children.Add(SettingsVisual.Text("GENERAL STATUS",10,"#818A9B"));
        var saved=SettingsVisual.Text("✓  Changes saved automatically",11,"#84B8AB");saved.Margin=new Thickness(0,10,0,0);saved.LineHeight=16;general.Children.Add(saved);
        status.Margin=new Thickness(0,12,0,0);status.LineHeight=16;general.Children.Add(status);
        widgetStatus.Margin=new Thickness(0,10,0,0);widgetStatus.LineHeight=16;general.Children.Add(widgetStatus);
        var widgets=SettingsVisual.Text("Move and resize widgets directly on screen.",11,"#8994A6");widgets.Margin=new Thickness(0,16,0,0);widgets.LineHeight=16;general.Children.Add(widgets);
        nav.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource==nav&&e.ButtonState==MouseButtonState.Pressed)DragMove();};
    }
    void BuildGeneral(Grid body){
        generalPage.VerticalAlignment=VerticalAlignment.Top;Grid.SetRow(generalPage,1);Grid.SetRowSpan(generalPage,3);body.Children.Add(generalPage);
        var group=Group(generalPage,"APPLICATION");var row=Row(group,"Language","Choose the language used by Aion 2 Helper.");Grid.SetColumn(languagePicker,1);row.Children.Add(languagePicker);AutomationProperties.SetName(languagePicker,"Language");
        languagePicker.Items.Add(new ComboBoxItem{Content="English",Tag="en"});languagePicker.Items.Add(new ComboBoxItem{Content="Español",Tag="es"});
        languagePicker.SelectionChanged+=delegate{if(loading||IsDisposed)return;var item=languagePicker.SelectedItem as ComboBoxItem;if(item==null)return;var cfg=overlay.Configuration;cfg.Language=(string)item.Tag;overlay.ApplyConfiguration(cfg);};
        var note=SettingsVisual.Text("Changes apply immediately and are saved automatically.",11,"#8994A6");note.Margin=new Thickness(2,0,0,0);generalPage.Children.Add(note);
        var news=new StackPanel();news.Children.Add(SettingsVisual.Text("WHAT'S NEW · v1.7.0 Alpha",11,"#84B8AB"));
        var details=SettingsVisual.Text("Notifications now distinguish normal party invitations from dungeon-group invitations, with the sender name and independent switches. Both share sound, fades, appearance and position. Existing settings are preserved.",12,"#A7ADBA");details.Margin=new Thickness(0,10,0,0);news.Children.Add(details);
        var warning=SettingsVisual.Text("Known issue: party members' damage may stop updating during open-world bosses. This remains unresolved; DPS totals and rankings can be incomplete.",12,"#E4B986");warning.Margin=new Thickness(0,10,0,0);news.Children.Add(warning);
        var newsCard=SettingsVisual.Card(news,new Thickness(18,16,18,16));newsCard.Margin=new Thickness(0,20,0,0);generalPage.Children.Add(newsCard);
        var requirement=new StackPanel();requirement.Children.Add(SettingsVisual.Text("GAME DATA REQUIREMENT",10,"#929CAD"));npcMessage.Margin=new Thickness(0,10,0,0);requirement.Children.Add(npcMessage);
        var help=SettingsVisual.Text("Install Npcap for Energy Bar, DPS Meter and Notifications, then restart Aion 2 Helper. FPS Counter works without it.",12,"#A7ADBA");help.Margin=new Thickness(0,8,0,14);requirement.Children.Add(help);
        npcDownload=SettingsVisual.Button("Download Npcap",OpenNpcapDownload,true);npcDownload.HorizontalAlignment=HorizontalAlignment.Left;npcDownload.ToolTip=NpcapSupport.DownloadUrl;AutomationProperties.SetName(npcDownload,"Download Npcap");requirement.Children.Add(npcDownload);
        npcCard=SettingsVisual.Card(requirement,new Thickness(18,16,18,16));npcCard.Margin=new Thickness(0,24,0,0);generalPage.Children.Add(npcCard);UpdateNpcap();
    }
    void OpenNpcapDownload(){try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(NpcapSupport.DownloadUrl){UseShellExecute=true});}catch(System.ComponentModel.Win32Exception){DownloadFailed();}catch(InvalidOperationException){DownloadFailed();}}
    void DownloadFailed(){noticeUntil=DateTime.UtcNow.AddSeconds(10);status.Text=UiLanguage.Text("Could not open the browser. Download Npcap at https://npcap.com/#download",overlay.Language);}
    void UpdateNpcap(){var visibility=npcSupport.Required?Visibility.Visible:Visibility.Collapsed;if(npcCard.Visibility!=visibility)npcCard.Visibility=visibility;string message=UiLanguage.Text(npcSupport.Message,overlay.Language);if(npcMessage.Text!=message)npcMessage.Text=message;}
    void SelectSection(bool general){SelectModule(general?0:1);}
    void SelectModule(int module){
        generalSelected=module==0;sectionTitle.Text=module==0?"General":module==1?"Energy Bar":module==2?"FPS Counter":module==3?"DPS Meter":"Notifications";sectionSubtitle.Text=module==0?"App preferences":module==1?"Energy bar for dash and sprint":module==2?"Your game frame rate, at a glance":module==3?"Combat damage · experimental alpha":"Useful alerts while you play";
        generalPage.Visibility=module==0?Visibility.Visible:Visibility.Collapsed;fpsPage.Visibility=module==2?Visibility.Visible:Visibility.Collapsed;dpsPage.Visibility=module==3?Visibility.Visible:Visibility.Collapsed;notificationPage.Visibility=module==4?Visibility.Visible:Visibility.Collapsed;activationRow.Visibility=previewCard.Visibility=segmented.Visibility=scroller.Visibility=module==1?Visibility.Visible:Visibility.Collapsed;
        for(int n=0;n<5;n++){bool selected=n==module;sections[n].Background=SettingsVisual.Brush(selected?"#235DC7B0":"#0CFFFFFF");sections[n].Foreground=SettingsVisual.Brush(selected?"#A7E5D5":"#929CAD");sections[n].BorderBrush=SettingsVisual.Brush(selected?"#3067C7B0":"#10FFFFFF");}
        UiLanguage.Apply(this,overlay.Language);
    }
    void BuildPreview(Grid body){
        var content=new StackPanel();var head=SettingsVisual.Text("PREVIEW",10,"#929CAD");head.Margin=new Thickness(0,0,0,10);content.Children.Add(head);
        content.Children.Add(previewImage);var bottom=new Grid{Margin=new Thickness(0,9,0,0)};bottom.ColumnDefinitions.Add(new ColumnDefinition());bottom.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(57)});bottom.Children.Add(previewSlider);AutomationProperties.SetName(previewSlider,"Preview energy");Grid.SetColumn(previewValue,1);previewValue.HorizontalAlignment=HorizontalAlignment.Right;bottom.Children.Add(previewValue);content.Children.Add(bottom);
        previewCard=SettingsVisual.Card(content,new Thickness(18,12,18,12));Grid.SetRow(previewCard,1);body.Children.Add(previewCard);
    }
    StackPanel Group(StackPanel page,string title){page.Children.Add(new TextBlock{Text=title,FontSize=11,Foreground=SettingsVisual.Brush("#929CAD"),Margin=new Thickness(2,5,0,9)});var stack=new StackPanel();page.Children.Add(SettingsVisual.Card(stack,new Thickness(16,3,16,3)));return stack;}
    Grid Row(StackPanel group,string label,string note=null){
        var row=new Grid{MinHeight=57,Margin=new Thickness(0,5,0,5)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,20,0)};text.Children.Add(SettingsVisual.Text(label));if(note!=null)text.Children.Add(new TextBlock{Text=note,FontSize=11,Foreground=SettingsVisual.Brush("#8994A6"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,0),MaxWidth=360,HorizontalAlignment=HorizontalAlignment.Left});row.Children.Add(text);group.Children.Add(row);return row;
    }
    void Toggle(StackPanel group,string key,string text,string note=null){var row=Row(group,text,note);var control=new CheckBox();switches[key]=control;Grid.SetColumn(control,1);row.Children.Add(control);AutomationProperties.SetName(control,text);}
    void Number(StackPanel group,string key,string text,string unit,decimal min,decimal max,int decimals,decimal increment,string note=null){
        var row=Row(group,text,note);var side=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(side,1);row.Children.Add(side);var control=new SettingsNumber(text,min,max,decimals,increment);numbers[key]=control;side.Children.Add(control);side.Children.Add(new TextBlock{Text=unit,Width=28,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(7,0,0,0),FontSize=11,Foreground=SettingsVisual.Brush("#8994A6")});
    }
    Button ActionRow(StackPanel group,string label,Action action){var button=SettingsVisual.Button(label,action);button.HorizontalAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,8,0,12);group.Children.Add(button);return button;}
    void BuildBehavior(){
        var group=Group(pages[0],"VISIBILITY");Toggle(group,"AutoHide","Auto-hide","Waits, then hides when energy is full.");Number(group,"HoldSeconds","Delay at 100%","s",0,30,2,.1M);
        group=Group(pages[0],"ANIMATION");Toggle(group,"Fade","Fade in and out","Smooth transitions when appearing and hiding.");Number(group,"FadeMs","Fade duration","ms",0,2000,0,10);Toggle(group,"Smooth","Smooth energy changes","Smooths energy use and recovery.");Number(group,"SmoothMs","Smoothing time","ms",0,1000,0,5);
    }
    void BuildAppearance(){
        var group=Group(pages[1],"PALETTE");Toggle(group,"DynamicColors","Color by energy level","Gradual transitions between your three colors.");
        foreach(var pair in new[]{new[]{"HighColor","High energy","From 75%"},new[]{"MediumColor","Medium energy","35–65% · also used as the fixed color"},new[]{"LowColor","Low energy","Up to 25%"}}){
            string key=pair[0],label=pair[1];var row=Row(group,label,pair[2]);var button=SettingsVisual.Button("",delegate{PickColor(key,label);});button.Width=142;button.Padding=new Thickness(12,8,12,8);Grid.SetColumn(button,1);row.Children.Add(button);colors[key]=button;AutomationProperties.SetName(button,"Choose color: "+label);
        }
        group=Group(pages[1],"LIGHT & CONTRAST");Toggle(group,"Emissive","Emissive glow","A bright filament with a soft halo.");Number(group,"GlowPercent","Glow intensity","%",0,200,0,5);Number(group,"TrackPercent","Dark track opacity","%",0,100,0,1,"Opacity of the background line.");
        ActionRow(group,"Reset effects and colors",delegate{var cfg=overlay.Configuration;cfg.Options=new EnergyBarOptions{Enabled=cfg.Options.Enabled};overlay.ApplyConfiguration(cfg);});
    }
    void BuildPosition(){
        var group=Group(pages[2],"GEOMETRY");Number(group,"Width","Width","px",BarDesign.MinimumWidth,BarDesign.MaximumWidth,0,1);Number(group,"Height","Thickness","px",BarDesign.MinimumHeight,BarDesign.MaximumHeight,0,1);Number(group,"X","Horizontal position","px",-100000,100000,0,1);Number(group,"Y","Vertical position","px",-100000,100000,0,1);
        ActionRow(group,"Reset size · 320 × 4",delegate{var cfg=overlay.Configuration;cfg.Bounds=new Drawing.Rectangle(cfg.Bounds.Location,new Drawing.Size(320,4));overlay.ApplyConfiguration(cfg);});
    }
    void SelectTab(int selected){for(int n=0;n<3;n++){pages[n].Visibility=n==selected?Visibility.Visible:Visibility.Collapsed;tabs[n].Background=SettingsVisual.Brush(n==selected?"#383A4659":"#0CFFFFFF");tabs[n].BorderBrush=SettingsVisual.Brush(n==selected?"#3DFFFFFF":"#10FFFFFF");tabs[n].Foreground=SettingsVisual.Brush(n==selected?"#F0F1F5":"#929CAD");}scroller.ScrollToTop();}
    bool Flag(string key){return switches[key].IsChecked==true;}decimal Num(string key){return numbers[key].Value;}
    internal EnergyConfiguration ReadConfiguration(){
        var options=new EnergyBarOptions{Enabled=Flag("Enabled"),AutoHide=Flag("AutoHide"),Fade=Flag("Fade"),Smooth=Flag("Smooth"),Emissive=Flag("Emissive"),DynamicColors=Flag("DynamicColors"),HoldSeconds=(double)Num("HoldSeconds"),FadeSeconds=(double)Num("FadeMs")/1000,SmoothingSeconds=(double)Num("SmoothMs")/1000,GlowPercent=(int)Num("GlowPercent"),TrackOpacity=(int)Math.Round((double)Num("TrackPercent")*255/100),LowColor=colorValues["LowColor"],MediumColor=colorValues["MediumColor"],HighColor=colorValues["HighColor"]};options.Normalize();
        return new EnergyConfiguration{Options=options,Bounds=new Drawing.Rectangle((int)Num("X"),(int)Num("Y"),(int)Num("Width"),(int)Num("Height")),Locked=overlay.InteractionLock.Locked,Maximum=overlay.Configuration.Maximum,Language=overlay.Language,Fps=overlay.Configuration.Fps,Dps=overlay.Configuration.Dps,Notifications=overlay.Configuration.Notifications};
    }
    void Changed(){if(loading||IsDisposed)return;overlay.ApplyConfiguration(ReadConfiguration());}
    void UpdateWidgetStatus(){string text=UiLanguage.Text(overlay.InteractionLock.Locked?"Widgets locked":"Widgets unlocked",overlay.Language);if(widgetStatus.Text==text)return;widgetStatus.Text=text;widgetStatus.Foreground=SettingsVisual.Brush(overlay.InteractionLock.Locked?"#EB9A91":"#84B8AB");}
    void RefreshStatus(object sender,EventArgs e){fpsPage.UpdateStatus();dpsPage.UpdateStatus();notificationPage.UpdateStatus();UpdateNpcap();if(DateTime.UtcNow>=noticeUntil&&status.Text!=overlay.ReadingStatus)status.Text=overlay.ReadingStatus;UpdateWidgetStatus();}
    void Reload(){if(IsDisposed)return;loading=true;try{
        var cfg=overlay.Configuration;if(fpsPage!=null)fpsPage.Reload();if(dpsPage!=null)dpsPage.Reload();if(notificationPage!=null)notificationPage.Reload();languagePicker.SelectedIndex=cfg.Language=="es"?1:0;var o=cfg.Options;switches["Enabled"].IsChecked=o.Enabled;switches["AutoHide"].IsChecked=o.AutoHide;switches["Fade"].IsChecked=o.Fade;switches["Smooth"].IsChecked=o.Smooth;switches["Emissive"].IsChecked=o.Emissive;switches["DynamicColors"].IsChecked=o.DynamicColors;
        numbers["Width"].Value=cfg.Bounds.Width;numbers["Height"].Value=cfg.Bounds.Height;numbers["X"].Value=cfg.Bounds.X;numbers["Y"].Value=cfg.Bounds.Y;numbers["HoldSeconds"].Value=(decimal)o.HoldSeconds;numbers["FadeMs"].Value=(decimal)(o.FadeSeconds*1000);numbers["SmoothMs"].Value=(decimal)(o.SmoothingSeconds*1000);numbers["GlowPercent"].Value=o.GlowPercent;numbers["TrackPercent"].Value=(decimal)(o.TrackOpacity*100.0/255);
        SetColor("LowColor",o.LowColor);SetColor("MediumColor",o.MediumColor);SetColor("HighColor",o.HighColor);
        foreach(var input in numbers.Values)input.IsEnabled=o.Enabled;foreach(var input in colors.Values)input.IsEnabled=o.Enabled;foreach(var input in switches)if(input.Key!="Enabled")input.Value.IsEnabled=o.Enabled;foreach(var tab in tabs)tab.IsEnabled=o.Enabled;
        numbers["HoldSeconds"].IsEnabled=o.Enabled&&o.AutoHide;numbers["FadeMs"].IsEnabled=o.Enabled&&o.Fade;numbers["SmoothMs"].IsEnabled=o.Enabled&&o.Smooth;numbers["GlowPercent"].IsEnabled=o.Enabled&&o.Emissive;colors["LowColor"].IsEnabled=colors["HighColor"].IsEnabled=o.Enabled&&o.DynamicColors;
        previewCard.IsEnabled=segmented.IsEnabled=scroller.IsEnabled=previewSlider.IsEnabled=o.Enabled;previewCard.Opacity=o.Enabled?1:.38;segmented.Opacity=scroller.Opacity=o.Enabled?1:.45;
        activation.Text=o.Enabled?"Enabled":"Disabled";undo.IsEnabled=overlay.CanUndoConfiguration;redo.IsEnabled=overlay.CanRedoConfiguration;UpdatePreview();status.Text=overlay.ReadingStatus;UpdateWidgetStatus();UpdateNpcap();UiLanguage.Apply(this,overlay.Language);
    }finally{loading=false;}}
    void SetColor(string key,string hex){
        colorValues[key]=hex;var content=new StackPanel{Orientation=Orientation.Horizontal};content.Children.Add(new Border{Width=17,Height=17,CornerRadius=new CornerRadius(5),Background=SettingsVisual.Brush(hex),BorderBrush=SettingsVisual.Brush("#45FFFFFF"),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,9,0)});content.Children.Add(SettingsVisual.Text(hex.ToUpperInvariant(),12,"#D1D6E0"));colors[key].Content=content;
    }
    void PickColor(string key,string label){var picker=new HelperColorPicker(colorValues[key],label,overlay.Language){Owner=this};if(picker.ShowDialog()==true){SetColor(key,picker.SelectedHex);Changed();}}
    void UpdatePreview(){if(colorValues.Count!=3||IsDisposed)return;int width=(int)Math.Max(200,Math.Min(640,ActualWidth>0?ActualWidth-325:550));var o=ReadConfiguration().Options;if(!o.Enabled){o.DynamicColors=false;o.MediumColor="#555B65";}using(var image=BarDesign.RenderEmissive(width,4,previewSlider.Value/100,false,true,1,o,previewGlow))previewImage.Source=SettingsVisual.Bitmap(image);previewValue.Text=Math.Round(previewSlider.Value)+" %";}
    internal void ShowNotifications(){SelectModule(4);}
    internal void VerifyNotifications(string root){SelectModule(4);notificationPage.Verify();var c=overlay.Configuration;c.Language="en";overlay.ApplyConfiguration(c);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-notifications-en.png"));c.Language="es";overlay.ApplyConfiguration(c);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-notifications-es.png"));if(sectionTitle.Text!="Notificaciones")throw new Exception("Notifications localization");}
    internal void ShowFps(){SelectModule(2);}
    internal void ShowDps(){SelectModule(3);}
    internal void ShowDpsHistory(){SelectModule(3);dpsPage.SelectHistory(true);}
    internal void VerifyDps(string root){SelectModule(3);dpsPage.Verify();var original=overlay.Configuration;var c=original.Copy();c.Language="en";c.Dps.Enabled=true;c.Dps.AutoHide=true;overlay.ApplyConfiguration(c);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-dps-en.png"));c.Language="es";overlay.ApplyConfiguration(c);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-dps-es.png"));dpsPage.RenderSections(Path.Combine(root,"designs"));overlay.CombatHistory.Add(DpsVerification.HistorySample(DpsHistory.UtcNow(),Math.Max(60,overlay.Configuration.Dps.HistoryMinimumSeconds)));dpsPage.SelectHistory(true);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-dps-history-es.png"));dpsPage.RenderHistoryDetails(Path.Combine(root,"designs","settings-dps-history-players-es.png"));dpsPage.SelectHistory(false);overlay.CombatHistory.Clear();if(sectionTitle.Text!="Medidor de DPS")throw new Exception("DPS localization");overlay.ApplyConfiguration(original);}
    internal void VerifyFps(string root){SelectModule(2);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-fps-en.png"));var cfg=overlay.Configuration;cfg.Language="es";overlay.ApplyConfiguration(cfg);SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-fps-es.png"));if(sectionTitle.Text!="Contador de FPS")throw new Exception("FPS section localization");}
    internal void VerifyLayout(){SelectSection(false);if(switches.Count!=6||numbers.Count!=9||colors.Count!=3)throw new Exception("Missing settings control");for(int n=0;n<3;n++){SelectTab(n);if(pages[n].Children.Count==0||pages[n].Visibility!=Visibility.Visible)throw new Exception("Empty settings tab");}VerifyModuleState(overlay.Configuration.Options.Enabled);}
    void VerifyModuleState(bool enabled){if(!switches["Enabled"].IsEnabled||previewCard.IsEnabled!=enabled||segmented.IsEnabled!=enabled||scroller.IsEnabled!=enabled||previewSlider.IsEnabled!=enabled)throw new Exception("Module switch must stay enabled while all energy controls/preview are disabled");if(!enabled)foreach(var control in numbers.Values)if(control.IsEnabled)throw new Exception("Disabled energy inputs must reject editing");}
    internal void VerifyHistory(string root){
        var baseline=overlay.Configuration;if(undo.IsEnabled||redo.IsEnabled)throw new Exception("Initial history buttons");
        var changed=baseline.Copy();changed.Options.LowColor="#123456";changed.Options.AutoHide=!baseline.Options.AutoHide;changed.Options.Enabled=false;changed.Options.GlowPercent=145;changed.Maximum=120000;changed.Bounds=new Drawing.Rectangle(baseline.Bounds.X+5,baseline.Bounds.Y-5,320,3);overlay.ApplyConfiguration(changed);
        VerifyModuleState(false);
        string designs=Path.Combine(root,"designs");Directory.CreateDirectory(designs);SettingsVisual.RenderPreview(this,Path.Combine(designs,"settings-disabled.png"));
        if(!undo.IsEnabled||redo.IsEnabled)throw new Exception("History buttons after change");overlay.UndoConfiguration();
        if(!ConfigurationHistory.Same(overlay.Configuration,baseline)||!ConfigurationHistory.Same(ReadConfiguration(),baseline)||undo.IsEnabled||!redo.IsEnabled)throw new Exception("Undo must restore model and every settings control");
        overlay.RedoConfiguration();if(!ConfigurationHistory.Same(overlay.Configuration,changed)||!undo.IsEnabled||redo.IsEnabled)throw new Exception("Redo must restore settings without recording a new change");
        VerifyModuleState(false);
        overlay.UndoConfiguration();overlay.ApplyConfiguration(baseline.Copy());if(!redo.IsEnabled)throw new Exception("No-op settings save after undo");
        var branch=baseline.Copy();branch.Options.MediumColor="#654321";overlay.ApplyConfiguration(branch);if(redo.IsEnabled)throw new Exception("New settings branch must disable redo");overlay.UndoConfiguration();
        VerifyModuleState(baseline.Options.Enabled);
    }
    internal void VerifyReopenedHistory(EnergyConfiguration baseline){
        if(undo.IsEnabled||!redo.IsEnabled)throw new Exception("History must survive closing/reopening Settings");
        redo.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(overlay.Configuration.Options.MediumColor!="#654321"||!undo.IsEnabled||redo.IsEnabled)throw new Exception("Reopened redo button");
        undo.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(!ConfigurationHistory.Same(overlay.Configuration,baseline)||!ConfigurationHistory.Same(ReadConfiguration(),baseline)||undo.IsEnabled||!redo.IsEnabled)throw new Exception("Reopened undo button");
    }
    internal void VerifyNpcap(string root,NpcapSupport requirement,Action install){
        var original=overlay.Configuration;var cfg=original.Copy();cfg.Language="en";cfg.Fps.Enabled=true;overlay.ApplyConfiguration(cfg);SelectModule(0);UpdateNpcap();
        if(npcCard.Visibility!=Visibility.Visible||npcMessage.Text!=NpcapSupport.MissingMessage||!npcDownload.IsEnabled||(string)npcDownload.ToolTip!=NpcapSupport.DownloadUrl||(string)npcDownload.Content!="Download Npcap")throw new Exception("Missing Npcap requires visible guidance and the official download action");
        SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-npcap-en.png"));cfg.Language="es";overlay.ApplyConfiguration(cfg);
        if(npcMessage.Text!="Npcap es necesario para la barra de energía, el medidor de DPS y las notificaciones."||(string)npcDownload.Content!="Descargar Npcap"||AutomationProperties.GetName(npcDownload)!="Descargar Npcap")throw new Exception("Npcap guidance must switch language immediately");
        SettingsVisual.RenderPreview(this,Path.Combine(root,"designs","settings-npcap-es.png"));SelectModule(2);if(!fpsPage.IsEnabled||sectionTitle.Text!="Contador de FPS")throw new Exception("Missing Npcap must not disable FPS settings");
        cfg.Options.Enabled=false;overlay.ApplyConfiguration(cfg);SelectModule(0);if(!npcDownload.IsEnabled||npcCard.Visibility!=Visibility.Visible)throw new Exception("Dependency help remains accessible with Energy Bar off");
        install();requirement.EnsureAvailable();RefreshStatus(null,null);if(npcCard.Visibility!=Visibility.Collapsed)throw new Exception("Successful dependency validation must clear the warning");overlay.ApplyConfiguration(original);
    }
    internal void VerifyLanguages(string root){
        var original=overlay.Configuration;languagePicker.SelectedIndex=0;var english=overlay.Configuration;SelectSection(true);
        string designs=Path.Combine(root,"designs");Directory.CreateDirectory(designs);SettingsVisual.RenderPreview(this,Path.Combine(designs,"settings-general-en.png"));
        languagePicker.SelectedIndex=1;var spanish=english.Copy();spanish.Language="es";
        if(!ConfigurationHistory.Same(spanish,overlay.Configuration)||!ConfigurationHistory.Same(spanish,ReadConfiguration())||!generalSelected||Title!="Aion 2 Helper · Ajustes"||(string)sections[1].Content!="◉   Barra de energía")throw new Exception("Live Spanish selection must preserve energy preferences and the current section");
        SettingsVisual.RenderPreview(this,Path.Combine(designs,"settings-general-es.png"));SelectSection(false);SelectTab(1);
        if(sectionTitle.Text!="Barra de energía"||(string)tabs[1].Content!="Apariencia")throw new Exception("Energy section translations");
        SettingsVisual.RenderPreview(this,Path.Combine(designs,"settings-energy-es.png"));
        var menu=new HelperTrayMenu(overlay);try{menu.Verify();if(!menu.VerifyLanguage("Ajustes"))throw new Exception("Spanish tray menu");SettingsVisual.RenderPreview(menu,Path.Combine(designs,"tray-menu-es.png"));}finally{menu.Close();}
        var picker=new HelperColorPicker("#B85416","Medium energy","es");try{if(!picker.VerifyLanguage("Elegir color"))throw new Exception("Spanish color picker");SettingsVisual.RenderPreview(picker,Path.Combine(designs,"color-picker-es.png"));}finally{picker.Close();}
        SelectSection(true);var disabled=spanish.Copy();disabled.Options.Enabled=false;overlay.ApplyConfiguration(disabled);if(!languagePicker.IsEnabled||generalPage.Opacity!=1)throw new Exception("Language must remain editable with the energy module disabled");
        overlay.UndoConfiguration();overlay.UndoConfiguration();if(!ConfigurationHistory.Same(english,overlay.Configuration)||languagePicker.SelectedIndex!=0||Title!="Aion 2 Helper · Settings"||(string)tabs[1].Content!="Appearance"||(string)sections[1].Content!="◉   Energy Bar")throw new Exception("Undo language must restore all English labels");
        overlay.RedoConfiguration();if(!ConfigurationHistory.Same(spanish,overlay.Configuration)||languagePicker.SelectedIndex!=1)throw new Exception("Redo language");overlay.ApplyConfiguration(original);
    }
}

internal sealed class HelperTrayMenu : Window {
    [StructLayout(LayoutKind.Sequential)]struct NativeRect {internal int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out NativeRect rectangle);
    readonly List<Button> actions=new List<Button>();readonly string lockLabel;readonly string menuLanguage;internal bool IsDisposed {get;private set;}
    internal HelperTrayMenu(EnergyOverlay owner){
        menuLanguage=owner.Language;
        Title="Aion 2 Helper · Menu";SettingsVisual.Theme(this);Width=246;Height=194;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;WindowStartupLocation=WindowStartupLocation.Manual;
        var content=new StackPanel{Margin=new Thickness(9,10,9,9)};Content=SettingsVisual.Shell(content);KeyboardNavigation.SetDirectionalNavigation(content,KeyboardNavigationMode.Cycle);
        var title=SettingsVisual.Text("AION 2 HELPER",10,"#8895A8");title.Margin=new Thickness(12,2,0,9);content.Children.Add(title);
        AddAction(content,"Settings","settings",owner.OpenSettings);
        lockLabel=owner.Configuration.Locked?"Unlock":"Lock";AddAction(content,lockLabel,owner.Configuration.Locked?"unlock":"lock",owner.ToggleWidgetsLock);
        content.Children.Add(new Border{Height=1,Background=SettingsVisual.Brush("#18FFFFFF"),Margin=new Thickness(11,7,11,7)});
        AddAction(content,"Quit Aion 2 Helper","close",delegate{owner.Close();});
        Deactivated+=delegate{if(IsVisible&&!IsDisposed)Close();};Closed+=delegate{IsDisposed=true;};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){Close();e.Handled=true;}};UiLanguage.Apply(this,owner.Language);
    }
    void AddAction(StackPanel host,string label,string kind,Action action){
        var button=SettingsVisual.Button("",delegate{Close();action();});button.Padding=new Thickness(12,9,12,9);button.BorderThickness=new Thickness(0);button.Background=Brushes.Transparent;button.Margin=new Thickness(0,1,0,1);AutomationProperties.SetName(button,label);
        var layout=new Grid{Width=198};layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(28)});layout.ColumnDefinitions.Add(new ColumnDefinition());var icon=new Grid{Width=16,Height=16,HorizontalAlignment=HorizontalAlignment.Left};layout.Children.Add(icon);
        string color=kind=="close"?"#DFA8A2":"#A4C9C1";var brush=SettingsVisual.Brush(color);
        Action<string> stroke=delegate(string data){icon.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(data),Stroke=brush,StrokeThickness=1.4,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round});};
        if(kind=="settings"){stroke("M8,0.8 L8,3 M8,13 L8,15.2 M0.8,8 L3,8 M13,8 L15.2,8 M2.8,2.8 L4.3,4.3 M11.7,11.7 L13.2,13.2 M2.8,13.2 L4.3,11.7 M11.7,4.3 L13.2,2.8");icon.Children.Add(new System.Windows.Shapes.Ellipse{Width=10,Height=10,Stroke=brush,StrokeThickness=1.4});icon.Children.Add(new System.Windows.Shapes.Ellipse{Width=3,Height=3,Stroke=brush,StrokeThickness=1.2});}
        else if(kind=="close")stroke("M4,4 L12,12 M12,4 L4,12");
        else{stroke("M3.5,7 L12.5,7 L12.5,14 L3.5,14 Z M8,9.5 L8,11.5");stroke(kind=="unlock"?"M5,7 L5,4 C5,0.5 11,0.5 11,4":"M5,7 L5,4 C5,0.5 11,0.5 11,4 L11,7");}
        var text=SettingsVisual.Text(label,12,kind=="close"?"#DFA8A2":"#E8ECF3");Grid.SetColumn(text,1);layout.Children.Add(text);button.Content=layout;
        var style=new Style(typeof(Button),(Style)FindResource(typeof(Button)));style.Setters.Add(new Setter(Button.BackgroundProperty,Brushes.Transparent));var hover=new Trigger{Property=Button.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Button.BackgroundProperty,SettingsVisual.Brush("#183F7369")));style.Triggers.Add(hover);button.ClearValue(Button.BackgroundProperty);button.Style=style;host.Children.Add(button);actions.Add(button);
    }
    internal static Drawing.Point Position(Drawing.Point cursor,Drawing.Rectangle area,int width,int height){
        int x=Math.Max(area.Left+8,Math.Min(cursor.X-width+16,area.Right-width-8));int y=cursor.Y-height-10;if(y<area.Top+8)y=cursor.Y+10;y=Math.Max(area.Top+8,Math.Min(y,area.Bottom-height-8));return new Drawing.Point(x,y);
    }
    internal void ShowAt(Drawing.Point cursor){
        Opacity=0;Show();UpdateLayout();IntPtr handle=new WindowInteropHelper(this).Handle;var area=System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;
        for(int pass=0;pass<2;pass++){NativeRect rectangle;GetWindowRect(handle,out rectangle);var location=Position(cursor,area,Math.Max(1,rectangle.Right-rectangle.Left),Math.Max(1,rectangle.Bottom-rectangle.Top));OverlayNative.SetWindowPos(handle,new IntPtr(-1),location.X,location.Y,0,0,0x0001|0x0010);}
        Opacity=1;Activate();actions[0].Focus();
    }
    internal void Verify(){
        if(actions.Count!=3||AutomationProperties.GetName(actions[1])!=UiLanguage.Text(lockLabel,menuLanguage))throw new Exception("Tray menu commands/lock label");
        foreach(var area in new[]{new Drawing.Rectangle(0,0,1920,1040),new Drawing.Rectangle(-1920,0,1920,1040)})foreach(var cursor in new[]{new Drawing.Point(area.Left+2,area.Top+2),new Drawing.Point(area.Right-2,area.Bottom+30)})if(!area.Contains(new Drawing.Rectangle(Position(cursor,area,246,194),new Drawing.Size(246,194))))throw new Exception("Tray menu must fit the target monitor working area");
    }
    internal bool VerifyLanguage(string settingsLabel){return AutomationProperties.GetName(actions[0])==settingsLabel;}
    internal void VerifyLockCommand(EnergyOverlay owner){
        owner.Show();System.Windows.Forms.Application.DoEvents();var before=owner.Configuration;actions[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(!IsDisposed||owner.Configuration.Locked==before.Locked)throw new Exception("Tray lock command must close the popup and toggle the shared lock");
        owner.UndoConfiguration();if(!ConfigurationHistory.Same(owner.Configuration,before))throw new Exception("Tray lock must use the shared undo history");
    }
}

internal sealed class PickerPlane : FrameworkElement {
    internal double Hue,Saturation,Value;internal event Action Changed;
    internal PickerPlane(){Height=190;Cursor=Cursors.Cross;MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){CaptureMouse();Select(e.GetPosition(this));e.Handled=true;};MouseMove+=delegate(object sender,MouseEventArgs e){if(IsMouseCaptured)Select(e.GetPosition(this));};MouseLeftButtonUp+=delegate{ReleaseMouseCapture();};}
    void Select(Point p){Saturation=Math.Max(0,Math.Min(1,p.X/Math.Max(1,ActualWidth)));Value=1-Math.Max(0,Math.Min(1,p.Y/Math.Max(1,ActualHeight)));InvalidateVisual();if(Changed!=null)Changed();}
    protected override void OnRender(DrawingContext dc){base.OnRender(dc);var rect=new Rect(0,0,ActualWidth,ActualHeight);dc.PushClip(new RectangleGeometry(rect,10,10));dc.DrawRectangle(new SolidColorBrush(SettingsVisual.Hsv(Hue,1,1)),null,rect);dc.DrawRectangle(new LinearGradientBrush(Colors.White,Color.FromArgb(0,255,255,255),0),null,rect);dc.DrawRectangle(new LinearGradientBrush(Colors.Transparent,Colors.Black,90),null,rect);dc.Pop();var p=new Point(Math.Max(7,Math.Min(ActualWidth-7,Saturation*ActualWidth)),Math.Max(7,Math.Min(ActualHeight-7,(1-Value)*ActualHeight)));dc.DrawEllipse(null,new Pen(SettingsVisual.Brush("#80000000"),4),p,6,6);dc.DrawEllipse(null,new Pen(Brushes.White,2),p,6,6);}
}

internal sealed class PickerHue : FrameworkElement {
    internal double Hue;internal event Action Changed;
    internal PickerHue(){Height=23;Cursor=Cursors.Hand;MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){CaptureMouse();Select(e.GetPosition(this));e.Handled=true;};MouseMove+=delegate(object sender,MouseEventArgs e){if(IsMouseCaptured)Select(e.GetPosition(this));};MouseLeftButtonUp+=delegate{ReleaseMouseCapture();};}
    void Select(Point p){Hue=Math.Max(0,Math.Min(359.999,p.X/Math.Max(1,ActualWidth)*360));InvalidateVisual();if(Changed!=null)Changed();}
    protected override void OnRender(DrawingContext dc){base.OnRender(dc);var gradient=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,0)};for(int n=0;n<=6;n++)gradient.GradientStops.Add(new GradientStop(SettingsVisual.Hsv(n*60,1,1),n/6.0));dc.DrawRoundedRectangle(gradient,null,new Rect(0,5,ActualWidth,13),6,6);double x=Math.Max(5,Math.Min(ActualWidth-5,Hue/360*ActualWidth));dc.DrawRoundedRectangle(null,new Pen(Brushes.White,2),new Rect(x-4,2,8,19),4,4);}
}

internal sealed class HelperColorPicker : Window {
    readonly TextBlock heading=SettingsVisual.Text("Choose color",23);
    readonly PickerPlane plane=new PickerPlane();readonly PickerHue hue=new PickerHue();readonly TextBox hex=new TextBox();readonly TextBox[] rgb={new TextBox(),new TextBox(),new TextBox()};
    readonly Border current=new Border(),original=new Border();readonly TextBlock error=SettingsVisual.Text("",11,"#EB9A91");readonly Image glow=new Image{Height=20,Stretch=Stretch.None};readonly BarDesign.GlowWorkspace workspace=new BarDesign.GlowWorkspace();
    readonly string language;bool writing;internal string SelectedHex {get;private set;}
    internal HelperColorPicker(string initial,string title,string selectedLanguage="en"){
        language=UiLanguage.Normalize(selectedLanguage);
        Title="Aion 2 Helper · Color";SettingsVisual.Theme(this);ResizeMode=ResizeMode.NoResize;Width=434;Height=610;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var content=new StackPanel{Margin=new Thickness(23,20,23,20)};Content=SettingsVisual.Shell(content);
        var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});content.Children.Add(header);header.Children.Add(heading);var close=SettingsVisual.Button("×",delegate{DialogResult=false;});close.Width=30;close.Height=30;close.Padding=new Thickness(0);Grid.SetColumn(close,1);header.Children.Add(close);AutomationProperties.SetName(close,"Cancel color selection");header.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.ButtonState==MouseButtonState.Pressed&&e.OriginalSource is TextBlock)DragMove();};
        content.Children.Add(new TextBlock{Text=title,FontSize=12,Foreground=SettingsVisual.Brush("#929CAD"),Margin=new Thickness(0,6,0,18)});content.Children.Add(plane);hue.Margin=new Thickness(0,14,0,14);content.Children.Add(hue);
        var samples=new Grid();samples.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(48)});samples.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(48)});samples.ColumnDefinitions.Add(new ColumnDefinition());content.Children.Add(samples);
        original.Background=SettingsVisual.Brush(initial);original.CornerRadius=new CornerRadius(8,0,0,8);original.Height=36;original.ToolTip="Previous color";samples.Children.Add(original);current.CornerRadius=new CornerRadius(0,8,8,0);current.Height=36;Grid.SetColumn(current,1);samples.Children.Add(current);glow.Margin=new Thickness(15,0,0,0);Grid.SetColumn(glow,2);samples.Children.Add(glow);
        var labels=new Grid{Margin=new Thickness(0,17,0,5)};for(int n=0;n<4;n++)labels.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(n==0?1.7:1,GridUnitType.Star)});content.Children.Add(labels);
        var fields=new Grid();for(int n=0;n<4;n++){fields.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(n==0?1.7:1,GridUnitType.Star)});var label=SettingsVisual.Text(new[]{"HEX","R","G","B"}[n],10,"#8E98AA");Grid.SetColumn(label,n);labels.Children.Add(label);var input=n==0?hex:rgb[n-1];input.Margin=new Thickness(0,0,n==3?0:7,0);Grid.SetColumn(input,n);fields.Children.Add(input);AutomationProperties.SetName(input,new[]{"HEX color","Red","Green","Blue"}[n]);input.MaxLength=n==0?7:3;input.LostKeyboardFocus+=delegate{CommitFields();};input.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Enter){CommitFields();e.Handled=true;}};}content.Children.Add(fields);error.Margin=new Thickness(0,5,0,0);error.Height=18;content.Children.Add(error);
        content.Children.Add(new TextBlock{Text="PALETTE",FontSize=10,Foreground=SettingsVisual.Brush("#8E98AA"),Margin=new Thickness(0,9,0,8)});var presets=new StackPanel{Orientation=Orientation.Horizontal};content.Children.Add(presets);
        foreach(string color in new[]{"#287044","#B85416","#A02D2D","#207A86","#3D5EAF","#79529B","#C59648","#C5CED7"}){string selected=color;var b=SettingsVisual.Button("",delegate{SetHex(selected);});b.Width=30;b.Height=30;b.Padding=new Thickness(0);b.Margin=new Thickness(0,0,10,0);b.Background=SettingsVisual.Brush(color);b.ToolTip=color;AutomationProperties.SetName(b,"Color "+color);presets.Children.Add(b);}
        var actions=new Grid{Margin=new Thickness(0,22,0,0)};actions.ColumnDefinitions.Add(new ColumnDefinition());actions.ColumnDefinitions.Add(new ColumnDefinition());content.Children.Add(actions);var cancel=SettingsVisual.Button("Cancel",delegate{DialogResult=false;});cancel.Margin=new Thickness(0,0,5,0);actions.Children.Add(cancel);var apply=SettingsVisual.Button("Apply color",delegate{if(CommitFields())DialogResult=true;},true);apply.Margin=new Thickness(5,0,0,0);Grid.SetColumn(apply,1);actions.Children.Add(apply);
        plane.Changed+=UpdateColor;hue.Changed+=delegate{plane.Hue=hue.Hue;plane.InvalidateVisual();UpdateColor();};
        hex.TextChanged+=delegate{if(!writing)hex.Tag=true;};foreach(var input in rgb)input.TextChanged+=delegate{if(!writing)input.Tag=true;};
        KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){DialogResult=false;e.Handled=true;}};Closed+=delegate{glow.Source=null;};SetHex(initial);UiLanguage.Apply(this,language);
    }
    void SetHex(string value){double h,s,v;SettingsVisual.ToHsv((Color)ColorConverter.ConvertFromString(value),out h,out s,out v);plane.Hue=h;plane.Saturation=s;plane.Value=v;hue.Hue=h;plane.InvalidateVisual();hue.InvalidateVisual();UpdateColor();}
    void UpdateColor(){var color=SettingsVisual.Hsv(plane.Hue,plane.Saturation,plane.Value);SelectedHex=SettingsVisual.Hex(color);writing=true;try{hex.Text=SelectedHex;hex.Tag=null;for(int n=0;n<3;n++){rgb[n].Text=new[]{color.R,color.G,color.B}[n].ToString(CultureInfo.InvariantCulture);rgb[n].Tag=null;}}finally{writing=false;}error.Text="";current.Background=new SolidColorBrush(color);
        var options=new EnergyBarOptions{DynamicColors=false,MediumColor=SelectedHex};using(var bitmap=BarDesign.RenderEmissive(160,4,.8,false,true,1,options,workspace))glow.Source=SettingsVisual.Bitmap(bitmap);
    }
    bool CommitFields(){if(writing)return true;bool dirtyRgb=false;foreach(var input in rgb)if(input.Tag!=null)dirtyRgb=true;
        if(hex.Tag!=null){string value=hex.Text.Trim();if(!value.StartsWith("#"))value="#"+value;if(!EnergyBarOptions.ValidColor(value)){error.Text=UiLanguage.Text("Enter a valid HEX color, such as #B85416.",language);return false;}SetHex(value);}
        else if(dirtyRgb){byte r,g,b;if(!byte.TryParse(rgb[0].Text,out r)||!byte.TryParse(rgb[1].Text,out g)||!byte.TryParse(rgb[2].Text,out b)){error.Text=UiLanguage.Text("RGB values must be between 0 and 255.",language);return false;}SetHex(SettingsVisual.Hex(Color.FromRgb(r,g,b)));}return true;
    }
    internal void VerifyColorInputs(){hex.Text="invalid";if(CommitFields())throw new Exception("Invalid HEX accepted");hex.Text="123456";if(!CommitFields()||SelectedHex!="#123456")throw new Exception("HEX input");rgb[0].Text="256";if(CommitFields())throw new Exception("Invalid RGB accepted");rgb[0].Text="42";if(!CommitFields()||!SelectedHex.StartsWith("#2A"))throw new Exception("RGB input");}
    internal bool VerifyLanguage(string text){return heading.Text==text;}
}
