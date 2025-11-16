using Kingmaker.View;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.Camera;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.CameraElevationOffsetFeature")]
public partial class CameraElevationOffsetFeature : FeatureWithPatch {
    private static bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.CameraElevationOffset != 0f;
            return ref m_IsEnabled;
        }
    }
    public ref float Value {
        get {
            return ref Settings.CameraElevationOffset;
        }
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Slider(ref Value, -10f, 100f, 0f, 0, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
                if (IsEnabled) {
                    Initialize();
                } else {
                    Destroy();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_CameraElevationOffsetFeature_Name", "Modify Camera Elevation Offset")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_CameraElevationOffsetFeature_Description", "Adds a flat modifier to the height of the camera above the ground.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.CameraElevationOffsetFeature";
        }
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.PlaceOnGround2)), HarmonyPostfix, HarmonyPriority(Priority.HigherThanNormal)]
    private static void CameraRig_PlaceOnGround2_Patch(ref Vector3 __result) {
        __result.y += Settings.CameraElevationOffset;
    }
}
