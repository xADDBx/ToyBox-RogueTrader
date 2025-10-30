using ToyBox.Infrastructure.Hotkeys;
using UnityEngine;

namespace ToyBox;
[NeedsTesting]
public abstract partial class FeatureWithBindableAction : FeatureWithAction {
    private static Hotkey? m_CurerntlyBindingHotkey;
    private static FeatureWithBindableAction? m_BindingForAction;
    private static bool m_LastBindingConflicted = false;
    public Hotkey? Keybind {
        get;
        private set;
    }
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Button(Name.Cyan())) {
                ExecuteAction();
            }
            Space(10);
            if (Keybind != null) {
                UI.Label(Keybind.ToString().Orange());
                Space(10);
                if (UI.Button(m_DeleteLocalizedText.Orange())) {
                    _ = Hotkeys.RemoveHotkey(Keybind);
                    Keybind = null;
                }
                Space(10);
                if (UI.Button(m_RebindLocalizedText.Green())) {
                    m_CurerntlyBindingHotkey = new(default);
                    m_BindingForAction = this;
                    m_LastBindingConflicted = false;
                }
            } else {
                if (UI.Button(m_BindLocalizedText.Green())) {
                    m_CurerntlyBindingHotkey = new(default);
                    m_BindingForAction = this;
                    m_LastBindingConflicted = false;
                }
            }
            Space(10);
            if (m_CurerntlyBindingHotkey != null && m_BindingForAction == this) {
                UI.Label(m_CurrentlyLocalizedText + $": {m_CurerntlyBindingHotkey}");
                Space(5);
                if (UI.Button(m_CancelLocalizedText.Orange())) {
                    m_CurerntlyBindingHotkey = null;
                    m_BindingForAction = null;
                }
                Space(5);
                if (UI.Button(m_ApplyLocalizedText.Green()) && m_CurerntlyBindingHotkey != null) {
                    if (Hotkeys.AddHotkey(m_CurerntlyBindingHotkey, this)) {
                        Keybind = m_CurerntlyBindingHotkey;
                        m_CurerntlyBindingHotkey = null;
                        m_BindingForAction = null;
                    } else {
                        m_LastBindingConflicted = true;
                    }
                }
                if (m_LastBindingConflicted) {
                    Space(5);
                    UI.Label(m_CurrentKeybindConflictsWithExistLocalizedText.Red().Bold());
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
            Space(10);
            UI.Label(Description.Green());
        }
    }
    private static bool IsCtrlAltOrShift(KeyCode code) {
#pragma warning disable IDE0072 // Add missing cases
        return code switch {
            KeyCode.LeftControl or KeyCode.RightControl or KeyCode.LeftCommand or KeyCode.RightCommand or KeyCode.LeftShift or KeyCode.RightShift or KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.None => true,
            _ => false,
        };
#pragma warning restore IDE0072 // Add missing cases
    }

    [LocalizedString("ToyBox_FeatureWithBindableAction_m_DeleteLocalizedText", "Delete")]
    private static partial string m_DeleteLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_m_RebindLocalizedText", "Rebind")]
    private static partial string m_RebindLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_m_BindLocalizedText", "Bind")]
    private static partial string m_BindLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_m_CancelLocalizedText", "Cancel")]
    private static partial string m_CancelLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_m_ApplyLocalizedText", "Apply")]
    private static partial string m_ApplyLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_In_Progress_Keybind", "Currently")]
    private static partial string m_CurrentlyLocalizedText { get; }
    [LocalizedString("ToyBox_FeatureWithBindableAction_m_CurrentKeybindConflictsWithExistLocalizedText", "Current keybind conflicts with existing binding!")]
    private static partial string m_CurrentKeybindConflictsWithExistLocalizedText { get; }
}
