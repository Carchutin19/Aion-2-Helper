using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Documents;

internal static class AionNewsVerification {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    internal static void Run(string root,bool live){
        Directory.CreateDirectory(Path.Combine(root,"designs"));
        string folder=Path.Combine(root,"news-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            var source=AionNewsSource.Default;
            Check(source.Feed("kr","es","patch").Contains("aion2/board/update_ko/"),"Korea has its own board and provider");
            Check(source.Feed("global","es","patch").Contains("update_es/"),"Global localized patch board");
            Check(source.Feed("kr","en","events").Contains("DOMAIN_AION2%2C")&&!source.Feed("kr","en","events").Contains("GLOBAL"),"Korean event domain is distinct");
            Check(!AionNewsSource.Allowed(new Uri("https://plaync.com.evil.invalid/a"),true)&&!AionNewsSource.Allowed(new Uri("http://aion2.plaync.com/a"),false)&&!AionNewsSource.Allowed(new Uri("https://aion2.plaync.com:8443/a"),false),"News HTTPS host/port restrictions");
            var helper=HelperRelease(folder);
            Thumbnails(folder);
            foreach(string lang in UiLanguageMore.Codes){
                var korean=new AionNewsItem{Region="kr",Kind="patch",Locale="ko",Url=source.Page("kr",lang,"patch","sample & id")};
                var translation=AionNewsSource.Translation(korean,lang);Check(translation!=null&&translation.Host=="translate.google.com"&&translation.Query.Contains("sl=ko")&&translation.Query.Contains("tl="+lang)&&translation.Query.Contains(Uri.EscapeDataString(korean.Url)),"Translation keeps the exact Korean announcement and chosen target language");
                Check(AionNewsSource.Translation(new AionNewsItem{Region="kr",Kind="news",Url="https://evil.invalid/"},lang)==null,"Translation rejects untrusted article URLs");
                Check(UiLanguage.Text("What's new in Helper",lang)!=""&&UiLanguage.Text("Translate in browser",lang)!="","News actions have labels in all interface languages");
            }
            Check(AionNewsSource.Translation(helper,"es")==null&&AionNewsSource.Translation(new AionNewsItem{Region="global",Locale="es",Kind="news",Url=source.Page("global","es","news","1")},"es")==null,"No redundant or misleading translation for native articles or Helper notes");
            var item=new AionNewsItem{Id="abc",Title="AION 2 patch",Kind="patch",Region="global",Locale="en",Published="2026-11-11T08:00:00Z",Updated="2026-11-11T08:30:00Z",Url=source.Page("global","en","patch","abc"),Summary="Updates and improvements for your next adventure."};
            var service=new AionNewsService(folder);service.Initialize().GetAwaiter().GetResult();Check(service.IsUnread(item),"An unseen article is unread");service.MarkRead(item);Check(!service.IsUnread(item),"Opening an article marks its revision read");item.Updated="2026-11-11T08:31:00Z";Check(service.IsUnread(item),"Updated article becomes unread again");
            const string list="{\"contentList\":[{\"id\":\"123\",\"title\":\"Patch &amp; notes\",\"summary\":\"<b>Summary</b>\",\"timestamps\":{\"postedAt\":\"2026-11-11T08:00:00Z\",\"updatedAt\":\"2026-11-11T09:00:00Z\"},\"thumbnailUrlList\":[\"https://fizz-download.playnccdn.com/sample\"]}]}";
            var parsed=AionNewsJson.Feed(list,"global","en","patch",source);Check(parsed.Count==1&&parsed[0].Title=="Patch & notes"&&parsed[0].Published=="2026-11-11T08:00:00Z"&&parsed[0].Summary=="Summary","Official metadata and HTML entities");
            const string events="{\"content\":[{\"idx\":1,\"itemTitle\":\"Event\",\"itemStart\":\"2026-11-11T08:00:00Z\",\"itemEnd\":\"2026-12-11T08:00:00Z\",\"marketingEntrySet\":[{\"entryDevice\":\"NORMAL\",\"entryUrl\":\"/en-us/eventon/board/event_notice/view?articleId=1\"}],\"marketingItemAdditionSet\":[{\"additionType\":\"eventListImgUrl\",\"additionData\":\"https://fizz-download.playnccdn.com/event\"}]}]}";
            Check(AionNewsJson.Feed(events,"global","en","events",source).Count==1,"Promotion content schema and relative links");
            bool invalid=false;try{AionNewsJson.Feed("{}","global","en","news",source);}catch(FormatException){invalid=true;}Check(invalid,"Schema failures must not replace a feed with empty success");
            var cache=new AionNewsCacheEntry{Url=item.Url,Checked=DateTime.UtcNow,Data=new byte[]{0,255,8,42}};var json=new JavaScriptSerializer().Serialize(cache);var cached=new JavaScriptSerializer().Deserialize<AionNewsCacheEntry>(json);Check(cached.Data.Length==4&&cached.Data[1]==255&&json.Contains("Payload"),"Portable bounded binary cache round trip");
            var html=AionNewsArticle.Parse("<h2>Patch</h2><script>bad()</script><p>Hello <b>Daeva</b><br>again</p><table><tr><th>A</th><td>B</td></tr></table><iframe src='https://evil.invalid'></iframe>",CancellationToken.None).GetAwaiter().GetResult();
            var document=new AionNewsArticle(service,CancellationToken.None,item.Url,700).Render(html);string plain=new TextRange(document.ContentStart,document.ContentEnd).Text;Check(plain.Contains("Daeva")&&!plain.Contains("bad()")&&plain.Contains("B"),"Native article rendering preserves tables/text without executing remote content");
            NativeHudVisibility.Verify(root);
            if(live)Live(service,source,folder);
            else{
                using(var overlay=new EnergyOverlay(root,true)){
                    var settings=new HelperSettings(overlay);try{foreach(string lang in UiLanguageMore.Codes){var cfg=overlay.Configuration;cfg.Language=lang;overlay.ApplyConfiguration(cfg);settings.ShowNews();settings.NewsPage.Sample(lang,new List<AionNewsItem>{item,parsed[0]});SettingsVisual.RenderPreview(settings,Path.Combine(root,"designs","news-"+lang+".png"));settings.NewsPage.Sample(lang,new List<AionNewsItem>{helper});SettingsVisual.RenderPreview(settings,Path.Combine(root,"designs","news-helper-"+lang+".png"));var korean=new AionNewsItem{Title="한국 공지 · Korean article",Region="kr",Locale="ko",Kind="patch",Url=source.Page("kr",lang,"patch","1"),Published=item.Published};settings.NewsPage.SampleArticle(korean,"<h2>한국 공지</h2><p>Original Korean announcement.</p>",service);SettingsVisual.RenderPreview(settings,Path.Combine(root,"designs","news-translation-"+lang+".png"));}}finally{settings.Close();}
                }
            }
            File.WriteAllText(Path.Combine(root,live?"news-live-test.txt":"news-test.txt"),"PASS: regional providers, feed schemas, article sanitizer/tables, unread revisions, bounded cache, native HUD relocation/absence and "+(live?"live official sources.":"News UI preview."));
        }finally{
            // Only this uniquely created fixture directory is removed; production cache stays untouched.
            for(int i=0;i<40;i++){try{Directory.Delete(folder,true);break;}catch(IOException){Thread.Sleep(25);}catch(UnauthorizedAccessException){Thread.Sleep(25);}}
        }
    }
    static AionNewsItem HelperRelease(string folder){
        string url="https://github.com/"+UpdateManager.Repository+"/releases/tag/v1.30.0";
        var row=new Dictionary<string,object>{{"draft",false},{"prerelease",false},{"tag_name","v1.30.0"},{"name","v1.30.0 Alpha — Game News & Buffs / Debuffs"},{"html_url",url},{"published_at","2026-11-12T15:00:00Z"},{"body","# New release\n\n- **Buffs and debuffs**\n- Game news\n\n<script>not executable</script>"},{"assets",new[]{new Dictionary<string,object>{{"name","Aion-2-Helper-v1.30.0-windows-x64.zip"},{"browser_download_url","https://github.com/"+UpdateManager.Repository+"/releases/download/v1.30.0/Aion-2-Helper-v1.30.0-windows-x64.zip"},{"size",100},{"digest","sha256:"+new string('a',64)}}}}};
        var serializer=new JavaScriptSerializer();string json=serializer.Serialize(row);var item=AionNewsJson.HelperRelease(json)[0];
        Check(item.Id=="v1.30.0"&&item.Url==url&&item.Html.Contains("<strong>Buffs and debuffs</strong>")&&!item.Html.Contains("<script>"),"Helper card uses validated published release and encoded release notes");
        Check(!AionNewsSource.Allowed(new Uri(url),false)&&AionNewsSource.HelperPage(new Uri(url))&&!AionNewsSource.HelperPage(new Uri(url+"?redirect=evil"))&&!AionNewsSource.TransportAllowed(new Uri(UpdateManager.LatestUrl+"/extra"),false)&&!AionNewsSource.TransportAllowed(new Uri(UpdateManager.LatestUrl),true),"Helper API exception never broadens article or image destinations");
        foreach(string field in new[]{"draft","prerelease","html_url"}){object previous=row[field];row[field]=field=="html_url"?(object)"https://github.com/other/repository/releases/tag/v1.30.0":true;bool rejected=false;try{AionNewsJson.HelperRelease(serializer.Serialize(row));}catch(IOException){rejected=true;}catch(FormatException){rejected=true;}Check(rejected,"Ignore unpublished or foreign release: "+field);row[field]=previous;}
        var saved=new AionNewsCacheEntry{Url=UpdateManager.LatestUrl,Checked=DateTime.UtcNow,ContentType="application/json",Data=System.Text.Encoding.UTF8.GetBytes(json)};
        File.WriteAllText(Path.Combine(folder,AionNewsService.Hash(saved.Url)+".json"),serializer.Serialize(saved));
        var service=new AionNewsService(folder);service.Initialize().GetAwaiter().GetResult();service.RefreshFeed("global","es","helper",false).GetAwaiter().GetResult();
        var feed=service.Snapshot("kr","fr","helper");Check(!feed.Failed&&feed.Items.Count==1&&feed.Items[0].Id==item.Id,"Helper cached feed is independent of game region and UI language");
        service.MarkAll("kr","fr","helper");Check(!service.IsUnread(feed.Items[0]),"Helper read state can be marked separately");
        row["body"]="Edited release notes";Check(service.IsUnread(AionNewsJson.HelperRelease(serializer.Serialize(row))[0]),"Revised release notes become unread without a version change");
        return item;
    }
    static void Thumbnails(string folder){
        var urls=new[]{"https://fizz-download.playnccdn.com/fixture-decoration","https://fizz-download.playnccdn.com/fixture-news-picture"};
        for(int i=0;i<urls.Length;i++){
            var visual=new System.Windows.Media.DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawRectangle(i==0?System.Windows.Media.Brushes.Black:System.Windows.Media.Brushes.SteelBlue,null,new Rect(0,0,300,i==0?25:180));
            var image=new System.Windows.Media.Imaging.RenderTargetBitmap(300,i==0?25:180,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(visual);
            using(var stream=new MemoryStream()){var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));encoder.Save(stream);var entry=new AionNewsCacheEntry{Url=urls[i],Checked=DateTime.UtcNow,ContentType="image/png",Data=stream.ToArray()};File.WriteAllText(Path.Combine(folder,AionNewsService.Hash(urls[i])+".json"),new JavaScriptSerializer().Serialize(entry));}
        }
        var service=new AionNewsService(folder);var item=new AionNewsItem{Kind="news",Image=urls[0],Thumbnails=new List<string>(urls)};
        var selected=service.Thumbnail(item,320,CancellationToken.None).GetAwaiter().GetResult();Check(selected!=null&&selected.PixelHeight==180,"Cards skip decorative 300x25 strips and select actual article illustration");
        item.Thumbnails=new List<string>{urls[0]};Check(service.Thumbnail(item,320,CancellationToken.None).GetAwaiter().GetResult()==null,"Decoration-only articles keep readable fallback cards");
        Check(service.Image(urls[0],320,CancellationToken.None).GetAwaiter().GetResult()!=null,"Decorative images remain available in full original articles");
        item.Kind="events";Check(service.Thumbnail(item,320,CancellationToken.None).GetAwaiter().GetResult()!=null,"Official event banners retain their own aspect ratios");
        string json="{\"contentList\":[{\"id\":\"with-images\",\"title\":\"News\",\"thumbnailUrlList\":[\"https://evil.invalid/image\",\""+urls[0]+"\",\""+urls[1]+"\",\""+urls[1]+"\"]}]}";
        var parsed=AionNewsJson.Feed(json,"global","es","news",AionNewsSource.Default)[0];Check(parsed.Thumbnails.Count==2&&parsed.Thumbnails[1]==urls[1],"Feed retains distinct allowed thumbnail alternatives");
    }
    static void Live(AionNewsService service,AionNewsSource source,string folder){
        var counts=new List<string>();string sampleBody=null;AionNewsItem sampleItem=null;List<AionNewsItem> sampleCards=null;
        service.RefreshFeed("global","es","helper",true).GetAwaiter().GetResult();var helper=service.Snapshot("kr","de","helper");Check(!helper.Failed&&helper.Items.Count==1&&AionNewsSource.HelperPage(new Uri(helper.Items[0].Url)),"Latest published Helper release loads from GitHub: "+helper.Error);counts.Add("helper/latest:"+helper.Items[0].Id);Check(service.Article(helper.Items[0],CancellationToken.None).GetAwaiter().GetResult().Length>40,"Full published Helper release notes available offline after feed load");
        foreach(string region in new[]{"global","kr"})foreach(string kind in new[]{"news","patch","events"}){
            service.RefreshFeed(region,"en",kind,true).GetAwaiter().GetResult();var feed=service.Snapshot(region,"en",kind);Check(!feed.Failed&&feed.Items.Count>0,"Live regional feed: "+region+"/"+kind+" "+feed.Error);counts.Add(region+"/"+kind+":"+feed.Items.Count);if(kind=="news"&&region=="global"){var bitmap=service.Image(feed.Items[0].Image,320,CancellationToken.None).GetAwaiter().GetResult();Check(bitmap!=null,"Official thumbnail decodes in WPF without an external browser");}
            if(kind=="news"&&region=="global"){sampleCards=feed.Items.GetRange(0,Math.Min(4,feed.Items.Count));foreach(var card in sampleCards){var picture=service.Thumbnail(card,320,CancellationToken.None).GetAwaiter().GetResult();Check(picture==null||AionNewsService.CardImage(picture),"Global card never uses decorative strips");counts.Add("thumbnail/"+card.Id+":"+(picture==null?"fallback":picture.PixelWidth+"x"+picture.PixelHeight));}}
            if(kind=="patch"){
                string body=service.Article(feed.Items[0],CancellationToken.None).GetAwaiter().GetResult();Check(body.Length>40,"Live article content");
                if(region=="global"){
                    var dom=AionNewsArticle.Parse(body,CancellationToken.None).GetAwaiter().GetResult();
                    foreach(var img in dom.Descendants("img")){var url=AionNewsSource.SafeUrl(img.GetAttributeValue("src",""),feed.Items[0].Url,true);if(url!=null)service.Image(url.AbsoluteUri,1000,CancellationToken.None).GetAwaiter().GetResult();}
                    sampleBody=body;sampleItem=feed.Items[0];
                }
            }
        }
        if(sampleItem!=null){string testRoot=Path.GetDirectoryName(folder);using(var owner=new EnergyOverlay(testRoot,true)){var settings=new HelperSettings(owner);try{settings.ShowNews();settings.NewsPage.SampleArticle(sampleItem,sampleBody,service);SettingsVisual.RenderPreview(settings,Path.Combine(testRoot,"designs","news-official-article.png"));}finally{settings.Close();}}}
        if(sampleCards!=null){string testRoot=Path.GetDirectoryName(folder);using(var owner=new EnergyOverlay(testRoot,true)){var settings=new HelperSettings(owner);try{settings.ShowNews();settings.NewsPage.Sample("es",sampleCards);foreach(var item in sampleCards)settings.NewsPage.SamplePicture(item.Id,service.Thumbnail(item,320,CancellationToken.None).GetAwaiter().GetResult());SettingsVisual.RenderPreview(settings,Path.Combine(testRoot,"designs","news-global-thumbnails.png"));}finally{settings.Close();}}}
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(folder),"news-live-evidence.txt"),String.Join("\r\n",counts.ToArray()));
    }
}
