﻿using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.Infrastructure;
public static class LayoutHelper {
    public static bool PressedEnterInControl(string controlName) {
        Event e = Event.current;

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == controlName) {
            e.Use();
            return true;
        }
        return false;
    }
    public static bool ImguiCanChangeStateAtBeginning() => Event.current.type == EventType.Layout;
    public static bool ImguiCanChangeStateAtEnd() => Event.current.type == EventType.Repaint;
    public static GUILayout.HorizontalScope HorizontalScope(params GUILayoutOption[] options) => new(options);
    public static GUILayout.HorizontalScope HorizontalScope(float width) => new(GUILayout.Width(width));
    public static GUILayout.HorizontalScope HorizontalScope(GUIStyle style, params GUILayoutOption[] options) => new(style, options);
    public static GUILayout.HorizontalScope HorizontalScope(GUIStyle style, float width) => new(style, GUILayout.Width(width));
    public static GUILayout.VerticalScope VerticalScope(params GUILayoutOption[] options) => new(options);
    public static GUILayout.VerticalScope VerticalScope(float width) => new(GUILayout.Width(width));
    public static GUILayout.VerticalScope VerticalScope(GUIStyle style, params GUILayoutOption[] options) => new(style, options);
    public static GUILayout.VerticalScope VerticalScope(GUIStyle style, float width) => new(style, GUILayout.Width(width));
    public static GUILayoutOption Width(float width) => GUILayout.Width(width);
    public static GUILayoutOption Height(float height) => GUILayout.Height(height);
    public static GUILayoutOption AutoWidth() => GUILayout.ExpandWidth(false);
    public static GUILayoutOption AutoHeight() => GUILayout.ExpandHeight(false);
    public static void Space(float pixels) => GUILayout.Space(pixels);
    public static float EffectiveWindowWidth() => 0.98f * UnityModManager.Params.WindowWidth;
}
