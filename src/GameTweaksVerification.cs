using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

internal static class GameTweaksVerification {
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Rejected(Action action,string message){bool failed=false;try{action();}catch(InvalidOperationException){failed=true;}Require(failed,message);}
    static void Discovery(string fixtures,string game){
        Require(GameInstallation.IsValid(game)&&GameInstallation.Pick(new[]{"",Path.Combine(fixtures,"missing"),game})==game,"Detection validates layout and skips stale paths");
        string steam=Path.Combine(fixtures,"Steam Root"),library=Path.Combine(fixtures,"Library with spaces"),apps=Path.Combine(library,"steamapps"),custom=Path.Combine(apps,"common","Localized Game"),exe=Path.Combine(custom,"Aion2","Binaries","Win64","AION2.exe"),movies=Path.Combine(custom,"Aion2","Content","Movies");
        Directory.CreateDirectory(Path.GetDirectoryName(exe));File.WriteAllText(exe,"fixture only");Require(!GameInstallation.IsValid(custom),"An executable without the Movies folder is not a valid installation");Directory.CreateDirectory(movies);Directory.CreateDirectory(Path.Combine(steam,"steamapps"));
        var json=new JavaScriptSerializer();string quoted=json.Serialize(library);
        File.WriteAllText(Path.Combine(steam,"steamapps","libraryfolders.vdf"),"\"libraryfolders\" { \"0\" { \"path\" "+quoted+" \"apps\" { \"3393110\" \"1\" } } }");
        string acf=Path.Combine(apps,"appmanifest_3393110.acf");File.WriteAllText(acf,"\"AppState\" { \"name\" \"Localized game title\" \"installdir\" \"Localized Game\" }");
        Require(GameInstallation.FindSteam(new[]{Path.Combine(fixtures,"missing steam"),steam})==custom,"Find game in an additional Steam library with a manifest-selected folder and spaces");
        string[] old=GameInstallation.Libraries("\"LibraryFolders\" { \"TimeNextStatsReport\" \"123\" \"1\" "+quoted+" }");Require(old.Length==1&&old[0]==library,"Legacy VDF escaped paths are read without treating metadata as directories");
        Require(GameInstallation.Libraries(new string(' ',1024*1024+1)).Length==0,"Oversized discovery metadata must be ignored");
        File.WriteAllText(acf,"\"AppState\" { \"installdir\" \"..\\..\\Game With Spaces\" }");Require(GameInstallation.FindSteam(new[]{steam})=="","Manifest traversal is rejected and a missing installation returns empty for manual fallback");
        string direct=Path.Combine(Path.GetDirectoryName(custom),"AION2");Directory.Move(custom,direct);Require(GameInstallation.FindSteam(new[]{steam})==direct,"Default Steam folder is detected even with a malformed manifest");
    }
    internal static void Run(string root){
        // Keep fixture backups short even when a release is extracted into a deep path.
        string fixtures=Path.Combine(Path.GetTempPath(),"A2HIntroTests",Guid.NewGuid().ToString("N").Substring(0,8)),game=Path.Combine(fixtures,"Game With Spaces"),movie=Path.Combine(game,"Aion2","Content","Movies","CI_Type_A.bk2"),retained=movie+IntroTweaks.DisabledSuffix,exe=Path.Combine(game,"Aion2","Binaries","Win64","AION2.exe"),backups=Path.Combine(fixtures,"backups");
        Directory.CreateDirectory(Path.GetDirectoryName(movie));Directory.CreateDirectory(Path.GetDirectoryName(exe));File.WriteAllText(exe,"fixture only: not an executable");
        byte[] original=Encoding.ASCII.GetBytes("KB2n"+new string('A',256)),updated=Encoding.ASCII.GetBytes("KB2n"+new string('B',512)),placeholder=IntroTweaks.Replacement();File.WriteAllBytes(movie,original);
        Require(IntroTweaks.Hash(placeholder)==IntroTweaks.ReplacementHash&&BitConverter.ToUInt32(placeholder,8)==1&&BitConverter.ToUInt32(placeholder,20)==64&&BitConverter.ToUInt32(placeholder,24)==64&&BitConverter.ToUInt32(placeholder,28)==60&&BitConverter.ToUInt32(placeholder,32)==1&&BitConverter.ToUInt32(placeholder,40)==0,"Own generated video: one 64x64 frame at 60 FPS without audio");
        bool running=false;var service=new IntroTweaks(fixtures,backups,delegate{return running;});service.ChooseFolder(game);
        Discovery(fixtures,game);
        Require(!service.Inspect().Active&&service.Inspect().CanApply,"Own function defaults off and needs no external mod");
        running=true;Rejected(service.Apply,"Running game must prevent modification");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original),"Blocked apply preserves original");running=false;
        File.WriteAllBytes(retained,updated);Rejected(service.Apply,"Do not overwrite a retained filename from another operation");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original)&&IntroTweaks.Hash(File.ReadAllBytes(retained))==IntroTweaks.Hash(updated),"Collision preserves both files");File.Delete(retained);
        service.Apply();Require(File.Exists(movie)&&!File.Exists(retained)&&IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.ReplacementHash&&service.Inspect().CanRestore,"The expected game file must remain present after applying");
        service.Apply();string[] stored=Directory.GetFiles(backups,"original.bk2",SearchOption.AllDirectories);Require(stored.Length==1&&IntroTweaks.Hash(File.ReadAllBytes(stored[0]))==IntroTweaks.Hash(original),"Repeated apply must not back up the placeholder as the original");
        var reopened=new IntroTweaks(fixtures,backups,delegate{return running;});Require(reopened.GameFolder==game&&reopened.Inspect().Active,"Choices and active state survive reopening");
        byte[] backup=File.ReadAllBytes(stored[0]);File.WriteAllBytes(stored[0],updated);Rejected(reopened.Restore,"Corrupt backup is rejected");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.ReplacementHash,"Failed restore preserves current video");File.WriteAllBytes(stored[0],backup);
        File.WriteAllBytes(movie,updated);Rejected(reopened.Restore,"Do not replace a video changed by updates or other mods");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(updated),"Unknown video remains unchanged");File.WriteAllBytes(movie,placeholder);
        reopened.Restore();Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original)&&File.Exists(stored[0]),"Restoration puts back exact original and keeps backup");reopened.Restore();
        reopened.Apply();File.Delete(movie);Require(reopened.Inspect().CanRestore,"Missing video can be recovered from verified backup");reopened.Restore();Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original),"Missing-file recovery restores exact original");
        // Diagnostics identify each rejection without weakening restoration checks or changing files.
        reopened.Apply();string diagnosticRecord=Directory.GetFiles(backups,"active.json",SearchOption.AllDirectories)[0];string validRecord=File.ReadAllText(diagnosticRecord);var serializer=new JavaScriptSerializer();
        foreach(string reason in new[]{"format","replacement","folder","hash","json"}){
            var invalid=serializer.Deserialize<IntroTweaks.Manifest>(validRecord);if(reason=="format")invalid.Mode="unknown";if(reason=="replacement")invalid.ReplacementHash=new string('a',64);if(reason=="folder")invalid.GameFolder=game+"-other";if(reason=="hash")invalid.OriginalHash="bad";
            File.WriteAllText(diagnosticRecord,reason=="json"?"{bad-json":serializer.Serialize(invalid));var rejected=reopened.Inspect();Require(!rejected.CanApply&&!rejected.CanRestore&&!rejected.Active,"Invalid intro records never enable game modification");
            string expected=reason=="format"?"unsupported format":reason=="replacement"?"different replacement":reason=="folder"?"different game folder":reason=="hash"?"invalid original hash":"could not be read";
            Require(rejected.Message.Contains(expected)&&reopened.Diagnostics.Contains(rejected.Message)&&reopened.Diagnostics.Contains(diagnosticRecord),"Specific intro rejection and copyable diagnostic");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.ReplacementHash,"Inspection never changes the installed video");
        }
        File.WriteAllText(diagnosticRecord,validRecord);Require(reopened.Inspect().Active&&reopened.Diagnostics.Contains("Record version: 3"),"Valid record recovers after refresh");reopened.Restore();
        // An old rename installation is restorable, but can never be reapplied.
        string record=Directory.GetFiles(backups,"active.json",SearchOption.AllDirectories)[0];File.WriteAllText(record,new JavaScriptSerializer().Serialize(new IntroTweaks.Manifest{Version=2,Mode="rename-v1",GameFolder=game,OriginalHash=IntroTweaks.Hash(original)}));File.Move(movie,retained);
        Require(reopened.Inspect().CanRestore&&reopened.Inspect().Message.IndexOf("Restore the original",StringComparison.Ordinal)>=0,"Legacy rename must offer restoration before the new method");Rejected(reopened.Apply,"Never repeat the superseded rename method");reopened.Restore();Require(File.Exists(movie)&&!File.Exists(retained)&&IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original),"Legacy installation restored byte-exactly");
        reopened.Apply();File.WriteAllBytes(movie,updated);Rejected(reopened.Restore,"Preserve files recreated by game updates");reopened.Apply();reopened.Restore();Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(updated)&&Directory.GetFiles(backups,"original.bk2",SearchOption.AllDirectories).Length==2,"Reapplying versions backups and restores latest installed video");
        Require(Directory.GetFiles(fixtures,"*.tmp",SearchOption.AllDirectories).Length==0,"Atomic writes leave no temporary files");
        var unsafeBackups=new IntroTweaks(fixtures,game,delegate{return false;});Rejected(unsafeBackups.Apply,"Backups must stay outside game folder");
        int closedChecks=0;var lateGameStart=new IntroTweaks(fixtures,backups,delegate{return ++closedChecks>=2;});Rejected(lateGameStart.Apply,"Starting game during operation prevents replacement");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(updated),"Late game start preserves installed original");
        int changeChecks=0;var lateFileChange=new IntroTweaks(fixtures,backups,delegate{if(++changeChecks==2)File.WriteAllBytes(movie,original);return false;});Rejected(lateFileChange.Apply,"Changed file must not be overwritten using a stale backup");Require(IntroTweaks.Hash(File.ReadAllBytes(movie))==IntroTweaks.Hash(original),"Concurrent file change is preserved");
        Directory.CreateDirectory(Path.Combine(root,"designs"));using(var owner=new EnergyOverlay(root,true)){var settings=new HelperSettings(owner,null,reopened);try{settings.VerifyGameTweaks(root);}finally{settings.Close();}}
        File.WriteAllText(Path.Combine(root,"game-tweaks-test.txt"),"PASS: validated layout/stale paths; modern and legacy Steam libraries; escaped paths/spaces; manifest-selected directories; oversized metadata and path traversal rejection; manual fallback; own embedded one-frame video metadata/hash; default off/no external mod; expected filename always present; game-running/late-start rejection; collision protection; idempotence; persistence; backup corruption rejection; exact restore; missing-file recovery; legacy rename migration; update preservation/versioning; concurrent file-change rejection; temporary cleanup; external backup requirement; integrated EN/ES menu rendered. Only private fixtures changed. This fixture test does not validate actual Aion startup or mouse input.");
    }
}
