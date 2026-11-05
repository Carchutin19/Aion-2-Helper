using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

internal sealed class DpsEncounterEstimate {
    internal enum Kind { None, Remaining, Boss, Target }
    internal struct Estimate {
        internal Kind Subject;
        internal int? Seconds;
        internal Estimate(Kind subject,int? seconds){Subject=subject;Seconds=seconds;}
    }
    struct Health {
        internal double Time;
        internal uint Value;
        internal Health(double time,uint value){Time=time;Value=value;}
    }
    sealed class Enemy {
        internal bool Boss;
        internal uint Current,Maximum;
        internal double HpTime,LastHit,LastDrop;
        internal readonly List<Health> Samples=new List<Health>();
        internal void ClearFight(){Samples.Clear();LastHit=LastDrop=0;}
    }
    static readonly HashSet<uint> bossTemplates=LoadBossTemplates();
    readonly Dictionary<uint,Enemy> enemies=new Dictionary<uint,Enemy>();
    uint selected;
    const double Window=8, MaxGap=3;
    static HashSet<uint> LoadBossTemplates(){
        using(var stream=typeof(DpsEncounterEstimate).Assembly.GetManifestResourceStream("DpsBossTemplates.json")){
            if(stream==null)return new HashSet<uint>();
            using(var reader=new StreamReader(stream))return new HashSet<uint>(new JavaScriptSerializer().Deserialize<uint[]>(reader.ReadToEnd()));
        }
    }

    internal void Spawn(EnemyHealthSignal.Sample sample,double time){
        if(enemies.Count>=256&&!enemies.ContainsKey(sample.Actor)){
            uint oldest=0;double seen=double.MaxValue;
            foreach(var pair in enemies){
                if(pair.Key!=selected&&Math.Max(pair.Value.HpTime,pair.Value.LastHit)<seen){
                    oldest=pair.Key;seen=Math.Max(pair.Value.HpTime,pair.Value.LastHit);
                }
            }
            enemies.Remove(oldest);
        }
        enemies[sample.Actor]=new Enemy{Boss=bossTemplates.Contains(sample.Npc),Current=sample.Current,Maximum=sample.Maximum,HpTime=time};
    }

    internal void Select(uint actor){selected=actor;}
    internal bool Tracks(uint actor){return enemies.ContainsKey(actor);}
    internal void Remove(uint actor){enemies.Remove(actor);if(selected==actor)selected=0;}
    internal void Reset(){enemies.Clear();selected=0;}
    internal void ClearFight(){foreach(var enemy in enemies.Values)enemy.ClearFight();}
    internal void MarkHit(uint actor,double time){
        Enemy enemy;
        if(!enemies.TryGetValue(actor,out enemy)||enemy.Current==0||time<enemy.LastHit)return;
        if(enemy.LastHit==0||time-enemy.LastHit>Window){
            enemy.ClearFight();
            if(time>=enemy.HpTime&&time-enemy.HpTime<=1)enemy.Samples.Add(new Health(enemy.HpTime,enemy.Current));
        }
        enemy.LastHit=time;
    }

    internal void Observe(uint actor,uint? current,uint? maximum,double time){
        Enemy enemy;
        if(!enemies.TryGetValue(actor,out enemy)||time<enemy.HpTime)return;
        uint next=current??enemy.Current,cap=maximum??enemy.Maximum;
        if(cap==0||next>cap)return;
        uint before=enemy.Current;
        bool capChanged=cap!=enemy.Maximum;
        double priorTime=enemy.HpTime;
        enemy.Current=next;enemy.Maximum=cap;enemy.HpTime=time;
        if(next==0){
            enemy.ClearFight();
            if(selected==actor)selected=0;
            return;
        }
        if(enemy.LastHit==0||time-enemy.LastHit>Window)return;
        bool reset=capChanged||next>before&&next-before>cap*.08;
        if(reset){
            enemy.Samples.Clear();enemy.LastDrop=0;
        }
        if(next==before&&!capChanged)return;
        if(next<before){
            if(enemy.LastDrop>0&&time-enemy.LastDrop>MaxGap)enemy.Samples.Clear();
            enemy.LastDrop=time;
        }
        if(enemy.Samples.Count==0&&time-priorTime<=1&&time>priorTime&&before>0&&!reset)
            enemy.Samples.Add(new Health(priorTime,before));
        while(enemy.Samples.Count>0&&time-enemy.Samples[0].Time>Window)enemy.Samples.RemoveAt(0);
        int last=enemy.Samples.Count-1;
        if(last>=0&&Math.Floor(enemy.Samples[last].Time*4)==Math.Floor(time*4))enemy.Samples[last]=new Health(time,next);
        else enemy.Samples.Add(new Health(time,next));
    }

    internal Estimate Read(double now){
        Enemy single=null,boss=null,target=null;
        int count=0,bossCount=0;
        foreach(var pair in enemies){
            var enemy=pair.Value;
            if(enemy.Current==0||enemy.LastHit==0||now-enemy.LastHit>Window)continue;
            count++;single=enemy;
            if(enemy.Boss){boss=enemy;bossCount++;}
            if(pair.Key==selected)target=enemy;
        }
        if(bossCount==1)return new Estimate(Kind.Boss,Seconds(boss,now));
        if(count==1)return new Estimate(Kind.Remaining,Seconds(single,now));
        if(target!=null)return new Estimate(target.Boss?Kind.Boss:Kind.Target,Seconds(target,now));
        return new Estimate();
    }

    static int? Seconds(Enemy enemy,double now){
        if(enemy.LastDrop==0||now-enemy.LastDrop>MaxGap||now-enemy.HpTime>MaxGap||enemy.Samples.Count<3)return null;
        int first=0,last=enemy.Samples.Count-1;
        while(first<last&&now-enemy.Samples[first].Time>Window)first++;
        double span=enemy.Samples[last].Time-enemy.Samples[first].Time;
        long drop=(long)enemy.Samples[first].Value-enemy.Current;
        if(last-first<2||span<3||drop<=0)return null;
        double estimate=enemy.Current*span/drop;
        return estimate<=3600?(int?)Math.Max(1,Math.Round(estimate,MidpointRounding.AwayFromZero)):null;
    }
}
