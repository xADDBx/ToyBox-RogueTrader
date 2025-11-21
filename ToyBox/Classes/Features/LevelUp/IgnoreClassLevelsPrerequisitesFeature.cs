using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Progression.Prerequisites;

namespace ToyBox.Features.LevelUp;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.IgnoreClassLevelsPrerequisitesFeature")]
public partial class IgnoreClassLevelsPrerequisitesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreClassLevelsPrerequisites;
        }
    }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreClassLevelsPrerequisitesFeature_Name", "Ignore Class Level Prerequisites")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreClassLevelsPrerequisitesFeature_Description", "Automatically succeeds level-dependent prerequisite checks, both those asking for a minimum level and those asking for a feature maximum level.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.IgnoreClassLevelsPrerequisitesFeature";
        }
    }
    [HarmonyPatch(typeof(PrerequisiteLevel), nameof(PrerequisiteLevel.MeetsInternal)), HarmonyPostfix]
    private static void PrerequisiteLevel_MeetsInternal_Patch(PrerequisiteLevel __instance, IBaseUnitEntity unit, ref bool __result) {
        // if Not is true we want result to be false
        // if Not is false we want result to be false
        // => we want to change the result if result XOR Not is false
        // => !(result ^ Not) => result == Not
        if (ToyBoxUnitHelper.IsPartyOrPet(unit as BaseUnitEntity) && __result == __instance.Not) {
            __result = !__instance.Not;
        }
    }
}
