using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;

internal static class ProgressVerification {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void V(List<byte> b,uint v){while(v>=128){b.Add((byte)((v&127)|128));v>>=7;}b.Add((byte)v);}
    static byte[] Gauge(uint actor,uint current,uint maximum,bool zero=false,byte state=2){var b=new List<byte>(new byte[]{5,0xe0});V(b,actor);if(zero)b.AddRange(new byte[]{0,3,2});else{b.AddRange(new byte[]{3,1});b.AddRange(BitConverter.GetBytes(maximum));b.AddRange(BitConverter.GetBytes(current));b.Add(state);}return b.ToArray();}
    static byte[] Energy(uint actor){var b=new List<byte>(new byte[]{0,0x8d});V(b,actor);b.AddRange(new byte[]{1,1,3});b.AddRange(BitConverter.GetBytes(1000));return b.ToArray();}
    static byte[] Hit(uint source,uint target){var b=new List<byte>(new byte[]{4,0x38});V(b,target);V(b,6);V(b,0);V(b,source);b.AddRange(BitConverter.GetBytes(15210000));b.Add(1);V(b,2);b.AddRange(new byte[]{128,0,1,85,102,119,136,1,0,0,0});V(b,10817);V(b,400);b.AddRange(new byte[]{1,0});return b.ToArray();}
    static void Send(StaggerSignal s,byte[] b,double time,string flow="game"){s.Consume(b,0,b.Length,time,flow);}
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object value,string name){return value.GetType().GetField(name,Instance).GetValue(value);}
    static void Invoke(object value,string name){value.GetType().GetMethod(name,Instance).Invoke(value,null);}
    static void VerifyEditingSamples(string root){
        string source=Path.Combine(root,"protocol","sync-opcodes.json");root=Path.Combine(root,"editing-samples");Directory.CreateDirectory(Path.Combine(root,"protocol"));File.Copy(source,Path.Combine(root,"protocol","sync-opcodes.json"),true);
        using(var owner=new EnergyOverlay(root,true)){
            var cfg=owner.Configuration;cfg.Bounds=new Rectangle(-20000,-20000,420,4);cfg.Options.Enabled=true;cfg.Options.Fade=cfg.Options.Smooth=false;cfg.Options.CurrentText=cfg.Options.MaximumText=cfg.Options.PercentageText=true;cfg.Fps.Enabled=cfg.Ping.Enabled=cfg.Dps.Enabled=cfg.Notifications.Enabled=cfg.Stagger.Bar.Enabled=cfg.EnemyHealth.Bar.Enabled=cfg.EnemyTarget.Bar.Enabled=cfg.PlayerHealth.Bar.Enabled=cfg.PlayerMana.Bar.Enabled=false;owner.ApplyConfiguration(cfg);
            owner.Show();System.Windows.Forms.Application.DoEvents();((System.Windows.Forms.Timer)Field(owner,"timer")).Stop();
            // Only the renderer changes mode. The test receiver remains closed;
            // no capture, game process or memory reader is started.
            owner.GetType().GetField("preview",Instance).SetValue(owner,false);Invoke(owner,"UpdateBar");
            var motion=(BarMotion)Field(owner,"motion");var data=(DashSignal)Field(owner,"signal");var image=(LayeredImage)Field(owner,"image");
            Check(data.ReadSnapshot().Reading==null&&data.ReadSnapshot().DetectedMaximum==0,"Editing does not seed current/max energy into the receiver");
            Check(Math.Abs(motion.Value-.65)<.000001&&(bool)Field(owner,"imageFresh")&&(int)Field(owner,"imageFilled")==Math.Round(owner.Configuration.Bounds.Width*.65),"Unlocked energy uses colored65% sample instead of missing-data dashes");
            Check(((ProgressTextOverlay)Field(owner,"energyTexts")).Visible,"Unlocked energy shows configured sample labels");int uploads=image.Uploads;for(int i=0;i<500;i++)Invoke(owner,"UpdateBar");Check(image.Uploads==uploads,"Unchanged editing sample reuses its native bitmap");
            owner.InteractionLock.Locked=true;Check(!owner.Visible&&motion.Opacity==0&&data.ReadSnapshot().Reading==null,"Locking missing energy hides the sample and leaves actual data unknown");owner.InteractionLock.Locked=false;Check(owner.Visible&&Math.Abs(motion.Value-.65)<.000001,"Unlocking restores the sample without stale animation");
            ((EnergyBarOptions)Field(owner,"options")).Enabled=false;Invoke(owner,"UpdateBar");Check(!owner.Visible&&!((ProgressTextOverlay)Field(owner,"energyTexts")).Visible,"Disabled energy stays hidden even unlocked");
            using(var signalOwner=new EnergyOverlay(root,true)){
                var signal=new StaggerSignal();using(var bar=new StaggerOverlay(signalOwner,signal,false)){
                    var options=new StaggerOptions();options.Bar.Enabled=true;options.Bar.Fade=options.Bar.Smooth=false;options.Bounds=new Rectangle(-20000,-20000,400,6);bar.Configure(options,false);var staggerMotion=(BarMotion)Field(bar,"motion");
                    Check(bar.Visible&&Math.Abs(staggerMotion.Value-.65)<.000001&&signal.Read()==null,"Unlocked unknown stagger shows sample without inventing a gauge");Check(((ProgressTextOverlay)Field(bar,"texts")).Visible,"Stagger sample includes editable numeric labels and countdown");
                    var native=(LayeredImage)Field(bar,"image");uploads=native.Uploads;for(int i=0;i<500;i++)Invoke(bar,"Pulse");Check(native.Uploads==uploads,"Static stagger sample does not redraw continuously");bar.Configure(options,true);Check(!bar.Visible&&signal.Read()==null,"Locked unknown stagger hides its editing sample");
                    bar.Configure(options,false);double now=DpsHistory.UtcNow();Send(signal,Gauge(99,37,150),now);Invoke(bar,"Pulse");Check(Math.Abs(staggerMotion.Value-37.0/150)<.000001&&signal.Read().Current==37,"Actual partial gauge immediately replaces the sample");
                    Send(signal,Gauge(99,0,150),DpsHistory.UtcNow());Invoke(bar,"Pulse");Check(signal.Read().Current==0&&staggerMotion.Value<.05,"Confirmed zero uses actual stagger/refill, not the editing65% sample");
                    Send(signal,Gauge(99,150,150,false,0),DpsHistory.UtcNow()+.001);Invoke(bar,"Pulse");Check(signal.Read().State==0&&signal.Read().Current==150&&Math.Abs(staggerMotion.Value-.65)<.000001,"Unavailable phase can be edited without changing its actual state");bar.Configure(options,true);Check(!bar.Visible,"Locking unavailable phase cannot reveal its illustrative fill");
                    signal.Reset();Send(signal,Gauge(99,27,150),DpsHistory.UtcNow()-11);bar.Configure(options,false);Check(bar.Visible&&Math.Abs(staggerMotion.Value-.65)<.000001&&signal.Read().Current==27,"Stale gauge gets an editing sample without replacing retained readings");bar.Configure(options,true);Check(!bar.Visible,"Stale gauge sample disappears on lock");
                    options.Bar.Enabled=false;bar.Configure(options,false);Check(!bar.Visible&&!((ProgressTextOverlay)Field(bar,"texts")).Visible,"Disabled stagger does not show editing examples");
                }
            }
            owner.Close();
        }
    }
    static void VerifyVisualRefill(){
        var signal=new StaggerSignal();signal.Configure(true);Send(signal,Gauge(99,150,150),10);Send(signal,Gauge(99,0,0,true),11);
        var sample=signal.Read();foreach(double elapsed in new[]{0.0,1.25,2.5,4.999,5.0}){
            var progress=StaggerDisplayProgress.Read(sample,11+elapsed,5);
            Check(progress.Refilling&&Math.Abs(progress.Ratio-elapsed/5)<.000001&&Math.Abs(progress.Remaining-(5-elapsed))<.000001,"Visual refill follows countdown exactly");
            Check(sample.Current==0&&signal.Read().Current==0&&sample.Maximum==150,"Visual animation cannot change actual gauge/labels");
        }
        Check(StaggerDisplayProgress.Read(sample,10.999,5).Ratio==0,"Capture/UI skew cannot produce negative fill");
        Send(signal,Gauge(99,0,150),13);Check(signal.Read().Groggy&&signal.Read().GroggyStarted==11&&StaggerDisplayProgress.Read(signal.Read(),13.5,5).Ratio==.5,"Full-shaped repeated zero must preserve the existing animation origin");
        Send(signal,Gauge(99,0,0,true),14);Check(StaggerDisplayProgress.Read(signal.Read(),14,5).Ratio==.6,"Repeated zero cannot restart animation");
        var motion=new BarMotion();var options=new EnergyBarOptions{Enabled=true,AutoHide=false};motion.Step(0,true,true,0,0,150,options);motion.SnapValue(.75);motion.Step(.01,false,true,0,0,150,options);motion.SnapValue(1);Send(signal,Gauge(99,150,150),16);
        var real=StaggerDisplayProgress.Read(signal.Read(),16,5);motion.Step(.02,false,true,real.Ratio,150,150,options);Check(!real.Refilling&&real.Ratio==1&&motion.Value==1,"Actual refill takes over without jumping back or smoothing lag");
        Send(signal,Gauge(99,0,0,true),17);Send(signal,Gauge(99,100,150),18);real=StaggerDisplayProgress.Read(signal.Read(),18,5);Check(!real.Refilling&&real.Ratio==100.0/150,"Early actual positive update overrides estimate");
        Send(signal,Gauge(99,0,0,true),19);Send(signal,Gauge(99,150,150,false,0),20);Check(!StaggerDisplayProgress.Read(signal.Read(),20,5).Refilling&&!StaggerSignal.DisplayAvailable(signal.Read(),20),"Inactive phase stops animation and hides");
        Check(!StaggerDisplayProgress.Read(null,20,5).Refilling,"Unknown target cannot animate");
        Check(!StaggerDisplayProgress.Read(sample,22,5).Refilling,"Stale capture cannot animate");
        Check(StaggerDisplayProgress.Read(sample,13.5,double.NaN).Ratio==.5,"Invalid reference duration safely uses five seconds");
        Check(StaggerDisplayProgress.Read(sample,12,2).Ratio==.5,"Visual fill follows configured countdown reference");
        Check(!StaggerSignal.DisplayAvailable(sample,16),"Estimated full bar cannot bypass five-second zero guard");
    }
    internal static void Run(string root){
        VerifyEditingSamples(root);
        VerifyVisualRefill();
        VerifyLabelCache();
        var s=new StaggerSignal();int wakes=0;s.Changed=delegate{wakes++;};Send(s,Gauge(99,150,150),1);Check(s.Read()==null&&wakes==0,"Disabled reader must do no work");s.Configure(true);
        Send(s,Gauge(99,150,150),10);Check(s.Read().Current==150,"Initial full gauge");Send(s,Gauge(99,27,150),11);Send(s,Gauge(99,0,0,true),12);Check(s.Read().Current==0&&s.Read().Groggy&&s.Read().GroggyStarted==12,"Zero state");Send(s,Gauge(99,0,0,true),13);Check(s.Read().GroggyStarted==12,"Repeated state must not extend countdown");Check(s.Read().Current==0,"No timer-based fake refill");Send(s,Gauge(99,150,150),17.1);Check(s.Read().Current==150&&!s.Read().Groggy,"Actual refill ends state");Send(s,Gauge(99,20,150),11);Check(s.Read().Current==150,"Ignore old gauge");
        Check(StaggerSignal.Fresh(s.Read(),17.099),"New packet slightly ahead of UI clock must not hide gauge");Check(StaggerSignal.Fresh(s.Read(),27.1),"Retain gauge through capture grace");Check(!StaggerSignal.Fresh(s.Read(),27.2),"Inactive capture must still expire");Check(!StaggerSignal.Fresh(null,18),"No gauge must stay unknown");
        var phase=new StaggerSignal();phase.Configure(true);Send(phase,Gauge(99,100,150),100);Send(phase,Gauge(99,0,0,true),101);Check(!StaggerSignal.ZeroExpired(phase.Read(),105.999),"Zero must remain visible during five second stagger window");Send(phase,Gauge(99,0,0,true),106);Check(StaggerSignal.ZeroExpired(phase.Read(),106),"Repeated zero must not restart visibility window");Send(phase,Gauge(99,0,150),107);Check(StaggerSignal.ZeroExpired(phase.Read(),107),"Full-shaped zero cannot reopen expired zero");Send(phase,Hit(42,99),108);Check(StaggerSignal.ZeroExpired(phase.Read(),108),"Combat traffic cannot reveal expired zero");Send(phase,Gauge(99,150,150),109);Check(!StaggerSignal.ZeroExpired(phase.Read(),109)&&!phase.Read().ZeroStarted.HasValue,"New positive gauge must reveal without fake refill");Send(phase,Gauge(99,0,150),110);Check(!StaggerSignal.ZeroExpired(phase.Read(),114.999)&&StaggerSignal.ZeroExpired(phase.Read(),115),"Next phase receives an independent zero window");Check(!StaggerSignal.ZeroExpired(null,200),"Unknown gauge is not an expired state");
        var inactive=new StaggerSignal();inactive.Configure(true);Send(inactive,Energy(42),200);Send(inactive,Gauge(99,128,280),201);Send(inactive,Hit(42,99),201.1);Check(StaggerSignal.DisplayAvailable(inactive.Read(),201.1),"Active nonzero phase must display");
        Send(inactive,Gauge(99,280,280,false,0),202);Check(inactive.Read().Current==280&&inactive.Read().State==0&&!inactive.Read().Groggy&&!StaggerSignal.DisplayAvailable(inactive.Read(),202),"Actual state0 refill must replace stale128 and hide");
        Send(inactive,Gauge(99,280,280,false,1),202.1);Send(inactive,Hit(42,99),203);Check(inactive.Read().State==1&&!StaggerSignal.DisplayAvailable(inactive.Read(),203),"State1 plus combat traffic must not reopen unavailable phase");Send(inactive,Gauge(123,100,150),204);Check(inactive.Read().Actor==99&&!StaggerSignal.DisplayAvailable(inactive.Read(),204),"Unrelated active gauge cannot replace selected inactive target");
        Send(inactive,Gauge(99,128,280),201.5);Check(inactive.Read().State==1&&inactive.Read().Current==280,"Old active update cannot undo phase transition");Send(inactive,Gauge(99,280,280,false,3),205);Check(inactive.Read().State==3&&!StaggerSignal.DisplayAvailable(inactive.Read(),205),"Transitional state3 waits for validated active data");
        Send(inactive,Gauge(99,279,280),206);Check(StaggerSignal.DisplayAvailable(inactive.Read(),206)&&inactive.Read().Current==279,"Actual next active phase reveals gauge");Send(inactive,Gauge(99,0,0,true),207);Check(StaggerSignal.DisplayAvailable(inactive.Read(),211.999)&&!StaggerSignal.DisplayAvailable(inactive.Read(),212),"Active zero window still lasts five seconds");Send(inactive,Gauge(99,280,280,false,0),212.1);Check(inactive.Read().Current==280&&!inactive.Read().ZeroStarted.HasValue&&!inactive.Read().Groggy&&!StaggerSignal.DisplayAvailable(inactive.Read(),212.1),"Inactive real refill ends countdown without reopening");
        Send(inactive,Gauge(99,270,280,false,4),213);Send(inactive,Gauge(99,281,280,false,2),213);Check(inactive.Read().State==0&&inactive.Read().Current==280,"Unknown state and impossible current must not mutate phase");inactive.Reset();Check(inactive.Read()==null&&!StaggerSignal.DisplayAvailable(null,214),"Reset clears availability across sessions");
        Send(s,Energy(42),18);Send(s,Hit(42,99),19);Send(s,Gauge(123,70,100),20);Check(s.Read().Actor==99,"Other enemy must not replace attacked target");Send(s,Gauge(99,80,150),21,"other");Check(s.Read().Current==150&&s.Read().Flow=="game","Flow IDs isolated");Send(s,Hit(42,123),22);Check(s.Read().Actor==123,"Switch attacked target");Send(s,Hit(42,444),23);Check(s.Read().Actor==123,"Area hit without gauge must not hide tracked enemy");Send(s,Hit(42,123),24);var gone=new List<byte>(new byte[]{0x42,0x36});V(gone,123);Send(s,gone.ToArray(),25);Check(s.Read()==null,"Despawn cannot show an unrelated fallback gauge");
        for(int n=0;n<8;n++)Send(s,Gauge((uint)(800+n),100,150),25.1+n/100.0,"aux"+n);Check(s.Read()==null,"Retired target stays retired across bounded flows");
        s.Reset();Send(s,Energy(42),100);Send(s,Gauge(99,150,150),100);Send(s,Hit(42,99),100.1);
        for(int n=0;n<8;n++)Send(s,Gauge((uint)(800+n),100,150),100.2+n/100.0,"aux"+n);Check(s.Read().Actor==99,"Flow pressure must preserve selected gauge");
        for(uint n=200;n<340;n++)Send(s,Gauge(n,100,150),101+n/1000.0);Check(s.Read().Actor==99,"Entity pressure must preserve selected gauge");
        var keepVisible=new BarMotion();var legacyHide=new StaggerOptions();legacyHide.Bar.AutoHide=true;legacyHide.Normalize();Check(!legacyHide.Bar.AutoHide,"Legacy full-hide must be disabled for stagger");var visual=legacyHide.Bar.Copy();visual.Enabled=true;keepVisible.Step(0,true,true,1,150,150,visual,0);
        for(int n=0;n<20;n++){Send(s,Gauge(99,150,150),110+n);keepVisible.Step(n+1,false,true,1,150,150,visual,0);Check(keepVisible.Opacity==1,"Repeated full readings must not flicker");}
        keepVisible.Step(30,false,true,1,150,150,visual,11);keepVisible.Step(32,false,true,1,150,150,visual,13);Check(keepVisible.Opacity==1,"Valid full stagger gauge must stay visible after activity ends");
        s.Reset();Send(s,Gauge(99,0,0,true),26);Check(s.Read()==null,"Unknown zero must not invent maximum");var valid=Gauge(99,100,150);for(int n=0;n<valid.Length;n++){s.Reset();s.Consume(valid,0,n,30,"game");Check(s.Read()==null,"Truncated gauge accepted: "+n);}Send(s,Gauge(99,151,150),31);Send(s,Gauge(99,0,0),31);Check(s.Read()==null,"Reject impossible gauge");
        var random=new Random(25);for(int n=0;n<5000;n++){var b=new byte[random.Next(2,80)];random.NextBytes(b);b[0]=5;b[1]=0xe0;Send(s,b,32+n/1000.0);}s.Reset();Send(s,Gauge(99,100,150),40);var map=new byte[48];map[0]=0x21;map[1]=0x36;Array.Copy(BitConverter.GetBytes(1u),0,map,6,4);Send(s,map,41);Array.Copy(BitConverter.GetBytes(2u),0,map,6,4);Send(s,map,42);Check(s.Read()==null,"Map transition clears actors");s.Configure(false);Check(s.Read()==null,"Disable clears state");
        var o=new EnergyBarOptions{CurrentText=true,MaximumText=true,PercentageText=true,CurrentPosition=0,MaximumPosition=0,PercentagePosition=5};var slots=ProgressBarText.Slots(o,true,75,150);Check(slots[0]=="75  / 150"&&slots[5]=="50%","Independent positions and merged labels");foreach(var text in ProgressBarText.Slots(o,false,75,150))Check(text==null,"Unknown energy must not invent labels");
        o.MaximumOffsetY=10;var labels=ProgressBarText.Labels(o,true,75,150);Check(labels.Count==3&&labels[0].Offset==0&&labels[1].Offset==10,"Same anchor supports independent heights");var moved=o.Copy();moved.CurrentOffsetY=12;Check(!ProgressBarText.Same(o,moved),"Offsets participate in history");
        var cfg=new StaggerOptions();cfg.Bar.Enabled=true;cfg.Countdown.Color="#112233";cfg.Countdown.OffsetY=17;cfg.Bar.MaximumOffsetY=-9;var copy=cfg.Copy();copy.Countdown.FontSize=40;copy.Bar.CurrentText=false;Check(cfg.Countdown.FontSize==24&&cfg.Bar.CurrentText,"Independent snapshots");var saved=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>("{\"stagger\":"+new JavaScriptSerializer().Serialize(cfg)+"}");Check(StaggerOptions.Same(cfg,StaggerOptions.Read(saved)),"Stagger persistence");
        using(var owner=new EnergyOverlay(root,true)){var settings=new HelperSettings(owner);try{settings.VerifyProgress(root);}finally{settings.Close();}}
        File.WriteAllText(Path.Combine(root,"progress-test.txt"),"PASS: full phase states0/1/2/3; stale128 and zero refills; explicit inactive visibility and actual active resume; repeated-state countdown; linear visual refill isolated from actual gauge/labels; phase/early refill overrides; no timer-based data mutation; old/malformed/truncated envelopes; flow isolation; target change/no-gauge/despawn/map reset; disable; bounded parser fuzz; independently positioned/unknown labels; deep copies, persistence and independent EN/ES progress tabs, undo and disabled controls.");
    }

    static void VerifyLabelCache(){
        var options=new EnergyBarOptions{CurrentText=true,MaximumText=true,PercentageText=true,CurrentPosition=0,MaximumPosition=0,PercentagePosition=5};var cache=new ProgressLabelCache();var first=cache.Read(options,true,75,150);
        for(int i=0;i<10000;i++)Check(object.ReferenceEquals(first,cache.Read(options,true,75,150)),"Unchanged animation ticks must reuse formatted labels");Check(cache.Rebuilds==1,"Formatting happens once across ten thousand unchanged ticks");
        options.TextSize=40;options.TextColor="#112233";Check(object.ReferenceEquals(first,cache.Read(options,true,75,150)),"Paint changes cannot force numeric reformatting");
        options.MaximumOffsetY=10;var moved=cache.Read(options,true,75,150);Check(moved.Count==3&&moved[1].Offset==10&&!object.ReferenceEquals(first,moved),"Mutable layout change invalidates independently positioned labels");
        var changed=cache.Read(options,true,90,150);Check(changed[0].Text=="90"&&changed[2].Text=="60%","Actual values invalidate cached formatting");Check(cache.Read(options,false,90,150).Count==0&&cache.Read(options,true,90,0).Count==0,"Unknown or zero maximum cannot retain old labels");
        options.CurrentText=false;options.MaximumText=false;options.PercentageText=false;Check(cache.Read(options,true,90,150).Count==0,"Disabling labels clears previous values");options.CurrentText=true;var english=cache.Read(options,true,12345,20000,System.Globalization.CultureInfo.GetCultureInfo("en-US"));var spanish=cache.Read(options,true,12345,20000,System.Globalization.CultureInfo.GetCultureInfo("es-ES"));Check(english[0].Text=="12,345"&&spanish[0].Text=="12.345","Changing language cannot reuse another locale's numbers");
    }
}
