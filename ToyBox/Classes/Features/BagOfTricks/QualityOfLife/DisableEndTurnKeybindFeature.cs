using Kingmaker;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.DisableEndTurnKeybindFeature")]
public partial class DisableEndTurnKeybindFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.DisableEndTurnKeybindFeature;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_DisableEndTurnKeybindFeature_Name", "Disable End Turn Hotkey")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_DisableEndTurnKeybindFeature_Description", "Disables the base game end turn hotkey.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.DisableEndTurnKeybindFeature";
        }
    }
    [HarmonyPatch(typeof(Game), nameof(Game.EndTurnBind)), HarmonyPrefix]
    private static bool Game_EndTurnBind_Patch() {
        return false;
    }
}
