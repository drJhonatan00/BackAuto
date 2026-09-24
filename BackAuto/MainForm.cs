using BackAuto.Models;
using BackAuto.Services;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace BackAuto;

public sealed class MainForm : Form
{
    private readonly AppSettings settings = AppSettings.Load();
    private readonly BackupService backupService = new();
    private readonly System.Windows.Forms.Timer scheduleTimer = new() { Interval = 30_000 };
    private readonly CheckedListBox fileList = new();
    private readonly TextBox destinationBox = new();
    private readonly NumericUpDown intervalBox = new();
    private readonly CheckBox scheduleToggle = new();
    private readonly CheckBox themeToggle = new();
    private readonly Label statusLabel = new();
    private readonly Label nextRunLabel = new();
    private readonly Label filesCountLabel = new();
    private readonly Label lastBackupLabel = new();
    private readonly RichTextBox activityLog = new();
    private readonly Button runButton = new();
    private readonly Button backgroundButton = new();
    private readonly Button saveButton = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private CancellationTokenSource? backupCancellation;
    private bool darkMode;

    private Color Background => darkMode ? Color.FromArgb(18, 22, 30) : Color.FromArgb(247, 249, 252);
    private Color Surface => darkMode ? Color.FromArgb(29, 35, 46) : Color.White;
    private Color SurfaceAlt => darkMode ? Color.FromArgb(38, 45, 58) : Color.FromArgb(241, 244, 248);
    private Color TextPrimary => darkMode ? Color.FromArgb(239, 242, 248) : Color.FromArgb(26, 32, 44);
    private Color TextSecondary => darkMode ? Color.FromArgb(164, 174, 192) : Color.FromArgb(103, 113, 132);
    private Color Accent => Color.FromArgb(76, 110, 245);
    private Color Border => darkMode ? Color.FromArgb(53, 62, 78) : Color.FromArgb(224, 229, 238);

    public MainForm()
    {
        Text = "BackAuto";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 680);
        Size = new Size(1200, 780);
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered = true;
        darkMode = settings.DarkMode;
        ConfigureTrayIcon();
        BuildInterface();
        LoadSettings();
        scheduleTimer.Tick += (_, _) => TryRunScheduledBackup();
        scheduleTimer.Start();
        FormClosing += (_, _) => { settings.Save(); backupCancellation?.Cancel(); };
    }

    private void ConfigureTrayIcon()
    {
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.Text = "BackAuto — automated backups";
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        var restoreItem = new ToolStripMenuItem("Open BackAuto");
        restoreItem.Click += (_, _) => RestoreFromTray();
        var runItem = new ToolStripMenuItem("Run backup now");
        runItem.Click += async (_, _) => { RestoreFromTray(); await RunBackupAsync(); };
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => { trayIcon.Visible = false; Close(); };
        trayMenu.Items.AddRange([restoreItem, runItem, new ToolStripSeparator(), exitItem]);
        trayIcon.ContextMenuStrip = trayMenu;
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        trayIcon.ShowBalloonTip(2500, "BackAuto is running", "Scheduled backups will continue in the background.", ToolTipIcon.Info);
        Log("Application minimised to the Windows notification area.");
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void BuildInterface()
    {
        SuspendLayout();
        BackColor = Background;
        Font = new Font("Segoe UI", 9.5F);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 244));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildContent(), 1, 0);
        ResumeLayout();
    }

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        side.Paint += (_, e) => { using var pen = new Pen(Border); e.Graphics.DrawLine(pen, side.Width - 1, 0, side.Width - 1, side.Height); };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 24, 20, 20), RowCount = 5, ColumnCount = 1, BackColor = Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        side.Controls.Add(layout);

        var brand = new Panel { Dock = DockStyle.Fill };
        var mark = new Panel { Size = new Size(42, 42), Location = new Point(0, 6), BackColor = Accent };
        mark.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var brush = new SolidBrush(Color.White); e.Graphics.FillEllipse(brush, 11, 9, 20, 20); using var pen = new Pen(Accent, 3); e.Graphics.DrawLine(pen, 21, 13, 21, 27); e.Graphics.DrawLine(pen, 21, 20, 27, 20); };
        brand.Controls.Add(mark);
        brand.Controls.Add(LabelOf("BackAuto", 54, 5, 150, 28, 17, FontStyle.Bold, TextPrimary));
        brand.Controls.Add(LabelOf("AUTOMATED BACKUPS", 54, 31, 170, 18, 7.5F, FontStyle.Bold, TextSecondary));
        layout.Controls.Add(brand, 0, 0);

        var navLabel = LabelOf("WORKSPACE", 0, 0, 200, 22, 8, FontStyle.Bold, TextSecondary); navLabel.Margin = new Padding(0, 12, 0, 0); layout.Controls.Add(navLabel, 0, 1);
        var nav = new Panel { Dock = DockStyle.Fill };
        nav.Controls.Add(NavItem("▦", "Overview", true, 0));
        layout.Controls.Add(nav, 0, 2);



        var footer = new Panel { Dock = DockStyle.Fill };
        footer.Controls.Add(LinkLabelOf("Developed by drJhonatan00", 0, 4, 180, 18, 8, FontStyle.Bold, TextSecondary, "https://github.com/drJhonatan00"));
        themeToggle.Text = "Dark theme"; themeToggle.AutoSize = true; themeToggle.Location = new Point(0, 30); themeToggle.ForeColor = TextSecondary; themeToggle.BackColor = Surface; themeToggle.Checked = darkMode; themeToggle.CheckedChanged += (_, _) => { darkMode = themeToggle.Checked; ApplyTheme(); };
        footer.Controls.Add(themeToggle); layout.Controls.Add(footer, 0, 4);
        return side;
    }


    private LinkLabel LinkLabelOf(string text, int x,  int y, int w, int h, float size, FontStyle style, Color color, string url)
    {
        var link = new LinkLabel
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            Font = new Font(Font.FontFamily, size, style),
            LinkColor = color,
            Cursor = Cursors.Hand
        };
        link.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
        return link;
    }
    private Control NavItem(string icon, string text, bool selected, int top)
    {
        var p = new Panel { Location = new Point(0, top), Size = new Size(200, 40), BackColor = selected ? (darkMode ? Color.FromArgb(43, 54, 78) : Color.FromArgb(235, 240, 255)) : Surface };
        p.Controls.Add(LabelOf(icon, 14, 9, 24, 22, 14, FontStyle.Regular, selected ? Accent : TextSecondary));
        p.Controls.Add(LabelOf(text, 50, 10, 140, 22, 10, FontStyle.Bold, selected ? Accent : TextSecondary));
        return p;
    }

    private Control BuildContent()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Background, Padding = new Padding(34, 28, 34, 26), AutoScroll = true };
        outer.Controls.Add(BuildDashboard()); return outer;
    }

    private Control BuildDashboard()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 6, BackColor = Background };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 330)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(LabelOf("Backup overview", 0, 0, 400, 36, 20, FontStyle.Bold, TextPrimary));
        header.Controls.Add(LabelOf("Protect your important files with a simple, reliable schedule.", 2, 42, 600, 22, 10.5F, FontStyle.Regular, TextSecondary));
        backgroundButton.Text = "  Run in background"; backgroundButton.Size = new Size(166, 40); backgroundButton.Anchor = AnchorStyles.Top | AnchorStyles.Right; backgroundButton.Location = new Point(Width - 620, 8); StyleButton(backgroundButton, SurfaceAlt, TextPrimary); backgroundButton.Click += (_, _) => HideToTray(); backgroundButton.FlatAppearance.BorderSize = 1; backgroundButton.FlatAppearance.BorderColor = Border; header.Controls.Add(backgroundButton);
        runButton.Text = "  Run backup now"; runButton.Size = new Size(150, 40); runButton.Anchor = AnchorStyles.Top | AnchorStyles.Right; runButton.Location = new Point(Width - 440, 8); StyleButton(runButton, Accent, Color.White); runButton.Click += async (_, _) => await RunBackupAsync(); header.Controls.Add(runButton);
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(BuildStats(), 0, 1);
        panel.Controls.Add(LabelOf("\nBACKUP SETUP", 0, 0, 200, 22, 7, FontStyle.Bold, TextSecondary), 0, 2);
        panel.Controls.Add(BuildSetupCard(), 0, 3);
        panel.Controls.Add(LabelOf("\nACTIVITY", 0, 0, 200, 22, 7, FontStyle.Bold, TextSecondary), 0, 4);
        panel.Controls.Add(BuildActivityCard(), 0, 5);
        return panel;
    }

    private Control BuildStats()
    {
        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Background };
        for (var i = 0; i < 3; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        stats.Controls.Add(StatCard("SELECTED FILES", filesCountLabel, "0 files ready", Accent), 0, 0);
        stats.Controls.Add(StatCard("NEXT BACKUP", nextRunLabel, "Schedule is off", Color.FromArgb(30, 166, 114)), 1, 0);
        stats.Controls.Add(StatCard("LAST BACKUP", lastBackupLabel, "Not yet backed up", Color.FromArgb(236, 141, 54)), 2, 0);
        return stats;
    }

    private Panel StatCard(string title, Label value, string sub, Color stripe)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(18), BorderStyle = BorderStyle.FixedSingle };
        card.Paint += (_, e) => { using var b = new SolidBrush(stripe); e.Graphics.FillRectangle(b, 0, 0, 4, card.Height); };
        card.Controls.Add(LabelOf(title, 18, 14, 180, 17, 8, FontStyle.Bold, TextSecondary));
        value.AutoSize = true; value.Location = new Point(18, 34); value.Font = new Font("Segoe UI", 16, FontStyle.Bold); value.ForeColor = TextPrimary; value.BackColor = Color.Transparent; card.Controls.Add(value);
        card.Controls.Add(LabelOf(sub, 18, 68, 240, 18, 8.5F, FontStyle.Regular, TextSecondary)); return card;
    }

    private Control BuildSetupCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(22), BorderStyle = BorderStyle.FixedSingle };
        card.Controls.Add(LabelOf("What should be backed up?", 22, 18, 400, 26, 13, FontStyle.Bold, TextPrimary));
        var add = new Button { Text = "+  Add files", Location = new Point(22, 54), Size = new Size(112, 31) }; StyleButton(add, SurfaceAlt, TextPrimary); add.Click += (_, _) => AddFiles(); card.Controls.Add(add);
        var clear = new Button { Text = "Clear all", Location = new Point(142, 54), Size = new Size(82, 31) }; StyleButton(clear, SurfaceAlt, TextSecondary); clear.Click += (_, _) => { fileList.Items.Clear(); SaveSettings(); UpdateStats(); Log("All backup file paths cleared."); }; card.Controls.Add(clear);
        fileList.Location = new Point(22, 96); fileList.Size = new Size(390, 205); fileList.BorderStyle = BorderStyle.FixedSingle; fileList.CheckOnClick = true; fileList.Font = new Font("Segoe UI", 9); fileList.IntegralHeight = false; fileList.BackColor = SurfaceAlt; fileList.ForeColor = TextPrimary; fileList.Anchor = AnchorStyles.Top  | AnchorStyles.Left; fileList.SelectedIndexChanged += (_, _) => UpdateStats(); card.Controls.Add(fileList);
        card.Controls.Add(LabelOf("DESTINATION FOLDER", 440, 18, 300, 18, 8, FontStyle.Bold, TextSecondary));
        destinationBox.Location = new Point(440, 42); destinationBox.Size = new Size(400, 31); destinationBox.Anchor = AnchorStyles.Top | AnchorStyles.Left; destinationBox.BorderStyle = BorderStyle.FixedSingle; destinationBox.Font = new Font("Segoe UI", 9.5F); card.Controls.Add(destinationBox);
        var browse = new Button { Text = "Browse", Location = new Point(875, 42), Size = new Size(80, 31), Anchor = AnchorStyles.Top | AnchorStyles.Right }; StyleButton(browse, SurfaceAlt, TextPrimary); browse.Click += (_, _) => BrowseDestination(); card.Controls.Add(browse);
        card.Controls.Add(LabelOf("SCHEDULE", 440, 101, 250, 18, 8, FontStyle.Bold, TextSecondary));
        scheduleToggle.Text = "Run automatically"; scheduleToggle.Location = new Point(440, 125); scheduleToggle.AutoSize = true; scheduleToggle.ForeColor = TextPrimary; scheduleToggle.BackColor = Surface; scheduleToggle.CheckedChanged += (_, _) => { settings.ScheduleEnabled = scheduleToggle.Checked; UpdateStats(); settings.Save(); }; card.Controls.Add(scheduleToggle);
        card.Controls.Add(LabelOf("Every", 440, 171, 45, 24, 9.5F, FontStyle.Regular, TextSecondary));
        intervalBox.Location = new Point(484, 168); intervalBox.Size = new Size(74, 30); intervalBox.Minimum = 1; intervalBox.Maximum = 10080; intervalBox.Value = settings.IntervalMinutes; intervalBox.Font = new Font("Segoe UI", 9.5F); intervalBox.ValueChanged += (_, _) => { settings.IntervalMinutes = (int)intervalBox.Value; settings.Save(); UpdateStats(); }; card.Controls.Add(intervalBox);
        card.Controls.Add(LabelOf("minutes", 566, 171, 80, 24, 9.5F, FontStyle.Regular, TextSecondary));
        saveButton.Text = "Save settings"; saveButton.Location = new Point(440, 228); saveButton.Size = new Size(136, 36); saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; StyleButton(saveButton, Accent, Color.White); saveButton.Click += (_, _) => SaveSettings(); card.Controls.Add(saveButton);
        return card;
    }

    private Control BuildActivityCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(16), BorderStyle = BorderStyle.FixedSingle };
        activityLog.Dock = DockStyle.Fill; activityLog.BorderStyle = BorderStyle.None; activityLog.ReadOnly = true; activityLog.BackColor = Surface; activityLog.ForeColor = TextSecondary; activityLog.Font = new Font("Consolas", 9); activityLog.ScrollBars = RichTextBoxScrollBars.Vertical; card.Controls.Add(activityLog); return card;
    }

    private void AddFiles()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "Select files to back up", CheckFileExists = true, Filter = "All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var file in dialog.FileNames)
            if (!fileList.Items.Cast<string>().Any(existing => string.Equals(existing, file, StringComparison.OrdinalIgnoreCase))) fileList.Items.Add(file, true);
        UpdateStats(); SaveSettings();
    }

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose the folder where backups will be stored", SelectedPath = destinationBox.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK) { destinationBox.Text = dialog.SelectedPath; SaveSettings(); }
    }

    private async Task RunBackupAsync()
    {
        if (backupCancellation is not null) return;
        SaveSettings();
        if (fileList.CheckedItems.Count == 0) { SetStatus("Add at least one file before running a backup.", false); return; }
        if (string.IsNullOrWhiteSpace(destinationBox.Text)) { SetStatus("Choose a destination folder first.", false); return; }
        backupCancellation = new CancellationTokenSource(); runButton.Enabled = false; SetStatus("Backup in progress…", true); Log("Starting backup…");
        try
        {
            var progress = new Progress<string>(message => Log(message));
            var result = await backupService.RunAsync(fileList.CheckedItems.Cast<string>(), destinationBox.Text, progress, backupCancellation.Token);
            settings.LastBackupUtc = DateTime.UtcNow; settings.Save();
            SetStatus(result.Failed == 0 ? $"Backup completed • {result.Copied} copied, {result.Skipped} unchanged" : $"Backup completed with {result.Failed} error(s)", result.Failed == 0);
            Log($"Finished in {result.Duration.TotalSeconds:0.0}s — {result.Copied} copied, {result.Skipped} unchanged, {result.Failed} failed."); UpdateStats();
        }
        catch (OperationCanceledException) { SetStatus("Backup cancelled.", false); Log("Backup cancelled."); }
        catch (Exception ex) { SetStatus("Backup failed. Check the activity log.", false); Log($"Error: {ex.Message}"); }
        finally { backupCancellation.Dispose(); backupCancellation = null; runButton.Enabled = true; }
    }

    private void TryRunScheduledBackup()
    {
        if (settings.ScheduleEnabled && settings.LastBackupUtc is null || settings.ScheduleEnabled && DateTime.UtcNow - settings.LastBackupUtc!.Value >= TimeSpan.FromMinutes(settings.IntervalMinutes))
            _ = RunBackupAsync();
        UpdateStats();
    }

    private void SaveSettings()
    {
        settings.SourceFiles = fileList.Items.Cast<string>().ToList(); settings.DestinationFolder = destinationBox.Text; settings.IntervalMinutes = (int)intervalBox.Value; settings.ScheduleEnabled = scheduleToggle.Checked; settings.DarkMode = darkMode; settings.Save(); UpdateStats();
    }

    private void LoadSettings()
    {
        destinationBox.Text = settings.DestinationFolder;
        foreach (var file in settings.SourceFiles.Where(File.Exists)) fileList.Items.Add(file, true);
        intervalBox.Value = Math.Clamp(settings.IntervalMinutes, 1, 10080); scheduleToggle.Checked = settings.ScheduleEnabled; UpdateStats();
        Log("Ready. Add files and choose a backup schedule.");
    }

    private void UpdateStats()
    {
        if (filesCountLabel.IsDisposed) return;
        var count = fileList.Items.Count; filesCountLabel.Text = count == 1 ? "1 file" : $"{count} files";
        nextRunLabel.Text = settings.ScheduleEnabled ? $"In {settings.IntervalMinutes} min" : "Paused";
        nextRunLabel.ForeColor = settings.ScheduleEnabled ? Color.FromArgb(30, 166, 114) : TextPrimary;
        lastBackupLabel.Text = settings.LastBackupUtc is null ? "Not yet" : settings.LastBackupUtc.Value.ToLocalTime().ToString("dd MMM, HH:mm");
    }

    private void SetStatus(string text, bool success) 
    { 
        statusLabel.Text = text; 
        statusLabel.ForeColor = success ? Color.FromArgb(30, 166, 114) : Color.FromArgb(214, 78, 78); 
    }
    private void Log(string text) 
    { 
        activityLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  {text}{Environment.NewLine}"); 
        activityLog.ScrollToCaret(); 
    }
    private void ApplyTheme()
    {
        foreach (Control control in Controls) ApplyThemeTo(control); Invalidate(true); SaveSettings();
    }
    private void ApplyThemeTo(Control control)
    {
        if (control is Panel or TableLayoutPanel) control.BackColor = control == this ? Background : (control.BackColor == Color.Transparent ? Color.Transparent : Surface);
        if (control is Label label) label.ForeColor = label.ForeColor == Accent ? Accent : TextPrimary;
        if (control is TextBox box) { box.BackColor = SurfaceAlt; box.ForeColor = TextPrimary; }
        if (control is RichTextBox rich) { rich.BackColor = Surface; rich.ForeColor = TextSecondary; }
        if (control is CheckBox check) { check.BackColor = Surface; check.ForeColor = TextPrimary; }
        foreach (Control child in control.Controls) ApplyThemeTo(child);
    }

    private Label LabelOf(string text, int x, int y, int width, int height, float size, FontStyle style, Color colour) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, height), Font = new Font("Segoe UI", size, style), ForeColor = colour, BackColor = Color.Transparent };

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(284, 261);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        ResumeLayout(true);

    }

    private void StyleButton(Button button, Color back, Color fore) 
    { 
        button.FlatStyle = FlatStyle.Flat; 
        button.FlatAppearance.BorderSize = 0; 
        button.BackColor = back; 
        button.ForeColor = fore; 
        button.Font = new Font("Segoe UI", 9, FontStyle.Bold); 
        button.Cursor = Cursors.Hand; 
    }
}
