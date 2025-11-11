using UnityEngine;

namespace ToyBox.Infrastructure;

public static partial class UI {
    private static GUIStyle m_TextBoxStyle {
        get {
            field ??= new GUIStyle(GUI.skin.box) {
                richText = true
            };
            return field;
        }
    }
    private static GUIStyle m_CachedProgressBarStyle {
        get {
            if (field == null) {
                field = new GUIStyle(GUI.skin.box);
                var greenTexture = new Texture2D(1, 1);
                greenTexture.SetPixel(0, 0, Color.green);
                greenTexture.Apply();
                field.normal.background = greenTexture;
            }
            return field;
        }
    }
    public static void ProgressBar(double progress, string label) {
        Label(label + $" {progress * 100:F2}%");
        var progressRect = GUILayoutUtility.GetRect(200, 20);
        GUI.Box(progressRect, "");

        var fillWidth = (float)(progress * progressRect.width);
        var fillRect = new Rect(progressRect.x, progressRect.y, fillWidth, progressRect.height);

        GUI.Box(fillRect, GUIContent.none, m_CachedProgressBarStyle);
    }
    public static bool ValueAdjuster(ref int value, int increment = 1, int min = 0, int max = int.MaxValue, bool showMinMax = true) {
        var v = value;
        if (v > min) {
            _ = Button(" < ", () => { v = Math.Max(v - increment, min); }, m_TextBoxStyle, AutoWidth());
        } else if (showMinMax) {
            Space(-21);
            _ = Button(SharedStrings.MinText.Cyan() + " ", () => { }, m_TextBoxStyle, AutoWidth());
        } else {
            Space(34);
        }
        GUILayout.Label($"{v}".Orange().Bold(), m_TextBoxStyle, AutoWidth());
        if (v < max) {
            _ = Button(" > ", () => { v = Math.Min(v + increment, max); }, m_TextBoxStyle, AutoWidth());
        } else if (showMinMax) {
            _ = Button(" " + SharedStrings.MaxText.Cyan(), () => { }, m_TextBoxStyle, AutoWidth());
            Space(-27);
        } else {
            Space(34);
        }

        if (v != value) {
            value = v;
            return true;
        }
        return false;
    }
}
