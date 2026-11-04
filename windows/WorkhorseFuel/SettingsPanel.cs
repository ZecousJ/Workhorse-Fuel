using System.Globalization;

namespace WorkhorseFuel;

internal static class ScheduleText
{
    public static string Name(Workweek schedule) => schedule switch
    {
        Workweek.FiveDays => "双休",
        Workweek.Alternating => "大小周",
        _ => "单休"
    };
}

internal sealed class SettingsPanel : UserControl
{
    private static readonly Color Muted = Color.FromArgb(190, 187, 193);
    private static readonly Color Field = Color.FromArgb(45, 43, 49);
    private static readonly Color Accent = Color.FromArgb(255, 88, 73);
    private readonly TextBox salary = new();
    private readonly MaskedTextBox start = new("00:00");
    private readonly MaskedTextBox end = new("00:00");
    private readonly CheckBox workingSaturday = new();
    private readonly Label hint = new();
    private readonly RadioButton[] schedules = new RadioButton[3];
    private readonly DateOnly saturday;
    private readonly WorkSettings previous;
    private readonly Label scheduleHint = new();

    public event Action<WorkSettings>? Saved;
    public event Action? Cancelled;

    public SettingsPanel(WorkSettings settings)
    {
        SuspendLayout();
        previous = settings;
        saturday = PayrollMonth.SaturdayOfWeek(DateOnly.FromDateTime(DateTime.Today));
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Dock = DockStyle.Fill;
        Font = new Font("Microsoft YaHei UI", 10);

        var content = Row(1);
        content.Padding = new Padding(24, 18, 24, 14);
        content.MinimumSize = new Size(366, 0);

        var header = Row(3);
        header.ColumnStyles.Clear();
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        header.Margin = new Padding(0, 0, 0, 20);
        var back = Button("‹", () => Cancelled?.Invoke());
        back.AccessibleName = "返回计数器";
        back.TabStop = false;
        back.Font = new Font("Microsoft YaHei UI", 18);
        back.Padding = new Padding(4, 0, 4, 0);
        header.Controls.Add(back, 0, 0);
        var title = Label("工资与排班", 17, Color.White, FontStyle.Bold);
        title.Anchor = AnchorStyles.None;
        header.Controls.Add(title, 1, 0);
        content.Controls.Add(header);

        var pay = Row(2);
        pay.ColumnStyles.Clear();
        pay.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pay.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var payLabel = Label("月薪", 11, Color.White);
        payLabel.Anchor = AnchorStyles.Left;
        pay.Controls.Add(payLabel, 0, 0);
        salary.Text = settings.Salary == 0 ? "" : settings.Salary.ToString("0.00", CultureInfo.InvariantCulture);
        salary.PlaceholderText = "例如 12000";
        salary.AccessibleName = "月薪（元）";
        salary.TextAlign = HorizontalAlignment.Right;
        salary.Width = 140;
        pay.Controls.Add(Input(salary, "¥"), 1, 0);
        content.Controls.Add(pay);
        content.Controls.Add(Rule());

        var times = Row(2);
        times.ColumnStyles.Clear();
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        start.Text = FormatMinute(settings.StartMinute);
        end.Text = FormatMinute(settings.EndMinute);
        start.AccessibleName = "上班时间";
        end.AccessibleName = "下班时间";
        start.Width = end.Width = 64;
        var from = Input(start, "上班");
        from.Margin = new Padding(0, 0, 6, 0);
        var until = Input(end, "下班");
        until.Margin = new Padding(6, 0, 0, 0);
        times.Controls.Add(from, 0, 0);
        times.Controls.Add(until, 1, 0);
        content.Controls.Add(times);
        content.Controls.Add(Rule());

        var segment = Row(3);
        foreach (var schedule in Enum.GetValues<Workweek>())
        {
            var choice = new RadioButton
            {
                Text = ScheduleText.Name(schedule), Appearance = Appearance.Button,
                AutoSize = true, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 5, 8, 5), Margin = new Padding(2, 0, 2, 0),
                BackColor = Field, ForeColor = Color.White
            };
            choice.FlatAppearance.BorderSize = 0;
            choice.FlatAppearance.CheckedBackColor = Accent;
            choice.Checked = schedule == settings.Schedule;
            schedules[(int)schedule] = choice;
            segment.Controls.Add(choice);
        }
        content.Controls.Add(segment);
        scheduleHint.AutoSize = true;
        scheduleHint.Font = new Font("Microsoft YaHei UI", 9);
        scheduleHint.ForeColor = Muted;
        scheduleHint.Margin = new Padding(0, 8, 0, 6);
        content.Controls.Add(scheduleHint);
        workingSaturday.AutoSize = true;
        workingSaturday.Text = $"本周六（{saturday.Month}/{saturday.Day}）上班";
        workingSaturday.Checked = PayrollMonth.IsWorkingSaturday(saturday, settings.WorkingSaturday);
        workingSaturday.Margin = new Padding(0, 4, 0, 8);
        content.Controls.Add(workingSaturday);

        hint.AutoSize = true;
        hint.MaximumSize = new Size(318, 0);
        hint.Font = new Font("Microsoft YaHei UI", 9);
        hint.ForeColor = Muted;
        hint.Margin = new Padding(0, 18, 0, 0);
        content.Controls.Add(hint);
        content.Controls.Add(Rule());
        var footer = Row(2);
        var note = Label("设置保存在本机", 9, Muted);
        note.Anchor = AnchorStyles.Left;
        footer.Controls.Add(note, 0, 0);
        var save = Button("保存设置", Save);
        save.BackColor = Accent;
        save.Anchor = AnchorStyles.Right;
        save.Padding = new Padding(14, 6, 14, 6);
        footer.Controls.Add(save, 1, 0);
        content.Controls.Add(footer);
        Controls.Add(content);

        start.TextChanged += (_, _) => UpdateHint();
        end.TextChanged += (_, _) => UpdateHint();
        foreach (var choice in schedules) choice.CheckedChanged += (_, _) => UpdateHint();
        UpdateHint();
        ResumeLayout(true);
    }

    private Workweek SelectedSchedule => (Workweek)Array.FindIndex(schedules, choice => choice.Checked);

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Escape) { Cancelled?.Invoke(); return true; }
        if (keyData == Keys.Enter) { Save(); return true; }
        return base.ProcessCmdKey(ref message, keyData);
    }

    private static TableLayoutPanel Row(int columns)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = columns, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill, Margin = Padding.Empty
        };
        for (var index = 0; index < columns; index++)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
        return row;
    }

    private static Label Label(string text, int size, Color color, FontStyle style = FontStyle.Regular) =>
        new() { Text = text, AutoSize = true, ForeColor = color, Font = new Font("Microsoft YaHei UI", size, style), Margin = Padding.Empty };

    private static Button Button(string text, Action action)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Black, Margin = Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private static Control Input(TextBoxBase input, string caption)
    {
        input.BorderStyle = BorderStyle.None;
        input.BackColor = Field;
        input.ForeColor = Color.White;
        input.Font = new Font("Microsoft YaHei UI", 11);
        void FitText()
        {
            var sample = input is MaskedTextBox ? "00:00" : "99999999.99";
            input.MinimumSize = new Size(TextRenderer.MeasureText(sample, input.Font).Width + 8, 0);
        }
        input.FontChanged += (_, _) => FitText();
        FitText();
        input.Anchor = AnchorStyles.Right;
        input.Margin = new Padding(8, 0, 0, 0);
        var field = Row(2);
        field.BackColor = Field;
        field.Padding = new Padding(10, 9, 10, 9);
        field.ColumnStyles.Clear();
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        field.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var label = Label(caption, 10, Muted);
        label.Anchor = AnchorStyles.Left;
        field.Controls.Add(label, 0, 0);
        field.Controls.Add(input, 1, 0);
        return field;
    }

    private static Panel Rule() => new()
    {
        Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(66, 63, 72),
        Margin = new Padding(0, 16, 0, 16)
    };

    private static string FormatMinute(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static bool ParseTime(MaskedTextBox input, out TimeOnly time) =>
        TimeOnly.TryParseExact(input.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    private void UpdateHint()
    {
        workingSaturday.Visible = SelectedSchedule == Workweek.Alternating;
        scheduleHint.Text = SelectedSchedule switch
        {
            Workweek.FiveDays => "周六、周日休息",
            Workweek.Alternating => "周六隔周上班，周日休息",
            _ => "周日休息"
        };
        hint.ForeColor = Muted;
        hint.Text = ParseTime(start, out var from) && ParseTime(end, out var until) && until < from
            ? "下班时间为次日。修改后会重新计算本月金额。"
            : "修改设置会重新计算本月金额。";
    }

    private void Save()
    {
        if (!decimal.TryParse(salary.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var pay) ||
            pay <= 0 || pay > WorkSettings.MaxSalary || decimal.Round(pay, 2) != pay)
        {
            ShowError("请输入大于 0 的月薪，最多两位小数。");
            return;
        }
        if (!ParseTime(start, out var from) || !ParseTime(end, out var until) || from == until)
        {
            ShowError("请输入有效且不同的上下班时间，如 09:00。");
            return;
        }
        var anchor = SelectedSchedule == Workweek.Alternating
            ? (workingSaturday.Checked ? saturday : saturday.AddDays(7))
            : previous.WorkingSaturday;
        var updated = new WorkSettings(pay, from.Hour * 60 + from.Minute,
            until.Hour * 60 + until.Minute, SelectedSchedule, anchor);
        try { SettingsStore.Save(updated); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ShowError("无法保存设置，请检查本地文件夹是否可写。");
            return;
        }
        Saved?.Invoke(updated);
    }

    private void ShowError(string text)
    {
        hint.ForeColor = Color.FromArgb(255, 175, 165);
        hint.Text = text;
    }
}
