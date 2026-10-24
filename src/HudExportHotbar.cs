using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// Standalone HUD exports, not DeviceSetting_V2. Format facts are credited in
// assets/skill-slots/README.md. Only three Hide values change in the JSON text.
internal static class HudExportHotbar {
 internal const string Prefix="Helper_HiddenHotbar(";
 const int Limit=1024*1024;
 static readonly byte[] Mask={0x25,0xa8,0x7e,0x91};
 static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 internal sealed class Result {internal string File;internal bool Reused;}
 sealed class Node {internal int Start,End;internal Dictionary<string,Node> Properties;}
 sealed class Document {internal string Text;internal Node Root;internal Dictionary<string,object> Value;}
 static InvalidDataException Unsupported(){return new InvalidDataException("Choose a HUD file exported from Edit HUD > Export in AION 2. The original file was not changed.");}
 static byte[] Crypt(byte[] bytes){return bytes.Select((b,n)=>(byte)(b^Mask[n%4])).ToArray();}
 // Span scanner preserves formatting and detects duplicate keys, including
 // escaped duplicates, before the ordinary JSON decoder can collapse them.
 sealed class Scanner {
  readonly string text;int at,count;
  internal Scanner(string text){this.text=text;}
  void Space(){while(at<text.Length&&char.IsWhiteSpace(text[at]))at++;}
  string String(){int start=at;if(at>=text.Length||text[at++]!='"')throw Unsupported();while(at<text.Length){char c=text[at++];if(c=='\\'){if(at>=text.Length)throw Unsupported();at++;}else if(c=='"')return NativeHudFile.Json().Deserialize<string>(text.Substring(start,at-start));}throw Unsupported();}
  Node Value(int depth){Space();if(depth>64||++count>50000||at>=text.Length)throw Unsupported();var node=new Node{Start=at};char c=text[at];
   if(c=='{'){at++;node.Properties=new Dictionary<string,Node>(StringComparer.Ordinal);Space();if(at<text.Length&&text[at]=='}')at++;else while(true){Space();string key=String();Space();if(at>=text.Length||text[at++]!=':'||node.Properties.ContainsKey(key))throw Unsupported();node.Properties.Add(key,Value(depth+1));Space();if(at>=text.Length)throw Unsupported();char next=text[at++];if(next=='}')break;if(next!=',')throw Unsupported();}}
   else if(c=='['){at++;Space();if(at<text.Length&&text[at]==']')at++;else while(true){Value(depth+1);Space();if(at>=text.Length)throw Unsupported();char next=text[at++];if(next==']')break;if(next!=',')throw Unsupported();}}
   else if(c=='"')String();else {while(at<text.Length&&!char.IsWhiteSpace(text[at])&&text[at]!=','&&text[at]!=']'&&text[at]!='}')at++;if(at==node.Start)throw Unsupported();}
   node.End=at;return node;
  }
  internal Node Read(){var value=Value(0);Space();if(at!=text.Length)throw Unsupported();return value;}
 }
 static Document Read(byte[] input){
  try{if(input==null||input.Length<8||input.Length>Limit)throw Unsupported();string encoded=Utf8.GetString(input).TrimStart('\ufeff');var bytes=Crypt(Convert.FromBase64String(encoded));if(bytes.Length<6||BitConverter.ToInt32(bytes,0)!=bytes.Length-4)throw Unsupported();string text=Utf8.GetString(bytes,4,bytes.Length-4);if(text.EndsWith("\0",StringComparison.Ordinal))text=text.Substring(0,text.Length-1);var root=new Scanner(text).Read();var value=NativeHudFile.Json().Deserialize<Dictionary<string,object>>(text);if(root.Properties==null||value==null||!root.Properties.ContainsKey("Edit")||root.Properties["Edit"].Properties==null)throw Unsupported();return new Document{Text=text,Root=root,Value=value};}
  catch(Exception ex){if(ex is FormatException||ex is ArgumentException||ex is InvalidOperationException)throw Unsupported();throw;}
 }
 static byte[] Encode(string text,bool nul){byte[] payload=Utf8.GetBytes(text+(nul?"\0":""));var clear=new byte[payload.Length+4];Buffer.BlockCopy(BitConverter.GetBytes(payload.Length),0,clear,0,4);Buffer.BlockCopy(payload,0,clear,4,payload.Length);return Encoding.ASCII.GetBytes(Convert.ToBase64String(Crypt(clear)));}
 internal static byte[] Build(byte[] original){var doc=Read(original);var edit=doc.Root.Properties["Edit"];var expected=NativeHudFile.Json().Deserialize<Dictionary<string,object>>(doc.Text);var rows=expected["Edit"] as Dictionary<string,object>;if(rows==null)throw Unsupported();var spans=new List<Node>();
  foreach(string tag in NativeHudFile.Tags){Node row,flag;if(!edit.Properties.TryGetValue(tag,out row)||row.Properties==null||!row.Properties.TryGetValue("Hide",out flag))throw Unsupported();object raw;var value=rows.ContainsKey(tag)?rows[tag] as Dictionary<string,object>:null;if(value==null||!value.TryGetValue("Tag",out raw)||!(raw is string)||(string)raw!=tag||!value.TryGetValue("Hide",out raw)||!(raw is bool))throw Unsupported();value["Hide"]=true;string token=doc.Text.Substring(flag.Start,flag.End-flag.Start);if(token!="true"&&token!="false")throw Unsupported();if(token=="false")spans.Add(flag);}
  string text=doc.Text;foreach(var span in spans.OrderByDescending(s=>s.Start))text=text.Remove(span.Start,span.End-span.Start).Insert(span.Start,"true");if(NativeHudFile.Canonical(NativeHudFile.Json().DeserializeObject(text))!=NativeHudFile.Canonical(expected))throw Unsupported();
  var decoded=Crypt(Convert.FromBase64String(Utf8.GetString(original).TrimStart('\ufeff')));bool nul=decoded[decoded.Length-1]==0;var result=Encode(text,nul);if(NativeHudFile.Canonical(Read(result).Value)!=NativeHudFile.Canonical(expected))throw Unsupported();return result;
 }
 internal static Result Create(string source){
  source=Path.GetFullPath(source);VisualTweaks.NoLinks(source);if(string.Equals(Path.GetFileName(source),"DeviceSetting_V2.dat",StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetExtension(source),".dat",StringComparison.OrdinalIgnoreCase))throw Unsupported();if(new FileInfo(source).Length>Limit)throw Unsupported();byte[] before=File.ReadAllBytes(source),output=Build(before);string stem=Path.GetFileNameWithoutExtension(source);if(!stem.StartsWith(Prefix,StringComparison.Ordinal))stem=Prefix+stem+")";string folder=Path.GetDirectoryName(source);
  for(int index=1;index<=100;index++){string target=Path.Combine(folder,stem+(index==1?"":" ("+index+")")+".dat");if(string.Equals(target,source,StringComparison.OrdinalIgnoreCase))continue;VisualTweaks.NoLinks(target);if(!File.ReadAllBytes(source).SequenceEqual(before))throw new IOException("The HUD export changed. Export it again and retry.");if(File.Exists(target)){if(new FileInfo(target).Length<=Limit&&File.ReadAllBytes(target).SequenceEqual(output))return new Result{File=target,Reused=true};continue;}
   bool created=false;try{using(var stream=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None)){created=true;stream.Write(output,0,output.Length);stream.Flush(true);}}
   catch(IOException){if(!created&&File.Exists(target))continue;throw;}
   if(!File.ReadAllBytes(target).SequenceEqual(output)||!File.ReadAllBytes(source).SequenceEqual(before))throw new IOException("The HUD copy could not be verified. Export the original HUD again and retry.");return new Result{File=target};
  }throw new IOException("Too many HUD copies with this name. Choose a different export name.");
 }
}
