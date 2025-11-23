using Kingmaker.Controllers.Combat;

namespace ToyBox.Features.BagOfTricks.Cheats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.PartialUnlimitedActionsPerTurnFeature")]
public partial class PartialUnlimitedActionsPerTurnFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnablePartialUnlimitedActionsPerTurn;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_PartialUnlimitedActionsPerTurnFeature_Name", "Don't use AP (Partial)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_PartialUnlimitedActionsPerTurnFeature_Description", "Don't use any AP except for except for abilities which would consume all your AP at once.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.PartialUnlimitedActionsPerTurnFeature";
        }
    }
    [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.SpendActionPoints)), HarmonyPrefix]
    private static bool PartUnitCombatState_SpendActionPoints_Patch(PartUnitCombatState __instance) {
        return !ToyBoxUnitHelper.IsPartyOrPet(__instance.Owner);
    }
}
