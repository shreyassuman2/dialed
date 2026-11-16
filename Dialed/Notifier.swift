import Foundation
import UserNotifications

/// The app's two notifications: timer finished, and the Monday summary.
enum Notifier {
    private static let timerID = "timer-finished"
    private static let weeklyID = "weekly-summary"
    private static let delegate = ForegroundDelegate()

    static func setup() {
        let center = UNUserNotificationCenter.current()
        center.delegate = delegate
        center.requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

    static func scheduleTimerEnd(at date: Date, difficulty: Difficulty) {
        let content = UNMutableNotificationContent()
        content.title = "Time's up"
        content.body = "\(difficulty.minutes) minutes for a \(difficulty.rawValue) problem have passed."
        content.sound = .default
        let interval = max(1, date.timeIntervalSinceNow)
        let trigger = UNTimeIntervalNotificationTrigger(timeInterval: interval, repeats: false)
        let center = UNUserNotificationCenter.current()
        center.removePendingNotificationRequests(withIdentifiers: [timerID])
        center.add(UNNotificationRequest(identifier: timerID, content: content, trigger: trigger))
    }

    static func cancelTimerEnd() {
        UNUserNotificationCenter.current().removePendingNotificationRequests(withIdentifiers: [timerID])
    }

    /// Replaces the pending Monday summary. Called after every change, so its text stays current
    /// even if the app is not running on Monday morning.
    static func scheduleWeekly(at date: Date, title: String, body: String) {
        let center = UNUserNotificationCenter.current()
        center.removePendingNotificationRequests(withIdentifiers: [weeklyID])
        guard date > Date() else { return }
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        let parts = Pace.calendar.dateComponents([.year, .month, .day, .hour, .minute], from: date)
        let trigger = UNCalendarNotificationTrigger(dateMatching: parts, repeats: false)
        center.add(UNNotificationRequest(identifier: weeklyID, content: content, trigger: trigger))
    }

    static func cancelWeekly() {
        UNUserNotificationCenter.current().removePendingNotificationRequests(withIdentifiers: [weeklyID])
    }
}

/// Lets banners appear while the app counts as frontmost (the popover is open).
private final class ForegroundDelegate: NSObject, UNUserNotificationCenterDelegate {
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                                willPresent notification: UNNotification,
                                withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        completionHandler([.banner, .sound])
    }
}
