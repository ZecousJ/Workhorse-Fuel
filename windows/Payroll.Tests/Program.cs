using WorkhorseFuel;

var shanghai = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
var settings = WorkSettings.Default with { Salary = 21_000 };
var november = At(11, 1);
var weekdays = new PayrollMonth(november, settings, shanghai);

Check(weekdays.PlannedTicks == TimeSpan.FromHours(21 * 9).Ticks, "November has 21 weekdays.");
Check(weekdays.At(november).Earned == 0, "The month starts at zero.");
Check(weekdays.At(At(11, 2, 13, 30)).Earned == 500, "The first half shift earns half a day's pay.");
Check(weekdays.At(At(11, 2, 18)).Earned == weekdays.At(At(11, 3, 9)).Earned, "Earnings pause after work.");
Check(weekdays.At(At(11, 7, 12)).Phase == Phase.DayOff, "Saturday is off in a five-day week.");
Check(weekdays.At(At(11, 30, 18)).Earned == settings.Salary, "The final shift earns the full salary.");

var singleWeekend = new PayrollMonth(november, settings with { Schedule = Workweek.SixDays }, shanghai);
Check(singleWeekend.PlannedTicks == TimeSpan.FromHours(25 * 9).Ticks, "A six-day week includes every Saturday.");
var alternate = new PayrollMonth(november, settings with
{
    Schedule = Workweek.Alternating,
    WorkingSaturday = new DateOnly(2026, 11, 7)
}, shanghai);
Check(alternate.PlannedTicks == TimeSpan.FromHours(23 * 9).Ticks, "An alternating schedule includes two Saturdays.");
Check(alternate.At(At(11, 7, 12)).Phase == Phase.Earning, "The anchor Saturday is worked.");
Check(alternate.At(At(11, 14, 12)).Phase == Phase.DayOff, "The following Saturday is off.");
Check(alternate.At(At(11, 21, 12)).Phase == Phase.Earning, "The Saturday after that is worked.");
Check(PayrollMonth.SaturdayOfWeek(new DateOnly(2026, 11, 8)) == new DateOnly(2026, 11, 7),
    "The week starts on Monday.");

var overnightSettings = settings with { Schedule = Workweek.SixDays, StartMinute = 1320, EndMinute = 360 };
var overnight = new PayrollMonth(november, overnightSettings, shanghai);
var december = new PayrollMonth(At(12, 1), overnightSettings, shanghai);
Check(overnight.At(At(11, 1, 3)).Phase == Phase.Earning, "An overnight shift continues on Sunday.");
Check(overnight.At(At(11, 30, 23)).Earned < settings.Salary, "The month still has an hour left to earn.");
Check(december.At(At(12, 1)).Earned == 0, "Overnight earnings reset at the boundary.");
Check(december.At(At(12, 1, 3)) is { Phase: Phase.Earning, Earned: > 0 }, "The same shift earns in December.");

var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
var afterClockChange = new DateTimeOffset(2026, 3, 8, 3, 0, 0, TimeSpan.FromHours(-4));
var spring = new PayrollMonth(afterClockChange, overnightSettings, eastern);
var crossing = spring.At(afterClockChange).Active!;
Check(crossing.End - crossing.Start == TimeSpan.FromHours(7), "The spring-forward overnight shift has seven actual hours.");
Console.WriteLine("Payroll checks passed.");

static DateTimeOffset At(int month, int day, int hour = 0, int minute = 0) =>
    new(2026, month, day, hour, minute, 0, TimeSpan.FromHours(8));

static void Check(bool passed, string message)
{
    if (!passed) throw new InvalidOperationException(message);
}
