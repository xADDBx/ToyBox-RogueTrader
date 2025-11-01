using ToyBox.Infrastructure.Keybinds;
using UnityEngine;

namespace ToyBox.Infrastructure;
public static partial class UI {
    private static Hotkey? m_CurerntlyBindingHotkey;
    private static IBindableFeature? m_BindingForAction;
    private static bool m_LastBindingConflicted = false;
    public static bool HotkeyPicker(ref Hotkey? current, IBindableFeature feature) {
        var changed = false;
        using (HorizontalScope(AutoWidth())) {
            if (current != null) {
                Label(current.ToString().Orange());
                Space(10);
                if (Button(SharedStrings.DeleteLocalizedText.Orange())) {
                    _ = Hotkeys.RemoveHotkey(current);
                    changed = true;
                    current = null;
                }
                Space(10);
                if (Button(SharedStrings.RebindLocalizedText.Green())) {
                    m_CurerntlyBindingHotkey = new(default);
                    m_BindingForAction = feature;
                    m_LastBindingConflicted = false;
                }
            } else {
                if (Button(SharedStrings.BindLocalizedText.Cyan())) {
                    m_CurerntlyBindingHotkey = new(default);
                    m_BindingForAction = feature;
                    m_LastBindingConflicted = false;
                }
            }
            Space(10);
            if (m_CurerntlyBindingHotkey != null && m_BindingForAction == feature) {
                Label(SharedStrings.CurrentlyLocalizedText + $": {m_CurerntlyBindingHotkey}");
                Space(5);
                if (Button(SharedStrings.CancelLocalizedText.Orange())) {
                    m_CurerntlyBindingHotkey = null;
                    m_BindingForAction = null;
                }
                Space(5);
                if (Button(SharedStrings.ApplyLocalizedText.Green()) && m_CurerntlyBindingHotkey != null) {
                    if (Hotkeys.AddHotkey(m_CurerntlyBindingHotkey, feature)) {
                        current = m_CurerntlyBindingHotkey;
                        changed = true;
                        m_CurerntlyBindingHotkey = null;
                        m_BindingForAction = null;
                    } else {
                        m_LastBindingConflicted = true;
                    }
                }
                if (m_LastBindingConflicted) {
                    Space(5);
                    Label(SharedStrings.CurrentKeybindConflictsWithExistLocalizedText.Red().Bold());
                }
                if (Event.current.isKey && Event.current.type == EventType.KeyDown && m_CurerntlyBindingHotkey != null) {
                    m_LastBindingConflicted = false;
                    m_CurerntlyBindingHotkey.IsShift = Event.current.modifiers.HasFlag(EventModifiers.Shift);
                    m_CurerntlyBindingHotkey.IsAlt = Event.current.modifiers.HasFlag(EventModifiers.Alt);
                    m_CurerntlyBindingHotkey.IsCtrl = Event.current.modifiers.HasFlag(EventModifiers.Control) || Event.current.modifiers.HasFlag(EventModifiers.Command);
                    if (!IsCtrlAltOrShift(Event.current.keyCode)) {
                        m_CurerntlyBindingHotkey.Key = Event.current.keyCode;
                    } else if (Event.current.character != '\0') {
                        foreach (KeyCode c in Enum.GetValues(typeof(KeyCode))) {
                            if (Input.GetKeyDown(c)) {
                                m_CurerntlyBindingHotkey.Key = c;
                            }
                        }
                    } else {
                        m_CurerntlyBindingHotkey.Key = KeyCode.None;
                    }
                    Event.current.Use();
                }
            }
        }
        return changed;
    }
    private static bool IsCtrlAltOrShift(KeyCode code) {
#pragma warning disable IDE0072 // Add missing cases
        return code switch {
            KeyCode.LeftControl or KeyCode.RightControl or KeyCode.LeftCommand or KeyCode.RightCommand or KeyCode.LeftShift or KeyCode.RightShift or KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.None => true,
            _ => false,
        };
#pragma warning restore IDE0072 // Add missing cases
    }
}
