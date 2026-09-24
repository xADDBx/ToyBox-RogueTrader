using Kingmaker.Code.UI.MVVM.View.Overtips.SystemMap;
using Kingmaker.Code.UI.MVVM.VM.Overtips.SystemMap;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Star system map planet-name coloring (REPORT-starmap-planets). One postfix
/// on OvertipPlanetView.SetPlanetName: vanilla has just written its state
/// color, so we read the bound OvertipEntityPlanetVM's refreshed bools,
/// classify via StarMapPlanetColorRules and override the TMP label color.
/// The opt-in "unscanned reveal" additionally swaps vanilla's "?" sprite for
/// the real name; that visibility is restored by re-applying vanilla's own
/// rule (label iff scanned, "?" sprite iff not), so it is correct at any scan
/// state. The label color is restored from a per-view snapshot (CWT + holder
/// list, OvertipNameColorsFeature pattern) on feature-off, sub-toggle flips,
/// and unclassified states. Refresh timing is vanilla's own chain (bind, scan
/// via arrival-screen close, game-mode and area changes) - extractor/colony/
/// quest changes surface on map re-entry, which CheckIconStateAndScan covers;
/// no extra EventBus subscription is needed.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.StarMapPlanetColorsFeature")]
public partial class StarMapPlanetColorsFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;

    // Palette mirrors StarMapPlanetColorRules.ColorOf byte-for-byte (hex in
    // comments); duplicated as UnityEngine.Color so the postfix never
    // allocates tuples per call. Vanilla is NOT here - it is the snapshot.
    private static readonly Color UnscannedColor = new(1f, 1f, 1f, 1f);                     // #FFFFFF
    private static readonly Color QuestColor = new(1f, 183f / 255f, 0f, 1f);                // #FFB700
    private static readonly Color PoiColor = new(1f, 136f / 255f, 0f, 1f);                  // #FF8800
    private static readonly Color ColonyColor = new(68f / 255f, 204f / 255f, 68f / 255f, 1f); // #44CC44
    private static readonly Color ExtractorColor = new(0f, 170f / 255f, 170f / 255f, 1f);   // #00AAAA
    private static readonly Color ResourceColor = new(0f, 204f / 255f, 1f, 1f);             // #00CCFF
    private static readonly Color DoneColor = new(176f / 255f, 176f / 255f, 176f / 255f, 1f); // #B0B0B0
    private static readonly Color HiddenColor = new(160f / 255f, 144f / 255f, 128f / 255f, 1f); // #A09080

    private static int s_Failures;

    // Keyed on the pooled VIEW (not the label): the record needs both the
    // name label and the "?" sprite, and a pooled rebind reuses the view for
    // another planet while both children stay the same objects.
    private sealed class ViewRecord {
        public TextMeshProUGUI Label = null!;
        public Image? UnknownImage;
        public Color OriginalColor;
        public OvertipEntityPlanetVM? Vm;
        // True while WE forced the name label visible for an unscanned planet
        // (vanilla rule: label iff scanned, "?" sprite iff not).
        public bool RevealedUnscannedName;
    }

    private static readonly ConditionalWeakTable<OvertipPlanetView, ViewRecord> s_Views = new();

    // net481 ConditionalWeakTable has no enumeration; the holder list drives
    // the restore-on-off sweep, the sub-toggle refresh sweep, and pruning.
    private sealed class ViewHolder {
        public readonly WeakReference<OvertipPlanetView> Weak;
        public readonly ViewRecord Record;

        public ViewHolder(OvertipPlanetView view, ViewRecord record) {
            Weak = new WeakReference<OvertipPlanetView>(view);
            Record = record;
        }
    }

    private static readonly List<ViewHolder> s_Written = new();

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.StarMapPlanetColorsFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableStarMapPlanetColors;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_Name", "Star Map Planet Name Colors")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_Description", "Colors planet names on the star system map: amber = active quest or rumour, orange = unexplored points of interest, green = colony, teal = extractor placed, cyan = unmined resources, light grey = fully explored (nothing left), grey-brown = hidden remainder. Unscanned planets can show their real name in white instead of the vanilla '?' mark (spoilers).")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_m_ShowUnscannedText", "Show names of unscanned planets (white; vanilla hides them behind '?')")]
    private static partial string m_ShowUnscannedText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_m_QuestText", "Color quest/rumour planets (amber)")]
    private static partial string m_QuestText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_m_ResourcesText", "Color resource planets (cyan) and extractor planets (teal)")]
    private static partial string m_ResourcesText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_StarMapPlanetColorsFeature_m_DoneText", "Color finished planets (light grey = done, grey-brown = hidden remainder)")]
    private static partial string m_DoneText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
            if (IsEnabled) {
                using (HorizontalScope()) {
                    Space(50);
                    using (VerticalScope()) {
                        _ = UI.Toggle(m_ShowUnscannedText, "", ref Settings.StarMapShowUnscannedNames, RefreshAll, RefreshAll);
                        _ = UI.Toggle(m_QuestText, "", ref Settings.StarMapColorQuest, RefreshAll, RefreshAll);
                        _ = UI.Toggle(m_ResourcesText, "", ref Settings.StarMapColorResources, RefreshAll, RefreshAll);
                        _ = UI.Toggle(m_DoneText, "", ref Settings.StarMapColorDone, RefreshAll, RefreshAll);
                    }
                }
            }
        }
    }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the 3-strike counter (same policy as the marker
        // features: only consecutive failures may auto-disable).
        s_Failures = 0;
    }

    // User-off live sweep: restore every color we ever wrote and re-apply
    // vanilla's name-visibility rule where we had revealed an unscanned name.
    public override void Disable() {
        try {
            RestoreAll();
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: star map color restore-on-off failed: {ex.Message}");
        }
        base.Disable();
    }

    // Sub-toggle flip: re-classify every live record with the new settings so
    // the star map (if open behind the ToyBox window) recolors immediately.
    private static void RefreshAll() {
        try {
            for (var i = s_Written.Count - 1; i >= 0; i--) {
                if (!s_Written[i].Weak.TryGetTarget(out var view) || view == null) {
                    s_Written.RemoveAt(i);
                    continue;
                }
                var record = s_Written[i].Record;
                var vm = record.Vm;
                if (vm == null) {
                    continue;
                }
                Apply(record, vm, ClassifyVm(vm));
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void RestoreAll() {
        for (var i = s_Written.Count - 1; i >= 0; i--) {
            var holder = s_Written[i];
            if (holder.Weak.TryGetTarget(out var view) && view != null) {
                holder.Record.Label.color = holder.Record.OriginalColor;
                RestoreVanillaNameVisibility(holder.Record);
                _ = s_Views.Remove(view);
            }
            s_Written.RemoveAt(i);
        }
    }

    [HarmonyPatch(typeof(OvertipPlanetView), "SetPlanetName"), HarmonyPostfix]
    private static void OvertipPlanetView_SetPlanetName_Postfix(OvertipPlanetView __instance) {
        try {
            var vm = __instance.ViewModel;
            if (vm == null || __instance.m_PlanetNameLabel == null) {
                return;
            }
            // Prune dead views (system changes destroy scene UI) so the
            // holder list cannot leak for the session.
            _ = s_Written.RemoveAll(holder => !holder.Weak.TryGetTarget(out _));
            var record = s_Views.GetValue(__instance, view => {
                var created = new ViewRecord {
                    Label = view.m_PlanetNameLabel,
                    UnknownImage = view.m_UnknownPlanetNameImage,
                    OriginalColor = view.m_PlanetNameLabel.color,
                };
                s_Written.Add(new ViewHolder(view, created));
                return created;
            });
            // Pooled view rebound to another planet: vanilla just rewrote the
            // label color (scanned) or left it (unscanned) and re-applied its
            // visibility rule for the NEW VM. Refresh the VM ref and re-capture
            // the baseline - never while OUR color is on screen, or our write
            // would become the "original" and never restore.
            if (!ReferenceEquals(record.Vm, vm)) {
                record.Vm = vm;
                record.RevealedUnscannedName = false;
                if (!IsOurColor(record.Label.color)) {
                    record.OriginalColor = record.Label.color;
                }
            }
            Apply(record, vm, ClassifyVm(vm));
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static StarMapPlanetColorState ClassifyVm(OvertipEntityPlanetVM vm) {
        var data = vm.PlanetView.Value?.Data;
        return StarMapPlanetColorRules.Classify(
            vm.PlanetIsVisible.Value,
            vm.PlanetIsScanned.Value,
            vm.HasQuest.Value || vm.HasRumour.Value,
            vm.HasPoi.Value,
            vm.HasColony.Value,
            vm.HasExtractor.Value,
            vm.HasResource.Value,
            data?.IsFullyExplored ?? false,
            Settings.StarMapShowUnscannedNames,
            Settings.StarMapColorQuest,
            Settings.StarMapColorResources,
            Settings.StarMapColorDone);
    }

    private static void Apply(ViewRecord record, OvertipEntityPlanetVM vm, StarMapPlanetColorState state) {
        if (state == StarMapPlanetColorState.Vanilla) {
            record.Label.color = record.OriginalColor;
            RestoreVanillaNameVisibility(record);
            return;
        }
        if (state == StarMapPlanetColorState.Unscanned) {
            // Reveal (opt-in): vanilla deactivated the label and shows the "?"
            // sprite. PlanetName.Value is filled by the VM regardless of scan
            // state, so the real name is available without new plumbing.
            record.Label.text = vm.PlanetName.Value;
            record.Label.color = UnscannedColor;
            record.Label.gameObject.SetActive(true);
            if (record.UnknownImage != null) {
                record.UnknownImage.gameObject.SetActive(false);
            }
            record.RevealedUnscannedName = true;
            return;
        }
        // Scanned states: vanilla's SetPlanetName already activated the label
        // (and hid the "?" sprite) in this very call - recolor only.
        record.Label.color = StateColor(state);
        record.RevealedUnscannedName = false;
        if (state != StarMapPlanetColorState.Poi
            && state != StarMapPlanetColorState.Colony
            && TmProbe.OncePerArea("starmap-planet-color")) {
            // TM-probe : first signal color written this area proves
            // the postfix -> VM -> label chain end to end.
            TmProbe.Log($"first star map planet color written state={state} color=#{ColorUtility.ToHtmlStringRGBA(record.Label.color)}");
        }
    }

    private static void RestoreVanillaNameVisibility(ViewRecord record) {
        if (!record.RevealedUnscannedName) {
            return;
        }
        record.RevealedUnscannedName = false;
        // Vanilla's exact rule from SetPlanetName: name label iff scanned,
        // "?" sprite iff not. Recomputed (not snapshotted) so it is correct
        // regardless of when the scan state last changed. The label text
        // itself needs no restore: vanilla rewrites it on every scanned
        // SetPlanetName, and an unscanned label is now inactive anyway.
        var scanned = record.Vm?.PlanetIsScanned.Value ?? false;
        record.Label.gameObject.SetActive(scanned);
        if (record.UnknownImage != null) {
            record.UnknownImage.gameObject.SetActive(!scanned);
        }
    }

    // Color provenance: our exact palette values identify labels whose
    // current color is OURS (snapshot must not capture it on pooled rebind).
    private static bool IsOurColor(Color color) =>
        color == UnscannedColor || color == QuestColor || color == PoiColor || color == ColonyColor
        || color == ExtractorColor || color == ResourceColor || color == DoneColor || color == HiddenColor;

    private static Color StateColor(StarMapPlanetColorState state) => state switch {
        StarMapPlanetColorState.Unscanned => UnscannedColor,
        StarMapPlanetColorState.Quest => QuestColor,
        StarMapPlanetColorState.Poi => PoiColor,
        StarMapPlanetColorState.Colony => ColonyColor,
        StarMapPlanetColorState.Extractor => ExtractorColor,
        StarMapPlanetColorState.Resource => ResourceColor,
        StarMapPlanetColorState.Done => DoneColor,
        StarMapPlanetColorState.Hidden => HiddenColor,
        _ => Color.white,
    };

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: star map planet color patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling StarMapPlanetColorsFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<StarMapPlanetColorsFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
