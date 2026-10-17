using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;

internal static class PlayerShieldVerification {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Var(List<byte> b,uint value){do{byte n=(byte)(value&127);value>>=7;b.Add((byte)(n|(value>0?128:0)));}while(value>0);}
    internal static byte[] Status(uint actor,uint instance,uint effect=182400101,uint skill=18240010,bool single=false,bool removing=false){
        var b=new List<byte>{single?(byte)0x2b:(byte)0x2a,0x38};Var(b,actor);if(!single)Var(b,1);b.Add(3);Var(b,instance);b.AddRange(BitConverter.GetBytes(effect));b.AddRange(BitConverter.GetBytes(effect==182400101?20000L:10000L));b.AddRange(BitConverter.GetBytes(1792249077207L));Var(b,13238);b.Add(8);b.AddRange(BitConverter.GetBytes(skill));b.Add(removing?(byte)1:(byte)0);return b.ToArray();
    }
    internal static byte[] Tick(uint actor,uint instance,uint? remaining,uint effect=1824001011,uint skill=18240010,uint source=13238){var b=new List<byte>{5,0x38};Var(b,actor);b.Add((byte)(10|(remaining.HasValue?1:0)));Var(b,source);Var(b,instance);b.AddRange(BitConverter.GetBytes(effect));if(remaining.HasValue)Var(b,remaining.Value);Var(b,82);b.AddRange(BitConverter.GetBytes(skill));return b.ToArray();}
    internal static byte[] Remove(uint actor,uint instance){var b=new List<byte>{0x2c,0x38};Var(b,actor);Var(b,1);b.Add(0);Var(b,instance);b.Add(7);return b.ToArray();}
    static void Feed(PlayerVitalSignal s,byte[] b,double t,string flow="A"){s.Consume(b,0,b.Length,t,flow);}
    internal static void Run(string root){
        var s=new PlayerVitalSignal();s.Configure(true);double t=DpsHistory.UtcNow();Feed(s,EnemyTargetVerification.Player(14808,"Self",12765,12765,true),t);
        Feed(s,Status(14808,671),t);Feed(s,Tick(14808,671,2935),t);Check(s.Read(false,t).Shield==2935&&s.Read(true,t).Shield==0,"Only HP receives confirmed shield capacity");
        Feed(s,Tick(14808,671,2099,1824001011,18240010,26651),t+1);Check(s.Read(false,t+1).Shield==2099,"NPC absorption source still updates own shield");Feed(s,Tick(14808,671,9999),t+.5);Check(s.Read(false,t+1).Shield==2099,"Older values cannot restore consumed shield");
        Feed(s,Tick(14808,671,null),t+2);Check(s.Read(false,t+2).Shield==2099,"Missing remaining field is not an invented zero");
        Feed(s,Tick(111,671,8888),t+2);Feed(s,Tick(14808,671,8888),t+2,"B");Feed(s,Tick(14808,671,8888,1540002011,15400020),t+2);Check(s.Read(false,t+2).Shield==2099,"Other actors, flows and unknown periodic effects are isolated");
        var broken=Remove(14808,671);Array.Resize(ref broken,broken.Length-1);Feed(s,broken,t+3);Check(s.Read(false,t+3).Shield==2099,"Truncated removal cannot clear shield");Feed(s,Remove(14808,777),t+3);Check(s.Read(false,t+3).Shield==2099,"Different status removal cannot clear shield");Feed(s,Remove(14808,671),t+3);Check(s.Read(false,t+3).Shield==0,"Matching explicit removal clears remaining capacity");Feed(s,Tick(14808,671,2935),t+4);Check(s.Read(false,t+4).Shield==0,"Removed instance cannot be resurrected by another tick");
        Feed(s,Status(14808,760,187300021,18730002,true),t+4);Feed(s,Tick(14808,760,548,1873000211,18730002),t+4);Check(s.Read(false,t+4).Shield==548,"Single-status bit-packed envelope supports proc shield");
        Feed(s,EnemyTargetVerification.Player(14808,"Self",12765,12765,true),t+13);Check(s.Read(false,t+13).Shield==548,"Same-character profile does not drop active shield");Feed(s,Status(14808,760,187300021,18730002,true),t+13);Check(s.Read(false,t+14).Shield==0,"Duplicate status does not extend received expiry or gradually estimate capacity");
        Feed(s,Status(14808,900),t+14);Feed(s,Tick(14808,900,2935),t+14);Feed(s,Status(14808,901,187300021,18730002),t+14);Feed(s,Tick(14808,901,548,1873000211,18730002),t+14);Check(s.Read(false,t+14).Shield==2935,"Unvalidated concurrent shields must not be added");Feed(s,Remove(14808,900),t+15);Check(s.Read(false,t+15).Shield==548,"Removing largest leaves another confirmed active shield");
        Feed(s,Status(14808,901,187300021,18730002,true,true),t+16);Check(s.Read(false,t+16).Shield==0,"Removing status flag clears matching shield");
        Feed(s,Status(14808,902),t+17);Feed(s,Tick(14808,902,2935),t+17);Feed(s,EnemyTargetVerification.Player(14809,"NewSelf",1000,1000,true),t+18);Check(s.Read(false,t+18).Shield==0,"Own actor replacement clears prior shield");s.Reset();Check(s.Read(false,t+18)==null,"Transport reset clears resources and shield");
        Feed(s,EnemyTargetVerification.Player(14808,"Self",12765,12765,true),t+20);Feed(s,Status(14808,1000),t+20);Feed(s,Tick(14808,1000,2935),t+20);var map=new byte[48];map[0]=0x21;map[1]=0x36;Array.Copy(BitConverter.GetBytes(123u),0,map,6,4);Feed(s,map,t+20);Check(s.Read(false,t+20).Shield==2935,"First map metadata retains same-flow shield");Array.Copy(BitConverter.GetBytes(124u),0,map,6,4);Feed(s,map,t+21);Check(s.Read(false,t+21).Shield==0,"Known map change clears shield even within profile ordering grace");
        DpsSignal.Hit hit;foreach(var tick in new[]{Tick(14808,671,2099),Tick(14808,760,466,1873000211,18730002)})Check(!DpsSignal.Periodic(tick,0,tick.Length,out hit)&&!DpsSignal.HealingTick(tick,0,tick.Length,out hit),"Shield capacity and absorption cannot become DPS damage or healing");
        var ledger=new PlayerShieldSignal();var valid=Status(14808,1);for(int n=2;n<valid.Length;n++)Check(!ledger.Consume(valid,0,n,t,14808),"Truncated status must be atomic");
        var batch=new List<byte>{0x2a,0x38};Var(batch,14808);Var(batch,9);int bitIndex=-1;
        for(int i=0;i<9;i++){batch.Add(3);Var(batch,(uint)(i+1));batch.AddRange(BitConverter.GetBytes(i==8?187300021u:182400101u));batch.AddRange(BitConverter.GetBytes(10000L));batch.AddRange(BitConverter.GetBytes(1792249077207L));Var(batch,13238);batch.Add(8);batch.AddRange(BitConverter.GetBytes(i==8?18730002u:18240010u));if(i%8==0){bitIndex=batch.Count;batch.Add(0);}if(i==6)batch[bitIndex]|=(byte)(1<<(i%8));}
        uint target;List<PlayerShieldSignal.Status> parsed;var multi=batch.ToArray();Check(PlayerShieldSignal.ReadStatuses(multi,0,multi.Length,out target,out parsed)&&parsed.Count==9&&parsed[6].Removing&&!parsed[8].Removing&&parsed[8].Skill==18730002,"Bit-packed booleans span intervening fields and a second bit byte");
        var random=new Random(3817);int[] ops={0x2a38,0x2b38,0x0538,0x2c38};for(int i=0;i<4000;i++){var bytes=new byte[random.Next(2,400)];random.NextBytes(bytes);int op=ops[i%4];bytes[0]=(byte)(op>>8);bytes[1]=(byte)op;ledger.Consume(bytes,0,bytes.Length,t,14808);}Check(ledger.Read(t)==0,"Malformed random envelopes cannot fabricate supported shield");
        var hp=PlayerVitalOptions.Defaults(false);hp.Shield.Color="#FFC540";hp.Shield.Opacity=85;hp.Shield.GlowPercent=110;var copy=hp.Copy();copy.Shield.Color="#FFFFFF";Check(hp.Shield.Color=="#FFC540","Shield settings deep-copy independently");var json=new JavaScriptSerializer();var restored=PlayerVitalOptions.Read(json.Deserialize<Dictionary<string,object>>(json.Serialize(new{playerHealth=hp})),false);Check(PlayerVitalOptions.Same(hp,restored),"Shield appearance persists with player settings");hp.Shield=null;hp.Normalize();Check(hp.Shield!=null,"Old settings without shield have safe defaults");
        Directory.CreateDirectory(Path.Combine(root,"designs"));
        foreach(int width in new[]{80,420})foreach(double ratio in new[]{0,.05,.23,1,2}){
            var o=PlayerVitalOptions.Defaults(false);o.Bounds=new Rectangle(0,0,width,12);o.Shield.Opacity=100;
            using(var plain=PlayerVitalDesign.Render(o,false,.8,false))using(var shield=PlayerVitalDesign.Render(o,false,.8,false,null,null,ratio)){
                Check(plain.Core==shield.Core&&plain.Bitmap.Size==shield.Bitmap.Size,"Shield does not change geometry or caption positions");int count=0;for(int y=0;y<plain.Bitmap.Height;y++)for(int x=0;x<plain.Bitmap.Width;x++)if(plain.Bitmap.GetPixel(x,y)!=shield.Bitmap.GetPixel(x,y)){count++;Check(plain.Core.Contains(x,y),"Shield and its glow stay inside HP bar; no extra border");}
                Check(ratio==0?count==0:count>0,"Capacity produces bounded golden fill only when positive");if(width==420)shield.Bitmap.Save(Path.Combine(root,"designs","player-shield-"+ratio.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
            }
        }
        var mp=PlayerVitalOptions.Defaults(true);using(var a=PlayerVitalDesign.Render(mp,true,.8,false))using(var b=PlayerVitalDesign.Render(mp,true,.8,false,null,null,1))for(int y=0;y<a.Bitmap.Height;y++)for(int x=0;x<a.Bitmap.Width;x++)Check(a.Bitmap.GetPixel(x,y)==b.Bitmap.GetPixel(x,y),"MP rendering is unchanged by shields");
        File.WriteAllText(Path.Combine(root,"player-shields-test.txt"),"PASS: verified two-family whitelist; status/remaining/removal/expiry; attacking-NPC source; missing remaining retained; atomic/truncated/fuzz; actor/flow/reset isolation; conservative concurrent-shield handling; settings copy/persistence; independent HP/MP; golden overlay clipped inside original bar without own border.");
    }
    internal static void Replay(string root,string frames){
        var s=new PlayerVitalSignal();s.Configure(true);var json=new JavaScriptSerializer();bool seeded=false;var events=new List<object>();var values=new HashSet<uint>();uint previous=0;int changes=0;bool exhausted=false,expired=false;double last=0;
        foreach(string line in File.ReadLines(frames)){
            var row=json.Deserialize<Dictionary<string,object>>(line);var hex=(string)row["hex"];var b=new byte[hex.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(hex.Substring(i*2,2),16);double t=Convert.ToDouble(row["ts"]);last=t;string flow=(string)row["stream"];
            // The collector began after login. Test-only identity/maxima fixture
            // comes from the helper's captured live baseline, never production.
            if(!seeded){Feed(s,EnemyTargetVerification.Player(14808,"Self",12765,12765,true),t,flow);seeded=true;}
            s.Consume(b,0,b.Length,t,flow);var sample=s.Read(false,t);if(sample==null)continue;uint amount=sample.Shield;values.Add(amount);if(amount!=previous){events.Add(new{time=t,shield=amount});changes++;previous=amount;}
            exhausted|=Math.Abs(t-1792249256.103092)<.001&&amount==0&&row["opcode"].ToString()=="2C38";
            expired|=Math.Abs(t-1792249077.401829)<.001&&amount==0&&row["opcode"].ToString()=="2C38";
        }
        Check(values.Contains(2935)&&values.Contains(2099)&&values.Contains(504)&&values.Contains(548)&&values.Contains(466)&&values.Contains(310)&&exhausted&&expired,"Real capture must reproduce manual consumption/expiry and proc consumption/exhaustion through production reader");
        File.WriteAllText(Path.Combine(root,"player-shields-replay-evidence.json"),json.Serialize(new{testOnlyBaseline=true,events=events,lastFrame=last}));File.WriteAllText(Path.Combine(root,"player-shields-replay-test.txt"),"PASS: actual incoming capture through production shield reader; manual2935→2099→...→504→expired; proc548→466→310→explicit removal; source switches to attacking NPC; observed changes="+changes+".");
    }
}
