using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// Bounded metadata for the community HUD investigation. No full HUD contents,
// coordinates, packet payloads, game keys or screenshots are retained here.
internal sealed class NativeHudResearch {
    internal sealed class Preset {public string Record,Name,Flags;public int Index;}
    internal sealed class SharedJournal {
        public bool Exists,AppliedVerified;public bool? HideRequested;public int AppliedByPid,RestoreEntries;
        public string LastWriteUtc,BeforeHash,AfterHash,PreparedUtc,AppliedUtc,AppliedFromFolder,ReadError;
    }
    internal sealed class Event {
        public int Sequence,TotalPresets,HiddenPresets;
        public string Utc,Kind,Detail,SettingsFile,FileHash,LastAppliedHash,PresetFlags;
        public bool? GameRunning,Requested,ObservedHidden;public bool Pending;
    }
    readonly Queue<Event> events=new Queue<Event>();int sequence;
    internal readonly int Pid;internal readonly string Session=Guid.NewGuid().ToString("N").Substring(0,12);
    internal NativeHudResearch(string root){using(var process=Process.GetCurrentProcess())Pid=process.Id;}
    static string Limit(string value,int maximum){return value==null?null:value.Substring(0,Math.Min(value.Length,maximum));}
    internal static Preset[] ReadPresets(NativeHudFile.Document doc){
        return doc.Groups.Take(64).Select(g=>new Preset{
            Record=NativeHudFile.Hash(Encoding.UTF8.GetBytes(g.Record.Identity)).Substring(0,12),
            Index=g.Preset+1,Name=Limit(g.Name,80),
            Flags=string.Concat(g.Rows.Select(r=>(bool)r["Hide"]?"1":"0"))
        }).ToArray();
    }
    internal void Add(string kind,string detail,bool? game,bool? requested,bool pending,bool? observed,string file,string hash,string applied,int total,int hidden,Preset[] presets){
        events.Enqueue(new Event{Sequence=++sequence,Utc=DateTime.UtcNow.ToString("o"),Kind=kind,Detail=Limit(detail,512),SettingsFile=Limit(file,1024),GameRunning=game,Requested=requested,Pending=pending,ObservedHidden=observed,FileHash=hash,LastAppliedHash=applied,TotalPresets=total,HiddenPresets=hidden,PresetFlags=string.Join(";",presets.Select(p=>p.Record+"/"+p.Index+":"+p.Flags))});
        while(events.Count>32)events.Dequeue();
    }
    internal Event[] Events {get{return events.ToArray();}}
}
