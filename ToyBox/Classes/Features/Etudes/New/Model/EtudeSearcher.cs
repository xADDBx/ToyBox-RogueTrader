using System.Collections;
using System.Diagnostics;
using ToyBox.Features.Etudes;
using ToyBox.Infrastructure.Utilities;

namespace ToyBoxToyBox.Features.Etudes;

public class EtudeSearcher : IDisposable {
    private readonly object m_SyncRoot = new();
    internal bool IsRunning {
        get;
        private set;
    } = false;
    public string? LastPrompt = "";
    private Stopwatch? m_Stopwatch;
    public bool ShouldCancel = false;
    private bool m_DisposedValue;

    public bool DidSearch {
        get {
            return !string.IsNullOrEmpty(LastPrompt) && !IsRunning;
        }
    }
    public enum SearchMode {
        Default,
    }
    public void StartSearch(SearchMode mode, Dictionary<string, EtudeRecord> etudes, string query) {
        lock (m_SyncRoot) {
            if (etudes == null) {
                return;
            }
            var upperQuery = query.ToUpper();
            if (upperQuery != LastPrompt && !IsRunning) {
                Debug($"Started Etude Search for {etudes.Count} etudes with query {query} and mode {mode}");
                IsRunning = true;
                ShouldCancel = false;
                LastPrompt = upperQuery;
                m_Stopwatch = Stopwatch.StartNew();
                _ = ToyBoxBehaviour.Instance.StartCoroutine(SearchCoroutine(mode, etudes, query));
            }
        }
    }
    // This could be threaded instead of coroutine
    // Will make dependent on performance I guess
    private IEnumerator SearchCoroutine(SearchMode mode, Dictionary<string, EtudeRecord> etudes, string query) {
        var work = new Queue<EtudeRecord>();
        var processed = 0;
        foreach (var rec in etudes.Values) {
            rec.IsDescendantMatched = false;
            rec.IsMatched = MatchNode(mode, rec, query);
            if (++processed % 100 == 0) {
                if (ShouldCancel) {
                    lock (m_SyncRoot) {
                        LastPrompt = "";
                        IsRunning = false;
                        ShouldCancel = false;
                    }
                    Debug($"Etude Search aborted after  {m_Stopwatch?.ElapsedMilliseconds.ToString() ?? "??????? Something is seriously wrong "}ms");
                    yield break;
                } else {
                    Trace($"Etude Search: Processed {processed}");
                }
                yield return null;
            }
            if (rec.Parent != null) {
                work.Enqueue(rec.Parent);
            }
        }
        while (work.Count > 0) {
            var p = work.Dequeue();
            foreach (var child in p.Children) {
                p.IsDescendantMatched |= child.IsMatched || child.IsDescendantMatched;
            }
            if (p.Parent != null) {
                work.Enqueue(p.Parent);
            }
        }

        lock (m_SyncRoot) {
            IsRunning = false;
        }
        Debug($"Etude Search finished in {m_Stopwatch?.ElapsedMilliseconds.ToString() ?? "??????? Something is seriously wrong "}ms");
    }
    private bool MatchNode(SearchMode mode, EtudeRecord node, string query) {
        try {
            return MatchString(BPHelper.GetSearchKey(node.Blueprint), query);
        } catch (Exception ex) {
            Warn($"Error trying to match node {node?.Name ?? node?.Blueprint.AssetGuid ?? "Null?"} for mode {mode}:\n{ex}");
        }
        return false;
    }

    protected virtual void Dispose(bool disposing) {
        if (!m_DisposedValue) {
            if (disposing) {
                ShouldCancel = true;
                LastPrompt = "";
            }
            m_DisposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
