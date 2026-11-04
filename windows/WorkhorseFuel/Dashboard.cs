using System.Globalization;

namespace WorkhorseFuel;

internal sealed class Dashboard : Form
{
    private static readonly Color Surface = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621)
        ? Color.Black : Color.FromArgb(35, 34, 38);
    private static readonly Color Ink = Color.FromArgb(249, 247, 247);
    private static readonly Color Muted = Color.FromArgb(191, 187, 195);
    private static readonly Color Divider = Color.FromArgb(66, 63, 72);
    private static readonly Color Accent = Color.FromArgb(255, 88, 73);
    private static readonly CultureInfo Chinese = CultureInfo.GetCultureInfo("zh-CN");

    private readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };
    private readonly Font largeAmount = new("Segoe UI", 38, FontStyle.Bold);
    private readonly Font compactAmount = new("Segoe UI", 29, FontStyle.Bold);
    private readonly Label month;
    private readonly RollingAmount amount;
    private readonly Label state;
    private readonly Label percent;
    private readonly Label schedule;
    private readonly Label hours;
    private readonly Label detailTitle;
    private readonly Label detail;
    private readonly Panel track;
    private readonly Panel fill;
    private WorkSettings settings;
    private PayrollMonth? ledger;
    private Rectangle anchor;
    private Rectangle workArea;

    private readonly Control counter;
    private SettingsPanel? editor;
    public event Action<WorkSettings>? SettingsChanged;

    public Dashboard(WorkSettings settings)
    {
        SuspendLayout();
        this.settings = settings;
        Text = "牛马兴奋剂";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Surface;
        ForeColor = Ink;
        KeyPreview = true;
        DoubleBuffered = true;

        var content = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 12),
            MinimumSize = new Size(366, 0)
        };
        month = MakeLabel("", 20, Ink, bold: true);
        month.Anchor = AnchorStyles.None;
        month.Margin = new Padding(0, 0, 0, 20);
        content.Controls.Add(month);

        var caption = MakeLabel("本月已赚", 10, Muted);
        caption.Anchor = AnchorStyles.None;
        content.Controls.Add(caption);
        amount = new RollingAmount { ForeColor = Ink, Margin = Padding.Empty };
        amount.Font = largeAmount;
        amount.Anchor = AnchorStyles.None;
        amount.Margin = new Padding(0, 2, 0, 22);
        content.Controls.Add(amount);

        var statusRow = MakeRow(2);
        state = MakeLabel("", 10, Ink, bold: true);
        percent = MakeLabel("", 10, Muted);
        percent.Anchor = AnchorStyles.Right;
        percent.AccessibleName = "本月计薪进度";
        statusRow.Controls.Add(state, 0, 0);
        statusRow.Controls.Add(percent, 1, 0);
        content.Controls.Add(statusRow);
        track = new Panel { Dock = DockStyle.Fill, Height = 4, Margin = new Padding(0, 6, 0, 18) };
        track.BackColor = Divider;
        fill = new Panel { BackColor = Accent };
        track.Controls.Add(fill);
        content.Controls.Add(track);

        var details = MakeRow(2);
        details.Controls.Add(MakeLabel("工作时段", 9, Muted), 0, 0);
        detailTitle = MakeLabel("", 9, Muted);
        details.Controls.Add(detailTitle, 1, 0);
        hours = MakeLabel("", 11, Ink);
        hours.Margin = new Padding(0, 4, 18, 0);
        detail = MakeLabel("", 11, Ink);
        detail.Margin = new Padding(0, 4, 0, 0);
        details.Controls.Add(hours, 0, 1);
        details.Controls.Add(detail, 1, 1);
        content.Controls.Add(details);
        content.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill, Height = 1, BackColor = Divider, Margin = new Padding(0, 18, 0, 8)
        });

        var footer = MakeRow(3);
        footer.ColumnStyles.Clear();
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(MakeButton("设置", "打开设置", OpenSettings), 0, 0);
        schedule = MakeLabel("", 9, Muted);
        schedule.Anchor = AnchorStyles.Left;
        schedule.Margin = new Padding(10, 0, 10, 0);
        footer.Controls.Add(schedule, 1, 0);
        footer.Controls.Add(MakeButton("×", "关闭面板", Hide), 2, 0);
        content.Controls.Add(footer);
        counter = content;
        Controls.Add(counter);
        RefreshSnapshot();
        ResumeLayout(true);

        timer.Tick += (_, _) => RefreshSnapshot();
        VisibleChanged += (_, _) => timer.Enabled = Visible && editor is null;
        Deactivate += (_, _) => { if (editor is null) Hide(); };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape) return;
            if (editor is not null) ReturnToCounter();
            else Hide();
            e.Handled = true;
        };
    }

    public void ShowNear(Point trayPoint)
    {
        (workArea, anchor) = WindowsShell.PlacementArea(trayPoint);
        RefreshSnapshot();
        PositionAtAnchor();
        Show();
        PositionAtAnchor();
        Activate();
    }

    public void OpenSettings()
    {
        if (editor is not null) return;
        timer.Stop();
        editor = new SettingsPanel(settings);
        editor.Cancelled += ReturnToCounter;
        editor.Saved += updated =>
        {
            UpdateSettings(updated);
            SettingsChanged?.Invoke(updated);
            ReturnToCounter();
        };
        SuspendLayout();
        Controls.Add(editor);
        counter.Visible = false;
        editor.BringToFront();
        editor.SelectNextControl(null, true, true, true, false);
        ResumeLayout(true);
        if (Visible) PositionAtAnchor();
    }

    private void ReturnToCounter()
    {
        if (editor is null) return;
        SuspendLayout();
        counter.Visible = true;
        counter.BringToFront();
        counter.SelectNextControl(null, true, true, true, false);
        Controls.Remove(editor);
        editor.Dispose();
        editor = null;
        ResumeLayout(true);
        RefreshSnapshot();
        timer.Enabled = Visible;
        if (Visible) PositionAtAnchor();
    }

    public void UpdateSettings(WorkSettings updated)
    {
        settings = updated;
        ledger = null;
        RefreshSnapshot();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (Visible) BeginInvoke(PositionAtAnchor);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            timer.Dispose();
            if (counter.Parent is null) counter.Dispose();
            largeAmount.Dispose();
            compactAmount.Dispose();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!WindowsShell.ApplyBackdrop(Handle))
            BackColor = Color.FromArgb(35, 34, 38);
    }

    private void PositionAtAnchor()
    {
        Location = WindowsShell.PlacePopup(Size, workArea, anchor, LogicalToDeviceUnits(10));
    }

    private void RefreshSnapshot()
    {
        var now = DateTimeOffset.Now;
        var zone = TimeZoneInfo.Local;
        if (ledger is null || !ledger.Contains(now) || !ledger.Zone.Equals(zone))
            ledger = new PayrollMonth(now, settings, zone);
        var snapshot = ledger.At(now);
        var configured = settings.Salary > 0;
        var earning = configured && snapshot.Phase == Phase.Earning;
        month.Text = $"{ledger.Month.Year} / {ledger.Month.Month:00}";
        var value = "¥ " + snapshot.Earned.ToString("N2", Chinese);
        if (amount.Text != value)
        {
            amount.Font = value.Length > 13 ? compactAmount : largeAmount;
            amount.Text = value;
            amount.AccessibleName = $"本月已赚 {value}";
        }
        state.Text = !configured ? "●  待设置" : snapshot.Phase switch
        {
            Phase.Earning => "●  计薪中",
            Phase.BeforeWork => "●  待上班",
            Phase.AfterWork => "●  已下班",
            _ => "●  休息中"
        };
        state.ForeColor = earning ? Accent : Muted;
        percent.Text = configured ? (snapshot.Fraction * 100).ToString("0.0", Chinese) + "%" : "—";
        fill.BackColor = earning ? Accent : Muted;
        fill.Size = new Size(configured ? (int)Math.Round(track.Width * snapshot.Fraction) : 0, track.Height);
        hours.Text = configured
            ? $"{FormatTime(settings.StartMinute)} – {(settings.EndMinute < settings.StartMinute ? "次日 " : "")}{FormatTime(settings.EndMinute)}"
            : "—";
        detailTitle.Text = "下次上班";
        var detailText = "—";
        if (configured && snapshot.Active is { } active)
        {
            detailTitle.Text = "距下班";
            var remaining = TimeSpan.FromSeconds(Math.Ceiling((active.End - now).TotalSeconds));
            detailText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        else if (configured && snapshot.Next is { } next)
        {
            var today = ledger.LocalDate(now);
            var start = TimeZoneInfo.ConvertTime(next.Start, zone);
            var day = DateOnly.FromDateTime(start.DateTime);
            var dayText = day == today ? "今天" : day == today.AddDays(1) ? "明天" : $"{day.Month}月{day.Day}日";
            detailText = $"{dayText} {start:HH:mm}";
        }

        detail.Text = detailText;
        schedule.Text = configured
            ? $"{ScheduleText.Name(settings.Schedule)}  ·  {TimeZoneInfo.ConvertTime(now, zone).ToString("M月d日 ddd", Chinese)}"
            : "设置月薪和排班后开始计薪";
    }

    private static string FormatTime(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static TableLayoutPanel MakeRow(int columns)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = columns, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill, Margin = Padding.Empty
        };
        for (var i = 0; i < columns; i++)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
        return row;
    }

    private static Label MakeLabel(string text, int size, Color color, bool bold = false) => new()
    {
        Text = text, AutoSize = true, Margin = Padding.Empty,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
    };

    private static Button MakeButton(string text, string label, Action action)
    {
        var button = new Button
        {
            Text = text, AccessibleName = label, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(7, 4, 7, 4),
            Margin = Padding.Empty, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10), BackColor = Surface, ForeColor = Ink
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Divider;
        button.Click += (_, _) => action();
        return button;
    }
}
