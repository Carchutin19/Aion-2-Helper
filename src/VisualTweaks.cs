using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

// Only four renderer overrides are owned here. No polling, console injection or read-only flags.
internal sealed class VisualTweaks {
    internal static readonly string[] Keys={"r.DepthOfFieldQuality","r.SceneColorFringeQuality","r.FilmGrain","r.MotionBlurQuality"};
    internal const string Section="[/Script/Engine.RendererSettings]";
    readonly string preferences,backupBase;readonly Func<bool> running;
    internal string ConfigPath="";
    internal sealed class Preferences {public string ConfigPath="";}
    internal sealed class Record {public int Version=1;public string Path,BeforeHash,AfterHash,ManagedHash;}
    internal sealed class State {internal bool[] Enabled={true,true,true,true};internal byte[] Snapshot;internal bool Found,CanApply,CanRestore;internal string Message;}
    internal VisualTweaks(string root,string backups=null,Func<bool> gameRunning=null){
        preferences=Path.Combine(root,"visual-tweaks.json");backupBase=backups??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Aion2Helper","VisualBackups");running=gameRunning??IntroTweaks.Running;
        try{if(File.Exists(preferences)&&new FileInfo(preferences).Length<32768){var p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(preferences));if(p!=null)ConfigPath=p.ConfigPath??"";}}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }
    internal static void NoLinks(string path){for(string part=Path.GetFullPath(path);!string.IsNullOrEmpty(part);part=Path.GetDirectoryName(part))if((File.Exists(part)||Directory.Exists(part))&&(File.GetAttributes(part)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked folders or files are not supported.");}
    string Target(){if(string.IsNullOrEmpty(ConfigPath))throw new InvalidOperationException("Select Engine.ini first.");string p=Path.GetFullPath(ConfigPath);if(!string.Equals(Path.GetFileName(p),"Engine.ini",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose the game's Engine.ini file.");NoLinks(p);if(!File.Exists(p))throw new InvalidOperationException("Engine.ini was not found. Choose the file manually.");return p;}
    internal void ChooseFile(string path){string before=ConfigPath;try{ConfigPath=Path.GetFullPath(path);Read(Target());Atomic(preferences,Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new Preferences{ConfigPath=ConfigPath})));}catch{ConfigPath=before;throw;}}
    internal static string[] FindFiles(string root=null){
        root=root??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AION2");var found=new List<string>();var queue=new Queue<KeyValuePair<string,int>>();queue.Enqueue(new KeyValuePair<string,int>(root,0));int visited=0;
        while(queue.Count!=0&&visited++<256&&found.Count<32){var item=queue.Dequeue();try{if(!Directory.Exists(item.Key)||(File.GetAttributes(item.Key)&FileAttributes.ReparsePoint)!=0)continue;
            string file=Path.Combine(item.Key,"Engine.ini");if(File.Exists(file)&&(File.GetAttributes(file)&FileAttributes.ReparsePoint)==0&&new FileInfo(file).Length<=1024*1024)found.Add(Path.GetFullPath(file));
            if(item.Value<6)foreach(string sub in Directory.GetDirectories(item.Key)){if(queue.Count>=256)break;queue.Enqueue(new KeyValuePair<string,int>(sub,item.Value+1));}
        }catch(IOException){}catch(UnauthorizedAccessException){}}
        found.Sort(StringComparer.OrdinalIgnoreCase);return found.ToArray();
    }
    static byte[] Read(string path){NoLinks(path);using(var s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(s.Length>1024*1024)throw new InvalidOperationException("Engine.ini is too large to edit safely.");var b=new byte[(int)s.Length];int n=0,k;while(n<b.Length&&(k=s.Read(b,n,b.Length-n))>0)n+=k;if(n!=b.Length)throw new IOException("Engine.ini changed while reading.");return b;}}
    string BackupDirectory(string target){string b=Path.GetFullPath(backupBase);if(b.Equals(Path.GetDirectoryName(target),StringComparison.OrdinalIgnoreCase)||b.StartsWith(Path.GetDirectoryName(target)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Visual backups must stay outside the game's configuration folder.");NoLinks(b);return Path.Combine(b,IntroTweaks.Hash(Encoding.UTF8.GetBytes(target.ToUpperInvariant())));}
    static bool ValidHash(string s){return s!=null&&Regex.IsMatch(s,"\\A[a-f0-9]{64}\\z");}
    Record RecordFor(string target){string p=Path.Combine(BackupDirectory(target),"last-change.json");if(!File.Exists(p))return null;NoLinks(p);if(new FileInfo(p).Length>32768)throw new InvalidOperationException("The visual backup record is invalid.");Record r;try{r=new JavaScriptSerializer().Deserialize<Record>(File.ReadAllText(p));}catch{throw new InvalidOperationException("The visual backup record is invalid.");}if(r==null||r.Version!=1||!string.Equals(r.Path,target,StringComparison.OrdinalIgnoreCase)||!ValidHash(r.BeforeHash)||!ValidHash(r.AfterHash)||!ValidHash(r.ManagedHash))throw new InvalidOperationException("The visual backup record is invalid.");return r;}
    byte[] Backup(string target,Record r){string p=Path.Combine(BackupDirectory(target),r.BeforeHash+".ini");if(!File.Exists(p))throw new InvalidOperationException("The visual backup is missing or damaged.");var b=Read(p);if(IntroTweaks.Hash(b)!=r.BeforeHash)throw new InvalidOperationException("The visual backup is missing or damaged.");return b;}
    internal State Inspect(){var s=new State();try{string p=Target();byte[] b=Read(p);var ini=new Ini(b);s.Enabled=ini.Enabled();s.Snapshot=b;s.Found=true;bool open=running(),readOnly=(File.GetAttributes(p)&FileAttributes.ReadOnly)!=0;s.CanApply=!open&&!readOnly;s.Message=open?"Close Aion 2 to apply visual changes. They take effect on the next launch.":readOnly?"Engine.ini is read-only. Make it writable before applying visual changes.":"Ready. Enabled effects use game settings; disabled effects use a zero override.";
        var r=RecordFor(p);if(r!=null&&ini.ManagedHash()==r.ManagedHash){Backup(p,r);s.CanRestore=s.CanApply;}
    }catch(Exception e){s.Message=e.Message;s.CanApply=s.CanRestore=false;}return s;}
    void RequireWritable(string p){if(running())throw new InvalidOperationException("Close Aion 2 to apply visual changes. They take effect on the next launch.");NoLinks(p);if((File.GetAttributes(p)&FileAttributes.ReadOnly)!=0)throw new InvalidOperationException("Engine.ini is read-only. Make it writable before applying visual changes.");}
    internal void Apply(bool[] enabled,byte[] expected){if(enabled==null||enabled.Length!=4||expected==null)throw new ArgumentException("Four effect choices and a current snapshot are required.");Change(enabled,expected,false);}
    internal byte[] Snapshot(){return Read(Target());}
    internal void Restore(){Change(null,null,true);}
    void Change(bool[] enabled,byte[] expected,bool restore){string p=Target(),dir=BackupDirectory(p);using(var mutex=new Mutex(false,"Local\\Aion2Helper-Visual-"+Path.GetFileName(dir))){bool owned=false;try{
        try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new InvalidOperationException("Another visual change is in progress.");RequireWritable(p);var before=Read(p);var ini=new Ini(before);byte[] after;
        if(restore){var r=RecordFor(p);if(r==null)throw new InvalidOperationException("No visual change is available to restore.");var original=Backup(p,r);if(ini.ManagedHash()!=r.ManagedHash)throw new InvalidOperationException("Visual settings changed outside the helper. Refresh before making another change.");after=IntroTweaks.Hash(before)==r.AfterHash?original:ini.Restore(new Ini(original));}
        else{if(IntroTweaks.Hash(before)!=IntroTweaks.Hash(expected))throw new InvalidOperationException("Engine.ini changed. Refresh status and review your choices before applying.");after=ini.Apply(enabled);}
        if(IntroTweaks.Hash(before)==IntroTweaks.Hash(after))return;
        Directory.CreateDirectory(dir);string backup=Path.Combine(dir,IntroTweaks.Hash(before)+".ini");if(File.Exists(backup)){if(IntroTweaks.Hash(Read(backup))!=IntroTweaks.Hash(before))throw new InvalidOperationException("The visual backup is missing or damaged.");}else Atomic(backup,before);
        var record=new Record{Path=p,BeforeHash=IntroTweaks.Hash(before),AfterHash=IntroTweaks.Hash(after),ManagedHash=new Ini(after).ManagedHash()};
        string journal=Path.Combine(dir,"last-change.json");NoLinks(journal);byte[] previous=File.Exists(journal)?Read(journal):null;
        // Prepare the recoverable record first, but retain the previous undo if the commit is blocked.
        Atomic(journal,Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(record)));
        try{RequireWritable(p);if(IntroTweaks.Hash(Read(p))!=record.BeforeHash)throw new InvalidOperationException("Engine.ini changed. Refresh status and review your choices before applying.");Atomic(p,after);}
        catch{if(previous!=null)Atomic(journal,previous);else File.Delete(journal);throw;}
    }finally{if(owned)mutex.ReleaseMutex();}}}
    static void Atomic(string path,byte[] bytes){NoLinks(path);Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=Path.Combine(Path.GetDirectoryName(path),"."+Guid.NewGuid().ToString("N")+".tmp");try{using(var s=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){s.Write(bytes,0,bytes.Length);s.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}

    internal sealed class Ini {
        sealed class Line {internal string Text,End;internal Line(string t,string e){Text=t;End=e;}}
        readonly List<Line> lines=new List<Line>();readonly Encoding encoding;readonly byte[] preamble;readonly string newline;readonly bool trailing;
        internal Ini(byte[] bytes){int start=0;
            if(bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191){encoding=new UTF8Encoding(false,true);start=3;}
            else if(bytes.Length>=2&&bytes[0]==255&&bytes[1]==254){encoding=new UnicodeEncoding(false,false,true);start=2;}
            else if(bytes.Length>=2&&bytes[0]==254&&bytes[1]==255){encoding=new UnicodeEncoding(true,false,true);start=2;}
            else {encoding=new UTF8Encoding(false,true);try{encoding.GetString(bytes);}catch(DecoderFallbackException){encoding=Encoding.GetEncoding(1252,new EncoderExceptionFallback(),new DecoderExceptionFallback());}}
            preamble=new byte[start];Array.Copy(bytes,preamble,start);string text=encoding.GetString(bytes,start,bytes.Length-start);if(text.IndexOf('\0')>=0)throw new InvalidOperationException("Engine.ini has an unsupported encoding.");
            foreach(Match m in Regex.Matches(text,"([^\\r\\n]*)(\\r\\n|\\n|\\r|$)"))if(m.Length!=0)lines.Add(new Line(m.Groups[1].Value,m.Groups[2].Value));newline="\r\n";foreach(var l in lines)if(l.End.Length!=0){newline=l.End;break;}trailing=lines.Count>0&&lines[lines.Count-1].End.Length!=0;
        }
        static string Header(string text){text=text.Trim();int end=text.IndexOf(']');return text.StartsWith("[")&&end>0?text.Substring(0,end+1):null;}
        static int Key(string text){text=text.Trim();if(text.StartsWith(";")||text.StartsWith("#"))return -1;int e=text.IndexOf('=');if(e<0)return -1;string k=text.Substring(0,e).Trim();for(int i=0;i<Keys.Length;i++)if(k.Equals(Keys[i],StringComparison.OrdinalIgnoreCase))return i;return -1;}
        List<int> Indices(int key){var result=new List<int>();bool section=false;for(int i=0;i<lines.Count;i++){string h=Header(lines[i].Text);if(h!=null)section=h.Equals(Section,StringComparison.OrdinalIgnoreCase);else if(section&&Key(lines[i].Text)==key)result.Add(i);}return result;}
        internal bool[] Enabled(){var flags=new[]{true,true,true,true};for(int k=0;k<4;k++){var list=Indices(k);if(list.Count==0)continue;string text=lines[list[list.Count-1]].Text;string value=text.Substring(text.IndexOf('=')+1).Split(';')[0].Trim().Trim('"');double n;if(double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out n))flags[k]=n!=0;}return flags;}
        internal string ManagedHash(){var b=new StringBuilder();for(int k=0;k<4;k++){b.Append(k).Append(':');foreach(int i in Indices(k))b.Append(lines[i].Text).Append('\n');}return IntroTweaks.Hash(Encoding.UTF8.GetBytes(b.ToString()));}
        void Remove(int key){var list=Indices(key);for(int n=list.Count-1;n>=0;n--)lines.RemoveAt(list[n]);}
        void Add(string text){int insert=-1;bool section=false;for(int i=0;i<lines.Count;i++){string h=Header(lines[i].Text);if(h!=null){if(section)insert=i;section=h.Equals(Section,StringComparison.OrdinalIgnoreCase);}if(section)insert=i+1;}
            if(insert<0){if(lines.Count>0&&lines[lines.Count-1].End.Length==0)lines[lines.Count-1].End=newline;lines.Add(new Line(Section,newline));insert=lines.Count;}
            if(insert>0&&lines[insert-1].End.Length==0)lines[insert-1].End=newline;lines.Insert(insert,new Line(text,insert==lines.Count&&!trailing?"":newline));
        }
        internal byte[] Apply(bool[] enabled){bool[] before=Enabled();for(int k=0;k<4;k++){if(enabled[k]==before[k])continue;if(enabled[k])Remove(k);else{var list=Indices(k);if(list.Count==0)Add(Keys[k]+"=0");else{int first=list[0];string old=lines[first].Text;int comment=old.IndexOf(';',old.IndexOf('=')+1);lines[first].Text=old.Substring(0,old.IndexOf('=')+1)+"0"+(comment<0?"":" "+old.Substring(comment));for(int n=list.Count-1;n>0;n--)lines.RemoveAt(list[n]);}}}return Bytes();}
        internal byte[] Restore(Ini original){for(int k=0;k<4;k++){Remove(k);foreach(int i in original.Indices(k))Add(original.lines[i].Text);}return Bytes();}
        internal byte[] Bytes(){var text=new StringBuilder();foreach(var l in lines)text.Append(l.Text).Append(l.End);var b=encoding.GetBytes(text.ToString());var result=new byte[preamble.Length+b.Length];Array.Copy(preamble,result,preamble.Length);Array.Copy(b,0,result,preamble.Length,b.Length);return result;}
    }
}
