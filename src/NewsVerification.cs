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
                    var settings=new HelperSettings(overlay);try{foreach(string lang in UiLanguageMore.Codes){var cfg=overlay.Configuration;cfg.Language=lang;overlay.ApplyConfiguration(cfg);settings.ShowNews();settings.NewsPage.Sample(lang,new List<AionNewsItem>{item,parsed[0]});SettingsVisual.RenderPreview(settings,Path.Combine(root,"designs","news-"+lang+".png"));}}finally{settings.Close();}
                }
            }
            File.WriteAllText(Path.Combine(root,live?"news-live-test.txt":"news-test.txt"),"PASS: regional providers, feed schemas, article sanitizer/tables, unread revisions, bounded cache, native HUD relocation/absence and "+(live?"live official sources.":"News UI preview."));
        }finally{
            // Only this uniquely created fixture directory is removed; production cache stays untouched.
            for(int i=0;i<40;i++){try{Directory.Delete(folder,true);break;}catch(IOException){Thread.Sleep(25);}catch(UnauthorizedAccessException){Thread.Sleep(25);}}
        }
    }
    static void Live(AionNewsService service,AionNewsSource source,string folder){
        var counts=new List<string>();string sampleBody=null;AionNewsItem sampleItem=null;
        foreach(string region in new[]{"global","kr"})foreach(string kind in new[]{"news","patch","events"}){
            service.RefreshFeed(region,"en",kind,true).GetAwaiter().GetResult();var feed=service.Snapshot(region,"en",kind);Check(!feed.Failed&&feed.Items.Count>0,"Live regional feed: "+region+"/"+kind+" "+feed.Error);counts.Add(region+"/"+kind+":"+feed.Items.Count);if(kind=="news"&&region=="global"){var bitmap=service.Image(feed.Items[0].Image,320,CancellationToken.None).GetAwaiter().GetResult();Check(bitmap!=null,"Official thumbnail decodes in WPF without an external browser");}
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
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(folder),"news-live-evidence.txt"),String.Join("\r\n",counts.ToArray()));
    }
}
