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
        bool nextNewObj = false;
        Label? lastTarget = null;
        foreach (var instruction in instructions) {
            if (instruction.LoadsField(field)) {
                nextNewObj = true;
            } else {
                _ = instruction.Branches(out lastTarget);
            }
            if (instruction.opcode == OpCodes.Newobj && nextNewObj) {
                nextNewObj = false;
                yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(instruction.labels);
                yield return new(OpCodes.Ldloc_0);
                yield return CodeInstruction.Call((CameraRig camera, Vector2 vec) => MaybeChangeHeight(camera, vec));
                yield return new CodeInstruction(OpCodes.Brtrue, lastTarget);
            } else {
                yield return instruction;
            }
        }
    }
    private static bool MaybeChangeHeight(CameraRig camera, Vector2 vec) {
        if (Input.GetKey(KeyCode.LeftControl)) {
            camera.m_TargetPosition.y += vec.y / 10f;
            return true;
        }
        return false;
    }
}
