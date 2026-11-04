import Foundation

var calendar = Calendar(identifier: .gregorian)
calendar.timeZone = TimeZone(identifier: "Asia/Shanghai")!

@MainActor
func at(_ year: Int, _ month: Int, _ day: Int, _ hour: Int = 0, _ minute: Int = 0) -> Date {
    CivilDay(year: year, month: month, day: day).date(in: calendar, minute: hour * 60 + minute)
}

func check(_ condition: @autoclosure () -> Bool, _ message: String) {
    guard condition() else { fatalError(message) }
}

var settings = WorkSettings(salaryCents: 2_100_000)
let first = at(2026, 11, 1)
let doubleWeekend = PayrollMonth(containing: first, settings: settings, calendar: calendar)
check(doubleWeekend.plannedSeconds == 21 * 9 * 3600, "November 2026 has 21 weekdays.")
check(doubleWeekend.snapshot(at: first).earnedCents == 0, "A new month begins at zero.")
check(doubleWeekend.snapshot(at: at(2026, 11, 2, 13, 30)).earnedCents == 50_000,
      "Half the first workday earns half of one day's pay.")
check(doubleWeekend.snapshot(at: at(2026, 11, 2, 18)).earnedCents ==
      doubleWeekend.snapshot(at: at(2026, 11, 3, 9)).earnedCents, "Earnings pause between shifts.")
check(doubleWeekend.snapshot(at: at(2026, 11, 7, 12)).phase == .dayOff, "Saturday is off on a five-day week.")
check(doubleWeekend.snapshot(at: at(2026, 11, 30, 18)).earnedCents == settings.salaryCents,
      "All planned shifts earn the full salary.")

settings.workweek = .sixDays
let singleWeekend = PayrollMonth(containing: first, settings: settings, calendar: calendar)
check(singleWeekend.plannedSeconds == 25 * 9 * 3600, "A six-day schedule adds all four Saturdays.")
check(singleWeekend.snapshot(at: at(2026, 11, 7, 12)).phase == .earning, "Saturday earns on a six-day week.")

settings.workweek = .alternating
settings.workingSaturday = CivilDay(at(2026, 11, 7), calendar: calendar)
let alternating = PayrollMonth(containing: first, settings: settings, calendar: calendar)
check(alternating.plannedSeconds == 23 * 9 * 3600, "An alternating schedule adds two Saturdays.")
check(alternating.snapshot(at: at(2026, 11, 7, 12)).phase == .earning, "The anchor Saturday is worked.")
check(alternating.snapshot(at: at(2026, 11, 14, 12)).phase == .dayOff, "The following Saturday is off.")
check(alternating.snapshot(at: at(2026, 11, 21, 12)).phase == .earning, "The next working Saturday is two weeks later.")
check(PayrollMonth.isWorkingSaturday(at(2026, 10, 24), anchor: settings.workingSaturday, calendar: calendar),
      "Alternating schedules apply before the anchor.")
check(PayrollMonth.saturday(ofWeekContaining: at(2026, 11, 8), calendar: calendar) == at(2026, 11, 7),
      "The week starts on Monday even on Sunday.")

settings.workweek = .sixDays
settings.startMinute = 22 * 60
settings.endMinute = 6 * 60
let overnight = PayrollMonth(containing: first, settings: settings, calendar: calendar)
check(overnight.plannedSeconds == (25 * 8 + 6 - 6) * 3600,
      "The prior month's shift contributes six hours; the last shift leaves six hours for December.")
check(overnight.snapshot(at: at(2026, 11, 1, 3)).phase == .earning, "An overnight shift continues on a day off.")
check(overnight.snapshot(at: at(2026, 11, 1, 3)).earnedCents > 0, "Only the part after midnight earns November pay.")
check(overnight.snapshot(at: at(2026, 11, 30, 23)).phase == .earning, "The final shift starts in November.")
let december = PayrollMonth(containing: at(2026, 12, 1), settings: settings, calendar: calendar)
check(december.snapshot(at: at(2026, 12, 1)).earnedCents == 0, "An active shift does not carry earnings to a new month.")
check(december.snapshot(at: at(2026, 12, 1, 3)).phase == .earning, "The same shift continues earning in December.")
check(december.snapshot(at: at(2026, 12, 1, 3)).earnedCents > 0, "Hours after midnight accrue in December.")

check(Display.salaryCents(" 12000.50 ") == 1_200_050, "A decimal salary is preserved in cents.")
check(Display.salaryCents("12.5") == 1250, "One decimal digit represents tenths.")
check(Display.salaryCents("12.345") == nil && Display.salaryCents("0") == nil, "Reject invalid amounts.")
print("Payroll and salary input tests passed.")
