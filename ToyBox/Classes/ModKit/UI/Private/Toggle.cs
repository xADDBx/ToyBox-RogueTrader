using UnityEngine;

namespace ModKit.Private {
    public static partial class UI {
        // Helper functionality.

        public static readonly GUIContent _LabelContent = new();
        public static readonly GUIContent CheckOnContent = new(CheckGlyphOn);
        public static readonly GUIContent CheckOffContent = new(CheckGlyphOff);
        public static readonly GUIContent DisclosureOnContent = new(DisclosureGlyphOn);
        public static readonly GUIContent DisclosureOffContent = new(DisclosureGlyphOff);
        public static readonly GUIContent DisclosureEmptyContent = new(DisclosureGlyphEmpty);
        private static GUIContent LabelContent(string? text) {
            _LabelContent.text = text;
            _LabelContent.image = null;
            _LabelContent.tooltip = null;
            return _LabelContent;
        }

        private static readonly int s_ButtonHint = "MyGUI.Button".GetHashCode();

        public static bool Toggle(Rect rect, GUIContent label, bool value, bool isEmpty, GUIContent on, GUIContent off, GUIStyle stateStyle, GUIStyle labelStyle) {
            var controlID = GUIUtility.GetControlID(s_ButtonHint, FocusType.Passive, rect);
            var result = false;
            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (GUI.enabled && rect.Contains(Event.current.mousePosition)) {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) Event.current.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        GUIUtility.hotControl = 0;

                        if (rect.Contains(Event.current.mousePosition)) {
                            result = true;
                            Event.current.Use();
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == controlID)
                        if (Event.current.keyCode == KeyCode.Escape) {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                        }
                    break;

                case EventType.Repaint: {
                        var rightAlign = stateStyle.alignment == TextAnchor.MiddleRight
                                         || stateStyle.alignment == TextAnchor.UpperRight
                                         || stateStyle.alignment == TextAnchor.LowerRight
                            ;
                        // stateStyle.alignment determines position of state element
                        var state = isEmpty
                                        ? DisclosureEmptyContent
                                        : value
                                            ? on
                                            : off;
                        var stateSize = stateStyle.CalcSize(value ? on : off); // don't use the empty content to calculate size so titles line up in lists
                        var x = rightAlign ? rect.xMax - stateSize.x : rect.x;
                        Rect stateRect = new(x, rect.y, stateSize.x, stateSize.y);

                        // layout state before or after following alignment
                        var labelSize = labelStyle.CalcSize(label);
                        x = rightAlign ? stateRect.x - stateSize.x - 5 : stateRect.xMax + 5;
                        Rect labelRect = new(x, rect.y, labelSize.x, labelSize.y);

                        stateStyle.Draw(stateRect, state, controlID);
                        labelStyle.Draw(labelRect, label, controlID);
                    }
                    break;
            }
            return result;
        }

        // Button Control - Layout Version

        public static bool Toggle(GUIContent label, bool value, GUIContent on, GUIContent off, GUIStyle stateStyle, GUIStyle labelStyle, bool isEmpty = false, params GUILayoutOption[] options) {
            var state = value ? on : off;
            var sStyle = new GUIStyle(stateStyle);
            var lStyle = new GUIStyle(labelStyle) {
                wordWrap = false
            };
            var stateSize = sStyle.CalcSize(state);
            lStyle.fixedHeight = stateSize.y - 2;
            var padding = new RectOffset(0, (int)stateSize.x + 5, 0, 0);
            lStyle.padding = padding;
            var rect = GUILayoutUtility.GetRect(label, lStyle, options);
            return Toggle(rect, label, value, isEmpty, on, off, stateStyle, labelStyle);
        }
        public static bool Toggle(string? label, bool value, string? on, string? off, GUIStyle stateStyle, GUIStyle labelStyle, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, new GUIContent(on), new GUIContent(off), stateStyle, labelStyle, false, options);
        // Disclosure Toggles
        public static bool DisclosureToggle(GUIContent label, bool value, bool isEmpty = false, params GUILayoutOption[] options) => Toggle(label, value, DisclosureOnContent, DisclosureOffContent, GUI.skin.textArea, GUI.skin.label, isEmpty, options);
        public static bool DisclosureToggle(string? label, bool value, GUIStyle stateStyle, GUIStyle labelStyle, bool isEmpty = false, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, DisclosureOnContent, DisclosureOffContent, stateStyle, labelStyle, isEmpty, options);
        public static bool DisclosureToggle(string? label, bool value, bool isEmpty = false, params GUILayoutOption[] options) => DisclosureToggle(label, value, GUI.skin.box, GUI.skin.label, isEmpty, options);
        // CheckBox 
        public static bool CheckBox(GUIContent label, bool value, bool isEmpty, params GUILayoutOption[] options) => Toggle(label, value, CheckOnContent, CheckOffContent, GUI.skin.textArea, GUI.skin.label, isEmpty, options);

        public static bool CheckBox(string? label, bool value, bool isEmpty, GUIStyle style, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, CheckOnContent, CheckOffContent, GUI.skin.box, style, isEmpty, options);

        public static bool CheckBox(string? label, bool value, bool isEmpty, params GUILayoutOption[] options) => CheckBox(label, value, isEmpty, GUI.skin.label, options);
    }
}