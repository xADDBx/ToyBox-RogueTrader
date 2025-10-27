using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Classes.Features.PartyTab;
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
    private static readonly PartyTabSectionType[] m_Sections = [PartyTabSectionType.Classes, PartyTabSectionType.Stats, PartyTabSectionType.Features,
        PartyTabSectionType.Buffs, PartyTabSectionType.Abilities, PartyTabSectionType.Inspect];
    private static readonly Lazy<float> m_InspectLabelWidth = new(() => UI.WidthInDisclosureStyle(m_InspectPartyText));
    public override void InitializeAll() {
        Main.OnHideGUIAction += Refresh;
        base.InitializeAll();
    }
    public override void DestroyAll() {
        Main.OnHideGUIAction -= Refresh;
        base.DestroyAll();
    }
    public PartyFeatureTab() {
        AddFeature(new PartyBrowseFeatsFeature());
        AddFeature(new PartyBrowseAbilitiesFeature());
        AddFeature(new PartyBrowseBuffsFeature());
        AddFeature(new RenameUnitFeature());
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
        return CalculateLargestLabelSize(CharacterPicker.CurrentUnits.Select(u => GetUnitName(u) + " "));
    }, 60 * 60 * 24);
    private static string GetUnitName(BaseUnitEntity? unit) {
        return ToyBoxUnitHelper.GetUnitName(unit).Orange().Bold();
    }
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red());
            return;
        }
        if (CharacterPicker.OnFilterPickerGUI(6, GUILayout.ExpandWidth(true))) {
            NameSectionWidth.ForceRefresh();
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
                    using (HorizontalScope(Width(Math.Min(EffectiveWindowWidth() * 0.2f, NameSectionWidth + 110)))) {
                        UI.Label(GetUnitName(unit), Width(NameSectionWidth));
                        Space(2);

                        Feature.GetInstance<RenameUnitFeature>().OnGui(unit);

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
                            case PartyTabSectionType.Inspect: InspectorUI.Inspect(unit); break;
                            case PartyTabSectionType.Features: Feature.GetInstance<PartyBrowseFeatsFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Buffs: Feature.GetInstance<PartyBrowseBuffsFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Abilities: Feature.GetInstance<PartyBrowseAbilitiesFeature>().OnGui(unit); break;
                            case PartyTabSectionType.Classes: OnClassesGui(unit); break;
                            case PartyTabSectionType.Stats: OnStatsGui(unit); break;
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
    private static void OnStatsGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Stats");
    }
    private static void OnClassesGui(BaseUnitEntity unit) {
        Space(10);
#warning TODO
        UI.Label("Uncollapsed Classes");
    }
}
