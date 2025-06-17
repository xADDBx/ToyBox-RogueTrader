﻿using UnityEngine;

namespace ToyBox.Infrastructure.UI;
public static partial class UI {
    private static GUIStyle m_CachedDisclosureToggleStyle {
        get {
            field ??= new GUIStyle(GUI.skin.label) { imagePosition = ImagePosition.ImageLeft, alignment = TextAnchor.MiddleLeft };
            return field;
        }
    }
    public static Lazy<float> DisclosureGlyphWidth => new(() => {
        var on = m_CachedDisclosureToggleStyle.CalcSize(new(Glyphs.DisclosureOn));
        var off = m_CachedDisclosureToggleStyle.CalcSize(new(Glyphs.DisclosureOff));
        return Math.Max(on.x, off.x);
    });
    public static bool Toggle(string name, string description, ref bool setting, Action onEnable, Action onDisable, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth()] : options;
        bool changed = false;
        using (HorizontalScope()) {
            var newValue = GUILayout.Toggle(setting, name.Cyan(), options);
            if (newValue != setting) {
                changed = true;
                setting = newValue;
                if (newValue) {
                    onEnable();
                } else {
                    onDisable();
                }
            }
            Space(10);
            Label(description.Green());
        }
        return changed;
    }
    public static bool DisclosureToggle(ref bool state, string? name = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth()] : options;
        string glyph = state ? Glyphs.DisclosureOn : Glyphs.DisclosureOff;
        var newValue = GUILayout.Toggle(state, glyph + (name ?? ""), m_CachedDisclosureToggleStyle, options);
        if (newValue != state) {
            state = newValue;
            return true;
        } else {
            return false;
        }
    }
}
