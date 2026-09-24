using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Always-visible exit/transition markers (R2) plus looted-container hiding
/// (R10), both riding one postfix on LocalMapMarkerPart.IsVisible. The patch
/// is strictly conditioned per marker type: the Exit branch only bypasses the
/// scout gate for Exit markers, the Loot branch only hides Loot markers whose
/// owner's loot has been viewed. Every other marker type (VIP/POI/loot) keeps
/// vanilla behavior; the inner awareness check and the Hidden flag are never
/// touched.
/// The feature also APPENDS markers: AreaTransitions whose scene
/// data has AddMapMarker=0 have no LocalMapMarkerPart at all (36 known
/// game-wide), so the bypass has nothing to unhide - a SetMarkers postfix
/// (NPC-marker scaffolding) appends our own Exit-typed VM for those instead.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapExitMarkersFeature")]
public partial class LocalMapExitMarkersFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;

    private static int s_VisibilityFailures;
    private static int s_HighlightFailures;
    private static int s_AppendFailures;

    // Weak: the VM is disposed when the map closes; a stale ref must never
    // resurrect it. Refreshed by the SetMarkers postfix on every map open.
    private static readonly WeakReference<LocalMapVM> s_CurrentMapVM = new(null!);

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.LocalMapExitMarkersFeature";
        }
    }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the strike counters: only consecutive failures may
        // auto-disable.
        s_VisibilityFailures = 0;
        s_HighlightFailures = 0;
        s_AppendFailures = 0;
    }

    // Master/exits toggle live-off parity with the NPC-marker feature:
    // dispose-then-remove our appended exit VMs so no marker widget survives
    // until map close. Guarded because the auto-disable path reaches this
    // from inside a patch catch block.
    public override void Disable() {
        try {
            if (TryGetCurrentMapVM(out var vm)) {
                RemoveAppendedExitMarkers(vm);
            }
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: live exit marker removal failed: {ex.Message}");
        }
        base.Disable();
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLocalMapExitMarkers;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapExitMarkersFeature_Name", "Local Map Exit & Transition Markers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapExitMarkersFeature_Description", "Shows exit and transition markers (bridges, doors, area exits) on the local map even before they are scouted. Markers explicitly hidden by the area or an etude stay hidden.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapExitMarkersFeature_m_HideLootedContainersText", "Hide Looted Containers")]
    private static partial string m_HideLootedContainersText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_LocalMapExitMarkersFeature_m_HideLootedContainersDescription", "Containers whose loot has already been viewed no longer appear on the map or in Tab highlight. Re-fillable containers also stay hidden while they are flagged as viewed.")]
    private static partial string m_HideLootedContainersDescription { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
            if (IsEnabled) {
                using (HorizontalScope()) {
                    Space(50);
                    _ = UI.Toggle(m_HideLootedContainersText, m_HideLootedContainersDescription, ref Settings.HideLootedContainers);
                }
            }
        }
    }

    // Explicit interface implementation: the metadata name is the fully
    // qualified interface method name.
    [HarmonyPatch(typeof(LocalMapMarkerPart), "Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils.ILocalMapMarker.IsVisible"), HarmonyPostfix]
    private static void LocalMapMarkerPart_IsVisible_Postfix(LocalMapMarkerPart __instance, ref bool __result) {
        try {
            var settings = __instance.Settings;
            if (__result) {
                // Loot branch (R10): only demotes Loot markers of viewed loot.
                if (settings.Type == LocalMapMarkType.Loot
                    && NpcDialogClassifier.LootHidingEffective(Settings.EnhancedMapMaster, Settings.HideLootedContainers)
                    && IsLootViewed(__instance.Owner as MapObjectEntity)) {
                    __result = false;
                }
            } else {
                // Exit branch: bypass ONLY the scout gate (IsRevealed +
                // IsAwarenessCheckPassed). Hidden and IsInGame (the levers the
                // game uses for VisibilityFlag/VisibilityEtude gating) must
                // stay authoritative or etude-hidden secret exits would leak.
                if (settings.Type == LocalMapMarkType.Exit
                    && NpcDialogClassifier.ExitMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapExitMarkers)
                    && !__instance.Hidden
                    && __instance.Owner is MapObjectEntity { IsInGame: true }) {
                    __result = true;
                }
            }
            // Reset at the END of the pass (a guarded write; this body runs
            // per visibility query): a top-of-try reset would zero the
            // counter right before the risky code, so persistent exceptions
            // would oscillate 0<->1 and never reach the auto-disable
            // threshold while Warn fires on every failure.
            if (s_VisibilityFailures > 0) {
                s_VisibilityFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_VisibilityFailures, "IsVisible", ex);
        }
    }

    // Tab/world highlight suppression (R10). Composes with the fork's
    // HighlightHiddenObjectsFeature: its transpiler rewrites the body, this
    // postfix still runs after it, boolean AND composes the two.
    [HarmonyPatch(typeof(MapObjectView), nameof(MapObjectView.ShouldBeHighlighted)), HarmonyPostfix]
    private static void MapObjectView_ShouldBeHighlighted_Postfix(MapObjectView __instance, ref bool __result) {
        try {
            // Same master gate as the map path above: the master switch owns
            // "looted-container hiding" everywhere, not just on the map.
            if (__result && NpcDialogClassifier.LootHidingEffective(Settings.EnhancedMapMaster, Settings.HideLootedContainers)
                && IsLootViewed(__instance.Data)) {
                __result = false;
            }
            if (s_HighlightFailures > 0) {
                s_HighlightFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_HighlightFailures, "ShouldBeHighlighted", ex);
        }
    }

    internal static bool IsLootViewed(MapObjectEntity? mapObject) {
        try {
            return mapObject?.GetOptional<InteractionLootPart>() is { LootViewed: true };
        } catch {
            return false;
        }
    }

    internal static bool TryGetCurrentMapVM(out LocalMapVM vm) {
        _ = s_CurrentMapVM.TryGetTarget(out vm!);
        return vm != null;
    }

    /// <summary>
    /// Appends one Exit-typed marker VM per marker-less AreaTransitionPart
    /// . Same scaffolding as the NPC markers: WeakReference VM
    /// tracking, idempotent append deduped by owner, dispose-then-remove on
    /// Disable. Rides the SAME exits toggle - marker-less exits are the same
    /// user intent ("show exits"), no new setting.
    /// </summary>
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
            AppendMarkerlessExitMarkers(__instance);
            // Reset at the END of the pass: a top-of-try reset would zero
            // the counter right before the risky code, so persistent
            // exceptions would never reach the auto-disable threshold.
            if (s_AppendFailures > 0) {
                s_AppendFailures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ref s_AppendFailures, "SetMarkers", ex);
        }
    }

    internal static void AppendMarkerlessExitMarkers(LocalMapVM vm) {
        if (!NpcDialogClassifier.ExitMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapExitMarkers)) {
            return;
        }
        // Dedupe by owner against EVERYTHING already in the list: markers the
        // vanilla path created (its VMs expose GetEntity = LocalMapMarkerPart
        // owner) and our own previous appends.
        var claimed = new HashSet<Entity>();
        foreach (var marker in vm.MarkersVm) {
            var entity = marker.GetEntity();
            if (entity != null) {
                _ = claimed.Add(entity);
            }
        }
        var appended = 0;
        foreach (var mapObject in Game.Instance.State.MapObjects.All) {
            if (mapObject == null) {
                continue;
            }
            var part = mapObject.GetOptional<AreaTransitionPart>();
            if (part == null || claimed.Contains(mapObject)) {
                continue;
            }
            var settings = part.Settings;
            var hasMarkerPart = mapObject.GetOptional<LocalMapMarkerPart>() != null;
            // Position first: the area-bounds check needs it, and a detached
            // view skips the object without touching the predicate inputs.
            var inCurrentArea = TryGetViewPosition(mapObject, out var position)
                && LocalMapModel.IsInCurrentArea(position);
            // Etude/flag gates stay authoritative (ownerInGame) - etude-hidden
            // hub exits (e.g. every footfall-atrium exit) must STAY hidden.
            if (!MapObjectMarkerClassifier.ShouldAppendExitMarker(settings.AddMapMarker, hasMarkerPart, mapObject.IsInGame, mapObject.Suppressed, inCurrentArea)) {
                continue;
            }
            _ = claimed.Add(mapObject);
            vm.MarkersVm.Add(new ToyBoxLocalMapExitMarkerVM(mapObject, position, BuildExitDescription(part)));
            appended++;
        }
        if (appended > 0 && TmProbe.OncePerArea("exit-marker-append")) {
            // TM-probe : marker-less appends that reached the list.
            TmProbe.Log($"appended {appended} marker-less exit markers this map open");
        }
    }

    internal static void RemoveAppendedExitMarkers(LocalMapVM vm) {
        // Dispose THEN remove: the view only observes ObserveAdd, mimicking
        // LocalMapVM.OnUpdateHandler's own removal sequence (NPC parity).
        for (var i = vm.MarkersVm.Count - 1; i >= 0; i--) {
            if (vm.MarkersVm[i] is ToyBoxLocalMapExitMarkerVM) {
                vm.MarkersVm[i].Dispose();
                vm.MarkersVm.RemoveAt(i);
            }
        }
    }

    // Label = the transition's tooltip/area name, exactly like the vanilla
    // Exit markers (AreaTransitionPart.OnSettingsDidSet fills
    // NonLocalizedDescription from AreaEnterPoint.Tooltip(TooltipIndex)).
    private static string BuildExitDescription(AreaTransitionPart part) {
        try {
            var enterPoint = part.AreaEnterPoint;
            if (enterPoint == null) {
                return string.Empty;
            }
            var lines = new List<string>();
            var tooltip = enterPoint.Tooltip(part.Settings.TooltipIndex);
            if (!string.IsNullOrEmpty(tooltip) && !string.IsNullOrWhiteSpace(tooltip)) {
                lines.Add(tooltip);
            }
            var areaName = enterPoint.Area?.AreaDisplayName;
            if (areaName != null && areaName.Length > 0) {
                lines.Add(areaName);
            }
            return lines.Count > 0 ? string.Join("\n", lines) : enterPoint.name;
        } catch {
            // A broken tooltip must never fail the append pass.
            return string.Empty;
        }
    }

    // LocalMapMarkerPart.GetPosition uses View.ViewTransform.position; we use
    // the identical source for objects that never had a marker part.
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
            Warn($"ToyBox EnhancedMap: auto-disabling LocalMapExitMarkersFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<LocalMapExitMarkersFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
