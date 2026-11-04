namespace WorkhorseFuel;

internal enum Workweek { FiveDays, Alternating, SixDays }
internal enum Phase { Earning, BeforeWork, AfterWork, DayOff }

internal sealed record WorkSettings(
    decimal Salary, int StartMinute, int EndMinute, Workweek Schedule, DateOnly WorkingSaturday)
{
    public const decimal MaxSalary = 99_999_999.99m;
    public static WorkSettings Default => new(0, 540, 1080, Workweek.FiveDays, new DateOnly(2024, 1, 6));
    public bool IsValid => Salary is > 0 and <= MaxSalary && decimal.Round(Salary, 2) == Salary
        && StartMinute is >= 0 and < 1440 && EndMinute is >= 0 and < 1440 && StartMinute != EndMinute
        && Enum.IsDefined(Schedule) && WorkingSaturday.DayOfWeek == DayOfWeek.Saturday;
}

internal sealed record Shift(DateTimeOffset Start, DateTimeOffset End)
{
    public long TicksWithin(DateTimeOffset lower, DateTimeOffset upper)
    {
        var start = Start > lower ? Start : lower;
        var end = End < upper ? End : upper;
        return end > start ? (end - start).Ticks : 0;
    }
}

internal sealed record Snapshot(decimal Earned, double Fraction, Phase Phase, Shift? Active, Shift? Next);

internal sealed class PayrollMonth
{
    private readonly WorkSettings settings;
    private readonly List<Shift> shifts = [];
    private readonly DateTimeOffset start;
    private readonly DateTimeOffset end;

    public DateOnly Month { get; }
    public TimeZoneInfo Zone { get; }
    public long PlannedTicks { get; }

    public PayrollMonth(DateTimeOffset now, WorkSettings settings, TimeZoneInfo zone)
    {
        this.settings = settings;
        Zone = zone;
        var local = LocalDate(now);
        Month = new DateOnly(local.Year, local.Month, 1);
        var nextMonth = Month.AddMonths(1);
        start = At(Month, 0, zone);
        end = At(nextMonth, 0, zone);

        // Include overnight hours at the month boundaries and the next shift after month-end.
        for (var day = Month.AddDays(-1); day < nextMonth.AddDays(7); day = day.AddDays(1))
        {
            if (!IsWorkday(day)) continue;
            var endDay = settings.EndMinute < settings.StartMinute ? day.AddDays(1) : day;
            var shift = new Shift(At(day, settings.StartMinute, zone), At(endDay, settings.EndMinute, zone));
            if (shift.End > shift.Start) shifts.Add(shift);
        }
        PlannedTicks = shifts.Sum(shift => shift.TicksWithin(start, end));
    }

    public bool Contains(DateTimeOffset now) => now >= start && now < end;

    public DateOnly LocalDate(DateTimeOffset date) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(date, Zone).DateTime);

    public Snapshot At(DateTimeOffset now)
    {
        var cutoff = now < end ? now : end;
        var workedTicks = shifts.Sum(shift => shift.TicksWithin(start, cutoff));
        var earned = PlannedTicks == 0 ? 0 : decimal.Floor(settings.Salary * 100 * workedTicks / PlannedTicks) / 100;
        var fraction = PlannedTicks == 0 ? 0 : (double)workedTicks / PlannedTicks;
        var active = shifts.Find(shift => shift.Start <= now && now < shift.End);
        var next = shifts.Find(shift => shift.Start > now);
        var today = LocalDate(now);
        var phase = Phase.DayOff;
        if (active is not null)
            phase = Phase.Earning;
        else if (next is not null && LocalDate(next.Start) == today)
            phase = Phase.BeforeWork;
        else if (shifts.Exists(shift => LocalDate(shift.Start) == today || LocalDate(shift.End) == today))
            phase = Phase.AfterWork;
        return new Snapshot(earned, fraction, phase, active, next);
    }

    public static DateOnly SaturdayOfWeek(DateOnly day) => day.AddDays(5 - ((int)day.DayOfWeek + 6) % 7);

    public static bool IsWorkingSaturday(DateOnly day, DateOnly anchor) =>
        (day.DayNumber - anchor.DayNumber) % 14 == 0;

    private bool IsWorkday(DateOnly day) => day.DayOfWeek switch
    {
        DayOfWeek.Sunday => false,
        DayOfWeek.Saturday => settings.Schedule == Workweek.SixDays ||
            (settings.Schedule == Workweek.Alternating && IsWorkingSaturday(day, settings.WorkingSaturday)),
        _ => true
    };

    private static DateTimeOffset At(DateOnly day, int minute, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue).AddMinutes(minute);
        if (zone.IsInvalidTime(local))
            local += zone.GetUtcOffset(local.AddDays(1)) - zone.GetUtcOffset(local.AddDays(-1));
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}
