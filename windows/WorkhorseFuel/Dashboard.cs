using System.Globalization;

namespace WorkhorseFuel;

internal sealed class Dashboard : Form
{
    private static readonly Color Surface = Color.FromArgb(35, 34, 38);
    private static readonly Color Ink = Color.FromArgb(249, 247, 247);
    private static readonly Color Muted = Color.FromArgb(191, 187, 195);
    private static readonly Color Divider = Color.FromArgb(66, 63, 72);
    private static readonly Color Accent = Color.FromArgb(255, 88, 73);
    private static readonly CultureInfo Chinese = CultureInfo.GetCultureInfo("zh-CN");

    private readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };
    private readonly Font largeAmount = new("Segoe UI", 38, FontStyle.Bold);
    private readonly Font compactAmount = new("Segoe UI", 29, FontStyle.Bold);
    private readonly Label month;
    private readonly Label amount;
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
    private Point anchor;

    private bool editing;

    public Dashboard(WorkSettings settings, Action openSettings)
    {
        this.settings = settings;
        Text = "牛马兴奋剂";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(366, 318);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Surface;
        ForeColor = Ink;
        KeyPreview = true;

        month = AddLabel("", 20, 15, 326, 34, 19, Ink, center: true, bold: true);
        AddLabel("本月已赚", 20, 61, 326, 21, 10, Muted, center: true);
        amount = AddLabel("", 14, 82, 338, 70, 10, Ink, center: true);
        amount.Font = largeAmount;
        state = AddLabel("", 21, 163, 160, 23, 10, Ink, bold: true);
        percent = AddLabel("", 250, 163, 94, 23, 10, Muted);
        percent.TextAlign = ContentAlignment.MiddleRight;
        percent.AccessibleName = "本月计薪进度";
        track = new Panel { Bounds = new Rectangle(22, 191, 322, 4), BackColor = Divider };
        fill = new Panel { Bounds = Rectangle.Empty, BackColor = Accent };
        track.Controls.Add(fill);
        Controls.Add(track);

        AddLabel("工作时段", 21, 210, 156, 20, 9, Muted);
        hours = AddLabel("", 21, 231, 165, 23, 11, Ink);
        detailTitle = AddLabel("", 200, 210, 144, 20, 9, Muted);
        detail = AddLabel("", 200, 231, 144, 23, 11, Ink);
        Controls.Add(new Panel { Bounds = new Rectangle(22, 268, 322, 1), BackColor = Divider });
        AddButton("设置", 21, 278, 48, "打开设置", openSettings);
        schedule = AddLabel("", 77, 278, 234, 29, 9, Muted);
        AddButton("×", 314, 278, 30, "关闭面板", Hide);

        timer.Tick += (_, _) => RefreshSnapshot();
        VisibleChanged += (_, _) => timer.Enabled = Visible;
        Deactivate += (_, _) => { if (!editing) Hide(); };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape) return;
            Hide();
            e.Handled = true;
        };
    }

    public void ShowNear(Point trayPoint)
    {
        anchor = trayPoint;
        RefreshSnapshot();
        PositionAtAnchor();
        Show();
        PositionAtAnchor();
        Activate();
    }

    public DialogResult EditSettings(SettingsDialog dialog)
    {
        editing = true;
        try { return dialog.ShowDialog(this); }
        finally { editing = false; }
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
            largeAmount.Dispose();
            compactAmount.Dispose();
        }
    }

    private void PositionAtAnchor()
    {
        var area = Screen.FromPoint(anchor).WorkingArea;
        var margin = LogicalToDeviceUnits(8);
        var above = anchor.Y >= area.Top + area.Height / 2;
        var desiredY = above ? Math.Min(anchor.Y, area.Bottom) - Height - margin : Math.Max(anchor.Y, area.Top) + margin;
        Location = new Point(
            ClampToArea(anchor.X - Width / 2, area.Left + margin, area.Right - Width - margin),
            ClampToArea(desiredY, area.Top + margin, area.Bottom - Height - margin));
    }

    private static int ClampToArea(int position, int minimum, int maximum) =>
        Math.Clamp(position, minimum, Math.Max(minimum, maximum));

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

    private Label AddLabel(string text, int x, int y, int width, int height, int size, Color color,
        bool center = false, bool bold = false)
    {
        var label = new Label
        {
            Text = text,
            Bounds = new Rectangle(x, y, width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color,
            AutoEllipsis = true,
            TextAlign = center ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft
        };
        Controls.Add(label);
        return label;
    }

    private void AddButton(string text, int x, int y, int width, string label, Action action)
    {
        var button = new Button
        {
            Text = text,
            AccessibleName = label,
            Bounds = new Rectangle(x, y, width, 29),
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface,
            ForeColor = Ink
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Divider;
        button.Click += (_, _) => action();
        Controls.Add(button);
    }
}
