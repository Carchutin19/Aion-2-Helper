using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class StaggerScreenEffectVerification {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static object Field(object value,string name){return value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 static void Send(StaggerSignal s,byte[] b,double time,string flow="game"){ProgressVerification.Send(s,b,time,flow);}
 static StaggerSignal Ready(double time){var signal=new StaggerSignal();signal.Configure(true);Send(signal,ProgressVerification.Energy(42),time);Send(signal,ProgressVerification.Gauge(99,100,150),time);Send(signal,ProgressVerification.Hit(42,99),time+.01);return signal;}
 static void Zero(StaggerSignal s,double time,bool compact=true,uint actor=99,string flow="game"){Send(s,ProgressVerification.Gauge(actor,0,150,compact),time,flow);}
 static void VerifyTriggers(){
  var s=Ready(100);var gate=new StaggerScreenGate();gate.Configure(true,s.Read());Check(s.Read().ConfirmedTarget,"Real identity and attacked target confirm the gauge");
  Zero(s,101);Check(gate.Take(s.Read(),101.01),"Confirmed active positive-to-zero starts one effect");long serial=s.Read().BurstSerial;Check(!gate.Take(s.Read(),101.02),"Reading the same event cannot replay it");
  Zero(s,101.10);Zero(s,101.15,false);Check(s.Read().BurstSerial==serial&&!gate.Take(s.Read(),101.2),"Repeated compact and full zeros cannot restart the effect");
  Send(s,ProgressVerification.Gauge(99,150,150),102);Zero(s,103,false);Check(gate.Take(s.Read(),103.1),"A new positive-to-full-zero event can play again");
  var late=new StaggerScreenGate();late.Configure(true,s.Read());Check(!late.Take(s.Read(),103.1),"Enabling on a zero gauge never replays an old stagger");
  gate.Configure(false,s.Read());Send(s,ProgressVerification.Gauge(99,150,150),104);Zero(s,105);Check(!gate.Take(s.Read(),105.1),"Disabled animation cannot trigger");gate.Configure(true,s.Read());Check(!gate.Take(s.Read(),105.1),"Re-enabling skips events received while disabled");
  Send(s,ProgressVerification.Gauge(99,150,150),106);Zero(s,107);Check(!gate.Take(s.Read(),108),"Delayed historical events are not displayed");
  Send(s,ProgressVerification.Gauge(99,150,150),109);Zero(s,108.9);Check(s.Read().Current==150,"Out-of-order zero does not change the live gauge");
  Send(s,ProgressVerification.Gauge(99,150,150,false,0),110);Zero(s,111);Check(!gate.Take(s.Read(),111.1),"An unavailable phase reaching zero is not proof of a new stagger");
  var initial=new StaggerSignal();initial.Configure(true);Send(initial,ProgressVerification.Energy(42),200);Zero(initial,200,false);Send(initial,ProgressVerification.Hit(42,99),200.01);var initialGate=new StaggerScreenGate();initialGate.Configure(true,null);Check(!initialGate.Take(initial.Read(),200.1),"Initial zero has no observed positive transition");
  var fallback=new StaggerSignal();fallback.Configure(true);Send(fallback,ProgressVerification.Gauge(99,100,150),200);Zero(fallback,201);Check(!fallback.Read().ConfirmedTarget&&fallback.Read().BurstSerial==0,"Provisional fallback never raises a player-specific event");
  s=Ready(300);gate=new StaggerScreenGate();gate.Configure(true,s.Read());Send(s,ProgressVerification.Gauge(123,100,150),300.1);Zero(s,301,true,123);Zero(s,301.1,true,99,"other");Check(s.Read().Current==100&&!gate.Take(s.Read(),301.2),"Other targets and TCP flows cannot start this enemy's effect");
  Zero(s,302);long epoch=s.Read().Epoch;var map=new byte[48];map[0]=0x21;map[1]=0x36;Array.Copy(BitConverter.GetBytes(1u),0,map,6,4);Send(s,map,302.1);Array.Copy(BitConverter.GetBytes(2u),0,map,6,4);Send(s,map,302.2);Check(s.Read()==null&&!gate.Take(s.Read(),302.3),"Map transition removes pending old enemy data");
  Send(s,ProgressVerification.Gauge(99,150,150),303);Send(s,ProgressVerification.Hit(42,99),303.01);Check(s.Read().Epoch>epoch,"Map transition changes the animation context");Zero(s,304);Check(gate.Take(s.Read(),304.1),"Fresh map events are still accepted");
  Send(s,new byte[]{0x42,0x36,99},304.2);Check(s.Read()==null,"Despawn removes the animation target");s.Reset();Check(s.Read()==null,"Reset clears all animation data");
 }
 static void VerifyOptions(){
  var original=new StaggerOptions();Check(!original.ScreenEffect.Enabled,"Screen effect is opt-in");var copy=original.Copy();copy.ScreenEffect.Enabled=true;copy.ScreenEffect.TextColor="#112233";Check(!original.ScreenEffect.Enabled&&original.ScreenEffect.TextColor!="#112233","History owns independent effect options");
  var json=new JavaScriptSerializer();var saved=json.Deserialize<Dictionary<string,object>>("{\"stagger\":"+json.Serialize(copy)+"}");Check(StaggerOptions.Same(copy,StaggerOptions.Read(saved)),"Colors and activation survive save/load");var old=StaggerOptions.Read(json.Deserialize<Dictionary<string,object>>("{\"stagger\":{\"Countdown\":{\"ReferenceSeconds\":6}}}"));Check(!old.ScreenEffect.Enabled&&old.Countdown.ReferenceSeconds==6,"Old settings retain existing countdown and default effect off");
  copy.ScreenEffect.TextColor="invalid";copy.ScreenEffect.EffectColor=null;copy.Normalize();Check(copy.ScreenEffect.TextColor==original.ScreenEffect.TextColor&&copy.ScreenEffect.EffectColor==original.ScreenEffect.EffectColor,"Invalid colors recover reference defaults");
 }
 static long Ink(Bitmap image){long alpha=0;for(int y=0;y<image.Height;y+=3)for(int x=0;x<image.Width;x+=3)alpha+=image.GetPixel(x,y).A;return alpha;}
 static void VerifyPainting(string root){
  string renders=Path.Combine(root,"effect-frames");Directory.CreateDirectory(renders);var options=new StaggerScreenOptions();
  using(var painter=new StaggerScreenPainter())using(var frame=new Bitmap(600,412,PixelFormat.Format32bppPArgb)){
   painter.Paint(frame,-.1,options);Check(Ink(frame)==0,"Effect starts transparent");painter.Paint(frame,.1,options);long peak=Ink(frame);Check(peak>10000&&frame.GetPixel(0,0).A==0&&frame.GetPixel(599,411).A==0,"Screen artwork has no opaque background rectangle");frame.Save(Path.Combine(root,"stagger-impact.png"),ImageFormat.Png);
   painter.Paint(frame,.65,options);Check(Ink(frame)>0&&Ink(frame)<peak,"Opening impact gives way to a readable quiet hold");frame.Save(Path.Combine(root,"stagger-hold.png"),ImageFormat.Png);int before=frame.GetPixel(300,206).ToArgb();options.TextColor="#AADDFF";options.EffectColor="#2288FF";painter.Paint(frame,.65,options);Check(frame.GetPixel(300,206).ToArgb()!=before,"User colors change the rendered effect");frame.Save(Path.Combine(root,"stagger-blue.png"),ImageFormat.Png);
   options=new StaggerScreenOptions();for(int i=0;i<60;i++){painter.Paint(frame,i/40.0,options);frame.Save(Path.Combine(renders,"frame-"+i.ToString("D3")+".png"),ImageFormat.Png);}painter.Paint(frame,StaggerScreenPainter.Duration,options);Check(Ink(frame)==0,"Effect ends fully transparent");
  }
  var times=new List<object>();using(var painter=new StaggerScreenPainter())foreach(var size in new[]{new Size(1920,1080),new Size(3840,2160)})using(var frame=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb)){
   var watch=Stopwatch.StartNew();for(int i=0;i<12;i++)painter.Paint(frame,.02+i*.025,options);watch.Stop();times.Add(new{width=size.Width,height=size.Height,millisecondsPerFrame=watch.Elapsed.TotalMilliseconds/12});painter.Paint(frame,.12,options);frame.Save(Path.Combine(root,"stagger-"+size.Width+".png"),ImageFormat.Png);
  }File.WriteAllText(Path.Combine(root,"paint-timing.json"),new JavaScriptSerializer().Serialize(times));
 }
 static void Pump(int milliseconds){var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();Thread.Sleep(5);}}
 static void VerifyLifecycle(){
  var signal=Ready(DpsHistory.UtcNow());var before=signal.Read();using(var effect=new StaggerScreenEffect(signal.Read,true)){
   var options=new StaggerScreenOptions();effect.Configure(options,true,signal.Read());Check(effect.Frames==0&&!((System.Windows.Forms.Timer)Field(effect,"timer")).Enabled,"Disabled preview has no render timer");effect.Test(new Rectangle(0,0,600,412));Check(effect.Active&&effect.Testing&&effect.Tests==1&&!options.Enabled,"Test button works while automatic effect remains off");Check((OverlayNative.GetWindowLong(effect.Handle,-20)&0x080800A0)==0x080800A0,"Effect is layered, click-through, tool window, no-activate");
   Pump(1600);Check(!effect.Active&&!effect.Visible&&!((System.Windows.Forms.Timer)Field(effect,"timer")).Enabled&&Field(effect,"frame")==null&&Field(effect,"image")==null,"One-shot test stops timer and releases full viewport surfaces");int frames=effect.Frames;Pump(80);Check(frames==effect.Frames,"Idle effect performs no repaint");var after=signal.Read();Check(after.Current==before.Current&&after.Maximum==before.Maximum&&after.BurstSerial==before.BurstSerial,"Testing leaves live gauge and trigger data intact");
   effect.Test(Rectangle.Empty);effect.Test(Rectangle.Empty);Check(effect.Testing&&effect.Tests==3,"Repeated test clicks restart a single effect");effect.Stop();Check(!effect.Active&&!effect.Testing,"Stopping clears the test state");
  }
 }
 internal static void Run(string root){
  VerifyTriggers();VerifyOptions();VerifyPainting(root);VerifyLifecycle();using(var owner=new EnergyOverlay(root,true)){var settings=new HelperSettings(owner);try{settings.VerifyStaggerEffect(root);}finally{settings.Close();}}
  File.WriteAllText(Path.Combine(root,"stagger-effect-test.txt"),"PASS: selected positive-to-zero events; full/compact zeros; deduplication; stale/inactive/unidentified/foreign/map/reset/enable guards; independent persisted options; transparent procedural art; custom colors; 1080p/4K renders; one-shot offscreen preview; native no-activate/click-through; zero idle rendering; surface release; live gauge isolation; EN/ES settings, test action and undo.");
 }
}

