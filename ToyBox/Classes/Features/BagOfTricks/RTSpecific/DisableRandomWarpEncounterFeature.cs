using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.DialogSystem.Blueprints;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.RTSpecific.DisableRandomWarpEncounterFeature")]
public partial class DisableRandomWarpEncounterFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.DisableRandomWarpEncounters;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_DisableRandomWarpEncounterFeature_Name", "Disable Random Encounters in Warp")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_DisableRandomWarpEncounterFeature_Description", "Prevents random encounters in warp. Note that this also disables positive and flavor events!")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.RTSpecific.DisableRandomWarpEncounterFeature";
        }
    }
    [HarmonyPatch(typeof(SectorMapTravelController), nameof(SectorMapTravelController.ShouldProceedEvent)), HarmonyPrefix]
    private static bool SectorMapTravelController_ShouldProceedEvent_Patch(out bool shouldProceedRE, out BlueprintDialog? randomEncounter) {
        shouldProceedRE = false;
        randomEncounter = null;
        return false;
    }
}
