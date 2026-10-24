using System;
using System.Drawing;
using System.Windows.Forms;

// Use the framework's actual tray drop-down: NotifyIcon transfers foreground ownership
// before showing it. A separate activating WPF window races Explorer's overflow flyout.
internal sealed class HelperTrayMenu : ContextMenuStrip {
    readonly EnergyOverlay owner;
    readonly ToolStripMenuItem settings,lockItem,quit;
    internal HelperTrayMenu(EnergyOverlay overlay){
        owner=overlay;ShowImageMargin=false;ShowCheckMargin=false;AutoClose=true;
        Font=new Font("Segoe UI",9);BackColor=Color.FromArgb(25,28,35);ForeColor=Color.FromArgb(232,236,243);
        Padding=new Padding(7);Renderer=new ToolStripProfessionalRenderer(new MenuColors()){RoundedEdges=false};
        settings=Command("Settings",delegate{owner.OpenSettings();});
        lockItem=Command("Unlock",delegate{owner.ToggleWidgetsLock();});
        Items.Add(new ToolStripSeparator());
        quit=Command("Quit Aion 2 Helper",delegate{owner.Close();});quit.ForeColor=Color.FromArgb(223,168,162);
        Opening+=delegate{RefreshCommands();};RefreshCommands();
    }
    ToolStripMenuItem Command(string label,Action action){
        var item=new ToolStripMenuItem(label){AutoSize=false,Size=new Size(230,37),Padding=new Padding(12,6,12,6)};
        item.Click+=delegate{Close(ToolStripDropDownCloseReason.ItemClicked);action();};Items.Add(item);return item;
    }
    internal void RefreshCommands(){
        settings.Text=UiLanguage.Text("Settings",owner.Language);
        lockItem.Text=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language);
        quit.Text=UiLanguage.Text("Quit Aion 2 Helper",owner.Language);
    }
    internal void ShowAt(Point position){if(!Visible)Show(position);}
    internal bool VerifyLanguage(string label){RefreshCommands();return settings.Text==label;}
    internal void RenderPreview(string path){
        RefreshCommands();PerformLayout();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
    }
    internal void Verify(){
        RefreshCommands();if(Items.Count!=4||!AutoClose||lockItem.Text!=UiLanguage.Text(owner.Configuration.Locked?"Unlock":"Lock",owner.Language))throw new Exception("Native tray commands and automatic dismissal");
        // ToolStrip constrains its drop-down to the monitor, including negative-coordinate monitors.
        foreach(var screen in Screen.AllScreens){ShowAt(new Point(screen.WorkingArea.Right-2,screen.WorkingArea.Bottom-2));Application.DoEvents();if(!screen.WorkingArea.Contains(Bounds))throw new Exception("Tray menu monitor bounds");Close();}
    }
    internal void VerifyLockCommand(EnergyOverlay overlay){
        overlay.Show();Application.DoEvents();var before=overlay.Configuration;ShowAt(Cursor.Position);lockItem.PerformClick();Application.DoEvents();
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
