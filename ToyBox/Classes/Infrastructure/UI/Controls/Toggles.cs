using Kingmaker.Utility.UnityExtensions;
using UnityEngine;

namespace ToyBox.Infrastructure;
public static partial class UI {
    private static GUIStyle m_DisclosureToggleStyle {
        get {
            field ??= new GUIStyle(GUI.skin.label) { imagePosition = ImagePosition.ImageLeft, alignment = TextAnchor.MiddleLeft };
            return field;
        }
    }
    public static float WidthInDisclosureStyle(string label) {
        return CalculateLargestLabelSize([Glyphs.DisclosureOn + label, Glyphs.DisclosureOff + label], m_DisclosureToggleStyle);
    }
    public static Lazy<float> DisclosureGlyphWidth {
        get {
            return new(() => {
                return CalculateLargestLabelSize([Glyphs.DisclosureOn, Glyphs.DisclosureOff], m_DisclosureToggleStyle);
            });
        }
    }

    public static bool Toggle(string name, string? description, ref bool setting, Action? onEnable = null, Action? onDisable = null) {
        var changed = false;
        var nameWidth = GUI.skin.toggle.CalcSize(new(name)).x;
        var descWidth = 0f;
        if (description != null) {
            descWidth = GUI.skin.toggle.CalcSize(new(description)).x;
        }
        using (HorizontalScope(Width(nameWidth + descWidth))) {
            var newValue = GUILayout.Toggle(setting, name, Width(nameWidth));
            if (newValue != setting) {
                changed = true;
                setting = newValue;
                if (newValue) {
                    onEnable?.Invoke();
                } else {
                    onDisable?.Invoke();
                }
            }
            if (!description.IsNullOrEmpty()) {
                Space(10);
                Label(description!.Green(), Width(descWidth));
            }
        }
        return changed;
    }
    public static bool DisclosureToggle(ref bool state, string? name = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth()] : options;
        var glyph = state ? Glyphs.DisclosureOn : Glyphs.DisclosureOff;
        var newValue = GUILayout.Toggle(state, glyph + (name ?? ""), m_DisclosureToggleStyle, options);
        if (newValue != state) {
            state = newValue;
            return true;
        } else {
            return false;
        }
    }
}
