import SwiftUI

private let accent = Color(red: 1, green: 0.31, blue: 0.24)
private let secondary = Color.white.opacity(0.70)
private let hairline = Color.white.opacity(0.15)

struct Panel: View {
    @ObservedObject var model: AppModel
    let close: () -> Void
    @State private var editing = false

    var body: some View {
        Group {
            if editing {
                SettingsPanel(model: model) { editing = false }
            } else {
                Dashboard(model: model, edit: { editing = true }, close: close)
            }
        }
        .frame(width: 376, height: 366)
        .foregroundStyle(.white)
        .environment(\.locale, Display.chinese)
    }
}

private struct Dashboard: View {
    @ObservedObject var model: AppModel
    let edit: () -> Void
    let close: () -> Void

    var body: some View {
        TimelineView(.periodic(from: .now, by: 0.15)) { timeline in
            let now = timeline.date
            let ledger = model.payroll(for: now)
            let snapshot = ledger.snapshot(at: now)
            let configured = ledger.settings.salaryCents > 0
            let month = ledger.calendar.dateComponents([.year, .month], from: ledger.month.start)
            VStack(spacing: 0) {
                Text(String(format: "%d / %02d", month.year!, month.month!))
                    .font(.system(size: 25, weight: .semibold, design: .rounded))
                    .monospacedDigit()
                    .accessibilityLabel(Text(verbatim: "\(month.year!) 年 \(month.month!) 月"))

                Spacer(minLength: 13)
                Text("本月已赚")
                    .font(.system(size: 12, weight: .medium))
                    .foregroundStyle(secondary)
                amount(snapshot.earnedCents, configured: configured)
                    .padding(.top, 3)
                Spacer(minLength: 11)

                progress(snapshot, configured: configured)

                if configured {
                    schedule(ledger, snapshot: snapshot, now: now)
                        .frame(height: 71)
                } else {
                    Button(action: edit) {
                        Label("设置月薪与排班", systemImage: "arrow.right")
                    }
                    .buttonStyle(.bordered)
                    .controlSize(.regular)
                    .frame(height: 71)
                }

                Rectangle().fill(hairline).frame(height: 1)
                HStack(spacing: 9) {
                    GlyphButton(symbol: "slider.horizontal.3", label: "设置", action: edit)
                    Text(footer(now: now, ledger: ledger, configured: configured))
                        .font(.system(size: 12, weight: .medium))
                        .foregroundStyle(secondary)
                    Spacer()
                    GlyphButton(symbol: "xmark.circle", label: "关闭面板", action: close)
                        .keyboardShortcut(.cancelAction)
                }
                .padding(.top, 8)
            }
            .padding(.horizontal, 23)
            .padding(.top, 19)
            .padding(.bottom, 10)
        }
    }

    private func amount(_ cents: Int, configured: Bool) -> some View {
        HStack(alignment: .firstTextBaseline, spacing: 5) {
            Text("¥").font(.system(size: 25, weight: .medium, design: .rounded))
                .foregroundStyle(secondary)
            Text(Display.money(cents))
                .font(.system(size: 54, weight: .semibold, design: .rounded))
                .monospacedDigit()
                .tracking(-1.8)
                .contentTransition(.numericText(value: Double(cents)))
                .animation(.easeOut(duration: 0.14), value: cents)
                .lineLimit(1)
                .minimumScaleFactor(0.48)
        }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(configured ? "本月已赚 \(Display.money(cents)) 元" : "尚未设置月薪")
    }

    private func progress(_ snapshot: PayrollMonth.Snapshot, configured: Bool) -> some View {
        let earning = configured && snapshot.phase == .earning
        return VStack(spacing: 8) {
            HStack(spacing: 6) {
                Circle().fill(earning ? accent : Color.white.opacity(0.36)).frame(width: 6, height: 6)
                Text(configured ? snapshot.phase.label : "待设置")
                    .foregroundStyle(earning ? .white : secondary)
                Spacer()
                Text(configured ? snapshot.fraction.formatted(.percent.precision(.fractionLength(1))) : "—")
                    .monospacedDigit()
                    .foregroundStyle(secondary)
                    .accessibilityLabel(configured
                        ? "本月计薪进度 \(snapshot.fraction.formatted(.percent.precision(.fractionLength(1))))"
                        : "本月计薪进度待设置")
            }
            .font(.system(size: 12, weight: .medium))

            GeometryReader { geometry in
                ZStack(alignment: .leading) {
                    Capsule().fill(hairline)
                    if configured && snapshot.fraction > 0 {
                        Capsule().fill(earning ? accent : Color.white.opacity(0.54))
                            .frame(width: max(4, geometry.size.width * snapshot.fraction))
                    }
                }
            }
            .frame(height: 4)
            .accessibilityHidden(true)
        }
    }

    private func schedule(_ ledger: PayrollMonth, snapshot: PayrollMonth.Snapshot, now: Date) -> some View {
        let settings = ledger.settings
        let calendar = ledger.calendar
        let overnight = settings.endMinute < settings.startMinute ? "次日 " : ""
        let hours = "\(Display.time(settings.startMinute)) – \(overnight)\(Display.time(settings.endMinute))"
        let title: String
        let detail: String
        if let active = snapshot.activeShift {
            title = "距下班"
            detail = Display.countdown(active.end.timeIntervalSince(now))
        } else if let next = snapshot.nextShift {
            title = "下次上班"
            detail = "\(Display.day(next.start, relativeTo: now, calendar: calendar)) \(Display.time(next.start, calendar: calendar))"
        } else {
            title = "当前状态"
            detail = snapshot.phase.label
        }
        return HStack(spacing: 0) {
            scheduleItem("工作时段", value: hours)
                .frame(maxWidth: .infinity, alignment: .leading)
            Rectangle().fill(hairline).frame(width: 1, height: 28)
                .padding(.trailing, 20)
            scheduleItem(title, value: detail)
                .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    private func scheduleItem(_ title: String, value: String) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(title).font(.system(size: 11)).foregroundStyle(secondary)
            Text(value).font(.system(size: 14, weight: .medium)).monospacedDigit()
        }
        .accessibilityElement(children: .combine)
    }

    private func footer(now: Date, ledger: PayrollMonth, configured: Bool) -> String {
        if !configured { return "牛马兴奋剂" }
        var dateStyle = Date.FormatStyle().month().day().weekday(.abbreviated).locale(Display.chinese)
        dateStyle.timeZone = ledger.calendar.timeZone
        let date = now.formatted(dateStyle)
        return "\(ledger.settings.workweek.rawValue)  ·  \(date)"
    }
}

private struct SettingsPanel: View {
    @ObservedObject var model: AppModel
    let dismiss: () -> Void

    @State private var salary: String
    @State private var draft: WorkSettings
    @State private var start: Date
    @State private var end: Date
    @State private var error: String?
    private let calendar: Calendar
    private let saturday: Date

    init(model: AppModel, dismiss: @escaping () -> Void) {
        self.model = model
        self.dismiss = dismiss
        let settings = model.settings
        let calendar = AppModel.calendar()
        self.calendar = calendar
        self.saturday = PayrollMonth.saturday(ofWeekContaining: Date(), calendar: calendar)
        _draft = State(initialValue: settings)
        _salary = State(initialValue: settings.salaryCents == 0 ? "" :
            (Double(settings.salaryCents) / 100).formatted(
                .number.locale(Display.chinese).grouping(.never).precision(.fractionLength(2))))
        let pickerDay = CivilDay(year: 2000, month: 1, day: 15)
        _start = State(initialValue: pickerDay.date(in: calendar, minute: settings.startMinute))
        _end = State(initialValue: pickerDay.date(in: calendar, minute: settings.endMinute))
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 11) {
            Text("工资与排班")
                .font(.system(size: 19, weight: .semibold))
                .frame(maxWidth: .infinity)
                .overlay(alignment: .leading) {
                    GlyphButton(symbol: "chevron.left", label: "返回", action: dismiss)
                        .keyboardShortcut(.cancelAction)
                }
                .padding(.bottom, 4)

            HStack(spacing: 8) {
                Text("月薪").font(.system(size: 13, weight: .medium))
                Spacer()
                HStack(spacing: 5) {
                    Text("¥").foregroundStyle(secondary)
                    TextField("例如 12000", text: $salary)
                        .textFieldStyle(.plain)
                        .monospacedDigit()
                        .frame(width: 105)
                        .accessibilityLabel("月薪")
                }
                .font(.system(size: 13, weight: .medium))
                .padding(.horizontal, 8)
                .padding(.vertical, 6)
                .background(.white.opacity(0.09), in: RoundedRectangle(cornerRadius: 6))
                Text("元 / 月").font(.system(size: 11)).foregroundStyle(secondary)
            }

            rule

            VStack(alignment: .leading, spacing: 6) {
                HStack(spacing: 0) {
                    timeField("上班", date: $start)
                    Spacer()
                    timeField("下班", date: $end)
                }
                if minute(of: end) < minute(of: start) {
                    Text("下班时间为次日")
                        .font(.system(size: 11)).foregroundStyle(secondary)
                }
            }

            rule

            VStack(alignment: .leading, spacing: 8) {
                HStack {
                    Text("排班").font(.system(size: 13, weight: .medium))
                    Spacer(minLength: 20)
                    Picker("排班", selection: $draft.workweek) {
                        ForEach(Workweek.allCases) { workweek in
                            Text(workweek.rawValue).tag(workweek)
                        }
                    }
                    .labelsHidden()
                    .pickerStyle(.segmented)
                    .frame(width: 198)
                }
                if draft.workweek == .alternating {
                    let parts = calendar.dateComponents([.month, .day], from: saturday)
                    Toggle(isOn: Binding(get: {
                        PayrollMonth.isWorkingSaturday(saturday, anchor: draft.workingSaturday, calendar: calendar)
                    }, set: { working in
                        let anchor = working ? saturday : calendar.date(byAdding: .day, value: 7, to: saturday)!
                        draft.workingSaturday = CivilDay(anchor, calendar: calendar)
                    })) {
                        Text("本周六（\(parts.month!)/\(parts.day!)）上班")
                            .font(.system(size: 12))
                    }
                    .toggleStyle(.switch)
                    .controlSize(.small)
                }
                Text(draft.workweek.summary).font(.system(size: 11)).foregroundStyle(secondary)
            }

            Spacer(minLength: 0)
            if let error {
                Text(error).font(.system(size: 11)).foregroundStyle(Color(red: 1, green: 0.69, blue: 0.64))
            } else {
                Text("设置保存在本机，修改后会重新计算本月金额。")
                    .font(.system(size: 11)).foregroundStyle(secondary)
            }
            rule
            HStack {
                GlyphButton(symbol: "power", label: "退出应用") { NSApp.terminate(nil) }
                Spacer()
                Button("保存", action: save)
                    .buttonStyle(.borderedProminent)
                    .controlSize(.regular)
                    .tint(accent)
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(.horizontal, 23)
        .padding(.top, 20)
        .padding(.bottom, 10)
    }

    private var rule: some View {
        Rectangle().fill(hairline).frame(height: 1)
    }

    private func timeField(_ label: String, date: Binding<Date>) -> some View {
        HStack(spacing: 8) {
            Text(label).font(.system(size: 13, weight: .medium))
            DatePicker(label, selection: date, displayedComponents: .hourAndMinute)
                .labelsHidden()
                .datePickerStyle(.stepperField)
                .environment(\.timeZone, calendar.timeZone)
                .fixedSize()
        }
    }

    private func minute(of date: Date) -> Int {
        let parts = calendar.dateComponents([.hour, .minute], from: date)
        return parts.hour! * 60 + parts.minute!
    }

    private func save() {
        guard let cents = Display.salaryCents(salary) else {
            error = "请输入大于 0 的月薪，最多两位小数。"
            return
        }
        let startMinute = minute(of: start)
        let endMinute = minute(of: end)
        guard startMinute != endMinute else {
            error = "上班和下班时间不能相同。"
            return
        }
        draft.salaryCents = cents
        draft.startMinute = startMinute
        draft.endMinute = endMinute
        model.save(draft)
        dismiss()
    }
}

private struct GlyphButton: View {
    let symbol: String
    let label: String
    let action: () -> Void
    @State private var hovering = false

    var body: some View {
        Button(action: action) {
            Image(systemName: symbol)
                .font(.system(size: 15, weight: .medium))
                .frame(width: 28, height: 28)
                .background(.white.opacity(hovering ? 0.13 : 0), in: RoundedRectangle(cornerRadius: 7))
                .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .onHover { hovering = $0 }
        .accessibilityLabel(label)
        .help(label)
    }
}
