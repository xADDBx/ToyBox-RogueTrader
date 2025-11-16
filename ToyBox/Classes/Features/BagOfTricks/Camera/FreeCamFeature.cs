using Kingmaker.View;

namespace ToyBox.Features.BagOfTricks.Camera;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.FreeCamFeature")]
public partial class FreeCamFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableFreeCam;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_FreeCamFeature_Name", "Free Camera")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_FreeCamFeature_Description", "Makes the game no longer force the camera to ground.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.FreeCamFeature";
        }
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.ClampByLevelBounds)), HarmonyPostfix]
    private static void CameraRig_ClampByLevelBounds_Patch(ref UnityEngine.Vector3 __result, UnityEngine.Vector3 point) {
        __result = point;
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.PlaceOnGround)), HarmonyPostfix]
    private static void CameraRig_PlaceOnGround_Patch(ref UnityEngine.Vector3 __result, UnityEngine.Vector3 pos) {
        __result = pos;
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.LowerGently)), HarmonyPostfix]
    private static void CameraRig_LowerGently_Patch(ref UnityEngine.Vector3 __result, UnityEngine.Vector3 prevPos) {
        __result = prevPos;
    }
}
