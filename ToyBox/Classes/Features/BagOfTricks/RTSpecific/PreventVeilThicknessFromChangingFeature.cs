using Kingmaker;
using Kingmaker.Designers.WarhammerSurfaceCombatPrototype.PsychicPowers;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.RTSpecific.PreventVeilThicknessFromChangingFeature")]
public partial class PreventVeilThicknessFromChangingFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnablePreventVeilThicknessFromChanging;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_PreventVeilThicknessFromChangingFeature_Name", "Prevent Veil Thickness from Changing")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_PreventVeilThicknessFromChangingFeature_Description", "Forces the current veil thickness to 0.")]
    public override partial string Description { get; }
    public override void Initialize() {
        base.Initialize();
        if (IsInGame()) {
            Game.Instance.TurnController?.VeilThicknessCounter?.Value = 0;
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.RTSpecific.PreventVeilThicknessFromChangingFeature";
        }
    }
    [HarmonyPatch(typeof(VeilThicknessCounter), nameof(VeilThicknessCounter.Value), MethodType.Setter), HarmonyPrefix]
    private static void VeilThicknessCounter_setValue_Patch(ref int value) {
        value = 0;
    }
}
