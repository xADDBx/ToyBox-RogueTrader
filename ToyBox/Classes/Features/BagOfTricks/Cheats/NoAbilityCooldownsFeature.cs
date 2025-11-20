using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic.Commands;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.NoAbilityCooldownsFeature")]
public partial class NoAbilityCooldownsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableNoAbilityCooldownsFeature;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_NoAbilityCooldownsFeature_Name", "No Ability Cooldowns")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_NoAbilityCooldownsFeature_Description", "Makes abilities have no cooldown.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.NoAbilityCooldownsFeature";
        }
    }
    [HarmonyPatch(typeof(UnitUseAbilityParams), nameof(UnitUseAbilityParams.IgnoreCooldown), MethodType.Getter), HarmonyPostfix]
    private static void UnitUseAbilityParams_IgnoreCooldowns_Patch(UnitUseAbilityParams __instance, ref bool __result) {
        if (__instance.Ability?.Caster is AbstractUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            __result = true;
        }
    }
}
