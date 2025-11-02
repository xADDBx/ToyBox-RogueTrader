using Kingmaker.DialogSystem.Blueprints;

namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.IgnoreDialogRestrictionsSoulMarkFeature")]
public partial class IgnoreDialogRestrictionsSoulMarkFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreDialogRestrictionsSoulMark;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_IgnoreDialogRestrictionsSoulMarkFeature_Name", "Ignore Dialog Restrictions (SoulMark)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_IgnoreDialogRestrictionsSoulMarkFeature_Description", "Answers that were restricted based on your conviction/soul marks will be made available.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.IgnoreDialogRestrictionsSoulMarkFeature";
        }
    }
    [HarmonyPatch(typeof(BlueprintAnswerBase), nameof(BlueprintAnswerBase.IsSoulMarkRequirementSatisfied)), HarmonyPostfix]
    private static void BlueprintAnswerBasePatch_IsSoulMarkRequirementSatisfied_Patch(ref bool __result) {
        __result = true;
    }
}
