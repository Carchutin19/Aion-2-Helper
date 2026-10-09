using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

internal static class PerformanceHarness {
    sealed class Input {internal Segment Segment;internal double Time;}
    static long Allocated(){var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");return method==null?-1:(long)method.Invoke(null,null);}
    static object Measure(int count,Action action){
        for(int i=0;i<3;i++)action();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        int gen0=GC.CollectionCount(0);long allocated=Allocated();var watch=Stopwatch.StartNew();
        for(int i=0;i<count;i++)action();watch.Stop();long after=Allocated();
        return new {iterations=count,totalMs=watch.Elapsed.TotalMilliseconds,msPerIteration=watch.Elapsed.TotalMilliseconds/count,allocatedBytes=allocated<0?-1:after-allocated,gen0Collections=GC.CollectionCount(0)-gen0};
    }
    [STAThread] static int Main(string[] args){
        string root=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);var json=new JavaScriptSerializer();var inputs=new List<Input>();
        foreach(string line in File.ReadLines(Path.Combine(root,"captures","20261008-194328-734326","segments.jsonl"))){
            var row=json.Deserialize<Dictionary<string,object>>(line);string[] endpoints=((string)row["stream"]).Split('>');string src=endpoints[0],dst=endpoints[1],hex=(string)row["hex"];var data=new byte[hex.Length/2];
            for(int n=0;n<data.Length;n++)data[n]=Convert.ToByte(hex.Substring(n*2,2),16);
            inputs.Add(new Input{Time=Convert.ToDouble(row["ts"]),Segment=new Segment{Src=src.Substring(0,src.LastIndexOf(':')),SrcPort=int.Parse(src.Substring(src.LastIndexOf(':')+1)),Dst=dst.Substring(0,dst.LastIndexOf(':')),DstPort=int.Parse(dst.Substring(dst.LastIndexOf(':')+1)),Seq=Convert.ToUInt32(row["seq"]),Flags=Convert.ToByte(row["flags"]),Data=data}});
        }
        var replay=Measure(30,delegate{var decoder=new DashSignal(root);foreach(var input in inputs)decoder.Consume(input.Segment,input.Time);if(decoder.Samples!=79||decoder.Errors!=0||decoder.Current.Value!=113900)throw new Exception("Replay differs");});
        int frame=0;var workspace=new BarDesign.GlowWorkspace();var render=Measure(1000,delegate{double ratio=.2+(frame++%701)/1000.0;using(var bitmap=BarDesign.RenderEmissive(484,4,ratio,false,true,1,null,workspace)){} });
        var hashes=new Dictionary<string,string>();using(var sha=SHA256.Create())foreach(int height in new[]{3,4,8})foreach(double ratio in new[]{0,.2,.5,.9,1})foreach(bool edit in new[]{false,true}){
            using(var bitmap=BarDesign.RenderEmissive(484,height,ratio,edit,true,1)){
                var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
                try{var pixels=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);hashes[height+"/"+ratio+"/"+edit]=BitConverter.ToString(sha.ComputeHash(pixels)).Replace("-","");}finally{bitmap.UnlockBits(data);}
            }
        }
        File.WriteAllText(output,json.Serialize(new {fixtureSegments=inputs.Count,replay=replay,emissiveRender=render,pixelHashes=hashes}));return 0;
    }
}
