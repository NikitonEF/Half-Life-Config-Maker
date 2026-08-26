#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace conmaker
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const int WM_SETREDRAW = 11;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_GETMINMAXINFO = 0x0024;

        private const int HTCLIENT = 1;
        private const int HT_CAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTTRANSPARENT = -1;

        private const int RESIZE_HANDLE_SIZE = 8;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTL { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO { public POINTL ptReserved; public POINTL ptMaxSize; public POINTL ptMaxPosition; public POINTL ptMinTrackSize; public POINTL ptMaxTrackSize; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        private List<string> popularCommands = new List<string>
        {
            "weapon_crowbar", "weapon_9mmhandgun", "weapon_357", "weapon_shotgun",
            "weapon_crossbow", "weapon_rpg", "weapon_gauss", "weapon_egon",
            "weapon_snark", "weapon_tripmine", "weapon_satchel", "weapon_hornetgun",
            "weapon_9mmAR", "+attack", "+attack2", "+jump", "+duck", "+forward", "+back",
            "+moveleft", "+moveright", "+use", "+reload", "drop", "invnext", "invprev",
            "say", "say_team", "say_close", "play_close", "stopsound", "agstart", "agpause",
            "spectate", "retry", "customtimer", "+showscores", "-showscores", "loadauthid", "unloadauthid",
            "cancelselect", "escape", "+moveup", "sizeup", "sizedown", "+movedown", "+mlook", "toggleconsole",
            "+voicerecord", "messagemode2", "messagemode", "+left", "+right", "snapshot", "+strafe",
            "save quick", "load quick", "+klook", "+lookdown", "+lookup", "centerview", "pause", "exec",
            "slot1", "slot2", "slot3", "slot4", "slot5", "impulse 100", "impulse 201", "lastinv", "quit prompt"
        };

        private Dictionary<string, string> defaultBindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"TAB", "+showscores"}, {"ENTER", "+attack"}, {"ESC", "cancelselect"}, {"ESCAPE", "escape"}, {"SPACE", "+jump"},
            {"'", "+moveup"}, {"+", "sizeup"}, {",", "+moveleft"}, {"-", "sizedown"}, {".", "+moveright"},
            {"/", "+movedown"}, {"1", "slot1"}, {"2", "slot2"}, {"3", "slot3"},
            {"4", "slot4"}, {"5", "slot5"}, {";", "+mlook"}, {"=", "sizeup"},
            {"[", "invprev"}, {"]", "invnext"}, {"`", "toggleconsole"}, {"~", "toggleconsole"},
            {"A", "+moveleft"}, {"C", "+movedown"}, {"D", "+moveright"}, {"E", "+use"},
            {"F", "impulse 100"}, {"K", "+voicerecord"}, {"Q", "lastinv"}, {"R", "+reload"}, {"S", "+back"}, {"T", "impulse 201"},
            {"U", "messagemode2"}, {"V", "+moveup"}, {"W", "+forward"}, {"Y", "messagemode"},
            {"UPARROW", "+forward"}, {"DOWNARROW", "+back"},
            {"LEFTARROW", "+left"}, {"RIGHTARROW", "+right"}, {"ALT", "+strafe"}, {"CTRL", "+duck"},
            {"SHIFT", "+speed"}, {"F5", "snapshot"}, {"F6", "save quick"}, {"F7", "load quick"},
            {"F10", "quit prompt"}, {"INS", "+klook"}, {"PGDN", "+lookdown"}, {"PGUP", "+lookup"},
            {"END", "centerview"}, {"MWHEELDOWN", "invnext"}, {"MWHEELUP", "invprev"}, {"MOUSE1", "+attack"}, {"MOUSE2", "+attack2"},
            {"PAUSE", "pause"}
        };

        private Dictionary<string, string> bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, string[]> settingsCategories = new Dictionary<string, string[]>
        {
            { "ОСНОВНОЕ", new string[] { "name", "default_fov", "fps_max", "fps_override", "cl_bob", "cl_hidecorpses", "m_rawinput", "m_filter", "zoom_sensitivity_ratio", "cl_autojump", "cl_autorecord" } },
            { "СЕТЬ", new string[] { "rate", "cl_updaterate", "cl_cmdrate", "ex_interp", "cl_dlmax", "cl_lc", "cl_lw", "cl_cmdbackup", "cl_timeout", "cl_resend", "cl_latency" } },
            { "ЗВУК", new string[] { "volume", "hisound", "bgmvolume", "MP3Volume", "suitvolume", "voice_enable", "voice_scale", "ambient_level", "room_off", "s_a3d", "s_eax" } },
            { "ВИДЕО", new string[] { "gamma", "brightness", "r_drawviewmodel", "gl_vsync", "cl_forceenemymodels", "cl_forceteammatemodels", "hud_fastswitch", "net_graph" } },
            { "ПРИЦЕЛ", new string[] { "cl_cross", "cl_cross_size", "cl_cross_color", "cl_cross_thickness", "cl_cross_gap", "cl_cross_dot_size", "cl_cross_alpha" } }
        };
        private Dictionary<string, string> settingsValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private PrivateFontCollection pfc = new PrivateFontCollection();
        private Font iconFont;
        private bool showIcons = false;
        private bool isDarkMode = true;
        private bool hasUnbindAll = false;
        private Rectangle normalWindowBounds;

        private Color bgColor => isDarkMode ? Color.FromArgb(30, 30, 35) : Color.FromArgb(240, 240, 245);
        private Color btnColor => isDarkMode ? Color.FromArgb(45, 45, 50) : Color.FromArgb(220, 220, 225);
        private Color textColor => isDarkMode ? Color.White : Color.Black;
        private Color idleTextColor => isDarkMode ? Color.LightGray : Color.FromArgb(70, 70, 70);

        private Dictionary<string, string> iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"weapon_handgrenade", "\uf000"}, {"weapon_9mmAR", "\uf001"}, {"weapon_rpg", "\uf002"},
            {"weapon_smg1", "\uf003"}, {"weapon_shotgun", "\uf004"}, {"weapon_357", "\uf005"},
            {"weapon_snark", "\uf006"}, {"weapon_crossbow", "\uf007"}, {"weapon_crowbar", "\uf008"},
            {"weapon_gauss", "\uf009"}, {"weapon_egon", "\uf009"}
        };

        private List<string> originalFileLines = new List<string>();
        private string currentFilePath = "";
        private bool isNewConfig = false;
        private bool isEnglish = false;
        private string currentTab = "ОСНОВНОЕ";

        private TransparentPanel pnlTitleBar;
        private TransparentLabel lblAppTitle;
        private DBPanel keyboardPanel;
        private DBPanel settingsPanel;
        private RichTextBox txtAliases;
        private Label lblAliases;
        private Panel pnlAliasBorder;
        private Button btnSnippets;

        private System.Windows.Forms.Timer syntaxTimer;
        private System.Windows.Forms.Timer magicRefreshTimer;
        private DBPanel pnlCrosshairPreview;

        private Button btnOpen, btnNew, btnSave, btnChecklist, btnToggleIcons, btnUnbindAll, btnLang, btnTheme;
        private ToolTip globalToolTip;

        private Dictionary<string, string> displayNames = new Dictionary<string, string>
        {
            {"UPARROW", "UP"}, {"DOWNARROW", "DOWN"}, {"LEFTARROW", "LEFT"}, {"RIGHTARROW", "RIGHT"},
            {"INS", "INS"}, {"DEL", "DEL"}, {"HOME", "HOME"}, {"END", "END"}, {"PGUP", "PG UP"}, {"PGDN", "PG DN"},
            {"KP_SLASH", "/"}, {"*", "*"}, {"KP_MINUS", "-"}, {"KP_PLUS", "+"}, {"KP_ENTER", "ENTER"},
            {"KP_DEL", ".\nDEL"}, {"KP_INS", "0\nINS"}, {"KP_END", "1\nEND"},
            {"KP_DOWNARROW", "2\nDOWN"}, {"KP_PGDN", "3\nPG DN"}, {"KP_LEFTARROW", "4\nLEFT"},
            {"KP_5", "5"}, {"KP_RIGHTARROW", "6\nRIGHT"}, {"KP_HOME", "7\nHOME"},
            {"KP_UPARROW", "8\nUP"}, {"KP_PGUP", "9\nPG UP"}, {"NUMLOCK", "NUM"}
        };

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        private class DBPanel : Panel
        {
            public DBPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        private class TransparentPanel : Panel
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)HTTRANSPARENT; return; }
                base.WndProc(ref m);
            }
        }

        private class TransparentLabel : Label
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)HTTRANSPARENT; return; }
                base.WndProc(ref m);
            }
        }

        private string GetLastConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_cfg.txt");
        }

        private void WmGetMinMaxInfo(ref Message m)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO));
            IntPtr monitor = MonitorFromWindow(this.Handle, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, ref monitorInfo);

                Rectangle rcWorkArea = new Rectangle(monitorInfo.rcWork.Left, monitorInfo.rcWork.Top, monitorInfo.rcWork.Right - monitorInfo.rcWork.Left, monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);

                mmi.ptMaxPosition.X = 0;
                mmi.ptMaxPosition.Y = 0;
                mmi.ptMaxSize.X = rcWorkArea.Width;
                mmi.ptMaxSize.Y = rcWorkArea.Height;
                mmi.ptMaxTrackSize.X = rcWorkArea.Width;
                mmi.ptMaxTrackSize.Y = rcWorkArea.Height;
            }
            Marshal.StructureToPtr(mmi, m.LParam, true);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                base.WndProc(ref m);
                WmGetMinMaxInfo(ref m);
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT || (int)m.Result == HTTRANSPARENT)
                {
                    int x = unchecked((short)(long)m.LParam);
                    int y = unchecked((short)((long)m.LParam >> 16));
                    Point pt = this.PointToClient(new Point(x, y));

                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        if (pt.Y <= 35 && pt.X < this.ClientSize.Width - 120) m.Result = (IntPtr)HT_CAPTION;
                        return;
                    }

                    bool onLeft = pt.X <= RESIZE_HANDLE_SIZE;
                    bool onRight = pt.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE;
                    bool onTop = pt.Y <= RESIZE_HANDLE_SIZE;
                    bool onBottom = pt.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE;

                    if (onTop && onLeft) m.Result = (IntPtr)HTTOPLEFT;
                    else if (onTop && onRight) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (onBottom && onLeft) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (onBottom && onRight) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (onLeft) m.Result = (IntPtr)HTLEFT;
                    else if (onRight) m.Result = (IntPtr)HTRIGHT;
                    else if (onTop) m.Result = (IntPtr)HTTOP;
                    else if (onBottom) m.Result = (IntPtr)HTBOTTOM;
                    else if (pt.Y <= 35 && pt.X < this.ClientSize.Width - 120) m.Result = (IntPtr)HT_CAPTION;
                }
                return;
            }
            base.WndProc(ref m);
        }

        public Form1()
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "Half-Life Config Maker";
            this.Size = new Size(1650, 800);
            this.MinimumSize = new Size(1300, 680);
            this.BackColor = bgColor;
            this.ShowIcon = false;
            this.normalWindowBounds = this.Bounds;

            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            globalToolTip = new ToolTip();
            InitCustomFont();
            SetDefaultSettings();

            CreateTitleBar();

            int topMenuY = 45;

            btnOpen = new Button { Location = new Point(20, topMenuY), Size = new Size(150, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(70, 130, 180));
            btnOpen.Click += BtnOpen_Click;
            AttachHover(btnOpen);
            this.Controls.Add(btnOpen);

            btnNew = new Button { Location = new Point(180, topMenuY), Size = new Size(150, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(218, 165, 32));
            btnNew.Click += BtnNew_Click;
            AttachHover(btnNew);
            this.Controls.Add(btnNew);

            btnSave = new Button { Location = new Point(340, topMenuY), Size = new Size(160, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(60, 179, 113));
            btnSave.Click += BtnSave_Click;
            AttachHover(btnSave);
            this.Controls.Add(btnSave);

            btnChecklist = new Button { Location = new Point(510, topMenuY), Size = new Size(120, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnChecklist.FlatAppearance.BorderSize = 0;
            btnChecklist.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(147, 112, 219));
            btnChecklist.Click += BtnChecklist_Click;
            AttachHover(btnChecklist);
            this.Controls.Add(btnChecklist);

            btnUnbindAll = new Button { Location = new Point(640, topMenuY), Size = new Size(120, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnUnbindAll.FlatAppearance.BorderSize = 0;
            btnUnbindAll.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(200, 80, 80));
            btnUnbindAll.Click += (s, e) => {
                hasUnbindAll = !hasUnbindAll;
                UpdateUIThemeAndLanguage();
            };
            AttachHover(btnUnbindAll);
            this.Controls.Add(btnUnbindAll);

            btnToggleIcons = new Button { Size = new Size(40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14, FontStyle.Regular) };
            btnToggleIcons.FlatAppearance.BorderSize = 0;
            btnToggleIcons.Click += (s, e) => {
                showIcons = !showIcons;
                UpdateUIThemeAndLanguage();
                if (keyboardPanel != null) keyboardPanel.Invalidate(true);
            };
            AttachHover(btnToggleIcons);
            this.Controls.Add(btnToggleIcons);

            btnTheme = new Button { Size = new Size(40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14, FontStyle.Regular) };
            btnTheme.FlatAppearance.BorderSize = 0;
            btnTheme.Click += (s, e) => {
                isDarkMode = !isDarkMode;
                UpdateUIThemeAndLanguage();
            };
            AttachHover(btnTheme);
            this.Controls.Add(btnTheme);

            btnLang = new Button { Size = new Size(40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnLang.FlatAppearance.BorderSize = 0;
            btnLang.Click += (s, e) => {
                isEnglish = !isEnglish;
                UpdateUIThemeAndLanguage();
            };
            AttachHover(btnLang);
            this.Controls.Add(btnLang);

            keyboardPanel = new DBPanel
            {
                Location = new Point(20, topMenuY + 55),
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - (topMenuY + 55) - 20),
                AllowDrop = true
            };
            keyboardPanel.DragEnter += Form1_DragEnter;
            keyboardPanel.DragDrop += Form1_DragDrop;
            this.Controls.Add(keyboardPanel);

            lblAliases = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            btnSnippets = new Button { Text = "+", Size = new Size(25, 25), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSnippets.FlatAppearance.BorderSize = 0;
            btnSnippets.Click += BtnSnippets_Click;

            pnlAliasBorder = new Panel { Padding = new Padding(1) };
            txtAliases = new RichTextBox { BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10), HideSelection = false, Dock = DockStyle.Fill, WordWrap = false };
            pnlAliasBorder.Controls.Add(txtAliases);

            syntaxTimer = new System.Windows.Forms.Timer { Interval = 800 };
            syntaxTimer.Tick += (s, e) => { syntaxTimer.Stop(); HighlightSyntax(); };
            txtAliases.TextChanged += (s, e) => { syntaxTimer.Stop(); syntaxTimer.Start(); };

            keyboardPanel.Controls.Add(lblAliases);
            keyboardPanel.Controls.Add(btnSnippets);
            keyboardPanel.Controls.Add(pnlAliasBorder);

            magicRefreshTimer = new System.Windows.Forms.Timer { Interval = 30 };
            magicRefreshTimer.Tick += (s, e) => {
                magicRefreshTimer.Stop();
                if (keyboardPanel != null && this.WindowState != FormWindowState.Minimized)
                {
                    DrawInterface();
                    this.Refresh();
                }
            };

            this.Resize += (s, e) => {
                if (pnlTitleBar != null)
                {
                    pnlTitleBar.Top = 0;
                }
                if (keyboardPanel != null)
                {
                    keyboardPanel.Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - (topMenuY + 55) - 20);
                }
                magicRefreshTimer.Stop();
                magicRefreshTimer.Start();
            };

            UpdateUIThemeAndLanguage();

            string memFile = GetLastConfigPath();
            if (File.Exists(memFile))
            {
                string lastPath = File.ReadAllText(memFile).Trim();
                if (File.Exists(lastPath))
                {
                    currentFilePath = lastPath;
                    isNewConfig = false;
                    ParseConfig(currentFilePath);
                }
            }
        }

        private void CreateTitleBar()
        {
            pnlTitleBar = new TransparentPanel { Location = new Point(0, 0), Size = new Size(this.Width, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(20, 20, 25) };
            PictureBox picIcon = new PictureBox
            {
                Image = conmaker.Properties.Resources.HLCM.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(18, 18),
                Location = new Point(10, 6)
            };
            pnlTitleBar.Controls.Add(picIcon);

            lblAppTitle = new TransparentLabel { Text = "Half-Life Config Maker", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(38, 6) };
            pnlTitleBar.Controls.Add(lblAppTitle);

            pnlTitleBar.DoubleClick += ToggleMaximize;
            lblAppTitle.DoubleClick += ToggleMaximize;

            Button btnMin = new Button { Text = "—", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMin.MouseEnter += (s, e) => { btnMin.BackColor = Color.FromArgb(50, 50, 55); btnMin.ForeColor = Color.White; };
            btnMin.MouseLeave += (s, e) => { btnMin.BackColor = Color.Transparent; btnMin.ForeColor = Color.Gray; };

            Button btnMax = new Button { Text = "◻", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("Segoe UI", 12) };
            btnMax.FlatAppearance.BorderSize = 0;
            btnMax.Click += ToggleMaximize;
            btnMax.MouseEnter += (s, e) => { btnMax.BackColor = Color.FromArgb(50, 50, 55); btnMax.ForeColor = Color.White; };
            btnMax.MouseLeave += (s, e) => { btnMax.BackColor = Color.Transparent; btnMax.ForeColor = Color.Gray; };

            Button btnClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Crimson; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Color.Gray; };

            pnlTitleBar.Controls.Add(btnMin);
            pnlTitleBar.Controls.Add(btnMax);
            pnlTitleBar.Controls.Add(btnClose);

            this.Controls.Add(pnlTitleBar);
        }

        private void ToggleMaximize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                this.Bounds = normalWindowBounds;
            }
            else
            {
                normalWindowBounds = this.Bounds;
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
            }

            if (magicRefreshTimer != null)
            {
                magicRefreshTimer.Stop();
                magicRefreshTimer.Start();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0 && files[0].EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
            {
                currentFilePath = files[0];
                isNewConfig = false;
                ParseConfig(currentFilePath);
                DrawInterface();
                this.Refresh();
            }
        }

        private void BtnSnippets_Click(object sender, EventArgs e)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = btnColor;
            menu.ForeColor = textColor;
            menu.ShowImageMargin = false;

            menu.Items.Add("Bunnyhop (Auto-Jump)", null, (s, ev) => InsertSnippet("alias \"+bhop\" \"alias _special @bhop;@bhop\"\nalias \"-bhop\" \"alias _special\"\nalias \"@bhop\" \"special;wait;+jump;wait;-jump\""));
            menu.Items.Add("Double Duck", null, (s, ev) => InsertSnippet("alias \"+doubleduck\" \"-duck;wait;+duck;wait;-duck;wait;+duck\"\nalias \"-doubleduck\" \"-duck\""));
            menu.Items.Add("Duckroll (Russian Duck)", null, (s, ev) => InsertSnippet("alias \"+duckroll\" \"alias _zpecial duckroll;duckroll\"\nalias \"-duckroll\" \"alias _zpecial\"\nalias \"duckroll\" \"+duck;wait;-duck;wait;zpecial\""));
            menu.Items.Add("Fast Zoom (Crossbow)", null, (s, ev) => InsertSnippet("alias \"fastzoom\" \"+attack2;+attack;wait;wait;lastinv;lastinv;-attack;-attack2\""));
            menu.Items.Add("Movement Scripts (No-stuck)", null, (s, ev) => InsertSnippet("alias \"+mfwd\" \"-back;+forward;alias checkfwd +forward\"\nalias \"+mback\" \"-forward;+back;alias checkback +back\"\nalias \"+mleft\" \"-moveright;+moveleft;alias checkleft +moveleft\"\nalias \"+mright\" \"-moveleft;+moveright;alias checkright +moveright\"\nalias \"-mfwd\" \"-forward;checkback;alias checkfwd none\"\nalias \"-mback\" \"-back;checkfwd;alias checkback none\"\nalias \"-mleft\" \"-moveleft;checkright;alias checkleft none\"\nalias \"-mright\" \"-moveright;checkleft;alias checkright none\"\nalias \"checkfwd\" \"none\"\nalias \"checkback\" \"none\"\nalias \"checkleft\" \"none\"\nalias \"checkright\" \"none\"\nalias \"none\" \"\""));

            menu.Show(btnSnippets, new Point(0, btnSnippets.Height));
        }

        private void InsertSnippet(string text)
        {
            if (!string.IsNullOrWhiteSpace(txtAliases.Text)) txtAliases.AppendText("\n\n");
            txtAliases.AppendText(text);
            txtAliases.Focus();
            txtAliases.ScrollToCaret();
            HighlightSyntax();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private const int EM_GETSCROLLPOS = 0x0400 + 221;
        private const int EM_SETSCROLLPOS = 0x0400 + 222;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref POINT lParam);

        private void HighlightSyntax()
        {
            if (string.IsNullOrWhiteSpace(txtAliases.Text)) return;

            SendMessage(txtAliases.Handle, WM_SETREDRAW, 0, 0);
            int selStart = txtAliases.SelectionStart;
            int selLen = txtAliases.SelectionLength;

            POINT scrollPos = new POINT();
            SendMessage(txtAliases.Handle, EM_GETSCROLLPOS, 0, ref scrollPos);

            txtAliases.SelectAll();
            txtAliases.SelectionColor = textColor;

            Regex rAlias = new Regex(@"\balias\b", RegexOptions.IgnoreCase);
            Regex rQuotes = new Regex("\".*?\"");
            Regex rCommands = new Regex(@"(\+|-)[a-zA-Z0-9_]+");

            Color aliasCol = Color.FromArgb(147, 112, 219);
            Color quoteCol = isDarkMode ? Color.Gold : Color.DarkGoldenrod;
            Color cmdCol = isDarkMode ? Color.SpringGreen : Color.ForestGreen;

            foreach (Match m in rQuotes.Matches(txtAliases.Text))
            {
                txtAliases.Select(m.Index, m.Length);
                txtAliases.SelectionColor = quoteCol;
            }
            foreach (Match m in rCommands.Matches(txtAliases.Text))
            {
                txtAliases.Select(m.Index, m.Length);
                txtAliases.SelectionColor = cmdCol;
            }
            foreach (Match m in rAlias.Matches(txtAliases.Text))
            {
                txtAliases.Select(m.Index, m.Length);
                txtAliases.SelectionColor = aliasCol;
            }

            txtAliases.Select(selStart, selLen);

            SendMessage(txtAliases.Handle, EM_SETSCROLLPOS, 0, ref scrollPos);
            SendMessage(txtAliases.Handle, WM_SETREDRAW, 1, 0);
            txtAliases.Invalidate();
        }

        private void DrawTopAccent(object sender, PaintEventArgs e, Color color)
        {
            if (sender is Button btn)
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, btn.Width, 4);
                }
            }
        }

        private void AttachHover(Button btn)
        {
            btn.MouseEnter += (s, e) => {
                if (btn.BackColor == btnColor)
                    btn.BackColor = isDarkMode ? Color.FromArgb(60, 60, 65) : Color.FromArgb(200, 200, 205);
            };
            btn.MouseLeave += (s, e) => {
                if (btn.BackColor == Color.FromArgb(60, 60, 65) || btn.BackColor == Color.FromArgb(200, 200, 205) || btn.BackColor == Color.DarkOrange)
                    btn.BackColor = btnColor;
            };
            btn.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                    btn.BackColor = Color.DarkOrange;
            };
            btn.MouseUp += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    if (btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)))
                        btn.BackColor = isDarkMode ? Color.FromArgb(60, 60, 65) : Color.FromArgb(200, 200, 205);
                    else
                        btn.BackColor = btnColor;
                }
            };
        }

        private void SetDefaultSettings()
        {
            foreach (var cat in settingsCategories.Values)
                foreach (var key in cat) settingsValues[key] = "";
            settingsValues["rate"] = "250000";
            settingsValues["ex_interp"] = "0.01";

            settingsValues["cl_cross_size"] = "5";
            settingsValues["cl_cross_color"] = "0 255 0";
            settingsValues["cl_cross_thickness"] = "2";
            settingsValues["cl_cross_gap"] = "3";
        }

        private void InitCustomFont()
        {
            try
            {
                byte[] fontData = Properties.Resources.hlfont;
                IntPtr data = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, data, fontData.Length);
                pfc.AddMemoryFont(data, fontData.Length);
                Marshal.FreeCoTaskMem(data);

                iconFont = new Font(pfc.Families[0], 28f, FontStyle.Regular);
            }
            catch
            {
                iconFont = new Font("Segoe UI", 12f, FontStyle.Bold);
            }
        }

        private void UpdateUIThemeAndLanguage()
        {
            this.BackColor = bgColor;
            if (pnlTitleBar != null) pnlTitleBar.BackColor = isDarkMode ? Color.FromArgb(20, 20, 25) : Color.FromArgb(210, 210, 215);
            if (lblAppTitle != null) lblAppTitle.ForeColor = isDarkMode ? Color.LightGray : Color.Black;

            btnOpen.Text = isEnglish ? "OPEN CONFIG" : "ОТКРЫТЬ КОНФИГ";
            btnNew.Text = isEnglish ? "NEW CONFIG" : "НОВЫЙ КОНФИГ";
            btnSave.Text = isEnglish ? "SAVE & BACKUP" : "СОХРАНИТЬ И БЭКАП";
            btnChecklist.Text = isEnglish ? "CHECKLIST" : "ЧЕКЛИСТ";

            btnOpen.BackColor = btnColor;
            btnNew.BackColor = btnColor;
            btnSave.BackColor = btnColor;
            btnChecklist.BackColor = btnColor;

            btnOpen.ForeColor = textColor;
            btnNew.ForeColor = textColor;
            btnSave.ForeColor = textColor;
            btnChecklist.ForeColor = textColor;

            btnToggleIcons.Text = "👁";
            btnToggleIcons.BackColor = showIcons ? Color.FromArgb(70, 130, 180) : btnColor;
            btnToggleIcons.ForeColor = textColor;
            globalToolTip.SetToolTip(btnToggleIcons, isEnglish ? "Toggle Weapon Icons" : "Включить/выключить иконки оружия");

            btnUnbindAll.Text = "UNBINDALL";
            btnUnbindAll.BackColor = hasUnbindAll ? Color.FromArgb(200, 80, 80) : btnColor;
            btnUnbindAll.ForeColor = hasUnbindAll ? Color.White : textColor;
            globalToolTip.SetToolTip(btnUnbindAll, isEnglish ? "Toggle unbindall (clears standard config binds)" : "Включить/выключить unbindall (сброс стандартных биндов)");

            btnTheme.Text = isDarkMode ? "☀️" : "🌙";
            btnTheme.BackColor = btnColor;
            btnTheme.ForeColor = textColor;
            globalToolTip.SetToolTip(btnTheme, isEnglish ? "Toggle Theme" : "Сменить тему");

            btnLang.Text = isEnglish ? "EN" : "RU";
            btnLang.BackColor = btnColor;
            btnLang.ForeColor = textColor;
            globalToolTip.SetToolTip(btnLang, isEnglish ? "Change Language" : "Сменить язык");

            lblAliases.Text = isEnglish ? "АЛИАСЫ / СКРИПТЫ" : "АЛИАСЫ / СКРИПТЫ";
            lblAliases.ForeColor = textColor;

            btnSnippets.BackColor = Color.FromArgb(70, 130, 180);
            btnSnippets.ForeColor = Color.White;
            globalToolTip.SetToolTip(btnSnippets, isEnglish ? "Insert Code Snippets" : "Вставить скрипты из библиотеки");

            if (pnlAliasBorder != null)
            {
                pnlAliasBorder.BackColor = isDarkMode ? Color.FromArgb(80, 80, 85) : Color.Silver;
                txtAliases.BackColor = btnColor;
                txtAliases.ForeColor = textColor;
                HighlightSyntax();
            }

            DrawInterface();
            this.Refresh();
        }

        private void BtnChecklist_Click(object sender, EventArgs e)
        {
            Dictionary<string, List<string>> cmdToKeys = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> currentAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] aLines = txtAliases.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Regex aliasRegex = new Regex(@"^alias\s+""?([^""\s]+)""?\s+""?([^""]*)""?", RegexOptions.IgnoreCase);
            foreach (var l in aLines)
            {
                Match m = aliasRegex.Match(l.Trim());
                if (m.Success) currentAliases[m.Groups[1].Value] = m.Groups[2].Value;
            }

            void AddToKeys(string rawVal, string keyName)
            {
                var parts = rawVal.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> expandedParts = new List<string>();

                foreach (var p in parts)
                {
                    string cleanP = p.Trim();
                    if (currentAliases.ContainsKey(cleanP))
                        expandedParts.AddRange(currentAliases[cleanP].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                    expandedParts.Add(cleanP);
                }

                foreach (var p in expandedParts)
                {
                    string clean = p.Trim();
                    if (!cmdToKeys.ContainsKey(clean)) cmdToKeys[clean] = new List<string>();
                    if (!cmdToKeys[clean].Contains(keyName)) cmdToKeys[clean].Add(keyName);
                }
            }

            if (!hasUnbindAll)
            {
                foreach (var kvp in defaultBindings)
                {
                    if (!bindings.ContainsKey(kvp.Key) || string.IsNullOrWhiteSpace(bindings[kvp.Key]))
                    {
                        string keyName = displayNames.ContainsKey(kvp.Key) ? displayNames[kvp.Key].Replace("\n", " ") : kvp.Key;
                        AddToKeys(kvp.Value, keyName);
                    }
                }
            }

            foreach (var kvp in bindings)
            {
                if (kvp.Value != "UNBIND" && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    string keyName = displayNames.ContainsKey(kvp.Key) ? displayNames[kvp.Key].Replace("\n", " ") : kvp.Key;
                    AddToKeys(kvp.Value, keyName);
                }
            }

            string[] reqCmds = {
                "+forward", "+back", "+moveleft", "+moveright", "+jump", "+duck",
                "+attack", "+attack2", "+reload", "+use",
                "weapon_crowbar", "weapon_9mmhandgun", "weapon_357", "weapon_9mmAR",
                "weapon_shotgun", "weapon_crossbow", "weapon_rpg", "weapon_gauss",
                "weapon_egon", "weapon_hornetgun", "weapon_satchel", "weapon_tripmine",
                "weapon_handgrenade", "weapon_snark"
            };

            Form chkForm = new Form
            {
                Text = isEnglish ? "Command Checklist" : "Чеклист команд",
                Size = new Size(500, 650),
                BackColor = bgColor,
                ForeColor = textColor,
                ShowIcon = false,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None
            };

            Panel chkTitleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(20, 20, 25) };
            Label chkTitle = new Label { Text = chkForm.Text, ForeColor = Color.LightGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(10, 7) };
            chkTitleBar.Controls.Add(chkTitle);

            Button chkClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            chkClose.FlatAppearance.BorderSize = 0;
            chkClose.Click += (s, ev) => chkForm.Close();
            chkClose.MouseEnter += (s, ev) => { chkClose.BackColor = Color.Crimson; chkClose.ForeColor = Color.White; };
            chkClose.MouseLeave += (s, ev) => { chkClose.BackColor = Color.Transparent; chkClose.ForeColor = Color.Gray; };
            chkTitleBar.Controls.Add(chkClose);

            chkTitleBar.MouseDown += (s, ev) => {
                if (ev.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(chkForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            chkTitle.MouseDown += (s, ev) => {
                if (ev.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(chkForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            Panel pnl = new Panel
            {
                Location = new Point(0, 30),
                Size = new Size(chkForm.Width, chkForm.Height - 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true
            };

            chkForm.Controls.Add(pnl);
            chkForm.Controls.Add(chkTitleBar);

            int curY = 15;
            foreach (string cmd in reqCmds)
            {
                bool hasCmd = cmdToKeys.ContainsKey(cmd);

                Label lblStatus = new Label { Text = hasCmd ? "✔" : "✖", ForeColor = hasCmd ? Color.SpringGreen : Color.Crimson, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, curY), AutoSize = true };
                Label lblCmd = new Label { Text = cmd, ForeColor = isDarkMode ? Color.Gold : Color.DarkGoldenrod, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(60, curY + 2), AutoSize = true };

                string keysStr = hasCmd ? string.Join(", ", cmdToKeys[cmd]) : (isEnglish ? "NOT BOUND" : "НЕ НАЗНАЧЕНО");
                Label lblKeys = new Label { Text = keysStr, ForeColor = hasCmd ? textColor : Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Regular), Location = new Point(250, curY + 2), AutoSize = true };

                pnl.Controls.Add(lblStatus);
                pnl.Controls.Add(lblCmd);
                pnl.Controls.Add(lblKeys);

                Panel line = new Panel { BackColor = isDarkMode ? Color.FromArgb(50, 50, 55) : Color.FromArgb(200, 200, 205), Size = new Size(440, 1), Location = new Point(20, curY + 28) };
                pnl.Controls.Add(line);

                curY += 35;
            }

            chkForm.ShowDialog();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Config files (*.cfg)|*.cfg|All files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = ofd.FileName;
                    isNewConfig = false;
                    File.WriteAllText(GetLastConfigPath(), currentFilePath);
                    ParseConfig(currentFilePath);
                    DrawInterface();
                }
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            string msg = isEnglish ? "Create a new config? Unsaved changes will be lost." : "Создать новый конфиг? Все несохраненные изменения будут потеряны.";
            string title = isEnglish ? "WARNING" : "ВНИМАНИЕ";

            if (MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bindings.Clear();
                txtAliases.Text = "";
                originalFileLines.Clear();
                SetDefaultSettings();
                hasUnbindAll = false;

                currentFilePath = "";
                isNewConfig = true;
                UpdateUIThemeAndLanguage();
            }
        }

        private void SaveSettingsFromUI()
        {
            if (settingsPanel == null) return;
            foreach (Control c in settingsPanel.Controls)
            {
                if (c is TextBox tb && tb.Name.StartsWith("txt_set_"))
                {
                    string key = tb.Name.Substring(8);
                    settingsValues[key] = tb.Text;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUI();

            if (string.IsNullOrEmpty(currentFilePath))
            {
                using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Config files (*.cfg)|*.cfg|All files (*.*)|*.*", DefaultExt = "cfg", AddExtension = true })
                {
                    if (sfd.ShowDialog() == DialogResult.OK) currentFilePath = sfd.FileName;
                    else return;
                }
            }

            if (File.Exists(currentFilePath))
            {
                string backupPath = currentFilePath + ".backup";
                File.Copy(currentFilePath, backupPath, true);
            }

            File.WriteAllText(GetLastConfigPath(), currentFilePath);

            List<string> newLines = new List<string>();
            HashSet<string> handledBinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> handledGenerals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (isNewConfig)
            {
                newLines.Add("// Сделано при помощи Half-Life Config Maker\n");
                if (hasUnbindAll) newLines.Add("unbindall\n");
                newLines.Add("// --- ОСНОВНЫЕ НАСТРОЙКИ ---");
                foreach (var kvp in settingsValues) if (!string.IsNullOrWhiteSpace(kvp.Value)) newLines.Add($"{kvp.Key} \"{kvp.Value}\"");
                newLines.Add("\n// --- БИНДЫ ---");
                foreach (var kvp in bindings)
                {
                    if (kvp.Value == "UNBIND") newLines.Add($"unbind \"{kvp.Key.ToUpper()}\"");
                    else newLines.Add($"bind \"{kvp.Key.ToUpper()}\" \"{kvp.Value}\"");
                }
            }
            else
            {
                Regex bindRegex = new Regex(@"^bind\s+""?([^""\s]+)""?", RegexOptions.IgnoreCase);
                Regex unbindRegex = new Regex(@"^unbind\s+""?([^""\s]+)""?", RegexOptions.IgnoreCase);
                Regex aliasRegex = new Regex(@"^alias\s+""?([^""\s]+)""?", RegexOptions.IgnoreCase);

                bool unbindallAdded = false;

                foreach (string rawLine in originalFileLines)
                {
                    string line = rawLine.Trim();

                    if (line.Equals("unbindall", StringComparison.OrdinalIgnoreCase))
                    {
                        if (hasUnbindAll && !unbindallAdded)
                        {
                            newLines.Add("unbindall");
                            unbindallAdded = true;
                        }
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        if (line.Contains("Half-Life Config Maker") && hasUnbindAll && !unbindallAdded)
                        {
                            newLines.Add(rawLine);
                            newLines.Add("unbindall");
                            unbindallAdded = true;
                            continue;
                        }
                        newLines.Add(rawLine);
                        continue;
                    }

                    if (aliasRegex.IsMatch(line)) continue;

                    Match bindMatch = bindRegex.Match(line);
                    if (bindMatch.Success)
                    {
                        string key = bindMatch.Groups[1].Value.ToUpper();
                        if (bindings.ContainsKey(key))
                        {
                            if (bindings[key] == "UNBIND") newLines.Add($"unbind \"{key}\"");
                            else newLines.Add($"bind \"{key}\" \"{bindings[key]}\"");
                            handledBinds.Add(key);
                        }
                        continue;
                    }

                    Match unbindMatch = unbindRegex.Match(line);
                    if (unbindMatch.Success)
                    {
                        string key = unbindMatch.Groups[1].Value.ToUpper();
                        if (bindings.ContainsKey(key))
                        {
                            if (bindings[key] == "UNBIND") newLines.Add($"unbind \"{key}\"");
                            else newLines.Add($"bind \"{key}\" \"{bindings[key]}\"");
                            handledBinds.Add(key);
                        }
                        continue;
                    }

                    bool isGeneral = false;
                    foreach (var cat in settingsCategories.Values)
                    {
                        foreach (var gKey in cat)
                        {
                            if (line.StartsWith(gKey, StringComparison.OrdinalIgnoreCase))
                            {
                                if (settingsValues.ContainsKey(gKey) && !string.IsNullOrWhiteSpace(settingsValues[gKey]))
                                {
                                    newLines.Add($"{gKey} \"{settingsValues[gKey]}\"");
                                    handledGenerals.Add(gKey);
                                }
                                isGeneral = true;
                                break;
                            }
                        }
                        if (isGeneral) break;
                    }
                    if (isGeneral) continue;

                    newLines.Add(rawLine);
                }

                if (hasUnbindAll && !unbindallAdded)
                {
                    newLines.Insert(0, "unbindall");
                }

                bool addedHeader = false;
                foreach (var kvp in settingsValues)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value) && !handledGenerals.Contains(kvp.Key))
                    {
                        if (!addedHeader) { newLines.Add("\n// --- НОВЫЕ ЗАПИСИ (CONMAKER) ---"); addedHeader = true; }
                        newLines.Add($"{kvp.Key} \"{kvp.Value}\"");
                    }
                }
                foreach (var kvp in bindings)
                {
                    if (!handledBinds.Contains(kvp.Key))
                    {
                        if (!addedHeader) { newLines.Add("\n// --- НОВЫЕ ЗАПИСИ (CONMAKER) ---"); addedHeader = true; }
                        if (kvp.Value == "UNBIND") newLines.Add($"unbind \"{kvp.Key.ToUpper()}\"");
                        else newLines.Add($"bind \"{kvp.Key.ToUpper()}\" \"{kvp.Value}\"");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(txtAliases.Text))
            {
                newLines.Add("\n// --- АЛИАСЫ ---");
                string[] aliasLines = txtAliases.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string al in aliasLines)
                {
                    if (al.Trim().StartsWith("alias", StringComparison.OrdinalIgnoreCase))
                        newLines.Add(al.Trim());
                    else
                        newLines.Add("alias " + al.Trim());
                }
            }

            File.WriteAllLines(currentFilePath, newLines);
            originalFileLines = new List<string>(newLines);

            string msg = isEnglish ? "Config saved successfully!" : "Конфиг успешно сохранен, бэкап создан!";
            MessageBox.Show(msg, isEnglish ? "SUCCESS" : "УСПЕХ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            isNewConfig = false;
        }

        private void ParseConfig(string path)
        {
            bindings.Clear();
            txtAliases.Text = "";
            originalFileLines.Clear();
            hasUnbindAll = false;

            foreach (var cat in settingsCategories.Values)
                foreach (var key in cat) settingsValues[key] = "";

            if (!File.Exists(path)) return;

            originalFileLines = new List<string>(File.ReadAllLines(path));

            Regex bindRegex = new Regex(@"^bind\s+""?([^""\s]+)""?\s+""?([^""]*)""?", RegexOptions.IgnoreCase);
            Regex unbindRegex = new Regex(@"^unbind\s+""?([^""\s]+)""?", RegexOptions.IgnoreCase);
            Regex aliasRegex = new Regex(@"^alias\s+""?([^""\s]+)""?\s+""?([^""]*)""?", RegexOptions.IgnoreCase);

            List<string> parsedAliases = new List<string>();

            foreach (string rawLine in originalFileLines)
            {
                string line = rawLine.Trim();

                if (line.Equals("unbindall", StringComparison.OrdinalIgnoreCase) || line.StartsWith("unbindall;", StringComparison.OrdinalIgnoreCase))
                {
                    bindings.Clear();
                    hasUnbindAll = true;
                    continue;
                }

                int commentIdx = line.IndexOf("//");
                if (commentIdx >= 0) line = line.Substring(0, commentIdx).Trim();

                if (string.IsNullOrWhiteSpace(line)) continue;

                Match bindMatch = bindRegex.Match(line);
                if (bindMatch.Success)
                {
                    bindings[bindMatch.Groups[1].Value.ToUpper()] = bindMatch.Groups[2].Value;
                    continue;
                }

                Match unbindMatch = unbindRegex.Match(line);
                if (unbindMatch.Success)
                {
                    bindings[unbindMatch.Groups[1].Value.ToUpper()] = "UNBIND";
                    continue;
                }

                Match aliasMatch = aliasRegex.Match(line);
                if (aliasMatch.Success)
                {
                    parsedAliases.Add(rawLine.Trim());
                    continue;
                }

                foreach (var cat in settingsCategories.Values)
                {
                    foreach (var sKey in cat)
                    {
                        if (line.StartsWith(sKey, StringComparison.OrdinalIgnoreCase))
                        {
                            string value = line.Substring(sKey.Length).Trim(new char[] { ' ', '\t', '"' });
                            settingsValues[sKey] = value;
                            break;
                        }
                    }
                }
            }

            txtAliases.Text = string.Join(Environment.NewLine, parsedAliases);
            UpdateUIThemeAndLanguage();
        }

        private void DrawInterface()
        {
            if (keyboardPanel == null || keyboardPanel.Width == 0 || keyboardPanel.Height == 0) return;

            keyboardPanel.SuspendLayout();

            string[][] mainKeys = {
                new string[] { "ESC", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" },
                new string[] { "~", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "BACKSPACE" },
                new string[] { "TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" },
                new string[] { "CAPSLOCK", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "ENTER" },
                new string[] { "SHIFT", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "SHIFT" },
                new string[] { "CTRL", "WIN", "ALT", "SPACE", "ALT", "FN", "MENU", "CTRL" }
            };

            string[][] navKeys = {
                new string[] { "INS", "HOME", "PGUP" },
                new string[] { "DEL", "END", "PGDN" },
                new string[] { "SKIP", "SKIP", "SKIP" },
                new string[] { "SKIP", "UPARROW", "SKIP" },
                new string[] { "LEFTARROW", "DOWNARROW", "RIGHTARROW" }
            };

            string[][] numKeys = {
                new string[] { "NUMLOCK", "KP_SLASH", "*", "KP_MINUS" },
                new string[] { "KP_HOME", "KP_UPARROW", "KP_PGUP", "KP_PLUS" },
                new string[] { "KP_LEFTARROW", "KP_5", "KP_RIGHTARROW", "SKIP" },
                new string[] { "KP_END", "KP_DOWNARROW", "KP_PGDN", "KP_ENTER" },
                new string[] { "KP_INS", "SKIP", "KP_DEL", "SKIP" }
            };

            float scaleX = keyboardPanel.Width / 1800f;
            float scaleY = keyboardPanel.Height / 700f;

            int startX = (int)(10 * scaleX);
            int startY = 0;
            int standardBtnWidth = (int)(75 * scaleX);
            int btnHeight = (int)(60 * scaleY);
            int padding = (int)(5 * Math.Min(scaleX, scaleY));
            if (padding < 1) padding = 1;

            float fontSize = 8f * Math.Min(scaleX, scaleY);
            if (fontSize < 5f) fontSize = 5f;

            int targetRightEdge = (int)(1180 * scaleX);
            int horizontalGap = (int)(30 * scaleX);

            for (int row = 0; row < mainKeys.Length; row++)
            {
                int currentX = startX;
                for (int col = 0; col < mainKeys[row].Length; col++)
                {
                    string key = mainKeys[row][col];
                    string command = bindings.ContainsKey(key) ? bindings[key] : (hasUnbindAll ? "" : (defaultBindings.ContainsKey(key) ? defaultBindings[key] : ""));

                    string btnName = $"btn_{key}_{row}_{col}";
                    Button btn;

                    Control[] found = keyboardPanel.Controls.Find(btnName, false);
                    if (found.Length > 0)
                    {
                        btn = (Button)found[0];
                    }
                    else
                    {
                        btn = new Button { Name = btnName, FlatStyle = FlatStyle.Flat };
                        btn.FlatAppearance.BorderSize = 0;
                        btn.Click += (s, e) => EditBind(key, btn);
                        AttachHover(btn);
                        keyboardPanel.Controls.Add(btn);
                    }

                    btn.BackColor = btnColor;
                    string dName = displayNames.ContainsKey(key) ? displayNames[key] : key;
                    UpdateButtonStyle(btn, command, dName, fontSize);

                    int btnWidth = standardBtnWidth;
                    if (key == "TAB") btnWidth = (int)(112 * scaleX);
                    else if (key == "CAPSLOCK") btnWidth = (int)(131 * scaleX);
                    else if (key == "SHIFT" && col == 0) btnWidth = (int)(188 * scaleX);
                    else if (key == "SPACE") btnWidth = (int)(450 * scaleX);
                    else if (key == "CTRL" && col == 0) btnWidth = (int)(95 * scaleX);
                    else if (key == "WIN") btnWidth = (int)(75 * scaleX);
                    else if (key == "ALT" && col == 2) btnWidth = (int)(95 * scaleX);

                    if (col == mainKeys[row].Length - 1)
                    {
                        btnWidth = targetRightEdge - currentX;
                        if (btnWidth < standardBtnWidth) btnWidth = standardBtnWidth;
                    }

                    btn.Size = new Size(btnWidth, btnHeight);
                    btn.Location = new Point(currentX, startY + (row * (btnHeight + padding)));
                    currentX += btnWidth + padding;
                }
            }

            int navX = targetRightEdge + horizontalGap;
            int extraBlocksStartY = startY + btnHeight + padding;

            int curY = extraBlocksStartY;
            for (int r = 0; r < navKeys.Length; r++)
            {
                int curX = navX;
                for (int c = 0; c < navKeys[r].Length; c++)
                {
                    string key = navKeys[r][c];
                    if (key == "SKIP") { curX += standardBtnWidth + padding; continue; }

                    string command = bindings.ContainsKey(key) ? bindings[key] : (hasUnbindAll ? "" : (defaultBindings.ContainsKey(key) ? defaultBindings[key] : ""));
                    string btnName = $"btn_nav_{key}";
                    Button btn;

                    Control[] found = keyboardPanel.Controls.Find(btnName, false);
                    if (found.Length > 0)
                    {
                        btn = (Button)found[0];
                    }
                    else
                    {
                        btn = new Button { Name = btnName, FlatStyle = FlatStyle.Flat };
                        btn.FlatAppearance.BorderSize = 0;
                        btn.Click += (s, e) => EditBind(key, btn);
                        AttachHover(btn);
                        keyboardPanel.Controls.Add(btn);
                    }

                    btn.BackColor = btnColor;
                    string dName = displayNames.ContainsKey(key) ? displayNames[key] : key;
                    UpdateButtonStyle(btn, command, dName, fontSize);

                    btn.Size = new Size(standardBtnWidth, btnHeight);
                    btn.Location = new Point(curX, curY);
                    curX += standardBtnWidth + padding;
                }
                curY += btnHeight + padding;
            }

            int numX = navX + (3 * standardBtnWidth + 2 * padding) + horizontalGap;
            curY = extraBlocksStartY;

            for (int r = 0; r < numKeys.Length; r++)
            {
                int curX = numX;
                for (int c = 0; c < numKeys[r].Length; c++)
                {
                    string key = numKeys[r][c];
                    if (key == "SKIP") { curX += standardBtnWidth + padding; continue; }

                    string command = bindings.ContainsKey(key) ? bindings[key] : (hasUnbindAll ? "" : (defaultBindings.ContainsKey(key) ? defaultBindings[key] : ""));
                    string btnName = $"btn_num_{r}_{c}";
                    Button btn;

                    Control[] found = keyboardPanel.Controls.Find(btnName, false);
                    if (found.Length > 0)
                    {
                        btn = (Button)found[0];
                    }
                    else
                    {
                        btn = new Button { Name = btnName, FlatStyle = FlatStyle.Flat };
                        btn.FlatAppearance.BorderSize = 0;
                        btn.Click += (s, e) => EditBind(key, btn);
                        AttachHover(btn);
                        keyboardPanel.Controls.Add(btn);
                    }

                    btn.BackColor = btnColor;
                    int bw = standardBtnWidth;
                    int bh = btnHeight;

                    if (key == "KP_PLUS" || key == "KP_ENTER") bh = btnHeight * 2 + padding;
                    if (key == "KP_INS") bw = standardBtnWidth * 2 + padding;

                    string dName = displayNames.ContainsKey(key) ? displayNames[key] : key;
                    UpdateButtonStyle(btn, command, dName, fontSize);

                    btn.Size = new Size(bw, bh);
                    btn.Location = new Point(curX, curY);
                    curX += bw + padding;
                }
                curY += btnHeight + padding;
            }

            int numpadTotalWidth = (4 * standardBtnWidth) + (3 * padding);
            int numpadRightEdge = numX + numpadTotalWidth;

            if (btnOpen != null)
            {
                int leftAlignX = keyboardPanel.Left + startX;

                btnOpen.Left = leftAlignX;
                btnNew.Left = btnOpen.Right + 10;
                btnSave.Left = btnNew.Right + 10;
                btnChecklist.Left = btnSave.Right + 10;
                btnUnbindAll.Left = btnChecklist.Right + 10;
            }

            if (btnLang != null && btnTheme != null && btnToggleIcons != null && btnOpen != null)
            {
                int topMenuY = 45;
                int targetSize = 45;

                btnOpen.Height = targetSize;
                btnNew.Height = targetSize;
                btnSave.Height = targetSize;
                btnChecklist.Height = targetSize;
                btnUnbindAll.Height = targetSize;

                btnLang.Size = new Size(targetSize, targetSize);
                btnTheme.Size = new Size(targetSize, targetSize);
                btnToggleIcons.Size = new Size(targetSize, targetSize);

                btnToggleIcons.Paint -= (s, e) => DrawTopAccent(s, e, Color.Transparent);
                btnToggleIcons.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(70, 130, 180));

                btnLang.Paint -= (s, e) => DrawTopAccent(s, e, Color.Transparent);
                btnLang.Paint += (s, e) => DrawTopAccent(s, e, Color.FromArgb(147, 112, 219));

                btnTheme.Paint -= (s, e) => DrawTopAccent(s, e, Color.Transparent);
                btnTheme.Paint += (s, e) => DrawTopAccent(s, e, isDarkMode ? Color.FromArgb(240, 240, 245) : Color.FromArgb(30, 30, 35));

                int rightAlignX = keyboardPanel.Left + numpadRightEdge;

                btnLang.Location = new Point(rightAlignX - btnLang.Width, topMenuY);
                btnTheme.Location = new Point(btnLang.Left - btnTheme.Width - 10, topMenuY);
                btnToggleIcons.Location = new Point(btnTheme.Left - btnToggleIcons.Width - 10, topMenuY);
            }

            int bottomOfKeyboard = extraBlocksStartY + 5 * (btnHeight + padding);
            int lowerSectionY = bottomOfKeyboard + horizontalGap;

            int mX = numpadRightEdge - (int)(330 * scaleX);
            int mY = lowerSectionY + (int)(40 * scaleY);

            Label mouseLabel;
            Control[] foundMouse = keyboardPanel.Controls.Find("lblMouse", false);
            if (foundMouse.Length > 0)
            {
                mouseLabel = (Label)foundMouse[0];
            }
            else
            {
                mouseLabel = new Label { Name = "lblMouse", AutoSize = true };
                keyboardPanel.Controls.Add(mouseLabel);
            }

            mouseLabel.ForeColor = textColor;
            mouseLabel.Text = isEnglish ? "MOUSE" : "МЫШЬ";
            mouseLabel.Font = new Font("Segoe UI", fontSize + 2, FontStyle.Bold);

            int wheelCenterX = mX + (int)(128 * scaleX) + (int)(74 * scaleX) / 2;
            mouseLabel.Location = new Point(wheelCenterX - (isEnglish ? (int)(32 * scaleX) : (int)(35 * scaleX)), lowerSectionY + (int)(5 * scaleY));

            var mouseLayout = new Dictionary<string, Tuple<Rectangle, string>>
            {
                { "MOUSE1", new Tuple<Rectangle, string>(new Rectangle(mX, mY, (int)(120 * scaleX), (int)(220 * scaleY)), "LMB") },
                { "MOUSE2", new Tuple<Rectangle, string>(new Rectangle(mX + (int)(210 * scaleX), mY, (int)(120 * scaleX), (int)(220 * scaleY)), "RMB") },
                { "MWHEELUP", new Tuple<Rectangle, string>(new Rectangle(mX + (int)(128 * scaleX), mY, (int)(74 * scaleX), (int)(65 * scaleY)), "MWU") },
                { "MOUSE3", new Tuple<Rectangle, string>(new Rectangle(mX + (int)(128 * scaleX), mY + (int)(75 * scaleY), (int)(74 * scaleX), (int)(70 * scaleY)), "MMB") },
                { "MWHEELDOWN", new Tuple<Rectangle, string>(new Rectangle(mX + (int)(128 * scaleX), mY + (int)(155 * scaleY), (int)(74 * scaleX), (int)(65 * scaleY)), "MWD") },
                { "MOUSE5", new Tuple<Rectangle, string>(new Rectangle(mX - (int)(85 * scaleX), mY + (int)(35 * scaleY), (int)(75 * scaleX), (int)(65 * scaleY)), "M5") },
                { "MOUSE4", new Tuple<Rectangle, string>(new Rectangle(mX - (int)(85 * scaleX), mY + (int)(120 * scaleY), (int)(75 * scaleX), (int)(65 * scaleY)), "M4") }
            };

            foreach (var mouseBtn in mouseLayout)
            {
                string key = mouseBtn.Key;
                Rectangle rect = mouseBtn.Value.Item1;
                string displayName = mouseBtn.Value.Item2;
                string command = bindings.ContainsKey(key) ? bindings[key] : (hasUnbindAll ? "" : (defaultBindings.ContainsKey(key) ? defaultBindings[key] : ""));
                string btnName = $"btn_m_{key}";
                Button btn;

                Control[] foundBtn = keyboardPanel.Controls.Find(btnName, false);
                if (foundBtn.Length > 0)
                {
                    btn = (Button)foundBtn[0];
                }
                else
                {
                    btn = new Button { Name = btnName, FlatStyle = FlatStyle.Flat };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Click += (s, e) => EditBind(key, btn);
                    AttachHover(btn);
                    keyboardPanel.Controls.Add(btn);
                }

                btn.BackColor = btnColor;
                UpdateButtonStyle(btn, command, displayName, fontSize);
                btn.Size = rect.Size;
                btn.Location = rect.Location;
            }

            int totalTabsWidth = (int)(550 * scaleX); // Расширили зону настроек с 480 до 550
            int gapSettingsMouse = (int)(25 * scaleX);
            int leftmostMouseX = mX - (int)(85 * scaleX);
            int genX = leftmostMouseX - gapSettingsMouse - totalTabsWidth;

            int tabWidth = totalTabsWidth / 5;

            float tabFontSize = fontSize - 1f;
            if (tabFontSize < 4.5f) tabFontSize = 4.5f;

            int tabHeight = (int)(40 * scaleY);
            int settingsY = lowerSectionY + tabHeight + (int)(15 * scaleY);

            string[] tabs = { "ОСНОВНОЕ", "СЕТЬ", "ЗВУК", "ВИДЕО", "ПРИЦЕЛ" };
            for (int i = 0; i < tabs.Length; i++)
            {
                string tName = tabs[i];
                Button btnTab;
                Control[] foundTab = keyboardPanel.Controls.Find($"tab_{tName}", false);
                if (foundTab.Length > 0)
                {
                    btnTab = (Button)foundTab[0];
                }
                else
                {
                    btnTab = new Button { Name = $"tab_{tName}", FlatStyle = FlatStyle.Flat };
                    btnTab.FlatAppearance.BorderSize = 1;
                    btnTab.Click += (s, e) => { SaveSettingsFromUI(); currentTab = tName; DrawInterface(); };

                    Color hoverDark = Color.FromArgb(65, 65, 70);
                    Color hoverLight = Color.FromArgb(200, 200, 210);
                    btnTab.MouseEnter += (s, e) => { if (currentTab != tName) btnTab.BackColor = isDarkMode ? hoverDark : hoverLight; };
                    btnTab.MouseLeave += (s, e) => { if (currentTab != tName) btnTab.BackColor = btnColor; };
                    keyboardPanel.Controls.Add(btnTab);
                }

                string displayText = isEnglish && tName == "ОСНОВНОЕ" ? "MAIN" : (isEnglish && tName == "СЕТЬ" ? "NET" : (isEnglish && tName == "ЗВУК" ? "AUDIO" : (isEnglish && tName == "ВИДЕО" ? "VIDEO" : (isEnglish && tName == "ПРИЦЕЛ" ? "CROSSHAIR" : tName))));
                btnTab.Text = displayText;
                btnTab.Font = new Font("Segoe UI", tabFontSize, FontStyle.Bold);
                btnTab.ForeColor = isDarkMode ? Color.White : Color.Black;
                btnTab.UseVisualStyleBackColor = false;
                btnTab.FlatAppearance.BorderColor = bgColor;

                btnTab.Size = new Size(tabWidth, tabHeight);
                btnTab.Location = new Point(genX + (i * tabWidth), lowerSectionY);
                btnTab.BackColor = currentTab == tName ? Color.FromArgb(70, 130, 180) : btnColor;
            }

            int availableHeight = keyboardPanel.Height - settingsY - (int)(10 * scaleY);

            if (settingsPanel == null)
            {
                settingsPanel = new DBPanel { Name = "settingsPanel", BackColor = Color.Transparent };
                keyboardPanel.Controls.Add(settingsPanel);

                pnlCrosshairPreview = new DBPanel { Name = "pnlCrosshairPreview", BackColor = Color.FromArgb(15, 15, 18) };
                pnlCrosshairPreview.Paint += DrawCrosshairPreview;
                keyboardPanel.Controls.Add(pnlCrosshairPreview);
            }

            settingsPanel.Size = new Size(totalTabsWidth, availableHeight);
            settingsPanel.Location = new Point(genX, settingsY);

            PopulateSettingsTab(fontSize, scaleX, scaleY);

            int aliasTop = mY;
            int aliasW = genX - startX - horizontalGap;
            lblAliases.Location = new Point(startX, aliasTop - lblAliases.Height - 5);
            if (btnSnippets != null) btnSnippets.Location = new Point(lblAliases.Right + 10, lblAliases.Top - 2);

            if (pnlAliasBorder != null)
            {
                pnlAliasBorder.Size = new Size(aliasW, keyboardPanel.Height - aliasTop - (int)(10 * scaleY));
                pnlAliasBorder.Location = new Point(startX, aliasTop);
            }

            keyboardPanel.ResumeLayout(true);
        }

        private void PopulateSettingsTab(float fontSize, float scaleX, float scaleY)
        {
            settingsPanel.SuspendLayout();

            settingsPanel.AutoScroll = false;
            settingsPanel.AutoScrollPosition = new Point(0, 0);

            foreach (Control c in settingsPanel.Controls)
            {
                c.Visible = false;
            }

            string[] currentKeys = settingsCategories[currentTab];
            int panelInnerWidth = settingsPanel.Width - 25;

            bool isCrosshairTab = (currentTab == "ПРИЦЕЛ");
            int chSize = (int)(120 * scaleY);

            int columns = isCrosshairTab ? 1 : 2;
            int colWidth = isCrosshairTab ? (panelInnerWidth - chSize - 40) : (panelInnerWidth / 2);

            if (isCrosshairTab)
            {
                if (pnlCrosshairPreview == null)
                {
                    pnlCrosshairPreview = new DBPanel { Name = "pnlCrosshairPreview", BackColor = Color.FromArgb(15, 15, 18) };
                    pnlCrosshairPreview.Paint += DrawCrosshairPreview;
                    keyboardPanel.Controls.Add(pnlCrosshairPreview);
                }
                pnlCrosshairPreview.Visible = true;
                pnlCrosshairPreview.Size = new Size(chSize, chSize);
                pnlCrosshairPreview.Location = new Point(settingsPanel.Right - chSize - 35, settingsPanel.Top);
                pnlCrosshairPreview.BringToFront();
                pnlCrosshairPreview.Invalidate();
            }
            else
            {
                if (pnlCrosshairPreview != null) pnlCrosshairPreview.Visible = false;
            }

            int drawnCount = 0;

            for (int i = 0; i < currentKeys.Length; i++)
            {
                string gKey = currentKeys[i];

                int row = drawnCount / columns;
                int col = drawnCount % columns;

                int yOffset = row * (int)(45 * scaleY);
                int xOffset = col * colWidth;

                Control[] foundLbl = settingsPanel.Controls.Find($"lbl_set_{gKey}", false);
                Label lbl = foundLbl.Length > 0 ? (Label)foundLbl[0] : new Label { Name = $"lbl_set_{gKey}", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

                lbl.ForeColor = isDarkMode ? Color.White : Color.Black;
                lbl.Text = gKey;
                lbl.Location = new Point(xOffset, yOffset + (int)(5 * scaleY));
                lbl.Visible = true;
                if (!settingsPanel.Controls.Contains(lbl)) settingsPanel.Controls.Add(lbl);

                Control[] foundTxt = settingsPanel.Controls.Find($"txt_set_{gKey}", false);
                TextBox txt = foundTxt.Length > 0 ? (TextBox)foundTxt[0] : new TextBox { Name = $"txt_set_{gKey}", BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10, FontStyle.Regular) };

                txt.BackColor = btnColor;
                txt.ForeColor = isDarkMode ? Color.White : Color.Black;

                string txtVal = settingsValues.ContainsKey(gKey) && !string.IsNullOrWhiteSpace(settingsValues[gKey]) ? settingsValues[gKey] : "";
                if (string.IsNullOrWhiteSpace(txtVal))
                {
                    if (gKey.Equals("rate", StringComparison.OrdinalIgnoreCase)) txtVal = "250000";
                    if (gKey.Equals("ex_interp", StringComparison.OrdinalIgnoreCase)) txtVal = "0.01";
                }
                txt.Text = txtVal;

                txt.Size = new Size((int)(60 * scaleX), (int)(25 * scaleY));
                txt.Location = new Point(xOffset + (int)(170 * scaleX), yOffset + 2); // Сдвинули поле ввода правее со 130 до 170
                txt.Visible = true;

                if (currentTab == "ПРИЦЕЛ" && gKey.StartsWith("cl_cross"))
                {
                    txt.TextChanged -= TxtCrosshair_TextChanged;
                    txt.TextChanged += TxtCrosshair_TextChanged;
                }

                if (!settingsPanel.Controls.Contains(txt)) settingsPanel.Controls.Add(txt);

                Control[] foundUnd = settingsPanel.Controls.Find($"und_set_{gKey}", false);
                Panel underline = foundUnd.Length > 0 ? (Panel)foundUnd[0] : new Panel { Name = $"und_set_{gKey}" };

                underline.BackColor = isDarkMode ? Color.FromArgb(80, 80, 85) : Color.Silver;
                underline.Size = new Size(txt.Width, 2);
                underline.Location = new Point(txt.Left, txt.Bottom + 2);
                underline.Visible = true;

                if (underline.Tag == null)
                {
                    txt.Enter += (s, e) => { underline.BackColor = Color.FromArgb(70, 130, 180); };
                    txt.Leave += (s, e) => { underline.BackColor = isDarkMode ? Color.FromArgb(80, 80, 85) : Color.Silver; };
                    underline.Tag = true;
                }
                if (!settingsPanel.Controls.Contains(underline)) settingsPanel.Controls.Add(underline);

                // Изменили шрифт с 11 до 9
                Control[] foundBtnRes = settingsPanel.Controls.Find($"btn_res_{gKey}", false);
                Button btnRes = foundBtnRes.Length > 0 ? (Button)foundBtnRes[0] : new Button { Name = $"btn_res_{gKey}", FlatStyle = FlatStyle.Flat, Text = "↻", Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };

                btnRes.FlatAppearance.BorderSize = 0;
                btnRes.BackColor = Color.Transparent;
                btnRes.ForeColor = isDarkMode ? Color.Gray : Color.DarkGray;
                btnRes.Size = new Size((int)(24 * scaleX), txt.Height + 4);
                btnRes.Location = new Point(txt.Right + 2, txt.Top - 2);
                btnRes.Padding = new Padding(0);
                btnRes.TextAlign = ContentAlignment.MiddleCenter;
                btnRes.Visible = true;

                if (btnRes.Tag == null)
                {
                    btnRes.MouseEnter += (s, e) => { btnRes.ForeColor = isDarkMode ? Color.White : Color.Black; };
                    btnRes.MouseLeave += (s, e) => { btnRes.ForeColor = isDarkMode ? Color.Gray : Color.DarkGray; };
                    btnRes.Click += (s, e) => {
                        if (gKey.Equals("rate", StringComparison.OrdinalIgnoreCase)) txt.Text = "250000";
                        else if (gKey.Equals("ex_interp", StringComparison.OrdinalIgnoreCase)) txt.Text = "0.01";
                        else if (gKey.Equals("cl_cross_size", StringComparison.OrdinalIgnoreCase)) txt.Text = "5";
                        else if (gKey.Equals("cl_cross_color", StringComparison.OrdinalIgnoreCase)) txt.Text = "0 255 0";
                        else if (gKey.Equals("cl_cross_thickness", StringComparison.OrdinalIgnoreCase)) txt.Text = "2";
                        else if (gKey.Equals("cl_cross_gap", StringComparison.OrdinalIgnoreCase)) txt.Text = "3";
                        else txt.Text = "";

                        SaveSettingsFromUI();
                        if (currentTab == "ПРИЦЕЛ") pnlCrosshairPreview?.Invalidate();
                    };
                    btnRes.Tag = true;
                }

                if (!settingsPanel.Controls.Contains(btnRes)) settingsPanel.Controls.Add(btnRes);

                drawnCount++;
            }

            settingsPanel.ResumeLayout(true);

            settingsPanel.AutoScroll = true;
            settingsPanel.PerformLayout();
        }

        private void TxtCrosshair_TextChanged(object sender, EventArgs e)
        {
            SaveSettingsFromUI();
            if (pnlCrosshairPreview != null && pnlCrosshairPreview.Visible)
                pnlCrosshairPreview.Invalidate();
        }

        private void DrawCrosshairPreview(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            string colorStr = settingsValues.ContainsKey("cl_cross_color") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_color"]) ? settingsValues["cl_cross_color"] : "0 255 0";
            string[] rgb = colorStr.Split(' ');
            int r = 0, g = 255, b = 0;
            if (rgb.Length == 3) { int.TryParse(rgb[0], out r); int.TryParse(rgb[1], out g); int.TryParse(rgb[2], out b); }

            int alpha = 255;
            if (settingsValues.ContainsKey("cl_cross_alpha") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_alpha"])) int.TryParse(settingsValues["cl_cross_alpha"], out alpha);

            Color crossColor = Color.FromArgb(Math.Min(255, Math.Max(0, alpha)), Math.Min(255, Math.Max(0, r)), Math.Min(255, Math.Max(0, g)), Math.Min(255, Math.Max(0, b)));

            int size = 5; if (settingsValues.ContainsKey("cl_cross_size") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_size"])) int.TryParse(settingsValues["cl_cross_size"], out size);
            int gap = 3; if (settingsValues.ContainsKey("cl_cross_gap") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_gap"])) int.TryParse(settingsValues["cl_cross_gap"], out gap);
            int thick = 2; if (settingsValues.ContainsKey("cl_cross_thickness") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_thickness"])) int.TryParse(settingsValues["cl_cross_thickness"], out thick);
            int dotSize = 0; if (settingsValues.ContainsKey("cl_cross_dot_size") && !string.IsNullOrWhiteSpace(settingsValues["cl_cross_dot_size"])) int.TryParse(settingsValues["cl_cross_dot_size"], out dotSize);

            int cx = pnl.Width / 2;
            int cy = pnl.Height / 2;

            Pen pen = new Pen(crossColor, thick);

            float halfThick = thick / 2f;

            e.Graphics.DrawLine(pen, cx - gap - size, cy, cx - gap, cy);
            e.Graphics.DrawLine(pen, cx + gap, cy, cx + gap + size, cy);
            e.Graphics.DrawLine(pen, cx, cy - gap - size, cx, cy - gap);
            e.Graphics.DrawLine(pen, cx, cy + gap, cx, cy + gap + size);

            if (dotSize > 0)
            {
                e.Graphics.FillRectangle(new SolidBrush(crossColor), cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize);
            }
        }

        private void UpdateButtonStyle(Button btn, string command, string displayName, float fontSize)
        {
            btn.Text = "";
            btn.Tag = new Tuple<string, string, float>(displayName, command, fontSize);

            btn.Paint -= CustomButton_Paint;
            btn.Paint += CustomButton_Paint;

            btn.Invalidate();
        }

        private void CustomButton_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is Tuple<string, string, float> data)) return;

            string displayName = data.Item1;
            string command = data.Item2;
            float fontSize = data.Item3;

            Font keyFont = new Font("Segoe UI", fontSize, FontStyle.Bold);

            SizeF keySize = e.Graphics.MeasureString(displayName, keyFont);
            e.Graphics.DrawString(displayName, keyFont, new SolidBrush(idleTextColor), (btn.Width - keySize.Width) / 2, 5);

            if (string.IsNullOrEmpty(command)) return;

            if (command == "UNBIND")
            {
                SizeF uSize = e.Graphics.MeasureString("UNBIND", keyFont);
                e.Graphics.DrawString("UNBIND", keyFont, Brushes.Crimson, (btn.Width - uSize.Width) / 2, 5 + keySize.Height + 2);
                return;
            }

            string[] parts = command.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            float currentY = 5 + keySize.Height + 2;

            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (string.IsNullOrEmpty(part)) continue;

                Color c = textColor;
                string displayPart = part;

                Font drawFont = keyFont;

                if (showIcons && iconMap.ContainsKey(part) && iconFont != null)
                {
                    displayPart = iconMap[part];
                    drawFont = iconFont;
                    c = isDarkMode ? Color.Gold : Color.DarkGoldenrod;
                }
                else
                {
                    if (part.StartsWith("weapon_") || part.StartsWith("slot") || part.StartsWith("impulse")) { c = isDarkMode ? Color.Gold : Color.DarkGoldenrod; displayPart = part.StartsWith("weapon_") ? part.Substring(7) : part; }
                    else if (part.StartsWith("say") || part.StartsWith("say_team") || part.StartsWith("say_close") || part.StartsWith("play_close")) c = isDarkMode ? Color.DeepSkyBlue : Color.MediumBlue;
                    else if (part.Contains("agstart") || part.Contains("agpause") || part.Contains("spectate") || part.Contains("retry") || part.Contains("cancelselect") || part.Contains("escape")) c = isDarkMode ? Color.DarkOrange : Color.Crimson;
                    else if (part.StartsWith("+")) c = isDarkMode ? Color.SpringGreen : Color.ForestGreen;

                    displayPart = Regex.Replace(displayPart, @"\^[0-9]", "");
                }

                using (Brush b = new SolidBrush(c))
                {
                    SizeF pSize = e.Graphics.MeasureString(displayPart, drawFont);

                    float availableWidth = btn.Width - 6;
                    if (pSize.Width > availableWidth && availableWidth > 0)
                    {
                        float scaleRatio = availableWidth / pSize.Width;
                        float newSize = drawFont.Size * scaleRatio;
                        if (newSize < 5f) newSize = 5f;

                        using (Font scaledFont = new Font(drawFont.FontFamily, newSize, drawFont.Style))
                        {
                            pSize = e.Graphics.MeasureString(displayPart, scaledFont);
                            e.Graphics.DrawString(displayPart, scaledFont, b, (btn.Width - pSize.Width) / 2, currentY);
                        }
                    }
                    else
                    {
                        e.Graphics.DrawString(displayPart, drawFont, b, (btn.Width - pSize.Width) / 2, currentY);
                    }

                    currentY += pSize.Height;
                }

                if (currentY > btn.Height - 12) break;
            }
        }

        private void EditBind(string key, Button btn)
        {
            string currentCommand = bindings.ContainsKey(key) ? bindings[key] : "";

            string titleText = isEnglish ? $"EDIT BIND: {key}" : $"ИЗМЕНИТЬ БИНД: {key}";
            Form prompt = new Form() { Width = 420, Height = 180, FormBorderStyle = FormBorderStyle.FixedDialog, Text = titleText, StartPosition = FormStartPosition.CenterParent, BackColor = bgColor, ForeColor = textColor };

            Label lblCmd = new Label { Text = isEnglish ? "Command / Bind:" : "Команда (бинд):", Location = new Point(20, 10), AutoSize = true };
            prompt.Controls.Add(lblCmd);

            TextBox textBox = new TextBox() { Left = 20, Top = 30, Width = 360, Text = currentCommand, BackColor = btnColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle };

            if (!bindings.ContainsKey(key) && defaultBindings.ContainsKey(key))
            {
                Label lblDef = new Label
                {
                    Text = isEnglish ? $"(Default: {defaultBindings[key]})" : $"(По умолчанию: {defaultBindings[key]})",
                    Location = new Point(lblCmd.Right + 5, 10),
                    AutoSize = true,
                    ForeColor = Color.DodgerBlue,
                    Cursor = Cursors.Hand
                };
                lblDef.Click += (s, e) => { textBox.Text = defaultBindings[key]; };
                prompt.Controls.Add(lblDef);
            }

            AutoCompleteStringCollection autoSource = new AutoCompleteStringCollection();
            autoSource.AddRange(popularCommands.ToArray());

            string[] aLines = txtAliases.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Regex aliasRegex = new Regex(@"^alias\s+""?([^""\s]+)""?", RegexOptions.IgnoreCase);
            foreach (var l in aLines)
            {
                Match m = aliasRegex.Match(l.Trim());
                if (m.Success) autoSource.Add(m.Groups[1].Value);
            }

            textBox.AutoCompleteCustomSource = autoSource;
            textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

            Button btnUnbind = new Button() { Text = "UNBIND", Left = 20, Width = 90, Top = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.Crimson, ForeColor = Color.White };
            btnUnbind.FlatAppearance.BorderSize = 0;
            btnUnbind.Click += (s, e) => { textBox.Text = "unbind"; };

            Button confirmation = new Button() { Text = "ОК", Left = 280, Width = 100, Top = 80, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White };
            confirmation.FlatAppearance.BorderSize = 0;

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(btnUnbind);
            prompt.Controls.Add(confirmation);

            bool isAlias = false;
            int foundAliasIndex = -1;

            if (!string.IsNullOrWhiteSpace(currentCommand))
            {
                string searchStr1 = $"alias \"{currentCommand}\"";
                string searchStr2 = $"alias {currentCommand} ";

                foundAliasIndex = txtAliases.Text.IndexOf(searchStr1, StringComparison.OrdinalIgnoreCase);
                if (foundAliasIndex == -1) foundAliasIndex = txtAliases.Text.IndexOf(searchStr2, StringComparison.OrdinalIgnoreCase);
                if (foundAliasIndex >= 0) isAlias = true;
            }

            if (isAlias)
            {
                Button btnFindAlias = new Button() { Text = isEnglish ? "FIND ALIAS" : "НАЙТИ АЛИАС", Left = 120, Width = 150, Top = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(147, 112, 219), ForeColor = Color.White };
                btnFindAlias.FlatAppearance.BorderSize = 0;
                btnFindAlias.Click += (s, e) => {
                    prompt.DialogResult = DialogResult.Ignore;
                    prompt.Close();

                    txtAliases.Focus();
                    int lineEnd = txtAliases.Text.IndexOf('\n', foundAliasIndex);
                    if (lineEnd == -1) lineEnd = txtAliases.Text.Length;

                    txtAliases.Select(foundAliasIndex, lineEnd - foundAliasIndex);
                    txtAliases.ScrollToCaret();
                };
                prompt.Controls.Add(btnFindAlias);
            }

            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                string newCmd = textBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(newCmd)) bindings.Remove(key);
                else if (newCmd.ToLower() == "unbind") bindings[key] = "UNBIND";
                else bindings[key] = newCmd;

                DrawInterface();
            }
        }
    }
}