using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using static Kingmaker.UnitLogic.Abilities.AbilityData;

namespace ToyBox.Features.BagOfTricks.Cheats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.IgnoreAllAbilityRequirementsFeature")]
public partial class IgnoreAllAbilityRequirementsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreAllAbilityRequirements;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreAllAbilityRequirementsFeature_Name", "Ignore all Target Requirements for Abilities")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreAllAbilityRequirementsFeature_Description", "Always allows using abilities regardless of target restrictions like Line of Sight, Friendly Fire or something else.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.IgnoreAllAbilityRequirementsFeature";
        }
    }
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.CanTargetFromNode), [typeof(CustomGridNodeBase), typeof(CustomGridNodeBase), typeof(TargetWrapper), typeof(int), typeof(LosCalculations.CoverType), typeof(UnavailabilityReasonType?), typeof(int?)], 
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal]), HarmonyPostfix]
    private static void AbilityData_CanTargetFromNode_Patch(ref UnavailabilityReasonType? unavailabilityReason, AbilityData __instance, ref bool __result) {
        if (!__result && __instance.Caster is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            unavailabilityReason = UnavailabilityReasonType.None;
            __result = true;
        }
    }
}
