import SwiftUI

enum Style {
    /// The one accent colour. Everything else is primary or secondary text.
    static let accent = Color(red: 1.0, green: 0.62, blue: 0.04)
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
