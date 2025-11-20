using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using static Kingmaker.UnitLogic.Abilities.AbilityData;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.IgnoreAoeOverlapAbilityRequirementFeature")]
public partial class IgnoreAoeOverlapAbilityRequirementFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreAoeOverlapAbilityRequirement;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreAoeOverlapAbilityRequirementFeature_Name", "Ignore Ability Requirements - AoE Overlap")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreAoeOverlapAbilityRequirementFeature_Description", "")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.IgnoreAoeOverlapAbilityRequirementFeature";
        }
    }
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.CanTargetFromNode), [typeof(CustomGridNodeBase), typeof(CustomGridNodeBase), typeof(TargetWrapper), typeof(int), typeof(LosCalculations.CoverType), typeof(UnavailabilityReasonType?), typeof(int?)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal]), HarmonyPostfix]
    private static void AbilityData_CanTargetFromNode_Patch(ref UnavailabilityReasonType? unavailabilityReason, AbilityData __instance, ref bool __result) {
        if (!__result && __instance.Caster is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit) && unavailabilityReason == UnavailabilityReasonType.AreaEffectsCannotOverlap) {
            unavailabilityReason = UnavailabilityReasonType.None;
            __result = true;
        }
    }
}
