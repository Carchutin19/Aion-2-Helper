using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

// Original adapter for the Windows SDK visual-layer/Direct2D ABI. This module
// is embedded and loaded only when blur is requested, with transparent fallback.
namespace AionOverlayComposition {
 [ComImport,Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IDesktopInterop {void CreateDesktopWindowTarget(IntPtr window,[MarshalAs(UnmanagedType.Bool)]bool topmost,out IntPtr target);}
 [ComImport,Guid("629BDBC8-D932-4FF4-96B9-8D96C5C1E858"),InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
 interface IPropertyFactory {
  void Empty(out IntPtr v);void Byte(byte x,out IntPtr v);void Int16(short x,out IntPtr v);void UInt16(ushort x,out IntPtr v);void Int32(int x,out IntPtr v);void UInt32(uint x,out IntPtr v);void Int64(long x,out IntPtr v);void UInt64(ulong x,out IntPtr v);void Single(float x,out IntPtr v);
 }
 [ComVisible(true),Guid("2FC57384-A068-44D7-A331-30982FCF7177"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IEffectInterop {
  [PreserveSig]int GetEffectId(out Guid id);
  [PreserveSig]int GetNamedPropertyMapping([MarshalAs(UnmanagedType.LPWStr)]string name,out uint index,out uint mapping);
  [PreserveSig]int GetPropertyCount(out uint count);
  [PreserveSig]int GetProperty(uint index,out IntPtr value);
  [PreserveSig]int GetSource(uint index,out IGraphicsEffectSource source);
  [PreserveSig]int GetSourceCount(out uint count);
 }
 [ComVisible(true),ClassInterface(ClassInterfaceType.None)]
 public sealed class GaussianEffect:IGraphicsEffect,IGraphicsEffectSource,IEffectInterop {
  public string Name {get;set;} public IGraphicsEffectSource Source;
  public GaussianEffect(){Name="OverlayGaussian";}
  public int GetEffectId(out Guid id){id=new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");return 0;}
  public int GetNamedPropertyMapping(string name,out uint index,out uint mapping){mapping=1;index=name=="BlurAmount"?0u:name=="Optimization"?1u:2u;return name=="BlurAmount"||name=="Optimization"||name=="BorderMode"?0:unchecked((int)0x80070057);}
  public int GetPropertyCount(out uint count){count=3;return 0;}
  public int GetProperty(uint index,out IntPtr value){value=IntPtr.Zero;if(index>=3)return unchecked((int)0x8000000b);try{var factory=(IPropertyFactory)WindowsRuntimeMarshal.GetActivationFactory(typeof(PropertyValue));IntPtr raw;if(index==0)factory.Single(12,out raw);else factory.UInt32(1,out raw);var iid=new Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62");try{return Marshal.QueryInterface(raw,ref iid,out value);}finally{Marshal.Release(raw);}}catch(Exception ex){return Marshal.GetHRForException(ex);}}
  public int GetSource(uint index,out IGraphicsEffectSource source){source=index==0?Source:null;return index==0?0:unchecked((int)0x8000000b);}
  public int GetSourceCount(out uint count){count=1;return 0;}
 }
 public sealed class Backdrop:IDisposable {
  [StructLayout(LayoutKind.Sequential)]struct QueueOptions {public int Size,Thread,Apartment;}
  [DllImport("CoreMessaging.dll")]static extern int CreateDispatcherQueueController(QueueOptions options,[MarshalAs(UnmanagedType.IUnknown)]out object controller);
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int key,ref int value,int size);
  sealed class Context:IDisposable {
   public readonly Compositor Compositor;public readonly CompositionEffectFactory Factory;public int References;
   public Context(){Compositor=new Compositor();try{Factory=Compositor.CreateEffectFactory(new GaussianEffect{Source=new CompositionEffectSourceParameter("scene")});}catch{Compositor.Dispose();throw;}}
   public void Dispose(){Close(Factory);Close(Compositor);}
  }
  [ThreadStatic]static Context shared;[ThreadStatic]static object queueController;
  Context context;DesktopWindowTarget target;ContainerVisual root;SpriteVisual sprite;CompositionRoundedRectangleGeometry round;CompositionGeometricClip clip;CompositionEffectBrush effect;CompositionBackdropBrush scene;
  bool disposed;int width=-1,height=-1;float radius=-1,opacity=-1;
  public Backdrop(IntPtr window){
   if(!Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor","CreateHostBackdropBrush"))throw new PlatformNotSupportedException("Windows host backdrop is unavailable");
   int enabled=1;int hr=DwmSetWindowAttribute(window,17,ref enabled,4);if(hr<0)throw new PlatformNotSupportedException("Host backdrop requires supported Windows 11 composition");
   if(Windows.System.DispatcherQueue.GetForCurrentThread()==null){Marshal.ThrowExceptionForHR(CreateDispatcherQueueController(new QueueOptions{Size=12,Thread=2,Apartment=2},out queueController));}
   if(shared==null)shared=new Context();context=shared;context.References++;
   try{IntPtr raw;((IDesktopInterop)(object)context.Compositor).CreateDesktopWindowTarget(window,true,out raw);try{target=(DesktopWindowTarget)Marshal.GetObjectForIUnknown(raw);}finally{Marshal.Release(raw);}root=context.Compositor.CreateContainerVisual();root.IsVisible=true;root.Opacity=1;target.Root=root;sprite=context.Compositor.CreateSpriteVisual();sprite.IsVisible=true;sprite.Opacity=0;scene=context.Compositor.CreateHostBackdropBrush();effect=context.Factory.CreateBrush();effect.SetSourceParameter("scene",scene);sprite.Brush=effect;round=context.Compositor.CreateRoundedRectangleGeometry();clip=context.Compositor.CreateGeometricClip(round);sprite.Clip=clip;root.Children.InsertAtTop(sprite);}catch{Dispose();throw;}
  }
  public void Update(int w,int h,float r,float a){if(disposed)throw new ObjectDisposedException("Backdrop");if(w<1||h<1||w>4096||h>4096||float.IsNaN(r)||float.IsNaN(a))throw new ArgumentOutOfRangeException();r=Math.Max(0,Math.Min(Math.Min(w,h)/2f,r));a=Math.Max(0,Math.Min(1,a));if(width!=w||height!=h){var size=new Vector2(w,h);root.Size=sprite.Size=round.Size=size;width=w;height=h;}if(radius!=r){round.CornerRadius=new Vector2(r);radius=r;}if(opacity!=a){sprite.Opacity=a;opacity=a;}}
  public float[] Inspect(){return new[]{round.Size.X,round.Size.Y,round.CornerRadius.X,sprite.Opacity,sprite.Clip==clip?1f:0f};}
  static void Close(IDisposable value){if(value==null)return;try{value.Dispose();}catch(COMException){}catch(ObjectDisposedException){}}
  public void Dispose(){if(disposed)return;disposed=true;if(target!=null){try{target.Root=null;}catch(COMException){}catch(ObjectDisposedException){}Close(target);}Close(clip);Close(round);Close(sprite);Close(root);Close(effect);Close(scene);if(context!=null&&--context.References==0){context.Dispose();if(shared==context)shared=null;}context=null;}
 }
}
