using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;

// Metadata resolution and character-list recognition informed by
// SkeeveAN/Aion-DPS-Meter (MIT), e767bf486d52407d91f5fea698a9fdc5c7d1d5af.
// Copyright (c) 2026 SkeeveTV. Notice: assets/skills/LICENSE-MIT.txt.
// Names are game metadata, not evidence that an amount is damage.
internal sealed class SkillDefinition {
    public uint Id {get;set;}public string Name {get;set;}public string Spanish {get;set;}public string Kind {get;set;}
    public bool Periodic {get;set;}public bool Healing {get;set;}public bool Damage {get;set;}public bool NonDamage {get;set;}
    internal SkillDefinition Copy(){return (SkillDefinition)MemberwiseClone();}
}
internal sealed class SkillCatalogue {
    sealed class Data {public int Version {get;set;}public string Revision {get;set;}public List<SkillDefinition> Skills {get;set;}}
    static readonly Lazy<SkillCatalogue> shared=new Lazy<SkillCatalogue>(Load);
    internal static SkillCatalogue Shared {get{return shared.Value;}}
    readonly object gate=new object();readonly Dictionary<uint,SkillDefinition> definitions=new Dictionary<uint,SkillDefinition>();
    readonly HashSet<uint> unknown=new HashSet<uint>();long revision;internal string Error,SourceRevision;
    internal int Count {get{lock(gate)return definitions.Count;}}internal long Revision {get{return Interlocked.Read(ref revision);}}
    internal int UnknownCount {get{lock(gate)return unknown.Count;}}
    static SkillCatalogue Load(){var c=new SkillCatalogue();try{using(var s=typeof(SkillCatalogue).Assembly.GetManifestResourceStream("Aion2SkillCatalogue.json")){if(s==null)throw new IOException("Embedded skill catalogue is missing");using(var reader=new StreamReader(s)){string text=reader.ReadToEnd();if(text.Length>4000000)throw new InvalidDataException("Skill catalogue exceeds limit");var data=new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<Data>(text);if(data==null||data.Version!=1||data.Skills==null||data.Skills.Count>25000)throw new InvalidDataException("Unsupported skill catalogue");c.SourceRevision=data.Revision;foreach(var definition in data.Skills)if(!c.Register(definition))throw new InvalidDataException("Invalid skill definition");}}}catch(Exception ex){if(!(ex is IOException||ex is ArgumentException||ex is InvalidOperationException))throw;c.Error=ex.Message;}return c;}
    internal bool Register(SkillDefinition d){if(d==null||d.Id==0||string.IsNullOrWhiteSpace(d.Name)||d.Name.Length>160||d.Spanish!=null&&d.Spanish.Length>160)return false;lock(gate){if(definitions.Count>=25000&&!definitions.ContainsKey(d.Id))return false;definitions[d.Id]=d.Copy();unknown.Remove(d.Id);Interlocked.Increment(ref revision);return true;}}
    internal bool HasExact(uint id){lock(gate)return definitions.ContainsKey(id);}
    SkillDefinition Find(uint id){SkillDefinition d;if(definitions.TryGetValue(id,out d))return d;uint family=id/1000000;if(family>=11&&family<=19&&definitions.TryGetValue(id/10000*10000,out d))return d;return null;}
    internal string Name(uint id,string language){lock(gate){var d=Find(id);if(d==null){if(unknown.Count<512)unknown.Add(id);return UiLanguage.Text("Unknown skill",language)+" ("+id.ToString(CultureInfo.InvariantCulture)+")";}if(language=="es"&&!string.IsNullOrEmpty(d.Spanish))return d.Spanish;return d.Name;}}
    internal bool IsHealing(uint id){lock(gate){var d=Find(id);return d!=null&&d.Healing;}}
    internal bool IsDamage(uint id){lock(gate){var d=Find(id);return d!=null&&d.Damage&&!d.NonDamage;}}
    internal bool IsNonDamage(uint id){if(id>=2000000&&id<3000000)return true;lock(gate){var d=Find(id);return d!=null&&d.NonDamage;}}
}
internal sealed class CharacterSkillEntry {
    internal uint Id;internal byte Level,BaseLevel;
    internal CharacterSkillEntry Copy(){return (CharacterSkillEntry)MemberwiseClone();}
}
// The reference recognises entries inside 0051 against a known ID table; it
// does not decode an entire stable record schema. Keep that limitation explicit.
internal sealed class CharacterSkillState {
    sealed class Pending {internal List<CharacterSkillEntry> Entries;internal double Time;}
    readonly Dictionary<string,Pending> pending=new Dictionary<string,Pending>();readonly Dictionary<uint,CharacterSkillEntry> entries=new Dictionary<uint,CharacterSkillEntry>();
    internal uint Actor;internal string Flow;internal long Lists;internal int RejectedLists;
    internal int Count {get{return entries.Count;}}
    internal static bool Parse(byte[] b,int start,int end,SkillCatalogue catalog,out List<CharacterSkillEntry> result){result=new List<CharacterSkillEntry>();if(b==null||start<0||end>b.Length||end-start<13||end-start>65536||b[start]!=0||b[start+1]!=0x51)return false;var seen=new HashSet<uint>();for(int p=start+4;p+8<end;p++){uint id=BitConverter.ToUInt32(b,p);if(id<1000000||(b[p-1]!=1&&b[p-1]!=5)||!catalog.HasExact(id)||seen.Contains(id))continue;byte level=b[p+4],basis=b[p+5];if(level<1||level>60||basis>level)continue;if(result.Count>=512)return false;seen.Add(id);result.Add(new CharacterSkillEntry{Id=id,Level=level,BaseLevel=basis});}return result.Count>0;}
    internal void Observe(byte[] b,int start,int end,double time,string flow,SkillCatalogue catalog){List<CharacterSkillEntry> parsed;if(!Parse(b,start,end,catalog,out parsed)||flow==null){RejectedLists++;return;}Lists++;if(flow==Flow&&Actor!=0){Merge(parsed);return;}if(pending.Count>=4&&!pending.ContainsKey(flow))pending.Clear();pending[flow]=new Pending{Entries=parsed,Time=time};}
    void Merge(List<CharacterSkillEntry> parsed){foreach(var e in parsed)if(entries.Count<512||entries.ContainsKey(e.Id))entries[e.Id]=e.Copy();}
    internal void Bind(string flow,uint actor,double now){if(flow!=Flow||actor!=Actor){entries.Clear();Flow=flow;Actor=actor;}Pending p;if(pending.TryGetValue(flow,out p)&&now>=p.Time&&now-p.Time<=15)Merge(p.Entries);pending.Remove(flow);}
    internal void Unbind(){entries.Clear();Actor=0;Flow=null;}
    internal void Reset(){Unbind();pending.Clear();}
    internal List<CharacterSkillEntry> Read(){var copy=new List<CharacterSkillEntry>();foreach(var p in entries)copy.Add(p.Value.Copy());return copy;}
}

// Stable numeric keys survive late names and localisation changes. These are
// bounded incremental encounter summaries, never an unbounded packet log.
internal sealed class DpsSkillTotal {
    public uint SkillId {get;set;}public uint TargetId {get;set;}
    public long Damage {get;set;}public long PeriodicDamage {get;set;}public long Healing {get;set;}public long HealingReceived {get;set;}
    public long ReceivedPrimary {get;set;}public long ReceivedCritical {get;set;}public int ReceivedEvents {get;set;}public int ReceivedCriticals {get;set;}public int HealingTicks {get;set;}public int ReceivedTicks {get;set;}
    public long DamagePrimary {get;set;}public long DamageCritical {get;set;}public long HealingPrimary {get;set;}public long HealingCritical {get;set;}
    public int EligibleDamageImpacts {get;set;}public int DirectImpacts {get;set;}public int PeriodicTicks {get;set;}public int Criticals {get;set;}public int HealingEvents {get;set;}public int HealingCriticals {get;set;}
    public uint HighestHit {get;set;}public double First {get;set;}public double Last {get;set;}
    internal DpsSkillTotal Copy(){return (DpsSkillTotal)MemberwiseClone();}
    internal static List<DpsSkillTotal> Copies(IEnumerable<DpsSkillTotal> rows){var result=new List<DpsSkillTotal>();if(rows!=null)foreach(var row in rows)result.Add(row.Copy());return result;}
    internal void Add(DpsSignal.Hit h,double timestamp,string kind){if(First==0)First=timestamp;else First=Math.Min(First,timestamp);Last=Math.Max(Last,timestamp);if(kind=="received"){HealingReceived+=h.Amount;if(h.Periodic)ReceivedTicks++;if(h.CriticalKnown){ReceivedEvents++;ReceivedPrimary+=h.PrimaryAmount;if(h.Critical){ReceivedCriticals++;ReceivedCritical+=h.PrimaryAmount;}}return;}if(kind=="healing"){Healing+=h.Amount;if(h.Periodic)HealingTicks++;if(h.CriticalKnown){HealingEvents++;HealingPrimary+=h.PrimaryAmount;if(h.Critical){HealingCriticals++;HealingCritical+=h.PrimaryAmount;}}return;}Damage+=h.Amount;HighestHit=Math.Max(HighestHit,h.Periodic?h.Amount:h.PrimaryAmount);if(h.Periodic){PeriodicDamage+=h.Amount;PeriodicTicks++;}else{DirectImpacts+=1+h.Extras;if(h.CriticalKnown){EligibleDamageImpacts++;DamagePrimary+=h.PrimaryAmount;if(h.Critical){Criticals++;DamageCritical+=h.PrimaryAmount;}}}}
}
internal sealed class DpsSkillDisplay {
    public uint ActorId {get;set;}public string Player {get;set;}public uint SkillId {get;set;}public string Skill {get;set;}public uint TargetId {get;set;}
    public long Damage {get;set;}public long PeriodicDamage {get;set;}public long Healing {get;set;}public long HealingReceived {get;set;}public uint HighestHit {get;set;}
    public int EligibleDamageImpacts {get;set;}public int DirectImpacts {get;set;}public int PeriodicTicks {get;set;}public double? CriticalRate {get;set;}public double? CriticalShare {get;set;}
    public int HealingTicks {get;set;}public int ReceivedTicks {get;set;}public double? HealingCriticalRate {get;set;}public double? HealingCriticalShare {get;set;}public double? ReceivedCriticalRate {get;set;}public double? ReceivedCriticalShare {get;set;}
    internal static DpsSkillDisplay Of(DpsSkillTotal s,uint actor,string player,string language,SkillCatalogue catalogue){return new DpsSkillDisplay{ActorId=actor,Player=player,SkillId=s.SkillId,Skill=catalogue.Name(s.SkillId,language),TargetId=s.TargetId,Damage=s.Damage,PeriodicDamage=s.PeriodicDamage,Healing=s.Healing,HealingReceived=s.HealingReceived,HighestHit=s.HighestHit,DirectImpacts=s.DirectImpacts,PeriodicTicks=s.PeriodicTicks,HealingTicks=s.HealingTicks,ReceivedTicks=s.ReceivedTicks,HealingCriticalRate=s.HealingEvents>0?(double?)(100.0*s.HealingCriticals/s.HealingEvents):null,HealingCriticalShare=s.HealingPrimary>0?(double?)(100.0*s.HealingCritical/s.HealingPrimary):null,ReceivedCriticalRate=s.ReceivedEvents>0?(double?)(100.0*s.ReceivedCriticals/s.ReceivedEvents):null,ReceivedCriticalShare=s.ReceivedPrimary>0?(double?)(100.0*s.ReceivedCritical/s.ReceivedPrimary):null,CriticalRate=s.EligibleDamageImpacts>0?(double?)(100.0*s.Criticals/s.EligibleDamageImpacts):null,CriticalShare=s.DamagePrimary>0?(double?)(100.0*s.DamageCritical/s.DamagePrimary):null};}
}
