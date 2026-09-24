using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Marker VM for classifier-positive NPCs. NOT a LocalMapUnitMarkerVM subclass:
/// vanilla's OnUpdateHandler sweep disposes any LocalMapUnitMarkerVM whose
/// UnitInfo is not in enemy memory, which would delete friendly-NPC markers
/// every frame. MarkerType=Poi is remapped to VeryImportantThing by
/// LocalMapBaseView.AddLocalMapMarker, landing us in the VIP widget set.
/// </summary>
public class ToyBoxLocalMapNpcMarkerVM : LocalMapMarkerVM {
    public readonly BaseUnitEntity Unit;
    public NpcDialogMarkerState State;

    public ToyBoxLocalMapNpcMarkerVM(BaseUnitEntity unit, NpcDialogMarkerState state, string description) {
        Unit = unit;
        State = state;
        MarkerType = LocalMapMarkType.Poi;
        Position.Value = unit.Position;
        IsVisible.Value = true;
        IsMapObject.Value = false;
        Description.Value = description;
    }

    // Per-frame failure cap: a wrapper throwing every frame would otherwise
    // spam the log at 60 fps for hours (the only uncapped per-event warn in
    // the family - audit P2). One line per area, keyed by area identity.
    private static object? s_WarnAreaKey;

    public override void OnUpdateHandler() {
        // Same cadence as vanilla's own marker VMs: push position, self-hide
        // on death. A throw here would propagate into the per-frame
        // observable dispatch, so guard like every other ToyBox patch body.
        try {
            if (Unit == null || Unit.LifeState.IsDead) {
                IsVisible.Value = false;
                return;
            }
            Position.Value = Unit.Position;
        } catch (Exception ex) {
            IsVisible.Value = false;
            var area = (object?)Game.Instance?.CurrentlyLoadedArea;
            if (!ReferenceEquals(area, s_WarnAreaKey)) {
                s_WarnAreaKey = area;
                Warn($"ToyBox EnhancedMap: NPC marker update failed for {Unit}: {ex.Message}");
            }
        }
    }

    public override Entity GetEntity() => Unit;
}
