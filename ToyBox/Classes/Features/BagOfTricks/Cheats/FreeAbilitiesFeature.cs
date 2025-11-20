using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.FreeAbilitiesFeature")]
public partial class FreeAbilitiesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableFreeAbilities;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_FreeAbilitiesFeature_Name", "Free Abilities")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_FreeAbilitiesFeature_Description", "Makes abilities have no cost.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.FreeAbilitiesFeature";
        }
    }
    [HarmonyPatch(typeof(AbilityResourceLogic), nameof(AbilityResourceLogic.Spend)), HarmonyPrefix]
    private static bool AbilityResourceLogic_Spend_Patch(AbilityData ability) {
        if (ability.Caster is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(ActivatableAbilityResourceLogic), nameof(ActivatableAbilityResourceLogic.SpendResource)), HarmonyPrefix]
    private static bool ActivatableAbilityResourceLogic_SpendResource_Patch(ActivatableAbilityResourceLogic __instance) {
        if (__instance.Owner is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            return false;
        }
        return true;
    }
}
