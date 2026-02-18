using Kingmaker.View;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.Camera;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.FOVMultiplierFeature")]
public partial class FOVMultiplierFeature : FeatureWithPatch {
    private static bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.FOVMultiplierSetting != 1f;
            return ref m_IsEnabled;
        }
    }
    public ref float Value {
        get {
            return ref Settings.FOVMultiplierSetting;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_FOVMultiplierFeature_Name", "Field Of View")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_FOVMultiplierFeature_Description", "Changes the min/max FoV (you can scroll further in and out)")]
    public override partial string Description { get; }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Slider(ref Value, 0.4f, 5f, 1f, 2, null, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
                if (IsEnabled) {
                    Enable();
                } else {
                    Disable();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.FOVMultiplierFeature";
        }
    }
    [HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.FovMin), MethodType.Getter), HarmonyPostfix]
    private static void CameraZoom_getPhysicalZoomMin_Patch(ref float __result) {
        __result /= GetInstance<FOVMultiplierFeature>().Value;
    }
    [HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.FovMax), MethodType.Getter), HarmonyPostfix]
    private static void CameraZoom_getPhysicalZoomMax_Patch(ref float __result) {
        __result *= GetInstance<FOVMultiplierFeature>().Value;
    }
    [HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.TickZoom)), HarmonyPrefix]
    private static void NoInlining() { }
}
