using Kingmaker.Controllers.Combat;

namespace ToyBox.Features.BagOfTricks.Cheats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.CompleteUnlimitedActionsPerTurnFeature")]
public partial class CompleteUnlimitedActionsPerTurnFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableCompleteUnlimitedActionsPerTurn;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_CompleteUnlimitedActionsPerTurnFeature_Name", "Don't use any AP")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_CompleteUnlimitedActionsPerTurnFeature_Description", "This allows doing Infinite Actions per turn.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.CompleteUnlimitedActionsPerTurnFeature";
        }
    }

    [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.SpendActionPoints)), HarmonyPrefix]
    private static bool PartUnitCombatState_SpendActionPoints_Patch(PartUnitCombatState __instance) {
        return !ToyBoxUnitHelper.IsPartyOrPet(__instance.Owner);
    }
    [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.SpendActionPointsAll)), HarmonyPrefix]
    private static bool PartUnitCombatState_SpendActionPointsAll_Patch(PartUnitCombatState __instance) {
        return !ToyBoxUnitHelper.IsPartyOrPet(__instance.Owner);
    }
}
