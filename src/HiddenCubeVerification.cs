using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;

internal static class HiddenCubeVerification {
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static void Put(byte[] b,int offset,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,offset,4);}
    static void Coordinates(byte[] b,int at,float x,float y,float z){
        Buffer.BlockCopy(BitConverter.GetBytes(x),0,b,at,4);
        Buffer.BlockCopy(BitConverter.GetBytes(y),0,b,at+4,4);
        Buffer.BlockCopy(BitConverter.GetBytes(z),0,b,at+8,4);
    }
    static byte[] Map(int map){var b=new byte[48];b[0]=0x21;b[1]=0x36;Put(b,6,map);Coordinates(b,18,1000,1000,1000);return b;}
    static byte[] Cube(byte actor,int template){
        var b=new byte[55];b[0]=0x34;b[1]=0x36;b[2]=actor;b[3]=4;
        Put(b,5,template);Coordinates(b,9,5200,1000,1600);return b;
    }
    static byte[] Move(byte actor,float x,float y){
        var b=new byte[17];b[0]=0x1a;b[1]=0x37;b[2]=actor;Coordinates(b,5,x,y,1000);return b;
    }
    static void Feed(HiddenCubeSignal signal,byte[] data,double time,string flow="world"){signal.Consume(data,0,data.Length,time,flow);}
    internal static void Run(string root,string fixture){
        var signal=new HiddenCubeSignal();
        Feed(signal,Map(1010),100);
        signal.Configure(true);
        Feed(signal,Cube(18,1500041),101);
        Feed(signal,Cube(18,1301199),101);
        Feed(signal,Cube(18,1200039),101,"foreign");
        Check(signal.Read(101).Count==0,"Ordinary objects, unvalidated templates and foreign flows do not trigger the radar");
        Feed(signal,Cube(18,1200039),102);
        var reading=signal.Read(102);
        Check(reading.Count==1&&reading.Distance>42&&reading.Distance<43&&reading.Elevation==6&&!reading.Direction.HasValue,"Detected cube uses fresh world coordinates without an invented bearing");
        Check(signal.Read(105.5).Count==1&&!signal.Read(105.5).Distance.HasValue,"Stale position hides measurements without hiding detection");
        Feed(signal,Cube(19,1200041),106);
        Feed(signal,new byte[]{0x38,0x36,18,22,1},106);
        Check(signal.Read(106).Count==2,"Starting to open a cube does not remove it");
        Feed(signal,new byte[]{0x3a,0x36,18,22,1,0,0,0,0x23,0xc5,0x9a,0x25},107);
        Check(signal.Read(107).Count==1,"Verified completion removes only the opened cube");
        Feed(signal,new byte[]{0x35,0x36,19,1,1,0},108);
        Check(signal.Read(108).Count==0,"Leaving the observed area removes the last cube");
        Feed(signal,Cube(18,1200039),109);
        Feed(signal,Map(2001),110);
        Check(signal.Read(110).Count==0,"Map changes clear old cube IDs");
        Feed(signal,Map(1010),111);
        Feed(signal,EnemyTargetVerification.Player(12,"Radar",1000,1000,true),111);
        Feed(signal,Move(12,1000,1000),112);Feed(signal,Move(12,1100,1000),113);
        Feed(signal,Cube(18,1200039),113);
        reading=signal.Read(113);
        Check(reading.Direction.HasValue&&reading.Direction.Value.Y<-.99,"Fresh own movement puts a cube straight ahead at the top of the dial");
        Feed(signal,Move(13,1100,1200),114);
        Check(signal.Read(114).Direction==reading.Direction,"Movement by a different actor cannot change the bearing");
        var otherMovement=Move(12,1200,1000);otherMovement[0]=0x28;
        Feed(signal,otherMovement,118);
        Check(signal.Read(118).Distance<reading.Distance&&signal.Read(118).Direction.HasValue,"Radar follows the additional movement layouts supported by enemy distance");
        signal.Reset();
        Check(signal.Read(110).Count==0,"Reset clears cube state");
        Check(HiddenCubeOverlay.Fade(0,true,.12,240)==.5&&HiddenCubeOverlay.Fade(1,false,.24,240)==0,"Fade in and out use the configured duration");
        var options=new HiddenCubeOptions{Enabled=true,Blur=true,FadeMs=300,Opacity=75,BackgroundOpacity=45,Accent="#FFAA55"};
        var json=new JavaScriptSerializer();
        var copy=HiddenCubeOptions.Read(json.Deserialize<Dictionary<string,object>>(json.Serialize(new{hiddenCubeRadar=options})));
        Check(HiddenCubeOptions.Same(copy,options),"Radar preferences persist independently");
        Directory.CreateDirectory(Path.Combine(root,"designs"));
        using(var owner=new EnergyOverlay(root,true)){
            var form=new HelperSettings(owner);
            try{form.VerifyHiddenCube(root);}finally{form.Close();}
        }
        if(fixture!=null)Replay(fixture);
        File.WriteAllText(Path.Combine(root,"hidden-cube-test.txt"),"PASS: own-flow cube appearance, foreign and unrelated objects, stale position, multi-cube opening/removal, map reset, fade, preferences, localized preview"+(fixture==null?".":", and recorded Verteron lifecycle."));
    }
    static void Replay(string fixture){
        var signal=new HiddenCubeSignal();signal.Configure(true);
        bool showed=false,opened=false;
        foreach(string line in File.ReadLines(fixture)){
            var parts=line.Split(' ');
            double time=double.Parse(parts[0],System.Globalization.CultureInfo.InvariantCulture);
            var b=new byte[parts[1].Length/2];
            for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(parts[1].Substring(i*2,2),16);
            Feed(signal,b,time);
            if(b[0]==0x34&&b[1]==0x36)showed=signal.Read(time).Count==1;
            if(b[0]==0x3a&&b[1]==0x36)opened=showed&&signal.Read(time).Count==0;
        }
        Check(showed&&opened,"Actual recorded Verteron cube must appear and clear on opening");
    }
}
