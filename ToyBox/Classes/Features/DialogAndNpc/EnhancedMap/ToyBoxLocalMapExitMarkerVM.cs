using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Marker VM appended by the exits feature for AreaTransitionParts whose
/// scene data has AddMapMarker=0 - no LocalMapMarkerPart exists at all, so
/// the IsVisible awareness-bypass postfix has nothing to unhide. These VMs
/// are OURS exactly like the NPC marker VMs: appended straight into
/// MarkersVm, so they do NOT need (and never see) the awareness bypass -
/// IsVisible is simply set true here. MarkerType=Exit lands in the vanilla
/// Exit widget set (green door icon, zero new UI assets).
/// </summary>
public class ToyBoxLocalMapExitMarkerVM : LocalMapMarkerVM {
    // `new`: BaseDisposable.Owner is an unrelated inherited member; the NPC
    // VM avoids the clash by naming its field Unit, ours is the map object.
    public new readonly MapObjectEntity Owner;

    public ToyBoxLocalMapExitMarkerVM(MapObjectEntity owner, Vector3 position, string description) {
        Owner = owner;
        MarkerType = LocalMapMarkType.Exit;
        Position.Value = position;
        IsVisible.Value = true;
        IsMapObject.Value = true;
        Description.Value = description;
    }

    // Exits never move, so per-frame work is only the authoritative etude/
    // flag gate: UpdateVisibility() flips Owner.IsInGame when the etude
    // stops playing, and this marker must follow immediately - a stale exit
    // would advertise a door that physically does not exist.
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

    // Per-area failure cap: a wrapper throwing every frame would otherwise
    // spam the log at 60 fps for hours (NPC-marker VM parity).
    private static object? s_WarnAreaKey;

    private static void WarnOnce(Exception ex) {
        var area = (object?)Game.Instance?.CurrentlyLoadedArea;
        if (ReferenceEquals(area, s_WarnAreaKey)) {
            return;
        }
        s_WarnAreaKey = area;
        Warn($"ToyBox EnhancedMap: exit marker update failed: {ex.Message}");
    }

    public override Entity GetEntity() => Owner;
}
