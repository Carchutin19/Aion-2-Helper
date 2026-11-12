using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Collections.Generic;

internal static class EffectOverlayVerification {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static byte[] NpcImmunity(uint actor,bool present=true){
        var b=new List<byte>{0x41,0x36};Action<uint> variable=value=>{do{byte low=(byte)(value&127);value>>=7;b.Add((byte)(low|(value!=0?128:0)));}while(value!=0);};
        variable(actor);b.AddRange(BitConverter.GetBytes((ushort)8196));b.Add(0);b.AddRange(BitConverter.GetBytes(2100146u));b.AddRange(BitConverter.GetBytes((ushort)512));
        foreach(float f in new[]{93400f,27000f,26000f,0f})b.AddRange(BitConverter.GetBytes(f));b.AddRange(new byte[3]);variable(119700);variable(119700);b.AddRange(new byte[52]);b.Add(6);variable(present?1u:0u);
        if(present){b.Add(17);variable(17);b.AddRange(BitConverter.GetBytes(10000001u));b.AddRange(BitConverter.GetBytes(-1L));b.AddRange(BitConverter.GetBytes(4102412400000L));variable(actor);b.Add(1);b.Add(0);foreach(float f in new[]{93400f,27000f,26000f})b.AddRange(BitConverter.GetBytes(f));}
        b.Add(1);variable(0);b.AddRange(BitConverter.GetBytes(10));b.AddRange(new byte[4]);variable(0);return b.ToArray();
    }
    internal static void Run(string root){
        CombatOverlayState.Verify();EffectSnapshotReader.Verify();NativeHudShortcut.Verify();
        var state=new CombatOverlayState();state.Configure(true);
        Action<byte[],double,string> feed=(b,t,f)=>state.Consume(b,0,b.Length,t,f);
        feed(EnemyTargetVerification.Player(13238,"Own",100,100,true),1,"A");
        Check(EffectCatalogue.Find(137200001).kind=="P"&&!EffectCatalogue.Visible(137200001)&&EffectCatalogue.Visible(137200101),"Permanent passive records are hidden; their temporary beneficial activations remain visible");
        foreach(uint passive in new uint[]{12011,12031,80001,130000001,137100001,137200001,137300001,137400001,137500001,137600001,137700001,137800001,137900001,138000001})
            feed(PlayerShieldVerification.Status(13238,passive,passive,0),1.5,"A");
        Check(state.ReadBuffs(1.6,true,true).Length==0,"The recorded login passive list must not become a row of permanent buffs");
        feed(PlayerShieldVerification.Status(13238,137200101,137200101,13720010),1.7,"A");
        Check(state.ReadBuffs(1.7,true,true).Single().Id==137200101,"A passive's active proc displays once without its permanent definition");
        feed(PlayerShieldVerification.Remove(13238,137200101),1.8,"A");
        feed(PlayerShieldVerification.Status(13238,100,133100101,13310010),2,"A");
        feed(PlayerShieldVerification.Status(20000,100,137300071,13730007),2,"A");
        feed(PlayerShieldVerification.Status(13238,101,12000001,0),2,"A");
        Check(state.ReadEffects(0,null,3,true,true,true,true,64).Length==2,"Own attack-associated buff and own debuff survive catalog classification");
        Check(state.ReadEffects(20000,"A",3,true,true,true,true,64).Length==1,"Identical instances on different actors remain separate");
        Check(state.ReadEffects(20001,"A",3,true,true,true,true,64).Length==0&&state.ReadEffects(20000,"B",3,true,true,true,true,64).Length==0,"Different target/flow never receives another actor's effects");
        Check(state.ReadEffects(0,null,3,true,true,true,false,64).Single().Id==133100101,"Hide debuffs keeps beneficial effects");
        Check(state.ReadEffects(0,null,3,true,true,false,true,64).Single().Id==12000001,"Hide buffs keeps harmful effects, even from unknown source skills");
        feed(PlayerShieldVerification.Remove(20000,100),3,"A");
        Check(state.ReadEffects(0,null,3,true,true,true,true,64).Length==2&&state.ReadEffects(20000,"A",3,true,true,true,true,64).Length==0,"Target removal does not remove an identically numbered own effect");
        feed(PlayerShieldVerification.Status(20000,100,137300071,13730007),2.5,"A");
        Check(state.ReadEffects(20000,"A",4,true,true,true,true,64).Length==0,"Older updates cannot resurrect target effects");
        feed(PlayerShieldVerification.Status(13238,102,3015,13050240),4,"A");
        Check(!state.ReadBuffs(5,true,true).Any(v=>v.Id==3015),"Internal dash availability marker is not a visible debuff");
        feed(PlayerShieldVerification.Status(13238,104,200,0),5,"A");
        Check(!EffectCatalogue.Visible(200)&&!state.ReadBuffs(5,true,true).Any(v=>v.Id==200)&&state.ReadBuffs(5,true,true).Any(v=>v.Id==12000001),"Recorded dash action marker is excluded without hiding genuine debuffs");
        Check(BuffOverlay.StackLabel(3)=="x3"&&BuffOverlay.StackLabel(12)=="x12","Stack counters always include x");
        long epoch=1792249067207L;var clock=new byte[10];clock[1]=0x36;Buffer.BlockCopy(BitConverter.GetBytes(epoch),0,clock,2,8);feed(clock,10,"A");
        feed(PlayerShieldVerification.Status(13238,103,133100101,13310010),11,"A");
        Check(state.ReadBuffs(11,true,true).Single(v=>v.Id==133100101).Ends==12,"Catalog grouping chooses the earliest live instance");
        Check(state.ReadBuffs(13,true,true).Single(v=>v.Id==133100101).Ends==20&&!state.ReadBuffs(21,true,true).Any(v=>v.Id==133100101),"Server expiry is aligned to the same flow clock, not receipt plus full duration");
        state.Configure(false);Check(state.ReadBuffs(12,true,true).Length==0,"Disabling collection clears all actors");
        state.Configure(true);var immune=NpcImmunity(6000);EffectSnapshotReader.Item[] initial;int matches;
        Check(EffectSnapshotReader.TryRead(immune,0,immune.Length,0x4136,out initial,out matches)&&initial.Length==1&&initial[0].effectId==10000001&&initial[0].skillId==0,"Native NPC initial immunity does not require a skill or the summon-only 0702 suffix");
        for(int i=0;i<immune.Length;i++)Check(!EffectSnapshotReader.TryRead(immune,0,i,0x4136,out initial,out matches),"A truncated NPC profile cannot produce an initial effect list");
        feed(immune,13,"A");var enemyImmunity=state.ReadEffects(6000,"A",13,true,true,true,true,64);
        Check(enemyImmunity.Length==1&&enemyImmunity[0].Id==10000001&&!enemyImmunity[0].Harmful&&enemyImmunity[0].Ends==0,"Permanent native immunity is a positive effect, not a hidden passive or an invented timer");
        feed(NpcImmunity(6000,false),14,"A");
        Check(state.ReadEffects(6000,"A",14,true,true,true,true,64).Length==0,"A complete later NPC snapshot clears effects no longer listed");
        var effects=new[]{new CombatOverlayState.Effect{Id=133100101,Skill=13310010},new CombatOverlayState.Effect{Id=137200101,Skill=13720010},new CombatOverlayState.Effect{Id=12000001,Harmful=true},new CombatOverlayState.Effect{Id=100000301,Harmful=true}};
        for(int mode=0;mode<5;mode++)for(int grow=0;grow<4;grow++){
            var options=new BuffOverlayOptions{X=200,Y=300,Names=false,LayoutMode=mode,Grow=grow};Rectangle[] cells;var bounds=options.Geometry(effects,out cells);
            foreach(var cell in cells)Check(new Rectangle(Point.Empty,bounds.Size).Contains(cell),"Effect cell remains inside its widget in every layout");
            for(int i=0;i<cells.Length;i++)for(int j=i+1;j<cells.Length;j++)Check(!cells[i].IntersectsWith(cells[j]),"Effect layouts never overlap icons");
            if(mode==1)Check(cells[0].Left>cells[2].Left,"Buffs right, debuffs left");
            if(mode==2)Check(cells[0].Left<cells[2].Left,"Buffs left, debuffs right");
            if(mode==3)Check(cells[0].Top<cells[2].Top,"Buffs above debuffs");
            if(mode==4)Check(cells[0].Top>cells[2].Top,"Debuffs above buffs");
        }
        for(int mode=1;mode<5;mode++)for(int good=0;good<4;good++)for(int bad=0;bad<4;bad++)foreach(int gap in new[]{0,5})foreach(bool names in new[]{false,true}){
            var options=new BuffOverlayOptions{LayoutMode=mode,BuffGrow=good,DebuffGrow=bad,Gap=gap,Names=names};options.Normalize();Rectangle[] cells;var bounds=options.Geometry(effects,out cells);
            foreach(var cell in cells)Check(new Rectangle(Point.Empty,bounds.Size).Contains(cell),"Independent growth stays inside its group bounds");
            for(int i=0;i<cells.Length;i++)for(int j=i+1;j<cells.Length;j++)Check(!cells[i].IntersectsWith(cells[j]),"All independent layout/growth combinations avoid overlap");
            Check(good==0?cells[1].Left>cells[0].Left:good==1?cells[1].Left<cells[0].Left:good==2?cells[1].Top>cells[0].Top:cells[1].Top<cells[0].Top,"Buff direction follows its independent setting");
            Check(bad==0?cells[3].Left>cells[2].Left:bad==1?cells[3].Left<cells[2].Left:bad==2?cells[3].Top>cells[2].Top:cells[3].Top<cells[2].Top,"Debuff direction follows its independent setting");
        }
        foreach(bool first in new[]{false,true})foreach(int direction in new[]{0,1,2,3}){
            var options=new BuffOverlayOptions{LayoutMode=0,DebuffsFirst=first,Grow=direction};var ordered=options.Arrange(effects);
            Check(ordered[0].Harmful==first&&ordered[1].Harmful==first&&ordered[2].Harmful!=first,"Combined effects stay grouped by the selected first type");
            var changed=options.Arrange(new[]{effects[3],effects[1],effects[0]});Check(changed[0].Harmful==first&&changed[2].Harmful!=first,"New effect arrivals reapply type order");
        }
        var legacy=new JavaScriptSerializer().Deserialize<BuffOverlayOptions>("{\"LayoutMode\":3,\"Grow\":1}");legacy.Normalize();Check(legacy.Direction(false)==1&&legacy.Direction(true)==1,"Old settings retain their previous row direction");
        var original=new EnergyConfiguration{Options=new EnergyBarOptions()};original.EnemyHealth.Effects.LayoutMode=4;
        var copy=original.Copy();copy.EnemyHealth.Effects.X=100;Check(original.EnemyHealth.Effects.X==0&&!ConfigurationHistory.Same(original,copy),"Target effects are deep-copied and tracked by undo");
        using(var assets=new EffectIconAssets())Check(assets.Icon(133100101)!=null&&assets.Icon(100000301)!=null,"Native effect icons are available offline");
        using(var owner=new EnergyOverlay(root,true)){
            owner.Buffs.VerifyAppearance();
            var settings=new HelperSettings(owner);try{settings.VerifyTargetEffectTabs(root);}finally{settings.Close();}
            using(var expiry=owner.Buffs.ExpiryPreview())expiry.Save(Path.Combine(root,"expiry-preview.png"));
            var options=new BuffOverlayOptions{Names=false,LayoutMode=3,TypeBorders=true};
            using(var image=owner.Buffs.Preview(options,"es"))image.Save(Path.Combine(root,"effects-preview.png"));
            foreach(string language in UiLanguageMore.Codes){
                var cfg=owner.Configuration;cfg.Language=language;owner.ApplyConfiguration(cfg);
                var window=new System.Windows.Window{Width=870,Height=1190};SettingsVisual.Theme(window);
                var pane=new TargetEffectsSettingsPane(owner){Margin=new System.Windows.Thickness(20)};window.Content=pane;
                try{pane.Reload();pane.VerifyControls();UiLanguage.Apply(window,language);SettingsVisual.RenderPreview(window,Path.Combine(root,"target-effects-settings-"+language+".png"));}
                finally{window.Close();}
            }

        }
        File.WriteAllText(Path.Combine(root,"effects-test.txt"),"PASS: catalog/native icons, actor+instance isolation, own/target filters, tombstones, independent layouts + combined ordering, native HUD shortcut, dedicated tab, stack labels, deep-copy/undo, snapshot truncation and both visual styles.");
    }
    internal static void Replay(string root,string path){
        var state=new CombatOverlayState();state.Configure(true);int frames=0,statuses=0,known=0,unknown=0,reads=0,bad=0,npcSnapshots=0,immunitySnapshots=0;
        var json=new JavaScriptSerializer{MaxJsonLength=1000000};
        foreach(string line in File.ReadLines(path)){
            var row=json.Deserialize<Dictionary<string,object>>(line);string hex=Convert.ToString(row["hex"]);double time=Convert.ToDouble(row["ts"]);
            var bytes=new byte[hex.Length/2];for(int i=0;i<bytes.Length;i++)bytes[i]=Convert.ToByte(hex.Substring(i*2,2),16);
            state.Consume(bytes,0,bytes.Length,time,"replay");frames++;
            if(bytes.Length>2&&bytes[0]==0x41&&bytes[1]==0x36){
                EffectSnapshotReader.Item[] snapshot;int matches;
                if(EffectSnapshotReader.TryRead(bytes,0,bytes.Length,0x4136,out snapshot,out matches)){
                    npcSnapshots++;immunitySnapshots+=snapshot.Count(v=>v.effectId==10000001);
                }
            }
            if(bytes.Length>2&&(bytes[0]==0x2a||bytes[0]==0x2b)&&bytes[1]==0x38){
                uint actor;List<PlayerShieldSignal.Status> list;
                if(!PlayerShieldSignal.ReadStatuses(bytes,0,bytes.Length,out actor,out list))throw new Exception("Recorded status message rejected");
                foreach(var item in list){statuses++;if(EffectCatalogue.Visible(item.Effect))known++;else unknown++;}
                var active=state.ReadEffects(actor,"replay",time,true,true,true,true,64);reads+=active.Length;bad+=active.Count(e=>e.Harmful);
                Check(active.All(e=>e.Actor==actor&&EffectCatalogue.Visible(e.Id)),"Replay never mixes recipient actors or leaks hidden/unknown effects");
            }
        }
        Check(frames>1000&&statuses>100,"Replay must include representative recorded game data");
        File.WriteAllText(Path.Combine(root,"effects-replay-test.json"),json.Serialize(new{frames,statuses,visibleCatalogEntries=known,excludedFromWidget=unknown,effectReads=reads,harmfulReads=bad,npcSnapshots,immunitySnapshots,liveConfirmation=false}));
    }

}
