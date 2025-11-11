using System.ComponentModel;
using UnityEngine;

namespace ToyBox.Infrastructure.Keybinds;

public class HotkeyTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) {
        if (value is string s) {
            return Parse(s);
        }
        return base.ConvertFrom(context, culture, value);
    }
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType) {
        if (destinationType == typeof(string) && value is Hotkey h) {
            var result = "";
            if (h.IsAlt) {
                result += "Alt + ";
            }
            if (h.IsCtrl) {
                result += "Ctrl + ";
            }
            if (h.IsShift) {
                result += "Shift + ";
            }
            if (h.IsPseudo) {
                result += "Pseudo + ";
            }
            result += h.Key.ToString();
            return result;
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
    private static Hotkey? Parse(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }
        var parts = value!.Split(['+'], StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();

        var ctrl = parts.Any(p => p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase));
        var shift = parts.Any(p => p.Equals("Shift", StringComparison.OrdinalIgnoreCase));
        var alt = parts.Any(p => p.Equals("Alt", StringComparison.OrdinalIgnoreCase));
        var pseudo = parts.Any(p => p.Equals("Pseudo", StringComparison.OrdinalIgnoreCase));


        _ = Enum.TryParse<KeyCode>(parts.Last(), true, out var key);

        return new Hotkey(key, ctrl, shift, alt, pseudo);
    }
}
