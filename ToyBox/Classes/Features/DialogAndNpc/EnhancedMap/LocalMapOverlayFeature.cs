using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public static partial class MinimapRotation_Localizer {
    public static string GetLocalized(this MinimapRotation rotation) => rotation switch {
        MinimapRotation.HeadingUp => m_HeadingUpText,
        MinimapRotation.NorthUp => m_NorthUpText,
        _ => "!!Error Unknown MinimapRotation!!",
    };

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MinimapRotation_HeadingUpText", "Heading Up (map rotates)")]
    private static partial string m_HeadingUpText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MinimapRotation_NorthUpText", "North Up (locked)")]
    private static partial string m_NorthUpText { get; }
}

public static partial class MapOverlayCorner_Localizer {
    public static string GetLocalized(this MapOverlayCorner corner) => corner switch {
        MapOverlayCorner.TopRight => m_TopRightText,
        MapOverlayCorner.TopLeft => m_TopLeftText,
        MapOverlayCorner.BottomRight => m_BottomRightText,
        MapOverlayCorner.BottomLeft => m_BottomLeftText,
        _ => "!!Error Unknown MapOverlayCorner!!",
    };

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MapOverlayCorner_TopRightText", "Top Right")]
    private static partial string m_TopRightText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MapOverlayCorner_TopLeftText", "Top Left")]
    private static partial string m_TopLeftText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MapOverlayCorner_BottomRightText", "Bottom Right")]
    private static partial string m_BottomRightText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_MapOverlayCorner_BottomLeftText", "Bottom Left")]
    private static partial string m_BottomLeftText { get; }
}

/// <summary>
/// Local map minimap overlay : a small D4-style minimap of the
/// current area drawn over normal gameplay. Default HeadingUp spins the map
/// with the player's heading (arrow fixed, north mark on the crop rim);
/// NorthUp locks the map and rotates the arrow instead. Renders through the
/// harvested vanilla fullscreen-map material, so fog of war shading is
/// inherited; v1 shows no markers, so nothing beyond vanilla map knowledge
/// is revealed. Auto-hides during cutscenes and while any fullscreen window
/// (including the local map itself) is open; input passes through the
/// overlay (no raycaster). The fullscreen heading-up rotation feature and
/// the map window opacity feature from v9-v11 were removed in favour of
/// this overlay.
/// </summary>
[IsTested]
public partial class LocalMapOverlayFeature : ToggledFeature, IToggledWithBinding {
    public Hotkey? Keybind {
        get;
        set;
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLocalMapOverlay;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_Name", "Local Map Minimap Overlay")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_Description", "Shows a small corner minimap of the current area over normal gameplay. By default the map rotates with your heading and the arrow stays fixed; cutscenes and fullscreen windows hide it automatically. Clicks pass through the overlay.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_m_RotationText", "Minimap rotation")]
    private static partial string m_RotationText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_m_OpacityText", "Opacity (0.2-1.0)")]
    private static partial string m_OpacityText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_m_ZoomText", "Zoom (1-4; higher = tighter)")]
    private static partial string m_ZoomText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapOverlayFeature_m_CornerText", "Screen corner")]
    private static partial string m_CornerText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            // ToggledFeature.OnGui renders the checkbox + hotkey picker row.
            base.OnGui();
            if (IsEnabled) {
                using (HorizontalScope()) {
                    Space(50);
                    UI.Label(m_RotationText);
                    Space(10);
                    UI.SelectionGrid(ref Settings.MinimapRotation, 2, e => e.GetLocalized(), Width(0.4f * EffectiveWindowWidth()));
                }
                using (HorizontalScope()) {
                    Space(50);
                    _ = UI.Slider(ref Settings.OverlayOpacity, LocalMapOverlayMath.MinOpacity, LocalMapOverlayMath.MaxOpacity, LocalMapOverlayMath.DefaultOpacity, 2, OnSettingChanged, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
                    Space(10);
                    UI.Label(m_OpacityText);
                }
                using (HorizontalScope()) {
                    Space(50);
                    _ = UI.Slider(ref Settings.OverlayZoom, LocalMapOverlayMath.MinZoom, LocalMapOverlayMath.MaxZoom, LocalMapOverlayMath.DefaultZoom, 2, OnSettingChanged, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
                    Space(10);
                    UI.Label(m_ZoomText);
                }
                using (HorizontalScope()) {
                    Space(50);
                    UI.Label(m_CornerText);
                    Space(10);
                    UI.SelectionGrid(ref Settings.OverlayCorner, 4, e => e.GetLocalized(), Width(0.5f * EffectiveWindowWidth()));
                }
            }
            SyncRuntimeWithSettings();
        }
    }

    // Slider callbacks fire on user drags; the grids/checkbox paths are
    // caught by the last-seen sync below. Both converge on ApplySettings,
    // which is a no-op while the feature is off (Enable applies on boot).
    private void OnSettingChanged((float oldValue, float newValue) values) {
        if (IsEnabled && values.newValue != values.oldValue) {
            LocalMapOverlayRuntime.ApplySettings();
        }
    }

    private void SyncRuntimeWithSettings() {
        if (!IsEnabled) {
            return;
        }
        LocalMapOverlayRuntime.ApplySettings();
    }

    public override void Enable() {
        // The UMM checkbox binds ref Settings.EnableLocalMapOverlay: turning
        // it on must stick even though the checkbox already flipped the bool.
        Settings.EnableLocalMapOverlay = true;
        Keybind ??= Hotkeys.MaybeGetHotkey(GetType());
        LocalMapOverlayRuntime.Enable();
    }

    public override void Disable() {
        Settings.EnableLocalMapOverlay = false;
        LocalMapOverlayRuntime.Disable();
    }

    // Hotkey path (Hotkeys.UpdateLoop -> IBindableFeature.ExecuteAction):
    // flip the persisted toggle and run the same enable/disable work the
    // checkbox would.
    public void ExecuteAction(ActionParameter parameter) {
        LogExecution(parameter);
        if (IsEnabled) {
            Disable();
        } else {
            Enable();
        }
    }

    public void LogExecution(ActionParameter parameter) {
        Helpers.LogExecution(this, parameter);
    }
}
