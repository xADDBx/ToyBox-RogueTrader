using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitOverrideAiControlBehaviourFeature")]
public partial class UnitOverrideAiControlBehaviourFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    private static bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.OverrideEnableAiForCompanions.Count > 0;
            return ref m_IsEnabled;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideAiControlBehaviourFeature_Name", "Override AI Control Behaviour")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideAiControlBehaviourFeature_Description", "Allows toggling whether a unit should be controlled by the AI or by the player.")]
    public override partial string Description { get; }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        var currentlyOverriden = Settings.OverrideEnableAiForCompanions.TryGetValue(unit.UniqueId, out var enableAi);
        UI.Toggle(Name, Description, ref currentlyOverriden, () => {
            Settings.OverrideEnableAiForCompanions.Add(unit.UniqueId, enableAi);
            Enable();
        }, () => {
            Settings.OverrideEnableAiForCompanions.Remove(unit.UniqueId);
            if (!IsEnabled) {
                Disable();
            }
        });
        if (currentlyOverriden) {
            using (HorizontalScope()) {
                Space(20);
                if (UI.Toggle(m_MakeCharacterAIControlledLocalizedText, null, ref enableAi)) {
                    Settings.OverrideEnableAiForCompanions[unit.UniqueId] = enableAi;
                }
            }
        }
    }
    [HarmonyPatch(typeof(TurnController), nameof(TurnController.IsPlayerTurn), MethodType.Getter), HarmonyPostfix]
    private static void IsPlayerTurn(TurnController __instance, ref bool __result) {
        if (__instance.CurrentUnit != null && Settings.OverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.UniqueId, out var maybeOverride)) {
            __result = !maybeOverride;
        }
    }
    [HarmonyPatch(typeof(TurnController), nameof(TurnController.IsAiTurn), MethodType.Getter), HarmonyPostfix]
    private static void IsAiTurn(TurnController __instance, ref bool __result) {
        if (__instance.CurrentUnit != null && Settings.OverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.UniqueId, out var maybeOverride)) {
            __result = maybeOverride;
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitOverrideAiControlBehaviourFeature";
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideAiControlBehaviourFeature_m_MakeCharacterAIControlledLocalizedText", "AI should controll this Character")]
    private static partial string m_MakeCharacterAIControlledLocalizedText { get; }
}
