using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.InfiniteAbilitiesFeature")]
public partial class InfiniteAbilitiesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableInfiniteAbilities;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InfiniteAbilitiesFeature_Name", "Free Abilities")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InfiniteAbilitiesFeature_Description", "Makes abilities ignore cooldowns and have no cost.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.InfiniteAbilitiesFeature";
        }
    }
    [HarmonyPatch(typeof(UnitUseAbilityParams), nameof(UnitUseAbilityParams.IgnoreCooldown), MethodType.Getter), HarmonyPostfix]
    private static void UnitUseAbilityParams_IgnoreCooldown_Patch(UnitUseAbilityParams __instance, ref bool __result) {
        if (__instance.Ability?.Caster is AbstractUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            __result = true;
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
