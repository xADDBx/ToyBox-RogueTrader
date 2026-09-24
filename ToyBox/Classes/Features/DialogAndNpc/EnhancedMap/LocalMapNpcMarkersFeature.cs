using Kingmaker;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.Common.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.LocalMap;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public enum LocalMapNpcMarkerDetail {
    All,
    Active,
    NewOnly,
}

public static partial class LocalMapNpcMarkerDetail_Localizer {
    public static string GetLocalized(this LocalMapNpcMarkerDetail detail) => detail switch {
        LocalMapNpcMarkerDetail.All => m_AllText,
        LocalMapNpcMarkerDetail.Active => m_ActiveText,
        LocalMapNpcMarkerDetail.NewOnly => m_NewOnlyText,
        _ => "!!Error Unknown LocalMapNpcMarkerDetail!!",
    };

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkerDetail_AllText", "All")]
    private static partial string m_AllText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkerDetail_ActiveText", "Only Triggerable")]
    private static partial string m_ActiveText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkerDetail_NewOnlyText", "New Only")]
    private static partial string m_NewOnlyText { get; }
}

/// <summary>
/// Appends one custom marker VM per classifier-positive NPC to the local map
/// (R1/R3). The vanilla interestingness check is dead code (literally
/// Any(c => false)), so friendly dialog NPCs never appear without this.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapNpcMarkersFeature")]
public partial class LocalMapNpcMarkersFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;

    // v1 state encoding is tint-only on shipped sprites; shape differentiation
    // is deferred to the M6 runtime sprite probe behind the same enum.
    private static readonly Color NewColor = new(1f, 0.85f, 0.25f, 1f);
    private static readonly Color RepeatColor = new(0.4f, 0.62f, 1f, 1f);
    private static readonly Color InactiveColor = new(0.55f, 0.55f, 0.55f, 0.6f);

    private static int s_SetMarkersFailures;
    private static int s_SpriteFailures;
    private static int s_LegendFailures;

    // Weak: the VM is disposed when the map closes; a stale ref must never
    // resurrect it. Refreshed by the SetMarkers postfix on every map open.
    private static readonly WeakReference<LocalMapVM> s_CurrentMapVM = new(null!);

    // Sprite captured from the first bound VIP view; also feeds the legend patch.
    internal static Sprite? s_CachedNpcSprite;

    private sealed class TintRecord {
        public Color Original;
        public Color Applied;
    }

    private static readonly ConditionalWeakTable<LocalMapVipMarkerPCView, TintRecord> s_TintedViews = new();

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapNpcMarkersFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLocalMapNpcMarkers;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_Name", "Local Map NPC Dialog Markers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_Description", "Marks NPCs with dialog on the local map: yellow = new dialog, blue = repeatable, dimmed = no triggerable dialog right now (All mode only).")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_DetailText", "NPC Marker Detail")]
    private static partial string m_DetailText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_NewDialogText", "New dialog")]
    private static partial string m_NewDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_RepeatableDialogText", "Repeatable dialog")]
    private static partial string m_RepeatableDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_NoTriggerableDialogText", "No triggerable dialog right now (may change later)")]
    private static partial string m_NoTriggerableDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_OverlappingFormatText", "Overlapping: {0}")]
    private static partial string m_OverlappingFormatText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapNpcMarkersFeature_m_LegendText", "NPC dialog (yellow = new, blue = repeatable)")]
    private static partial string m_LegendText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
            if (IsEnabled) {
                using (HorizontalScope()) {
                    Space(50);
                    UI.Label(m_DetailText);
                    Space(10);
                    UI.SelectionGrid(ref Settings.LocalMapNpcMarkerDetail, 3, e => e.GetLocalized(), Width(0.5f * EffectiveWindowWidth()));
                }
                if (Settings.LocalMapNpcMarkerDetail != s_LastDetail) {
                    s_LastDetail = Settings.LocalMapNpcMarkerDetail;
                    RemoveStaleMarkers();
                }
            }
        }
    }

    // User-off parity with the master toggle: live dispose-then-remove so no
    // marker widget survives until map close. Guarded because the
    // auto-disable path reaches this from inside a patch catch block.
    public override void Disable() {
        try {
            if (TryGetCurrentMapVM(out var vm)) {
                RemoveNpcMarkers(vm);
            }
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: live marker removal failed: {ex.Message}");
        }
        if (s_PumpWired) {
            s_PumpWired = false;
            NpcDialogClassifierRuntime.Pumped -= OnPumped;
        }
        base.Disable();
    }

    // Detail-enum live filter: switching modes (e.g. All -> Active) must
    // drop the now-filtered markers (e.g. INACTIVE) immediately.
    private static void RemoveStaleMarkers() {
        if (!TryGetCurrentMapVM(out var vm)) {
            return;
        }
        try {
            for (var i = vm.MarkersVm.Count - 1; i >= 0; i--) {
                if (vm.MarkersVm[i] is ToyBoxLocalMapNpcMarkerVM ours && !PassesDetailFilter(ours.State)) {
                    vm.MarkersVm[i].Dispose();
                    vm.MarkersVm.RemoveAt(i);
                }
            }
            if (s_SetMarkersFailures > 0) {
                s_SetMarkersFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_SetMarkersFailures, "detail filter", ex);
        }
    }

    private static bool s_PumpWired;
    private static LocalMapNpcMarkerDetail s_LastDetail = Settings.LocalMapNpcMarkerDetail;

    public override void Enable() {
        base.Enable();
        // Re-enable also resets the 3-strike counter: transient area-transition
        // exceptions must not keep the feature bricked for the session.
        s_SetMarkersFailures = 0;
        s_SpriteFailures = 0;
        s_LegendFailures = 0;
        NpcDialogClassifierRuntime.EnsureSubscribed();
        NpcDialogClassifierRuntime.EnsurePumpStarted();
        if (!s_PumpWired) {
            s_PumpWired = true;
            // Deferred classifications land in the cache a few frames after
            // map-open; re-run the idempotent append to materialize them.
            NpcDialogClassifierRuntime.Pumped += OnPumped;
        }
    }

    private static void OnPumped(object? sender, EventArgs eventArgs) {
        if (!TryGetCurrentMapVM(out var vm)) {
            return;
        }
        try {
            AppendNpcMarkers(vm);
            if (s_SetMarkersFailures > 0) {
                s_SetMarkersFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_SetMarkersFailures, "SetMarkers", ex);
        }
    }

    internal static bool TryGetCurrentMapVM(out LocalMapVM vm) {
        _ = s_CurrentMapVM.TryGetTarget(out vm!);
        return vm != null;
    }

    /// <summary>
    /// Shared append routine: the ONLY thing master-ON may call after the map
    /// is already open. Never re-invoke SetMarkers - its non-clearing Adds
    /// would duplicate every vanilla marker.
    /// </summary>
    internal static void AppendNpcMarkers(LocalMapVM vm) {
        if (!NpcDialogClassifier.NpcMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapNpcMarkers)) {
            return;
        }
        var existing = new HashSet<BaseUnitEntity>();
        foreach (var marker in vm.MarkersVm) {
            if (marker is ToyBoxLocalMapNpcMarkerVM ours) {
                _ = existing.Add(ours.Unit);
            }
        }
        var candidates = new List<MarkerCandidate>();
        var units = new List<BaseUnitEntity>();
        foreach (var unit in Game.Instance.State.AllBaseUnits.All) {
            if (unit == null || !unit.IsInGame || unit.LifeState.IsHiddenBecauseDead) {
                continue;
            }
            if (!LocalMapModel.IsInCurrentArea(unit.Position)) {
                continue;
            }
            if (existing.Contains(unit)) {
                continue;
            }
            var fresh = NpcDialogClassifierRuntime.GetFreshState(unit);
            if (fresh == null) {
                // Not classified yet (or stale): defer, the pump re-runs this
                // routine via the Pumped event with fresh cache entries.
                _ = NpcDialogClassifierRuntime.Scheduler.Enqueue(unit);
                continue;
            }
            if (!PassesDetailFilter(fresh.Value)) {
                continue;
            }
            // Compute the layout point BEFORE adding the unit so a no-graph
            // map skips the unit without desyncing the units/candidates pair.
            if (!TryToLayoutPoint(unit, out var point)) {
                continue;
            }
            units.Add(unit);
            candidates.Add(new MarkerCandidate(units.Count - 1, fresh.Value, point));
        }
        AddCandidateMarkers(vm, units, candidates);
    }

    internal static void RemoveNpcMarkers(LocalMapVM vm) {
        // Dispose THEN remove: the view only observes ObserveAdd, mimicking
        // LocalMapVM.OnUpdateHandler's own removal sequence.
        for (var i = vm.MarkersVm.Count - 1; i >= 0; i--) {
            if (vm.MarkersVm[i] is ToyBoxLocalMapNpcMarkerVM) {
                vm.MarkersVm[i].Dispose();
                vm.MarkersVm.RemoveAt(i);
            }
        }
    }

    private static void AddCandidateMarkers(LocalMapVM vm, List<BaseUnitEntity> units, List<MarkerCandidate> candidates) {
        var suppressed = MarkerLayout.SelectSuppressed(candidates);
        var suppressedBySuppressor = new Dictionary<int, List<int>>();
        var suppressedIndex = suppressed.Select(p => p.Suppressed).ToHashSet();
        foreach (var (suppressedIdx, suppressorIdx) in suppressed) {
            if (!suppressedBySuppressor.TryGetValue(suppressorIdx, out var list)) {
                list = [];
                suppressedBySuppressor[suppressorIdx] = list;
            }
            list.Add(suppressedIdx);
        }
        BaseUnitEntity? firstAppended = null;
        NpcDialogMarkerState firstState = NpcDialogMarkerState.None;
        for (var i = 0; i < units.Count; i++) {
            var unit = units[i];
            var state = candidates[i].State;
            var description = $"{unit.CharacterName}\n{StateText(state)}";
            if (suppressedIndex.Contains(i)) {
                continue;
            }
            if (suppressedBySuppressor.TryGetValue(i, out var hidden)) {
                var names = string.Join(", ", hidden.Select(idx => units[idx].CharacterName));
                description += $"\n{string.Format(m_OverlappingFormatText, names)}";
            }
            vm.MarkersVm.Add(new ToyBoxLocalMapNpcMarkerVM(unit, state, description));
            if (firstAppended == null) {
                firstAppended = unit;
                firstState = state;
            }
        }
        if (firstAppended != null && TmProbe.OncePerArea("npc-marker")) {
            // TM-probe (v10): first custom NPC marker that reached the map's
            // marker list this area.
            TmProbe.Log($"first NPC map marker appended state={firstState} name={firstAppended.CharacterName}");
        }
    }

    internal static bool PassesDetailFilter(NpcDialogMarkerState state) => Settings.LocalMapNpcMarkerDetail switch {
        LocalMapNpcMarkerDetail.All => state is NpcDialogMarkerState.New or NpcDialogMarkerState.Repeat or NpcDialogMarkerState.Inactive,
        LocalMapNpcMarkerDetail.Active => state is NpcDialogMarkerState.New or NpcDialogMarkerState.Repeat,
        LocalMapNpcMarkerDetail.NewOnly => state is NpcDialogMarkerState.New,
        _ => false,
    };

    private static string StateText(NpcDialogMarkerState state) => state switch {
        NpcDialogMarkerState.New => m_NewDialogText,
        NpcDialogMarkerState.Repeat => m_RepeatableDialogText,
        NpcDialogMarkerState.Inactive => m_NoTriggerableDialogText,
        _ => string.Empty,
    };

    internal static Color StateColor(NpcDialogMarkerState state) => state switch {
        NpcDialogMarkerState.New => NewColor,
        NpcDialogMarkerState.Repeat => RepeatColor,
        NpcDialogMarkerState.Inactive => InactiveColor,
        _ => Color.white,
    };

    // Layout points are viewport * 1000, so MarkerLayout.MinSpacing (12) is
    // 1.2% of a map axis (~12 px on a ~1000 px image). Tuned at runtime (M6).
    private static bool TryToLayoutPoint(BaseUnitEntity unit, out System.Numerics.Vector2 point) {
        point = default;
        var renderer = WarhammerLocalMapRenderer.Instance;
        if (renderer == null) {
            WarnNoGraphMap("renderer missing");
            return false;
        }
        try {
            var viewport = renderer.WorldToViewportPoint(unit.Position);
            point = new System.Numerics.Vector2(viewport.x * 1000f, viewport.y * 1000f);
            return true;
        } catch (Exception ex) {
            // WorldToViewportPoint NREs on maps without a baked navmesh graph;
            // that is an AREA property, not a patch failure - never feed it
            // to the auto-disable counter.
            WarnNoGraphMap(ex.Message);
            return false;
        }
    }

    private static object? s_NoGraphArea;

    private static void WarnNoGraphMap(string why) {
        var area = Game.Instance?.CurrentlyLoadedArea;
        if (ReferenceEquals(area, s_NoGraphArea)) {
            return;
        }
        s_NoGraphArea = area;
        Warn($"ToyBox EnhancedMap: local map has no renderable graph this area, skipping NPC marker layout ({why})");
    }

    [HarmonyPatch(typeof(LocalMapVM), "SetMarkers"), HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    private static void SetMarkers_Postfix(LocalMapVM __instance) {
        try {
            _ = s_CurrentMapVM.TryGetTarget(out var tracked);
            if (!ReferenceEquals(tracked, __instance)) {
                s_CurrentMapVM.SetTarget(__instance);
                // Map close runs the VM's disposables: null the weak target
                // with them so it can never resolve a DISPOSED VM in the GC
                // window (closing mid-backlog or the master hotkey right
                // after close would otherwise append orphan marker VMs).
                __instance.AddDisposable(UniRx.Disposable.Create(() => s_CurrentMapVM.SetTarget(null!)));
            }
            NpcDialogClassifierRuntime.EnsureSubscribed();
            NpcDialogClassifierRuntime.EnsurePumpStarted();
            AppendNpcMarkers(__instance);
            // Reset at the END of the pass: a top-of-try reset would zero
            // the counter right before the risky code, so persistent
            // exceptions would never reach the auto-disable threshold.
            if (s_SetMarkersFailures > 0) {
                s_SetMarkersFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_SetMarkersFailures, "SetMarkers", ex);
        }
    }

    [HarmonyPatch(typeof(LocalMapVipMarkerPCView), "BindViewImplementation"), HarmonyPostfix]
    private static void VipMarkerPCView_BindViewImplementation_Postfix(LocalMapVipMarkerPCView __instance) {
        try {
            var mark = __instance.m_Mark;
            if (mark == null) {
                return;
            }
            LogSpriteProbe(__instance);
            if (__instance.ViewModel is ToyBoxLocalMapNpcMarkerVM ours) {
                if (__instance.m_NpcSprite != null) {
                    s_CachedNpcSprite = __instance.m_NpcSprite;
                }
                // Capture the pre-tint color only on first tint: a pooled
                // rebind of our own marker must keep the original vanilla
                // color, not our previous tint.
                var record = s_TintedViews.GetValue(__instance, view => new TintRecord { Original = view.m_Mark.color });
                record.Applied = StateColor(ours.State);
                mark.sprite = __instance.m_NpcSprite;
                mark.color = record.Applied;
            } else {
                // Pooled view reuse: restore the vanilla sprite choice and
                // the pre-tint color ONLY while the view still shows OUR
                // tint (color provenance) - the interactable-markers feature
                //  patches this method too and tints with its own
                // constants, so an unconditional restore would clobber its
                // tint on shared pooled views.
                mark.sprite = __instance.ViewModel.IsMapObject.Value ? __instance.m_MapObjectSprite : __instance.m_NpcSprite;
                if (s_TintedViews.TryGetValue(__instance, out var record) && mark.color == record.Applied) {
                    mark.color = record.Original;
                    s_TintedViews.Remove(__instance);
                }
            }
            if (s_SpriteFailures > 0) {
                s_SpriteFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_SpriteFailures, "VipMarker sprite", ex);
        }
    }

    // Legend entries for our marker family (M6). Runs inside the legend VM
    // ctor via AddItems; appended after vanilla entries, toggle-guarded.
    [HarmonyPatch(typeof(LocalMapLegendBlockVM), "AddItems"), HarmonyPostfix]
    private static void LocalMapLegendBlockVM_AddItems_Postfix(LocalMapLegendBlockVM __instance) {
        try {
            if (!NpcDialogClassifier.NpcMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapNpcMarkers)) {
                return;
            }
            var sprite = s_CachedNpcSprite;
            if (sprite == null) {
                // First map open binds the legend before any VIP view; the
                // entry appears from the next open. Verified in checklist step 21.
                return;
            }
            __instance.LocalMapItemsVMs.Add(new LocalMapLegendBlockItemVM(sprite, m_LegendText));
            if (s_LegendFailures > 0) {
                s_LegendFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_LegendFailures, "Legend", ex);
        }
    }

    // Shape-encoding probe (v5 M6): v1 state encoding is tint-only. This
    // probe logs the sprite names the VIP marker views actually use so a
    // hollow/slash variant can be chosen at runtime if one exists; if not,
    // tint+tooltip remains the v1 encoding. Evaluate against checklist
    // step 5/6 screenshots; wire a chosen variant behind the existing
    // LocalMapNpcMarkerDetail enum - no new setting.
    private static bool s_SpriteProbeLogged;

    private static void LogSpriteProbe(LocalMapVipMarkerPCView view) {
        if (s_SpriteProbeLogged) {
            return;
        }
        s_SpriteProbeLogged = true;
        Log($"ToyBox EnhancedMap sprite probe: mark={view.m_Mark?.sprite?.name}, mapObject={view.m_MapObjectSprite?.name}, npc={view.m_NpcSprite?.name}");
        // TODO(runtime, checklist step 5/6): if the probe reveals a
        // hollow/slashed marker sprite, add the shape to StateColor's
        // call-site in this postfix (sprite per NpcDialogMarkerState).
    }

    private static void HandleFailure(ref int failures, string patch, Exception ex) {
        failures++;
        if (failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: {patch} patch failed ({failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling LocalMapNpcMarkersFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<LocalMapNpcMarkersFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
