using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace InputRepeater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Native.EnableDpiAwareness();

            if (args.Length > 0 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
            {
                SelfTest.Run();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MacroDocument
    {
        public string Version;
        public string CreatedAt;
        public string MouseCoordinateMode;
        public int RecordedCenterX;
        public int RecordedCenterY;
        public List<MacroEvent> Events;

        public MacroDocument()
        {
            Version = "3";
            CreatedAt = DateTime.UtcNow.ToString("o");
            MouseCoordinateMode = MouseCoordinateModes.ScreenCenterRelative;
            Events = new List<MacroEvent>();
        }
    }

    internal static class MouseCoordinateModes
    {
        public const string Absolute = "Absolute";
        public const string ScreenCenterRelative = "ScreenCenterRelative";
    }

    public class AppSettings
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public bool Maximized;
        public string ThemeName;
    }

    public class MacroEvent
    {
        public int TimeMs;
        public string Device;
        public int Message;
        public int X;
        public int Y;
        public int MouseData;
        public int VkCode;
        public int ScanCode;
        public int Flags;
    }

    internal sealed class ReplayRules
    {
        public int Repeats;
        public int MaxSeconds;
        public int StartDelaySeconds;
        public double SpeedMultiplier;
        public string RequiredTitle;
        public string[] BlockedTitles;
    }

    internal sealed class ThemeProfile
    {
        public string Name;
        public Color WindowBack;
        public Color PanelBack;
        public Color HeaderBack;
        public Color Text;
        public Color MutedText;
        public Color Accent;
        public Color AccentAlt;
        public Color Danger;
        public Color Warning;
        public Color Neutral;
        public Color StatusBack;
        public Color StatusText;
        public Color ListBack;
        public Color ListText;
        public Color GlassBack;
        public Color GlassBorder;
        public Color PopupBack;
        public bool DarkTitleBar;
        public int DwmBackdropType;

        public ThemeProfile(string name, Color windowBack, Color panelBack, Color headerBack, Color text, Color mutedText, Color accent, Color accentAlt, Color danger, Color warning, Color neutral, Color statusBack, Color statusText, Color listBack, Color listText, Color glassBack, Color glassBorder, Color popupBack, bool darkTitleBar, int dwmBackdropType)
        {
            Name = name;
            WindowBack = windowBack;
            PanelBack = panelBack;
            HeaderBack = headerBack;
            Text = text;
            MutedText = mutedText;
            Accent = accent;
            AccentAlt = accentAlt;
            Danger = danger;
            Warning = warning;
            Neutral = neutral;
            StatusBack = statusBack;
            StatusText = statusText;
            ListBack = listBack;
            ListText = listText;
            GlassBack = glassBack;
            GlassBorder = glassBorder;
            PopupBack = popupBack;
            DarkTitleBar = darkTitleBar;
            DwmBackdropType = dwmBackdropType;
        }
    }

    internal static class DwmBackdropTypes
    {
        public const int None = 1;
        public const int MainWindow = 2;
        public const int TransientWindow = 3;
        public const int TabbedWindow = 4;
    }

    internal sealed class GlassPanel : Panel
    {
        public Color FillColor;
        public Color BorderColor;
        public Color OutsideColor;
        public int Radius;

        public GlassPanel()
        {
            FillColor = Color.White;
            BorderColor = Color.FromArgb(220, 224, 230);
            OutsideColor = Color.FromArgb(246, 247, 249);
            Radius = 16;
            DoubleBuffered = true;
            BackColor = OutsideColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(OutsideColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using (System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(rect, Radius))
            using (SolidBrush brush = new SolidBrush(FillColor))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius * 2);
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class PillButton : Button
    {
        public PillButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            int radius = Math.Max(10, Math.Min(Height, 28));
            using (System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width, Height), radius))
            {
                Region = new Region(path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius);
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Bottom - diameter - 1, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter - 1, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class TrayPopupForm : Form
    {
        private readonly Func<ThemeProfile> themeProvider;
        private readonly Func<bool> recordingProvider;
        private readonly Func<bool> replayingProvider;
        private readonly Func<int> actionCountProvider;
        private readonly Action showWindowAction;
        private readonly Action recordAction;
        private readonly Action stopRecordAction;
        private readonly Action playAction;
        private readonly Action stopPlayAction;
        private readonly Action exitAction;

        private GlassPanel surface;
        private Label titleLabel;
        private Label statusLabel;
        private Button recordButton;
        private Button stopButton;
        private Button playButton;
        private Button stopPlayButton;
        private Button windowButton;
        private Button exitButton;
        private Label pinnedLabel;
        private Label recommendedLabel;
        private Label actionCountLabel;
        private Label modeLabel;
        private Label footerLabel;
        private Label hotkeyLabel;

        public TrayPopupForm(Func<ThemeProfile> getTheme, Func<bool> isRecording, Func<bool> isReplaying, Func<int> getActionCount, Action showWindow, Action record, Action stopRecord, Action play, Action stopPlay, Action exit)
        {
            themeProvider = getTheme;
            recordingProvider = isRecording;
            replayingProvider = isReplaying;
            actionCountProvider = getActionCount;
            showWindowAction = showWindow;
            recordAction = record;
            stopRecordAction = stopRecord;
            playAction = play;
            stopPlayAction = stopPlay;
            exitAction = exit;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Size = new Size(410, 430);
            Padding = new Padding(1);
            Font = new Font("Segoe UI", 9F);
            Opacity = 1.0;

            BuildUi();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000;
                return cp;
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedWindow();
            ThemeProfile theme = themeProvider();
            if (theme != null) Native.ApplyWindowBackdrop(Handle, theme.DarkTitleBar, DwmBackdropTypes.TransientWindow);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedWindow();
        }

        private void BuildUi()
        {
            surface = new GlassPanel();
            surface.Dock = DockStyle.Fill;
            surface.Padding = new Padding(24, 22, 24, 18);
            surface.Radius = 28;
            Controls.Add(surface);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            surface.Controls.Add(layout);

            var header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 2;
            header.RowCount = 1;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            layout.Controls.Add(header, 0, 0);

            titleLabel = new Label();
            titleLabel.Text = "Input Repeater";
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(titleLabel, 0, 0);

            windowButton = MakePopupButton("Full window", delegate { Hide(); showWindowAction(); });
            windowButton.Margin = new Padding(8, 5, 0, 5);
            header.Controls.Add(windowButton, 1, 0);

            pinnedLabel = MakeSectionLabel("Quick Actions");
            layout.Controls.Add(pinnedLabel, 0, 1);

            recordButton = MakePopupButton("Record", delegate { recordAction(); UpdateThemeAndState(); });
            stopButton = MakePopupButton("Stop record", delegate { stopRecordAction(); UpdateThemeAndState(); });
            playButton = MakePopupButton("Play macro", delegate { playAction(); UpdateThemeAndState(); });
            stopPlayButton = MakePopupButton("Stop Play", delegate { stopPlayAction(); UpdateThemeAndState(); });

            var pinnedGrid = new TableLayoutPanel();
            pinnedGrid.Dock = DockStyle.Fill;
            pinnedGrid.ColumnCount = 2;
            pinnedGrid.RowCount = 2;
            pinnedGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pinnedGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pinnedGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            pinnedGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.Controls.Add(pinnedGrid, 0, 2);
            pinnedGrid.Controls.Add(recordButton, 0, 0);
            pinnedGrid.Controls.Add(stopButton, 1, 0);
            pinnedGrid.Controls.Add(playButton, 0, 1);
            pinnedGrid.Controls.Add(stopPlayButton, 1, 1);

            recommendedLabel = MakeSectionLabel("Status");
            layout.Controls.Add(recommendedLabel, 0, 3);

            var recommendedGrid = new TableLayoutPanel();
            recommendedGrid.Dock = DockStyle.Fill;
            recommendedGrid.ColumnCount = 2;
            recommendedGrid.RowCount = 2;
            recommendedGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            recommendedGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            recommendedGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            recommendedGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.Controls.Add(recommendedGrid, 0, 4);

            actionCountLabel = MakeInfoRow();
            modeLabel = MakeInfoRow();
            statusLabel = MakeInfoRow();
            hotkeyLabel = MakeInfoRow();
            hotkeyLabel.Text = "Hotkeys\r\nF8 record, F9 play, F12 stop";
            recommendedGrid.Controls.Add(actionCountLabel, 0, 0);
            recommendedGrid.Controls.Add(modeLabel, 1, 0);
            recommendedGrid.Controls.Add(statusLabel, 0, 1);
            recommendedGrid.Controls.Add(hotkeyLabel, 1, 1);

            var footer = new TableLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.ColumnCount = 2;
            footer.RowCount = 1;
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            layout.Controls.Add(footer, 0, 6);

            footerLabel = new Label();
            footerLabel.Text = "Local automation";
            footerLabel.Dock = DockStyle.Fill;
            footerLabel.TextAlign = ContentAlignment.MiddleLeft;
            footerLabel.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular);
            footer.Controls.Add(footerLabel, 0, 0);

            exitButton = MakePopupButton("Exit", delegate { exitAction(); });
            exitButton.Margin = new Padding(8, 8, 0, 6);
            footer.Controls.Add(exitButton, 1, 0);
        }

        private Button MakePopupButton(string text, Action action)
        {
            var button = new PillButton();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            button.Click += delegate { action(); };
            return button;
        }

        private Label MakeSectionLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private Label MakeInfoRow()
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = new Padding(4);
            label.Padding = new Padding(14, 7, 14, 7);
            label.Font = new Font(Font.FontFamily, 8.8F, FontStyle.Regular);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        public void ShowNear(Point point)
        {
            UpdateThemeAndState();
            Rectangle area = Screen.FromPoint(point).WorkingArea;
            int x = point.X - Width + 18;
            int y = point.Y - Height - 18;
            if (x < area.Left + 8) x = area.Left + 8;
            if (y < area.Top + 8) y = point.Y + 18;
            if (x + Width > area.Right - 8) x = area.Right - Width - 8;
            if (y + Height > area.Bottom - 8) y = area.Bottom - Height - 8;
            Location = new Point(x, y);
                ApplyRoundedWindow();
            Show();
            Activate();
        }

        public void UpdateThemeAndState()
        {
            ThemeProfile theme = themeProvider();
            if (theme == null) return;

            BackColor = Opaque(theme.PopupBack);
            surface.FillColor = theme.GlassBack;
            surface.BorderColor = theme.GlassBorder;
            surface.OutsideColor = Opaque(theme.PopupBack);
            surface.BackColor = surface.OutsideColor;
            surface.Invalidate();
            titleLabel.ForeColor = theme.Text;
            pinnedLabel.ForeColor = theme.Text;
            recommendedLabel.ForeColor = theme.Text;
            footerLabel.ForeColor = theme.MutedText;
            statusLabel.ForeColor = theme.MutedText;
            StyleInfoRow(actionCountLabel, theme);
            StyleInfoRow(modeLabel, theme);
            StyleInfoRow(statusLabel, theme);
            StyleInfoRow(hotkeyLabel, theme);

            bool recording = recordingProvider();
            bool replaying = replayingProvider();
            int count = actionCountProvider();
            string state = recording ? "Recording now" : replaying ? "Playing macro" : "Ready";
            actionCountLabel.Text = "Actions\r\n" + count;
            modeLabel.Text = "Mode\r\nMouse recording";
            statusLabel.Text = "Status\r\n" + state;

            recordButton.Enabled = !recording && !replaying;
            stopButton.Enabled = recording;
            playButton.Enabled = !recording && !replaying && count > 0;
            stopPlayButton.Enabled = replaying;

            StylePopupButton(recordButton, theme.Accent, theme, false);
            StylePopupButton(stopButton, theme.Danger, theme, false);
            StylePopupButton(playButton, theme.AccentAlt, theme, false);
            StylePopupButton(stopPlayButton, theme.Warning, theme, false);
            StylePopupButton(windowButton, theme.Neutral, theme, true);
            StylePopupButton(exitButton, theme.Neutral, theme, true);

            if (IsHandleCreated) Native.ApplyWindowBackdrop(Handle, theme.DarkTitleBar, DwmBackdropTypes.TransientWindow);
        }

        private void ApplyRoundedWindow()
        {
            if (Width <= 0 || Height <= 0) return;
            using (System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width, Height), 28))
            {
                Region = new Region(path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius * 2);
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Bottom - diameter - 1, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter - 1, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void StyleInfoRow(Label label, ThemeProfile theme)
        {
            if (label == null) return;
            label.BackColor = theme.HeaderBack;
            label.ForeColor = theme.MutedText;
        }

        private static void StylePopupButton(Button button, Color color, ThemeProfile theme, bool neutral)
        {
            if (neutral)
            {
                button.BackColor = theme.HeaderBack;
                button.ForeColor = theme.Text;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, theme.HeaderBack.R + 10), Math.Min(255, theme.HeaderBack.G + 10), Math.Min(255, theme.HeaderBack.B + 10));
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(Math.Max(0, theme.HeaderBack.R - 10), Math.Max(0, theme.HeaderBack.G - 10), Math.Max(0, theme.HeaderBack.B - 10));
                return;
            }

            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, color.R + 14), Math.Min(255, color.G + 14), Math.Min(255, color.B + 14));
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(Math.Max(0, color.R - 14), Math.Max(0, color.G - 14), Math.Max(0, color.B - 14));
        }

        private static Color Opaque(Color color)
        {
            return Color.FromArgb(color.R, color.G, color.B);
        }
    }

    internal sealed class MainForm : Form
    {
        private const int HOTKEY_RECORD_TOGGLE = 1001;
        private const int HOTKEY_REPLAY = 1002;
        private const int HOTKEY_STOP = 1003;
        private const int VK_F8 = 0x77;
        private const int VK_F9 = 0x78;
        private const int VK_F12 = 0x7B;

        private readonly InputRecorder recorder;
        private readonly List<MacroEvent> events;
        private readonly object eventLock;
        private readonly GlobalHotkeyWatcher hotkeyWatcher;
        private MacroPlayer player;
        private Thread replayThread;
        private string mouseCoordinateMode;
        private Point recordingMouseOrigin;
        private int lastF8Tick;
        private int lastF9Tick;
        private int lastF12Tick;
        private int lastTrayClickTick;
        private bool allowExit;
        private bool startHidden;
        private bool mainWindowVisible;
        private bool listDirty;
        private ThemeProfile currentTheme;

        private Panel headerPanel;
        private Panel contentPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label themeLabel;
        private Button recordButton;
        private Button stopRecordButton;
        private Button replayButton;
        private Button stopReplayButton;
        private Button saveButton;
        private Button loadButton;
        private Button clearButton;
        private CheckBox recordKeyboardBox;
        private ListView eventList;
        private Label statusLabel;
        private Label countLabel;
        private NumericUpDown repeatBox;
        private NumericUpDown maxSecondsBox;
        private NumericUpDown delayBox;
        private NumericUpDown speedBox;
        private TextBox requiredTitleBox;
        private TextBox blockedTitlesBox;
        private ComboBox themeBox;
        private NotifyIcon trayIcon;
        private TrayPopupForm trayPopup;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayRecordItem;
        private ToolStripMenuItem trayStopRecordItem;
        private ToolStripMenuItem trayPlayItem;
        private ToolStripMenuItem trayStopPlayItem;
        private ToolStripMenuItem trayShowItem;
        private ToolStripMenuItem trayHideItem;
        private Icon appLogoIcon;
        private Icon generatedTrayIcon;
        private string trayIconStateKey;

        public MainForm()
        {
            Text = "Input Repeater";
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(460, 520);
            Size = new Size(520, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);
            appLogoIcon = CreateLogoIcon();
            if (appLogoIcon != null) Icon = appLogoIcon;

            events = new List<MacroEvent>();
            eventLock = new object();
            mouseCoordinateMode = MouseCoordinateModes.ScreenCenterRelative;
            recordingMouseOrigin = Point.Empty;
            startHidden = true;
            recorder = new InputRecorder();
            recorder.EventRecorded += RecorderOnEventRecorded;
            hotkeyWatcher = new GlobalHotkeyWatcher();
            hotkeyWatcher.HotkeyPressed += HotkeyWatcherOnHotkeyPressed;

            BuildUi();
            BuildTrayIcon();
            LoadWindowSettings();
            SetStatus("Ready. F8 records, F9 plays, F12 stops.");
            UpdateButtons();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RegisterHotKey(Handle, HOTKEY_RECORD_TOGGLE, 0, VK_F8);
            Native.RegisterHotKey(Handle, HOTKEY_REPLAY, 0, VK_F9);
            Native.RegisterHotKey(Handle, HOTKEY_STOP, 0, VK_F12);
            if (currentTheme != null) Native.ApplyWindowBackdrop(Handle, currentTheme.DarkTitleBar, currentTheme.DwmBackdropType);
            hotkeyWatcher.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            hotkeyWatcher.Stop();
            Native.UnregisterHotKey(Handle, HOTKEY_RECORD_TOGGLE);
            Native.UnregisterHotKey(Handle, HOTKEY_REPLAY);
            Native.UnregisterHotKey(Handle, HOTKEY_STOP);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                HandleHotkey(m.WParam.ToInt32());
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (startHidden)
            {
                startHidden = false;
                HideMainWindow();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            mainWindowVisible = Visible;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                HideMainWindow();
            }
        }

        private void HotkeyWatcherOnHotkeyPressed(object sender, HotkeyPressedEventArgs e)
        {
            int id = 0;
            if (e.VkCode == VK_F8) id = HOTKEY_RECORD_TOGGLE;
            else if (e.VkCode == VK_F9) id = HOTKEY_REPLAY;
            else if (e.VkCode == VK_F12) id = HOTKEY_STOP;
            if (id == 0 || IsDisposed) return;

            BeginInvoke(new MethodInvoker(delegate { HandleHotkey(id); }));
        }

        private void HandleHotkey(int id)
        {
            if (id == HOTKEY_RECORD_TOGGLE)
            {
                if (IsHotkeyRepeat(ref lastF8Tick)) return;
                if (recorder.IsRecording) StopRecording();
                else StartRecording();
                return;
            }
            if (id == HOTKEY_REPLAY)
            {
                if (IsHotkeyRepeat(ref lastF9Tick)) return;
                StartReplay();
                return;
            }
            if (id == HOTKEY_STOP)
            {
                if (IsHotkeyRepeat(ref lastF12Tick)) return;
                StopReplay();
            }
        }

        private static bool IsHotkeyRepeat(ref int lastTick)
        {
            int now = Environment.TickCount;
            if (Math.Abs(now - lastTick) < 350) return true;
            lastTick = now;
            return false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideMainWindow();
                return;
            }

            SaveWindowSettings();
            recorder.Stop();
            hotkeyWatcher.Stop();
            StopReplay();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
            if (generatedTrayIcon != null)
            {
                generatedTrayIcon.Dispose();
                generatedTrayIcon = null;
            }
            if (appLogoIcon != null)
            {
                appLogoIcon.Dispose();
                appLogoIcon = null;
            }
            if (trayPopup != null)
            {
                trayPopup.Dispose();
                trayPopup = null;
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }
            base.OnFormClosing(e);
        }

        private void BuildUi()
        {
            SuspendLayout();

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);

            headerPanel = new GlassPanel();
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Margin = new Padding(0, 0, 0, 10);
            headerPanel.Padding = new Padding(16, 8, 16, 8);
            headerPanel.Tag = "HeaderGlass";
            root.Controls.Add(headerPanel, 0, 0);

            var headerLayout = new TableLayoutPanel();
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.ColumnCount = 2;
            headerLayout.RowCount = 1;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            headerPanel.Controls.Add(headerLayout);

            var titleLayout = new TableLayoutPanel();
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.ColumnCount = 1;
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            headerLayout.Controls.Add(titleLayout, 0, 0);

            titleLabel = new Label();
            titleLabel.Text = "Input Repeater";
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.BottomLeft;
            titleLabel.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            titleLabel.Tag = "Title";
            titleLayout.Controls.Add(titleLabel, 0, 0);

            subtitleLabel = new Label();
            subtitleLabel.Text = "Small macro remote";
            subtitleLabel.Dock = DockStyle.Fill;
            subtitleLabel.TextAlign = ContentAlignment.TopLeft;
            subtitleLabel.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular);
            subtitleLabel.Tag = "Muted";
            titleLayout.Controls.Add(subtitleLabel, 0, 1);

            var themeLayout = new TableLayoutPanel();
            themeLayout.Dock = DockStyle.Fill;
            themeLayout.ColumnCount = 1;
            themeLayout.RowCount = 2;
            themeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            themeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            headerLayout.Controls.Add(themeLayout, 1, 0);

            themeLabel = MakePlainLabel("Theme");
            themeLabel.TextAlign = ContentAlignment.BottomLeft;
            themeLayout.Controls.Add(themeLabel, 0, 0);

            themeBox = new ComboBox();
            themeBox.Dock = DockStyle.Top;
            themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            themeBox.Margin = new Padding(0, 3, 0, 0);
            string[] themeNames = ThemeNames();
            for (int i = 0; i < themeNames.Length; i++) themeBox.Items.Add(themeNames[i]);
            themeBox.SelectedIndex = 0;
            themeBox.SelectedIndexChanged += delegate { ApplyTheme(FindTheme(themeBox.Text)); };
            themeLayout.Controls.Add(themeBox, 0, 1);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.AutoScroll = true;
            contentPanel.Margin = new Padding(0);
            root.Controls.Add(contentPanel, 0, 1);

            var summary = MakeGroup("Current Macro");
            summary.Height = 96;
            summary.Dock = DockStyle.Top;
            contentPanel.Controls.Add(summary);

            var summaryLayout = new TableLayoutPanel();
            summaryLayout.Dock = DockStyle.Fill;
            summaryLayout.ColumnCount = 1;
            summaryLayout.RowCount = 2;
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            summary.Controls.Add(summaryLayout);

            countLabel = new Label();
            countLabel.Text = "No macro recorded";
            countLabel.Dock = DockStyle.Fill;
            countLabel.TextAlign = ContentAlignment.BottomLeft;
            countLabel.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
            summaryLayout.Controls.Add(countLabel, 0, 0);

            var hintLabel = new Label();
            hintLabel.Text = "Use Record, then Play. Coordinates are hidden.";
            hintLabel.Dock = DockStyle.Fill;
            hintLabel.TextAlign = ContentAlignment.TopLeft;
            hintLabel.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular);
            hintLabel.Tag = "Muted";
            summaryLayout.Controls.Add(hintLabel, 0, 1);

            var actions = MakeGroup("Controls");
            actions.Height = 156;
            actions.Dock = DockStyle.Top;
            contentPanel.Controls.Add(actions);

            var actionGrid = new TableLayoutPanel();
            actionGrid.Dock = DockStyle.Fill;
            actionGrid.ColumnCount = 2;
            actionGrid.RowCount = 3;
            actionGrid.Padding = new Padding(8);
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            actions.Controls.Add(actionGrid);

            recordButton = MakeButton("Record", StartRecording, Color.FromArgb(46, 125, 50));
            stopRecordButton = MakeButton("Stop", StopRecording, Color.FromArgb(183, 28, 28));
            replayButton = MakeButton("Play", StartReplay, Color.FromArgb(21, 101, 192));
            stopReplayButton = MakeButton("Stop Play", StopReplay, Color.FromArgb(230, 126, 34));
            recordButton.Tag = "Accent";
            stopRecordButton.Tag = "Danger";
            replayButton.Tag = "AccentAlt";
            stopReplayButton.Tag = "Warning";

            actionGrid.Controls.Add(recordButton, 0, 0);
            actionGrid.Controls.Add(stopRecordButton, 1, 0);
            actionGrid.Controls.Add(replayButton, 0, 1);
            actionGrid.Controls.Add(stopReplayButton, 1, 1);

            recordKeyboardBox = new CheckBox();
            recordKeyboardBox.Text = "Also record keyboard";
            recordKeyboardBox.Dock = DockStyle.Fill;
            recordKeyboardBox.Margin = new Padding(8, 0, 4, 0);
            recordKeyboardBox.TextAlign = ContentAlignment.MiddleLeft;
            actionGrid.Controls.Add(recordKeyboardBox, 0, 2);
            actionGrid.SetColumnSpan(recordKeyboardBox, 2);

            var replay = MakeGroup("Replay Options");
            replay.Height = 148;
            replay.Dock = DockStyle.Top;
            contentPanel.Controls.Add(replay);

            var replayGrid = new TableLayoutPanel();
            replayGrid.Dock = DockStyle.Fill;
            replayGrid.ColumnCount = 2;
            replayGrid.RowCount = 4;
            replayGrid.Padding = new Padding(10, 4, 10, 8);
            replayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            replayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            for (int i = 0; i < 4; i++) replayGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            replay.Controls.Add(replayGrid);

            repeatBox = MakeNumber(replayGrid, "Repeats", 0, 1, 20, 1);
            maxSecondsBox = MakeNumber(replayGrid, "Stop after", 1, 1, 3600, 120);
            delayBox = MakeNumber(replayGrid, "Start delay", 2, 0, 60, 3);
            speedBox = MakeNumber(replayGrid, "Speed %", 3, 10, 400, 100);

            var files = MakeGroup("Macros");
            files.Height = 86;
            files.Dock = DockStyle.Top;
            contentPanel.Controls.Add(files);

            var fileGrid = new TableLayoutPanel();
            fileGrid.Dock = DockStyle.Fill;
            fileGrid.ColumnCount = 3;
            fileGrid.RowCount = 1;
            fileGrid.Padding = new Padding(8);
            fileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            fileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            fileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            files.Controls.Add(fileGrid);

            saveButton = MakeButton("Save", SaveMacro, Color.FromArgb(70, 79, 96));
            loadButton = MakeButton("Open", LoadMacro, Color.FromArgb(70, 79, 96));
            clearButton = MakeButton("Clear", ClearMacro, Color.FromArgb(117, 117, 117));
            saveButton.Tag = "Neutral";
            loadButton.Tag = "Neutral";
            clearButton.Tag = "Neutral";
            fileGrid.Controls.Add(saveButton, 0, 0);
            fileGrid.Controls.Add(loadButton, 1, 0);
            fileGrid.Controls.Add(clearButton, 2, 0);
            contentPanel.Controls.SetChildIndex(summary, 0);
            contentPanel.Controls.SetChildIndex(actions, 1);
            contentPanel.Controls.SetChildIndex(replay, 2);
            contentPanel.Controls.SetChildIndex(files, 3);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Padding = new Padding(12, 0, 12, 0);
            statusLabel.Font = new Font(Font.FontFamily, 8.8F, FontStyle.Regular);
            root.Controls.Add(statusLabel, 0, 2);

            requiredTitleBox = new TextBox();
            blockedTitlesBox = new TextBox();
            blockedTitlesBox.Text = "password\r\nlogin\r\nsign in\r\nbank\r\ncheckout";

            eventList = new ListView();
            eventList.View = View.Details;
            eventList.FullRowSelect = true;
            eventList.GridLines = true;
            eventList.Columns.Add("#", 60);
            eventList.Columns.Add("When", 90);
            eventList.Columns.Add("Type", 80);
            eventList.Columns.Add("Action", 160);
            eventList.Columns.Add("Position / Detail", 420);

            ApplyTheme(FindTheme(themeBox.Text));
            ResumeLayout();
        }

        private static string[] ThemeNames()
        {
            return new string[] { "Swift Light", "Swift Dark", "Frost Blue" };
        }

        private static ThemeProfile FindTheme(string name)
        {
            if (string.Equals(name, "Swift Dark", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Apple Dark", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Liquid Dark", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Graphite", StringComparison.OrdinalIgnoreCase))
            {
                return new ThemeProfile(
                    "Swift Dark",
                    Color.FromArgb(18, 18, 20),
                    Color.FromArgb(31, 31, 35),
                    Color.FromArgb(42, 42, 47),
                    Color.FromArgb(246, 246, 248),
                    Color.FromArgb(174, 174, 181),
                    Color.FromArgb(64, 156, 255),
                    Color.FromArgb(48, 209, 88),
                    Color.FromArgb(255, 69, 58),
                    Color.FromArgb(255, 159, 10),
                    Color.FromArgb(72, 72, 78),
                    Color.FromArgb(31, 31, 35),
                    Color.FromArgb(246, 246, 248),
                    Color.FromArgb(26, 26, 30),
                    Color.FromArgb(246, 246, 248),
                    Color.FromArgb(37, 37, 42),
                    Color.FromArgb(67, 67, 74),
                    Color.FromArgb(26, 26, 30),
                    true,
                    DwmBackdropTypes.MainWindow);
            }
            if (string.Equals(name, "Frost Blue", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Blue Tint", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Aurora", StringComparison.OrdinalIgnoreCase))
            {
                return new ThemeProfile(
                    "Frost Blue",
                    Color.FromArgb(236, 243, 255),
                    Color.FromArgb(250, 253, 255),
                    Color.FromArgb(229, 239, 255),
                    Color.FromArgb(24, 31, 42),
                    Color.FromArgb(89, 102, 122),
                    Color.FromArgb(0, 122, 255),
                    Color.FromArgb(32, 188, 123),
                    Color.FromArgb(255, 59, 48),
                    Color.FromArgb(255, 149, 0),
                    Color.FromArgb(116, 128, 145),
                    Color.FromArgb(225, 237, 255),
                    Color.FromArgb(24, 31, 42),
                    Color.FromArgb(252, 253, 255),
                    Color.FromArgb(24, 31, 42),
                    Color.FromArgb(250, 252, 255),
                    Color.FromArgb(212, 225, 242),
                    Color.FromArgb(250, 252, 255),
                    false,
                    DwmBackdropTypes.MainWindow);
            }

            return new ThemeProfile(
                "Swift Light",
                Color.FromArgb(242, 242, 247),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(247, 247, 250),
                Color.FromArgb(29, 29, 31),
                Color.FromArgb(99, 99, 102),
                Color.FromArgb(0, 122, 255),
                Color.FromArgb(52, 199, 89),
                Color.FromArgb(255, 59, 48),
                Color.FromArgb(255, 149, 0),
                Color.FromArgb(142, 142, 147),
                Color.FromArgb(248, 248, 250),
                Color.FromArgb(29, 29, 31),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(29, 29, 31),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(218, 218, 222),
                Color.FromArgb(255, 255, 255),
                false,
                DwmBackdropTypes.MainWindow);
        }

        internal static ThemeProfile TestTheme()
        {
            return FindTheme("Swift Light");
        }

        private void ApplyTheme(ThemeProfile theme)
        {
            if (theme == null) return;
            currentTheme = theme;
            BackColor = theme.WindowBack;
            ApplyThemeToControl(this, theme);

            if (headerPanel != null) headerPanel.BackColor = theme.HeaderBack;
            if (contentPanel != null) contentPanel.BackColor = theme.WindowBack;
            if (statusLabel != null)
            {
                statusLabel.BackColor = theme.StatusBack;
                statusLabel.ForeColor = theme.StatusText;
            }
            if (eventList != null)
            {
                eventList.BackColor = theme.ListBack;
                eventList.ForeColor = theme.ListText;
            }
            ApplyButtonTheme(recordButton, theme.Accent);
            ApplyButtonTheme(replayButton, theme.AccentAlt);
            ApplyButtonTheme(stopRecordButton, theme.Danger);
            ApplyButtonTheme(stopReplayButton, theme.Warning);
            ApplyButtonTheme(saveButton, theme.Neutral);
            ApplyButtonTheme(loadButton, theme.Neutral);
            ApplyButtonTheme(clearButton, theme.Neutral);
            if (IsHandleCreated) Native.ApplyWindowBackdrop(Handle, theme.DarkTitleBar, theme.DwmBackdropType);
        }

        private void ApplyThemeToControl(Control control, ThemeProfile theme)
        {
            if (control == this)
            {
                control.BackColor = theme.WindowBack;
            }
            else if (control is GlassPanel)
            {
                var glass = (GlassPanel)control;
                glass.FillColor = string.Equals(Convert.ToString(control.Tag), "HeaderGlass", StringComparison.OrdinalIgnoreCase) ? theme.HeaderBack : theme.GlassBack;
                glass.BorderColor = theme.GlassBorder;
                glass.OutsideColor = theme.WindowBack;
                glass.BackColor = theme.WindowBack;
                glass.ForeColor = theme.Text;
                glass.Invalidate();
            }
            else if (control is GroupBox)
            {
                control.BackColor = theme.PanelBack;
                control.ForeColor = theme.Text;
            }
            else if (control is Label)
            {
                control.BackColor = Color.Transparent;
                control.ForeColor = string.Equals(Convert.ToString(control.Tag), "Muted", StringComparison.OrdinalIgnoreCase) ? theme.MutedText : theme.Text;
            }
            else if (control is TextBox || control is NumericUpDown || control is ComboBox)
            {
                control.BackColor = theme.ListBack;
                control.ForeColor = theme.ListText;
            }
            else if (control is CheckBox)
            {
                control.BackColor = theme.PanelBack;
                control.ForeColor = theme.MutedText;
            }
            else if (control is Panel)
            {
                control.BackColor = theme.PanelBack;
            }
            else if (control is TableLayoutPanel)
            {
                control.BackColor = Color.Transparent;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, theme);
            }
        }

        private void ApplyButtonTheme(Button button, Color color)
        {
            if (button == null) return;
            bool neutral = button.Tag != null && string.Equals(Convert.ToString(button.Tag), "Neutral", StringComparison.OrdinalIgnoreCase);
            if (neutral)
            {
                button.BackColor = currentTheme == null ? Color.FromArgb(232, 232, 237) : currentTheme.HeaderBack;
                button.ForeColor = currentTheme == null ? Color.FromArgb(29, 29, 31) : currentTheme.Text;
                button.FlatAppearance.MouseOverBackColor = currentTheme == null ? Color.FromArgb(222, 222, 227) : Lighten(currentTheme.HeaderBack, 10);
                button.FlatAppearance.MouseDownBackColor = currentTheme == null ? Color.FromArgb(210, 210, 215) : Darken(currentTheme.HeaderBack, 10);
                return;
            }

            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Lighten(color, 14);
            button.FlatAppearance.MouseDownBackColor = Darken(color, 14);
        }

        private static Color Lighten(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }

        private static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        private void BuildTrayIcon()
        {
            trayPopup = new TrayPopupForm(
                GetCurrentTheme,
                IsRecording,
                IsReplaying,
                EventCount,
                ShowMainWindow,
                StartRecording,
                StopRecording,
                StartReplay,
                StopReplay,
                ExitApplication);
            BuildTrayContextMenu();
            trayIcon = new NotifyIcon();
            generatedTrayIcon = CreateLogoIcon();
            if (generatedTrayIcon == null) generatedTrayIcon = CreateTrayIcon(GetCurrentTheme(), "idle");
            trayIconStateKey = "logo";
            trayIcon.Icon = generatedTrayIcon;
            trayIcon.Text = "Input Repeater";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseClick += TrayIconOnMouseClick;
            trayIcon.MouseDoubleClick += delegate
            {
                lastTrayClickTick = Environment.TickCount;
                if (trayPopup != null) trayPopup.Hide();
                ShowMainWindow();
            };
        }

        private void BuildTrayContextMenu()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Opening += delegate { UpdateTrayContextMenu(); };
            trayRecordItem = MakeTrayMenuItem("Record", StartRecording);
            trayStopRecordItem = MakeTrayMenuItem("Stop Recording", StopRecording);
            trayPlayItem = MakeTrayMenuItem("Play", StartReplay);
            trayStopPlayItem = MakeTrayMenuItem("Stop Play", StopReplay);
            trayShowItem = MakeTrayMenuItem("Show Full Window", ShowMainWindow);
            trayHideItem = MakeTrayMenuItem("Hide Full Window", HideMainWindow);
            trayMenu.Items.Add(trayRecordItem);
            trayMenu.Items.Add(trayStopRecordItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayPlayItem);
            trayMenu.Items.Add(trayStopPlayItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayShowItem);
            trayMenu.Items.Add(trayHideItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(MakeTrayMenuItem("Exit", ExitApplication));
        }

        private ToolStripMenuItem MakeTrayMenuItem(string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += delegate { action(); };
            return item;
        }

        private void TrayIconOnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Math.Abs(Environment.TickCount - lastTrayClickTick) < 250) return;
                if (trayPopup == null) return;
                if (trayPopup.Visible) trayPopup.Hide();
                else trayPopup.ShowNear(Cursor.Position);
                lastTrayClickTick = Environment.TickCount;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (trayPopup != null) trayPopup.Hide();
                UpdateTrayContextMenu();
            }
        }

        private ThemeProfile GetCurrentTheme()
        {
            return currentTheme == null ? FindTheme("Swift Light") : currentTheme;
        }

        private bool IsRecording()
        {
            return recorder.IsRecording;
        }

        private int EventCount()
        {
            lock (eventLock)
            {
                return events.Count;
            }
        }

        private void ShowMainWindow()
        {
            ShowInTaskbar = true;
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            if (listDirty)
            {
                listDirty = false;
                RefreshEventList();
            }
            UpdateButtons();
        }

        private void HideMainWindow()
        {
            Hide();
            ShowInTaskbar = false;
            UpdateButtons();
        }

        private void ExitApplication()
        {
            allowExit = true;
            Close();
        }

        private void LoadWindowSettings()
        {
            try
            {
                string path = SettingsPath();
                if (!File.Exists(path)) return;
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    var settings = (AppSettings)serializer.Deserialize(stream);
                    SelectTheme(settings.ThemeName);
                    if (settings.Width < MinimumSize.Width || settings.Height < MinimumSize.Height) return;
                    if (settings.Width > 700 || settings.Height > 780) return;

                    var bounds = new Rectangle(settings.X, settings.Y, settings.Width, settings.Height);
                    if (!IsVisibleOnAnyScreen(bounds)) return;

                    StartPosition = FormStartPosition.Manual;
                    Bounds = bounds;
                    if (settings.Maximized) WindowState = FormWindowState.Maximized;
                }
            }
            catch
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                var settings = new AppSettings();
                settings.X = bounds.X;
                settings.Y = bounds.Y;
                settings.Width = bounds.Width;
                settings.Height = bounds.Height;
                settings.Maximized = WindowState == FormWindowState.Maximized;
                settings.ThemeName = currentTheme == null ? "Swift Light" : currentTheme.Name;

                string path = SettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = File.Create(path))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(stream, settings);
                }
            }
            catch
            {
            }
        }

        private static string SettingsPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InputRepeater");
            return Path.Combine(folder, "settings.xml");
        }

        private static bool IsVisibleOnAnyScreen(Rectangle bounds)
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                if (Screen.AllScreens[i].WorkingArea.IntersectsWith(bounds)) return true;
            }
            return false;
        }

        private void SelectTheme(string themeName)
        {
            if (themeBox == null || string.IsNullOrEmpty(themeName)) return;
            for (int i = 0; i < themeBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(themeBox.Items[i]), themeName, StringComparison.OrdinalIgnoreCase))
                {
                    themeBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ResetWindowSize()
        {
            WindowState = FormWindowState.Normal;
            Size = new Size(520, 640);
            CenterToScreen();
            SetStatus("Window size reset.");
        }

        private Panel MakeGroup(string text)
        {
            var group = new GlassPanel();
            group.Dock = DockStyle.Fill;
            group.Margin = new Padding(0, 0, 0, 12);
            group.Padding = new Padding(12, 32, 12, 12);
            group.Tag = "Glass";

            var label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Height = 24;
            label.Left = 14;
            label.Top = 8;
            label.Width = 260;
            label.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Tag = "SectionTitle";
            label.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            group.Controls.Add(label);
            return group;
        }

        private Button MakeButton(string text, Action action, Color color)
        {
            var button = new PillButton();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            button.Click += delegate { action(); };
            return button;
        }

        private Label MakePlainLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.ForeColor = Color.FromArgb(70, 79, 96);
            return label;
        }

        private NumericUpDown MakeNumber(TableLayoutPanel parent, string labelText, int row, int min, int max, int value)
        {
            var label = MakePlainLabel(labelText);
            label.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(label, 0, row);

            var number = new NumericUpDown();
            number.Dock = DockStyle.Right;
            number.Width = 110;
            number.Height = 22;
            number.Margin = new Padding(3, 2, 3, 2);
            number.Minimum = min;
            number.Maximum = max;
            number.Value = value;
            parent.Controls.Add(number, 1, row);
            return number;
        }

        private void StartRecording()
        {
            if (IsReplaying())
            {
                SetStatus("Stop play first.");
                return;
            }

            ClearMacro();
            mouseCoordinateMode = MouseCoordinateModes.ScreenCenterRelative;
            Point center = ScreenCenterForCursor();
            Native.MoveMousePrecisely(center.X, center.Y);
            recordingMouseOrigin = center;
            recorder.MouseCoordinateMode = mouseCoordinateMode;
            recorder.MouseOrigin = center;
            recorder.RecordKeyboard = recordKeyboardBox.Checked;
            recorder.Start();
            string mode = recordKeyboardBox.Checked ? "mouse and keyboard" : "mouse";
            SetStatus("Recording " + mode + " from screen center. Press F8 when you are done.");
            UpdateButtons();
        }

        private void StopRecording()
        {
            recorder.Stop();
            SetStatus("Recording stopped.");
            UpdateButtons();
        }

        private void StartReplay()
        {
            if (recorder.IsRecording)
            {
                SetStatus("Stop recording first.");
                return;
            }

            if (IsReplaying())
            {
                SetStatus("Already playing.");
                return;
            }

            List<MacroEvent> snapshot;
            lock (eventLock)
            {
                snapshot = new List<MacroEvent>(events);
            }

            if (snapshot.Count == 0)
            {
                SetStatus("Record or open a file first.");
                return;
            }

            var rules = ReadRules();
            string reason;
            if (!MacroPlayer.WindowAllowed(rules, out reason))
            {
                SetStatus(reason);
                MessageBox.Show(this, reason, "Replay blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            player = new MacroPlayer(snapshot, rules, mouseCoordinateMode);
            replayThread = new Thread(new ThreadStart(delegate
            {
                player.Play(PostStatus);
                BeginInvoke(new MethodInvoker(delegate
                {
                    SetStatus(player.LastStatus);
                    player = null;
                    replayThread = null;
                    UpdateButtons();
                }));
            }));
            replayThread.IsBackground = true;
            replayThread.Start();
            SetStatus("Playing. Press F12 to stop.");
            UpdateButtons();
        }

        private void StopReplay()
        {
            if (player != null)
            {
                player.RequestStop();
                SetStatus("Stopping...");
            }
            UpdateButtons();
        }

        private void SaveMacro()
        {
            List<MacroEvent> snapshot;
            lock (eventLock)
            {
                snapshot = new List<MacroEvent>(events);
            }

            if (snapshot.Count == 0)
            {
                SetStatus("Nothing to save yet.");
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Input Repeater Macro (*.irm.xml)|*.irm.xml|XML files (*.xml)|*.xml";
                dialog.FileName = "macro.irm.xml";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                var doc = new MacroDocument();
                doc.MouseCoordinateMode = mouseCoordinateMode;
                doc.RecordedCenterX = recordingMouseOrigin.X;
                doc.RecordedCenterY = recordingMouseOrigin.Y;
                doc.Events = snapshot;
                using (var stream = File.Create(dialog.FileName))
                {
                    var serializer = new XmlSerializer(typeof(MacroDocument));
                    serializer.Serialize(stream, doc);
                }
                SetStatus("Saved " + snapshot.Count + " actions.");
            }
        }

        private void LoadMacro()
        {
            if (recorder.IsRecording || IsReplaying())
            {
                SetStatus("Stop first, then open a file.");
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Input Repeater Macro (*.irm.xml)|*.irm.xml|XML files (*.xml)|*.xml";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                using (var stream = File.OpenRead(dialog.FileName))
                {
                    var serializer = new XmlSerializer(typeof(MacroDocument));
                    var doc = (MacroDocument)serializer.Deserialize(stream);
                    lock (eventLock)
                    {
                        events.Clear();
                        events.AddRange(doc.Events);
                    }
                    mouseCoordinateMode = string.IsNullOrEmpty(doc.MouseCoordinateMode) ? MouseCoordinateModes.Absolute : doc.MouseCoordinateMode;
                    recordingMouseOrigin = new Point(doc.RecordedCenterX, doc.RecordedCenterY);
                    RefreshEventList();
                    SetStatus("Opened " + events.Count + " actions.");
                }
            }
        }

        private void ClearMacro()
        {
            if (recorder.IsRecording) return;
            lock (eventLock)
            {
                events.Clear();
            }
            mouseCoordinateMode = MouseCoordinateModes.ScreenCenterRelative;
            recordingMouseOrigin = Point.Empty;
            eventList.Items.Clear();
            UpdateCount();
            SetStatus("Cleared.");
        }

        private static Point ScreenCenterForCursor()
        {
            Rectangle screen = Screen.FromPoint(Cursor.Position).Bounds;
            return new Point(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
        }

        private ReplayRules ReadRules()
        {
            var rules = new ReplayRules();
            rules.Repeats = (int)repeatBox.Value;
            rules.MaxSeconds = (int)maxSecondsBox.Value;
            rules.StartDelaySeconds = (int)delayBox.Value;
            rules.SpeedMultiplier = Math.Max(0.1, (double)speedBox.Value / 100.0);
            rules.RequiredTitle = requiredTitleBox.Text.Trim();
            rules.BlockedTitles = SplitLines(blockedTitlesBox.Text);
            return rules;
        }

        private static string[] SplitLines(string text)
        {
            var raw = text.Replace("\r", "").Split('\n');
            var kept = new List<string>();
            for (int i = 0; i < raw.Length; i++)
            {
                string item = raw[i].Trim();
                if (item.Length > 0) kept.Add(item);
            }
            return kept.ToArray();
        }

        private void RecorderOnEventRecorded(object sender, MacroEventRecordedEventArgs e)
        {
            if (IsControlHotkey(e.Event)) return;

            int count;
            lock (eventLock)
            {
                events.Add(e.Event);
                count = events.Count;
            }

            if (!mainWindowVisible || !IsHandleCreated || IsDisposed)
            {
                listDirty = true;
                return;
            }

            BeginInvoke(new MethodInvoker(delegate
            {
                if (IsDisposed) return;
                AddEventRow(e.Event, count);
                UpdateCount();
            }));
        }

        private static bool IsControlHotkey(MacroEvent item)
        {
            if (item.Device != "Keyboard") return false;
            return item.VkCode == VK_F8 || item.VkCode == VK_F9 || item.VkCode == VK_F12;
        }

        private void RefreshEventList()
        {
            eventList.BeginUpdate();
            eventList.Items.Clear();
            lock (eventLock)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    AddEventRow(events[i], i + 1);
                }
            }
            eventList.EndUpdate();
            UpdateCount();
        }

        private void AddEventRow(MacroEvent item, int index)
        {
            var row = new ListViewItem(index.ToString());
            row.SubItems.Add(item.TimeMs + " ms");
            row.SubItems.Add(item.Device);
            row.SubItems.Add(EventDescriptions.Action(item));
            row.SubItems.Add(EventDescriptions.Detail(item, mouseCoordinateMode));
            eventList.Items.Add(row);
            if (eventList.Items.Count > 0) eventList.EnsureVisible(eventList.Items.Count - 1);
        }

        private bool IsReplaying()
        {
            return player != null && player.IsRunning;
        }

        private void UpdateButtons()
        {
            bool recording = recorder.IsRecording;
            bool replaying = IsReplaying();
            recordButton.Enabled = !recording && !replaying;
            stopRecordButton.Enabled = recording;
            replayButton.Enabled = !recording && !replaying;
            stopReplayButton.Enabled = replaying;
            saveButton.Enabled = !recording && !replaying && events.Count > 0;
            loadButton.Enabled = !recording && !replaying;
            clearButton.Enabled = !recording && !replaying && events.Count > 0;
            recordKeyboardBox.Enabled = !recording && !replaying;
            UpdateTrayMenu();
        }

        private void UpdateCount()
        {
            countLabel.Text = events.Count == 0 ? "No macro recorded" : events.Count + " action" + (events.Count == 1 ? "" : "s") + " ready";
            UpdateButtons();
        }

        private void PostStatus(string text)
        {
            if (IsDisposed) return;
            BeginInvoke(new MethodInvoker(delegate { SetStatus(text); }));
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
            if (trayIcon != null)
            {
                trayIcon.Text = ShortTrayText("Input Repeater - " + text);
            }
        }

        private void UpdateTrayMenu()
        {
            UpdateTrayIcon();
            if (trayPopup != null) trayPopup.UpdateThemeAndState();
            UpdateTrayContextMenu();
        }

        private void UpdateTrayContextMenu()
        {
            if (trayMenu == null || trayRecordItem == null) return;

            bool recording = recorder.IsRecording;
            bool replaying = IsReplaying();
            int count = EventCount();

            trayRecordItem.Enabled = !recording && !replaying;
            trayStopRecordItem.Enabled = recording;
            trayPlayItem.Enabled = !recording && !replaying && count > 0;
            trayStopPlayItem.Enabled = replaying;
            trayShowItem.Enabled = !mainWindowVisible;
            trayHideItem.Enabled = mainWindowVisible;

            ThemeProfile theme = GetCurrentTheme();
            trayMenu.BackColor = theme.PopupBack;
            trayMenu.ForeColor = theme.Text;
            for (int i = 0; i < trayMenu.Items.Count; i++)
            {
                trayMenu.Items[i].BackColor = theme.PopupBack;
                trayMenu.Items[i].ForeColor = theme.Text;
            }
        }

        private void UpdateTrayIcon()
        {
            if (trayIcon == null) return;
            if (HasEmbeddedLogoIcon())
            {
                if (string.Equals(trayIconStateKey, "logo", StringComparison.Ordinal)) return;

                Icon nextLogo = CreateLogoIcon();
                if (nextLogo != null)
                {
                    Icon oldLogo = generatedTrayIcon;
                    generatedTrayIcon = nextLogo;
                    trayIcon.Icon = nextLogo;
                    trayIconStateKey = "logo";
                    if (oldLogo != null) oldLogo.Dispose();
                    return;
                }
            }

            string state = recorder.IsRecording ? "recording" : IsReplaying() ? "playing" : "idle";
            ThemeProfile theme = GetCurrentTheme();
            string key = theme.Name + ":" + state;
            if (string.Equals(key, trayIconStateKey, StringComparison.Ordinal)) return;

            Icon next = CreateTrayIcon(theme, state);
            Icon old = generatedTrayIcon;
            generatedTrayIcon = next;
            trayIcon.Icon = next;
            trayIconStateKey = key;
            if (old != null) old.Dispose();
        }

        private static Icon CreateTrayIcon(ThemeProfile theme, string state)
        {
            int size = 32;
            using (var bitmap = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Color accent = theme.Accent;
                if (state == "recording") accent = theme.Danger;
                else if (state == "playing") accent = theme.AccentAlt;

                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(theme.DarkTitleBar ? 90 : 45, Color.Black)))
                {
                    g.FillEllipse(shadow, 4, 5, 24, 24);
                }
                using (SolidBrush fill = new SolidBrush(accent))
                using (Pen ring = new Pen(Color.FromArgb(190, Color.White), 2))
                {
                    g.FillEllipse(fill, 3, 3, 24, 24);
                    g.DrawEllipse(ring, 3, 3, 24, 24);
                }

                if (state == "recording")
                {
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(brush, 11, 11, 8, 8);
                    }
                }
                else if (state == "playing")
                {
                    Point[] points = new Point[] { new Point(12, 9), new Point(12, 21), new Point(21, 15) };
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    {
                        g.FillPolygon(brush, points);
                    }
                }
                else
                {
                    using (Pen pen = new Pen(Color.White, 2))
                    {
                        g.DrawArc(pen, 9, 10, 11, 9, 35, 265);
                        g.DrawLine(pen, 19, 10, 22, 10);
                        g.DrawLine(pen, 20, 8, 22, 10);
                    }
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    Icon icon = (Icon)Icon.FromHandle(handle).Clone();
                    return icon;
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }

        private static Icon CreateLogoIcon()
        {
            try
            {
                Icon extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (extracted == null) return null;
                using (extracted)
                {
                    return (Icon)extracted.Clone();
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool HasEmbeddedLogoIcon()
        {
            return true;
        }

        private static string ShortTrayText(string text)
        {
            if (text.Length <= 63) return text;
            return text.Substring(0, 60) + "...";
        }
    }

    internal sealed class HotkeyPressedEventArgs : EventArgs
    {
        public int VkCode { get; private set; }

        public HotkeyPressedEventArgs(int vkCode)
        {
            VkCode = vkCode;
        }
    }

    internal sealed class GlobalHotkeyWatcher
    {
        private const int VK_F8 = 0x77;
        private const int VK_F9 = 0x78;
        private const int VK_F12 = 0x7B;

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        private readonly Dictionary<int, bool> keyDown;
        private Native.LowLevelProc keyboardProc;
        private IntPtr keyboardHook;

        public GlobalHotkeyWatcher()
        {
            keyDown = new Dictionary<int, bool>();
        }

        public void Start()
        {
            if (keyboardHook != IntPtr.Zero) return;

            keyboardProc = KeyboardHookCallback;
            using (Process current = Process.GetCurrentProcess())
            using (ProcessModule module = current.MainModule)
            {
                IntPtr handle = Native.GetModuleHandle(module.ModuleName);
                keyboardHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, keyboardProc, handle, 0);
            }
        }

        public void Stop()
        {
            if (keyboardHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
            }
            keyDown.Clear();
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var data = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
                int vkCode = (int)data.vkCode;
                bool isWatchedKey = vkCode == VK_F8 || vkCode == VK_F9 || vkCode == VK_F12;
                bool injected = (data.flags & Native.LLKHF_INJECTED) != 0;

                if (isWatchedKey && !injected)
                {
                    bool isDownMessage = wParam.ToInt32() == Native.WM_KEYDOWN || wParam.ToInt32() == Native.WM_SYSKEYDOWN;
                    bool isUpMessage = wParam.ToInt32() == Native.WM_KEYUP || wParam.ToInt32() == Native.WM_SYSKEYUP;

                    if (isDownMessage)
                    {
                        bool wasDown = keyDown.ContainsKey(vkCode) && keyDown[vkCode];
                        if (!wasDown)
                        {
                            keyDown[vkCode] = true;
                            var handler = HotkeyPressed;
                            if (handler != null) handler(this, new HotkeyPressedEventArgs(vkCode));
                        }
                    }
                    else if (isUpMessage)
                    {
                        keyDown[vkCode] = false;
                    }
                }
            }

            return Native.CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }
    }

    internal sealed class InputRecorder
    {
        private const int MinMouseMoveIntervalMs = 8;

        public event EventHandler<MacroEventRecordedEventArgs> EventRecorded;
        public Point MouseOrigin;
        public string MouseCoordinateMode;
        public bool RecordKeyboard;

        private Native.LowLevelProc keyboardProc;
        private Native.LowLevelProc mouseProc;
        private IntPtr keyboardHook;
        private IntPtr mouseHook;
        private int startTick;
        private bool hasMouseMove;
        private int lastMouseMoveElapsedMs;
        private int lastMouseMoveX;
        private int lastMouseMoveY;

        public bool IsRecording { get; private set; }

        public InputRecorder()
        {
            MouseCoordinateMode = MouseCoordinateModes.ScreenCenterRelative;
            RecordKeyboard = false;
        }

        public void Start()
        {
            if (IsRecording) return;

            mouseProc = MouseHookCallback;
            using (Process current = Process.GetCurrentProcess())
            using (ProcessModule module = current.MainModule)
            {
                IntPtr handle = Native.GetModuleHandle(module.ModuleName);
                if (RecordKeyboard)
                {
                    keyboardProc = KeyboardHookCallback;
                    keyboardHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, keyboardProc, handle, 0);
                }
                mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, mouseProc, handle, 0);
            }

            if ((RecordKeyboard && keyboardHook == IntPtr.Zero) || mouseHook == IntPtr.Zero)
            {
                Stop();
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install input hooks.");
            }

            startTick = Environment.TickCount;
            hasMouseMove = false;
            lastMouseMoveElapsedMs = 0;
            lastMouseMoveX = 0;
            lastMouseMoveY = 0;
            IsRecording = true;
        }

        public void Stop()
        {
            if (keyboardHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
            }
            if (mouseHook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
            IsRecording = false;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsRecording && RecordKeyboard)
            {
                var data = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
                if ((data.flags & Native.LLKHF_INJECTED) == 0)
                {
                    var item = new MacroEvent();
                    item.TimeMs = Environment.TickCount - startTick;
                    item.Device = "Keyboard";
                    item.Message = wParam.ToInt32();
                    item.VkCode = (int)data.vkCode;
                    item.ScanCode = (int)data.scanCode;
                    item.Flags = (int)data.flags;
                    Raise(item);
                }
            }
            return Native.CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsRecording)
            {
                var data = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT));
                if ((data.flags & Native.LLMHF_INJECTED) == 0)
                {
                    int message = wParam.ToInt32();
                    int elapsed = Environment.TickCount - startTick;
                    if (message == Native.WM_MOUSEMOVE && ShouldSkipMouseMove(elapsed, data.pt.x, data.pt.y))
                    {
                        return Native.CallNextHookEx(mouseHook, nCode, wParam, lParam);
                    }

                    var item = new MacroEvent();
                    item.TimeMs = elapsed;
                    item.Device = "Mouse";
                    item.Message = message;
                    if (MouseCoordinateMode == MouseCoordinateModes.ScreenCenterRelative)
                    {
                        item.X = data.pt.x - MouseOrigin.X;
                        item.Y = data.pt.y - MouseOrigin.Y;
                    }
                    else
                    {
                        item.X = data.pt.x;
                        item.Y = data.pt.y;
                    }
                    item.MouseData = (int)data.mouseData;
                    item.Flags = (int)data.flags;
                    Raise(item);
                }
            }
            return Native.CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private bool ShouldSkipMouseMove(int elapsedMs, int x, int y)
        {
            if (hasMouseMove && x == lastMouseMoveX && y == lastMouseMoveY) return true;
            if (hasMouseMove && elapsedMs - lastMouseMoveElapsedMs < MinMouseMoveIntervalMs) return true;

            hasMouseMove = true;
            lastMouseMoveElapsedMs = elapsedMs;
            lastMouseMoveX = x;
            lastMouseMoveY = y;
            return false;
        }

        private void Raise(MacroEvent item)
        {
            var handler = EventRecorded;
            if (handler != null) handler(this, new MacroEventRecordedEventArgs(item));
        }
    }

    internal sealed class MacroEventRecordedEventArgs : EventArgs
    {
        public MacroEvent Event { get; private set; }

        public MacroEventRecordedEventArgs(MacroEvent item)
        {
            Event = item;
        }
    }

    internal sealed class MacroPlayer
    {
        private const int VK_F12 = 0x7B;

        private readonly List<MacroEvent> events;
        private readonly ReplayRules rules;
        private readonly string mouseCoordinateMode;
        private volatile bool stopRequested;
        private Point replayMouseOrigin;

        public bool IsRunning { get; private set; }
        public string LastStatus { get; private set; }

        public MacroPlayer(List<MacroEvent> eventsToPlay, ReplayRules replayRules, string coordinateMode)
        {
            events = eventsToPlay;
            rules = replayRules;
            mouseCoordinateMode = string.IsNullOrEmpty(coordinateMode) ? MouseCoordinateModes.Absolute : coordinateMode;
            LastStatus = "Ready.";
        }

        public void RequestStop()
        {
            stopRequested = true;
        }

        public void Play(Action<string> status)
        {
            IsRunning = true;
            Stopwatch total = Stopwatch.StartNew();
            try
            {
                for (int second = rules.StartDelaySeconds; second > 0; second--)
                {
                    status("Replay starts in " + second + "...");
                    if (!WaitWithStop(1000)) return;
                }

                for (int repeat = 1; repeat <= rules.Repeats; repeat++)
                {
                    if (UsesScreenCenter())
                    {
                        replayMouseOrigin = ScreenCenterForCursor();
                        Native.MoveMousePrecisely(replayMouseOrigin.X, replayMouseOrigin.Y);
                    }

                    status("Playing " + repeat + " of " + rules.Repeats + ". F12 stops.");
                    int previous = 0;
                    for (int i = 0; i < events.Count; i++)
                    {
                        string reason;
                        if (!WindowAllowed(rules, out reason))
                        {
                            LastStatus = reason;
                            return;
                        }

                        if (rules.MaxSeconds > 0 && total.Elapsed.TotalSeconds >= rules.MaxSeconds)
                        {
                            LastStatus = "Replay stopped by max seconds restriction.";
                            return;
                        }

                        MacroEvent item = events[i];
                        int delay = item.TimeMs - previous;
                        if (delay < 0) delay = 0;
                        previous = item.TimeMs;
                        int adjustedDelay = (int)Math.Round(delay / rules.SpeedMultiplier);
                        if (!WaitWithStop(adjustedDelay)) return;

                        Send(item);
                    }
                }

                LastStatus = "Replay finished.";
            }
            finally
            {
                IsRunning = false;
                if (stopRequested || IsStopKeyDown())
                {
                    LastStatus = "Replay stopped.";
                }
            }
        }

        public static bool WindowAllowed(ReplayRules rules, out string reason)
        {
            string title = Native.GetForegroundWindowTitle();
            string lowerTitle = title.ToLowerInvariant();

            for (int i = 0; i < rules.BlockedTitles.Length; i++)
            {
                string blocked = rules.BlockedTitles[i].Trim().ToLowerInvariant();
                if (blocked.Length > 0 && lowerTitle.IndexOf(blocked) >= 0)
                {
                    reason = "Replay blocked because the active window title contains \"" + rules.BlockedTitles[i] + "\".";
                    return false;
                }
            }

            if (rules.RequiredTitle.Length > 0 && lowerTitle.IndexOf(rules.RequiredTitle.ToLowerInvariant()) < 0)
            {
                reason = "Replay blocked because the active window is \"" + title + "\".";
                return false;
            }

            reason = "";
            return true;
        }

        private bool WaitWithStop(int milliseconds)
        {
            int remaining = milliseconds;
            while (remaining > 0)
            {
                if (stopRequested || IsStopKeyDown())
                {
                    LastStatus = "Replay stopped.";
                    return false;
                }
                int slice = Math.Min(remaining, 25);
                Thread.Sleep(slice);
                remaining -= slice;
            }
            return true;
        }

        private static bool IsStopKeyDown()
        {
            return (Native.GetAsyncKeyState(VK_F12) & 0x8000) != 0;
        }

        private bool UsesScreenCenter()
        {
            return mouseCoordinateMode == MouseCoordinateModes.ScreenCenterRelative;
        }

        private static Point ScreenCenterForCursor()
        {
            Rectangle screen = Screen.FromPoint(Cursor.Position).Bounds;
            return new Point(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
        }

        private void Send(MacroEvent item)
        {
            if (item.Device == "Keyboard")
            {
                SendKeyboard(item);
            }
            else if (item.Device == "Mouse")
            {
                SendMouse(item);
            }
        }

        private static void SendKeyboard(MacroEvent item)
        {
            uint flags = 0;
            if (item.Message == Native.WM_KEYUP || item.Message == Native.WM_SYSKEYUP) flags |= Native.KEYEVENTF_KEYUP;
            if ((item.Flags & Native.LLKHF_EXTENDED) != 0) flags |= Native.KEYEVENTF_EXTENDEDKEY;

            var input = new Native.INPUT();
            input.type = Native.INPUT_KEYBOARD;
            input.U.ki.wVk = (ushort)item.VkCode;
            input.U.ki.wScan = 0;
            input.U.ki.dwFlags = flags;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = UIntPtr.Zero;
            Native.SendSingleInput(input);
        }

        private void SendMouse(MacroEvent item)
        {
            Point target = MouseTarget(item);
            if (item.Message == Native.WM_MOUSEMOVE)
            {
                Native.MoveMousePrecisely(target.X, target.Y);
                return;
            }

            Native.MoveMousePrecisely(target.X, target.Y);

            uint flags = 0;
            uint data = 0;
            if (item.Message == Native.WM_LBUTTONDOWN) flags = Native.MOUSEEVENTF_LEFTDOWN;
            else if (item.Message == Native.WM_LBUTTONUP) flags = Native.MOUSEEVENTF_LEFTUP;
            else if (item.Message == Native.WM_RBUTTONDOWN) flags = Native.MOUSEEVENTF_RIGHTDOWN;
            else if (item.Message == Native.WM_RBUTTONUP) flags = Native.MOUSEEVENTF_RIGHTUP;
            else if (item.Message == Native.WM_MBUTTONDOWN) flags = Native.MOUSEEVENTF_MIDDLEDOWN;
            else if (item.Message == Native.WM_MBUTTONUP) flags = Native.MOUSEEVENTF_MIDDLEUP;
            else if (item.Message == Native.WM_MOUSEWHEEL)
            {
                flags = Native.MOUSEEVENTF_WHEEL;
                data = (uint)(short)Native.HighWord(item.MouseData);
            }
            else if (item.Message == Native.WM_XBUTTONDOWN)
            {
                flags = Native.MOUSEEVENTF_XDOWN;
                data = (uint)Native.HighWord(item.MouseData);
            }
            else if (item.Message == Native.WM_XBUTTONUP)
            {
                flags = Native.MOUSEEVENTF_XUP;
                data = (uint)Native.HighWord(item.MouseData);
            }
            else
            {
                return;
            }

            var input = new Native.INPUT();
            input.type = Native.INPUT_MOUSE;
            input.U.mi.dx = 0;
            input.U.mi.dy = 0;
            input.U.mi.mouseData = data;
            input.U.mi.dwFlags = flags;
            input.U.mi.time = 0;
            input.U.mi.dwExtraInfo = UIntPtr.Zero;
            Native.SendSingleInput(input);
        }

        private Point MouseTarget(MacroEvent item)
        {
            if (UsesScreenCenter())
            {
                return new Point(replayMouseOrigin.X + item.X, replayMouseOrigin.Y + item.Y);
            }
            return new Point(item.X, item.Y);
        }
    }

    internal static class EventDescriptions
    {
        public static string Action(MacroEvent item)
        {
            if (item.Device == "Keyboard")
            {
                if (item.Message == Native.WM_KEYDOWN || item.Message == Native.WM_SYSKEYDOWN) return "Key down";
                if (item.Message == Native.WM_KEYUP || item.Message == Native.WM_SYSKEYUP) return "Key up";
                return "Keyboard " + item.Message;
            }

            if (item.Message == Native.WM_MOUSEMOVE) return "Move";
            if (item.Message == Native.WM_LBUTTONDOWN) return "Left down";
            if (item.Message == Native.WM_LBUTTONUP) return "Left up";
            if (item.Message == Native.WM_RBUTTONDOWN) return "Right down";
            if (item.Message == Native.WM_RBUTTONUP) return "Right up";
            if (item.Message == Native.WM_MBUTTONDOWN) return "Middle down";
            if (item.Message == Native.WM_MBUTTONUP) return "Middle up";
            if (item.Message == Native.WM_MOUSEWHEEL) return "Wheel";
            if (item.Message == Native.WM_XBUTTONDOWN) return "XButton down";
            if (item.Message == Native.WM_XBUTTONUP) return "XButton up";
            return "Mouse " + item.Message;
        }

        public static string Detail(MacroEvent item, string mouseCoordinateMode)
        {
            if (item.Device == "Keyboard")
            {
                return "VK " + item.VkCode + ", scan " + item.ScanCode;
            }
            string position;
            if (mouseCoordinateMode == MouseCoordinateModes.ScreenCenterRelative)
            {
                position = "right " + item.X + ", down " + item.Y + " from center";
            }
            else
            {
                position = "x " + item.X + ", y " + item.Y;
            }
            if (item.Message == Native.WM_MOUSEWHEEL)
            {
                return position + ", wheel " + (short)Native.HighWord(item.MouseData);
            }
            return position;
        }
    }

    internal static class SelfTest
    {
        public static void Run()
        {
            var doc = new MacroDocument();
            doc.Events.Add(new MacroEvent { TimeMs = 10, Device = "Keyboard", Message = Native.WM_KEYDOWN, VkCode = 65 });
            doc.Events.Add(new MacroEvent { TimeMs = 30, Device = "Keyboard", Message = Native.WM_KEYUP, VkCode = 65 });

            var serializer = new XmlSerializer(typeof(MacroDocument));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, doc);
                stream.Position = 0;
                var loaded = (MacroDocument)serializer.Deserialize(stream);
                if (loaded.Events.Count != 2) throw new Exception("Self-test failed.");
            }

            using (var popup = new TrayPopupForm(
                delegate { return MainForm.TestTheme(); },
                delegate { return false; },
                delegate { return false; },
                delegate { return 0; },
                delegate { },
                delegate { },
                delegate { },
                delegate { },
                delegate { },
                delegate { }))
            {
                popup.UpdateThemeAndState();
            }

            Console.WriteLine("Self-test passed.");
        }
    }

    internal static class Native
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;

        public const uint LLKHF_EXTENDED = 0x01;
        public const uint LLKHF_INJECTED = 0x10;
        public const uint LLMHF_INJECTED = 0x01;

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWCP_ROUND = 2;

        public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessDpiAwarenessContextPrivate(IntPtr dpiContext);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        public static void SendSingleInput(INPUT input)
        {
            INPUT[] inputs = new INPUT[] { input };
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContextPrivate(new IntPtr(-4))) return;
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        public static void ApplyWindowBackdrop(IntPtr hwnd, bool darkTitleBar, int backdropType)
        {
            try
            {
                int dark = darkTitleBar ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, Marshal.SizeOf(typeof(int)));
            }
            catch
            {
            }

            try
            {
                int corners = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, Marshal.SizeOf(typeof(int)));
            }
            catch
            {
            }

            try
            {
                int backdrop = backdropType;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, Marshal.SizeOf(typeof(int)));
            }
            catch
            {
            }
        }

        public static void MoveMousePrecisely(int x, int y)
        {
            SendAbsoluteMouseMove(x, y);

            POINT actual;
            if (!GetCursorPos(out actual) || actual.x != x || actual.y != y)
            {
                SetCursorPos(x, y);
            }
        }

        private static void SendAbsoluteMouseMove(int x, int y)
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = Math.Max(1, GetSystemMetrics(SM_CXVIRTUALSCREEN));
            int height = Math.Max(1, GetSystemMetrics(SM_CYVIRTUALSCREEN));

            int normalizedX = NormalizeAbsoluteCoordinate(x, left, width);
            int normalizedY = NormalizeAbsoluteCoordinate(y, top, height);

            var input = new INPUT();
            input.type = INPUT_MOUSE;
            input.U.mi.dx = normalizedX;
            input.U.mi.dy = normalizedY;
            input.U.mi.mouseData = 0;
            input.U.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_MOVE_NOCOALESCE;
            input.U.mi.time = 0;
            input.U.mi.dwExtraInfo = UIntPtr.Zero;
            SendSingleInput(input);
        }

        private static int NormalizeAbsoluteCoordinate(int value, int origin, int size)
        {
            if (size <= 1) return 0;
            long relative = value - origin;
            long normalized = relative * 65535L / (size - 1);
            if (normalized < 0) return 0;
            if (normalized > 65535) return 65535;
            return (int)normalized;
        }

        public static int HighWord(int value)
        {
            return (value >> 16) & 0xffff;
        }

        public static string GetForegroundWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            var builder = new StringBuilder(512);
            GetWindowText(handle, builder, builder.Capacity);
            return builder.ToString();
        }
    }
}
