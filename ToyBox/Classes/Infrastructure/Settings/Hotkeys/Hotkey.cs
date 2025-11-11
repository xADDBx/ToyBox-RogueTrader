using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;

namespace ToyBox.Infrastructure.Keybinds;

[TypeConverter(typeof(HotkeyTypeConverter))]
public class Hotkey(KeyCode key, bool ctrl = false, bool shift = false, bool alt = false, bool isPseudo = false) {
    [JsonProperty]
    internal bool IsCtrl = ctrl;
    [JsonProperty]
    internal bool IsShift = shift;
    [JsonProperty]
    internal bool IsAlt = alt;
    [JsonProperty]
    internal KeyCode Key = key;
    [JsonProperty]
    public bool IsPseudo = isPseudo;
    private uint? m_PrecomputedMask;
    private uint InternalGetMask() {
        return (IsCtrl ? 4u : 0u)
             | (IsShift ? 2u : 0u)
             | (IsAlt ? 1u : 0u);
    }
    public uint GetMask() {
        if (m_PrecomputedMask.HasValue) {
            return m_PrecomputedMask.Value;
        } else {
            return InternalGetMask();
        }
    }
    public void Precompute() {
        m_PrecomputedMask = InternalGetMask();
    }
    public bool IsActive(uint currentMask) {
        if (currentMask != GetMask()) {
            return false;
        }
        return Key == KeyCode.None || Input.GetKeyDown(Key);
    }
    public override string ToString() {
        var result = "";
        if (IsAlt) {
            result += "Alt + ";
        }
        if (IsCtrl) {
            result += "Ctrl + ";
        }
        if (IsShift) {
            result += "Shift + ";
        }
        result += Key.ToString();
        return result;
    }
}
