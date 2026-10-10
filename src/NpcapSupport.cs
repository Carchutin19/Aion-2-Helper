using System;
using System.IO;
using System.Runtime.InteropServices;

// The installer remains on Npcap's official site. Only Energy Bar needs it.
// File/native checks run on capture maintenance, not on the rendering timer.
internal sealed class NpcapSupport {
    internal const string DownloadUrl="https://npcap.com/#download";
    internal const string MissingMessage="Npcap is required for Energy Bar.";
    internal const string LoadMessage="Npcap could not be loaded. Reinstall Npcap for Energy Bar.";
    internal static readonly NpcapSupport Current=new NpcapSupport(LibraryPresent,LoadLibrary);
    readonly Func<bool> present;readonly Action load;readonly object gate=new object();
    volatile int state; // 0 unchecked, 1 available, 2 missing, 3 load failure
    internal NpcapSupport(Func<bool> installed,Action loader){present=installed;load=loader;state=present()?0:2;}
    internal bool Required {get{return state>=2;}}
    internal string Message {get{return state==3?LoadMessage:MissingMessage;}}
    static bool LibraryPresent(){string system=Environment.GetFolderPath(Environment.SpecialFolder.System);return File.Exists(Path.Combine(system,"Npcap","wpcap.dll"))||File.Exists(Path.Combine(system,"wpcap.dll"));}
    static void LoadLibrary(){Native.SetDllDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"Npcap"));if(Native.pcap_lib_version()==IntPtr.Zero)throw new DllNotFoundException("Npcap version entry point returned no library information.");}
    internal void EnsureAvailable(){if(state==1)return;lock(gate){if(state==1)return;
        if(!present()){state=2;throw new InvalidOperationException(MissingMessage);}
        try{load();state=1;}
        catch(DllNotFoundException ex){Fail(ex);}
        catch(BadImageFormatException ex){Fail(ex);}
        catch(EntryPointNotFoundException ex){Fail(ex);}
    }}
    void Fail(Exception ex){state=3;throw new InvalidOperationException(LoadMessage,ex);}
    internal static void Verify(){bool installed=false;int loads=0;var missing=new NpcapSupport(delegate{return installed;},delegate{loads++;});
        if(!missing.Required||missing.Message!=MissingMessage)throw new Exception("Missing Npcap must be visible before game startup");
        try{missing.EnsureAvailable();throw new Exception("Missing Npcap was accepted");}catch(InvalidOperationException ex){if(ex.Message!=MissingMessage||loads!=0)throw new Exception("Missing library must produce guidance without attempting capture");}
        installed=true;missing.EnsureAvailable();missing.EnsureAvailable();if(missing.Required||loads!=1)throw new Exception("Installing Npcap must clear the warning and cache successful validation");
        foreach(Exception failure in new Exception[]{new DllNotFoundException(),new BadImageFormatException(),new EntryPointNotFoundException()}){
            var broken=new NpcapSupport(delegate{return true;},delegate{throw failure;});
            try{broken.EnsureAvailable();throw new Exception("Broken Npcap was accepted");}catch(InvalidOperationException ex){if(ex.Message!=LoadMessage||ex.InnerException!=failure||!broken.Required)throw new Exception("Native library failures must show reinstall guidance");}
        }
        var unrelated=new NpcapSupport(delegate{return true;},delegate{throw new IOException("unrelated");});bool propagated=false;try{unrelated.EnsureAvailable();}catch(IOException){propagated=true;}if(!propagated)throw new Exception("Unrelated capture errors must not be mislabeled as missing Npcap");
        if(new Uri(DownloadUrl).Host!="npcap.com")throw new Exception("Dependency download must use the official site");
    }
}
