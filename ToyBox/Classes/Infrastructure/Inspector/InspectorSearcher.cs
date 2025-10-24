using System.Collections;
using System.Diagnostics;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Inspector;
public static class InspectorSearcher {
    private static readonly object m_SyncRoot = new();
    internal static bool IsRunning {
        get;
        private set;
    } = false;
    public static string? LastPrompt = "";
    private static Stopwatch? m_Stopwatch;
    public static bool ShouldCancel = false;
    public static bool DidSearch {
        get {
            return LastPrompt != "" && !IsRunning;
        }
    }
    public enum SearchMode {
        ValueSearch,
        TypeSearch,
        NameSearch
    }
    public static void StartSearch(SearchMode mode, InspectorNode root, int depthToSearch, string query) {
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
    private static IEnumerator SearchCoroutine(SearchMode mode, InspectorNode root, int depthToSearch, string query) {
        var work = new Stack<(InspectorNode node, int depth, bool childrenPushed)>();
        foreach (var child in root.Children) {
            child.IsChildMatched = false;
            child.IsSelfMatched = false;
            work.Push((child, depthToSearch, false));
        }

        int processed = 0;
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
    private static bool MatchNode(SearchMode mode, InspectorNode node, string query) {
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
    private static bool MatchString(string text, string query) {
        if (!string.IsNullOrEmpty(query)) {
            var terms = query.Split(' ').Select(s => s.ToUpper());
            text = text.ToUpper();
            return terms.All(text.Contains);
        } else {
            return false;
        }
    }
}
