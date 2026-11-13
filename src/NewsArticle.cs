using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using HtmlAgilityPack;

// HTML is parsed as data and mapped to native WPF elements. Never execute remote
// script, CSS, forms, XAML, media, or embedded browser content.
internal sealed class AionNewsArticle {
    readonly AionNewsService service;
    readonly CancellationToken cancel;
    readonly string original;
    readonly string language;
    readonly double width;
    int nodes,cells;
    bool truncated;
    internal AionNewsArticle(AionNewsService news,CancellationToken cancellation,string url,double availableWidth,string locale="en"){service=news;cancel=cancellation;original=url;language=locale;width=Math.Max(280,Math.Min(850,availableWidth));}
    internal static Task<HtmlNode> Parse(string html,CancellationToken cancellation){return Task.Run(delegate{cancellation.ThrowIfCancellationRequested();var document=new HtmlDocument{OptionFixNestedTags=true,OptionReadEncoding=false,OptionCheckSyntax=false};document.LoadHtml(html??"");return document.DocumentNode;},cancellation);}
    internal FlowDocument Render(HtmlNode root){
        nodes=cells=0;truncated=false;var doc=new FlowDocument{FontFamily=new FontFamily("Segoe UI"),FontSize=14,Foreground=SettingsVisual.Brush("#EDF5FF"),Background=Brushes.Transparent,PagePadding=new Thickness(16),ColumnWidth=10000,LineHeight=22,FlowDirection=FlowDirection.LeftToRight};
        Blocks(root,doc.Blocks,0);if(truncated){var p=new Paragraph(new Run(UiLanguage.Text("This article is very long. Read the remaining content in the official announcement.",language))){Foreground=SettingsVisual.Brush("#FFD990"),Margin=new Thickness(0,20,0,8)};doc.Blocks.Add(p);}return doc;
    }
    static bool Skip(HtmlNode node){return node.Name=="script"||node.Name=="style"||node.Name=="iframe"||node.Name=="object"||node.Name=="form"||node.Name=="noscript"||node.Name=="svg"||node.Name=="head";}
    bool Limit(int depth){if(cancel.IsCancellationRequested)return true;if(depth>32||++nodes>16000){truncated=true;return true;}return false;}
    void Blocks(HtmlNode parent,BlockCollection blocks,int depth){
        if(Limit(depth))return;
        Paragraph pending=null;
        foreach(var node in parent.ChildNodes){
            if(Skip(node))continue;
            if(node.Name=="table"){
                pending=null;var table=new Table{CellSpacing=0,Margin=new Thickness(0,10,0,14),FontSize=12};var group=new TableRowGroup();table.RowGroups.Add(group);
                var rows=node.SelectNodes(".//tr");if(rows==null)continue;int rowCount=0;
                foreach(var tr in rows){if(cancel.IsCancellationRequested)break;if(++rowCount>400||cells>=2400){truncated=true;break;}var row=new TableRow();group.Rows.Add(row);int columns=0;foreach(var td in tr.ChildNodes){if(td.Name!="td"&&td.Name!="th")continue;if(++cells>2400||++columns>24){truncated=true;break;}var cell=new TableCell{Padding=new Thickness(6),BorderBrush=SettingsVisual.Brush("#657F9BBE"),BorderThickness=new Thickness(.5),Background=rowCount%2==0?SettingsVisual.Brush("#202F5588"):Brushes.Transparent};int span;if(Int32.TryParse(td.GetAttributeValue("colspan","1"),out span))cell.ColumnSpan=Math.Max(1,Math.Min(24,span));Blocks(td,cell.Blocks,depth+1);if(cell.Blocks.Count==0)cell.Blocks.Add(new Paragraph());row.Cells.Add(cell);}}
                blocks.Add(table);continue;
            }
            if(node.Name=="ul"||node.Name=="ol"){
                pending=null;var list=new System.Windows.Documents.List{MarkerStyle=node.Name=="ol"?TextMarkerStyle.Decimal:TextMarkerStyle.Disc,Margin=new Thickness(10,4,0,8),Padding=new Thickness(18,0,0,0)};
                foreach(var li in node.ChildNodes)if(li.Name=="li"){var item=new ListItem();Blocks(li,item.Blocks,depth+1);list.ListItems.Add(item);}if(list.ListItems.Count>0)blocks.Add(list);continue;
            }
            if(node.Name=="div"||node.Name=="section"||node.Name=="article"||node.Name=="body"||node.Name=="html"||node.Name=="center"||node.Name=="li"){
                pending=null;Blocks(node,blocks,depth+1);continue;
            }
            if(node.Name=="p"||node.Name.Length==2&&node.Name[0]=='h'&&node.Name[1]>='1'&&node.Name[1]<='6'){
                pending=null;var p=new Paragraph{Margin=new Thickness(0,5,0,8)};if(node.Name!="p"){p.FontSize=node.Name=="h1"?23:18;p.FontWeight=FontWeights.SemiBold;p.Foreground=SettingsVisual.Brush("#ACEEFF");p.Margin=new Thickness(0,14,0,8);}Inlines(node,p.Inlines,depth+1);blocks.Add(p);continue;
            }
            if(node.Name=="hr"){pending=null;blocks.Add(new BlockUIContainer(new Border{Height=1,Background=FantasyUi.CardEdge,Margin=new Thickness(0,10,0,10)}));continue;}
            if(node.NodeType==HtmlNodeType.Comment)continue;
            if(pending==null){pending=new Paragraph{Margin=new Thickness(0,2,0,6)};blocks.Add(pending);}InlineNode(node,pending.Inlines,depth+1);
        }
    }
    void Inlines(HtmlNode parent,InlineCollection target,int depth){foreach(var child in parent.ChildNodes)InlineNode(child,target,depth+1);}
    void InlineNode(HtmlNode node,InlineCollection target,int depth){
        if(Limit(depth)||Skip(node))return;
        if(node.NodeType==HtmlNodeType.Text){string text=HtmlEntity.DeEntitize(node.InnerText).Replace('\u00a0',' ');if(text.Length>20000)text=text.Substring(0,20000);target.Add(new Run(text));return;}
        if(node.Name=="br"){target.Add(new LineBreak());return;}
        if(node.Name=="img"){
            Uri url=AionNewsSource.SafeUrl(node.GetAttributeValue("src",""),original,true);if(url==null)return;
            var picture=new Image{Width=width-64,Height=180,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left};var holder=new Border{Background=SettingsVisual.Brush("#30223961"),CornerRadius=new CornerRadius(6),Child=picture,Margin=new Thickness(0,8,0,8)};target.Add(new InlineUIContainer(holder));Load(picture,url.AbsoluteUri);return;
        }
        Span span=node.Name=="strong"||node.Name=="b"?(Span)new Bold():node.Name=="i"||node.Name=="em"?(Span)new Italic():new Span();
        if(node.Name=="a"){
            Uri url=AionNewsSource.SafeUrl(node.GetAttributeValue("href",""),original,false);
            if(url!=null){var link=new Hyperlink{Foreground=SettingsVisual.Brush("#8FEAFF"),NavigateUri=url};link.RequestNavigate+=delegate(object sender,System.Windows.Navigation.RequestNavigateEventArgs e){Open(e.Uri);e.Handled=true;};span=link;}
        }
        Inlines(node,span.Inlines,depth);target.Add(span);
    }
    async void Load(Image image,string url){
        var picture=await service.Image(url,1000,cancel).ConfigureAwait(false);if(cancel.IsCancellationRequested||image.Dispatcher.HasShutdownStarted)return;
        try{await image.Dispatcher.InvokeAsync(new Action(delegate{if(cancel.IsCancellationRequested)return;if(picture!=null){image.Source=picture;image.Height=Math.Min(1600,image.Width*picture.PixelHeight/Math.Max(1,picture.PixelWidth));}else{image.Height=34;image.ToolTip=UiLanguage.Text("Image unavailable · open the original article",language);}})).Task.ConfigureAwait(false);}catch(OperationCanceledException){}
    }
    internal static void Open(Uri url){if(!AionNewsSource.Allowed(url,false))return;try{Process.Start(new ProcessStartInfo(url.AbsoluteUri){UseShellExecute=true});}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
    internal static void OpenAnnouncement(AionNewsItem item){Uri url;if(item==null||!Uri.TryCreate(item.Url,UriKind.Absolute,out url))return;if(item.Kind=="helper"){if(AionNewsSource.HelperPage(url))Launch(url);}else Open(url);}
    internal static void OpenTranslation(AionNewsItem item,string language){var url=AionNewsSource.Translation(item,language);if(url!=null)Launch(url);}
    static void Launch(Uri url){try{Process.Start(new ProcessStartInfo(url.AbsoluteUri){UseShellExecute=true});}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
}
