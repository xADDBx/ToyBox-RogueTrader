using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Progression.Prerequisites;

namespace ToyBox.Features.LevelUp;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.IgnoreStatPrerequisitesFeature")]
public partial class IgnoreStatPrerequisitesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreStatPrerequisites;
        }
    }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreStatPrerequisitesFeature_Name", "Ignore Stat Prerequisites")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreStatPrerequisitesFeature_Description", "Automatically succeeds stat-dependent prerequisite checks, both those asking for a minimum value and those asking for a feature maximum value.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.IgnoreStatPrerequisitesFeature";
        }
    }
    [HarmonyPatch(typeof(PrerequisiteStat), nameof(PrerequisiteStat.MeetsInternal)), HarmonyPostfix]
    private static void PrerequisiteStat_MeetsInternal_Patch(PrerequisiteStat __instance, IBaseUnitEntity unit, ref bool __result) {
        // if Not is true we want result to be false
        // if Not is false we want result to be false
        // => we want to change the result if result XOR Not is false
        // => !(result ^ Not) => result == Not
        if (ToyBoxUnitHelper.IsPartyOrPet(unit as BaseUnitEntity) && __result == __instance.Not) {
            __result = !__instance.Not;
        }
    }
}
