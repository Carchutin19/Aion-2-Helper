using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

internal sealed class AionNewsCard : INotifyPropertyChanged {
    internal AionNewsItem Item;
    public string Title {get;set;}
    public string Summary {get;set;}
    public string DateLabel {get;set;}
    public string Category {get;set;}
    public string Placeholder {get;set;}
    public Visibility NewVisibility {get;set;}
    BitmapSource image;
    public BitmapSource Picture {get{return image;}set{image=value;var handler=PropertyChanged;if(handler!=null)handler(this,new PropertyChangedEventArgs("Picture"));}}
    public event PropertyChangedEventHandler PropertyChanged;
}

internal sealed class AionNewsSettingsPane : Grid {
    readonly EnergyOverlay overlay;
    readonly AionNewsService service;
    readonly bool preview;
    readonly Button[] regionButtons=new Button[2],tabButtons=new Button[3];
    readonly Button refresh,readAll,back,original;
    readonly TextBlock status=SettingsVisual.Text("Loading news…",11,"#C1D5F3"),source=SettingsVisual.Text("",11,"#ACC7E8");
    readonly TextBlock detailTitle=SettingsVisual.Text("",23),detailDate=SettingsVisual.Text("",11,"#B8CDE8");
    readonly ListBox list=new ListBox{Background=Brushes.Transparent,BorderThickness=new Thickness(0),HorizontalContentAlignment=HorizontalAlignment.Stretch};
    readonly FlowDocumentScrollViewer article=new FlowDocumentScrollViewer{Background=Brushes.Transparent,BorderThickness=new Thickness(0),IsToolBarVisible=false,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
    readonly Grid feedHost=new Grid(),detailHost=new Grid();
    readonly DispatcherTimer timer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(30)};
    CancellationTokenSource images=new CancellationTokenSource(),detail=new CancellationTokenSource();
    string region="global",kind="news",language="en",renderKey="";
    int generation;
    bool released,started,fixture,changeQueued;
    AionNewsItem selected;
    internal event Action<bool> UnreadChanged;
    internal AionNewsSettingsPane(EnergyOverlay owner,bool isPreview){
        overlay=owner;preview=isPreview;service=AionNewsService.Current;
        RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition());RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var top=new Grid{Margin=new Thickness(0,0,0,12)};top.ColumnDefinitions.Add(new ColumnDefinition());top.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});Children.Add(top);
        var regions=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(regions);
        for(int i=0;i<2;i++){int index=i;var button=SettingsVisual.Button(i==0?"Global":"Korea",delegate{SwitchRegion(index==0?"global":"kr");});button.MinWidth=100;button.Margin=new Thickness(0,0,7,0);regionButtons[i]=button;regions.Children.Add(button);}
        var actions=new StackPanel{Orientation=Orientation.Horizontal};Grid.SetColumn(actions,1);top.Children.Add(actions);
        refresh=SettingsVisual.Button("Refresh news",delegate{Refresh(true);});refresh.ToolTip="News refresh at most once every 15 minutes. Manual refresh is limited to once every 30 seconds.";actions.Children.Add(refresh);
        readAll=SettingsVisual.Button("Mark all as read",delegate{service.MarkAll(region,language);});readAll.Margin=new Thickness(7,0,0,0);actions.Children.Add(readAll);
        var tabs=new Grid{Margin=new Thickness(0,0,0,12)};Grid.SetRow(tabs,1);Children.Add(tabs);
        string[] labels={"News","Patch Notes","Events"};string[] kinds={"news","patch","events"};
        for(int i=0;i<3;i++){int n=i;tabs.ColumnDefinitions.Add(new ColumnDefinition());var b=SettingsVisual.Button(labels[i],delegate{kind=kinds[n];CloseArticle();RenderFeed();});b.Margin=new Thickness(i==0?0:4,0,i==2?0:4,0);Grid.SetColumn(b,i);tabs.Children.Add(b);tabButtons[i]=b;}
        Grid.SetRow(feedHost,2);Children.Add(feedHost);feedHost.Children.Add(list);
        ScrollViewer.SetHorizontalScrollBarVisibility(list,ScrollBarVisibility.Disabled);ScrollViewer.SetVerticalScrollBarVisibility(list,ScrollBarVisibility.Auto);VirtualizingStackPanel.SetIsVirtualizing(list,true);VirtualizingStackPanel.SetVirtualizationMode(list,VirtualizationMode.Recycling);ScrollViewer.SetCanContentScroll(list,true);
        var itemStyle=new Style(typeof(ListBoxItem));itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));itemStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(0)));itemStyle.Setters.Add(new Setter(Control.MarginProperty,new Thickness(0,0,5,10)));itemStyle.Setters.Add(new Setter(Control.BackgroundProperty,Brushes.Transparent));itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty,new Thickness(0)));list.ItemContainerStyle=itemStyle;
        list.ItemTemplate=CardTemplate();list.SelectionChanged+=delegate{var card=list.SelectedItem as AionNewsCard;if(card!=null)ShowArticle(card.Item);};
        Grid.SetRow(detailHost,2);Children.Add(detailHost);detailHost.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});detailHost.RowDefinitions.Add(new RowDefinition());detailHost.Visibility=Visibility.Collapsed;
        var heading=new StackPanel{Margin=new Thickness(0,0,6,12)};detailHost.Children.Add(heading);
        var detailActions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,12)};heading.Children.Add(detailActions);back=SettingsVisual.Button("← Back to news",CloseArticle);original=SettingsVisual.Button("Open official article",delegate{if(selected!=null)AionNewsArticle.Open(new Uri(selected.Url));});original.Margin=new Thickness(8,0,0,0);detailActions.Children.Add(back);detailActions.Children.Add(original);heading.Children.Add(detailTitle);detailTitle.FontWeight=FontWeights.SemiBold;detailTitle.Margin=new Thickness(0,0,0,8);heading.Children.Add(detailDate);UiLanguage.PreserveDataText(detailTitle);UiLanguage.PreserveDataText(detailDate);
        var documentShell=SettingsVisual.Card(article,new Thickness(0));Grid.SetRow(documentShell,1);detailHost.Children.Add(documentShell);
        var bottom=new StackPanel{Margin=new Thickness(0,10,0,0)};Grid.SetRow(bottom,3);Children.Add(bottom);bottom.Children.Add(status);bottom.Children.Add(source);source.Margin=new Thickness(0,5,0,0);UiLanguage.PreserveDataText(status);UiLanguage.PreserveDataText(source);
        service.Changed+=OnChanged;timer.Tick+=OnTimer;
        IsVisibleChanged+=delegate{if(IsVisible&&!released){renderKey="";RenderFeed();if(started)Refresh(false);}};
        Loaded+=OnLoaded;SetLanguage(owner.Language);RenderFeed();
    }
    static FrameworkElementFactory BoundText(string path,double size,string color){var t=new FrameworkElementFactory(typeof(TextBlock));t.SetBinding(TextBlock.TextProperty,new Binding(path));t.SetValue(TextBlock.FontSizeProperty,size);t.SetValue(TextBlock.ForegroundProperty,SettingsVisual.Brush(color));t.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);return t;}
    static DataTemplate CardTemplate(){
        var shell=new FrameworkElementFactory(typeof(Border));shell.SetValue(Border.CornerRadiusProperty,new CornerRadius(12));shell.SetValue(Border.BackgroundProperty,FantasyUi.Card);shell.SetValue(Border.BorderBrushProperty,FantasyUi.CardEdge);shell.SetValue(Border.BorderThicknessProperty,new Thickness(1));shell.SetValue(Border.PaddingProperty,new Thickness(14));
        var row=new FrameworkElementFactory(typeof(DockPanel));row.SetValue(DockPanel.LastChildFillProperty,true);shell.AppendChild(row);
        var pictureShell=new FrameworkElementFactory(typeof(Grid));pictureShell.SetValue(FrameworkElement.WidthProperty,132.0);pictureShell.SetValue(FrameworkElement.HeightProperty,92.0);pictureShell.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,16,0));pictureShell.SetValue(DockPanel.DockProperty,Dock.Left);row.AppendChild(pictureShell);
        var placeholder=BoundText("Placeholder",28,"#A5CFF8");placeholder.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);placeholder.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);pictureShell.AppendChild(placeholder);
        var picture=new FrameworkElementFactory(typeof(Image));picture.SetBinding(Image.SourceProperty,new Binding("Picture"));picture.SetValue(Image.StretchProperty,Stretch.UniformToFill);picture.SetValue(UIElement.ClipToBoundsProperty,true);pictureShell.AppendChild(picture);
        var content=new FrameworkElementFactory(typeof(StackPanel));row.AppendChild(content);
        var category=new FrameworkElementFactory(typeof(DockPanel));content.AppendChild(category);
        var badge=BoundText("Placeholder",10,"#8AF1FF");badge.SetValue(TextBlock.TextProperty,"NEW");badge.SetBinding(UIElement.VisibilityProperty,new Binding("NewVisibility"));badge.SetValue(FrameworkElement.MarginProperty,new Thickness(8,0,0,0));badge.SetValue(DockPanel.DockProperty,Dock.Right);category.AppendChild(badge);category.AppendChild(BoundText("Category",10,"#9BE9FF"));
        var title=BoundText("Title",15,"#F5F8FF");title.SetValue(TextBlock.FontWeightProperty,FontWeights.SemiBold);title.SetValue(FrameworkElement.MarginProperty,new Thickness(0,4,0,5));content.AppendChild(title);content.AppendChild(BoundText("DateLabel",11,"#B5CBE8"));
        var summary=BoundText("Summary",12,"#D1DFF2");summary.SetValue(TextBlock.MaxHeightProperty,36.0);summary.SetValue(FrameworkElement.MarginProperty,new Thickness(0,6,0,0));content.AppendChild(summary);
        return new DataTemplate(typeof(AionNewsCard)){VisualTree=shell};
    }
    async void OnLoaded(object sender,RoutedEventArgs e){
        if(started||preview)return;started=true;await service.Initialize();if(released)return;region=service.Region;RenderFeed();var handler=UnreadChanged;if(handler!=null)handler(service.HasUnread);timer.Start();Refresh(false);
    }
    void OnTimer(object sender,EventArgs e){if(IsVisible)Refresh(false);}
    void SwitchRegion(string value){region=value;service.SelectRegion(value);CloseArticle();RenderFeed();Refresh(false);}
    internal void SetLanguage(string value){value=UiLanguage.Normalize(value);if(language==value&&renderKey!="")return;language=value;renderKey="";CloseArticle();RenderFeed();UiLanguage.Apply(this,language);if(started)Refresh(false);}
    async void Refresh(bool force){
        if(released||preview||fixture)return;string requested=region,lang=language;
        try{await service.Refresh(requested,lang,force);}catch(Exception e){if(!AionNewsService.Expected(e))throw;}
    }
    void OnChanged(){
        if(released||Dispatcher.HasShutdownStarted)return;
        lock(this){if(changeQueued)return;changeQueued=true;}
        Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{lock(this)changeQueued=false;if(released)return;var handler=UnreadChanged;if(handler!=null)handler(service.HasUnread);if(IsVisible&&selected==null)RenderFeed();}));
    }
    string Text(string value){return UiLanguage.Text(value,language);}
    string Category(string value){return Text(value=="patch"?"Patch Notes":value=="events"?"Events":"News");}
    string DateLabel(string value){var d=AionNewsJson.Date(value);return d.HasValue?d.Value.ToLocalTime().ToString("g",CultureInfo.GetCultureInfo(language=="ar"?"ar-SA":language=="es"?"es-ES":language=="de"?"de-DE":language=="fr"?"fr-FR":"en-US")):Text("Date unavailable");}
    void RenderFeed(){
        if(released||fixture)return;var feed=service.Snapshot(region,language,kind);
        foreach(var button in regionButtons)FantasyUi.Select(button,button==regionButtons[region=="kr"?1:0],false);for(int i=0;i<3;i++)FantasyUi.Select(tabButtons[i],i==(kind=="news"?0:kind=="patch"?1:2),false);
        source.Text=Text("Official source: NCSOFT")+" · "+(region=="kr"?Text("Korea · Original Korean articles"):Text("Global"))+" · "+Text("Dates shown in your local time");
        status.Text=feed.Loading?Text("Loading news…"):feed.Failed?Text(feed.Items.Count>0?"Source unavailable. Showing saved news.":"News could not be loaded. Try again later."):feed.Items.Count==0?Text("No articles available."):Text("Updated")+" · "+DateLabel(feed.Checked.ToString("o"));
        refresh.IsEnabled=!feed.Loading;readAll.IsEnabled=feed.Items.Count>0;
        string key=region+":"+language+":"+kind;foreach(var item in feed.Items)key+="|"+item.Id+":"+item.Revision+":"+service.IsUnread(item);
        if(key==renderKey)return;renderKey=key;images.Cancel();images.Dispose();images=new CancellationTokenSource();int epoch=++generation;
        var cards=new List<AionNewsCard>();foreach(var item in feed.Items)cards.Add(new AionNewsCard{Item=item,Title=item.Title,Summary=item.Summary,Category=Category(item.Kind),DateLabel=item.Kind=="events"?DateLabel(item.Published)+" — "+DateLabel(item.Ends):DateLabel(item.Published),Placeholder=item.Kind=="events"?"✦":"≡",NewVisibility=service.IsUnread(item)?Visibility.Visible:Visibility.Collapsed});
        UiLanguage.PreserveDataText(list);list.ItemsSource=cards;if(IsVisible)foreach(var card in cards)LoadImage(card,epoch,images.Token);
    }
    async void LoadImage(AionNewsCard card,int epoch,CancellationToken token){var image=await service.Image(card.Item.Image,320,token).ConfigureAwait(false);if(released||token.IsCancellationRequested||Dispatcher.HasShutdownStarted)return;try{await Dispatcher.InvokeAsync(new Action(delegate{if(!released&&!token.IsCancellationRequested&&epoch==generation)card.Picture=image;})).Task.ConfigureAwait(false);}catch(OperationCanceledException){}}
    async void ShowArticle(AionNewsItem item){
        if(released)return;selected=item;feedHost.Visibility=Visibility.Collapsed;detailHost.Visibility=Visibility.Visible;detail.Cancel();detail.Dispose();detail=new CancellationTokenSource();var token=detail.Token;service.MarkRead(item);detailTitle.Text=item.Title;detailDate.Text=Category(item.Kind)+" · "+DateLabel(item.Published)+(item.Kind=="events"?" — "+DateLabel(item.Ends):"");article.Document=new FlowDocument(new Paragraph(new Run(Text("Loading article…"))));UiLanguage.PreserveDataText(article);
        status.Text=Text("Official source: NCSOFT");
        try{
            string body=await service.Article(item,token);if(token.IsCancellationRequested||released)return;
            if(item.Kind=="events"){
                body=(String.IsNullOrEmpty(item.Image)?"":"<p><img src=\""+System.Net.WebUtility.HtmlEncode(item.Image)+"\"></p>")+"<p>"+System.Net.WebUtility.HtmlEncode(Text("Open the official article for event rules, rewards and interactive content."))+"</p>";
            }
            var parsed=await AionNewsArticle.Parse(body,token);if(token.IsCancellationRequested||released)return;
            article.Document=new AionNewsArticle(service,token,item.Url,Math.Max(320,ActualWidth-18),language).Render(parsed);
        }catch(Exception error){if(token.IsCancellationRequested||released)return;if(!AionNewsService.Expected(error))throw;article.Document=new FlowDocument(new Paragraph(new Run(Text("Article unavailable. You can still open the official announcement."))));}
    }
    void CloseArticle(){selected=null;detail.Cancel();article.Document=null;detailHost.Visibility=Visibility.Collapsed;feedHost.Visibility=Visibility.Visible;list.SelectedItem=null;}
    internal void Release(){released=true;timer.Stop();timer.Tick-=OnTimer;service.Changed-=OnChanged;images.Cancel();detail.Cancel();images.Dispose();detail.Dispose();list.ItemsSource=null;article.Document=null;UnreadChanged=null;}
    internal void Sample(string lang,List<AionNewsItem> items){fixture=true;language=lang;var cards=new List<AionNewsCard>();foreach(var i in items)cards.Add(new AionNewsCard{Item=i,Title=i.Title,Summary=i.Summary,Category=Category(i.Kind),DateLabel=DateLabel(i.Published),Placeholder="✦",NewVisibility=Visibility.Visible});list.ItemsSource=cards;status.Text=Text("Updated")+" · "+DateLabel(DateTime.UtcNow.ToString("o"));source.Text=Text("Official source: NCSOFT");UiLanguage.PreserveDataText(list);UiLanguage.Apply(this,lang);}
    internal void SampleArticle(AionNewsItem item,string html,AionNewsService sampleService){fixture=true;selected=item;detail.Cancel();detail.Dispose();detail=new CancellationTokenSource();feedHost.Visibility=Visibility.Collapsed;detailHost.Visibility=Visibility.Visible;detailTitle.Text=item.Title;detailDate.Text=Category(item.Kind)+" · "+DateLabel(item.Published);UiLanguage.PreserveDataText(article);article.Document=new AionNewsArticle(sampleService,detail.Token,item.Url,750,language).Render(AionNewsArticle.Parse(html,detail.Token).GetAwaiter().GetResult());}
}
