using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine;

namespace ToyBox.Infrastructure;

public class ThreadedListSearcher<T> where T : notnull {
    private float m_LastSharedResults = 0f;
    private const float m_ShareResultsDelay = 0.05f;
    private const int m_MaxNumForPartialUpdate = 200;
    public bool IsRunning = false;
    public int CurrentlyFound;
    private readonly VerticalList<T> m_Parent;
    private CancellationTokenSource? m_SearchCts;
    public ConcurrentQueue<T>? m_InProgress;
    public ThreadedListSearcher(VerticalList<T> parent) {
        m_Parent = parent;
    }
    public void StartSearch(IEnumerable<T> items, string query, Func<T, string> getSearchKey, Func<T, string> getSortKey) {
        Trace($"Start Search:\n{new StackTrace()}");
        lock (this) {
            if (IsRunning) {
                StopSearch();
            }
        }
        m_SearchCts = new();
        _ = Task.Run(() => DoSearch(items, query, getSearchKey, getSortKey, m_SearchCts.Token, m_SearchCts));
    }
    private void DoSearch(IEnumerable<T> items, string query, Func<T, string> getSearchKey, Func<T, string> getSortKey, CancellationToken ct, CancellationTokenSource cts) {
        lock (this) {
            IsRunning = true;
        }
        try {
            var watch = Stopwatch.StartNew();
            m_LastSharedResults = Time.time;
            Debug("Start DoSearch");
            var allResults = new List<T>();
            m_InProgress = new();
            CurrentlyFound = 0;
            var lastShared = 0;
            int searched = 0;
            if (!string.IsNullOrEmpty(query)) {
                var terms = query.Split(' ').Select(s => s.ToUpper());
                foreach (var item in items) {
                    if (ct.IsCancellationRequested) {
                        lock (this) {
                            IsRunning = false;
                            cts.Dispose();
                        }
                        #warning Sorting Order
                        m_Parent.QueueUpdateItems(allResults.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).OrderBy(getSortKey).ToArray(), 1, true);
                        Debug("Cancelled Search");
                        return;
                    }
                    searched++;
                    var text = getSearchKey(item).ToUpper();
                    if (terms.All(text.Contains)) {
                        allResults.Add(item);
                        m_InProgress.Enqueue(item);
                        CurrentlyFound++;
                        if (lastShared < m_MaxNumForPartialUpdate && (Time.time - m_LastSharedResults) > m_ShareResultsDelay) {
                            lastShared = CurrentlyFound;
                            m_LastSharedResults = Time.time;
                            m_Parent.QueueUpdateItems([.. m_InProgress], 1, true);
                        }
                    }
                }
#warning Sorting Order
                m_Parent.QueueUpdateItems(allResults.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).OrderBy(getSortKey).ToArray(), 1, true);
                Debug($"Searched {searched} items in {watch.ElapsedMilliseconds}ms; found {allResults.Count} results");
            } else {
#warning Sorting Order
                m_Parent.QueueUpdateItems(items.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).OrderBy(getSortKey).ToArray(), 1, true);
                Debug($"Searched {searched} items in {watch.ElapsedMilliseconds}ms; query is empty so used all items as result");
            }
        } catch (Exception e) {
            Error($"Encountered exception while trying to search!\n{e}");
        }
        lock (this) {
            IsRunning = false;
            cts.Dispose();
        }
    }
    public void StopSearch() {
        m_SearchCts?.Cancel();
    }
}
