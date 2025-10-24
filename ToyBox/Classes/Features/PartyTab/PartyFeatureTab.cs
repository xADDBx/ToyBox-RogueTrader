using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Progression.Features;
using ToyBox.Infrastructure.Inspector;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab;
public partial class PartyFeatureTab : FeatureTab {
    private readonly TimedCache<Dictionary<BaseUnitEntity, float>> m_DistanceToCache = new(() => []);
    [LocalizedString("ToyBox_Features_PartyTab_PartyFeatureTab_PartyLevelText", "Party Level")]
    private static partial string m_PartyLevelText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_PartyFeatureTab_InspectParty_forDebugging__Text", "Inspect Party (for debugging)")]
    private static partial string m_InspectPartyText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_PartyFeatureTab_Name", "Party")]
    public override partial string Name { get; }
    private static PartyTabSectionType m_UncollapsedSection = PartyTabSectionType.None;
    private static BaseUnitEntity? m_UncollapsedUnit = null;
    private static readonly PartyTabSectionType[] m_Sections = [PartyTabSectionType.Classes, PartyTabSectionType.Stats, PartyTabSectionType.Features,
        PartyTabSectionType.Buffs, PartyTabSectionType.Abilities, PartyTabSectionType.Inspect];
    private static readonly Lazy<float> m_InspectLabelWidth = new(() => CalculateLargestLabelSize([m_InspectPartyText]));
    private static bool m_FeatureNeedsUpdate = true;
    private static bool m_BuffNeedsUpdate = true;
    private static bool m_AbilityNeedsUpdate = true;
    private static readonly Browser<BlueprintFeature> m_FeatureBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)EffectiveWindowWidth() - 40);
    private static readonly Browser<BlueprintBuff> m_BuffBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)EffectiveWindowWidth() - 40);
    private static readonly Browser<BlueprintAbility> m_AbilityBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)EffectiveWindowWidth() - 40);
    public PartyFeatureTab() {
        AddFeature(new FeatureBrowserUnitFeature());
    }
    static PartyFeatureTab() {
        Main.OnHideGUIAction += Refresh;
    }
    public static void Refresh() {
        m_UncollapsedSection = PartyTabSectionType.None;
        m_UncollapsedUnit = null;
        m_NameSectionWidth.ForceRefresh();
        FeatureRefresh();
    }
    public static void FeatureRefresh() {
        m_FeatureNeedsUpdate = true;
        m_BuffNeedsUpdate = true;
        m_AbilityNeedsUpdate = true;
    }
    private static readonly TimedCache<float> m_NameSectionWidth = new(() => {
        return CalculateLargestLabelSize(CharacterPicker.CurrentUnits.Select(u => u.CharacterName.Bold()));
    }, 60 * 60 * 24);
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red());
            return;
        }
        if (CharacterPicker.OnFilterPickerGUI(6, GUILayout.ExpandWidth(true))) {
            m_NameSectionWidth.ForceRefresh();
        }
        var units = CharacterPicker.CurrentUnits;
        using (VerticalScope()) {
            using (HorizontalScope()) {
                UI.Label((m_PartyLevelText + ": ").Cyan() + Game.Instance.Player.PartyLevel.ToString().Orange().Bold(), Width(150));
                InspectorUI.InspectToggle("Party", m_InspectPartyText, units, -150, true, Width(m_InspectLabelWidth.Value + UI.DisclosureGlyphWidth.Value));
            }
            var mainChar = GameHelper.GetPlayerCharacter();
            foreach (var unit in units) {
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(Math.Min(EffectiveWindowWidth() * 0.2f, m_NameSectionWidth + 110)))) {
                        UI.Label(ToyBoxUnitHelper.GetUnitName(unit).Orange().Bold(), Width(m_NameSectionWidth));
                        Space(2);

                        UI.EditableLabel(unit.CharacterName, unit.UniqueId, newName => {
                            unit.Description.CustomName = newName;
                            EventBus.RaiseEvent<IUnitNameHandler>(handler => handler.OnUnitNameChanged());
                            Main.ScheduleForMainThread(m_NameSectionWidth.ForceRefresh);
                        });

                        Dictionary<BaseUnitEntity, float> distanceCache = m_DistanceToCache;
                        if (!distanceCache.TryGetValue(unit, out var dist)) {
                            dist = mainChar.DistanceTo(unit);
                            distanceCache[unit] = dist;
                        }
                        Space(13);
                        UI.Label(dist < 1 ? "" : dist.ToString("0") + "m", Width(70));
                        GUILayout.FlexibleSpace();
                    }
                    foreach (var sec in m_Sections) {
                        var isUncollapsed = sec == m_UncollapsedSection && unit == m_UncollapsedUnit;
                        if (UI.DisclosureToggle(ref isUncollapsed, " " + sec.GetLocalized())) {
                            FeatureRefresh();
                            if (isUncollapsed) {
                                m_UncollapsedSection = sec;
                                m_UncollapsedUnit = unit;
                            } else {
                                m_UncollapsedSection = PartyTabSectionType.None;
                                m_UncollapsedUnit = null;
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
                if (m_UncollapsedUnit == unit && m_UncollapsedSection != PartyTabSectionType.None) {
                    using (HorizontalScope()) {
                        switch (m_UncollapsedSection) {
                            case PartyTabSectionType.Inspect: OnInspectGui(unit); break;
                            case PartyTabSectionType.Classes: OnClassesGui(unit); break;
                            case PartyTabSectionType.Features: OnFeaturesGui(unit); break;
                            case PartyTabSectionType.Buffs: OnBuffsGui(unit); break;
                            case PartyTabSectionType.Abilities: OnAbilitiesGui(unit); break;
                            case PartyTabSectionType.Stats: OnStatsGui(unit); break;
                            case PartyTabSectionType.None:
                                break;
                        }
                    }
                }
            }
        }
    }
    private static void OnStatsGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Stats");
    }
    private static void OnInspectGui(BaseUnitEntity unit) {
        InspectorUI.Inspect(unit);
    }
    private static void OnClassesGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Classes");
    }
    private static void OnFeaturesGui(BaseUnitEntity unit) {
        if (m_FeatureNeedsUpdate) {
            m_FeatureNeedsUpdate = false;
            m_FeatureBrowser.UpdateItems(unit.Progression.Features.Enumerable.Select(f => f.Blueprint));
        }
        m_FeatureBrowser.OnGUI(feature => {
            BlueprintUI.BlueprintRowGUI(feature, unit);
        });
    }
    private static void OnAbilitiesGui(BaseUnitEntity unit) {
        if (m_AbilityNeedsUpdate) {
            m_AbilityNeedsUpdate = false;
            m_AbilityBrowser.UpdateItems(unit.Abilities.Enumerable.Select(f => f.Blueprint));
        }
        m_AbilityBrowser.OnGUI(ability => {
            BlueprintUI.BlueprintRowGUI(ability, unit);
        });
    }
    private static void OnBuffsGui(BaseUnitEntity unit) {
        if (m_BuffNeedsUpdate) {
            m_BuffNeedsUpdate = false;
            m_BuffBrowser.UpdateItems(unit.Buffs.Enumerable.Select(f => f.Blueprint));
        }
        m_BuffBrowser.OnGUI(buff => {
            BlueprintUI.BlueprintRowGUI(buff, unit);
        });
    }
}
