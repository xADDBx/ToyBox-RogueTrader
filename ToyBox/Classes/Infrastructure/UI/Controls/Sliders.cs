using UnityEngine;

namespace ToyBox.Infrastructure;

public static partial class UI {
    public static bool Slider(ref int value, float minValue, float maxValue, int? defaultValue = null, Action<(int oldValue, int newValue)>? onValueChanged = null, int? valueLabelWidth = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var oldValue = value;
        var result = (int)Math.Round(GUILayout.HorizontalSlider(oldValue, minValue, maxValue, options), 0);
        if (valueLabelWidth.HasValue) {
            Label(value.ToString("F0").Orange() + " ", Width(valueLabelWidth.Value));
        } else {
            Label(value.ToString("F0").Orange() + " ");
        }
        if (defaultValue != null) {
            Space(4);
            _ = Button(SharedStrings.ResetToDefault, () => {
                result = defaultValue.Value;
            });
        }
        if (result != value) {
            value = result;
            onValueChanged?.Invoke((oldValue, value));
            return true;
        }
        return false;
    }
    public static bool Slider(ref float value, float minValue, float maxValue, float? defaultValue = null, int digits = 2, Action<(float oldValue, float newValue)>? onValueChanged = null, int? valueLabelWidth = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var oldValue = value;
        var result = (float)Math.Round(GUILayout.HorizontalSlider(oldValue, minValue, maxValue, options), digits);
        if (valueLabelWidth.HasValue) {
            Label(value.ToString("F").Orange() + " ", Width(valueLabelWidth.Value));
        } else {
            Label(value.ToString("F").Orange() + " ");
        }
        if (defaultValue != null) {
            Space(4);
            _ = Button(SharedStrings.ResetToDefault, () => {
                result = defaultValue.Value;
            });
        }
        if (result != value) {
            value = result;
            onValueChanged?.Invoke((oldValue, value));
            return true;
        }
        return false;
    }
    public static bool LogSlider(ref int value, float minValue, float maxValue, int? defaultValue = null, Action<(int oldValue, int newValue)>? onValueChanged = null, int? valueLabelWidth = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var oldValue = value;
        // Log(0) is bad; so shift to positive
        double offset = minValue + 1;

        var logValue = 100f * (float)Math.Log10(value + offset);
        var logMin = 100f * (float)Math.Log10(minValue + offset);
        var logMax = 100f * (float)Math.Log10(maxValue + offset);

        var logResult = GUILayout.HorizontalSlider(logValue, logMin, logMax, options);
        var result = (int)Math.Round(Math.Pow(10, logResult / 100f) - offset, 0);
        if (valueLabelWidth.HasValue) {
            Label(value.ToString("F0").Orange() + " ", Width(valueLabelWidth.Value));
        } else {
            Label(value.ToString("F0").Orange() + " ");
        }
        if (defaultValue != null) {
            Space(4);
            _ = Button(SharedStrings.ResetToDefault, () => {
                result = defaultValue.Value;
            });
        }
        if (Math.Abs(result - value) > float.Epsilon) {
            value = result;
            onValueChanged?.Invoke((oldValue, value));
            return true;
        }
        return false;
    }
    public static bool LogSlider(ref float value, float minValue, float maxValue, float? defaultValue = null, int digits = 2, Action<(float oldValue, float newValue)>? onValueChanged = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var oldValue = value;
        // Log(0) is bad; so shift to positive
        double offset = minValue + 1;

        var logValue = 100f * (float)Math.Log10(value + offset);
        var logMin = 100f * (float)Math.Log10(minValue + offset);
        var logMax = 100f * (float)Math.Log10(maxValue + offset);

        var logResult = GUILayout.HorizontalSlider(logValue, logMin, logMax, options);
        var result = (float)Math.Round(Math.Pow(10, logResult / 100f) - offset, digits);
        Label(value.ToString("F").Orange() + " ");
        if (defaultValue != null) {
            Space(4);
            _ = Button(SharedStrings.ResetToDefault, () => {
                result = defaultValue.Value;
            });
        }
        if (Math.Abs(result - value) > float.Epsilon) {
            value = result;
            onValueChanged?.Invoke((oldValue, value));
            return true;
        }
        return false;
    }
}
