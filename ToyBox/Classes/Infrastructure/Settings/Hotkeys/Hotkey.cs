using Newtonsoft.Json;
using System.ComponentModel;
using UnityEngine;

namespace ToyBox.Infrastructure.Keybinds;
[TypeConverter(typeof(HotkeyTypeConverter))]
public class Hotkey(KeyCode key, bool ctrl = false, bool shift = false, bool alt = false) {
    [JsonProperty]
    internal bool IsCtrl = ctrl;
    [JsonProperty]
    internal bool IsShift = shift;
    [JsonProperty]
    internal bool IsAlt = alt;
    [JsonProperty]
    internal KeyCode Key = key;
    public uint GetMask() {
        return (IsCtrl ? 4u : 0u)
             | (IsShift ? 2u : 0u)
             | (IsAlt ? 1u : 0u);
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
