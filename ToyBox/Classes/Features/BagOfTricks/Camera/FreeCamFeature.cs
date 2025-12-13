using Kingmaker.View;
using System.Reflection.Emit;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.Camera;

[IsTested]
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
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickScroll)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CameraRig_TickScroll_Patch(IEnumerable<CodeInstruction> instructions) {
        var getNoClamp = AccessTools.PropertyGetter(typeof(CameraRig), nameof(CameraRig.NoClamp));
        var scrollOffset = AccessTools.Field(typeof(CameraRig), nameof(CameraRig.m_ScrollOffset));
        var getZero = AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero));
        var foundCall = false;
        var foundField = false;
        var startSkipping = false;
        List<Label> labels = [];
        foreach (var inst in instructions) {
            if (inst.Calls(getNoClamp)) {
                yield return new(OpCodes.Pop);
                startSkipping = true;
                foundCall = true;
            } else if (inst.StoresField(scrollOffset)) {
                yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels);
                yield return new CodeInstruction(OpCodes.Call, getZero);
                startSkipping = false;
                foundField = true;
            }
            if (!startSkipping) {
                yield return inst;
            } else {
                labels.AddRange(inst.labels);
            }
        }
        ThrowIfTrue(!foundField || !foundCall);
    }
}
