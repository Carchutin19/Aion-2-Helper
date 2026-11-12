using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using System.Web.Script.Serialization;

// Public editorial data only. No game/session identifiers enter the news module.
internal sealed class AionNewsItem {
    public string Id {get;set;}
    public string Region {get;set;}
    public string Locale {get;set;}
    public string Kind {get;set;}
    public string Title {get;set;}
    public string Summary {get;set;}
    public string Url {get;set;}
    public string Image {get;set;}
    public string Published {get;set;}
    public string Updated {get;set;}
    public string Ends {get;set;}
    public string Html {get;set;}
    internal string Key {get{return Region+":"+Locale+":"+Kind+":"+Id;}}
    internal string Revision {get{return System.String.IsNullOrEmpty(Updated)?Published??"":Updated;}}
}

internal sealed class AionNewsSource {
    public string GlobalApi {get;set;}
    public string KoreaApi {get;set;}
    public string PromotionApi {get;set;}
    public string OfficialWeb {get;set;}
    internal static AionNewsSource Default {get{return new AionNewsSource{
        GlobalApi="https://api-global-community.plaync.com/aion2_global",
        KoreaApi="https://api-community.plaync.com/aion2",
        PromotionApi="https://promotion.plaync.com",OfficialWeb="https://aion2.plaync.com"};}}
    internal static string Language(string language){return language=="es"||language=="de"||language=="fr"?language:"en";}
    internal static string Locale(string region,string language){return region=="kr"?"ko-kr":Language(language)=="en"?"en-us":Language(language)=="es"?"es-es":Language(language)=="de"?"de-de":"fr-fr";}
    internal static string Board(string region,string language,string kind){return (kind=="patch"?"update_":"notice_")+(region=="kr"?"ko":Language(language));}
    internal string Feed(string region,string language,string kind){
        if(kind=="events")return PromotionApi+"/"+Locale(region,language)+"/eventon/item?tag="+Uri.EscapeDataString("MKT_PROMOTION,DOMAIN_AION2"+(region=="kr"?"":"_GLOBAL")+",COUNTRY_"+(region=="kr"?"KR":"US")+",")+"&status=RUNNING&pageSize=24&page=1";
        return (region=="kr"?KoreaApi:GlobalApi)+"/board/"+Board(region,language,kind)+"/article/search/moreArticle?isVote=true&moreSize=24&moreDirection=BEFORE&previousArticleId=0";
    }
    internal string Article(AionNewsItem item){return (item.Region=="kr"?KoreaApi:GlobalApi)+"/board/"+Board(item.Region,item.Locale,item.Kind)+"/article/"+Uri.EscapeDataString(item.Id);}
    internal string Page(string region,string language,string kind,string id){return OfficialWeb+"/"+Locale(region,language)+"/board/"+(kind=="patch"?"update":"notice")+"/view?articleId="+Uri.EscapeDataString(id);}
    internal static bool Allowed(Uri uri,bool image){
        if(uri==null||uri.Scheme!=Uri.UriSchemeHttps||!uri.IsDefaultPort||!System.String.IsNullOrEmpty(uri.UserInfo))return false;
        string h=uri.IdnHost.ToLowerInvariant();
        return h=="aion2.plaync.com"||h=="api-global-community.plaync.com"||h=="api-community.plaync.com"||h=="promotion.plaync.com"||
            image&&(h=="fizz-download.playnccdn.com"||h=="wstatic-cdn.plaync.com"||h=="file.plaync.com"||h=="wstatic.plaync.co.kr");
    }
    internal static Uri SafeUrl(string value,string baseUrl,bool image){Uri uri;return Uri.TryCreate(new Uri(baseUrl),value??"",out uri)&&Allowed(uri,image)?uri:null;}
}

internal static class AionNewsJson {
    internal static Dictionary<string,object> Object(object value){return value as Dictionary<string,object>??new Dictionary<string,object>();}
    internal static object Get(object value,string key){object item;return Object(value).TryGetValue(key,out item)?item:null;}
    internal static string String(object value,string key){return Convert.ToString(Get(value,key),CultureInfo.InvariantCulture);}
    internal static IEnumerable<object> Array(object value){var list=value as IEnumerable;if(list!=null&&!(value is string)&&!(value is IDictionary))foreach(object item in list)yield return item;}
    internal static string Plain(string text,int maximum){
        text=Regex.Replace(text??"","<[^>]*>"," ");text=WebUtility.HtmlDecode(text);text=Regex.Replace(text,@"\s+"," ").Trim();return text.Length>maximum?text.Substring(0,maximum)+"â€¦":text;
    }
    internal static DateTime? Date(string value){DateTimeOffset d;return DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out d)?(DateTime?)d.UtcDateTime:null;}
    internal static List<AionNewsItem> Feed(string json,string region,string language,string kind,AionNewsSource sources){
        var root=new JavaScriptSerializer{MaxJsonLength=2097152}.DeserializeObject(json);var output=new List<AionNewsItem>();
        object rows=Get(root,kind=="events"?"content":"contentList");
        if(kind=="events"&&rows==null)rows=Get(Get(root,"data"),"content");
        if(rows==null)throw new FormatException("News feed schema changed");
        var keys=new HashSet<string>();
        foreach(object row in Array(rows)){
            if(output.Count>=24)break;
            var item=new AionNewsItem{Region=region,Locale=region=="kr"?"ko":AionNewsSource.Language(language),Kind=kind};
            if(kind=="events"){
                item.Id=String(row,"idx");item.Title=Plain(String(row,"itemTitle"),260);item.Published=String(row,"itemStart");item.Ends=String(row,"itemEnd");
                foreach(object addition in Array(Get(row,"marketingItemAdditionSet"))){string key=String(addition,"additionType");if(key=="eventListImgUrl"||key=="eventListSmallImgUrl"&&System.String.IsNullOrEmpty(item.Image))item.Image=String(addition,"additionData");if(key=="updateTimestamp")item.Updated=String(addition,"additionData");}
                foreach(object entry in Array(Get(row,"marketingEntrySet")))if(System.String.IsNullOrEmpty(item.Url)||String(entry,"entryDevice")=="NORMAL")item.Url=String(entry,"entryUrl");
            }else{
                object meta=Get(row,"contentMeta")??row;
                item.Id=String(meta,"id");item.Title=Plain(String(meta,"title"),260);item.Summary=Plain(String(row,"summary"),220);
                item.Published=String(Get(meta,"timestamps"),"postedAt");item.Updated=String(Get(meta,"timestamps"),"updatedAt");
                foreach(object thumb in Array(Get(row,"thumbnailUrlList")??Get(meta,"thumbnailUrlList"))){item.Image=thumb as string??String(thumb,"url");if(!System.String.IsNullOrEmpty(item.Image))break;}
                item.Url=sources.Page(region,language,kind,item.Id);
            }
            Uri link=System.String.IsNullOrWhiteSpace(item.Url)?null:AionNewsSource.SafeUrl(item.Url,sources.OfficialWeb,false);
            if(System.String.IsNullOrWhiteSpace(item.Id)||System.String.IsNullOrWhiteSpace(item.Title)||link==null||!keys.Add(item.Key))continue;
            item.Url=link.AbsoluteUri;
            Uri picture=System.String.IsNullOrEmpty(item.Image)?null:AionNewsSource.SafeUrl(item.Image,sources.OfficialWeb,true);item.Image=picture==null?null:picture.AbsoluteUri;
            output.Add(item);
        }
        return output;
    }
    internal static string Body(string json){
        var root=new JavaScriptSerializer{MaxJsonLength=2097152}.DeserializeObject(json);
        object article=Get(root,"article"),content=Get(article,"content");
        string html=String(content,"content");if(article==null||html.Length==0)throw new FormatException("Article content unavailable");return html;
    }
}
