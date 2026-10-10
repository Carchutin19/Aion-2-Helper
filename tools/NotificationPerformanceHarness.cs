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

// Repeatable local comparison of notification work; no live capture or audio.
internal static class NotificationPerformanceHarness {
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static long Allocated(){var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");return method==null?-1:(long)method.Invoke(null,null);}
    static object Measure(int count,Action action){for(int i=0;i<20;i++)action();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();long before=Allocated();int gen0=GC.CollectionCount(0);var watch=Stopwatch.StartNew();for(int i=0;i<count;i++)action();watch.Stop();return new{iterations=count,totalMs=watch.Elapsed.TotalMilliseconds,allocatedBytes=before<0?-1:Allocated()-before,gen0Collections=GC.CollectionCount(0)-gen0};}
    static void Set(object value,string field,object item){value.GetType().GetField(field,Hidden).SetValue(value,item);}
    [STAThread] static int Main(string[] args){
        var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));string root=Path.GetFullPath(args[1]);
        var signal=Activator.CreateInstance(assembly.GetType("NotificationSignal",true),true);var options=Activator.CreateInstance(assembly.GetType("NotificationOptions",true),true);Set(options,"Sound",false);Set(options,"Fade",false);Set(options,"DurationSeconds",60.0);
        signal.GetType().GetMethod("Configure",Hidden).Invoke(signal,new[]{options});
        var consume=(Action<byte[],int,int,double,string>)Delegate.CreateDelegate(typeof(Action<byte[],int,int,double,string>),signal,signal.GetType().GetMethod("Consume",Hidden));var unrelated=new byte[]{0,0x8d,1,0};
        var unrelatedFrames=Measure(1000000,delegate{consume(unrelated,0,unrelated.Length,1,"fixture");});
        var owner=Activator.CreateInstance(assembly.GetType("EnergyOverlay",true),Hidden,null,new object[]{root,true},null);
        var overlay=Activator.CreateInstance(assembly.GetType("NotificationOverlay",true),Hidden,null,new object[]{owner,root,signal,true},null);
        var config=overlay.GetType().GetMethod("Configure",Hidden);config.Invoke(overlay,new object[]{options,true});
        var pulse=(Action)Delegate.CreateDelegate(typeof(Action),overlay,overlay.GetType().GetMethod("Pulse",Hidden));
        var maintenance=overlay.GetType().GetMethod("Maintain",Hidden);Action maintain=pulse;
        if(maintenance!=null){var tick=(Action<double>)Delegate.CreateDelegate(typeof(Action<double>),overlay,maintenance);double time=0;maintain=delegate{tick(time+=.016);};}
        var idle=Measure(1000000,maintain);
        overlay.GetType().GetMethod("Test",Hidden).Invoke(overlay,null);var hold=Measure(100000,pulse);
        var settings=Activator.CreateInstance(assembly.GetType("HelperSettings",true),Hidden,null,new object[]{owner,null},null);
        var pane=settings.GetType().GetField("notificationPage",Hidden).GetValue(settings);var reload=(Action)Delegate.CreateDelegate(typeof(Action),pane,pane.GetType().GetMethod("Reload",Hidden));var unchangedSettings=Measure(200,reload);
        var design=assembly.GetType("NotificationDesign",true);var render=design.GetMethod("Render",Hidden);var workspace=Activator.CreateInstance(assembly.GetType("FpsDesign+Workspace",true),true);var hashes=new Dictionary<string,string>();
        using(var sha=SHA256.Create())foreach(var size in new[]{new Size(260,60),new Size(440,90),new Size(960,200)})foreach(string language in new[]{"en","es"})foreach(string name in new[]{null,"Edeln"})foreach(bool edit in new[]{false,true})using(var bitmap=(Bitmap)render.Invoke(null,new object[]{size.Width,size.Height,name,language,options,edit,workspace})){
            var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{var pixels=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);hashes[size.Width+"/"+size.Height+"/"+language+"/"+(name??"sample")+"/"+edit]=BitConverter.ToString(sha.ComputeHash(pixels)).Replace("-","");}finally{bitmap.UnlockBits(data);}
        }
        ((IDisposable)workspace).Dispose();((System.Windows.Window)settings).Close();((IDisposable)overlay).Dispose();((IDisposable)owner).Dispose();
        File.WriteAllText(args[2],new JavaScriptSerializer().Serialize(new{unrelatedFrames=unrelatedFrames,idle=idle,hold=hold,unchangedSettings=unchangedSettings,pixelHashes=hashes}));return 0;
    }
}
