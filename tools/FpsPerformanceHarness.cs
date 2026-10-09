using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

// Runs against a built executable. No game or private traffic fixture required.
internal static class FpsPerformanceHarness {
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static long Allocated(){var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");return method==null?-1:(long)method.Invoke(null,null);}
    static object Measure(int count,Action action){for(int i=0;i<200;i++)action();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();int gen0=GC.CollectionCount(0);long before=Allocated();var watch=Stopwatch.StartNew();for(int i=0;i<count;i++)action();watch.Stop();return new{iterations=count,totalMs=watch.Elapsed.TotalMilliseconds,allocatedBytes=before<0?-1:Allocated()-before,gen0Collections=GC.CollectionCount(0)-gen0};}
    [STAThread] static int Main(string[] args){
        var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var optionsType=assembly.GetType("FpsOptions",true);var options=Activator.CreateInstance(optionsType,true);
        var design=assembly.GetType("FpsDesign",true);var render=design.GetMethod("Render",Hidden);object workspace=null;
        if(render.GetParameters().Length==6)workspace=Activator.CreateInstance(design.GetNestedType("Workspace",Hidden),true);
        var fields=new Dictionary<string,FieldInfo>();foreach(string field in new[]{"Color","Background","BackgroundOpacity","Softness"})fields[field]=optionsType.GetField(field,Hidden);
        Func<int,int,string,bool,Bitmap> draw=delegate(int w,int h,string text,bool editing){return (Bitmap)render.Invoke(null,workspace==null?new object[]{w,h,text,options,editing}:new object[]{w,h,text,options,editing,workspace});};
        var samplesType=assembly.GetType("FpsSamples",true);var samples=Activator.CreateInstance(samplesType,Hidden,null,new object[]{42},null);var add=samplesType.GetMethod("Add",Hidden);int sequence=0;Action frame;
        if(add.GetParameters()[1].ParameterType==typeof(ulong)){var accept=(Action<int,ulong,double>)Delegate.CreateDelegate(typeof(Action<int,ulong,double>),samples,add);frame=delegate{accept(42,123,sequence++/120.0);};}
        else{var accept=(Action<int,string,double>)Delegate.CreateDelegate(typeof(Action<int,string,double>),samples,add);frame=delegate{accept(42,((ulong)123).ToString("X"),sequence++/120.0);};}
        var data=Measure(100000,frame);int index=0;string[] readings={"83 FPS","120 FPS","99 FPS"};
        var small=Measure(1000,delegate{using(var bitmap=draw(76,30,readings[index++%3],false)){} });
        var large=Measure(200,delegate{using(var bitmap=draw(448,176,readings[index++%3],false)){} });
        var hashes=new Dictionary<string,string>();using(var sha=SHA256.Create())foreach(var size in new[]{new Size(48,20),new Size(76,30),new Size(112,44),new Size(448,176)})foreach(string text in new[]{"\u2014 FPS","83 FPS","9999 FPS"})foreach(bool background in new[]{false,true})foreach(bool editing in new[]{false,true}){
            fields["Color"].SetValue(options,"#00FF63");fields["Background"].SetValue(options,background);fields["BackgroundOpacity"].SetValue(options,130);fields["Softness"].SetValue(options,9);
            using(var bitmap=draw(size.Width,size.Height,text,editing)){var pixels=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{var bytes=new byte[pixels.Stride*pixels.Height];Marshal.Copy(pixels.Scan0,bytes,0,bytes.Length);hashes[size.Width+"/"+size.Height+"/"+text+"/"+background+"/"+editing]=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}finally{bitmap.UnlockBits(pixels);}}
        }
        if(workspace!=null)((IDisposable)workspace).Dispose();
        File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{presentations=data,smallRender=small,largeRender=large,pixelHashes=hashes}));return 0;
    }
}
