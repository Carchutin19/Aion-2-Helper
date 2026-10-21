using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// The optional adapter is embedded, so older systems can load the main app
// without resolving any WinRT composition types until the effect is requested.
internal sealed class OverlayCompositionAdapter:IDisposable {
 static Type backendType;static readonly object gate=new object();
 readonly Action<int,int,float,float> update;readonly Action release;readonly Func<float[]> inspect;
 OverlayCompositionAdapter(object backend){var t=backend.GetType();update=(Action<int,int,float,float>)Delegate.CreateDelegate(typeof(Action<int,int,float,float>),backend,t.GetMethod("Update"));release=(Action)Delegate.CreateDelegate(typeof(Action),backend,t.GetMethod("Dispose"));inspect=(Func<float[]>)Delegate.CreateDelegate(typeof(Func<float[]>),backend,t.GetMethod("Inspect"));}
 internal static OverlayCompositionAdapter Create(IntPtr window,out string failure){failure=null;try{lock(gate){if(backendType==null){using(var input=typeof(OverlayCompositionAdapter).Assembly.GetManifestResourceStream("OverlayBackdrop.dll")){if(input==null||input.Length>1000000)throw new InvalidDataException("Embedded composition adapter unavailable");using(var memory=new MemoryStream()){input.CopyTo(memory);backendType=Assembly.Load(memory.ToArray()).GetType("AionOverlayComposition.Backdrop",true);}}}}return new OverlayCompositionAdapter(Activator.CreateInstance(backendType,new object[]{window}));}catch(Exception ex){if(ex is TargetInvocationException&&ex.InnerException!=null)ex=ex.InnerException;if(ex is OutOfMemoryException)throw;failure=ex.GetType().Name+": "+ex.Message;return null;}}
 internal void Update(int width,int height,float radius,float alpha){update(width,height,radius,alpha);}
 internal float[] Inspect(){return inspect();}
 public void Dispose(){release();}
}

// A compositor-only window: no legacy accent policy, per-pixel layered window,
// GDI black paint, DWM frame extension, system backdrop or window region.
internal sealed class OverlayGlassLayer:Form {
 Rectangle configured;float clipRadius=-1;byte opacity;bool ready,attempted,disposed;OverlayCompositionAdapter backend;
 internal int Setups,Presentations;internal string Failure {get;private set;}
 internal OverlayGlassLayer(){Text="Aion 2 Helper - Overlay glass";FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;TopMost=true;}
 internal bool Sync(Rectangle bounds,byte alpha,IntPtr foreground,bool show,float radius=-1){if(!show||alpha==0){if(Visible)Hide();return false;}Prepare(bounds,radius);if(!ready){if(Visible)Hide();return false;}bool present=!Visible||opacity!=alpha||configured!=bounds;
  if(present&&!UpdateSurface(bounds,clipRadius,alpha/255f)){if(Visible)Hide();return false;}if(!Visible)Show();if(present){OverlayNative.SetWindowPos(Handle,foreground,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x10);opacity=alpha;configured=bounds;Presentations++;}return true;
 }
 void Prepare(Rectangle bounds,float radius=-1){float nextRadius=radius<0?11*bounds.Height/86f:radius;nextRadius=Math.Max(0,Math.Min(Math.Min(bounds.Width,bounds.Height)/2f,nextRadius));bool changed=Size!=bounds.Size||clipRadius!=nextRadius;if(Bounds!=bounds)Bounds=bounds;
  if(!attempted){attempted=true;string failure;backend=OverlayCompositionAdapter.Create(Handle,out failure);Failure=failure;ready=backend!=null;Setups++;changed=true;}if(changed){clipRadius=nextRadius;if(ready)UpdateSurface(bounds,nextRadius,opacity/255f);}
 }
 bool UpdateSurface(Rectangle bounds,float radius,float alpha){try{backend.Update(bounds.Width,bounds.Height,radius,alpha);return true;}catch(Exception ex){if(ex is OutOfMemoryException)throw;Failure=ex.GetType().Name+": "+ex.Message;ready=false;backend.Dispose();backend=null;return false;}}
 internal void VerifyNative(){var bounds=new Rectangle(-2000,-2000,269,61);Prepare(bounds);int style=OverlayNative.GetWindowLong(Handle,-20);if((style&0x80000)!=0||(style&0x00200000)==0||Region!=null)throw new Exception("Backdrop must use a compositor-only surface, without layered/GDI region rendering");if(!ready)throw new Exception("Modern backdrop unavailable in this native verification environment: "+Failure);var state=backend.Inspect();if(state[0]!=269||state[1]!=61||Math.Abs(state[2]-11*61/86f)>.001||state[4]!=1)throw new Exception("Rounded GPU clip must follow the card geometry");int setups=Setups;for(int i=0;i<1000;i++)Prepare(bounds);if(Setups!=setups)throw new Exception("Unchanged backdrops cannot recreate composition");Prepare(new Rectangle(-2000,-2000,380,86),10);state=backend.Inspect();if(state[0]!=380||state[1]!=86||state[2]!=10)throw new Exception("The same backend supports independent DPS/card clipping");UpdateSurface(bounds,7,.4f);state=backend.Inspect();if(Math.Abs(state[3]-.4f)>.001)throw new Exception("Composition opacity follows the existing widget fade");Sync(bounds,0,IntPtr.Zero,true);if(Visible)throw new Exception("Zero alpha hides the entire backdrop");}
 protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.ExStyle=(cp.ExStyle|0x00200000|0x80|0x20|0x08000000)&~(0x80000|0x40000);cp.ClassStyle&=~0x20000;return cp;}}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override void OnPaintBackground(PaintEventArgs e){}
 protected override void OnPaint(PaintEventArgs e){}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
 protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;if(backend!=null){backend.Dispose();backend=null;}}base.Dispose(disposing);}
}
