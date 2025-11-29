using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.Infrastructure;

public static class LayoutHelper {
    public static bool PressedEnterInControl(string controlName) {
        var e = Event.current;

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == controlName) {
            e.Use();
            return true;
        }
        return false;
    }
    public static bool ImguiCanChangeStateAtBeginning() {
        return Event.current.type == EventType.Layout;
    }

    public static bool ImguiCanChangeStateAtEnd() {
        return Event.current.type == EventType.Repaint;
    }

    public static GUILayout.HorizontalScope HorizontalScope(params GUILayoutOption[] options) {
        return new(options);
    }

    public static GUILayout.HorizontalScope HorizontalScope(float width) {
        return new(GUILayout.Width(width));
    }

    public static GUILayout.HorizontalScope HorizontalScope(GUIStyle style, params GUILayoutOption[] options) {
        return new(style, options);
    }

    public static GUILayout.HorizontalScope HorizontalScope(GUIStyle style, float width) {
        return new(style, GUILayout.Width(width));
    }

    public static GUILayout.VerticalScope VerticalScope(params GUILayoutOption[] options) {
        return new(options);
    }

    public static GUILayout.VerticalScope VerticalScope(float width) {
        return new(GUILayout.Width(width));
    }

    public static GUILayout.VerticalScope VerticalScope(GUIStyle style, params GUILayoutOption[] options) {
        return new(style, options);
    }

    public static GUILayout.VerticalScope VerticalScope(GUIStyle style, float width) {
        return new(style, GUILayout.Width(width));
    }

    public static GUILayoutOption Width(float width) {
        return GUILayout.Width(width);
    }

    public static GUILayoutOption Height(float height) {
        return GUILayout.Height(height);
    }

    public static GUILayoutOption AutoWidth() {
        return GUILayout.ExpandWidth(false);
    }

    public static GUILayoutOption AutoHeight() {
        return GUILayout.ExpandHeight(false);
    }

    public static void Space(float pixels) {
        GUILayout.Space(pixels * Main.UIScale);
    }

    public static void UnscaledSpace(float pixels) {
        GUILayout.Space(pixels);
    }

    public static float EffectiveWindowWidth() {
        return 0.98f * UnityModManager.Params.WindowWidth;
    }
}
