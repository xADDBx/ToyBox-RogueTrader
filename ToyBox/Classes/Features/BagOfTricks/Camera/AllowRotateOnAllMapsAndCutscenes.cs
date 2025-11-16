using Kingmaker.Controllers.Rest;
using Kingmaker.View;

namespace ToyBox.Features.BagOfTricks.Camera;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.AllowRotateOnAllMapsAndCutscenes")]
public partial class AllowRotateOnAllMapsAndCutscenes : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableAllowRotateOnAllMapsAndCutscenes;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowRotateOnAllMapsAndCutscenes_Name", "Enable Rotate on all maps and cutscenes")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowRotateOnAllMapsAndCutscenes_Description", "Makes CameraController always allow rotate and ignore m_HandRotationLock.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.AllowRotateOnAllMapsAndCutscenes";
        }
    }
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.Tick)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraController_Tick_Patch(IEnumerable<CodeInstruction> instructions) {
        var field = AccessTools.Field(typeof(CameraController), nameof(CameraController.m_AllowRotate));
        foreach (var instruction in instructions) {
            if (instruction.LoadsField(field)) {
                yield return CodeInstruction.Call((object obj) => ReplacementTrue(obj)).WithLabels(instruction.labels);
            } else {
                yield return instruction;
            }
        }
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraRig_TickRotate_Patch(IEnumerable<CodeInstruction> instructions) {
        var field = AccessTools.Field(typeof(CameraRig), nameof(CameraRig.m_HandRotationLock));
        foreach (var instruction in instructions) {
            if (instruction.LoadsField(field)) {
                yield return CodeInstruction.Call((object obj) => ReplacementFalse(obj)).WithLabels(instruction.labels);
            } else {
                yield return instruction;
            }
        }
    }
    private static bool ReplacementTrue(object _) {
        return true;
    }
    private static bool ReplacementFalse(object _) {
        return false;
    }
}
