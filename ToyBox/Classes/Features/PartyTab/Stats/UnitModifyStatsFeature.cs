using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.UnitLogic;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Features.PartyTab.Stats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitModifyStatsFeature")]
public partial class UnitModifyStatsFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableUnitModifyStats;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifyStatsFeature_Name", "Modify Unit Stats")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifyStatsFeature_Description", "Allows modifying the various stats of a unit.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitModifyStatsFeature";
        }
    }
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
    private readonly TimedCache<float> m_LabelWidth = new(() => {
        List<string> names = [];
        foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
            if (Constants.WeirdStats.Contains(stat) || Constants.LegacyStats.Contains(stat) || Constants.StarshipStats.Contains(stat)) {
                continue;
            }
            var name = LocalizedTexts.Instance.Stats.GetText(stat);
            if (string.IsNullOrWhiteSpace(name)) {
                name = stat.ToString();
            }
            names.Add(name);
        }
        return CalculateLargestLabelWidth(names, GUI.skin.label);
    });
    private bool m_ShowDisclaimer = false;
    public void OnGui(BaseUnitEntity unit) {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(25);
                using (VerticalScope()) {
                    UI.DisclosureToggle(ref m_ShowDisclaimer, m_TryToKeepThisFeatureActivatedAftLocalizedText.Orange());
                    if (m_ShowDisclaimer) {
                        using (HorizontalScope()) {
                            Space(35);
                            UI.Label("When this is turned off, the changed stats will still work in-game, but the respec UI might be very slightly buggy (e.g. +- values might be wrong). This is not a hard dependency as any issues can be fixed by respeccing the unit after disabling this feature/ToyBox.".Cyan(), Width(0.5f * EffectiveWindowWidth()));
                        }
                    }
                    foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                        if (Constants.WeirdStats.Contains(stat) || Constants.LegacyStats.Contains(stat) || (Constants.StarshipStats.Contains(stat) && !unit.IsStarship())) {
                            continue;
                        }
                        var modifiableValue = unit.Stats.GetStatOptional(stat);
                        var baseValue = 0;
                        var modifiedValue = 0;
                        if (modifiableValue != null) {
                            baseValue = modifiableValue.BaseValue;
                            modifiedValue = modifiableValue.ModifiedValue;
                        } else {
                            // Note: We *could* support this by just not skipping the iteration here. 
                            continue;
                        }
                        var change = 0;
                        using (HorizontalScope()) {
                            Space(10);
                            var name = LocalizedTexts.Instance.Stats.GetText(stat);
                            if (string.IsNullOrWhiteSpace(name)) {
                                name = stat.ToString();
                            }
                            UI.Label(name, Width(m_LabelWidth));
                            _ = UI.Button("<", () => {
                                if (modifiableValue == null) {
                                    modifiableValue = AddStat(stat, unit);
                                    baseValue = modifiableValue.BaseValue;
                                    modifiedValue = modifiableValue.ModifiedValue;
                                }
                                change -= 1;
                                modifiableValue.BaseValue += change;
                                modifiedValue += change;
                            });
                            UI.Label($" {modifiedValue} ".Bold().Orange(), Width(50 * Main.UIScale));
                            _ = UI.Button(">", () => {
                                if (modifiableValue == null) {
                                    modifiableValue = AddStat(stat, unit);
                                    baseValue = modifiableValue.BaseValue;
                                    modifiedValue = modifiableValue.ModifiedValue;
                                }
                                change += 1;
                                modifiableValue.BaseValue += change;
                                modifiedValue += change;
                            });
                            Space(10);
                            var val = modifiedValue;
                            UI.TextField(ref val, pair => {
                                if (modifiableValue == null) {
                                    modifiableValue = AddStat(stat, unit);
                                    baseValue = modifiableValue.BaseValue;
                                    modifiedValue = modifiableValue.ModifiedValue;
                                }
                                change += pair.newContent - modifiableValue.ModifiedValue;
                                modifiableValue.BaseValue += change;
                            }, Width(75 * Main.UIScale));
                        }
                        if (change > 0) {
                            if (InSaveSettings != null) {
                                InSaveSettings.AppliedUnitStatChanges.TryGetValue(unit.UniqueId, out var dict);
                                dict ??= [];
                                if (dict.TryGetValue(stat, out var current)) {
                                    current += change;
                                    if (current == 0) {
                                        dict.Remove(stat);
                                    } else {
                                        dict[stat] = current;
                                    }
                                } else {
                                    dict[stat] = change;
                                }
                                InSaveSettings.AppliedUnitStatChanges[unit.UniqueId] = dict;
                                InSaveSettings.Save();
                            }
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.CreatePreview)), HarmonyPostfix]
    private static void UnitHelper_CreatePreview_Patch(BaseUnitEntity _this, ref BaseUnitEntity __result) {
        if (InSaveSettings?.AppliedUnitStatChanges.TryGetValue(_this.UniqueId, out var changes) ?? false) {
            foreach (var change in changes) {
                try {
                    var modifiableValue2 = __result.Stats.GetStatOptional(change.Key) ?? AddStat(change.Key, __result);
                    modifiableValue2.BaseValue += change.Value;
                } catch (Exception ex) {
                    Error(ex);
                }
            }
        }
    }
    private static ModifiableValue AddStat(StatType stat, BaseUnitEntity unit) {
        ModifiableValue? ret;
        if (StatTypeHelper.IsSkill(stat)) {
            ret = unit.Stats.Container.RegisterSkill(stat);
        } else if (StatTypeHelper.IsAttribute(stat)) {
            ret = unit.Stats.Container.RegisterAttribute(stat);
        } else {
            ret = unit.Stats.Container.Register(stat);
        }
        return ret;
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifyStatsFeature_m_TryToKeepThisFeatureActivatedAftLocalizedText", "Try to keep this feature activated after using it (Click for Explanation)")]
    private static partial string m_TryToKeepThisFeatureActivatedAftLocalizedText { get; }
}
