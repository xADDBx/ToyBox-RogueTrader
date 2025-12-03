using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Classes.Features.PartyTab;
using ToyBox.Features.PartyTab.Careers;
using ToyBox.Features.PartyTab.Stats;
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
    private PartyTabSectionType m_UncollapsedSection = PartyTabSectionType.None;
    private BaseUnitEntity? m_UncollapsedUnit = null;
    private static readonly PartyTabSectionType[] m_Sections = [PartyTabSectionType.Careers, PartyTabSectionType.Stats, PartyTabSectionType.Features,
        PartyTabSectionType.Buffs, PartyTabSectionType.Abilities, PartyTabSectionType.Mechadendrites, PartyTabSectionType.FeatureLists, PartyTabSectionType.Inspect];
    private readonly TimedCache<float> m_InspectLabelWidth = new(() => UI.WidthInDisclosureStyle(m_InspectPartyText));
    private void RefreshNameCache() {
        NameSectionWidth.ForceRefresh();
    }
    public override void InitializeAll() {
        Main.OnHideGUIAction += Refresh;
        Main.OnUIScaleChanged += RefreshNameCache;
        base.InitializeAll();
    }
    public override void DestroyAll() {
        Main.OnHideGUIAction -= Refresh;
        Main.OnUIScaleChanged -= RefreshNameCache;
        base.DestroyAll();
    }
    public PartyFeatureTab() {
        AddFeature(new RenameUnitFeature());

        AddFeature(new IncreaseUnitLevelFeature());

        AddFeature(new ModifyExperienceFeature());
        AddFeature(new ModifyCharacterLevelFeature());
        AddFeature(new ShowCareersFeature());

        AddFeature(new PortraitEditorFeature());
        AddFeature(new ChangeGenderFeature());
        AddFeature(new UnitBrowseVoicesFeature());
        AddFeature(new UnitDisableVoiceoverAndBarksFeature());
        AddFeature(new UnitOverrideAiControlBehaviourFeature());
        AddFeature(new UnitOverrideMechanicalSizeFeature());
        AddFeature(new UnitOverrideVisualSizeFeature());
        AddFeature(new UnitModifySoulMarksFeature());

        AddFeature(new PartyBrowseFeatsFeature());

        AddFeature(new PartyBrowseAbilitiesFeature());

        AddFeature(new PartyBrowseBuffsFeature());
    }
    public void Refresh() {
        m_UncollapsedSection = PartyTabSectionType.None;
        m_UncollapsedUnit = null;
        NameSectionWidth.ForceRefresh();
        ClearCache();
    }
    public static void ClearCache() {
        Feature.GetInstance<PartyBrowseFeatsFeature>().ClearFeatureCache();
        Feature.GetInstance<PartyBrowseAbilitiesFeature>().ClearFeatureCache();
        Feature.GetInstance<PartyBrowseBuffsFeature>().ClearFeatureCache();
    }
    public static void FeatureRefresh() {
        Feature.GetInstance<PartyBrowseFeatsFeature>().MarkInvalid();
        Feature.GetInstance<PartyBrowseAbilitiesFeature>().MarkInvalid();
        Feature.GetInstance<PartyBrowseBuffsFeature>().MarkInvalid();
    }
    public readonly TimedCache<float> NameSectionWidth = new(() => {
        return CalculateLargestLabelWidth(CharacterPicker.CurrentUnits.Select(u => GetUnitName(u) + " "));
    }, 60 * 60 * 24);
    private static string GetUnitName(BaseUnitEntity? unit) {
        return ToyBoxUnitHelper.GetUnitName(unit).Orange().Bold();
    }
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        if (CharacterPicker.OnFilterPickerGUI(6, GUILayout.ExpandWidth(true))) {
            NameSectionWidth.ForceRefresh();
        }
        var units = CharacterPicker.CurrentUnits;
        using (VerticalScope()) {
            using (HorizontalScope()) {
                UI.Label((m_PartyLevelText + ": ").Cyan() + Game.Instance.Player.PartyLevel.ToString().Orange().Bold(), Width(150 * Main.UIScale));
                InspectorUI.InspectToggle("Party", m_InspectPartyText, units, -150, true, Width(m_InspectLabelWidth.Value + UI.DisclosureGlyphWidth.Value));
            }
            if (units.Count == 0) {
                return;
            }
            var mainChar = GameHelper.GetPlayerCharacter();
            Dictionary<BaseUnitEntity, float> distanceCache = m_DistanceToCache.Value;
            float distanceLabelWidth;
            if (distanceCache.Count == 0) {
                foreach (var unit in units) {
                    var dist = mainChar.DistanceTo(unit);
                    distanceCache[unit] = dist;
                }
            }
            distanceLabelWidth = CalculateLargestLabelWidth(distanceCache.Values.Select(dist => dist < 1 ? "" : dist.ToString("0") + "m"));
            foreach (var unit in units) {
                using (HorizontalScope()) {
                    UI.Label(GetUnitName(unit), Width(NameSectionWidth));
                    Space(2);

                    Feature.GetInstance<RenameUnitFeature>().OnGui(unit);

                    if (!distanceCache.TryGetValue(unit, out var dist)) {
                        dist = mainChar.DistanceTo(unit);
                        distanceCache[unit] = dist;
                    }

                    Space(5);
                    UI.Label(dist < 1 ? "" : dist.ToString("0") + "m", Width(distanceLabelWidth + 5 * Main.UIScale));
                    Space(5);

                    Feature.GetInstance<IncreaseUnitLevelFeature>().OnGui(unit);

                    Space(15);
                    foreach (var sec in m_Sections) {
                        var isUncollapsed = sec == m_UncollapsedSection && unit == m_UncollapsedUnit;
                        if (UI.DisclosureToggle(ref isUncollapsed, " " + sec.GetLocalized())) {
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
                        Space(50);
                        switch (m_UncollapsedSection) {
                            case PartyTabSectionType.Inspect: InspectorUI.Inspect(unit); break;
                            case PartyTabSectionType.Features: Feature.GetInstance<PartyBrowseFeatsFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Buffs: Feature.GetInstance<PartyBrowseBuffsFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Abilities: Feature.GetInstance<PartyBrowseAbilitiesFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Careers: OnCareersGui(unit); break;
                            case PartyTabSectionType.Stats: OnStatsGui(unit); break;
                            case PartyTabSectionType.FeatureLists: OnFeatureListsGui(unit); break;
                            case PartyTabSectionType.Mechadendrites: OnMechadendritesGui(unit); break;
                            case PartyTabSectionType.None:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
    private static void OnFeatureListsGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Feature Lists");
    }
    private static void OnMechadendritesGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Mechadendrites");
    }
    private static void OnStatsGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        using (VerticalScope()) {
            Div.DrawDiv();
            Feature.GetInstance<PortraitEditorFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<ChangeGenderFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitBrowseVoicesFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitDisableVoiceoverAndBarksFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitOverrideAiControlBehaviourFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitOverrideMechanicalSizeFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitOverrideVisualSizeFeature>().OnGui(unit);
            Div.DrawDiv();
            Feature.GetInstance<UnitModifySoulMarksFeature>().OnGui(unit);
        }
    }
    private static void OnCareersGui(BaseUnitEntity unit) {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                Feature.GetInstance<ModifyCharacterLevelFeature>().OnGui(unit);
            }
            using (HorizontalScope()) {
                Feature.GetInstance<ModifyExperienceFeature>().OnGui(unit);
            }
            using (HorizontalScope()) {
                Feature.GetInstance<ShowCareersFeature>().OnGui(unit);
            }
        }
    }
}
