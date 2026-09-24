using Kingmaker.EntitySystem.Entities;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Defer queue for classification work: at most MaxPerFrame units are
/// evaluated per frame so a dense hub cannot spike a single frame. Pure so
/// the offline harness can link it.
/// </summary>
public sealed class MarkerScheduler {
    public const int MaxPerFrame = 25;

    private readonly Queue<BaseUnitEntity> m_Pending = new();
    private readonly HashSet<BaseUnitEntity> m_Queued = new();

    public int PendingCount => m_Pending.Count;

    public bool Enqueue(BaseUnitEntity unit) {
        if (unit == null || !m_Queued.Add(unit)) {
            return false;
        }
        m_Pending.Enqueue(unit);
        return true;
    }

    public List<BaseUnitEntity> Pump(int maxPerFrame = MaxPerFrame) {
        var pumped = new List<BaseUnitEntity>();
        while (pumped.Count < maxPerFrame && m_Pending.Count > 0) {
            var unit = m_Pending.Dequeue();
            _ = m_Queued.Remove(unit);
            pumped.Add(unit);
        }
        return pumped;
    }

    public void Clear() {
        m_Pending.Clear();
        m_Queued.Clear();
    }
}
