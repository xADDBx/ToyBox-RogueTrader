using UnityEngine;

namespace ToyBox.Infrastructure;

public static partial class UI {
    private readonly static Dictionary<Type, Array> m_EnumCache = [];
    private readonly static Dictionary<Type, Dictionary<object, int>> m_IndexToEnumCache = [];
    private readonly static Dictionary<Type, string[]> m_DefaultEnumNameCache = [];
    private readonly static Dictionary<Type, string[]> m_EnumNameCache = [];
    public static bool SelectionGridWithDefault<TEnum>(ref TEnum selected, ref bool isDefault, int xCols, Func<(TEnum val, bool isNone), string>? titler, params GUILayoutOption[] options) where TEnum : Enum {
        if (!m_EnumCache.TryGetValue(typeof(TEnum), out var vals)) {
            vals = Enum.GetValues(typeof(TEnum));
            m_EnumCache[typeof(TEnum)] = vals;
        }
        if (!m_DefaultEnumNameCache.TryGetValue(typeof(TEnum), out var names)) {
            Dictionary<object, int> indexToEnum = [];
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            var defaultName = titler != null ? titler((default, true)) : SharedStrings.NoneText;
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            List<string> tmpNames = [defaultName];
            for (var i = 0; i < vals.Length; i++) {
                string name;
                var val = vals.GetValue(i);
                indexToEnum[val] = i;
                if (titler != null) {
                    name = titler(((TEnum)val, false));
                } else {
                    name = Enum.GetName(typeof(TEnum), val);
                }
                tmpNames.Add(name);
            }
            names = [.. tmpNames];
            m_DefaultEnumNameCache[typeof(TEnum)] = names;
            m_IndexToEnumCache[typeof(TEnum)] = indexToEnum;
        }
        if (xCols <= 0) {
            xCols = vals.Length + 1;
        }
        var selectedInt = 0;
        if (!isDefault) {
            selectedInt = m_IndexToEnumCache[typeof(TEnum)][selected] + 1;
        }
        // Create a copy to not recolour the selected element permanently
        // names = [.. names];
        // Better idea: Just cache that one name and change it back after
        var uncolored = names[selectedInt];
        names[selectedInt] = uncolored.Orange();
        var newSel = GUILayout.SelectionGrid(selectedInt, names, xCols, options);
        names[selectedInt] = uncolored;
        var changed = selectedInt != newSel;
        if (changed) {
            if (newSel == 0) {
                isDefault = true;
            } else {
                isDefault = false;
                selected = (TEnum)vals.GetValue(newSel - 1);
            }
        }
        return changed;
    }
    public static bool SelectionGrid<TEnum>(ref TEnum selected, int xCols, Func<TEnum, string>? titler, params GUILayoutOption[] options) where TEnum : Enum {
        if (!m_EnumCache.TryGetValue(typeof(TEnum), out var vals)) {
            vals = Enum.GetValues(typeof(TEnum));
            m_EnumCache[typeof(TEnum)] = vals;
        }
        if (!m_EnumNameCache.TryGetValue(typeof(TEnum), out var names)) {
            Dictionary<object, int> indexToEnum = [];
            List<string> tmpNames = [];
            for (var i = 0; i < vals.Length; i++) {
                string name;
                var val = vals.GetValue(i);
                indexToEnum[val] = i;
                if (titler != null) {
                    name = titler((TEnum)val);
                } else {
                    name = Enum.GetName(typeof(TEnum), val);
                }
                tmpNames.Add(name);
            }
            names = [.. tmpNames];
            m_EnumNameCache[typeof(TEnum)] = names;
            m_IndexToEnumCache[typeof(TEnum)] = indexToEnum;
        }
        if (xCols <= 0) {
            xCols = vals.Length;
        }
        var selectedInt = m_IndexToEnumCache[typeof(TEnum)][selected];
        // Create a copy to not recolour the selected element permanently
        // names = [.. names];
        // Better idea: Just cache that one name and change it back after
        var uncolored = names[selectedInt];
        names[selectedInt] = uncolored.Orange();
        var newSel = GUILayout.SelectionGrid(selectedInt, names, xCols, options);
        names[selectedInt] = uncolored;
        var changed = selectedInt != newSel;
        if (changed) {
            selected = (TEnum)vals.GetValue(newSel);
        }
        return changed;
    }
    public static bool SelectionGrid<T>(ref T? selected, IList<T> vals, int xCols, Func<T, string>? titler, params GUILayoutOption[] options) where T : notnull {
        if (xCols <= 0) {
            xCols = vals.Count;
        }
        var selectedInt = selected != null ? vals.IndexOf(selected) + 1 : 0;
        string[] names = [SharedStrings.NoneText, .. vals.Select(x => {
            if (titler != null) {
                return titler(x);
            } else {
                return x.ToString();
            }
        })];
        names[selectedInt] = names[selectedInt].Orange();
        var newSel = GUILayout.SelectionGrid(selectedInt, names, xCols, options);
        var changed = selectedInt != newSel;
        if (changed) {
            if (newSel == 0) {
                selected = default;
            } else {
                selected = vals[newSel - 1];
            }
        }
        return changed;
    }
}
