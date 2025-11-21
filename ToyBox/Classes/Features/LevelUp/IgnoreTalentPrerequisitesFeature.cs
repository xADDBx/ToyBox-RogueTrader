using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.MVVM.VM.CharGen;
using Kingmaker.UnitLogic.Progression.Prerequisites;

namespace ToyBox.Features.LevelUp;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.IgnoreTalentPrerequisitesFeature")]
public partial class IgnoreTalentPrerequisitesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreTalentPrerequisites;
        }
    }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreTalentPrerequisitesFeature_Name", "Ignore Talent Prerequisites")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_IgnoreTalentPrerequisitesFeature_Description", "Automatically succeeds fact-dependent prerequisite checks, both those asking for a feature to be present and those asking for a feature to not be present.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.IgnoreTalentPrerequisitesFeature";
        }
    }
    // Chargen checks some prerequisites. Succeeding all of them breaks Chargen UI (iirc), so this is probably necessary.
    private static bool m_IsChargenCall = false;
    [HarmonyPatch(typeof(PrerequisiteFact), nameof(PrerequisiteFact.MeetsInternal)), HarmonyPostfix]
    private static void PrerequisiteFact_MeetsInternal_Patch(PrerequisiteFact __instance, IBaseUnitEntity unit, ref bool __result) {
        // if Not is true we want result to be false
        // if Not is false we want result to be false
        // => we want to change the result if result XOR Not is false
        // => !(result ^ Not) => result == Not
        if (ToyBoxUnitHelper.IsPartyOrPet(unit as BaseUnitEntity) && !m_IsChargenCall && __result == __instance.Not) {
            __result = !__instance.Not;
        }
    }
    [HarmonyPatch(typeof(CharGenUtility), nameof(CharGenUtility.GetFeatureSelectionsByGroup)), HarmonyPrefix]
    private static void CharGenUtility_GetFeatureSelectionsByGroup_PrePatch() {
        m_IsChargenCall = true;
    }
    [HarmonyPatch(typeof(CharGenUtility), nameof(CharGenUtility.GetFeatureSelectionsByGroup)), HarmonyPostfix]
    private static void CharGenUtility_GetFeatureSelectionsByGroup_PostPatch() {
        m_IsChargenCall = false;
    }
}
