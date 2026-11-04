import Foundation

enum Workweek: String, Codable, CaseIterable, Identifiable {
    case fiveDays = "双休"
    case alternating = "大小周"
    case sixDays = "单休"

    var id: Self { self }
    var summary: String {
        switch self {
        case .fiveDays: "周六、周日休息"
        case .alternating: "周六隔周上班，周日休息"
        case .sixDays: "周日休息"
        }
    }
}

struct WorkSettings: Codable, Equatable {
    var salaryCents = 0
    var startMinute = 9 * 60
    var endMinute = 18 * 60
    var workweek: Workweek = .fiveDays
    var workingSaturday = CivilDay(year: 2024, month: 1, day: 6)
}

struct CivilDay: Codable, Equatable, Hashable {
    let year: Int
    let month: Int
    let day: Int

    init(year: Int, month: Int, day: Int) {
        self.year = year
        self.month = month
        self.day = day
    }

    init(_ date: Date, calendar: Calendar) {
        let parts = calendar.dateComponents([.year, .month, .day], from: date)
        self.init(year: parts.year!, month: parts.month!, day: parts.day!)
    }

    func date(in calendar: Calendar, minute: Int = 0) -> Date {
        calendar.date(from: DateComponents(year: year, month: month, day: day,
                                           hour: minute / 60, minute: minute % 60))!
    }
}

struct WorkShift {
    let start: Date
    let end: Date

    func seconds(from lower: Date, to upper: Date) -> TimeInterval {
        max(0, min(end, upper).timeIntervalSince(max(start, lower)))
    }
}

struct PayrollMonth {
    enum Phase {
        case earning, beforeWork, afterWork, dayOff

        var label: String {
            switch self {
            case .earning: "计薪中"
            case .beforeWork: "待上班"
            case .afterWork: "已下班"
            case .dayOff: "休息中"
            }
        }
    }

    struct Snapshot {
        let fraction: Double
        let earnedCents: Int
        let phase: Phase
        let activeShift: WorkShift?
        let nextShift: WorkShift?
    }

    let calendar: Calendar
    let month: DateInterval
    let settings: WorkSettings
    private let shifts: [WorkShift]
    let plannedSeconds: TimeInterval

    init(containing date: Date, settings: WorkSettings, calendar: Calendar) {
        let month = calendar.dateInterval(of: .month, for: date)!
        self.calendar = calendar
        self.month = month
        self.settings = settings

        // Include shifts crossing either month boundary and the next workday.
        var day = calendar.date(byAdding: .day, value: -1, to: month.start)!
        let limit = calendar.date(byAdding: .day, value: 7, to: month.end)!
        var shifts: [WorkShift] = []
        while day < limit {
            if Self.isWorkday(day, settings: settings, calendar: calendar) {
                let nextDay = calendar.date(byAdding: .day, value: 1, to: day)!
                let endDay = settings.endMinute < settings.startMinute ? nextDay : day
                let start = CivilDay(day, calendar: calendar).date(in: calendar, minute: settings.startMinute)
                let end = CivilDay(endDay, calendar: calendar).date(in: calendar, minute: settings.endMinute)
                if end > start { shifts.append(WorkShift(start: start, end: end)) }
            }
            day = calendar.date(byAdding: .day, value: 1, to: day)!
        }
        self.shifts = shifts
        plannedSeconds = shifts.reduce(0) { $0 + $1.seconds(from: month.start, to: month.end) }
    }

    func snapshot(at now: Date) -> Snapshot {
        let cutoff = min(now, month.end)
        let worked = shifts.reduce(0) { $0 + $1.seconds(from: month.start, to: cutoff) }
        let fraction = plannedSeconds > 0 ? min(worked / plannedSeconds, 1) : 0
        let cents = Int((Double(settings.salaryCents) * fraction).rounded(.down))
        let active = shifts.first { $0.start <= now && now < $0.end }
        let next = shifts.first { $0.start > now }
        let phase: Phase
        if active != nil {
            phase = .earning
        } else if let next, calendar.isDate(next.start, inSameDayAs: now) {
            phase = .beforeWork
        } else if shifts.contains(where: {
            calendar.isDate($0.start, inSameDayAs: now) || calendar.isDate($0.end, inSameDayAs: now)
        }) {
            phase = .afterWork
        } else {
            phase = .dayOff
        }
        return Snapshot(fraction: fraction, earnedCents: cents, phase: phase,
                        activeShift: active, nextShift: next)
    }

    static func saturday(ofWeekContaining date: Date, calendar: Calendar) -> Date {
        let day = calendar.startOfDay(for: date)
        let daysSinceMonday = (calendar.component(.weekday, from: day) + 5) % 7
        return calendar.date(byAdding: .day, value: 5 - daysSinceMonday, to: day)!
    }

    static func isWorkingSaturday(_ date: Date, anchor: CivilDay, calendar: Calendar) -> Bool {
        let days = calendar.dateComponents([.day], from: anchor.date(in: calendar),
                                           to: calendar.startOfDay(for: date)).day!
        return days.isMultiple(of: 14)
    }

    private static func isWorkday(_ date: Date, settings: WorkSettings, calendar: Calendar) -> Bool {
        switch calendar.component(.weekday, from: date) {
        case 1: false
        case 7:
            switch settings.workweek {
            case .fiveDays: false
            case .sixDays: true
            case .alternating: isWorkingSaturday(date, anchor: settings.workingSaturday, calendar: calendar)
            }
        default: true
        }
    }
}
