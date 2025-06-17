﻿using UnityEngine;

namespace ToyBox.Infrastructure.UI;
public static partial class UI {
    public static bool Button(string? title = null, Action? onPressed = null, GUIStyle? style = null, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth()] : options;
        bool pressed = false;
        if (GUILayout.Button(title ?? "", style ?? GUI.skin.button, options)) {
            onPressed?.Invoke();
            pressed = true;
        }
        return pressed;
    }
}
