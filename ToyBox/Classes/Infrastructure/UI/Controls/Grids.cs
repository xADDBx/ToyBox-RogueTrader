﻿using Newtonsoft.Json;
using UnityEngine;

namespace ToyBox.Infrastructure.UI;
public static partial class UI {
    private static Dictionary<Type, Array> m_EnumCache = new();
    private static Dictionary<Type, Dictionary<object, int>> m_IndexToEnumCache = new();
    private static Dictionary<Type, string[]> m_EnumNameCache = new();
    public static bool SelectionGrid<TEnum>(ref TEnum selected, int xCols, Func<TEnum, string>? titler, params GUILayoutOption[] options) where TEnum : Enum {
        if (!m_EnumCache.TryGetValue(typeof(TEnum), out var vals)) {
            vals = Enum.GetValues(typeof(TEnum));
            m_EnumCache[typeof(TEnum)] = vals;
        }
        if (!m_EnumNameCache.TryGetValue(typeof(TEnum), out var names)) {
            Dictionary<object, int> indexToEnum = new();
            List<string> tmpNames = new();
            for (int i = 0; i < vals.Length; i++) {
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
        if (xCols < 0) {
            xCols = vals.Length;
        }
        var selectedInt = m_IndexToEnumCache[typeof(TEnum)][selected];
        // Create a copy to not recolour the selected element permanently
        names = [.. names];
        names[selectedInt] = names[selectedInt].Orange();
        var newSel = GUILayout.SelectionGrid(selectedInt, names, xCols, options);
        bool changed = selectedInt != newSel;
        if (changed) {
            selected = (TEnum)vals.GetValue(newSel);
        }
        return changed;
    }
}
