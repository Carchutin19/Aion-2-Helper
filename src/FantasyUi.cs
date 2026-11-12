using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Desktop chrome only. Game widgets retain their existing renderers and settings.
internal static class FantasyUi {
    internal static readonly BitmapSource Brand=Load("Fantasy.Brand.png",400);
    static readonly BitmapSource Wallpaper=Load("Fantasy.Wallpaper.jpg",0);
    static readonly Brush Tint=Gradient(new Point(0,0),new Point(.22,1),
        "#212A4275",0,"#65304471",.23,"#BB172451",.49,"#C41C204A",.77,"#801D2055",1);
    static readonly Brush Edge=Gradient(new Point(0,0),new Point(1,1),
        "#F4BAF4FF",0,"#84579CEB",.31,"#DCC4A2FF",.79,"#EC78D8FF",1);
    static readonly Brush Rail=Gradient(new Point(0,0),new Point(1,1),
        "#4C54A3E8",0,"#A5294577",.37,"#A32C4674",.84,"#775DCEF5",1);
    internal static readonly Brush RailEdge=Gradient(new Point(0,0),new Point(0,1),
        "#BB9DDCFF",0,"#728EC9F9",.21,"#3E96B9F6",.55,"#7EA5D7FF",.83,"#BBACF0FF",1);
    internal static readonly Brush Card=Gradient(new Point(0,0),new Point(1,1),
        "#C2263664",0,"#D0182344",.51,"#C822294E",1);
    internal static readonly Brush CardEdge=Gradient(new Point(0,0),new Point(1,1),
        "#8D92B7FF",0,"#456C8FCE",.46,"#586F6DBB",1);
    internal static readonly ImageSource[] ModuleIcons={
        Icon("M9,2.5 H15 L15.7,5.6 18.5,5.5 21.5,10 19.6,12 21.5,14 18.5,18.5 15.7,18.4 15,21.5 H9 L8.3,18.4 5.5,18.5 2.5,14 4.4,12 2.5,10 5.5,5.5 8.3,5.6 Z M15.2,12 A3.2,3.2 0 1 1 8.8,12 A3.2,3.2 0 1 1 15.2,12"),
        Icon("M3,21 H21 M4,17 V11 H7 V17 Z M10.5,17 V4 H13.5 V17 Z M17,17 V8 H20 V17 Z"),
        Icon("M4,18 A9,9 0 1 1 20,18 M12,13 L16.5,8.5 M5,13 H7 M12,5 V7 M17,13 H19 M8,19 H16"),
        Icon("M3,3 L8,4 17,13 13,17 4,8 Z M12,18 L18,12 M16,16 L21,21 M13,7 L16,4 21,3 20,8 17,11 M7.5,12.5 L6.5,13.5 10.5,17.5 11.5,16.5 M5.5,12.5 L11.5,18.5 M8,16 L3,21"),
        Icon("M12,4 C8,4 6,7 6,11 V14 L3,17 H21 L18,14 V11 C18,7 16,4 12,4 Z M10,21 H14 M12,2 V4"),
        Icon("M4,6 H20 M4,12 H20 M4,18 H20 M8,4 V8 M16,10 V14 M10,16 V20"),
        Icon("M8,13 C10,10 14,10 16,13 C18,16 21,17 19,20 C17,23 14,20 12,20 C10,20 7,23 5,20 C3,17 6,16 8,13 Z M6.2,10.7 C4.6,12.3 2.2,10.1 2,7.9 C1.7,5.5 4,5 5.7,7 C7,8.4 7.3,9.7 6.2,10.7 Z M10.1,8.7 C8,9.1 7.1,6.3 7.4,4.1 C7.8,1.3 10,1.1 11,3.2 C12,5.4 11.6,8.3 10.1,8.7 Z M13.9,8.7 C16,9.1 16.9,6.3 16.6,4.1 C16.2,1.3 14,1.1 13,3.2 C12,5.4 12.4,8.3 13.9,8.7 Z M17.8,10.7 C19.4,12.3 21.8,10.1 22,7.9 C22.3,5.5 20,5 18.3,7 C17,8.4 16.7,9.7 17.8,10.7 Z",true),
        Icon("M3,20 L12,11 M5,22 L14,13 M9,7 L14,2 21,9 16,14 Z M12,4 L19,11"),
        Icon("M3,6 H21 V18 H3 Z M6,9 H7 M11,9 H12 M16,9 H17 M6,12 H7 M11,12 H12 M16,12 H17 M7,15 H17"),
        Icon("M3,18 L8,5 13,18 M5,13 H11 M14,18 L17.5,9 21,18 M15.5,15 H19.5"),
        Icon("M13,2 L4,14 H11 L10,22 20,10 H13 Z"),
        Icon("M5,3 H15 L19,7 V21 H5 Z M15,3 V7 H19 M8,10 H16 M8,14 H16 M8,18 H14"),
        Icon("M12,2 L21,7 V17 L12,22 3,17 V7 Z M3,7 L12,12 21,7 M12,12 V22"),
        Icon("M12,2 L21,8 17,18 12,22 7,18 3,8 Z M3,8 H21 M12,2 L8,8 12,22 16,8 Z"),
        Icon("M3,3 H18 V21 H3 Z M18,7 H22 V19 Q22,21 20,21 H18 M6,6 H15 V10 H6 Z M6,13 H15 M6,16 H15 M6,19 H12")
    };

    static ImageSource Icon(string data,bool solid=false){
        var glyph=new DrawingGroup();
        var ice=new SolidColorBrush(Color.FromRgb(208,237,255));
        using(var dc=glyph.Open()){
            dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,24,24));
            var stroke=solid?null:new Pen(ice,1.6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};
            dc.DrawGeometry(solid?ice:null,stroke,Geometry.Parse(data));
        }
        var image=new DrawingImage(glyph);image.Freeze();return image;
    }
    static BitmapSource Load(string name,int width){
        using(var stream=typeof(FantasyUi).Assembly.GetManifestResourceStream(name)){
            var bitmap=new BitmapImage();
            bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;
            if(width>0)bitmap.DecodePixelWidth=width;
            bitmap.EndInit();bitmap.Freeze();return bitmap;
        }
    }
    static Color ColorAt(string value){return (Color)ColorConverter.ConvertFromString(value);}
    static Brush Gradient(Point start,Point end,params object[] stops){
        var gradient=new LinearGradientBrush{StartPoint=start,EndPoint=end};
        for(int i=0;i<stops.Length;i+=2)gradient.GradientStops.Add(new GradientStop(ColorAt((string)stops[i]),Convert.ToDouble(stops[i+1])));
        gradient.Freeze();return gradient;
    }
    internal static Brush RailSurface(){return Rail;}
    internal static void Select(Button button,bool selected,bool nav=false){
        button.SetResourceReference(FrameworkElement.StyleProperty,nav?(!button.IsEnabled?"ComingSoonNavButton":selected?"SelectedNavButton":"NavButton"):(selected?"SelectedButton":"TabButton"));
    }
    internal static Border Shell(UIElement content){
        var art=new ImageBrush(Wallpaper){Stretch=Stretch.UniformToFill,AlignmentX=AlignmentX.Center,AlignmentY=AlignmentY.Top};
        art.Freeze();
        var layers=new Grid{Background=SettingsVisual.Brush("#101A36")};
        var background=new Grid{FlowDirection=FlowDirection.LeftToRight,IsHitTestVisible=false};
        layers.Children.Add(background);
        background.Children.Add(new Border{Background=art,Margin=new Thickness(0,-168,0,0)});
        background.Children.Add(new Border{Background=Tint});
        background.Children.Add(new Border{Background=Bloom("#796CBFFF",.015,.94,.37,.50)});
        background.Children.Add(new Border{Background=Bloom("#65AF62FF",.99,.13,.37,.53)});
        background.Children.Add(new Border{CornerRadius=new CornerRadius(19),Margin=new Thickness(5),BorderThickness=new Thickness(1),
            BorderBrush=Gradient(new Point(0,0),new Point(1,1),"#8EACECFF",0,"#001E6AB3",.43,"#7AC5ABFF",1)});
        layers.Children.Add(content);
        layers.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler(UpdateScrollFade),true);
        layers.SizeChanged+=delegate(object sender,SizeChangedEventArgs e){layers.Clip=new RectangleGeometry(new Rect(e.NewSize),20,20);};
        return new Border{CornerRadius=new CornerRadius(21),BorderThickness=new Thickness(1),BorderBrush=Edge,Child=layers};
    }
    static void UpdateScrollFade(object sender,ScrollChangedEventArgs e){
        var viewer=e.OriginalSource as ScrollViewer;
        if(viewer==null||viewer.Template==null||viewer.TemplatedParent is TextBoxBase)return;
        var viewport=viewer.Template.FindName("PART_ScrollContentPresenter",viewer) as ScrollContentPresenter;
        if(viewport==null)return;

        // Mask the viewport only, leaving the thumb, surrounding controls and wallpaper intact.
        bool canFade=viewport.ActualHeight>=48&&viewer.VerticalScrollBarVisibility!=ScrollBarVisibility.Disabled;
        bool above=canFade&&viewer.VerticalOffset>.01;
        bool below=canFade&&viewer.ScrollableHeight-viewer.VerticalOffset>.01;
        if(!above&&!below){
            if(viewport.OpacityMask!=null)viewport.ClearValue(UIElement.OpacityMaskProperty);
            return;
        }
        double height=viewport.ActualHeight,edge=Math.Min(height*.20,26);
        double topEnd=above?edge/height:0,bottomStart=below?1-edge/height:1;
        var current=viewport.OpacityMask as LinearGradientBrush;
        if(current!=null&&current.EndPoint.Y==height&&current.GradientStops.Count==4&&
            current.GradientStops[1].Offset==topEnd&&current.GradientStops[2].Offset==bottomStart)return;
        var mask=new LinearGradientBrush{MappingMode=BrushMappingMode.Absolute,StartPoint=new Point(0,0),EndPoint=new Point(0,height)};
        mask.GradientStops.Add(new GradientStop(above?Colors.Transparent:Colors.White,0));
        mask.GradientStops.Add(new GradientStop(Colors.White,topEnd));
        mask.GradientStops.Add(new GradientStop(Colors.White,bottomStart));
        mask.GradientStops.Add(new GradientStop(below?Colors.Transparent:Colors.White,1));
        mask.Freeze();viewport.OpacityMask=mask;
    }
    static Brush Bloom(string hex,double x,double y,double width,double height){
        var center=new Point(x,y);var color=ColorAt(hex);
        var brush=new RadialGradientBrush(color,Color.FromArgb(0,color.R,color.G,color.B)){
            Center=center,GradientOrigin=center,RadiusX=width,RadiusY=height};
        brush.Freeze();return brush;
    }
}
