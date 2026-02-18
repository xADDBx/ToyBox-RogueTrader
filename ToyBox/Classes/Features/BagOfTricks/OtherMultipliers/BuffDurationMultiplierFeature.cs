using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.OtherMultipliers.BuffDurationMultiplierFeature")]
public partial class BuffDurationMultiplierFeature : FeatureWithPatch {
    private bool m_IsEnabled = false;
    private Browser<BlueprintBuff>? m_ExclusionBrowser;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.BuffDurationMultiplier != null;
            return ref m_IsEnabled;
        }
    }
    private readonly TimedCache<float> m_ButtonWidth = new(() => CalculateLargestLabelWidth([m_StopExcludingLocalizedText, m_ExcludeLocalizedText], GUI.skin.button));
    private bool m_ShowExclusionEditor = false;
    public override void OnGui() {
        var tmp = Settings.BuffDurationMultiplier ?? 1f;
        using (HorizontalScope()) {
            if (UI.LogSlider(ref tmp, 0f, 10000f, 1f, 2, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MinWidth(150))) {
                if (tmp == 1f) {
                    Settings.BuffDurationMultiplier = null;
                    Disable();
                } else {
                    Settings.BuffDurationMultiplier = tmp;
                    Enable();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
        UI.DisclosureToggle(ref m_ShowExclusionEditor, m_ManageExceptionsToBuffDurationMuLocalizedText);
        if (m_ShowExclusionEditor) {
            if (m_ExclusionBrowser == null) {
                if (BPLoader.CanStart) {
                    m_ExclusionBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, [.. BPLoader.GetBlueprintsByGuids<BlueprintBuff>(Settings.BuffDurationMultiplierExclusions)], func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)(0.8f * EffectiveWindowWidth()));
                }
            } else {
                using (HorizontalScope()) {
                    Space(40);
                    bool updateItems = false;
                    m_ExclusionBrowser.OnGUI(buff => {
                        using (HorizontalScope()) {
                            var isExcluded = Settings.BuffDurationMultiplierExclusions.Contains(buff.AssetGuid);
                            var name = BPHelper.GetTitle(buff);
                            var desc = BPHelper.GetDescription(buff);
                            if (isExcluded) {
                                if (UI.Button(m_StopExcludingLocalizedText.Cyan(), null, null, Width(m_ButtonWidth))) {
                                    Settings.BuffDurationMultiplierExclusions.Remove(buff.AssetGuid);
                                    updateItems = !m_ExclusionBrowser.ShowAll;
                                }
                                name = name.Cyan();
                            } else {
                                if (UI.Button(m_ExcludeLocalizedText, null, null, Width(m_ButtonWidth))) {
                                    Settings.BuffDurationMultiplierExclusions.Add(buff.AssetGuid);
                                    updateItems = !m_ExclusionBrowser.ShowAll;
                                }
                                name = name.Green();
                            }
                            Space(10);
                            UI.Label(name);
                            Space(5);
                            GUILayout.TextArea(buff.AssetGuid, GUILayout.ExpandWidth(false));
                            if (desc != null) {
                                UI.Label(desc);
                            }
                        }
                    });
                    if (updateItems) {
                        m_ExclusionBrowser.UpdateItems([.. BPLoader.GetBlueprintsByGuids<BlueprintBuff>(Settings.BuffDurationMultiplierExclusions)]);
                    }
                }
            }
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.OtherMultipliers.BuffDurationMultiplierFeature";
        }
    }
    [HarmonyPatch(typeof(BuffCollection), nameof(BuffCollection.Add), [typeof(BlueprintBuff), typeof(MechanicEntity), typeof(MechanicsContext), typeof(BuffDuration)]), HarmonyPostfix]
    private static void BuffCollection_Add_Patch(BlueprintBuff blueprint, MechanicEntity caster, ref BuffDuration duration) {
        try {
            if (!duration.Rounds.HasValue || caster == null || caster.IsPlayerEnemy || Settings.BuffDurationMultiplierExclusions.Contains(blueprint.AssetGuid) || duration.IsPermanent) {
                return;
            }
            var newRounds = new Kingmaker.Utility.Rounds(Mathf.FloorToInt(duration.Rounds.Value.Value * (Settings.BuffDurationMultiplier ?? 1)));
            duration = new(newRounds, duration.EndCondition);
        } catch (Exception ex) {
            Error(ex);
        }
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_BuffDurationMultiplierFeature_m_ManageExceptionsToBuffDurationMuLocalizedText", "Manage Exceptions to Buff Duration Multiplier")]
    private static partial string m_ManageExceptionsToBuffDurationMuLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_BuffDurationMultiplierFeature_Name", "Buff Duration Multiplier")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_BuffDurationMultiplierFeature_Description", "Multiplies the duration of buffs assuming they are not excluded and not casted by an enemy.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_BuffDurationMultiplierFeature_m_StopExcludingLocalizedText", "Stop Excluding")]
    private static partial string m_StopExcludingLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_BuffDurationMultiplierFeature_m_ExcludeLocalizedText", "Exclude")]
    private static partial string m_ExcludeLocalizedText { get; }
}
