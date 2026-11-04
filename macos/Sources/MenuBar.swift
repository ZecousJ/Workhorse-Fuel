import AppKit
import SwiftUI

@main
@MainActor
struct NiumaApp {
    static func main() {
        let app = NSApplication.shared
        let delegate = MenuBarController()
        app.delegate = delegate
        app.setActivationPolicy(.accessory)
        withExtendedLifetime(delegate) { app.run() }
    }
}

@MainActor
final class MenuBarController: NSObject, NSApplicationDelegate, NSPopoverDelegate {
    private let model = AppModel()
    private let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
    private let popover = NSPopover()

    func applicationDidFinishLaunching(_ notification: Notification) {
        statusItem.button?.image = Self.statusIcon()
        statusItem.button?.toolTip = "牛马兴奋剂"
        statusItem.button?.setAccessibilityLabel("牛马兴奋剂")
        statusItem.button?.target = self
        statusItem.button?.action = #selector(togglePopover)
        popover.behavior = .transient
        popover.animates = false
        popover.delegate = self
        popover.appearance = NSAppearance(named: .darkAqua)
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        showPopover()
        return false
    }

    @objc private func togglePopover() {
        if popover.isShown {
            popover.performClose(nil)
        } else {
            showPopover()
        }
    }

    private func showPopover() {
        guard !popover.isShown, let button = statusItem.button else { return }
        popover.contentViewController = NSHostingController(rootView: Panel(model: model) { [weak self] in
            self?.popover.performClose(nil)
        })
        NSApp.activate()
        let bottomEdge: NSRectEdge = button.isFlipped ? .maxY : .minY
        popover.show(relativeTo: button.bounds, of: button, preferredEdge: bottomEdge)
        guard let popup = popover.contentViewController?.view.window,
              let statusWindow = button.window, let screen = statusWindow.screen else { return }
        let icon = statusWindow.convertToScreen(button.convert(button.bounds, to: nil))
        let menuBottom = min(icon.minY, screen.visibleFrame.maxY)
        // Keep AppKit's horizontal anchor and arrow, with the window below the menu bar.
        popup.setFrameOrigin(NSPoint(x: popup.frame.minX, y: menuBottom - popup.frame.height))
        popup.makeKey()
    }

    func popoverDidClose(_ notification: Notification) {
        popover.contentViewController = nil
    }

    private static func statusIcon() -> NSImage {
        let icon = NSImage(size: NSSize(width: 19, height: 18), flipped: true) { rect in
            guard let context = NSGraphicsContext.current?.cgContext else { return false }
            context.saveGState()
            context.translateBy(x: rect.minX + 0.5, y: rect.minY)
            context.scaleBy(x: 18 / 24, y: 18 / 24)
            context.addPath(Brand.silhouette())
            context.setFillColor(NSColor.black.cgColor)
            context.fillPath(using: .evenOdd)
            context.restoreGState()
            return true
        }
        icon.isTemplate = true
        return icon
    }
}
