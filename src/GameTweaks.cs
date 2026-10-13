using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

// Independent implementation with our own generated one-frame black video.
// No downloaded scripts or files from another mod are used.
internal sealed class IntroTweaks {
    const int MaxVideoBytes=32*1024*1024;
    internal const string ReplacementHash="afbf665659bda86a01e139cf872f8e007591935ae8805eeac0ff9b0d9908b307";
    // Read-only migration path for the unsuccessful rename prototype.
    internal const string DisabledSuffix=".aion2helper-disabled";
    readonly string preferences,backupBase,diagnosticPath;readonly Func<bool> gameRunning;string recordDiagnostic="";
    internal string Diagnostics="";
    internal string GameFolder="";
    internal sealed class Preferences {public string GameFolder="";}
    internal sealed class Manifest {public int Version=3;public string Mode="blank-v1",GameFolder,OriginalHash,ReplacementHash;}
    internal sealed class State {internal bool Active,CanApply,CanRestore,Running;internal string Message;}
    internal IntroTweaks(string root,string backups=null,Func<bool> running=null){
        preferences=Path.Combine(root,"game-tweaks.json");diagnosticPath=Path.Combine(root,"intro-status.txt");backupBase=backups??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Aion2Helper","IntroBackups");gameRunning=running??Running;
        try{if(File.Exists(preferences)&&new FileInfo(preferences).Length<32768){var p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(preferences));if(p!=null)GameFolder=p.GameFolder??"";}}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }
    internal static bool Running(){var processes=Process.GetProcessesByName("AION2");try{return processes.Length!=0;}finally{foreach(var p in processes)p.Dispose();}}
    internal static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static string Canonical(string path){return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);}
    static void NoLinks(string path){for(string part=Path.GetFullPath(path);!string.IsNullOrEmpty(part);part=Path.GetDirectoryName(part)){if((Directory.Exists(part)||File.Exists(part))&&(File.GetAttributes(part)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked folders or files are not supported.");}}
    string Target(){if(string.IsNullOrWhiteSpace(GameFolder))throw new InvalidOperationException("Select the game folder first.");string root=Canonical(GameFolder);NoLinks(root);
        string exe=Path.Combine(root,"Aion2","Binaries","Win64","AION2.exe"),movie=Path.Combine(root,"Aion2","Content","Movies","CI_Type_A.bk2");NoLinks(exe);NoLinks(movie);NoLinks(movie+DisabledSuffix);
        if(!File.Exists(exe))throw new InvalidOperationException("This folder does not contain Aion2/Binaries/Win64/AION2.exe.");if(!Directory.Exists(Path.GetDirectoryName(movie)))throw new InvalidOperationException("The game Movies folder is missing.");return movie;
    }
    string BackupDirectory(){string root=Canonical(GameFolder),basePath=Canonical(backupBase);if(basePath.Equals(root,StringComparison.OrdinalIgnoreCase)||basePath.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Backups must be outside the game folder.");NoLinks(basePath);return Path.Combine(basePath,Hash(Encoding.UTF8.GetBytes(root.ToUpperInvariant())));}
    static byte[] ReadVideo(string path){NoLinks(path);byte[] bytes;using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){long length=stream.Length;if(length<76||length>MaxVideoBytes)throw new InvalidOperationException("The intro video has an unsupported size.");bytes=new byte[(int)length];int n=0,read;while(n<bytes.Length&&(read=stream.Read(bytes,n,bytes.Length-n))>0)n+=read;if(n!=bytes.Length)throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");}
        if(bytes[0]!='K'||bytes[1]!='B'||bytes[2]!='2')throw new InvalidOperationException("The selected file is not a supported Bink 2 intro video.");return bytes;
    }
    internal static byte[] Replacement(){using(var stream=typeof(IntroTweaks).Assembly.GetManifestResourceStream("OwnBlankIntro.bk2")){
        if(stream==null||stream.Length!=76)throw new InvalidOperationException("The helper's own startup video failed verification. No game file was changed.");var bytes=new byte[76];int n=0,read;while(n<bytes.Length&&(read=stream.Read(bytes,n,bytes.Length-n))>0)n+=read;
        if(n!=bytes.Length||Hash(bytes)!=ReplacementHash)throw new InvalidOperationException("The helper's own startup video failed verification. No game file was changed.");return bytes;}}
    internal void ChooseFolder(string folder){string before=GameFolder;try{GameFolder=Canonical(folder);Target();Save();}catch{GameFolder=before;throw;}}
    void Save(){Atomic(preferences,Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new Preferences{GameFolder=GameFolder})));}
    internal static string FindGameFolder(){return GameInstallation.Find();}
    Manifest ReadManifest(){string file=Path.Combine(BackupDirectory(),"active.json");recordDiagnostic="Record: "+file;NoLinks(file);if(!File.Exists(file)){recordDiagnostic+="\r\nRecord missing";return null;}if(new FileInfo(file).Length>32768)throw new InvalidOperationException("The saved intro backup record is invalid.");Manifest m;
        try{m=new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(file));}catch(Exception error){recordDiagnostic+="\r\nRead error: "+error;throw new InvalidOperationException("The intro backup record could not be read. Copy intro diagnostics to report this problem.",error);}
        if(m==null)throw new InvalidOperationException("The saved intro backup record is invalid.");
        recordDiagnostic+="\r\nRecord version: "+m.Version+"; mode: "+m.Mode+"\r\nRecord folder: "+m.GameFolder+"\r\nOriginal hash: "+m.OriginalHash+"\r\nReplacement hash: "+m.ReplacementHash;
        if(!((m.Version==2&&m.Mode=="rename-v1")||(m.Version==3&&m.Mode=="blank-v1")))throw new InvalidOperationException("The intro backup belongs to an unsupported format.");
        if(m.Version==3&&m.ReplacementHash!=ReplacementHash)throw new InvalidOperationException("The intro backup uses a different replacement video.");
        if(!string.Equals(m.GameFolder,Canonical(GameFolder),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The intro backup belongs to a different game folder.");
        if(!ValidHash(m.OriginalHash)||m.OriginalHash==ReplacementHash)throw new InvalidOperationException("The intro backup contains an invalid original hash.");return m;
    }
    static bool ValidHash(string value){if(value==null||value.Length!=64)return false;foreach(char c in value)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;return true;}
    byte[] Backup(Manifest m){string path=Path.Combine(BackupDirectory(),m.OriginalHash,"original.bk2");if(!File.Exists(path))throw new InvalidOperationException("The original intro backup is missing. Repair the game files to recover it.");byte[] bytes=ReadVideo(path);if(Hash(bytes)!=m.OriginalHash)throw new InvalidOperationException("The original intro backup failed verification. No game file was changed.");return bytes;}
    internal State Inspect(){var s=new State();recordDiagnostic="";try{
        if(string.IsNullOrEmpty(GameFolder)){s.Message="Select the game folder to check its intro.";return Report(s);}
        string target=Target(),retained=target+DisabledSuffix;s.Running=gameRunning();
        if(File.Exists(target)){byte[] bytes=ReadVideo(target);if(Hash(bytes)==ReplacementHash){Manifest m=ReadManifest();if(m==null||m.Version!=3)throw new InvalidOperationException("The saved intro backup record is invalid.");Backup(m);s.Active=true;s.CanRestore=!s.Running;s.Message="Intro skipping is active. A verified backup is available.";}
            else{if(bytes.Length<128)throw new InvalidOperationException("The intro video has an unsupported size.");s.Message="The intro video is available. Skipping is disabled.";s.CanApply=!s.Running;}}
        else{Manifest m=ReadManifest();if(m==null)throw new InvalidOperationException("The intro video is missing and this helper has no verified backup.");Backup(m);
            if(File.Exists(retained)&&Hash(ReadVideo(retained))!=m.OriginalHash)throw new InvalidOperationException("The retained intro changed. Its current file will be preserved.");
            s.Active=true;s.CanRestore=!s.Running;s.Message=m.Version==2?"The previous rename method is active. Restore the original before applying the new method.":"The startup video is missing. Restore the original from its verified backup.";}
        if(s.Running)s.Message="Close Aion 2 before applying or restoring this tweak.";
    }catch(Exception e){s.Message=e.Message;s.CanApply=s.CanRestore=false;recordDiagnostic+="\r\nInspect error: "+e;}return Report(s);}
    State Report(State s){
        Diagnostics="Aion 2 Helper "+typeof(IntroTweaks).Assembly.GetName().Version+"\r\nUser: "+Environment.UserName+"\r\nGame folder: "+GameFolder+"\r\nBackup base: "+backupBase+"\r\nStatus: "+s.Message+"\r\nActive: "+s.Active+"; can restore: "+s.CanRestore+"; game running: "+s.Running+"\r\n"+recordDiagnostic;
        try{File.WriteAllText(diagnosticPath,Diagnostics,Encoding.UTF8);}catch(IOException){}catch(UnauthorizedAccessException){}return s;}
    void RequireClosed(){if(gameRunning())throw new InvalidOperationException("Close Aion 2 before applying or restoring this tweak.");}
    internal void Apply(){RequireClosed();string target=Target(),retained=target+DisabledSuffix,directory=BackupDirectory();using(var mutex=new Mutex(false,"Local\\Aion2Helper-Intro-"+Path.GetFileName(directory))){bool owned=false;try{
        try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new InvalidOperationException("Another intro operation is in progress.");
        if(!File.Exists(target))throw new InvalidOperationException("Restore the original intro before applying the new method.");
        byte[] original=ReadVideo(target);string originalHash=Hash(original);Manifest previous=ReadManifest();
        if(originalHash==ReplacementHash){if(previous==null||previous.Version!=3)throw new InvalidOperationException("The saved intro backup record is invalid.");Backup(previous);return;}
        if(original.Length<128)throw new InvalidOperationException("The intro video has an unsupported size.");
        if(File.Exists(retained))throw new InvalidOperationException("Restore the original intro before applying the new method.");
        byte[] replacement=Replacement();
        string backupPath=Path.Combine(directory,originalHash,"original.bk2");NoLinks(backupPath);Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
        if(File.Exists(backupPath)){if(Hash(ReadVideo(backupPath))!=originalHash)throw new InvalidOperationException("The original intro backup failed verification. No game file was changed.");}else Atomic(backupPath,original);
        if(Hash(ReadVideo(backupPath))!=originalHash)throw new InvalidOperationException("The original intro backup failed verification. No game file was changed.");
        RequireClosed();if(Hash(ReadVideo(target))!=originalHash)throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");
        var manifest=new Manifest{GameFolder=Canonical(GameFolder),OriginalHash=originalHash,ReplacementHash=ReplacementHash};Atomic(Path.Combine(directory,"active.json"),Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(manifest)));
        RequireClosed();if(Hash(ReadVideo(target))!=originalHash)throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");Atomic(target,replacement);
        if(Hash(ReadVideo(target))!=ReplacementHash)throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");
    }finally{if(owned)mutex.ReleaseMutex();}}}
    internal void Restore(){RequireClosed();string target=Target(),retained=target+DisabledSuffix,directory=BackupDirectory();using(var mutex=new Mutex(false,"Local\\Aion2Helper-Intro-"+Path.GetFileName(directory))){bool owned=false;try{
        try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new InvalidOperationException("Another intro operation is in progress.");
        Manifest m=ReadManifest();if(m==null)throw new InvalidOperationException("The original intro backup is missing. Repair the game files to recover it.");
        if(File.Exists(target)){string current=Hash(ReadVideo(target));if(current==m.OriginalHash)return;if(m.Version!=3||current!=ReplacementHash)throw new InvalidOperationException("The game intro changed after an update or another mod. Its current file will be preserved.");
            byte[] original=Backup(m);RequireClosed();if(Hash(ReadVideo(target))!=ReplacementHash)throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");Atomic(target,original);return;}
        byte[] missingOriginal=Backup(m);RequireClosed();if(File.Exists(target))throw new InvalidOperationException("The game video changed during the operation. Refresh and try again.");
        if(File.Exists(retained)){if(m.Version!=2||Hash(ReadVideo(retained))!=m.OriginalHash)throw new InvalidOperationException("The retained intro changed. Its current file will be preserved.");File.Move(retained,target);}
        else Atomic(target,missingOriginal,false);
    }finally{if(owned)mutex.ReleaseMutex();}}}
    static void Atomic(string path,byte[] data,bool overwrite=true){NoLinks(path);Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=Path.Combine(Path.GetDirectoryName(path),"."+Guid.NewGuid().ToString("N").Substring(0,16)+".tmp");try{
        using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(data,0,data.Length);stream.Flush(true);}if(overwrite&&File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
    }finally{if(File.Exists(temp))File.Delete(temp);}}
}
