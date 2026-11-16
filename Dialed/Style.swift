import AppKit
import SwiftUI

enum Style {
    /// The one accent colour. Everything else is primary or secondary text.
    /// Blue, darker in light mode and brighter in dark mode so it reads on both.
    static let accent = Color(nsColor: NSColor(name: nil) { appearance in
        appearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
            ? NSColor(red: 0.40, green: 0.68, blue: 1.00, alpha: 1)
            : NSColor(red: 0.00, green: 0.38, blue: 0.90, alpha: 1)
    })
    static let font = Font.system(size: 12, design: .monospaced)
    static let small = Font.system(size: 11, design: .monospaced)
    static let big = Font.system(size: 44, weight: .bold, design: .monospaced)
    static let width: CGFloat = 400
}

/// A bracketed text button: `[ easy 20 ]`.
struct TermButtonStyle: ButtonStyle {
    var selected = false

    func makeBody(configuration: Configuration) -> some View {
        TermButtonBody(configuration: configuration, selected: selected)
    }

    private struct TermButtonBody: View {
        let configuration: Configuration
        let selected: Bool
        @State private var hovering = false
        @Environment(\.isEnabled) private var enabled

        var body: some View {
            HStack(spacing: 0) {
                Text("[ ").foregroundStyle(.secondary)
                configuration.label
                    .fontWeight(selected ? .bold : .regular)
                Text(" ]").foregroundStyle(.secondary)
            }
            .foregroundStyle(selected || hovering ? Style.accent : Color.primary)
            .opacity(enabled ? (configuration.isPressed ? 0.5 : 1) : 0.35)
            .contentShape(Rectangle())
            .onHover { hovering = $0 }
        }
    }
}

/// A plain text link: `undo`.
struct LinkButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        LinkBody(configuration: configuration)
    }

    private struct LinkBody: View {
        let configuration: Configuration
        @State private var hovering = false
        @Environment(\.isEnabled) private var enabled

        var body: some View {
            configuration.label
                .foregroundStyle(hovering ? Style.accent : Color.secondary)
                .opacity(enabled ? (configuration.isPressed ? 0.5 : 1) : 0.35)
                .contentShape(Rectangle())
                .onHover { hovering = $0 }
        }
    }
}

extension String {
    func padded(_ length: Int) -> String {
        padding(toLength: length, withPad: " ", startingAt: 0)
    }
}
