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

internal sealed class SettingsDialog : Form
{
    private readonly NumericUpDown salary = new();
    private readonly DateTimePicker start = new();
    private readonly DateTimePicker end = new();
    private readonly ComboBox schedule = new();
    private readonly CheckBox workingSaturday = new();
    private readonly Label hint = new();
    private readonly DateOnly saturday;
    private readonly WorkSettings previous;

    public WorkSettings? Result { get; private set; }

    public SettingsDialog(WorkSettings settings)
    {
        previous = settings;
        saturday = PayrollMonth.SaturdayOfWeek(DateOnly.FromDateTime(DateTime.Today));
        Text = "工资与排班";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(366, 294);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        AddLabel("月薪（元）", 20, 21, 125);
        salary.Bounds = new Rectangle(148, 22, 190, 25);
        salary.DecimalPlaces = 2;
        salary.Maximum = WorkSettings.MaxSalary;
        salary.Value = settings.Salary;
        salary.AccessibleName = "月薪（元）";
        Controls.Add(salary);

        AddLabel("上班时间", 20, 61, 125);
        AddTime(start, "上班时间", 61, settings.StartMinute);
        AddLabel("下班时间", 20, 100, 125);
        AddTime(end, "下班时间", 100, settings.EndMinute);
        AddLabel("排班类型", 20, 139, 125);
        schedule.Bounds = new Rectangle(148, 139, 190, 25);
        schedule.AccessibleName = "排班类型";
        schedule.DropDownStyle = ComboBoxStyle.DropDownList;
        schedule.Items.AddRange(Enum.GetValues<Workweek>().Select(ScheduleText.Name).ToArray());
        schedule.SelectedIndex = (int)settings.Schedule;
        Controls.Add(schedule);

        workingSaturday.Bounds = new Rectangle(148, 170, 190, 24);
        workingSaturday.Text = $"本周六（{saturday.Month}/{saturday.Day}）上班";
        workingSaturday.Checked = PayrollMonth.IsWorkingSaturday(saturday, settings.WorkingSaturday);
        Controls.Add(workingSaturday);
        hint.Bounds = new Rectangle(20, 207, 320, 24);
        hint.ForeColor = SystemColors.GrayText;
        Controls.Add(hint);

        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(166, 249, 80, 29) };
        var save = new Button { Text = "保存", Bounds = new Rectangle(258, 249, 80, 29) };
        save.Click += (_, _) => Save();
        Controls.AddRange([cancel, save]);
        CancelButton = cancel;
        AcceptButton = save;

        start.ValueChanged += (_, _) => UpdateHint();
        end.ValueChanged += (_, _) => UpdateHint();
        schedule.SelectedIndexChanged += (_, _) => UpdateHint();
        UpdateHint();
    }

    private void AddLabel(string text, int x, int y, int width)
    {
        Controls.Add(new Label
        {
            Text = text, Bounds = new Rectangle(x, y, width, 25), TextAlign = ContentAlignment.MiddleLeft
        });
    }

    private void AddTime(DateTimePicker picker, string label, int y, int minute)
    {
        picker.Bounds = new Rectangle(148, y, 190, 25);
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = DateTime.Today.AddMinutes(minute);
        picker.AccessibleName = label;
        Controls.Add(picker);
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
        Result = updated;
        DialogResult = DialogResult.OK;
    }

    private void ShowError(string text)
    {
        hint.ForeColor = Color.Firebrick;
        hint.Text = text;
    }
}
