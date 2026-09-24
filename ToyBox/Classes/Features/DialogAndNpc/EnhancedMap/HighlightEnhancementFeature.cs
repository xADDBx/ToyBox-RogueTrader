using Kingmaker.Controllers.MapObjects;
using Owlcat.Runtime.Visual.Highlighting;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Highlight enhancement (R6, dual knob): (a) an outline thickness slider
/// that scales the HighlightingFeature's public blur tuning fields and (b)
/// the fill ladder - rung 1 lives here as alpha clamp-up prefixes on
/// Highlighter.ConstantOn/ConstantOnImmediate; rungs 2/3 live in
/// HighlightFillRuntime behind the FillRung constant.
///
/// The game methods take Color by value; the ref modifier exists only on the
/// patch side to intercept the argument before it is consumed into private
/// state. Units and map objects both funnel into these two methods, so one
/// patch pair covers both families. FlashingOn (ping/hidden-object flash)
/// bypasses ConstantOn and is NOT filled by rung 1.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.HighlightEnhancementFeature")]
public partial class HighlightEnhancementFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;
    private static int s_Failures;

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.HighlightEnhancementFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableHighlightFill;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_HighlightEnhancementFeature_Name", "Highlight Enhancement (Fill & Outline)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_HighlightEnhancementFeature_Description", "Fill: interaction highlights render as filled silhouettes instead of outlines (quick flashes such as pings keep their outline). Thickness: scales the outline blur, 1 = vanilla.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_HighlightEnhancementFeature_m_ThicknessText", "Outline Thickness (1 = vanilla)")]
    private static partial string m_ThicknessText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
            if (IsEnabled) {
                // The slider is part of the feature: hidden (and inert) while
                // the toggle is OFF, which now also restores the pipeline.
                using (HorizontalScope()) {
                    Space(50);
                    _ = UI.Slider(ref Settings.HighlightOutlineThickness, 1f, 4f, 1f, 1, OnThicknessChanged, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
                    Space(10);
                    UI.Label(m_ThicknessText);
                }
            }
        }
    }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the strike counter: only consecutive failures may
        // auto-disable.
        s_Failures = 0;
        HighlightFillRuntime.SyncFromSettings();
    }

    public override void Disable() {
        base.Disable();
        // Both knobs restore their full snapshot: the three blur fields and
        // the cut material state.
        HighlightFillRuntime.SyncFromSettings();
    }

    private void OnThicknessChanged((float oldValue, float newValue) values) {
        HighlightFillRuntime.SyncFromSettings();
    }

    // Rung 1: clamp the alpha UP only, never invert. A color arriving with
    // a<=0 would make GetRendererInfos return null and kill the highlight.
    [HarmonyPatch(typeof(Highlighter), nameof(Highlighter.ConstantOn)), HarmonyPrefix]
    private static void Highlighter_ConstantOn_Prefix(ref Color color) {
        try {
            if (Settings.EnableHighlightFill && HighlightFillRuntime.FillRung == 1) {
                color.a = Mathf.Max(color.a, HighlightFillRuntime.FillFloorAlpha);
            }
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    [HarmonyPatch(typeof(Highlighter), nameof(Highlighter.ConstantOnImmediate)), HarmonyPrefix]
    private static void Highlighter_ConstantOnImmediate_Prefix(ref Color color) {
        try {
            if (Settings.EnableHighlightFill && HighlightFillRuntime.FillRung == 1) {
                color.a = Mathf.Max(color.a, HighlightFillRuntime.FillFloorAlpha);
            }
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Lazy re-apply: HighlightingFeature.Create() rebuilds all materials on
    // pipeline reload (graphics setting change); re-locating on every
    // highlight-on keeps thickness/fill in sync without user action.
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.HighlightOn)), HarmonyPostfix]
    private static void InteractionHighlightController_HighlightOn_Postfix() {
        try {
            HighlightFillRuntime.SyncFromSettings();
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: highlight patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling HighlightEnhancementFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<HighlightEnhancementFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
