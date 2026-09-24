using Kingmaker;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.Common.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Appends one custom marker per dialog-opening or skill-check map object
/// . Scope per REPORT-exits-interactables: InteractionDialogPart
/// (opens a dialog on interact) and InteractionSkillCheckPart are IN;
/// devices, actions, barks and every other passive text/flavor overlay are
/// OUT, and objects owned by the exits feature (AreaTransitionPart holders)
/// are never duplicated. State encoding is tint-only v1 like the NPC
/// markers, and "used up" content is simply absent (absence convention - no
/// gone-forever markers). Classification happens at map-open only: every
/// input is a cheap persisted bool, so the NPC classifier's cache/scheduler
/// pump is deliberately NOT reused.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapInteractableMarkersFeature")]
public partial class LocalMapInteractableMarkersFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;

    // Volume guard: skill checks number ~1400 game-wide; per-area counts are
    // far smaller, but the cap keeps a degenerate area from flooding the map.
    private const int MaxAppendsPerArea = MapObjectMarkerClassifier.DefaultMaxAppendsPerArea;

    // Tint-only v1. These differ from the NPC feature's constants by a hair
    // ON PURPOSE: the two VIP-view postfixes restore each other's tint by
    // color provenance, so equal constants would make the restore ambiguous.
    private static readonly Color NewColor = new(1f, 0.84f, 0.26f, 1f);
    private static readonly Color RepeatColor = new(0.42f, 0.64f, 1f, 1f);
    private static readonly Color CheckColor = NewColor;

    private static int s_SetMarkersFailures;
    private static int s_SpriteFailures;
    private static int s_LegendFailures;

    // Weak: the VM is disposed when the map closes; a stale ref must never
    // resurrect it. Refreshed by the SetMarkers postfix on every map open.
    private static readonly WeakReference<LocalMapVM> s_CurrentMapVM = new(null!);

    // Sprite captured from the first bound VIP view; also feeds the legend
    // patch (NPC feature parity).
    internal static Sprite? s_CachedMapObjectSprite;

    private sealed class TintRecord {
        public Color Original;
        public Color Applied;
    }

    private static readonly ConditionalWeakTable<LocalMapVipMarkerPCView, TintRecord> s_TintedViews = new();

    private static object? s_CapWarnArea;

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapInteractableMarkersFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLocalMapInteractableMarkers;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_Name", "Local Map Interactable Markers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_Description", "Marks interactable objects that open a dialog or hold a skill check (consoles, shrines, cogitators) on the local map: yellow = new dialog or unpassed check, blue = already-seen dialog. Passed and locked-out checks are hidden - nothing to come back for. Plain devices and text-only overlays are not marked.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_m_NewDialogText", "New dialog")]
    private static partial string m_NewDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_m_RepeatableDialogText", "Repeatable dialog")]
    private static partial string m_RepeatableDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_m_SkillCheckAvailableText", "Skill check available")]
    private static partial string m_SkillCheckAvailableText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapInteractableMarkersFeature_m_LegendText", "Dialog / skill-check objects (yellow = new, blue = repeatable)")]
    private static partial string m_LegendText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
        }
    }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the strike counters: only consecutive failures may
        // auto-disable.
        s_SetMarkersFailures = 0;
        s_SpriteFailures = 0;
        s_LegendFailures = 0;
    }

    // Master/toggle live-off parity with the NPC-marker feature: live
    // dispose-then-remove so no marker widget survives until map close.
    // Guarded because the auto-disable path reaches this from inside a patch
    // catch block.
    public override void Disable() {
        try {
            if (TryGetCurrentMapVM(out var vm)) {
                RemoveInteractableMarkers(vm);
            }
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: live interactable marker removal failed: {ex.Message}");
        }
        base.Disable();
    }

    internal static bool TryGetCurrentMapVM(out LocalMapVM vm) {
        _ = s_CurrentMapVM.TryGetTarget(out vm!);
        return vm != null;
    }

    [HarmonyPatch(typeof(LocalMapVM), "SetMarkers"), HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    private static void SetMarkers_Postfix(LocalMapVM __instance) {
        try {
            _ = s_CurrentMapVM.TryGetTarget(out var tracked);
            if (!ReferenceEquals(tracked, __instance)) {
                s_CurrentMapVM.SetTarget(__instance);
                // Map close runs the VM's disposables: null the weak target
                // with them so it can never resolve a DISPOSED VM (NPC
                // marker parity).
                __instance.AddDisposable(UniRx.Disposable.Create(() => s_CurrentMapVM.SetTarget(null!)));
            }
            AppendInteractableMarkers(__instance);
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

    /// <summary>
    /// Idempotent append, deduped by owner against the vanilla markers AND
    /// our own previous appends. Objects are far fewer than units and every
    /// classification input is a persisted bool, so classification runs
    /// synchronously here - no cache, no scheduler, nothing per-frame.
    /// </summary>
    internal static void AppendInteractableMarkers(LocalMapVM vm) {
        if (!MapObjectMarkerClassifier.InteractableMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapInteractableMarkers)) {
            return;
        }
        var claimed = new HashSet<Entity>();
        foreach (var marker in vm.MarkersVm) {
            var entity = marker.GetEntity();
            if (entity != null) {
                _ = claimed.Add(entity);
            }
        }
        var appended = 0;
        var cappedOut = 0;
        foreach (var mapObject in Game.Instance.State.MapObjects.All) {
            if (mapObject == null || claimed.Contains(mapObject)) {
                continue;
            }
            var (state, label) = ClassifyObject(mapObject);
            if (state == InteractableMarkerState.None) {
                continue;
            }
            if (!TryGetViewPosition(mapObject, out var position) || !LocalMapModel.IsInCurrentArea(position)) {
                continue;
            }
            if (!MapObjectMarkerClassifier.WithinVolumeCap(appended, MaxAppendsPerArea)) {
                cappedOut++;
                continue;
            }
            _ = claimed.Add(mapObject);
            var description = string.IsNullOrEmpty(label) ? StateText(state) : $"{label}\n{StateText(state)}";
            vm.MarkersVm.Add(new ToyBoxLocalMapInteractableMarkerVM(mapObject, state, position, description));
            appended++;
        }
        if (cappedOut > 0) {
            WarnCapExceeded(cappedOut);
        }
        if (appended > 0 && TmProbe.OncePerArea("interactable-marker-append")) {
            // TM-probe : interactable appends that reached the list.
            TmProbe.Log($"appended {appended} interactable markers this map open (capped out: {cappedOut})");
        }
    }

    internal static void RemoveInteractableMarkers(LocalMapVM vm) {
        // Dispose THEN remove: the view only observes ObserveAdd, mimicking
        // LocalMapVM.OnUpdateHandler's own removal sequence (NPC parity).
        for (var i = vm.MarkersVm.Count - 1; i >= 0; i--) {
            if (vm.MarkersVm[i] is ToyBoxLocalMapInteractableMarkerVM) {
                vm.MarkersVm[i].Dispose();
                vm.MarkersVm.RemoveAt(i);
            }
        }
    }

    // Whole-object classification: every in-scope interaction part votes, the
    // highest-interest state wins and donates its label (the overtip naming
    // switch mirrored: skill-check DisplayName, dialog blueprint name).
    private static (InteractableMarkerState State, string Label) ClassifyObject(MapObjectEntity mapObject) {
        var isExitOwner = mapObject.GetOptional<AreaTransitionPart>() != null;
        var hasDialog = false;
        var hasSkillCheck = false;
        var best = InteractableMarkerState.None;
        var label = string.Empty;
        var fallbackLabel = string.Empty;
        foreach (var interaction in mapObject.Interactions) {
            InteractableMarkerState state;
            string name;
            switch (interaction) {
                case InteractionDialogPart dialog: {
                    hasDialog = true;
                    var blueprint = SafeDialog(dialog);
                    state = MapObjectMarkerClassifier.ClassifyDialogObject(blueprint != null && NpcDialogClassifier.HasSeen(blueprint), dialog.Enabled, mapObject.IsInGame);
                    name = blueprint?.name ?? string.Empty;
                    break;
                }
                case InteractionSkillCheckPart check: {
                    hasSkillCheck = true;
                    state = MapObjectMarkerClassifier.ClassifySkillCheckObject(check.CheckPassed, check.AlreadyUsed, SafeOnlyCheckOnce(check), check.Enabled, mapObject.IsInGame);
                    name = SafeAssetString(check.Settings?.DisplayName);
                    break;
                }
                default:
                    // Devices, actions, barks, doors, stairs, loot, traps:
                    // passive/flavor interactions are out of scope by design.
                    continue;
            }
            if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(fallbackLabel)) {
                fallbackLabel = name;
            }
            if (MapObjectMarkerClassifier.InterestRank(state) > MapObjectMarkerClassifier.InterestRank(best)) {
                best = state;
                label = name;
            }
        }
        if (!MapObjectMarkerClassifier.IsInteractableScope(hasDialog, hasSkillCheck, isExitOwner)) {
            // Text-only objects (and exits-feature territory) never mark.
            return (InteractableMarkerState.None, string.Empty);
        }
        if (string.IsNullOrEmpty(label)) {
            label = fallbackLabel;
        }
        return (best, label);
    }

    private static Kingmaker.DialogSystem.Blueprints.BlueprintDialog? SafeDialog(InteractionDialogPart dialog) {
        try {
            return dialog.Settings?.Dialog;
        } catch {
            return null;
        }
    }

    private static bool SafeOnlyCheckOnce(InteractionSkillCheckPart check) {
        try {
            return check.Settings?.OnlyCheckOnce ?? true;
        } catch {
            return true;
        }
    }

    private static string SafeAssetString(Kingmaker.Localization.SharedStringAsset? asset) {
        try {
            return asset?.String ?? string.Empty;
        } catch {
            return string.Empty;
        }
    }

    private static string StateText(InteractableMarkerState state) => state switch {
        InteractableMarkerState.New => m_NewDialogText,
        InteractableMarkerState.Repeat => m_RepeatableDialogText,
        InteractableMarkerState.Check => m_SkillCheckAvailableText,
        _ => string.Empty,
    };

    internal static Color StateColor(InteractableMarkerState state) => state switch {
        InteractableMarkerState.New => NewColor,
        InteractableMarkerState.Repeat => RepeatColor,
        InteractableMarkerState.Check => CheckColor,
        _ => Color.white,
    };

    // Once-per-area warn: exceeding the cap is an anomaly worth one line,
    // not one line per map open.
    private static void WarnCapExceeded(int skipped) {
        var area = (object?)Game.Instance?.CurrentlyLoadedArea;
        if (ReferenceEquals(area, s_CapWarnArea)) {
            return;
        }
        s_CapWarnArea = area;
        Warn($"ToyBox EnhancedMap: interactable marker cap ({MaxAppendsPerArea}/area) reached; {skipped} objects not marked this area");
    }

    // The NPC-markers feature patches this method too (report risk: chained
    // postfixes). Priority.Low runs ours AFTER the NPC postfix (Normal), and
    // the else-branch restores the pre-tint color ONLY while the view still
    // shows OUR tint (color provenance), so the two patches compose in any
    // reuse order without clobbering each other's tint.
    [HarmonyPatch(typeof(LocalMapVipMarkerPCView), "BindViewImplementation"), HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    private static void VipMarkerPCView_BindViewImplementation_Postfix(LocalMapVipMarkerPCView __instance) {
        try {
            var mark = __instance.m_Mark;
            if (mark == null) {
                return;
            }
            if (__instance.ViewModel is ToyBoxLocalMapInteractableMarkerVM ours) {
                if (__instance.m_MapObjectSprite != null) {
                    s_CachedMapObjectSprite = __instance.m_MapObjectSprite;
                }
                // Capture the pre-tint color only on first tint: a pooled
                // rebind of our own marker must keep the original vanilla
                // color, not our previous tint.
                var record = s_TintedViews.GetValue(__instance, view => new TintRecord { Original = view.m_Mark.color });
                record.Applied = StateColor(ours.State);
                mark.sprite = __instance.m_MapObjectSprite;
                mark.color = record.Applied;
            } else {
                // Pooled view reuse: restore the vanilla sprite choice
                // (idempotent across both postfixes) and the pre-tint color
                // only while our tint is still applied.
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

    // Legend entry for our marker family. Runs inside the legend VM ctor via
    // AddItems; appended after vanilla entries, toggle-guarded (NPC parity).
    [HarmonyPatch(typeof(LocalMapLegendBlockVM), "AddItems"), HarmonyPostfix]
    private static void LocalMapLegendBlockVM_AddItems_Postfix(LocalMapLegendBlockVM __instance) {
        try {
            if (!MapObjectMarkerClassifier.InteractableMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapInteractableMarkers)) {
                return;
            }
            var sprite = s_CachedMapObjectSprite;
            if (sprite == null) {
                // First map open binds the legend before any VIP view; the
                // entry appears from the next open (NPC parity).
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

    // Position source: View.ViewTransform.position, the exact source
    // LocalMapMarkerPart.GetPosition() uses (the view performs the
    // WorldToViewportPoint conversion itself).
    private static bool TryGetViewPosition(MapObjectEntity mapObject, out Vector3 position) {
        position = Vector3.zero;
        try {
            if (mapObject.View is EntityViewBase { ViewTransform: not null } view) {
                position = view.ViewTransform.position;
                return true;
            }
        } catch {
            // A mid-teardown view must never fail the whole append pass.
        }
        return false;
    }

    private static void HandleFailure(ref int failures, string patch, Exception ex) {
        failures++;
        if (failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: {patch} patch failed ({failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling LocalMapInteractableMarkersFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<LocalMapInteractableMarkersFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
