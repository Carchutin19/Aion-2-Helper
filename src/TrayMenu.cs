using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// The tray uses a native Windows popup. Keep the styled drop-down for widget clicks.
internal sealed class HelperTrayMenu : ContextMenuStrip {
    static readonly MenuColors Palette=new MenuColors();
    static readonly Color CommandText=Color.FromArgb(241,247,255), QuitText=Color.FromArgb(255,186,201);
    [StructLayout(LayoutKind.Sequential)]
    struct MenuInfo {
        internal uint Size,Mask,Style,MaximumHeight;
        internal IntPtr Background;
        internal uint HelpId;
        internal UIntPtr Data;
    }
    [DllImport("user32.dll")] static extern bool SetMenuInfo(IntPtr menu,ref MenuInfo info);
    [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(int color);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr handle);
    readonly EnergyOverlay owner;
    readonly ToolStripMenuItem settings,lockItem,quit;
    IntPtr nativeBackground;
    internal readonly ContextMenu Native;
    internal bool NativeOpen {get;private set;}
    internal HelperTrayMenu(EnergyOverlay overlay){
        owner=overlay;ShowImageMargin=false;ShowCheckMargin=false;AutoClose=true;
        Font=new Font("Segoe UI",9);BackColor=Palette.ToolStripDropDownBackground;ForeColor=CommandText;
        Padding=new Padding(7);Renderer=new GlassRenderer();
        settings=Command("Settings",delegate{owner.OpenSettings();});
        lockItem=Command("Unlock",delegate{owner.ToggleWidgetsLock();});
        Items.Add(new ToolStripSeparator());
        quit=Command("Quit Aion 2 Helper",delegate{owner.Close();});quit.ForeColor=QuitText;
        Native=new ContextMenu(new[]{NativeRow(settings,0),NativeRow(lockItem,1),NativeRow(null,2),NativeRow(quit,3)});
        nativeBackground=CreateSolidBrush(ColorTranslator.ToWin32(BackColor));
        Native.Popup+=delegate{RefreshCommands();ApplyNativeBackground();NativeOpen=true;};
        Native.Collapse+=delegate{NativeOpen=false;};
        Opening+=delegate{RefreshCommands();};RefreshCommands();
    }
    void ApplyNativeBackground(){
        if(nativeBackground==IntPtr.Zero)return;
        var info=new MenuInfo{Size=(uint)Marshal.SizeOf(typeof(MenuInfo)),Mask=2,Background=nativeBackground};
        SetMenuInfo(Native.Handle,ref info);
    }
    MenuItem NativeRow(ToolStripMenuItem command,int row){
        var item=new MenuItem(command==null?"-":command.Text){OwnerDraw=true};
        if(command!=null)item.Click+=delegate{NativeOpen=false;command.PerformClick();};
        item.MeasureItem+=delegate(object sender,MeasureItemEventArgs e){
            e.ItemWidth=Pixels(260,e.Graphics.DpiX);
            e.ItemHeight=Pixels(row==2?9:row==1?42:49,e.Graphics.DpiY);
        };
        item.DrawItem+=delegate(object sender,DrawItemEventArgs e){PaintNativeRow(e.Graphics,e.Bounds,row,item.Text,(e.State&DrawItemState.Selected)!=0);};
        return item;
    }
    static int Pixels(int size,float dpi){return Math.Max(1,(int)Math.Round(size*dpi/96));}
    void PaintNativeRow(Graphics g,Rectangle area,int row,string label,bool selected){
        using(var background=new SolidBrush(BackColor))g.FillRectangle(background,area);
        int inset=Pixels(7,g.DpiX),pad=Pixels(7,g.DpiY);
        var content=Rectangle.FromLTRB(area.Left+inset,area.Top+(row==0?pad:0),area.Right-inset,area.Bottom-(row==3?pad:0));
        if(content.Width<=1||content.Height<=0)return;
        if(row==2){
            using(var line=new Pen(Palette.SeparatorDark))g.DrawLine(line,content.Left,content.Top+content.Height/2,content.Right-1,content.Top+content.Height/2);
            return;
        }
        PaintGlass(g,content,selected);
        int gap=Pixels(12,g.DpiX),size=Pixels(17,g.DpiX);
        bool rtl=owner.Language=="ar";
        int x=rtl?content.Right-gap-size:content.Left+gap;
        var icon=new Rectangle(x,content.Top+(content.Height-size)/2,size,size);
        PaintGlyph(g,icon,row,row==3?QuitText:Palette.MenuItemBorder,owner.Configuration.Locked);
        var text=rtl?Rectangle.FromLTRB(content.Left+gap,content.Top,x-gap,content.Bottom)
            :Rectangle.FromLTRB(icon.Right+gap,content.Top,content.Right-gap,content.Bottom);
        var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;
        if(text.Width>0)TextRenderer.DrawText(g,label,Font,text,row==3?QuitText:CommandText,
            flags|(rtl?TextFormatFlags.RightToLeft|TextFormatFlags.Right:TextFormatFlags.Left));
    }
    static void PaintGlass(Graphics g,Rectangle bounds,bool selected){
        if(bounds.Width<2||bounds.Height<2)return;
        bounds.Width--;bounds.Height--;
        int arc=Pixels(15,g.DpiX);
        var smoothing=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=new GraphicsPath()){
            path.AddArc(bounds.Left,bounds.Top,arc,arc,180,90);
            path.AddArc(bounds.Right-arc,bounds.Top,arc,arc,270,90);
            path.AddArc(bounds.Right-arc,bounds.Bottom-arc,arc,arc,0,90);
            path.AddArc(bounds.Left,bounds.Bottom-arc,arc,arc,90,90);path.CloseFigure();
            using(var brush=new LinearGradientBrush(bounds,selected?Palette.MenuItemSelected:Color.FromArgb(40,62,104),
                selected?Color.FromArgb(47,58,123):Color.FromArgb(34,42,85),LinearGradientMode.ForwardDiagonal))g.FillPath(brush,path);
            using(var border=new Pen(selected?Palette.MenuItemBorder:Color.FromArgb(79,105,156)))g.DrawPath(border,path);
        }
        g.SmoothingMode=smoothing;
    }
    static void PaintGlyph(Graphics g,Rectangle icon,int row,Color color,bool locked){
        var state=g.Save();
        try{
            g.TranslateTransform(icon.X,icon.Y);g.ScaleTransform(icon.Width/18f,icon.Height/18f);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(color,1.35f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}){
                if(row==0){
                    g.DrawLines(pen,new[]{new Point(9,1),new Point(16,5),new Point(16,13),new Point(9,17),new Point(2,13),new Point(2,5),new Point(9,1)});
                    g.DrawEllipse(pen,6,6,6,6);
                }else if(row==1){
                    g.DrawRectangle(pen,3,8,12,8);
                    g.DrawArc(pen,locked?5:8,2,8,11,180,180);
                    if(locked)g.DrawLine(pen,5,7,5,8);
                    g.DrawLine(pen,9,11,9,13);
                }else{
                    g.DrawLines(pen,new[]{new Point(8,2),new Point(2,2),new Point(2,16),new Point(8,16)});
                    g.DrawLine(pen,6,9,16,9);g.DrawLines(pen,new[]{new Point(12,5),new Point(16,9),new Point(12,13)});
                }
            }
        }finally{g.Restore(state);}
    }
    sealed class GlassRenderer:ToolStripProfessionalRenderer {
        internal GlassRenderer():base(Palette){RoundedEdges=false;}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
            var bounds=new Rectangle(Point.Empty,e.Item.Size);bounds.Inflate(-1,-2);PaintGlass(e.Graphics,bounds,e.Item.Selected);
        }
    }
    ToolStripMenuItem Command(string label,Action action){
        var item=new ToolStripMenuItem(label){AutoSize=false,Size=new Size(230,37),Padding=new Padding(12,6,12,6)};
        item.Click+=delegate{Close(ToolStripDropDownCloseReason.ItemClicked);action();};Items.Add(item);return item;
    }
    internal void RefreshCommands(){
        settings.Text=UiLanguage.Text("Settings",owner.Language);
        lockItem.Text=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language);
        quit.Text=UiLanguage.Text("Quit Aion 2 Helper",owner.Language);
        RightToLeft=owner.Language=="ar"?RightToLeft.Yes:RightToLeft.No;
        Native.MenuItems[0].Text=settings.Text;
        Native.MenuItems[1].Text=lockItem.Text;
        Native.MenuItems[3].Text=quit.Text;
    }
    protected override void Dispose(bool disposing){
        if(disposing&&Native!=null)Native.Dispose();
        if(disposing&&nativeBackground!=IntPtr.Zero){DeleteObject(nativeBackground);nativeBackground=IntPtr.Zero;}
        base.Dispose(disposing);
    }
    internal void ShowAt(Point position){if(!Visible)Show(position);}
    internal bool VerifyLanguage(string label){RefreshCommands();return settings.Text==label&&Native.MenuItems[0].Text==label;}
    internal void RenderPreview(string path){
        RefreshCommands();
        var rows=new[]{49,42,9,49};
        using(var bitmap=new Bitmap(260,149)){
            bitmap.SetResolution(96,96);
            using(var g=Graphics.FromImage(bitmap)){
                int y=0;
                for(int i=0;i<rows.Length;i++){PaintNativeRow(g,new Rectangle(0,y,260,rows[i]),i,Native.MenuItems[i].Text,i==0);y+=rows[i];}
            }
            bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    internal void Verify(){
        RefreshCommands();if(Items.Count!=4||Native.MenuItems.Count!=4||!AutoClose||lockItem.Text!=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language)||Native.MenuItems[1].Text!=lockItem.Text)throw new Exception("Native tray commands and automatic dismissal");
        foreach(MenuItem item in Native.MenuItems)if(!item.OwnerDraw)throw new Exception("Native tray rows must retain the Helper theme");
        using(var bitmap=new Bitmap(244,44)){
            using(var g=Graphics.FromImage(bitmap))PaintNativeRow(g,new Rectangle(0,0,244,44),0,settings.Text,true);
            if(bitmap.GetPixel(0,0).ToArgb()!=BackColor.ToArgb()||bitmap.GetPixel(125,14).ToArgb()==BackColor.ToArgb())throw new Exception("Tray popup must paint a selected glass row over its background");
        }
        // ToolStrip constrains its drop-down to the monitor, including negative-coordinate monitors.
        foreach(var screen in Screen.AllScreens){ShowAt(new Point(screen.WorkingArea.Right-2,screen.WorkingArea.Bottom-2));Application.DoEvents();if(!screen.WorkingArea.Contains(Bounds))throw new Exception("Tray menu monitor bounds");Close();}
    }
    internal void VerifyLockCommand(EnergyOverlay overlay){
        overlay.Show();Application.DoEvents();var before=overlay.Configuration;Native.MenuItems[1].PerformClick();Application.DoEvents();
        if(Visible||overlay.Configuration.Locked==before.Locked)throw new Exception("Tray lock command must close and toggle shared lock");
        overlay.UndoConfiguration();if(!ConfigurationHistory.Same(overlay.Configuration,before))throw new Exception("Tray lock must use shared undo history");
    }
    internal void VerifyLifetime(EnergyOverlay overlay){
        overlay.OpenTrayMenu(Cursor.Position);Application.DoEvents();var menu=overlay.ContextMenuStrip;
        overlay.OpenTrayMenu(Cursor.Position);Application.DoEvents();
        if(!menu.Visible||!ReferenceEquals(menu,overlay.ContextMenuStrip))throw new Exception("Repeated opening must retain the visible menu");
        var actual=(HelperTrayMenu)menu;actual.ProcessDialogKey(Keys.Escape);Application.DoEvents();
        if(menu.Visible||menu.IsDisposed)throw new Exception("Escape must dismiss without disposing the reusable menu");
        overlay.OpenTrayMenu(Cursor.Position);Application.DoEvents();menu.Close(ToolStripDropDownCloseReason.AppClicked);Application.DoEvents();
        if(menu.Visible||menu.IsDisposed)throw new Exception("Outside dismissal must preserve the reusable menu");
        overlay.OpenTrayMenu(Cursor.Position);Application.DoEvents();if(!menu.Visible)throw new Exception("Tray menu must reopen after dismissal");menu.Close();
    }
    sealed class MenuColors : ProfessionalColorTable {
        public override Color ToolStripDropDownBackground {get{return Color.FromArgb(17,27,56);}}
        public override Color ImageMarginGradientBegin {get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientMiddle {get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientEnd {get{return ToolStripDropDownBackground;}}
        public override Color MenuBorder {get{return Color.FromArgb(87,115,171);}}
        public override Color MenuItemSelected {get{return Color.FromArgb(52,124,188);}}
        public override Color MenuItemBorder {get{return Color.FromArgb(151,231,255);}}
        public override Color SeparatorDark {get{return MenuBorder;}}
        public override Color SeparatorLight {get{return ToolStripDropDownBackground;}}
    }
}
