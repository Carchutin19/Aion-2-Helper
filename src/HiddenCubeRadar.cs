using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Web.Script.Serialization;

internal sealed class HiddenCubeOptions {
    internal const int MinWidth=250, MaxWidth=700, MinHeight=84, MaxHeight=260;
    public bool Enabled, Blur, Fade=true;
    public int Opacity=100, BackgroundOpacity=78, FadeMs=240;
    public string Accent="#FFD08B";
    public Rectangle Bounds=new Rectangle(30,245,375,108);

    internal static Size LimitSize(Size size) {
        return new Size(Math.Max(MinWidth,Math.Min(MaxWidth,size.Width)),Math.Max(MinHeight,Math.Min(MaxHeight,size.Height)));
    }
    internal void Normalize() {
        Opacity=Math.Max(0,Math.Min(100,Opacity));
        BackgroundOpacity=Math.Max(0,Math.Min(100,BackgroundOpacity));
        FadeMs=Math.Max(0,Math.Min(1500,FadeMs));
        if(!EnergyBarOptions.ValidColor(Accent))Accent="#FFD08B";
        Bounds=new Rectangle(new Point(Math.Max(-100000,Math.Min(100000,Bounds.X)),Math.Max(-100000,Math.Min(100000,Bounds.Y))),LimitSize(Bounds.Size));
    }
    internal HiddenCubeOptions Copy(){return (HiddenCubeOptions)MemberwiseClone();}
    internal static bool Same(HiddenCubeOptions a,HiddenCubeOptions b){
        return a.Enabled==b.Enabled&&a.Blur==b.Blur&&a.Fade==b.Fade&&a.FadeMs==b.FadeMs
            &&a.Opacity==b.Opacity&&a.BackgroundOpacity==b.BackgroundOpacity&&a.Accent==b.Accent&&a.Bounds==b.Bounds;
    }
    internal static HiddenCubeOptions Read(Dictionary<string,object> root){
        var o=new HiddenCubeOptions();object raw;
        if(root.TryGetValue("hiddenCubeRadar",out raw)){
            try{o=new JavaScriptSerializer().ConvertToType<HiddenCubeOptions>(raw)??o;}
            catch(ArgumentException){}catch(InvalidOperationException){}
        }
        o.Normalize();return o;
    }
}

internal sealed class HiddenCubeSignal {
    const double MinHeadingStepSquared=75*75, MaxPositionStepSquared=4000*4000;
    internal struct Reading {
        internal int Count;
        internal double? Distance, Elevation;
        internal PointF? Direction;
    }
    readonly object gate=new object();
    readonly Dictionary<uint,EnemyDetailReader.Position> cubes=new Dictionary<uint,EnemyDetailReader.Position>();
    volatile bool enabled;
    string active,name;
    uint map,self;
    double lastTraffic;
    EnemyDetailReader.Position position, headingAnchor;
    PointF? heading;
    internal ClientPositionReader ClientPositions;
    internal void Configure(bool value){lock(gate){if(enabled!=value){enabled=value;cubes.Clear();headingAnchor=position;heading=null;}}}
    internal void Reset(){lock(gate){active=null;map=self=0;name=null;lastTraffic=0;ClearLocation();}}
    void ClearLocation(){cubes.Clear();position=headingAnchor=new EnemyDetailReader.Position();heading=null;}
    // Verteron: local replay confirmed 1200039. The second template and
    // lifecycle are corroborated by dr89mh/aion2-cube-radar, docs/FINDINGS.md.
    static bool IsCube(uint template){return template==1200039||template==1200041;}
    static bool IdentityMap(byte[] b,int start,int end,out uint map){
        map=0;
        if(end-start!=48||BitConverter.ToUInt32(b,start+2)>10000)return false;
        map=BitConverter.ToUInt32(b,start+6);
        return map>0&&map<10000000;
    }
    void UpdatePosition(EnemyDetailReader.Position next,double time,bool teleport=false){
        if(!next.Known||position.Known&&time<position.Time)return;
        next.Time=time;
        double dx=next.X-position.X,dy=next.Y-position.Y;
        if(teleport||!position.Known||dx*dx+dy*dy>MaxPositionStepSquared){
            heading=null;
            headingAnchor=next;
        }else if(time-position.Time>5){
            headingAnchor=next;
        }else{
            // Accumulate small samples and keep the verified heading through stationary updates.
            double x=next.X-headingAnchor.X,y=next.Y-headingAnchor.Y;
            if(x*x+y*y>=MinHeadingStepSquared){
                heading=new PointF((float)x,(float)y);
                headingAnchor=next;
            }
        }
        position=next;
    }
    internal void Consume(byte[] b,int start,int end,double time,string flow){
        if(b==null||start<0||end>b.Length||end-start<2||string.IsNullOrEmpty(flow)||double.IsNaN(time)||double.IsInfinity(time))return;
        int op=(b[start]<<8)|b[start+1], p=start+2;
        if(!enabled&&op!=0x2136&&op!=0x3336)return;
        if(op!=0x0036&&op!=0x2136&&op!=0x2336&&op!=0x3336&&op!=0x3436&&op!=0x3536&&op!=0x3a36
            &&op!=0x2a38&&op!=0x2b38&&!EnemyDetailReader.MovementOpcode(op))return;
        lock(gate){
            uint actor,newMap;string ownName;byte role;
            EnemyDetailReader.Position next;
            if(op==0x2136&&IdentityMap(b,start,end,out newMap)){
                if(flow==active&&time<lastTraffic)return;
                if(flow!=active||map!=newMap){ClearLocation();self=0;name=null;}
                active=flow;map=newMap;lastTraffic=time;
                if(EnemyDetailReader.Coordinates(b,start+18,end,out next))UpdatePosition(next,time,true);
                return;
            }
            if(op==0x3336&&DpsSignal.PlayerIdentity(b,start,end,true,out actor,out ownName,out role)){
                if(flow==active&&time<lastTraffic)return;
                if(flow!=active){ClearLocation();map=0;}
                else if(self!=0&&(actor!=self||ownName!=name))ClearLocation();
                active=flow;self=actor;name=ownName;lastTraffic=time;
                int level;
                if(EnemyDetailReader.Character(b,start,end,out level,out next))UpdatePosition(next,time);
                return;
            }
            if(!enabled||active!=flow||time<lastTraffic)return;
            if(time-lastTraffic>12)ClearLocation();
            lastTraffic=time;
            if(op==0x0036)return;
            if(op==0x2336&&end-start==26&&DpsSignal.Var(b,ref p,end,out actor)&&actor==0){
                if(EnemyDetailReader.Coordinates(b,p+5,end,out next))UpdatePosition(next,time,true);
            }else if(op==0x3436){
                if(map!=1010||!DpsSignal.Var(b,ref p,end,out actor)||actor==0||end-p!=52
                    ||!EnemyDetailReader.Coordinates(b,p+6,end,out next))return;
                uint template=BitConverter.ToUInt32(b,p+2);
                // Only the unopened layout and templates verified for Verteron.
                if(BitConverter.ToUInt16(b,p)!=4||!IsCube(template)||BitConverter.ToUInt32(b,p+37)!=0){
                    cubes.Remove(actor);return;
                }
                if(cubes.Count<32||cubes.ContainsKey(actor))cubes[actor]=next;
            }else if(op==0x3536){
                if(DpsSignal.Var(b,ref p,end,out actor)&&end-p==3&&b[p+2]==0&&(b[p]==1||b[p]==4))cubes.Remove(actor);
            }else if(op==0x3a36){
                uint opener;
                if(DpsSignal.Var(b,ref p,end,out actor)&&cubes.ContainsKey(actor)&&DpsSignal.Var(b,ref p,end,out opener)&&opener!=0
                    &&end-p==8&&BitConverter.ToUInt32(b,p)==1&&BitConverter.ToUInt32(b,p+4)==630900003)cubes.Remove(actor);
            }else if(self!=0&&(op==0x2a38||op==0x2b38)){
                List<PlayerShieldSignal.Status> statuses;
                if(PlayerShieldSignal.ReadStatuses(b,start,end,out actor,out statuses,true))
                    foreach(var status in statuses)if(status.Source==self&&status.SourcePosition.Known){UpdatePosition(status.SourcePosition,time);break;}
            }else if(self!=0&&EnemyDetailReader.Movement(b,start,end,out actor,out next)&&actor==self){
                UpdatePosition(next,time);
            }
        }
    }
    internal Reading Read(double now){
        lock(gate){
            var result=new Reading();
            if(!enabled||active==null||Math.Max(0,now-lastTraffic)>6||cubes.Count==0)return result;
            result.Count=cubes.Count;
            var exact=ClientPositions==null?null:ClientPositions.Read(active,now);
            if(exact!=null&&(!position.Known||exact.Position.Time>position.Time))UpdatePosition(exact.Position,exact.Position.Time);
            bool continuous=exact!=null&&position.Time==exact.Position.Time;
            var target=new EnemyDetailReader.Position();
            double closest=double.MaxValue;
            foreach(var cube in cubes.Values){
                double? distance=EnemyDetailReader.Distance(position,cube,now,continuous);
                if(distance.HasValue&&distance.Value<closest){closest=distance.Value;target=cube;}
            }
            if(!target.Known)return result;
            result.Distance=closest;
            result.Elevation=(target.Z-position.Z)/100.0;
            // The top of the dial is the last observed direction of travel.
            // It does not depend on an assumed north axis or camera yaw.
            if(heading.HasValue){
                double fx=heading.Value.X,fy=heading.Value.Y;
                double tx=target.X-position.X,ty=target.Y-position.Y;
                double length=Math.Sqrt((fx*fx+fy*fy)*(tx*tx+ty*ty));
                if(length>1)result.Direction=new PointF((float)((fx*ty-fy*tx)/length),(float)(-(fx*tx+fy*ty)/length));
            }
            return result;
        }
    }
}

internal sealed class HiddenCubePainter:IDisposable {
    readonly CrispOverlayText title=new CrispOverlayText(), distance=new CrispOverlayText(), detail=new CrispOverlayText(), footer=new CrispOverlayText();
    internal static readonly HiddenCubeSignal.Reading Sample=new HiddenCubeSignal.Reading{Count=1,Distance=42,Elevation=6,Direction=new PointF(.71f,-.71f)};
    internal Bitmap Render(Size size,HiddenCubeOptions o,HiddenCubeSignal.Reading data,string language,bool editing){
        var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
        var accent=ColorTranslator.FromHtml(o.Accent);
        using(var g=Graphics.FromImage(image)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var shape=SkillSlotPainter.Round(new RectangleF(.5f,.5f,size.Width-1,size.Height-1),12))
            using(var fill=new SolidBrush(Color.FromArgb((int)Math.Round(o.BackgroundOpacity*255/100.0),10,15,22)))
            using(var border=new Pen(Color.FromArgb(140,110,129,148))){
                g.FillPath(fill,shape);g.DrawPath(border,shape);
            }
            int dial=Math.Min(size.Height-22,91);
            var box=new RectangleF(12,(size.Height-dial)/2f,dial,dial);
            DrawDial(g,box,accent,data.Direction);
            int x=dial+24,w=size.Width-x-16;
            var pale=Color.FromArgb(229,237,244);
            title.Draw(g,UiLanguage.Text("Hidden cube",language),13,true,accent,new Rectangle(x,8,w,19),0,true);
            string reading=data.Distance.HasValue?Math.Round(data.Distance.Value).ToString(CultureInfo.InvariantCulture)+" m":UiLanguage.Text("Detected",language);
            if(data.Count>1)reading+="  +"+(data.Count-1).ToString(CultureInfo.InvariantCulture);
            distance.Draw(g,reading,23,true,pale,new Rectangle(x,29,w,28),0,true);
            string info;
            if(!data.Distance.HasValue)info=UiLanguage.Text("Move to locate cube",language);
            else {
                double z=data.Elevation.GetValueOrDefault();
                info=Math.Abs(z)<3?UiLanguage.Text("Same height",language):string.Format(UiLanguage.Text(z>0?"{0} m above":"{0} m below",language),Math.Round(Math.Abs(z)));
            }
            detail.Draw(g,info,12,false,Color.FromArgb(169,190,206),new Rectangle(x,57,w,18),0,true);
            if(data.Direction.HasValue&&size.Height>=100)
                footer.Draw(g,UiLanguage.Text("Relative to your movement",language),10,false,Color.FromArgb(132,153,168),new Rectangle(x,78,w,16),0,true);
            if(editing)using(var mark=new Pen(accent,1.5f))g.DrawLine(mark,size.Width-17,size.Height-7,size.Width-7,size.Height-17);
        }
        return image;
    }
    static void DrawDial(Graphics g,RectangleF box,Color accent,PointF? direction){
        float x=box.X+box.Width/2,y=box.Y+box.Height/2;
        using(var tile=SkillSlotPainter.Round(box,9))
        using(var fill=new SolidBrush(Color.FromArgb(55,accent)))
        using(var stroke=new Pen(accent,1.5f)){
            g.FillPath(fill,tile);
            if(direction.HasValue){
                using(var grid=new Pen(Color.FromArgb(95,accent))){
                    for(int n=1;n<=2;n++){float r=box.Width*n/5.6f;g.DrawEllipse(grid,x-r,y-r,r*2,r*2);}
                }
                float dx=x+direction.Value.X*box.Width*.29f,dy=y+direction.Value.Y*box.Width*.29f;
                g.DrawLine(stroke,x,y,dx,dy);
                using(var ink=new SolidBrush(accent)){
                    g.FillEllipse(ink,x-2,y-2,4,4);
                    g.FillPolygon(ink,new[]{new PointF(dx,dy-5),new PointF(dx+5,dy),new PointF(dx,dy+5),new PointF(dx-5,dy)});
                }
            }else{
                float r=box.Width*.23f, v=r*.58f;
                g.DrawPolygon(stroke,new[]{new PointF(x,y-v*2),new PointF(x+r,y-v),new PointF(x+r,y+v),new PointF(x,y+v*2),new PointF(x-r,y+v),new PointF(x-r,y-v)});
                g.DrawLines(stroke,new[]{new PointF(x-r,y-v),new PointF(x,y),new PointF(x+r,y-v)});
                g.DrawLine(stroke,x,y,x,y+v*2);
            }
        }
    }
    public void Dispose(){title.Dispose();distance.Dispose();detail.Dispose();footer.Dispose();}
}

internal sealed class HiddenCubeOverlay:IDisposable {
    readonly HiddenCubeSignal signal;
    readonly HiddenCubePainter painter=new HiddenCubePainter();
    readonly MovableOverlayWindow window=new MovableOverlayWindow("Hidden Cube Radar");
    HiddenCubeSignal.Reading displayed;
    double alpha,previousTime;
    internal int NextInterval {get;private set;}
    internal HiddenCubeOverlay(EnergyOverlay owner,HiddenCubeSignal signal){
        this.signal=signal;
        window.LimitResize=HiddenCubeOptions.LimitSize;
        window.Moved=bounds=>{var cfg=owner.Configuration;cfg.HiddenCube.Bounds=bounds;owner.ApplyConfiguration(cfg);};
    }
    internal static double Fade(double alpha,bool show,double elapsed,int milliseconds){
        if(milliseconds<=0)return show?1:0;
        return Math.Max(0,Math.Min(1,alpha+(show?1:-1)*Math.Max(0,elapsed)*1000/milliseconds));
    }
    internal void Maintain(HiddenCubeOptions o,bool editing,bool isolated,double utc,double now,string language){
        double dt=now-previousTime;previousTime=now;
        NextInterval=o.Enabled?125:1000;
        if(!o.Enabled||isolated||!editing&&!FloatingTextService.GameForeground()||DpsDesign.ShellUiActive()){
            alpha=0;window.HideOverlay();return;
        }
        var reading=signal.Read(utc);
        bool show=editing||reading.Count>0;
        if(!show&&alpha==0)NextInterval=250;
        if(show)displayed=editing&&reading.Count==0?HiddenCubePainter.Sample:reading;
        alpha=Fade(alpha,show,Math.Min(.04,dt),editing||!o.Fade?0:o.FadeMs);
        if(alpha>0&&alpha<1||show&&alpha==0)NextInterval=16;
        int opacity=(int)Math.Round(o.Opacity*2.55*alpha);
        if(opacity==0){window.HideOverlay();return;}
        string key=language+"/"+o.Accent+"/"+o.BackgroundOpacity+"/"+editing+"/"+displayed.Count+"/"
            +Math.Round(displayed.Distance.GetValueOrDefault(),0)+"/"+displayed.Distance.HasValue+"/"+Math.Round(displayed.Elevation.GetValueOrDefault(),0)+"/"+displayed.Direction;
        window.Render(o.Bounds,(byte)opacity,editing,key,size=>painter.Render(size,o,displayed,language,editing),o.Blur);
    }
    public void Dispose(){window.Dispose();painter.Dispose();}
}
