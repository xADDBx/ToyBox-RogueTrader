using Kingmaker.Code.Utility;
using Kingmaker.DialogSystem.Blueprints;

namespace ToyBox.Features.BagOfTricks.Preview;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Preview.PreviewDialogConditionsFeature")]
public partial class PreviewDialogConditionsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnablePreviewDialogConditions;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_PreviewDialogConditionsFeature_Name", "Dialog Conditions")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_PreviewDialogConditionsFeature_Description", "Shows conditions of answers in dialog.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Preview.PreviewDialogConditionsFeature";
        }
    }
    [HarmonyPatch(typeof(UIConstsExtensions), nameof(UIConstsExtensions.GetAnswerFormattedString)), HarmonyPriority(Priority.HigherThanNormal), HarmonyPostfix]
    private static void GetAnswerFormattedString_Patch(BlueprintAnswer answer, ref string __result) {
        try {
            if (answer != null) {
                var conditions = DialogPreviewUtilities.FormatConditionsAsList(answer) ?? [];
                var conditionsText = string.Join("", conditions.Select(s => "\n" + DialogPreviewUtilities.Indent + s));
                if (!string.IsNullOrWhiteSpace(conditionsText)) {
                    __result += $"<size=65%>{conditionsText}</size>";
                }
            }
        } catch (Exception ex) {
            Error(ex);
        }
    }
}
