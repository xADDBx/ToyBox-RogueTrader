using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.CareerPath;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;

namespace ToyBox.Features.LevelUp;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.IgnoreArchetypePrerequisitesFeature")]
public partial class IgnoreArchetypePrerequisitesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreArchetypePrerequisites;
        }
    }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreArchetypePrerequisitesFeature_Name", "Ignore Archetypes Prerequisites")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreArchetypePrerequisitesFeature_Description", "Slightly Buggy UI. This allows picking any one career per stage regardless of prerequisites. Warning: Picking e.g. Exemplar as second archetype will cause issues because you won't have a second archetype ability to upgrade.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.IgnoreArchetypePrerequisitesFeature";
        }
    }
    [HarmonyPatch(typeof(PartUnitProgression), nameof(PartUnitProgression.CanUpgradePath)), HarmonyPostfix]
    private static void PartUnitProgression_CanUpgradePath_Patch(PartUnitProgression __instance, BlueprintPath path, ref bool __result) {
        __result = __instance.GetPathRank(path) < path.Ranks;

    }
    [HarmonyPatch(typeof(CareerPathsListVM), nameof(CareerPathsListVM.GetPrerequisitesCareers)), HarmonyPrefix]
    private static void CareerPathsListVM_GetPrerequisitesCareers_Patch(CareerPathsListVM __instance, ref List<BlueprintCareerPath> careerPaths, ref List<BlueprintFeature> features) {
        careerPaths = [];
        features = [];
    }
}
