using Kingmaker.View;
using System.Reflection.Emit;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.Camera;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.DragCameraElevationFeature")]
public partial class DragCameraElevationFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableDragCameraElevation;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_DragCameraElevationFeature_Name", "Press CTRL while rotating camera to change height")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_DragCameraElevationFeature_Description", "CTRL + Mouse3 to adjust camera height")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.DragCameraElevationFeature";
        }
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraRig_TickRotate_Patch(IEnumerable<CodeInstruction> instructions) {
        var field = AccessTools.Field(typeof(CameraRig), nameof(CameraRig.m_RotationByKeyboard));
        var foundField = false;
        var insts = instructions.ToArray();
        for (var i = 0; i < insts.Length - 2; i++) {
            if (insts[i].LoadsField(field) && insts[i + 1].opcode == OpCodes.Brfalse && insts[i + 2].opcode == OpCodes.Newobj) {
                yield return insts[i];
                yield return insts[i + 1];
                yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(insts[i + 2].ExtractLabels());
                yield return new(OpCodes.Ldloc_0);
                yield return CodeInstruction.Call((CameraRig camera, Vector2 vec) => MaybeChangeHeight(camera, vec));
                yield return new CodeInstruction(OpCodes.Brtrue, insts[i + 1].operand);
                yield return insts[i + 2];
                i += 2;
                foundField = true;
            } else {
                yield return insts[i];
            }
        }
        yield return insts[insts.Length - 2];
        yield return insts[insts.Length - 1];
        ThrowIfTrue(!foundField);
    }
    private static bool MaybeChangeHeight(CameraRig camera, Vector2 vec) {
        if (Input.GetKey(KeyCode.LeftControl)) {
            camera.m_TargetPosition.y += vec.y / 10f;
            return true;
        }
        return false;
    }
}
