using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility.DotNetExtensions;

namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.RemoteCompanionDialogFeature")]
public partial class RemoteCompanionDialogFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableRemoteCompanionDialog;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_RemoteCompanionDialogFeature_Name", "Expand Dialog To Include Remote Companions")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_RemoteCompanionDialogFeature_Description", "Allow remote companions to make comments on dialog you are having.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.RemoteCompanionDialogFeature";
        }
    }
    [HarmonyPatch(typeof(CompanionInParty), nameof(CompanionInParty.CheckCondition)), HarmonyPostfix]
    private static void CompanionInParty_CheckCondition_Patch(CompanionInParty __instance, ref bool __result) {
        // No need to override if the result is already true
        if (__result) {
            return;
        }
        // We only want this patch to run for conditions requiring the character to be in the party so if it is for the inverse we bail.
        // Example of this comes up with Lann and Wenduag in the final scene of the Prologue Labyrinth
        if (__instance.Not) {
            return;
        }
        // We don't want to match when the game only checks for Ex companions since this is basically a check for companions which left the party then
        // Example is 6aeb6812dcc1464a9b087786556c9b18 which checks whether Pascal left as a companion. Really weird design from Owlcat right there.
        if (__instance.MatchWhenEx && !__instance.MatchWhenActive && !__instance.MatchWhenDetached && !__instance.MatchWhenRemote) {
            return;
        }
        try {
            var maybeCompanion = Game.Instance.Player.AllCrossSceneUnits.FirstOrDefault(u => u.Blueprint == __instance.companion && !u.IsDisposed && !u.IsDisposingNow)?.GetOptional<UnitPartCompanion>();
            if (maybeCompanion != null) {
                if (maybeCompanion.State != CompanionState.None) {
                    if (maybeCompanion.State != CompanionState.ExCompanion || GetInstance<ExCompanionDialogFeature>().IsEnabled) {
                        if (__instance.Owner is BlueprintCue cueBP) {
                            OwlLog($"Overiding {cueBP.name} Companion {__instance.companion.name} ({__instance.companion.AssetGuid}) In Party to true");
                            __result = true;
                        }
                    }
                }
            } else {
                Log($"Could not override check {__instance.name} on {__instance.Owner?.AssetGuid ?? "Null BP Owner?"} because no unit with blueprint {__instance.companion?.AssetGuid ?? "Null Companion BP?"} was found.");
            }
        } catch (Exception ex) {
            Error(ex);
        }
    }
    private static bool m_OriginallyIncludedEx;
    private static bool m_OriginallyIncludedRemote;
    [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty), nameof(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty.GetAbstractUnitEntityInternal)), HarmonyPrefix]
    private static void CompanionInParty_GetAbstractUnitEntityInternal_Pre_Patch(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty __instance) {
        if (__instance.Owner is BlueprintCue) {
            m_OriginallyIncludedEx = __instance.IncludeExCompanions;
            m_OriginallyIncludedRemote = __instance.IncludeRemote;
            __instance.IncludeExCompanions = GetInstance<ExCompanionDialogFeature>().IsEnabled;
            __instance.IncludeRemote = true;
            OwlLog($"Evalutors checking {__instance} Guid:{__instance.AssetGuid} Owner:{__instance.Owner.name} OwnerGuid: {__instance.Owner.AssetGuid}); Allowed ex: {m_OriginallyIncludedEx}, now: {__instance.IncludeExCompanions}; Allowed remote: {m_OriginallyIncludedRemote}, now: true");
        }
    }
    [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty), nameof(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty.GetAbstractUnitEntityInternal)), HarmonyPostfix]
    private static void CompanionInParty_GetAbstractUnitEntityInternal_Post_Patch(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty __instance) {
        if (__instance.Owner is BlueprintCue) {
            __instance.IncludeExCompanions = m_OriginallyIncludedEx;
            __instance.IncludeRemote = m_OriginallyIncludedRemote;
        }
    }
    [HarmonyPatch(typeof(DialogSpeaker), nameof(DialogSpeaker.GetEntity)), HarmonyPostfix]
    public static void DialogSpeaker_GetEntity_Patch(DialogSpeaker __instance, ref BaseUnitEntity __result) {
        if (__result == null && __instance.Blueprint != null) {
            var units = Game.Instance.EntitySpawner.CreationQueue.Select((EntitySpawnController.SpawnEntry ce) => ce.Entity).OfType<BaseUnitEntity>();
            var maybeUnit = Game.Instance.Player.AllCrossSceneUnits.Where(u => GetInstance<ExCompanionDialogFeature>().IsEnabled || u.GetCompanionOptional()?.State != CompanionState.ExCompanion)
                .Concat(units).Select(__instance.SelectMatchingUnit).NotNull().Distinct().Nearest(Game.Instance.DialogController.DialogPosition);
            if (maybeUnit != null) {
                __instance.ReplacedSpeakerWithErrorSpeaker = false;
                __result = maybeUnit;
                return;
            }

        }
    }
}
