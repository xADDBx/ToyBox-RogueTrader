using UnityEngine;

namespace ToyBox.Infrastructure;

public static class Glyphs {
    private const string m_DefaultDisclosureOn = "▼";
    private const string m_DefaultDisclosureOff = "▶";
    private const string m_DefaultEdit = "✎";
    private const string m_DefaultCheckOn = "✔";
    private const string m_DefaultCheckOff = "✖";
    private const string m_CharCodeDisclosureOn = "v";
    private const string m_CharCodeDisclosureOff = ">";
    private const string m_CharCodeEdit = "edit";
    private const string m_CharCodeCheckOn = "[X]";
    private const string m_CharCodeCheckOff = "[ ]";

    public static string DisclosureOn {
        get {
            return Settings.UseDefaultGlyphs ? m_DefaultDisclosureOn : m_CharCodeDisclosureOn;
        }
    }

    public static string DisclosureOff {
        get {
            return Settings.UseDefaultGlyphs ? m_DefaultDisclosureOff : m_CharCodeDisclosureOff;
        }
    }

    public static string Edit {
        get {
            return Settings.UseDefaultGlyphs ? m_DefaultEdit : m_CharCodeEdit;
        }
    }

    public static string CheckOn {
        get {
            return Settings.UseDefaultGlyphs ? m_DefaultCheckOn : m_CharCodeCheckOn;
        }
    }

    public static string CheckOff {
        get {
            return Settings.UseDefaultGlyphs ? m_DefaultCheckOff : m_CharCodeCheckOff;
        }
    }

    public static void CheckGlyphSupport() {
        if (!Settings.CheckForGlyphSupport) {
            Log("Skipping glyph support check (disabled).");
            return;
        }
        var font = GUI.skin.font;
        Settings.UseDefaultGlyphs = font != null
            && font.HasCharacter(m_DefaultDisclosureOn[0])
            && font.HasCharacter(m_DefaultDisclosureOff[0])
            && font.HasCharacter(m_DefaultEdit[0])
            && font.HasCharacter(m_DefaultCheckOn[0])
            && font.HasCharacter(m_DefaultCheckOff[0]);
        Log($"Glyph support check: UseDefaultGlyphs = {Settings.UseDefaultGlyphs}");
    }
}
