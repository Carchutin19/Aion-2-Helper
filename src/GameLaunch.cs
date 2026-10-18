using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

// Discovery happens only on request, outside the UI thread. No background polling.
internal sealed class GameLaunchService {
    internal sealed class Preferences {public string GameFolder="",LauncherPath="";}
    internal sealed class Target {internal string Folder="",Path="",Mode="",Message="";internal bool Ready {get{return Path.Length!=0;}}}
    readonly string preferences;readonly Func<string> findGame,findPurple;readonly Func<bool> running;readonly Action<ProcessStartInfo> start;
    Preferences selected;
    internal GameLaunchService(string root,Func<string> detect=null,Func<string> purple=null,Func<bool> isRunning=null,Action<ProcessStartInfo> launch=null){
        preferences=System.IO.Path.Combine(root,"game-launch.json");selected=Read(preferences)??Read(System.IO.Path.Combine(root,"game-tweaks.json"))??new Preferences();
        findGame=detect??GameInstallation.Find;findPurple=purple??GameInstallation.FindPurpleLauncher;running=isRunning??IntroTweaks.Running;start=launch??delegate(ProcessStartInfo info){using(var process=Process.Start(info)){if(process==null)throw new IOException("Launcher did not start.");}};
    }
    static Preferences Read(string path){try{if(!File.Exists(path)||new FileInfo(path).Length>32768)return null;var value=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(path));if(value!=null){value.GameFolder=value.GameFolder??"";value.LauncherPath=value.LauncherPath??"";}return value;}catch(Exception e){if(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException||e is System.Security.SecurityException||e is NotSupportedException)return null;throw;}}
    internal string ConfiguredFolder {get{return selected.GameFolder;}}
    internal static bool ValidLauncher(string path){try{if(string.IsNullOrWhiteSpace(path)||!System.IO.Path.IsPathRooted(path)||!File.Exists(path))return false;string ext=System.IO.Path.GetExtension(path);return ext.Equals(".exe",StringComparison.OrdinalIgnoreCase)||ext.Equals(".lnk",StringComparison.OrdinalIgnoreCase)||ext.Equals(".url",StringComparison.OrdinalIgnoreCase);}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}}
    void Save(Preferences next){string temp=preferences+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(next),new UTF8Encoding(false));if(File.Exists(preferences))File.Replace(temp,preferences,null);else File.Move(temp,preferences);selected=next;}finally{if(File.Exists(temp))File.Delete(temp);}}
    internal void ChooseFolder(string folder){if(!GameInstallation.IsValid(folder))throw new InvalidOperationException("This folder does not contain Aion2/Binaries/Win64/AION2.exe.");string full=System.IO.Path.GetFullPath(folder);Save(new Preferences{GameFolder=full,LauncherPath=string.Equals(full,selected.GameFolder,StringComparison.OrdinalIgnoreCase)?selected.LauncherPath:""});}
    internal void ChooseLauncher(string path){if(!ValidLauncher(path))throw new InvalidOperationException("Select an existing launcher or game shortcut (.exe, .lnk or .url).");Save(new Preferences{GameFolder=selected.GameFolder,LauncherPath=System.IO.Path.GetFullPath(path)});}
    internal void AutomaticLauncher(){Save(new Preferences{GameFolder=selected.GameFolder});}
    internal Target Detect(){
        if(selected.LauncherPath.Length!=0)return ValidLauncher(selected.LauncherPath)?new Target{Folder=GameInstallation.IsValid(selected.GameFolder)?selected.GameFolder:"",Path=selected.LauncherPath,Mode="manual",Message="Selected game launcher is ready."}:new Target{Folder=selected.GameFolder,Message="The selected launcher was not found. Choose it again."};
        string folder=GameInstallation.IsValid(selected.GameFolder)?selected.GameFolder:findGame();
        if(!GameInstallation.IsValid(folder))return new Target{Message="Aion 2 was not found. Select the game installation folder."};
        if(GameInstallation.IsSteamGame(folder))return new Target{Folder=folder,Path="steam://run/3393110",Mode="steam",Message="Aion 2 detected · Steam"};
        string purple=findPurple();if(ValidLauncher(purple))return new Target{Folder=folder,Path=purple,Mode="purple",Message="Aion 2 detected · PURPLE"};
        return new Target{Folder=folder,Message="Game found. Select its launcher or game shortcut."};
    }
    internal string Launch(Target target){
        if(running())return "Aion 2 is already running.";
        if(target==null||!target.Ready)return target==null?"Aion 2 was not found. Select the game installation folder.":target.Message;
        if(target.Mode=="steam"?!GameInstallation.IsSteamGame(target.Folder):!ValidLauncher(target.Path))throw new InvalidOperationException("Game files or launcher have moved. Detect the game again.");
        var info=new ProcessStartInfo(target.Path){UseShellExecute=true};if(target.Mode!="steam")info.WorkingDirectory=System.IO.Path.GetDirectoryName(target.Path);start(info);
        return target.Mode=="steam"?"Launch request sent to Steam.":target.Mode=="purple"?"PURPLE opened. Start Aion 2 from the launcher.":"Launch request sent to your selected launcher.";
    }
}
