using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using ToyBoxToyBox.Features.Etudes;

namespace ToyBox.Features.Etudes;

public class EtudeSnapshot : IDisposable {
    public readonly Dictionary<string, EtudeRecord> EtudesById;
    public readonly Dictionary<BlueprintEtudeConflictingGroup, HashSet<EtudeRecord>> ConflictingGroups;
    public readonly EtudeRecord? RootEtude;
    private readonly EtudeSearcher m_Searcher = new();
    public bool DidSearch {
        get {
            return m_Searcher.DidSearch;
        }
    }
    public bool IsSearching {
        get {
            return m_Searcher.IsRunning;
        }
    }
    public void CancelSearch() {
        m_Searcher.ShouldCancel = true;
    }
    public void Search(string query) {
        m_Searcher.StartSearch(EtudeSearcher.SearchMode.Default, EtudesById, query);
    }
    public EtudeSnapshot(Dictionary<string, EtudeRecord> etudesById, Dictionary<BlueprintEtudeConflictingGroup, HashSet<EtudeRecord>> conflictingGroups) {
        EtudesById = etudesById;
        ConflictingGroups = conflictingGroups ?? [];
        if (etudesById.TryGetValue(Constants.RootEtudeId, out var root)) {
            RootEtude = root;
            root.IsRoot = true;
        }
    }

    public void BuildDerivedSets(string? initialSearch = null) {
        BuildAreaClosure();
        BuildFlagLikeClosure();
        if (!string.IsNullOrEmpty(initialSearch)) {
            Search(initialSearch!);
        }
    }

    private void BuildAreaClosure() {
        foreach (var pair in EtudesById) {
            var id = pair.Value.LinkedAreaId;
            if (string.IsNullOrEmpty(id)) {
                continue;
            }

            AddToAncestors(pair.Value, (rec, areaId) => {
                return rec.DescendantAreaIds.Add((string)areaId);
            }, id!);

            AddToDescendants(pair.Value, (rec, areaId) => {
                return rec.AncestorAreaIds.Add((string)areaId);
            }, id!);
        }
    }

    private void BuildFlagLikeClosure() {
        foreach (var pair in EtudesById) {
            var rec = pair.Value;
            var isFlagLike = rec.ChainedTo == null && rec.LinkedTo == null && rec.LinkedAreaId == null && !ParentHasArea(rec);
            rec.IsFlagLike = isFlagLike;
            if (isFlagLike) {
                AddToAncestors(rec, (rec, _) => {
                    return rec.IsDescendantFlagLike = true;
                }, true);
            }
        }
    }

    private bool ParentHasArea(EtudeRecord e) {
        var cur = e;
        while (cur.Parent is { } parent) {
            if (!string.IsNullOrEmpty(parent.LinkedAreaId)) {
                return true;
            }

            cur = parent;
        }
        return false;
    }

    private void AddToAncestors(EtudeRecord rec, Func<EtudeRecord, object, bool> action, object val) {
        HashSet<EtudeRecord> visited = [];
        var cur = rec.Parent;
        while (cur != null) {
            if (!visited.Add(cur)) {
                break;
            }

            _ = action(cur, val);

            cur = cur.Parent;
        }
    }

    private void AddToDescendants(EtudeRecord rec, Func<EtudeRecord, object, bool> action, object val) {
        HashSet<EtudeRecord> visited = [];
        Stack<EtudeRecord> stack = [];
        foreach (var c in rec.Children) {
            stack.Push(c);
        }
        while (stack.Count > 0) {
            var cur = stack.Pop();

            if (!visited.Add(cur)) {
                continue;
            }

            _ = action(cur, val);

            foreach (var c in cur.Children) {
                stack.Push(c);
            }
        }
    }

    public void UpdateRuntimeStates() {
        foreach (var e in EtudesById.Values) {
            e.State = GetState(e.Blueprint);
        }
    }

    private static EtudeState GetState(BlueprintEtude blueprintEtude) {
        try {
            var tree = Game.Instance.Player.EtudesSystem?.Etudes;
            if (tree == null) {
                return EtudeState.NotStarted;
            }

            var fact = tree.Get(blueprintEtude);
            if (fact != null) {
                if (fact.IsCompleted) {
                    return EtudeState.Completed;
                }

                if (fact.CompletionInProgress) {
                    return EtudeState.CompletionInProgress;
                }

                return fact.IsPlaying ? EtudeState.Active : EtudeState.Started;
            }

            if (Game.Instance.Player.EtudesSystem!.EtudeIsPreCompleted(blueprintEtude)) {
                return EtudeState.PreCompleted;
            }

            if (Game.Instance.Player.EtudesSystem.EtudeIsCompleted(blueprintEtude)) {
                return EtudeState.Completed;
            }

            return EtudeState.NotStarted;
        } catch (Exception ex) {
            Debug($"Caught exception while getting state for etude {blueprintEtude.AssetGuid} ({BPHelper.GetTitle(blueprintEtude)}):\n{ex}");
            return EtudeState.NotStarted;
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            m_Searcher.Dispose();
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
