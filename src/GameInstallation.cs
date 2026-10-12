using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// Bounded, read-only discovery. Called only when opening Game Tweaks or on request.
internal static class GameInstallation {
    const int MaxMetadataBytes=1024*1024;
    static readonly Regex Pair=new Regex("\"(?<key>(?:\\\\.|[^\"\\\\])*)\"\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",RegexOptions.CultureInvariant);
    internal static bool IsValid(string folder){try{
        if(string.IsNullOrWhiteSpace(folder)||!Path.IsPathRooted(folder))return false;string root=Path.GetFullPath(folder);
        string exe=Path.Combine(root,"Aion2","Binaries","Win64","AION2.exe"),movies=Path.Combine(root,"Aion2","Content","Movies");
        if(!File.Exists(exe)||!Directory.Exists(movies))return false;
        foreach(string path in new[]{exe,movies})for(string part=path;!string.IsNullOrEmpty(part);part=Path.GetDirectoryName(part))if((File.GetAttributes(part)&FileAttributes.ReparsePoint)!=0)return false;
        return true;
    }catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}catch(SecurityException){return false;}}
    internal static string Pick(IEnumerable<string> folders){foreach(string folder in folders)if(IsValid(folder))return Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);return "";}
    static string FromExe(string exe){try{string folder=Path.GetDirectoryName(exe);for(int n=0;n<3&&folder!=null;n++)folder=Path.GetDirectoryName(folder);return IsValid(folder)?folder:"";}catch(ArgumentException){return "";}}
    static string RegistryValue(RegistryKey hive,string path,string name){try{using(var key=hive.OpenSubKey(path))return key==null?"":Convert.ToString(key.GetValue(name));}catch(SecurityException){return "";}catch(UnauthorizedAccessException){return "";}catch(IOException){return "";}}
    static string RegisteredGame(){foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})foreach(string path in new[]{@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"})try{
        using(var key=hive.OpenSubKey(path)){if(key==null)continue;foreach(string name in key.GetSubKeyNames()){
            // Validate the actual layout, including entries with localized display names.
            string folder=RegistryValue(hive,path+"\\"+name,"InstallLocation");if(IsValid(folder))return Path.GetFullPath(folder);
        }}
    }catch(SecurityException){}catch(UnauthorizedAccessException){}catch(IOException){}return "";}
    static string ReadMetadata(string path){try{using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
        if(stream.Length>MaxMetadataBytes)return "";var bytes=new byte[(int)stream.Length];int n=0,read;while(n<bytes.Length&&(read=stream.Read(bytes,n,bytes.Length-n))>0)n+=read;return Encoding.UTF8.GetString(bytes,0,n);
    }}catch(IOException){return "";}catch(UnauthorizedAccessException){return "";}catch(ArgumentException){return "";}catch(NotSupportedException){return "";}catch(SecurityException){return "";}}
    static string Unquote(string value){return value.Replace("\\\\","\\").Replace("\\\"","\"");}
    internal static string[] Libraries(string text){var paths=new List<string>();if(text==null||text.Length>MaxMetadataBytes)return paths.ToArray();foreach(Match pair in Pair.Matches(text)){
        string key=Unquote(pair.Groups["key"].Value),value=Unquote(pair.Groups["value"].Value);int number;
        if(key.Equals("path",StringComparison.OrdinalIgnoreCase)||int.TryParse(key,out number))try{if(Path.IsPathRooted(value)){paths.Add(value);if(paths.Count>=64)break;}}catch(ArgumentException){}
    }return paths.ToArray();}
    static string InstallDir(string text){foreach(Match pair in Pair.Matches(text))if(pair.Groups["key"].Value.Equals("installdir",StringComparison.OrdinalIgnoreCase))return Unquote(pair.Groups["value"].Value);return "";}
    internal static string FindSteam(IEnumerable<string> steamRoots){var libraries=new List<string>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(string steam in steamRoots){if(string.IsNullOrWhiteSpace(steam))continue;try{
            string full=Path.GetFullPath(steam);if(seen.Add(full))libraries.Add(full);
            foreach(string metadata in new[]{Path.Combine(full,"steamapps","libraryfolders.vdf"),Path.Combine(full,"config","libraryfolders.vdf")})foreach(string folder in Libraries(ReadMetadata(metadata))){string library=Path.GetFullPath(folder);if(seen.Add(library))libraries.Add(library);}
        }catch(ArgumentException){}catch(NotSupportedException){}catch(IOException){}catch(UnauthorizedAccessException){} }
        foreach(string library in libraries){string apps=Path.Combine(library,"steamapps"),common=Path.Combine(apps,"common"),direct=Path.Combine(common,"AION2");if(IsValid(direct))return direct;
            try{int scanned=0;foreach(string manifest in Directory.EnumerateFiles(apps,"appmanifest_*.acf",SearchOption.TopDirectoryOnly)){
                if(++scanned>512)break;string directory=InstallDir(ReadMetadata(manifest));if(string.IsNullOrWhiteSpace(directory)||Path.IsPathRooted(directory)||directory.IndexOfAny(new[]{'\\','/'})>=0||directory=="."||directory=="..")continue;
                string candidate=Path.Combine(common,directory);if(IsValid(candidate))return candidate;
            }}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(SecurityException){}
        }return "";
    }
    internal static string Find(){var processes=Process.GetProcessesByName("AION2");try{foreach(var process in processes)try{string found=FromExe(process.MainModule.FileName);if(found.Length!=0)return found;}catch(Win32Exception){}catch(InvalidOperationException){}catch(NotSupportedException){}}finally{foreach(var process in processes)process.Dispose();}
        string registered=RegisteredGame();if(registered.Length!=0)return registered;
        string steam=FindSteam(new[]{RegistryValue(Registry.CurrentUser,@"SOFTWARE\Valve\Steam","SteamPath"),RegistryValue(Registry.LocalMachine,@"SOFTWARE\Valve\Steam","InstallPath"),RegistryValue(Registry.LocalMachine,@"SOFTWARE\WOW6432Node\Valve\Steam","InstallPath"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam")});if(steam.Length!=0)return steam;
        var defaults=new List<string>();foreach(string program in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)})foreach(string name in new[]{"AION2","AION2_TW","AION2_KR"})defaults.Add(Path.Combine(program,"NCSOFT",name));return Pick(defaults);
    }
}
