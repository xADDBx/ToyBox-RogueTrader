using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Code.UI.MVVM.VM.Bark;
using Kingmaker.Code.UI.MVVM.VM.Dialog.Dialog;
using Kingmaker.Controllers.Dialog;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitDisableVoiceoverAndBarksFeature")]
public partial class UnitDisableVoiceoverAndBarksFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    private static bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.DisableVoiceoverForCharacterName.Count > 0;
            return ref m_IsEnabled;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitDisableVoiceoverAndBarksFeature_Name", "Disable Voiceover and Barks for Character")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitDisableVoiceoverAndBarksFeature_Description", "Prevents this character from talking with his voice.")]
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
        var cName = unit.Blueprint?.CharacterName?.ToLower() ?? unit.Blueprint?.AssetGuid?.ToString() ?? "<Null>";
        var currentlyDisabled = Settings.DisableVoiceoverForCharacterName.Contains(cName);
        UI.Toggle(Name, Description, ref currentlyDisabled, () => {
            Settings.DisableVoiceoverForCharacterName.Add(cName);
            Initialize();
        }, () => {
            Settings.DisableVoiceoverForCharacterName.Remove(cName);
            if (!IsEnabled) {
                Destroy();
            }
        });
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitDisableVoiceoverAndBarksFeature";
        }
    }
    private static BlueprintUnit? m_CurrentSpeaker;
    [HarmonyPatch(typeof(BarkPlayer), nameof(BarkPlayer.Bark), [typeof(Entity), typeof(Kingmaker.Localization.LocalizedString), typeof(string), typeof(float), typeof(bool)]), HarmonyPrefix]
    private static void BarkPlayer_Bark1_Patch(Entity entity) {
        if (entity is BaseUnitEntity entity2) {
            m_CurrentSpeaker = entity2.Blueprint;
        } else {
            m_CurrentSpeaker = null;
        }
    }
    [HarmonyPatch(typeof(BarkPlayer), nameof(BarkPlayer.Bark), [typeof(Entity), typeof(Kingmaker.Localization.LocalizedString), typeof(float), typeof(bool), typeof(BaseUnitEntity), typeof(bool), typeof(string), typeof(UnityEngine.Color)]), HarmonyPrefix]
    private static void BarkPlayer_Bark2_Patch(Entity entity) {
        if (entity is BaseUnitEntity entity2) {
            m_CurrentSpeaker = entity2.Blueprint;
        } else {
            m_CurrentSpeaker = null;
        }
    }
    [HarmonyPatch(typeof(DialogVM), nameof(DialogVM.HandleOnCueShow)), HarmonyPrefix]
    private static void DialogVM_HandleOnCueShow_Patch(CueShowData data) {
        m_CurrentSpeaker = data?.Cue?.Speaker?.Blueprint ?? Game.Instance.DialogController?.CurrentSpeaker?.Blueprint;
    }
    [HarmonyPatch(typeof(SpaceEventVM), nameof(SpaceEventVM.HandleOnCueShow)), HarmonyPrefix]
    private static void SpaceEventVM_HandleOnCueShow_Patch(CueShowData data) {
        m_CurrentSpeaker = data?.Cue?.Speaker?.Blueprint ?? Game.Instance.DialogController?.CurrentSpeaker?.Blueprint;
    }
    [HarmonyPatch(typeof(Kingmaker.Localization.LocalizedString), nameof(Kingmaker.Localization.LocalizedString.GetVoiceOverSound)), HarmonyPrefix]
    private static bool LocalizedString_GetVoiceOverSound_Patch(ref string __result) {
        var cName = m_CurrentSpeaker?.CharacterName?.ToLower() ?? m_CurrentSpeaker?.AssetGuid?.ToString() ?? "";
        if (!string.IsNullOrEmpty(cName) && Settings.DisableVoiceoverForCharacterName.Contains(cName)) {
            __result = "";
            return false;
        }
        return true;
    }
}
