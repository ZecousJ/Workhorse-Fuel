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
    private readonly NumericUpDown salary = new();
    private readonly DateTimePicker start = new();
    private readonly DateTimePicker end = new();
    private readonly ComboBox schedule = new();
    private readonly CheckBox workingSaturday = new();
    private readonly Label hint = new();
    private readonly DateOnly saturday;
    private readonly WorkSettings previous;

    public event Action<WorkSettings>? Saved;
    public event Action? Cancelled;

    public SettingsPanel(WorkSettings settings)
    {
        SuspendLayout();
        previous = settings;
        saturday = PayrollMonth.SaturdayOfWeek(DateOnly.FromDateTime(DateTime.Today));
        Text = "工资与排班";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 10);

        var content = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill,
            ColumnCount = 2, Padding = new Padding(20), MinimumSize = new Size(380, 0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var back = new Button { Text = "‹", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat };
        back.AccessibleName = "返回计数器";
        back.FlatAppearance.BorderSize = 0;
        back.Click += (_, _) => Cancelled?.Invoke();
        content.Controls.Add(back, 0, 0);
        content.Controls.Add(new Label { Text = "工资与排班", AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold), Margin = new Padding(0, 0, 0, 20),
            Anchor = AnchorStyles.Left }, 1, 0);
        salary.DecimalPlaces = 2;
        salary.Maximum = WorkSettings.MaxSalary;
        salary.Value = settings.Salary;
        salary.AccessibleName = "月薪（元）";
        AddField(content, "月薪（元）", salary, 1);
        PrepareTime(start, "上班时间", settings.StartMinute);
        PrepareTime(end, "下班时间", settings.EndMinute);
        AddField(content, "上班时间", start, 2);
        AddField(content, "下班时间", end, 3);
        schedule.AccessibleName = "排班类型";
        schedule.DropDownStyle = ComboBoxStyle.DropDownList;
        schedule.Items.AddRange(Enum.GetValues<Workweek>().Select(ScheduleText.Name).ToArray());
        schedule.SelectedIndex = (int)settings.Schedule;
        AddField(content, "排班类型", schedule, 4);

        workingSaturday.AutoSize = true;
        workingSaturday.Text = $"本周六（{saturday.Month}/{saturday.Day}）上班";
        workingSaturday.Checked = PayrollMonth.IsWorkingSaturday(saturday, settings.WorkingSaturday);
        workingSaturday.Margin = new Padding(0, 0, 0, 12);
        content.Controls.Add(workingSaturday, 1, 5);
        hint.AutoSize = true;
        hint.Margin = new Padding(0, 8, 0, 18);
        hint.ForeColor = SystemColors.GrayText;
        content.Controls.Add(hint, 0, 6);
        content.SetColumnSpan(hint, 2);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        var cancel = new Button { Text = "返回", AutoSize = true };
        cancel.Click += (_, _) => Cancelled?.Invoke();
        var save = new Button { Text = "保存", AutoSize = true };
        save.Click += (_, _) => Save();
        buttons.Controls.AddRange([save, cancel]);
        content.Controls.Add(buttons, 0, 7);
        content.SetColumnSpan(buttons, 2);
        Controls.Add(content);

        ResumeLayout(true);

        start.ValueChanged += (_, _) => UpdateHint();
        end.ValueChanged += (_, _) => UpdateHint();
        schedule.SelectedIndexChanged += (_, _) => UpdateHint();
        UpdateHint();
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Escape) { Cancelled?.Invoke(); return true; }
        if (keyData == Keys.Enter) { Save(); return true; }
        return base.ProcessCmdKey(ref message, keyData);
    }

    private static void AddField(TableLayoutPanel parent, string text, Control input, int row)
    {
        parent.Controls.Add(new Label
        {
            Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 22, 14)
        }, 0, row);
        input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        input.MinimumSize = new Size(180, 0);
        input.Margin = new Padding(0, 0, 0, 14);
        parent.Controls.Add(input, 1, row);
    }

    private static void PrepareTime(DateTimePicker picker, string label, int minute)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = DateTime.Today.AddMinutes(minute);
        picker.AccessibleName = label;
    }

    private static int Minute(DateTimePicker picker) => picker.Value.Hour * 60 + picker.Value.Minute;

    private void UpdateHint()
    {
        workingSaturday.Visible = schedule.SelectedIndex == (int)Workweek.Alternating;
        hint.ForeColor = SystemColors.GrayText;
        hint.Text = Minute(end) < Minute(start) ? "下班时间为次日。修改后会重新计算本月金额。"
            : "修改设置会重新计算本月金额。";
    }

    private void Save()
    {
        var workweek = (Workweek)schedule.SelectedIndex;
        var anchor = workweek == Workweek.Alternating
            ? (workingSaturday.Checked ? saturday : saturday.AddDays(7))
            : previous.WorkingSaturday;
        var updated = new WorkSettings(salary.Value, Minute(start), Minute(end), workweek, anchor);
        if (!updated.IsValid)
        {
            ShowError(updated.StartMinute == updated.EndMinute
                ? "上班和下班时间不能相同。" : "请输入大于 0 的月薪，最多两位小数。");
            return;
        }
        try
        {
            SettingsStore.Save(updated);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ShowError("无法保存设置，请检查本地文件夹是否可写。");
            return;
        }
        Saved?.Invoke(updated);
    }

    private void ShowError(string text)
    {
        hint.ForeColor = Color.Firebrick;
        hint.Text = text;
    }
}
