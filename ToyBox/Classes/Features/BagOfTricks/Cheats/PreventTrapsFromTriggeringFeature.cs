using Kingmaker.View.MapObjects.Traps;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.PreventTrapsFromTriggeringFeature")]
public partial class PreventTrapsFromTriggeringFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.PreventTrapsFromTriggering;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_PreventTrapsFromTriggeringFeature_Name", "Prevent Traps from Triggering")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_PreventTrapsFromTriggeringFeature_Description", "Entering a trap zone while having traps disabled will prevent that trap from triggering even if you deactivate this option in the future.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.PreventTrapsFromTriggeringFeature";
        }
    }
    [HarmonyPatch(typeof(TrapObjectData), nameof(TrapObjectData.TryTriggerTrap)), HarmonyPrefix]
    private static bool TrapObjectData_TryTriggerTrap_Patch() {
        return false;
    }
}
