import Foundation
import SwiftUI

@MainActor
final class AppModel: ObservableObject {
    @Published private(set) var settings: WorkSettings
    private var ledger: PayrollMonth?
    private let defaults: UserDefaults
    private let storageKey = "workSettings"

    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
        if let data = defaults.data(forKey: storageKey),
           let saved = try? JSONDecoder().decode(WorkSettings.self, from: data) {
            settings = saved
        } else {
            settings = WorkSettings()
        }
    }

    func save(_ settings: WorkSettings) {
        defaults.set(try! JSONEncoder().encode(settings), forKey: storageKey)
        ledger = nil
        self.settings = settings
    }

    static func calendar() -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = .current
        return calendar
    }

    func payroll(for date: Date) -> PayrollMonth {
        let calendar = Self.calendar()
        if let ledger, ledger.calendar.timeZone == calendar.timeZone,
           ledger.month.start <= date, date < ledger.month.end {
            return ledger
        }
        let new = PayrollMonth(containing: date, settings: settings, calendar: calendar)
        ledger = new
        return new
    }
}

enum Display {
    static let chinese = Locale(identifier: "zh_CN")

    static func money(_ cents: Int) -> String {
        (Double(cents) / 100).formatted(.number.locale(chinese).precision(.fractionLength(2)))
    }

    static func time(_ minute: Int) -> String {
        String(format: "%02d:%02d", minute / 60, minute % 60)
    }

    static func time(_ date: Date, calendar: Calendar) -> String {
        let parts = calendar.dateComponents([.hour, .minute], from: date)
        return time(parts.hour! * 60 + parts.minute!)
    }

    static func countdown(_ seconds: TimeInterval) -> String {
        let count = Int(max(0, seconds).rounded(.up))
        return String(format: "%02d:%02d:%02d", count / 3600, (count / 60) % 60, count % 60)
    }

    static func day(_ date: Date, relativeTo now: Date, calendar: Calendar) -> String {
        if calendar.isDate(date, inSameDayAs: now) { return "今天" }
        if let tomorrow = calendar.date(byAdding: .day, value: 1, to: now),
           calendar.isDate(date, inSameDayAs: tomorrow) { return "明天" }
        let parts = calendar.dateComponents([.month, .day], from: date)
        return "\(parts.month!) 月 \(parts.day!) 日"
    }

    static func salaryCents(_ input: String) -> Int? {
        let value = input.trimmingCharacters(in: .whitespacesAndNewlines)
        guard value.wholeMatch(of: /[0-9]{1,8}(\.[0-9]{1,2})?/) != nil else { return nil }
        let parts = value.split(separator: ".")
        let fraction = parts.count == 2 ? Int(parts[1])! * (parts[1].count == 1 ? 10 : 1) : 0
        let cents = Int(parts[0])! * 100 + fraction
        return cents > 0 ? cents : nil
    }
}
