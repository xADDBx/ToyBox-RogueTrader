using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Marker VM for dialog-opening / skill-check map objects. Same reuse as the
/// NPC markers: NOT a LocalMapUnitMarkerVM subclass (the vanilla
/// OnUpdateHandler sweep would dispose it), MarkerType=Poi is remapped to
/// VeryImportantThing by LocalMapBaseView.AddLocalMapMarker and
/// IsMapObject=true makes the vanilla VIP view pick m_MapObjectSprite - no
/// new UI assets. Like all our appended VMs it never goes through the
/// LocalMapMarkerPart.IsVisible bypass: IsVisible is set directly.
/// </summary>
public class ToyBoxLocalMapInteractableMarkerVM : LocalMapMarkerVM {
    // `new`: BaseDisposable.Owner is an unrelated inherited member (the NPC
    // VM avoids the clash by naming its field Unit, ours is the map object).
    public new readonly MapObjectEntity Owner;
    public InteractableMarkerState State;

    public ToyBoxLocalMapInteractableMarkerVM(MapObjectEntity owner, InteractableMarkerState state, Vector3 position, string description) {
        Owner = owner;
        State = state;
        MarkerType = LocalMapMarkType.Poi;
        Position.Value = position;
        IsVisible.Value = true;
        IsMapObject.Value = true;
        Description.Value = description;
    }

    // Map objects never move; states are classified at map-open only (v1:
    // a dialog finished or a check passed while the map stays open renders
    // stale until the next open - every classification input is a cheap
    // persisted bool, but re-classifying per frame is not worth it). The
    // per-frame check is only the owner gate: disabled/removed objects must
    // not keep a live marker.
    public override void OnUpdateHandler() {
        try {
            if (Owner == null || !Owner.IsInGame || Owner.Suppressed) {
                IsVisible.Value = false;
            }
        } catch (Exception ex) {
            IsVisible.Value = false;
            WarnOnce(ex);
        }
    }

    // Per-area failure cap (NPC-marker VM parity).
    private static object? s_WarnAreaKey;

    private static void WarnOnce(Exception ex) {
        var area = (object?)Game.Instance?.CurrentlyLoadedArea;
        if (ReferenceEquals(area, s_WarnAreaKey)) {
            return;
        }
        s_WarnAreaKey = area;
        Warn($"ToyBox EnhancedMap: interactable marker update failed: {ex.Message}");
    }

    public override Entity GetEntity() => Owner;
}
