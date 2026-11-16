import ServiceManagement
import SwiftData
import SwiftUI

@main
struct DialedApp: App {
    private let container: ModelContainer
    @State private var model: AppModel

    init() {
        // An explicit store path: an unsandboxed app would otherwise share "default.store".
        let folder = URL.applicationSupportDirectory.appending(path: "Dialed", directoryHint: .isDirectory)
        // The app used to be called Lock In. Move its data over the first time.
        let oldFolder = URL.applicationSupportDirectory.appending(path: "LockIn", directoryHint: .isDirectory)
        if !FileManager.default.fileExists(atPath: folder.path), FileManager.default.fileExists(atPath: oldFolder.path) {
            try? FileManager.default.moveItem(at: oldFolder, to: folder)
            for suffix in ["", "-shm", "-wal"] {
                try? FileManager.default.moveItem(at: folder.appending(path: "LockIn.store" + suffix),
                                                  to: folder.appending(path: "Dialed.store" + suffix))
            }
        }
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        let config = ModelConfiguration(url: folder.appending(path: "Dialed.store"))
        do {
            container = try ModelContainer(for: Goals.self, Entry.self, Week.self, configurations: config)
        } catch {
            fatalError("Could not open the data store: \(error)")
        }
        _model = State(initialValue: AppModel(context: container.mainContext))

        Notifier.setup()

        // Launch at login is on by default. Registered once, so turning it off in
        // System Settings > General > Login Items stays respected.
        let key = "didRegisterLoginItem"
        if !UserDefaults.standard.bool(forKey: key) {
            try? SMAppService.mainApp.register()
            UserDefaults.standard.set(true, forKey: key)
        }
    }

    var body: some Scene {
        MenuBarExtra {
            PopoverView(model: model)
        } label: {
            if let seconds = model.remainingSeconds {
                Text(Pace.clock(seconds)).monospacedDigit()
            } else {
                Image(systemName: "lock.fill")
            }
        }
        .menuBarExtraStyle(.window)
    }
}
