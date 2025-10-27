using UnityEngine;

namespace ToyBox.Infrastructure;
public static partial class UI {
    public static void Label(string? title = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth()] : options;
        GUILayout.Label(title ?? "", options);
    }
    private static readonly Dictionary<string, string> m_EditStateCaches = [];
    public static void EditableLabel(string curVal, string name, Action<string> onConfirm, params GUILayoutOption[] options) {
        using (HorizontalScope()) {
            if (m_EditStateCaches.TryGetValue(name, out var newVal)) {
                _ = ActionTextField(ref newVal, name, null, e => {
                    if (curVal != e) {
                        onConfirm(e);
                    }
                    _ = m_EditStateCaches.Remove(name);
                }, options);
                if (m_EditStateCaches.ContainsKey(name)) {
                    m_EditStateCaches[name] = newVal;
                }
                Space(15);
                if (Button(Glyphs.CheckOff.Red(), null, GUI.skin.box)) {
                    _ = m_EditStateCaches.Remove(name);
                }
                if (Button(Glyphs.CheckOn.Green(), null, GUI.skin.box)) {
                    _ = m_EditStateCaches.Remove(name);
                    if (curVal != newVal) {
                        onConfirm(newVal);
                    }
                }
            } else {
                if (Button(Glyphs.Edit, null, GUI.skin.box)) {
                    m_EditStateCaches.Add(name, curVal);
                }
            }
        }
    }
}
