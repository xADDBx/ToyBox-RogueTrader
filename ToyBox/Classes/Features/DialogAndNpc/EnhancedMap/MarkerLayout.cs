using System.Numerics;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public sealed class MarkerCandidate(int index, NpcDialogMarkerState state, Vector2 position) {
    public readonly int Index = index;
    public readonly NpcDialogMarkerState State = state;
    public readonly Vector2 Position = position;
}

/// <summary>
/// Pure declutter pass: NEW beats REPEAT beats INACTIVE; a marker within
/// MinSpacing of an already-kept marker is suppressed and attributed to its
/// nearest kept suppressor (for the suppressor's tooltip). Uses
/// System.Numerics so the offline harness can link it without Unity.
/// </summary>
public static class MarkerLayout {
    // Distance unit: layout points are viewport*1000 (see the markers
    // feature's ToLayoutPoint), so 12 units = 1.2% of a map axis, which is
    // ~12 px on a ~1000 px map image.
    public const float MinSpacing = 12f;

    public static int Priority(NpcDialogMarkerState state) => state switch {
        NpcDialogMarkerState.New => 3,
        NpcDialogMarkerState.Repeat => 2,
        NpcDialogMarkerState.Inactive => 1,
        _ => 0,
    };

    /// <summary>Returns (suppressed index, suppressor index) pairs in selection order.</summary>
    public static List<(int Suppressed, int Suppressor)> SelectSuppressed(IEnumerable<MarkerCandidate> candidates, float minSpacing = MinSpacing) {
        var result = new List<(int Suppressed, int Suppressor)>();
        var kept = new List<MarkerCandidate>();
        // Priority order first, stable by original index within equal priority.
        foreach (var candidate in candidates.OrderByDescending(c => Priority(c.State)).ThenBy(c => c.Index)) {
            if (Priority(candidate.State) <= 0) {
                continue;
            }
            MarkerCandidate? suppressor = null;
            var bestDistance = float.MaxValue;
            foreach (var keptCandidate in kept) {
                var distance = Vector2.Distance(keptCandidate.Position, candidate.Position);
                if (distance < minSpacing && distance < bestDistance) {
                    bestDistance = distance;
                    suppressor = keptCandidate;
                }
            }
            if (suppressor != null) {
                result.Add((candidate.Index, suppressor.Index));
            } else {
                kept.Add(candidate);
            }
        }
        return result;
    }
}
