using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.Dialog.Dialog;
using Kingmaker.Code.Utility;
using Kingmaker.DialogSystem.Blueprints;

namespace ToyBox.Features.BagOfTricks.Preview;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Preview.PreviewDialogResultsFeature")]
public partial class PreviewDialogResultsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnablePreviewDialogResults;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_PreviewDialogResultsFeature_Name", "Dialog Results")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_PreviewDialogResultsFeature_Description", "Shows results of cues and answers in dialog.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Preview.PreviewDialogResultsFeature";
        }
    }
    [HarmonyPatch(typeof(CueVM), nameof(CueVM.GetCueText)), HarmonyPostfix]
    private static void GetCueText_Patch(ref string __result) {
        try {
            var cue = Game.Instance.DialogController.CurrentCue;
            if (cue != null) {
                __result += DialogPreviewUtilities.GetCueResultText(cue);
            }
        } catch (Exception ex) {
            Error(ex);
        }
    }
    [HarmonyPatch(typeof(UIConstsExtensions), nameof(UIConstsExtensions.GetAnswerFormattedString)), HarmonyPriority(Priority.LowerThanNormal), HarmonyPostfix]
    private static void GetAnswerFormattedString_Patch(BlueprintAnswer answer, ref string __result) {
        try {
            if (answer != null) {
                __result += DialogPreviewUtilities.GetAnswerResultText(answer);
            }
        } catch (Exception ex) {
            Error(ex);
        }
    }
}
