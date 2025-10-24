using UnityEngine;

namespace ToyBox.Infrastructure;
public static partial class UI {
    public static bool TextField(ref string content, Action<(string oldContent, string newContent)>? onContentChanged, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var hasChanged = false;
        var oldContent = content;
        var newText = GUILayout.TextField(oldContent, options);
        if (newText != oldContent) {
            content = newText;
            onContentChanged?.Invoke((oldContent, content));
            hasChanged = true;

        }
        return hasChanged;
    }
    // I'd like to make this generic
    // But implementations would probably either end up needing caching or
    // have a (probably negligible) overhead to parse an unspecified type
    public static bool TextField(ref int content, Action<(int oldContent, int newContent)>? onContentChanged, params GUILayoutOption[] options) {
        options = options.Length == 0 ? [AutoWidth(), Width(600)] : options;
        var hasChanged = false;
        var oldContent = content;
        var contentText = oldContent.ToString();
        var newText = GUILayout.TextField(contentText, options);
        if (newText != contentText && int.TryParse(newText, out var newContent)) {
            content = newContent;
            onContentChanged?.Invoke((oldContent, content));
            hasChanged = true;
        }
        return hasChanged;
    }
    public static bool ActionTextField(ref string content, string name, Action<(string oldContent, string newContent)>? onContentChanged, Action<string>? onEnterPressed, params GUILayoutOption[] options) {
        var hasChanged = false;
        if (name != null) {
            GUI.SetNextControlName(name);
        }
        hasChanged = TextField(ref content, onContentChanged, options);
        if (name != null && onEnterPressed != null && PressedEnterInControl(name)) {
            onEnterPressed(content);
        }
        return hasChanged;
    }
}
