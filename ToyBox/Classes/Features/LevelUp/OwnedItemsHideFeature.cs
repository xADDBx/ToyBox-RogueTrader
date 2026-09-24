using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry;
using Owlcat.Runtime.UI.MVVM;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// Hides owned-and-maxed entries in the level-up feature picker (R13). Both
/// display pipelines (non-ship HandleFilterChange via GetAll, ship
/// RunLegacyFilterPipeline via GetFiltered) funnel through these two methods
/// BEFORE grouping/header creation, so emptied groups drop their headers,
/// FeatureCount stays correct and hidden items never get views. The state
/// layer (SelectionStateFeature.Items) is never touched: an all-owned slot is
/// simply NotSelectable and skippable, exactly like vanilla. Takes effect at
/// the next list build (picker open / filter / grouping change).
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.OwnedItemsHideFeature")]
public partial class OwnedItemsHideFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;
    private static int s_Failures;

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.OwnedItemsHideFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.HideOwnedMaxedEntries;
        }
    }

    [LocalizedString("ToyBox_Features_LevelUp_OwnedItemsHideFeature_Name", "Hide Owned Features In Picker")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_OwnedItemsHideFeature_Description", "Hides picker entries you already own and can no longer take (the greyed owned ones, including the ship picker's trailing owned section). Owned entries that can still be ranked up, stat advancements and not-owned prerequisite failures are unaffected. Applies on the next list build.")]
    public override partial string Description { get; }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the strike counter: only consecutive failures may
        // auto-disable.
        s_Failures = 0;
    }

    // Sole funnel of the non-ship pipeline. Both methods return fresh List
    // copies of FeatureList, so the RemoveAll can never touch the group's own
    // data or the selection state layer.
    [HarmonyPatch(typeof(RankEntryFeatureGroupVM), nameof(RankEntryFeatureGroupVM.GetAll)), HarmonyPostfix]
    private static void RankEntryFeatureGroupVM_GetAll_Postfix(ref List<VirtualListElementVMBase> __result) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            if (Settings.HideOwnedMaxedEntries) {
                _ = OwnedItemsFilter.RemoveHidden(__result);
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Sole funnel of the ship (legacy) pipeline; also re-enters GetAll
    // internally for the None filter, which the GetAll postfix already
    // covers.
    [HarmonyPatch(typeof(RankEntryFeatureGroupVM), nameof(RankEntryFeatureGroupVM.GetFiltered)), HarmonyPostfix]
    private static void RankEntryFeatureGroupVM_GetFiltered_Postfix(ref List<VirtualListElementVMBase> __result) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            if (Settings.HideOwnedMaxedEntries) {
                _ = OwnedItemsFilter.RemoveHidden(__result);
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox LevelUp: owned-items hide patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox LevelUp: auto-disabling OwnedItemsHideFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<OwnedItemsHideFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
