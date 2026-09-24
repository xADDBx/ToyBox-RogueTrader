using System.Runtime.CompilerServices;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Levelup.Selections;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// Pure session store for picker fold persistence (R12). Mirrors the game's own
/// per-VM expansion dictionaries at app-session scope so a fresh
/// RankEntrySelectionVM can be seeded with the folds its FeatureGroup last had.
/// Deliberately free of Harmony/game-view dependencies: the feature class owns
/// the patches and reflection, this type only copies dictionaries around.
/// </summary>
public sealed class PickerFoldStore {
    /// <summary>Copy of one picker's expansion state, per grouping mode exactly like vanilla.</summary>
    internal sealed class Snapshot {
        public Dictionary<FeaturesFilter.FeatureFilterType, bool> ByType { get; } = new();
        public Dictionary<string, bool> BySource { get; } = new();
    }

    private readonly Dictionary<FeatureGroup, Snapshot> m_Store = new();

    public int Count => m_Store.Count;

    /// <summary>
    /// Replace the stored snapshot for a group. The live dictionaries are only
    /// read here (never handed out), so a later Record cannot be mutated by the
    /// VM that produced it.
    /// </summary>
    public void Record(
        FeatureGroup group,
        IDictionary<FeaturesFilter.FeatureFilterType, bool> byType,
        IDictionary<string, bool> bySource) {
        var snapshot = new Snapshot();
        foreach (var pair in byType) {
            snapshot.ByType[pair.Key] = pair.Value;
        }
        foreach (var pair in bySource) {
            snapshot.BySource[pair.Key] = pair.Value;
        }
        m_Store[group] = snapshot;
    }

    /// <summary>
    /// Copy a stored snapshot into the live dictionaries of a freshly built VM.
    /// Indexer assignment only: the game's own dictionaries are readonly fields
    /// and must be populated in place, never replaced.
    /// </summary>
    public bool TrySeed(
        FeatureGroup group,
        IDictionary<FeaturesFilter.FeatureFilterType, bool> byType,
        IDictionary<string, bool> bySource) {
        if (!m_Store.TryGetValue(group, out var snapshot)) {
            return false;
        }
        foreach (var pair in snapshot.ByType) {
            byType[pair.Key] = pair.Value;
        }
        foreach (var pair in snapshot.BySource) {
            bySource[pair.Key] = pair.Value;
        }
        return true;
    }

    /// <summary>Session clear: the progression window was disposed.</summary>
    public void Clear() => m_Store.Clear();
}

/// <summary>
/// Binds each collapsible header to the picker VM that created it, so a later
/// toggle on the header can be recorded back into the owning VM's store entry.
/// Weak on the header side: rebuilt headers die with their VM and drop out of
/// the table automatically. Generic so the offline contract tests can drive it
/// with doubles.
/// </summary>
public sealed class HeaderOwnerRegistry<THeader, TOwner>
    where THeader : class
    where TOwner : class {
    private readonly ConditionalWeakTable<THeader, TOwner> m_Table = new();

    public void Bind(THeader header, TOwner owner) {
        // Rebinding the same header instance replaces the owner; vanilla
        // creates a fresh header per rebuild, so this only guards accidents.
        m_Table.Remove(header);
        m_Table.Add(header, owner);
    }

    public bool TryGetOwner(THeader header, out TOwner? owner) {
        return m_Table.TryGetValue(header, out owner);
    }
}
