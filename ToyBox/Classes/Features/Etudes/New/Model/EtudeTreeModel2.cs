using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Utility.DotNetExtensions;

namespace ToyBox.Features.Etudes;

public class EtudesTreeModel2 {
    private bool m_IsBuildingSnapshot = false;
    public Exception? ErrorDuringBuild;
    private EtudeSnapshot? m_Snapshot;

    public void Invalidate() {
        lock (this) {
            m_IsBuildingSnapshot = false;
            m_Snapshot?.Dispose();
            m_Snapshot = null;
            ErrorDuringBuild = null;
        }
    }

    public bool TryEnsureSnapshot(out EtudeSnapshot? snapshot, string? initialSearchText = null) {
        snapshot = null;
        if (m_Snapshot != null) {
            snapshot = m_Snapshot;
            return true;
        }

        if (ErrorDuringBuild == null) {
            if (BPLoader.GetBlueprintsOfType<BlueprintEtude>() is { } bps) {
                lock (this) {
                    if (!m_IsBuildingSnapshot) {
                        m_IsBuildingSnapshot = true;
                        _ = Task.Run(() => BuildSnapshotThreaded(bps, initialSearchText));
                    }
                }
            }
        }
        return false;
    }
    private void BuildSnapshotThreaded(IEnumerable<BlueprintEtude> bps, string? initialSearchText) {
        try {
            var conflictingGroups = new Dictionary<BlueprintEtudeConflictingGroup, HashSet<EtudeRecord>>();
            var bpList = bps.ToList();
            var records = new Dictionary<string, EtudeRecord>(bpList.Count, StringComparer.Ordinal);

            foreach (var bp in bpList) {
                records.Add(bp.AssetGuid, new EtudeRecord(bp));
            }
            foreach (var pair in records) {
                var rec = pair.Value;
                var parentId = rec.Blueprint.Parent?.Get()?.AssetGuid ?? string.Empty;
                if (records.TryGetValue(parentId, out var parent)) {
                    rec.Parent = parent;
                    parent.Children.Add(rec);
                }

                // Chained / Linked edges
                foreach (var chained in rec.Blueprint.StartsOnComplete) {
                    if (records.TryGetValue(chained.guid, out var c)) {
                        rec.ChainedEtudes.Add(c);
                    }
                }

                foreach (var linked in rec.Blueprint.StartsWith) {
                    if (records.TryGetValue(linked.guid, out var l)) {
                        rec.LinkedEtudes.Add(l);
                    }
                }

                // Conflicting groups
                foreach (var g in rec.Blueprint.ConflictingGroups) {
                    if (g.Get() is { } gBp) {
                        rec.ConflictingGroups.Add(gBp);
                        if (!conflictingGroups.TryGetValue(gBp, out var set)) {
                            set = [];
                            conflictingGroups[gBp] = set;
                        }
                        _ = set.Add(rec);
                    }
                }
            }
            // wire children + reverse edges (linkedTo/chainedTo)
            foreach (var (id, rec) in records) {
                foreach (var child in rec.LinkedEtudes) {
                    child.LinkedTo = rec;
                }
                foreach (var child in rec.ChainedEtudes) {
                    child.ChainedTo = rec;
                }
            }

            var snapshot = new EtudeSnapshot(records, conflictingGroups);
            snapshot.BuildDerivedSets(initialSearchText);

            Main.ScheduleForMainThread(() => {
                lock (this) {
                    if (m_IsBuildingSnapshot) {
                        m_IsBuildingSnapshot = false;
                        m_Snapshot = snapshot;
                    }
                }
            });
        } catch (Exception ex) {
            Critical($"Failed to build Etudes snapshot:\n{ex}", false);
            Main.ScheduleForMainThread(() => {
                lock (this) {
                    m_IsBuildingSnapshot = false;
                    ErrorDuringBuild = ex;
                }
            });
        }
    }
}

