using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.WarhammerSurfaceCombatPrototype;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.RTSpecific.CustomizePsychicPhenomena")]
// Early Init to prevent threading from causing issues with mods adding new phenomenas
public partial class CustomizePsychicPhenomena : FeatureWithPatch, INeedEarlyInitFeature {
    private IEnumerable<BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData>? m_BackupPsychicPhenomena;
    private IEnumerable<BlueprintAbilityReference>? m_BackupPerilsOfTheWarpMinor;
    private IEnumerable<BlueprintAbilityReference>? m_BackupPerilsOfTheWarpMajor;
    private static string GetPsychicPhenomenaString(BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData p) {
        var text = p.Bark?.Entries?[0]?.Text;
        if (text != null) {
            return $"{text.String.GetText()} ({text.name})";
        } else {
            return "<Null>!".Red().Bold();
        }
    }
    private static string GetPsychicPhenomenaKey(BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData p) {
        var text = p.Bark?.Entries?[0]?.Text;
        if (text != null) {
            return text.String?.Key ?? text.name;
        } else {
            return "<Null>!".Red().Bold();
        }
    }
    private readonly Browser<BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData> m_PsychicPhenomenaBrowser = new(GetPsychicPhenomenaString, GetPsychicPhenomenaString, overridePageWidth: (int)(0.8f * EffectiveWindowWidth()));
    private readonly Browser<BlueprintAbility> m_PerilsOfTheWarpMinorBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, overridePageWidth:(int)(0.8f * EffectiveWindowWidth()));
    private readonly Browser<BlueprintAbility> m_PerilsOfTheWarpMajorBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, overridePageWidth: (int)(0.8f * EffectiveWindowWidth()));
    private readonly TimedCache<float> m_ButtonWidth = new(() => CalculateLargestLabelSize([m_StopExcludingLocalizedText, m_ExcludeLocalizedText], GUI.skin.button)); 
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableCustomizePsychicPhenomena;
        }
    }
    public override void Destroy() {
        base.Destroy();
        RestorePhenomena();
    }
    private void BackupPhenomena() {
        var root = BlueprintRoot.Instance.WarhammerRoot.PsychicPhenomenaRoot;
        m_BackupPsychicPhenomena = root.PsychicPhenomena.Select(p => p);
        m_PsychicPhenomenaBrowser.UpdateItems(m_BackupPsychicPhenomena);
        m_BackupPerilsOfTheWarpMinor = root.PerilsOfTheWarpMinor.Select(p => p);
        m_PerilsOfTheWarpMinorBrowser.UpdateItems(m_BackupPerilsOfTheWarpMinor.Select(r => r.Get()));
        m_BackupPerilsOfTheWarpMajor = root.PerilsOfTheWarpMajor.Select(p => p);
        m_PerilsOfTheWarpMajorBrowser.UpdateItems(m_BackupPerilsOfTheWarpMajor.Select(r => r.Get()));
    }
    private void RestorePhenomena() {
        if (m_BackupPsychicPhenomena != null) {
            var root = BlueprintRoot.Instance.WarhammerRoot.PsychicPhenomenaRoot;
            root.PsychicPhenomena = [.. root.PsychicPhenomena.Union(m_BackupPsychicPhenomena)];
            root.PerilsOfTheWarpMinor = [.. root.PerilsOfTheWarpMinor.Union(m_BackupPerilsOfTheWarpMinor)];
            root.PerilsOfTheWarpMajor = [.. root.PerilsOfTheWarpMajor.Union(m_BackupPerilsOfTheWarpMajor)];
            m_BackupPsychicPhenomena = root.PsychicPhenomena;
            m_BackupPerilsOfTheWarpMinor = root.PerilsOfTheWarpMinor;
            m_BackupPerilsOfTheWarpMajor = root.PerilsOfTheWarpMajor;
        }
    }
    private static T[] RemoveAll<T>(T[] collection, Func<T, bool> pred) {
        return [.. collection.Where(x => !pred(x))];
    }
    private void RemovePhenomena() {
        if (m_BackupPsychicPhenomena == null) {
            BackupPhenomena();
        }
        var root = BlueprintRoot.Instance.WarhammerRoot.PsychicPhenomenaRoot;
        root.PsychicPhenomena = RemoveAll(root.PsychicPhenomena, phenomena => Settings.ExcludedRandomPhenomena.Contains(phenomena.Bark?.Entries?[0]?.Text?.String?.Key ?? "<null>"));
        root.PerilsOfTheWarpMinor = RemoveAll(root.PerilsOfTheWarpMinor, minor => Settings.ExcludedPerilsMinor.Contains(minor.guid ?? "<null>"));
        root.PerilsOfTheWarpMajor = RemoveAll(root.PerilsOfTheWarpMajor, major => Settings.ExcludedPerilsMajor.Contains(major.guid ?? "<null>"));
    }
    private void PerilsGUI(BlueprintAbility item, ref HashSet<string> collection) {
        using (HorizontalScope()) {
            var key = item.AssetGuid;
            var isExcluded = collection.Contains(key);
            var name = BPHelper.GetTitle(item) + ": " + BPHelper.GetDescription(item) ?? "<Null>".Orange();
            if (isExcluded) {
                if (UI.Button(m_StopExcludingLocalizedText.Cyan(), null, null, Width(m_ButtonWidth))) {
                    collection.Remove(key);
                    RestorePhenomena();
                    RemovePhenomena();
                }
                Space(10);
                UI.Label(name.Cyan());
            } else {
                if (UI.Button(m_ExcludeLocalizedText, null, null, Width(m_ButtonWidth))) {
                    collection.Add(key);
                    RemovePhenomena();
                }
                Space(10);
                UI.Label(name);
            }
        }
    }
    private bool m_IsCustomizing = false;
    public override void OnGui() {
        _ = UI.Toggle(Name, Description, ref IsEnabled, Initialize, Destroy);
        if (Settings.EnableCustomizePsychicPhenomena) {
            using (HorizontalScope()) {
                Space(40);
                using (VerticalScope()) {
                    UI.Toggle(m_CustomizeLocalizedText, null, ref m_IsCustomizing);
                    if (m_IsCustomizing) {
                        if (UI.Button(m_RefreshAvailablePhenomenas_PerilLocalizedText) || m_BackupPsychicPhenomena == null) {
                            RestorePhenomena();
                            BackupPhenomena();
                            RemovePhenomena();
                        }

                        UI.Label(m_PsychicPhenomenaLocalizedText);
                        m_PsychicPhenomenaBrowser.OnGUI(item => {
                            using (HorizontalScope()) {
                                var key = GetPsychicPhenomenaKey(item);
                                var isExcluded = Settings.ExcludedRandomPhenomena.Contains(key);
                                var name = GetPsychicPhenomenaString(item);
                                if (isExcluded) {
                                    if (UI.Button(m_StopExcludingLocalizedText.Cyan(), null, null, Width(m_ButtonWidth))) {
                                        Settings.ExcludedRandomPhenomena.Remove(key);
                                        RestorePhenomena();
                                        RemovePhenomena();
                                    }
                                    Space(10);
                                    UI.Label(name.Cyan());
                                } else {
                                    if (UI.Button(m_ExcludeLocalizedText, null, null, Width(m_ButtonWidth))) {
                                        Settings.ExcludedRandomPhenomena.Add(key);
                                        RemovePhenomena();
                                    }
                                    Space(10);
                                    UI.Label(name);
                                }
                            }
                        });
                        UI.Label(m_MinorPerilsLocalizedText);
                        m_PerilsOfTheWarpMinorBrowser.OnGUI(item => PerilsGUI(item, ref Settings.ExcludedPerilsMinor));
                        UI.Label(m_MajorPerilsLocalizedText);
                        m_PerilsOfTheWarpMajorBrowser.OnGUI(item => PerilsGUI(item, ref Settings.ExcludedPerilsMajor));
                    }
                }
            }
        }
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_CustomizeLocalizedText", "Open Customize UI")]
    private static partial string m_CustomizeLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_PsychicPhenomenaLocalizedText", "Psychic Phenomena")]
    private static partial string m_PsychicPhenomenaLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_MinorPerilsLocalizedText", "Minor Perils")]
    private static partial string m_MinorPerilsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_MajorPerilsLocalizedText", "Major Perils")]
    private static partial string m_MajorPerilsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_Name", "Customize Psychic Phenomena / Perils of the Warp")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_Description", "Allows disabling specific psychic phenomenas or perils of the warp.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_RefreshAvailablePhenomenas_PerilLocalizedText", "Refresh Available Phenomenas/Perils (for mod compat)")]
    private static partial string m_RefreshAvailablePhenomenas_PerilLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_StopExcludingLocalizedText", "Stop Excluding")]
    private static partial string m_StopExcludingLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_CustomizePsychicPhenomena_m_ExcludeLocalizedText", "Exclude")]
    private static partial string m_ExcludeLocalizedText { get; }
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init), Priority.Last), HarmonyPostfix]
    private static void InitializePatch() {
        var feature = GetInstance<CustomizePsychicPhenomena>();
        if (feature.IsEnabled) {
            feature.BackupPhenomena();
            feature.RemovePhenomena();
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.RTSpecific.CustomizePsychicPhenomena";
        }
    }
}
