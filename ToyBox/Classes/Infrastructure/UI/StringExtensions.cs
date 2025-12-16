using System.Text;
using UnityEngine;

namespace ToyBox.Infrastructure;

public static class StringExtensions {
    public static string Size(this string s, int size) {
        return $"<size={size}>{s}</size>";
    }

    public static string Bold(this string s) {
        return $"<b>{s}</b>";
    }

    public static string Color(this string s, string color) {
        return $"<color={color}>{s}</color>";
    }

    public static string Color(this string str, Color color) {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
    }

    public static string White(this string s) {
        return s.Color("white");
    }

    public static string Grey(this string s) {
        return s.Color("#A0A0A0FF");
    }

    public static string DarkGrey(this string s) {
        return s.Color("#505050FF");
    }

    public static string Red(this string s) {
        return s.Color("#C04040E0");
    }

    public static string Green(this string s) {
        return s.Color("#00ff00ff");
    }

    public static string Blue(this string s) {
        return s.Color("blue");
    }

    public static string Cyan(this string s) {
        return s.Color("cyan");
    }

    public static string Magenta(this string s) {
        return s.Color("magenta");
    }

    public static string Aqua(this string s) {
        return s.Color("#00FEFEFE");
    }

    public static string Yellow(this string s) {
        return s.Color("yellow");
    }

    public static string Orange(this string s) {
        return s.Color("#f99245");
    }

    public static string SizePercent(this string s, int percent) {
        return $"<size={percent}%>{s}</size>";
    }
    public static string MakeRainbow(this string input, float offset = 0f) {
        if (string.IsNullOrEmpty(input)) {
            return input;
        }

        var sb = new StringBuilder();
        var len = input.Length;
        for (var i = 0; i < len; i++) {
            var hue = ((float)i / Mathf.Max(1, len) + offset) % 1f;
            var c = UnityEngine.Color.HSVToRGB(hue, 1, 1);
            var hex = ColorUtility.ToHtmlStringRGB(c);
            sb.AppendFormat("<color=#{0}>{1}</color>", hex, input[i]);
        }
        return sb.ToString();
    }
}
