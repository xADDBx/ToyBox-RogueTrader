﻿using UnityEngine;

namespace ToyBox.Infrastructure;
public static partial class UI {
    private static GUIStyle m_TextBoxStyle {
        get {
            if (field == null) {
                field = new GUIStyle(GUI.skin.box) {
                    richText = true
                };
            }
            return field;
        }
    }
    private static GUIStyle m_CachedProgressBarStyle {
        get {
            if (field == null) {
                field = new GUIStyle(GUI.skin.box);
                Texture2D greenTexture = new Texture2D(1, 1);
                greenTexture.SetPixel(0, 0, Color.green);
                greenTexture.Apply();
                field.normal.background = greenTexture;
            }
            return field;
        }
    }
    public static void ProgressBar(double progress, string label) {
        Label(label + $" {progress * 100:F2}%");
        Rect progressRect = GUILayoutUtility.GetRect(200, 20);
        GUI.Box(progressRect, "");

        float fillWidth = (float)(progress * progressRect.width);
        Rect fillRect = new Rect(progressRect.x, progressRect.y, fillWidth, progressRect.height);

        GUI.Box(fillRect, GUIContent.none, m_CachedProgressBarStyle);
    }
    public static bool ValueAdjuster(ref int value, int increment = 1, int min = 0, int max = int.MaxValue, bool showMinMax = true) {
        var v = value;
        if (v > min)
            Button(" < ", () => { v = Math.Max(v - increment, min); }, m_TextBoxStyle, AutoWidth());
        else if (showMinMax) {
            Space(-21);
            Button(SharedStrings.MinText.Cyan() + " ", () => { }, m_TextBoxStyle, AutoWidth());
        } else {
            Space(34);
        }
        GUILayout.Label($"{v}".Orange().Bold(), m_TextBoxStyle, AutoWidth());
        if (v < max)
            Button(" > ", () => { v = Math.Min(v + increment, max); }, m_TextBoxStyle, AutoWidth());
        else if (showMinMax) {
            Button(" " + SharedStrings.MaxText.Cyan(), () => { }, m_TextBoxStyle, AutoWidth());
            Space(-27);
        } else
            Space(34);
        if (v != value) {
            value = v;
            return true;
        }
        return false;
    }
}
