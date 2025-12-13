using Kingmaker.Controllers.Rest;
using Kingmaker.View;

namespace ToyBox.Features.BagOfTricks.Camera;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.AllowRotateOnAllMapsAndCutscenesFeature")]
public partial class AllowRotateOnAllMapsAndCutscenesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableAllowRotateOnAllMapsAndCutscenes;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowRotateOnAllMapsAndCutscenesFeature_Name", "Enable Rotate on all maps and cutscenes")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowRotateOnAllMapsAndCutscenesFeature_Description", "Note: For cutscenes and some situations the rotation keys are disabled so you have to hold down Mouse3 to drag in order to get rotation")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.AllowRotateOnAllMapsAndCutscenesFeature";
        }
    }
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.Tick)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraController_Tick_Patch(IEnumerable<CodeInstruction> instructions) {
        var field = AccessTools.Field(typeof(CameraController), nameof(CameraController.m_AllowRotate));
        var foundField = false;
        foreach (var instruction in instructions) {
            if (instruction.LoadsField(field)) {
                yield return CodeInstruction.Call((object obj) => ReplacementTrue(obj)).WithLabels(instruction.labels);
                foundField = true;
            } else {
                yield return instruction;
            }
        }
        ThrowIfTrue(!foundField);
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraRig_TickRotate_Patch(IEnumerable<CodeInstruction> instructions) {
        var field = AccessTools.Field(typeof(CameraRig), nameof(CameraRig.m_HandRotationLock));
        var foundField = false;
        foreach (var instruction in instructions) {
            if (instruction.LoadsField(field)) {
                yield return CodeInstruction.Call((object obj) => ReplacementFalse(obj)).WithLabels(instruction.labels);
                foundField = true;
            } else {
                yield return instruction;
            }
        }
        ThrowIfTrue(!foundField);
    }
    private static bool ReplacementTrue(object _) {
        return true;
    }
    private static bool ReplacementFalse(object _) {
        return false;
    }
}
