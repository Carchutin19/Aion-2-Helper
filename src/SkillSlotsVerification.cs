using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

internal static class SkillSlotsVerification {
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);}
 static byte[] Hex(string value){var b=new byte[value.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(value.Substring(i*2,2),16);return b;}
 static byte[] Bar(){var b=new byte[3+42*2+25];b[0]=0;b[1]=0x56;b[2]=3;for(int n=0;n<2;n++){int p=3+n*42;b[p]=1;b[p+1]=(byte)(n==0?0:7);b[p+19]=4;uint skill=SkillSlotsOverlay.ExampleSkills[n];Buffer.BlockCopy(BitConverter.GetBytes(skill),0,b,p+15,4);Buffer.BlockCopy(BitConverter.GetBytes(skill),0,b,p+20,4);}b[87]=0;b[88]=8;b[89]=1;return b;}
 static byte[] Profile(){var b=new byte[128];b[0]=0x33;b[1]=0x36;b[2]=12;b[3]=6;Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("Tester"),0,b,4,6);Buffer.BlockCopy(BitConverter.GetBytes(10),0,b,12,4);b[16]=1;return b;}
 static byte[] Cd(uint skill,uint raw){var b=new List<byte>{0x47,0x38,1};b.AddRange(BitConverter.GetBytes(skill));do{byte n=(byte)(raw&127);raw>>=7;b.Add(raw==0?n:(byte)(n|128));}while(raw!=0);return b.ToArray();}
 static void Feed(SkillSlotsSignal signal,byte[] b,double t,string flow="self"){signal.Consume(b,0,b.Length,t,flow);}
 static void Var(List<byte> b,uint v){do{byte n=(byte)(v&127);v>>=7;b.Add(v==0?n:(byte)(n|128));}while(v!=0);}
 static byte[] Mark(uint actor,uint instance,double until,uint effect=157100071){var b=new List<byte>{0x2a,0x38};Var(b,actor);b.Add(1);b.Add(0);Var(b,instance);b.AddRange(BitConverter.GetBytes(effect));b.AddRange(BitConverter.GetBytes(5000L));b.AddRange(BitConverter.GetBytes((long)Math.Round((until-2.6)*1000)));b.Add(1);b.Add(0);return b.ToArray();}
 static byte[] Clock(double local){return new byte[]{0,0x36}.Concat(BitConverter.GetBytes((long)Math.Round((local-2.6)*1000))).ToArray();}
 static byte[] Target(uint actor){var b=new List<byte>{0x31,0x38,0,0};Var(b,actor);b.Add(0);b.Add(0);return b.ToArray();}
 static void ContextRegression(double now){var ledger=new SkillSlotContext{Enabled=true};var clock=Clock(now);var target=Target(200);var mark=Mark(200,7,now+5);ledger.Consume(target,0,target.Length,now);ledger.Consume(mark,0,mark.Length,now);Check(ledger.Until==0,"No server-clock anchor means unknown expiry, not assumed local UTC");ledger.Consume(clock,0,clock.Length,now);ledger.Consume(mark,0,mark.Length,now);Check(Math.Abs(ledger.Until-(now+5))<.002,"The real2.6second server/client skew cannot prematurely expire Fire Mark");clock=Clock(now+1);ledger.Consume(clock,0,clock.Length,now+1);ledger.Consume(mark,0,mark.Length,now+1);Check(Math.Abs(ledger.Until-(now+5))<.002,"Repeated status snapshot cannot extend the original server deadline");var signal=new SkillSlotsSignal();signal.Configure(true);var bar=Bar();Buffer.BlockCopy(BitConverter.GetBytes(15280000u),0,bar,18,4);Buffer.BlockCopy(BitConverter.GetBytes(15280000u),0,bar,23,4);Buffer.BlockCopy(BitConverter.GetBytes(15050000u),0,bar,27,4);Feed(signal,Profile(),now);Feed(signal,bar,now);Feed(signal,Clock(now),now);Feed(signal,Target(200),now);var v=signal.Read(now);Check(v.Slots[0].Alternative==15050000,"Context alternative comes from the actual assigned bar");
 Feed(signal,Cd(15280000,13850),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Primary CD alone cannot enable the alternative");
 Feed(signal,Mark(201,7,now+5),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"A mark on another target cannot enable Blaze");
 Feed(signal,Mark(200,7,now+5,153900101),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Other fire debuffs are not Fire Mark");
 Feed(signal,Mark(200,7,now+5),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15050000,"Selected target Fire Mark plus received primary CD selects assigned Blaze");
 Feed(signal,Cd(15050000,4600),now);v=signal.Read(now);uint displayed=SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now);Check(displayed==15050000&&SkillSlotsSignal.For(v,displayed).Raw==4600,"Alternative uses its own cooldown rather than the primary timer");
 Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now+5.01)==15280000,"Absolute mark expiry restores the primary without a new packet");
 Feed(signal,Cd(15280000,0),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Fire Mark alone cannot replace a ready primary");Feed(signal,Cd(15280000,13850),now);
 Feed(signal,Target(0),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Explicit target clear disables the conditional icon");Feed(signal,Target(200),now);
 var removal=new List<byte>{0x2c,0x38};Var(removal,200);removal.Add(1);removal.Add(0);Var(removal,7);removal.Add(1);byte[] full=removal.ToArray();for(int n=2;n<full.Length;n++){Feed(signal,full.Take(n).ToArray(),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15050000,"Truncated removals cannot clear a valid mark");}Feed(signal,full,now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Explicit effect-instance removal clears conditional availability");
 Feed(signal,Mark(200,8,now+5),now,"foreign");v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Foreign flow mark cannot enable own context");Feed(signal,Mark(200,8,now+5),now);signal.Reset();Feed(signal,Profile(),now);Feed(signal,bar,now);Feed(signal,Cd(15280000,13850),now);Feed(signal,Target(200),now);v=signal.Read(now);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0],now)==15280000,"Transport reset cannot carry a previous target mark");
 }
 static void EvasionRegression(double now){
  var signal=new SkillSlotsSignal();signal.Configure(true);
  var bar=Bar();Buffer.BlockCopy(BitConverter.GetBytes(13340000u),0,bar,18,4);Buffer.BlockCopy(BitConverter.GetBytes(13340000u),0,bar,23,4);Buffer.BlockCopy(BitConverter.GetBytes(13210000u),0,bar,27,4);
  Feed(signal,Profile(),now);Feed(signal,bar,now);Feed(signal,Clock(now),now);
  Feed(signal,Mark(12,7,now+4,SkillSlotContext.EvasionMarkerA),now);
  Feed(signal,Mark(13,8,now+4,SkillSlotContext.EvasionMarkerB),now);
  Check(signal.Read(now).EvasionUntil==0,"A single self marker cannot pair with another actor's marker");
  Feed(signal,Mark(12,8,now+5,SkillSlotContext.EvasionMarkerB),now);
  Check(signal.Read(now).EvasionUntil==0,"Markers with different server deadlines cannot activate Whirlwind Slice");
  Feed(signal,Mark(12,7,now+5,SkillSlotContext.EvasionMarkerA),now);
  var view=signal.Read(now);var slot=view.Slots[0];
  Check(SkillSlotsSignal.DisplaySkill(view,slot,now)==13210000,"Matching self markers activate the assigned Whirlwind Slice");
  Check(SkillSlotsSignal.DisplaySkill(view,slot,now+5.01)==13340000,"The server deadline ends the evasion variant without another packet");
  Feed(signal,Cd(13210000,10000),now);
  Check(SkillSlotsSignal.DisplaySkill(signal.Read(now),slot,now)==13340000,"A cooling evasion variant does not replace the base icon");
 }
 static byte[] Map(uint id){var b=new byte[48];b[0]=0x21;b[1]=0x36;Buffer.BlockCopy(BitConverter.GetBytes(id),0,b,6,4);return b;}
 static void CharacterSwitchRegression(){
  SplitEntryRegression();
  var signal=new SkillSlotsSignal();signal.Configure(true);
  Feed(signal,Map(1010),1);Feed(signal,Profile(),1);Feed(signal,Bar(),1);Feed(signal,Cd(15210000,42000),2);
  Feed(signal,DashSignal.LoginFixture(34),3);
  Check(signal.Read(3)==null,"Same-map login clears the visible old character and its cooldowns");
  var fresh=Bar();Buffer.BlockCopy(BitConverter.GetBytes(13100000u),0,fresh,18,4);
  Buffer.BlockCopy(BitConverter.GetBytes(13100000u),0,fresh,23,4);
  Feed(signal,Map(1010),4);Feed(signal,fresh,5);Feed(signal,Profile(),5);
  Check(signal.Read(5)==null,"The old profile cannot publish the new character's staged bar");
  var profile=EnemyTargetVerification.Player(34,"Other",100,200,true);
  Feed(signal,profile,5);var other=signal.Read(5);
  Check(other!=null&&other.Self==34&&other.Slots[0].Skill==13100000&&other.Cooldowns.Count==0,"Full bar before identity belongs to the newly confirmed character");
  var partial=(byte[])fresh.Clone();partial[0]=5;Array.Clear(partial,18,17);
  Feed(signal,partial,6);other=signal.Read(6);
  Check(other.Slots.Count==1&&other.Slots[0].Index==7,"A bounded empty skill entry clears its slot while the other records are accepted");
  partial[22]=3;
  List<SkillSlotsSignal.Slot> slots;
  Check(!SkillSlotsSignal.Bar(partial,0,partial.Length,out slots),"An unknown empty entry marker cannot clear a slot");
  signal.Reset();Feed(signal,Profile(),10);Feed(signal,Bar(),10);Feed(signal,Cd(15210000,42000),11);
  Feed(signal,fresh,12);Feed(signal,profile,12);other=signal.Read(12);
  Check(other!=null&&other.Self==34&&other.Slots[0].Skill==13100000&&other.Cooldowns.Count==0,"A full bar from the same captured batch survives a new profile even if the login header was missed");
 }
 static void SplitEntryRegression(){
  var next=EnemyTargetVerification.Player(34,"Other",100,200,true);
  var fresh=Bar();Buffer.BlockCopy(BitConverter.GetBytes(13100000u),0,fresh,18,4);Buffer.BlockCopy(BitConverter.GetBytes(13100000u),0,fresh,23,4);
  foreach(double delay in new[]{.000001,.1,5.0}){
   var signal=new SkillSlotsSignal();signal.Configure(true);Feed(signal,Map(1010),1);Feed(signal,Profile(),1);Feed(signal,Bar(),1);Feed(signal,Cd(15210000,42000),2);
   Feed(signal,Map(1010),3);Feed(signal,fresh,4);
   Check(signal.Read(4)==null,"A staged entry bar is not displayed under the previous character preset");
   Feed(signal,next,4+delay);var view=signal.Read(4+delay);
   Check(view!=null&&view.Self==34&&view.Slots[0].Skill==13100000&&view.Cooldowns.Count==0,"Same-map entry preserves the NEW full bar across distinct packet timestamps");
   Feed(signal,Profile(),10);
   Check(signal.Read(10)==null,"An unrelated later identity cannot reuse the previously consumed entry bar");
  }
  foreach(bool partialOnly in new[]{false,true}){
   var signal=new SkillSlotsSignal();signal.Configure(true);Feed(signal,Map(1010),1);Feed(signal,Profile(),1);Feed(signal,Bar(),1);Feed(signal,Map(1010),2);
   if(partialOnly){var partial=(byte[])fresh.Clone();partial[0]=5;Feed(signal,partial,3);}else Feed(signal,fresh,3,"foreign");
   Feed(signal,next,4);Check(signal.Read(4)==null,"Neither an early partial nor a full bar on another flow can transfer the old layout");
  }
 }
 static byte[] PlainCast(byte actor,uint skill){
  var data=new byte[18];data[0]=2;data[1]=0x38;data[2]=actor;
  Buffer.BlockCopy(BitConverter.GetBytes(skill),0,data,4,4);
  return data;
 }
 static void CastFlashRegression(){
  var signal=new SkillSlotsSignal();signal.Configure(true,true);
  Feed(signal,Map(1010),1);Feed(signal,Profile(),1);Feed(signal,Bar(),1);
  Feed(signal,Cd(15210000,1000),1.1);
  Check(signal.Read(1.1).Slots[0].CastAt==0,"A cooldown correction never impersonates a cast");
  Feed(signal,PlainCast(13,15210040),1.2);
  Check(signal.Read(1.2).Slots[0].CastAt==0,"Another player's cast cannot flash our bar");
  Feed(signal,PlainCast(12,15210040),2);
  var view=signal.Read(2);
  Check(view.Slots[0].CastAt==2&&view.Slots[1].CastAt==0,"An own zero-cooldown cast flashes only its assigned slot");
  Feed(signal,PlainCast(12,15210040),2.05);
  Check(signal.Read(2.05).Slots[0].CastAt==2,"Rapid components cannot keep a single flash continuously on");
  Feed(signal,PlainCast(12,15210008),2.2);
  Check(signal.Read(2.2).Slots[0].CastAt==2,"Passive components cannot flash a skill merely because they share its family");
  var chain=new List<byte>{0x18,0x38};chain.AddRange(BitConverter.GetBytes(15210000u));chain.Add(1);
  chain.AddRange(BitConverter.GetBytes(15050000u));
  Feed(signal,chain.ToArray(),3);Feed(signal,PlainCast(12,15050040),3.1);
  Check(signal.Read(3.1).Slots[0].CastAt==3.1,"A known chain variant flashes the same slot");
  Feed(signal,Map(200003),4);Feed(signal,Profile(),4.1);
  Check(signal.Read(4.1).Slots[0].CastAt==0,"Zone loading cannot replay the previous cast flash");
  Check(SkillSlotMotion.CastFlashRemaining(5,0,5.05)>.05
      &&SkillSlotMotion.CastFlashRemaining(5,4.9,5.05)==0
      &&SkillSlotMotion.CastFlashRemaining(5,0,5.2)==0,
      "A cast flashes briefly without duplicating a recent physical press");
 }
 static void MapTransitionRegression(){
  var signal=new SkillSlotsSignal();signal.Configure(true);double now=100;var original=Bar();uint skill=SkillSlotsOverlay.ExampleSkills[0];
  Feed(signal,Map(1010),now);Feed(signal,Profile(),now);Feed(signal,original,now);Feed(signal,Cd(skill,42000),now);
  var chain=new List<byte>{0x18,0x38};chain.AddRange(BitConverter.GetBytes(skill));chain.Add(1);chain.AddRange(BitConverter.GetBytes(15050000u));Feed(signal,chain.ToArray(),now);
  var oldView=signal.Read(now);Feed(signal,Map(200003),now+1);Check(signal.Read(now+1)==null&&oldView.Slots.Count==2,"Map loading suspends the received bar without mutating the old immutable view");
  Feed(signal,Profile(),now+2,"foreign");Check(signal.Read(now+2)==null,"A different flow cannot resume the suspended bar");Feed(signal,Profile(),now+3);var view=signal.Read(now+3);
  Check(view!=null&&view.Map==200003&&view.Slots.Count==2&&view.Slots[0].Skill==skill,"Same-flow same-actor confirmation restores assignments without another full0056");Check(Math.Abs(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(view,skill),now+3)-39)<.001,"Zone loading cannot restart or discard received cooldown anchors");Check(view.Chains.Count==0&&view.ContextTarget==0&&view.FireMarkUntil==0,"A new map cannot retain old enemy chains or conditional marks");
  Feed(signal,Map(1010),now+4);Check(signal.Read(now+4)==null,"Leaving the instance also awaits own identity");Feed(signal,Profile(),now+5);view=signal.Read(now+5);Check(view!=null&&view.Map==1010&&view.Slots.Count==2&&Math.Abs(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(view,skill),now+5)-37)<.001,"Returning to the world restores the same bar and continued cooldown");
  Feed(signal,Map(200003),now+6);var partial=Bar();partial[0]=5;Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,partial,18,4);Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,partial,23,4);Feed(signal,partial,now+7);Check(signal.Read(now+7)==null,"Loading-time updates remain hidden until identity is confirmed");Feed(signal,Profile(),now+8);Check(signal.Read(now+8).Slots[0].Skill==15110000,"A received partial assignment update is retained during loading");
  var other=Profile();other[2]=13;Feed(signal,Map(1010),now+9);Feed(signal,other,now+10);Check(signal.Read(now+10)==null,"A different actor cannot inherit the old bar or loading-time partials");Feed(signal,original,now+11);Check(signal.Read(now+11).Cooldowns.Count==0,"New character starts from its own bar without old cooldowns");
  Feed(signal,Map(200003),now+12);var replacement=Bar();Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,replacement,18,4);Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,replacement,23,4);Feed(signal,replacement,now+13);Feed(signal,Profile(),now+14);view=signal.Read(now+14);Check(view!=null&&view.Self==12&&view.Slots[0].Skill==15110000,"A fresh full bar received before a different actor's profile overrides the old layout");
  var reused=new SkillSlotsSignal();reused.Configure(true);Feed(reused,Map(1010),now);Feed(reused,Profile(),now);Feed(reused,original,now);Feed(reused,Map(200003),now+1);var changedName=Profile();Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("OtherX"),0,changedName,4,6);Feed(reused,changedName,now+2);Check(reused.Read(now+2)==null,"A reused actor ID with a different character name cannot inherit the suspended bar");
  Feed(signal,Map(1010),now+15);Feed(signal,Map(200003),now+16);Feed(signal,Profile(),now+17);Check(signal.Read(now+17)!=null,"Repeated loading maps cannot erase a suspended known layout");Feed(signal,Profile(),now+18,"new-connection");Check(signal.Read(now+18)==null,"A new TCP flow never borrows a previous flow's layout");
  Feed(signal,Profile(),now+19);Feed(signal,Map(1010),now+20);signal.Reset();Feed(signal,Profile(),now+21);Check(signal.Read(now+21)==null,"Transport reset revokes the suspended layout");Feed(signal,original,now+22);Feed(signal,Map(200003),now+23);signal.Configure(false);signal.Configure(true);Feed(signal,Profile(),now+24);Check(signal.Read(now+24)!=null,"Visibility changes during map loading preserve assignments after own identity is confirmed");
 }
 static void ReceptionClockRegression(){double now=1000;var signal=new SkillSlotsSignal();signal.ReceiptClock=delegate{return now;};signal.Configure(true);Feed(signal,Profile(),now+1.5);Feed(signal,Bar(),now+1.5);Check(signal.Read(now)!=null,"Npcap clock ahead cannot hide a received live bar");Feed(signal,Cd(15210000,42000),now+1.5);var view=signal.Read(now);var cd=SkillSlotsSignal.For(view,15210000);Check(cd!=null&&Math.Abs(SkillSlotsSignal.Remaining(cd,now)-42)<.001,"Received duration anchors to reception, not drifted capture clock");now+=1;Feed(signal,Clock(now),now-20);view=signal.Read(now);Check(view!=null&&Math.Abs(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(view,15210000),now)-41)<.001,"Capture clock moving backwards cannot freeze live reception");Check(signal.Read(now+16)==null,"Real stale reception still hides the bar");signal.Reset();Check(signal.Read(now)==null,"Clock fix cannot retain bar across transport reset");}
 static void ConnectionTransitionRegression(){
  const string name="SameCharacter";
  var signal=new SkillSlotsSignal();signal.Configure(true,true);
  var profile=EnemyTargetVerification.Player(18,name,100,200,true);
  Feed(signal,profile,1,"instance");Feed(signal,Bar(),1,"instance");
  Feed(signal,Cd(SkillSlotsOverlay.ExampleSkills[0],42000),2,"instance");
  var partial=Bar().Take(45).ToArray();partial[0]=5;partial[2]=1;
  uint replacement=15110000;
  Buffer.BlockCopy(BitConverter.GetBytes(replacement),0,partial,18,4);
  Buffer.BlockCopy(BitConverter.GetBytes(replacement),0,partial,23,4);
  Feed(signal,partial,220,"world");
  Check(signal.Read(220)==null,"A partial on a new flow cannot publish an unconfirmed layout");
  Feed(signal,EnemyTargetVerification.Player(13014,name,100,200,true),235,"world");
  var view=signal.Read(235);
  Check(view!=null&&view.Flow=="world"&&view.Self==13014&&view.Slots.Count==2
      &&view.Slots[0].Skill==replacement&&view.Slots[1].Skill==SkillSlotsOverlay.ExampleSkills[1]
      &&view.Cooldowns.Count==0&&view.Chains.Count==0,"Confirmed dungeon exit resumes assignments and merges early partials, without carrying combat state");
  var differentServer=EnemyTargetVerification.Player(23,name,100,200,true);
  differentServer[9+name.Length]=0x16;
  Feed(signal,partial,236,"other-server");Feed(signal,differentServer,237,"other-server");
  Check(signal.Read(237)==null,"Identical names on different servers cannot inherit assignments");
  Feed(signal,EnemyTargetVerification.Player(13014,name,100,200,true),238,"world");
  Feed(signal,DashSignal.LoginFixture(34),239,"explicit-login");
  Feed(signal,EnemyTargetVerification.Player(34,name,100,200,true),240,"explicit-login");
  Check(signal.Read(240)==null,"An explicit character login must wait for its own full bar");
  Feed(signal,Bar(),241,"explicit-login");
  Feed(signal,partial,242,"world2");Feed(signal,Bar(),243,"world2");
  Feed(signal,EnemyTargetVerification.Player(35,name,100,200,true),244,"world2");
  Check(signal.Read(244).Slots[0].Skill==SkillSlotsOverlay.ExampleSkills[0],"A new complete bar supersedes both cached assignments and early partials");
 }
 internal static void Regression(string root){ReceptionClockRegression();MapTransitionRegression();ConnectionTransitionRegression();CharacterSwitchRegression();CastFlashRegression();ContextRegression(DpsHistory.UtcNow());EvasionRegression(DpsHistory.UtcNow());var signal=new SkillSlotsSignal();signal.Configure(true);double now=DpsHistory.UtcNow();Feed(signal,Profile(),now);Feed(signal,Bar(),now);var cast=Hex("02380C00AFCCE500C402EAFC0171921BC3B924EFC71916D0C70080C0469E550180C502");uint actor,skill,ms;bool charges;
 using(var icons=new SkillSlotAssets()){Check(icons.Icon(15210340)!=null&&icons.Icon(15030340)!=null&&icons.Icon(15250340)!=null,"All three chain stages have actual bundled icons");}
 Check(SkillSlotsOverlay.Counter(null,now,true)==null,"Missing cooldown data must not draw a question mark, zero or invented countdown");Check(SkillSlotsOverlay.Counter(new SkillSlotsSignal.Cooldown{Raw=41600,Peak=41600,At=now},now,true)=="42","Received milliseconds become the actual displayed countdown");
 Check(SkillSlotsSignal.Cast(cast,0,cast.Length,out actor,out skill,out ms,out charges)&&actor==12&&skill==15060143&&ms==41600&&!charges,"Captured charged release has explicit cooldown milliseconds");for(int i=0;i<cast.Length;i++)Check(!SkillSlotsSignal.Cast(cast,0,i,out actor,out skill,out ms,out charges),"A truncated cast cannot start a cooldown");
 Feed(signal,cast,now+.1);var view=signal.Read(now+.1);var cd=SkillSlotsSignal.For(view,15060000);Check(cd!=null&&cd.Raw==41600&&Math.Ceiling(SkillSlotsSignal.Remaining(cd,now+.1))==42,"Captured own Hellfire cast must start its server-provided41600ms cooldown before a buff/correction");Check(signal.Read(now+.099)!=null,"A capture timestamp one millisecond ahead of the UI clock cannot hide all slots");Check(Math.Abs(SkillSlotsSignal.Remaining(cd,now+1.1)-40.6)<.001,"Cooldown counts down in server milliseconds");
 var foreign=(byte[])cast.Clone();foreign[2]=13;Feed(signal,foreign,now+.2);Check(SkillSlotsSignal.For(signal.Read(now+.2),15060000).At==now+.1,"Another actor on the same connection cannot start own cooldowns");Feed(signal,cast,now+.3,"foreign-flow");Check(SkillSlotsSignal.For(signal.Read(now+.3),15060000).At==now+.1,"A foreign connection cannot start own cooldowns");
 var noCooldown=cast.Take(cast.Length-3).Concat(new byte[]{0}).ToArray();Feed(signal,noCooldown,now+.4);Check(SkillSlotsSignal.For(signal.Read(now+.4),15060000).Raw==41600,"Zero-duration multistage component cannot erase a running cooldown");Feed(signal,Cd(15060000,30550),now+.5);cd=SkillSlotsSignal.For(signal.Read(now+.5),15060000);Check(Math.Abs(SkillSlotsSignal.Remaining(cd,now+.5)-30.55)<.001,"Buff correction rebases the countdown with its remaining milliseconds");Feed(signal,Cd(15060000,0),now+.6);Check(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(signal.Read(now+.6),15060000),now+.6)==0,"Explicit reset is immediate");Feed(signal,cast,now+.7);cd=SkillSlotsSignal.For(signal.Read(now+.7),15060000);Check(SkillSlotsSignal.Remaining(cd,now+43)==0,"Server interval expiry restores ready without waiting for an unsolicited correction");
 Feed(signal,Hex("18386417E800014458E500"),now+.8);view=signal.Read(now+.8);Check(SkillSlotsSignal.DisplaySkill(view,view.Slots[0])==15030340,"First chain window replaces Flame Arrow with Burst");Feed(signal,Hex("18384458E50001A4B3E800"),now+.9);Feed(signal,Hex("1938014458E500"),now+.9);view=signal.Read(now+.9);Check(SkillSlotsSignal.DisplaySkill(view,view.Slots[0])==15250340,"Opening Pyroclasm then closing Burst must retain the third skill on the original slot");Feed(signal,Hex("193801A4B3E800"),now+1);view=signal.Read(now+1);Check(SkillSlotsSignal.DisplaySkill(view,view.Slots[0])==15210000,"Final chain close restores the original skill");
 File.WriteAllText(Path.Combine(root,"skill-slots-regression-test.txt"),"PASS: map entry/exit resume only after same actor+name confirmation, no cross-flow/character/reset inheritance, loading full/partial authority and continued CD anchors; captured41600ms own Hellfire cast starts42s display without a buff; strict/truncated parsing; foreign actor/flow isolation; zero-duration components preserve CD; buff correction/reset/local expiry; one-millisecond clock edge; actual three-stage Flame Arrow/Burst/Pyroclasm sequence including previous-window close and final restoration; conditional T requires assigned pair, selected-target exact Fire Mark and confirmed primary CD, handles own alternative CD, expiry, resets, removal, malformed packets and foreign flow.");}
 // Synthetic665-byte layout reproducing the reported field/offset/value;
 // the community supplied diagnostic metadata, not the original packet body.
 internal static byte[] OpaqueTailFixture(){
  var bytes=new byte[665];bytes[1]=0x56;bytes[2]=19;
  int[] slots={0,1,2,3,4,5,7,20,21,22,23};
  for(int i=0;i<slots.Length;i++){int p=3+i*42;bytes[p]=1;bytes[p+1]=(byte)slots[i];bytes[p+19]=4;uint skill=SkillSlotsOverlay.ExampleSkills[i];Buffer.BlockCopy(BitConverter.GetBytes(skill),0,bytes,p+15,4);Buffer.BlockCopy(BitConverter.GetBytes(skill),0,bytes,p+20,4);}
  for(int i=0;i<8;i++){int p=3+11*42+i*25;bytes[p+1]=(byte)(10+i);bytes[p+2]=1;}
  bytes[77]=192;return bytes;
 }
 static void OpaqueTailRegression(){
  var bytes=OpaqueTailFixture();List<SkillSlotsSignal.Slot> parsed;
  Check(SkillSlotsSignal.Bar(bytes,0,bytes.Length,out parsed)&&parsed.Count==19&&parsed.Count(x=>x.Skill>0)==11,"Nonzero opaque skill tail must not discard the complete assigned bar");
  var expected=parsed.Select(x=>x.Skill).ToArray();
  for(int z=32;z<42;z++)for(int value=1;value<=255;value++){
   var variant=(byte[])bytes.Clone();variant[45+z]=(byte)value;
   Check(SkillSlotsSignal.Bar(variant,0,variant.Length,out parsed)&&parsed.Select(x=>x.Skill).SequenceEqual(expected),"Opaque tail values cannot change alignment or assigned skills");
  }
  for(int end=0;end<bytes.Length;end++)Check(!SkillSlotsSignal.Bar(bytes,0,end,out parsed),"Nonzero metadata must not permit truncated entries");
  var wrong=(byte[])bytes.Clone();wrong[65]^=1;Check(!SkillSlotsSignal.Bar(wrong,0,wrong.Length,out parsed),"Skill ID mirror remains validated with opaque metadata");
  wrong=(byte[])bytes.Clone();wrong[46]=0;Check(!SkillSlotsSignal.Bar(wrong,0,wrong.Length,out parsed),"Duplicate slots remain invalid with opaque metadata");
  var signal=new SkillSlotsSignal();signal.Configure(true);double now=DpsHistory.UtcNow();Feed(signal,Profile(),now);Feed(signal,bytes,now);var view=signal.Read(now);
  Check(view!=null&&view.Slots.Count==11&&view.Slots[1].Index==1,"Accepted metadata-bearing bar reaches the actual live receiver");
  uint id=view.Slots[1].Skill;Feed(signal,Cd(id,42000),now);Check(Math.Ceiling(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(signal.Read(now),id),now))==42,"Opaque metadata never supplies cooldowns; explicit server CD still works");
  var partial=Bar();partial[0]=5;partial[35]=255;Feed(signal,partial,now);Check(signal.Read(now).Slots.Count>0,"Partial slot updates also allow the opaque ten-byte tail");
 }
 static void BarDiagnostics(){
  var signal=new SkillSlotsSignal();signal.Configure(true);var now=DpsHistory.UtcNow();var bar=Bar();var partial=(byte[])bar.Clone();partial[0]=5;
  Feed(signal,Profile(),now);Feed(signal,partial,now);
  var json=new JavaScriptSerializer();Func<Dictionary<string,object>> read=delegate{return json.Deserialize<Dictionary<string,object>>(json.Serialize(signal.Diagnostic(now)));};
  var diagnostic=read();var trace=(Dictionary<string,object>)diagnostic["bar"];
  Check(Convert.ToInt64(trace["PartialWithoutFull"])==1&&!Convert.ToBoolean(diagnostic["activeBar"]),"Diagnostics distinguish a received partial bar without a full baseline");
  var bad=(byte[])bar.Clone();bad[22]=3;Feed(signal,bad,now);diagnostic=read();trace=(Dictionary<string,object>)diagnostic["bar"];var fault=(Dictionary<string,object>)trace["LastRejection"];
  Check(Convert.ToInt64(trace["FullRejected"])==1&&(string)fault["Field"]=="skill marker"&&Convert.ToInt32(fault["Offset"])==22&&signal.Read(now)==null,"Diagnostics explain rejected full bar without accepting its layout");
  Feed(signal,bar,now);diagnostic=read();trace=(Dictionary<string,object>)diagnostic["bar"];
  Check(Convert.ToInt64(trace["FullReceived"])==2&&Convert.ToInt64(trace["FullAccepted"])==1&&signal.Read(now).Slots.Count==2,"Full reception, acceptance and rendering remain distinct");
  Feed(signal,bad,now);Check(signal.Read(now).Slots.Count==2,"Rejected full bar never discards prior valid assignments");
  Feed(signal,bar,now,"other");diagnostic=read();Check(((System.Collections.IList)diagnostic["barFlows"]).Count==2,"Non-active flow bar metadata is retained separately without endpoints");
  signal.Reset();diagnostic=read();Check(Convert.ToInt64(diagnostic["resets"])==1&&Convert.ToInt32(diagnostic["flows"])==0&&diagnostic["bar"]==null,"Transport reset revokes flow metadata and records the reset reason");
 }
 static void VerifyIconSlide(){
  var a=new SkillSlotPainter.IconFrame{Skill=SkillSlotsOverlay.ExampleSkills[0]};
  var b=new SkillSlotPainter.IconFrame{Skill=SkillSlotsOverlay.ExampleSkills[1]};
  var motion=new SkillSlotMotion();
  Check(motion.Advance(a,10,true)==1,"First appearance has no outgoing icon");
  Check(motion.Advance(b,11,true)==0&&motion.Outgoing.Skill==a.Skill,"Variant starts with the previous icon");
  double mid=motion.Advance(b,11+SkillSlotMotion.SlideSeconds/2,true);
  Check(Math.Abs(mid-.5)<.001,"Both icons are visible halfway through the slide");
  using(var painter=new SkillSlotPainter())
  using(var before=painter.Render(100,a,"Q","",true,false,18,false,new SkillSlotPainter.IconFrame(),1))
  using(var pressed=painter.Render(100,a,"Q","",true,false,18,true,new SkillSlotPainter.IconFrame(),1))
  using(var ready=painter.Render(100,new SkillSlotPainter.IconFrame{Skill=a.Skill,ReactiveReady=true,ReactivePulse=12},"Q","",true,false,18,false,new SkillSlotPainter.IconFrame(),1))
  using(var trough=painter.Render(100,new SkillSlotPainter.IconFrame{Skill=a.Skill,ReactiveReady=true,ReactivePulse=0},"Q","",true,false,18,false,new SkillSlotPainter.IconFrame(),1))
  using(var blocked=painter.Render(100,new SkillSlotPainter.IconFrame{Skill=a.Skill,Unavailable=true},"Q","",true,false,18,false,new SkillSlotPainter.IconFrame(),1))
  using(var during=painter.Render(100,b,"Q","",true,false,18,false,motion.Outgoing,mid)){
   var edge=pressed.GetPixel(50,1);
   Check(edge.R>edge.G&&edge.G>edge.B&&edge!=before.GetPixel(50,1),"Press draws a gold highlight on the frame");
   var reactiveEdge=ready.GetPixel(50,1);
   Check(reactiveEdge.G>reactiveEdge.R&&reactiveEdge.G>reactiveEdge.B&&ready.GetPixel(50,50)==before.GetPixel(50,50),"Reactive readiness lights the frame green while the icon stays clear");
   Check(reactiveEdge.A>trough.GetPixel(50,1).A&&ready.GetPixel(50,50)==trough.GetPixel(50,50),"The green pulse changes only the frame");
   Check(blocked.GetPixel(50,50).GetBrightness()<before.GetPixel(50,50).GetBrightness()&&blocked.GetPixel(50,1)==before.GetPixel(50,1),"A failed prerequisite dims the icon without adding a countdown");
   bool changed=false;
   for(int y=0;y<100;y++)for(int x=0;x<100;x++){
    if(x>=13&&x<87&&y>=13&&y<87)Check(before.GetPixel(x,y)==pressed.GetPixel(x,y),"Press does not tint or move the icon");
    if(before.GetPixel(x,y)==during.GetPixel(x,y))continue;
    Check(x>=13&&x<87&&y>=13&&y<87,"Slide must stay within the icon viewport");
    changed=true;
   }
   Check(changed,"Slide visibly changes the icons");
  }
  Check(motion.Advance(b,12,true)==1&&motion.Outgoing.Skill==0,"Completed slide releases previous icon");
  Check(motion.Advance(a,13,false)==1&&motion.Outgoing.Skill==0,"Assignment changes show immediately");
  motion.Reset();
  Check(motion.Advance(b,14,true)==1,"Reappearing slots show immediately");
  a.Unavailable=true;motion.Advance(a,15,true);
  a.Unavailable=false;a.ReactiveReady=true;motion.Advance(a,16,true);
  Check(motion.RevealShade>.99,"Availability starts brightening from the blocked shade");
  motion.Advance(a,16+SkillSlotMotion.RevealSeconds/2,true);
  Check(Math.Abs(motion.RevealShade-.5)<.001,"Ready icon brightens smoothly while the green frame is already active");
  motion.Advance(a,17,true);Check(motion.RevealShade==0,"Ready icon becomes fully clear");
  Check(SkillSlotMotion.ReadyPulse(0)==12&&SkillSlotMotion.ReadyPulse(SkillSlotMotion.PulseSeconds/2)==0
    &&SkillSlotMotion.ReadyPulse(SkillSlotMotion.PulseSeconds)==12,"Ready frame repeatedly brightens and dims");
  Check(SkillSlotKey.Parse("Q")==81&&SkillSlotKey.Parse("1")==49&&SkillSlotKey.Parse("F1")==112&&SkillSlotKey.Parse("M1")==1
      &&SkillSlotKey.Parse("")==0&&SkillSlotKey.Parse("Ctrl+Q")==0&&SkillSlotKey.Parse("F+1")==0,"Only explicit supported key labels bind an animation");
 }
 static void VerifyRequirements(){
  var stagger=new StaggerSignal();stagger.Configure(true);
  var profile=Profile();stagger.Consume(profile,0,profile.Length,10,"self");
  var e=new SkillSlotsSignal.Slot{Skill=13340000,Alternative=13210000,Third=13350000};
  var view=new SkillSlotsSignal.View{Flow="self",ContextTarget=99,HeartGoreUntil=14};
  Check(SkillSlotsSignal.Requirement(view,e,e.Skill,10,stagger)==false,"Reactive slot dims without an open stagger window");
  var full=ProgressVerification.Gauge(99,100,150);stagger.Consume(full,0,full.Length,10,"self");
  Check(SkillSlotsSignal.Requirement(view,e,e.Skill,10,stagger)==false,"A positive gauge on the selected enemy blocks Storm Rampage");
  Check(SkillSlotsSignal.Requirement(view,e,e.Third,10,stagger)==true,"The received Heart Gore window opens independently of the base skill's stagger requirement");
  var moved=new SkillSlotsSignal.Slot{Index=1,Skill=13130000,Alternative=13350000};
  Check(SkillSlotsSignal.DisplaySkill(view,moved,10)==moved.Alternative&&
      SkillSlotsSignal.Requirement(view,moved,moved.Alternative,10,stagger)==true,
      "A reactive skill follows its received condition after moving to a different base skill or slot");
  Check(SkillSlotsSignal.DisplaySkill(view,moved,15)==moved.Skill,"An expired activation restores the new slot's base");
  var stacked=new SkillSlotsSignal.Slot{Index=5,Skill=13390000,Alternative=13310000};
  view.Cooldowns[stacked.Skill]=new SkillSlotsSignal.Cooldown{At=10,Raw=57900};
  Check(SkillSlotsSignal.DisplaySkill(view,stacked,11)==stacked.Alternative,"An independent second-row skill replaces a cooling first-row skill");
  stacked.Index=12;
  Check(SkillSlotsSignal.DisplaySkill(view,stacked,11)==stacked.Alternative,"Moving the stack preserves independent cooldown selection");
  view.Cooldowns[stacked.Alternative]=new SkillSlotsSignal.Cooldown{At=11,Raw=60000};
  Check(SkillSlotsSignal.DisplaySkill(view,stacked,12)==stacked.Skill,"If both skills are cooling, display the first slot");
  view.Cooldowns.Remove(stacked.Alternative);
  Check(SkillSlotsSignal.DisplaySkill(view,stacked,68)==stacked.Skill,"The first row regains priority as its cooldown expires");
  var conditional=new SkillSlotsSignal.Slot{Index=5,Skill=stacked.Skill,Alternative=13220000};
  Check(SkillSlotsSignal.DisplaySkill(view,conditional,12)==conditional.Skill,"A cooldown alone cannot open an unconfirmed reactive skill");
  var triple=new SkillSlotsSignal.Slot{Index=2,Skill=13070000,Alternative=13270000,Third=13300000};
  view.Cooldowns[triple.Skill]=new SkillSlotsSignal.Cooldown{At=20,Raw=19300};
  Check(SkillSlotsSignal.DisplaySkill(view,triple,21)==triple.Alternative,"The first ready assigned fallback wins in a three-row slot");
  view.Cooldowns[triple.Alternative]=new SkillSlotsSignal.Cooldown{At=21,Raw=57900};
  Check(SkillSlotsSignal.DisplaySkill(view,triple,22)==triple.Third,"A cooling second row exposes the ready third row");
  view.Cooldowns[triple.Third]=new SkillSlotsSignal.Cooldown{At=22,Raw=43400};
  Check(SkillSlotsSignal.DisplaySkill(view,triple,23)==triple.Skill&&SkillSlotsSignal.DisplaySkill(view,triple,40)==triple.Skill,
      "A three-row slot returns to the first row when none are ready or the first recovers");
  var four=Bar();
  Buffer.BlockCopy(BitConverter.GetBytes(13130000u),0,four,18,4);Buffer.BlockCopy(BitConverter.GetBytes(13130000u),0,four,23,4);
  Buffer.BlockCopy(BitConverter.GetBytes(13350000u),0,four,27,4);Buffer.BlockCopy(BitConverter.GetBytes(13100000u),0,four,35,4);
  List<SkillSlotsSignal.Slot> parsed;
  Check(SkillSlotsSignal.Bar(four,0,four.Length,out parsed)&&parsed[0].Fourth==13100000&&parsed[0].Third==0,"A fourth row survives an empty third row");
  view.Cooldowns[13130000]=new SkillSlotsSignal.Cooldown{At=30,Raw=9650};view.HeartGoreUntil=0;
  Check(SkillSlotsSignal.DisplaySkill(view,parsed[0],31)==13100000,"A cooling Insignia Explosion exposes assigned Fang Strike in row 3");
  view.Chains[13100000]=13110047;
  Check(SkillSlotsSignal.DisplaySkill(view,parsed[0],31)==13110047,"The fourth-row skill's own combo advances in the same slot");
  view.Chains.Remove(13100000);
  Check(SkillSlotsSignal.DisplaySkill(view,parsed[0],40)==13130000,"The primary returns once its received cooldown expires");
  var remembered=new SkillSlotsOptions();remembered.Remember(parsed);
  Check(remembered.EditableSlots(null)[0].Fourth==13100000,"The fourth row is preserved in per-character saved assignments");
  var zero=ProgressVerification.Gauge(99,0,150,true);stagger.Consume(zero,0,zero.Length,11,"self");
  Check(SkillSlotsSignal.Requirement(view,e,e.Skill,12,stagger)==true,"Confirmed groggy opens Storm Rampage");
  Check(SkillSlotsSignal.Requirement(view,e,e.Skill,16,stagger)==false&&SkillSlotsSignal.Requirement(view,e,e.Third,16,stagger)==null,"Expired evidence cannot leave a green highlight");
  view.ContextTarget=101;
  Check(SkillSlotsSignal.Requirement(view,e,e.Skill,12,stagger)==false,"A nearby enemy's stagger cannot authorize the selected skill");
  double now=DpsHistory.UtcNow();
  var signal=new SkillSlotsSignal();signal.Configure(true);
  var bar=Bar();Buffer.BlockCopy(BitConverter.GetBytes(13360000u),0,bar,18,4);
  Buffer.BlockCopy(BitConverter.GetBytes(13360000u),0,bar,23,4);
  Feed(signal,Profile(),now);Feed(signal,bar,now);Feed(signal,Clock(now),now);
  var effect=new List<byte>{0x2a,0x38,12,1,3,4};
  effect.AddRange(BitConverter.GetBytes(3015u));effect.AddRange(BitConverter.GetBytes(3000L));
  effect.AddRange(BitConverter.GetBytes((long)Math.Round((now+3-2.6)*1000)));
  effect.Add(12);effect.Add(1);effect.AddRange(BitConverter.GetBytes(13000100u));effect.Add(0);
  Feed(signal,effect.ToArray(),now);
  view=signal.Read(now);var q=view.Slots[0];
  Check(SkillSlotsSignal.Requirement(view,q,q.Skill,now+1,null)==true,"Own received Emergency Evasion effect opens the Infiltrate window");
  Check(SkillSlotsSignal.Requirement(view,q,q.Skill,now+4,null)==false,"The grounded Infiltrate window dims again when the received dodge expires");
  var flight=new List<byte>{0x2a,0x38,12,1,1,39};
  flight.AddRange(BitConverter.GetBytes(SkillSlotContext.Flight));
  flight.AddRange(BitConverter.GetBytes(-1L));
  flight.AddRange(BitConverter.GetBytes(4102412400000L));
  flight.AddRange(new byte[]{12,1,0});
  var neighbor=flight.ToArray();neighbor[2]=13;neighbor[26]=13;
  Feed(signal,neighbor,now+5);
  Check(SkillSlotsSignal.Requirement(signal.Read(now+5),q,q.Skill,now+5,null)==false,"A neighbor opening wings does not enable Infiltration");
  Feed(signal,flight.ToArray(),now+6,"other");
  Check(SkillSlotsSignal.Requirement(signal.Read(now+6),q,q.Skill,now+6,null)==false,"A different flow cannot open the current character's wings");
  Feed(signal,flight.ToArray(),now+7);
  Check(SkillSlotsSignal.Requirement(signal.Read(now+7),q,q.Skill,now+7,null)==true,"The own persistent flight state opens Infiltration without a dash or a recent clock");
  Feed(signal,new byte[]{0x2c,0x38,12,1,0,38,16},now+8);
  Check(SkillSlotsSignal.Requirement(signal.Read(now+8),q,q.Skill,now+8,null)==true,"Only removal of the active flight instance closes flight readiness");
  Feed(signal,new byte[]{0x2c,0x38,12,1,0,39,16},now+9);
  Check(SkillSlotsSignal.Requirement(signal.Read(now+9),q,q.Skill,now+9,null)==false,"Folding wings closes flight readiness without waiting for landing or energy");
 }
 internal static void Run(string root){VerifyIconSlide();VerifyRequirements();OpaqueTailRegression();BarDiagnostics();HotbarProfiles.Verify(root);Check(SkillSlotsOverlay.LoopSeconds(0)==12&&SkillSlotsOverlay.LoopSeconds(11)==1&&SkillSlotsOverlay.LoopSeconds(12.5)==0&&SkillSlotsOverlay.LoopSeconds(13)==12&&SkillSlotsOverlay.LoopSeconds(26.5)==11.5&&SkillSlotsOverlay.LoopSeconds(1300)==12,"Simulated cooldown counts down, reaches ready and repeats without a time limit");var labels=new SkillSlotsOptions();var assigned=new List<SkillSlotsSignal.Slot>{new SkillSlotsSignal.Slot{Index=0,Skill=15210000},new SkillSlotsSignal.Slot{Index=23,Skill=15010000}};labels.Remember(assigned);Check(labels.EditableSlots(null).Count==2,"Saved editor has only actual detected assignments, not48 protocol positions");var second=labels.Ensure(23,1);Check(SkillSlotsOptions.Label(second,1)=="2","Empty manual label defaults to consecutive detected ordinal, not internal24");second.Key="T";Check(SkillSlotsOptions.Label(second,1)=="T","Custom key label is preserved");second.Visible=false;Check(labels.EditableSlots(null).Count==2,"Hiding does not discard the editor choice");var labelJson=new JavaScriptSerializer();var persisted=SkillSlotsOptions.Read(labelJson.Deserialize<Dictionary<string,object>>(labelJson.Serialize(new{skillSlots=labels})));Check(!persisted.Placements.First(p=>p.Slot==23).Visible&&persisted.Placements.First(p=>p.Slot==23).Key=="T","Hidden state and custom label survive restart");labels.Ensure(12,2).Key="F";var auto=labels.Copy();auto.AutoLabels(assigned.Select(s=>s.Index));Check(auto.Placements.First(p=>p.Slot==23).Key==""&&!auto.Placements.First(p=>p.Slot==23).Visible&&auto.Placements.First(p=>p.Slot==12).Key=="F","Automatic numbering changes only detected labels and preserves visibility/inactive configuration");var independent=labels.Copy();independent.KnownSlots[0].Skill=15060000;Check(labels.KnownSlots[0].Skill==15210000,"Saved assignment cache uses independent snapshots");var slots=Bar();List<SkillSlotsSignal.Slot> parsed;Check(SkillSlotsSignal.Bar(slots,0,slots.Length,out parsed)&&parsed.Count==3&&parsed[1].Index==7&&parsed[2].Skill==0,"Bar retains skill slot IDs and marks omitted items");for(int i=0;i<slots.Length;i++)Check(!SkillSlotsSignal.Bar(slots,0,i,out parsed),"Partial bar must not replace active assignments");var invalid=(byte[])slots.Clone();invalid[22]=3;Check(!SkillSlotsSignal.Bar(invalid,0,invalid.Length,out parsed),"Unknown skill-record marker rejected");
 var buffered=new SkillSlotsSignal();double bufferedAt=DpsHistory.UtcNow();Feed(buffered,Profile(),bufferedAt);Feed(buffered,slots,bufferedAt);Check(buffered.Read(bufferedAt)==null,"A disabled profile never displays buffered skill data");buffered.Configure(true);Check(buffered.Read(bufferedAt)!=null,"A profile enabled after its login messages must retain the received bar");buffered.Configure(false);int hiddenUpdates=0;buffered.Changed=delegate{hiddenUpdates++;};var delta=(byte[])slots.Clone();delta[0]=5;Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,delta,18,4);Buffer.BlockCopy(BitConverter.GetBytes(15110000u),0,delta,23,4);Feed(buffered,delta,bufferedAt+1);Feed(buffered,Cd(15110000,12000),bufferedAt+2);Check(buffered.Read(bufferedAt+2)==null&&hiddenUpdates==0,"Hidden hotbar tracks updates without waking the overlay");buffered.Configure(true);var retained=buffered.Read(bufferedAt+2);Check(retained!=null&&retained.Slots[0].Skill==15110000&&SkillSlotsSignal.For(retained,15110000).Raw==12000,"Re-enabling restores live assignments and cooldowns without another full bar");buffered.Configure(false);buffered.Reset();buffered.Configure(true);Check(buffered.Read(bufferedAt+2)==null,"Transport reset invalidates a hidden hotbar");var signal=new SkillSlotsSignal();signal.Configure(true);double now=DpsHistory.UtcNow();Feed(signal,slots,now);Check(signal.Read(now)==null,"Assignments alone cannot bind local identity");Feed(signal,Profile(),now+.1);var v=signal.Read(now+.1);Check(v!=null&&v.Slots.Count==2&&v.Self==12,"Only confirmed self profile activates own bar without items");uint id=v.Slots[0].Skill;Feed(signal,Cd(id,24000),now+.2);v=signal.Read(now+.2);Check(SkillSlotsSignal.For(v,id).Raw==24000,"Server cooldown accepted");Feed(signal,Cd(id,4800),now+.3);v=signal.Read(now+.3);Check(SkillSlotsSignal.For(v,id).Raw==4800&&Math.Abs(SkillSlotsSignal.Remaining(SkillSlotsSignal.For(v,id),now+.3)-4.8)<.001,"External correction rebases immediately");Check(SkillSlotsSignal.For(v,id).Raw>0&&SkillSlotsSignal.Remaining(SkillSlotsSignal.For(v,id),now+10)==0,"Stored server interval expires without mutating the recorded raw value");Feed(signal,Cd(id,0),now+.4,"other");Check(SkillSlotsSignal.For(signal.Read(now+.4),id).Raw==4800,"Foreign flow cannot reset cooldown");Feed(signal,Cd(id,0),now+.5);Check(SkillSlotsSignal.For(signal.Read(now+.5),id).Raw==0,"Explicit zero confirms ready");
 var chain=new List<byte>{0x18,0x38};chain.AddRange(BitConverter.GetBytes(id));chain.Add(1);chain.AddRange(BitConverter.GetBytes(15050000u));Feed(signal,chain.ToArray(),now+.6);v=signal.Read(now+.6);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0])==15050000,"Explicit chain replaces the same slot");var close=new List<byte>{0x19,0x38,1};close.AddRange(BitConverter.GetBytes(15050000u));Feed(signal,close.ToArray(),now+.7);v=signal.Read(now+.7);Check(SkillSlotsSignal.DisplaySkill(v,v.Slots[0])==id,"Chain close restores base skill");Check(signal.Read(now+20)==null,"Old flow cannot leave a live bar");signal.Reset();Check(signal.Read(now+.8)==null,"Transport reset clears assignments and CD");
 var o=new SkillSlotsOptions{Enabled=true};o.Arrange(new[]{0,7,20});Check(o.Placements[1].X==o.Size+o.Gap,"Auto layout compacts non-contiguous server slots");var copied=o.Copy();copied.Placements[0].X=900;Check(o.Placements[0].X==0,"Placement copies are independent");var json=new JavaScriptSerializer();var restored=SkillSlotsOptions.Read(json.Deserialize<Dictionary<string,object>>(json.Serialize(new{skillSlots=o})));Check(SkillSlotsOptions.Same(o,restored),"Slot settings survive persistence");var history=new ConfigurationHistory();var cfg=new EnergyConfiguration{Options=new EnergyBarOptions()};history.Observe(cfg);var next=cfg.Copy();next.SkillSlots=o.Copy();history.Observe(next);EnergyConfiguration undo;Check(history.Undo(out undo)&&!undo.SkillSlots.Enabled,"Skill slots participate in undo");
 var r=new Rectangle(100,100,76,76);Check(SkillSlotsOptions.Snapped(new Rectangle(183,100,76,76),new[]{r},10,14).X==186,"Right snap gap");Check(SkillSlotsOptions.Snapped(new Rectangle(17,100,76,76),new[]{r},10,14).X==14,"Left snap gap");Check(SkillSlotsOptions.Snapped(new Rectangle(100,184,76,76),new[]{r},10,14).Y==186,"Bottom snap gap");Check(SkillSlotsOptions.Snapped(new Rectangle(100,17,76,76),new[]{r},10,14).Y==14,"Top snap gap");
 var malformed=new Random(914);for(int i=0;i<3000;i++){var b=new byte[malformed.Next(3,200)];malformed.NextBytes(b);Dictionary<uint,uint> cds;SkillSlotsSignal.Bar(b,0,b.Length,out parsed);SkillSlotsSignal.Cooldowns(b,0,b.Length,out cds);}
 Directory.CreateDirectory(Path.Combine(root,"designs"));Design(root);using(var owner=new EnergyOverlay(root,true)){var config=owner.Configuration;config.Language="es";config.SkillSlots.Enabled=true;config.Locked=false;int[] rawSlots={0,1,2,3,4,5,7,20,21,22,23};config.SkillSlots.Remember(SkillSlotsOverlay.ExampleSkills.Select((entrySkill,n)=>new SkillSlotsSignal.Slot{Index=rawSlots[n],Skill=entrySkill}).ToList());owner.ApplyConfiguration(config);owner.SkillSlots.Maintain();var before=json.Serialize(owner.SkillSlots.Status);var process=System.Diagnostics.Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<10000;i++)owner.SkillSlots.Maintain();watch.Stop();double idleMs=watch.Elapsed.TotalMilliseconds,idleCpu=(process.TotalProcessorTime-cpu).TotalMilliseconds;Check(json.Serialize(owner.SkillSlots.Status)==before,"Idle slots must reuse bitmap/icon caches and no extra timer");var paints=new List<double>();using(var painter=new SkillSlotPainter()){for(int frame=0;frame<100;frame++){watch.Restart();foreach(uint skill in SkillSlotsOverlay.ExampleSkills)using(var b=painter.Render(76,skill,"Q",true,frame/100.0,"7",false,false,18)){Check(b.GetPixel(0,0).A==0,"Outside the rounded card is transparent");}watch.Stop();paints.Add(watch.Elapsed.TotalMilliseconds);}}paints.Sort();process.Refresh();File.WriteAllText(Path.Combine(root,"skill-slots-performance.json"),json.Serialize(new{idleUpdates=10000,idleMs,idleCpuMs=idleCpu,idleAdditionalUploads=0,icons=owner.SkillSlots.Painter.Assets.Cached,slots=11,paintMedianMs=paints[50],paintP95Ms=paints[95],workingSetBytes=process.WorkingSet64,privateBytes=process.PrivateMemorySize64,scope="Offline isolated Helper process; paint cost for eleven 76px slots. Not gameplay CPU/FPS; DWM/GPU not measured."}));var start=owner.SkillSlots.Options.Copy();owner.SkillSlots.Drag(0,start,new Point(25,15),false);Check(owner.SkillSlots.Options.X==start.X+25&&owner.SkillSlots.Options.Placements[1].X==start.Placements[1].X,"Group move preserves relative positions");var settings=new HelperSettings(owner);try{settings.VerifySkillSlots(root);}finally{settings.Close();}}
 File.WriteAllText(Path.Combine(root,"skill-slots-test.txt"),"PASS: bounded bar layouts with nonzero opaque skill tails, strict markers/mirrored IDs/truncation; own identity/flow/reset/stale isolation; authoritative cooldown correction/explicit zero; server interval expiry; chain replace/close; detected-only icon/name selector; consecutive default labels and preserved custom keys; hidden-slot persistence/restoration; compact slots; 4-side gap snapping; deep copy/persistence/undo; group movement; 10000 idle updates reuse cached paint and icons; own settings category; character/server profile persistence and reconnect isolation; global native visibility independent of characters; first-profile legacy preservation; new-character assignments/labels isolation; corrupt profile file preservation; single start/stop test button and translated labels; indefinite countdown loop with ready hold; detected slot/layout/activation preservation including disabled module; transparent renderer and simulated cooldown design. Server milliseconds validated with captured own casts; charge layouts and all contextual variants need further validation.");}
 internal static void Design(string root){using(var painter=new SkillSlotPainter())using(var image=new Bitmap(1100,690)){using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var bg=new LinearGradientBrush(new Rectangle(0,0,1100,690),Color.FromArgb(27,48,58),Color.FromArgb(10,19,29),35f))g.FillRectangle(bg,0,0,1100,690);using(var title=new Font("Segoe UI",24,FontStyle.Bold))using(var small=new Font("Segoe UI",12))using(var label=new Font("Segoe UI",10))using(var white=new SolidBrush(Color.FromArgb(233,244,251)))using(var muted=new SolidBrush(Color.FromArgb(153,185,204))){g.DrawString("HABILIDADES",title,white,44,28);g.DrawString("Cristal transparente · cuadrado sencillo · sin huecos vacíos",small,muted,46,73);var ids=SkillSlotsOverlay.ExampleSkills;for(int i=0;i<8;i++)using(var slot=painter.Render(108,ids[i],(i+1).ToString(),i==1||i==3,i==1?.42:.76,i==1?"7":"3",i==5,false,18))g.DrawImageUnscaled(slot,44+i*125,133);g.DrawString("Lista",label,muted,76,260);g.DrawString("En CD",label,muted,195,260);g.DrawString("Relleno de arriba hacia abajo",small,white,46,320);
 for(int i=0;i<5;i++)using(var slot=painter.Render(102,15060000,"Q",i<4,i/4.0,(12-i*3).ToString(),false,false,18)){g.DrawImageUnscaled(slot,47+i*128,369);g.DrawString(i==4?"Lista":(12-i*3)+" s",label,muted,79+i*128,482);}using(var slot=painter.Render(130,15110000,"E",false,1,"",false,true,18))g.DrawImageUnscaled(slot,846,357);g.DrawString("Editar y escalar",label,muted,857,503);g.DrawString("Individual / Todo junto   ·   Snap en cuatro direcciones   ·   Separación ajustable",small,white,46,561);g.DrawString("Diseño real del renderizador. Tiempos simulados; validación de CD en juego pendiente.",label,muted,46,610);}}image.Save(Path.Combine(root,"designs","skill-slots-design.png"));}}
 internal static void Replay(string root,string path){string profileRoot=Path.Combine(root,"replayed-profiles");Directory.CreateDirectory(profileRoot);var scoped=new SkillSlotsOptions{Enabled=true};var profiles=new HotbarProfiles(profileRoot,scoped,false);var signal=new SkillSlotsSignal();signal.Configure(true);var json=new JavaScriptSerializer();int bars=0,cooldowns=0,positive=0,zero=0,views=0,chains=0,ownCasts=0,ownStarts=0,hellfire41600=0,ownInvalid=0,thirdSkillViews=0,contextViews=0,contextCasts=0,contextCastsCovered=0;double now=0;var contextExceptions=new List<object>();var intervals=new Dictionary<uint,HashSet<uint>>();foreach(var line in File.ReadLines(path)){var row=json.Deserialize<Dictionary<string,object>>(line);now=Convert.ToDouble(row["ts"]);var b=Hex((string)row["hex"]);string op=(string)row["opcode"];if(op=="0056"){List<SkillSlotsSignal.Slot> parsed;Check(SkillSlotsSignal.Bar(b,0,b.Length,out parsed),"Recorded bar parse");Check(parsed.Count(s=>s.Skill>0)==11,"Recorded eleven equipped skills");bars++;}if(op=="4738"){Dictionary<uint,uint> values;Check(SkillSlotsSignal.Cooldowns(b,0,b.Length,out values),"Recorded cooldown must parse to exact end");cooldowns++;positive+=values.Count(x=>x.Value>0);zero+=values.Count(x=>x.Value==0);}if(op=="1838")chains++;profiles.Consume(b,0,b.Length,now,(string)row["stream"]);var profile=profiles.Sync(scoped);if(profile!=null)scoped=profile;signal.Consume(b,0,b.Length,now,(string)row["stream"]);var v=signal.Read(now);if(v!=null){scoped.Remember(v.Slots);views++;if(v.Slots.Any(s=>SkillSlotsSignal.IsFireContext(s)&&SkillSlotsSignal.Base(SkillSlotsSignal.DisplaySkill(v,s,now))==15050000))contextViews++;if(v.Slots.Any(s=>SkillSlotsSignal.Base(s.Skill)==15210000&&SkillSlotsSignal.Base(SkillSlotsSignal.DisplaySkill(v,s,now))==15250000))thirdSkillViews++;if(op=="0238"&&v.Flow==(string)row["stream"]){int p=2;uint source;if(DpsSignal.Var(b,ref p,b.Length,out source)&&source==v.Self){uint actor,skill,ms;bool charges;if(!SkillSlotsSignal.Cast(b,0,b.Length,out actor,out skill,out ms,out charges)){ownInvalid++;continue;}ownCasts++;if(SkillSlotsSignal.Base(skill)==15050000&&ms>0){contextCasts++;if(v.Slots.Any(s=>SkillSlotsSignal.IsFireContext(s)&&SkillSlotsSignal.Base(SkillSlotsSignal.DisplaySkill(v,s,now))==15050000))contextCastsCovered++;else{uint aimedActor,aimedTarget,aimedSkill;EnemyHealthSignal.Aim(b,0,b.Length,out aimedActor,out aimedTarget,out aimedSkill);contextExceptions.Add(new{ts=now,selectedTarget=v.ContextTarget,castTarget=aimedTarget,markRemaining=v.FireMarkUntil-now,primaryRemaining=SkillSlotsSignal.Remaining(SkillSlotsSignal.For(v,15280000),now)});}}if(!charges&&ms>0){ownStarts++;uint key=skill>=11000000&&skill<20000000?SkillSlotsSignal.Base(skill):skill;HashSet<uint> set;if(!intervals.TryGetValue(key,out set))intervals[key]=set=new HashSet<uint>();set.Add(ms);var current=SkillSlotsSignal.For(v,skill);Check(current!=null&&current.Raw==ms,"Own cast must immediately reach production cooldown view");if(key==15060000&&ms==41600){hellfire41600++;Check(Math.Ceiling(SkillSlotsSignal.Remaining(current,now))==42,"Captured Hellfire starts native42s counter");}}}}}}profiles.Save(scoped);Check(profiles.Name!=null,"Actual own-profile messages must bind a character/server preset");Check(bars>0&&cooldowns>0&&positive>0&&zero>0&&views>0&&chains>0&&ownStarts>0,"Replay covers layouts, normal cooldown starts, corrections and chain messages");Check(ownInvalid==0,"Recorded own casts must have a complete recognized layout");Check(thirdSkillViews>0,"Captured three-stage rotation must display Pyroclasm on the original Flame Arrow slot");File.WriteAllText(Path.Combine(root,"skill-slots-replay-details.json"),json.Serialize(new{bars,cooldowns,positive,zero,views,chains,ownCasts,ownStarts,ownInvalid,hellfire41600,thirdSkillViews,contextViews,contextCasts,contextCastsCovered,contextExceptions,intervals=intervals.ToDictionary(x=>x.Key.ToString(),x=>x.Value.ToArray()),millisecondsProtocol=true,allChargeLayoutsValidated=false}));File.WriteAllText(Path.Combine(root,"skill-slots-replay-test.txt"),"PASS: actual own cast cooldown starts reach production view, interval milliseconds, explicit corrections/resets and chains. All charge and macro/group mappings still require separate validation.");}
}
