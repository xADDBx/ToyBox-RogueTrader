using System.Collections;
using System.Diagnostics;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Inspector;

public class InspectorSearcher : IDisposable {
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
        ValueSearch,
        TypeSearch,
        NameSearch
    }
    public void StartSearch(SearchMode mode, InspectorNode root, int depthToSearch, string query) {
        lock (m_SyncRoot) {
            var upperQuery = query.ToUpper();
            if (upperQuery != LastPrompt && !IsRunning) {
                IsRunning = true;
                ShouldCancel = false;
                LastPrompt = upperQuery;
                m_Stopwatch = Stopwatch.StartNew();
                _ = ToyBoxBehaviour.Instance.StartCoroutine(SearchCoroutine(mode, root, depthToSearch, query));
            }
        }
    }
    private IEnumerator SearchCoroutine(SearchMode mode, InspectorNode root, int depthToSearch, string query) {
        var work = new Stack<(InspectorNode node, int depth, bool childrenPushed)>();
        foreach (var child in root.Children) {
            child.IsChildMatched = false;
            child.IsSelfMatched = false;
            work.Push((child, depthToSearch, false));
        }

        var processed = 0;
        while (work.Count > 0) {
            var (node, depth, childrenPushed) = work.Pop();

            if (!childrenPushed && depth > 0) {
                work.Push((node, depth, true));
                foreach (var c in node.Children) {
                    c.IsChildMatched = false;
                    c.IsSelfMatched = false;
                    work.Push((c, depth - 1, false));
                }
            } else {
                node.IsSelfMatched = MatchNode(mode, node, query);
                node.Parent!.IsChildMatched |= node.IsMatched;
            }
            if (++processed % Settings.InspectorSearchBatchSize == 0) {
                if (ShouldCancel) {
                    lock (m_SyncRoot) {
                        LastPrompt = "";
                        IsRunning = false;
                        ShouldCancel = false;
                    }
                    Debug($"Inspector Search aborted after  {m_Stopwatch?.ElapsedMilliseconds.ToString() ?? "??????? Something is seriously wrong "}ms");
                    yield break;
                }
                yield return null;
            }
        }
        lock (m_SyncRoot) {
            IsRunning = false;
        }
        Debug($"Inspector Search finished in {m_Stopwatch?.ElapsedMilliseconds.ToString() ?? "??????? Something is seriously wrong "}ms");
    }
    private bool MatchNode(SearchMode mode, InspectorNode node, string query) {
        try {
            if (mode == SearchMode.ValueSearch) {
                return MatchString(node.ValueText, query);
            } else if (mode == SearchMode.NameSearch) {
                return MatchString(node.NameText, query);
            } else if (mode == SearchMode.TypeSearch) {
                return MatchString(node.TypeNameText, query);
            }
        } catch (Exception ex) {
            Warn($"Error trying to match node {node?.NameText ?? "NullName"} for mode {mode}:\n{ex}");
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
