using Kingmaker.Controllers;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.RTSpecific.PreventPsychicPhenomena")]
public partial class PreventPsychicPhenomena : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnablePreventPsychicPhenomena;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_PreventPsychicPhenomena_Name", "Prevent Psychic Phenomena")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_PreventPsychicPhenomena_Description", "Prevents Psyker Abilities from causing any kind of psychic phenomena. This includes e.g. the Dark Prayer ability and Chaos Sword.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.RTSpecific.PreventPsychicPhenomena";
        }
    }
    [HarmonyPatch(typeof(ContextActionRunPsychicPhenomena), nameof(ContextActionRunPsychicPhenomena.RunAction)), HarmonyPrefix]
    private static bool ContextActionRunPsychicPhenomena_RunAction_Patch() {
        return false;
    }
    [HarmonyPatch(typeof(PsychicPhenomenaController), nameof(PsychicPhenomenaController.HandleExecutionProcessEnd)), HarmonyPrefix]
    private static bool PsychicPhenomenaController_HandleExecutionProcessEnd_Patch() {
        return false;
    }
}
