using Kingmaker.DialogSystem.Blueprints;
using ToyBox.Features.SettingsTab.Other;

namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.IgnoreDialogRestrictionsEverythingFeature")]
public partial class IgnoreDialogRestrictionsEverythingFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreDialogRestrictionsEverything;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_IgnoreDialogRestrictionsEverythingFeature_Name", "Ignore Dialog Restrictions (Everything, Experimental)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_IgnoreDialogRestrictionsEverythingFeature_Description", "Answers that were restricted will be made available.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.IgnoreDialogRestrictionsEverythingFeature";
        }
    }
    public override bool ShouldHide {
        get {
            return !GetInstance<ShowRiskyTogglesFeature>().IsEnabled;
        }
    }
    [HarmonyPatch(typeof(BlueprintAnswer), nameof(BlueprintAnswer.CanSelect)), HarmonyPostfix]
    private static void BlueprintAnswer_CanSelect_Patch(ref bool __result) {
        __result = true;
    }
}
