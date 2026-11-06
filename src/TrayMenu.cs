using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// The tray uses a native Windows popup. Keep the styled drop-down for widget clicks.
internal sealed class HelperTrayMenu : ContextMenuStrip {
    static readonly MenuColors Palette=new MenuColors();
    static readonly Color CommandText=Color.FromArgb(232,236,243), QuitText=Color.FromArgb(223,168,162);
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
        Padding=new Padding(7);Renderer=new ToolStripProfessionalRenderer(Palette){RoundedEdges=false};
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
            e.ItemWidth=Pixels(244,e.Graphics.DpiX);
            e.ItemHeight=Pixels(row==2?7:row==1?37:44,e.Graphics.DpiY);
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
        if(selected){
            using(var brush=new SolidBrush(Palette.MenuItemSelected))g.FillRectangle(brush,content);
            using(var pen=new Pen(Palette.MenuItemBorder))g.DrawRectangle(pen,content.X,content.Y,content.Width-1,content.Height-1);
        }
        content.Inflate(-Pixels(12,g.DpiX),0);
        if(content.Width>0)TextRenderer.DrawText(g,label,Font,content,row==3?QuitText:CommandText,
            TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
    ToolStripMenuItem Command(string label,Action action){
        var item=new ToolStripMenuItem(label){AutoSize=false,Size=new Size(230,37),Padding=new Padding(12,6,12,6)};
        item.Click+=delegate{Close(ToolStripDropDownCloseReason.ItemClicked);action();};Items.Add(item);return item;
    }
    internal void RefreshCommands(){
        settings.Text=UiLanguage.Text("Settings",owner.Language);
        lockItem.Text=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language);
        quit.Text=UiLanguage.Text("Quit Aion 2 Helper",owner.Language);
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
        RefreshCommands();PerformLayout();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
    }
    internal void Verify(){
        RefreshCommands();if(Items.Count!=4||Native.MenuItems.Count!=4||!AutoClose||lockItem.Text!=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language)||Native.MenuItems[1].Text!=lockItem.Text)throw new Exception("Native tray commands and automatic dismissal");
        foreach(MenuItem item in Native.MenuItems)if(!item.OwnerDraw)throw new Exception("Native tray rows must retain the Helper theme");
        using(var bitmap=new Bitmap(244,44)){
            using(var g=Graphics.FromImage(bitmap))PaintNativeRow(g,new Rectangle(0,0,244,44),0,settings.Text,true);
            if(bitmap.GetPixel(0,0).ToArgb()!=BackColor.ToArgb()||bitmap.GetPixel(12,12).ToArgb()!=Palette.MenuItemSelected.ToArgb())throw new Exception("Tray popup background and selected row must use the Helper palette");
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
        public override Color ToolStripDropDownBackground {get{return Color.FromArgb(25,28,35);}}
        public override Color ImageMarginGradientBegin {get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientMiddle {get{return ToolStripDropDownBackground;}}
        public override Color ImageMarginGradientEnd {get{return ToolStripDropDownBackground;}}
        public override Color MenuBorder {get{return Color.FromArgb(60,68,76);}}
        public override Color MenuItemSelected {get{return Color.FromArgb(40,66,65);}}
        public override Color MenuItemBorder {get{return Color.FromArgb(103,199,176);}}
        public override Color SeparatorDark {get{return Color.FromArgb(60,68,76);}}
        public override Color SeparatorLight {get{return ToolStripDropDownBackground;}}
    }
}
