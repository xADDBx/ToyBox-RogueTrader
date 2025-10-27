using Kingmaker.Utility.UnityExtensions;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ToyBox.Infrastructure;
/// <summary>
/// A searchable, paginated browser view over a collection of items of type <typeparamref name="T"/>.
/// Extends <see cref="VerticalList{T}"/> with live, threaded searching and optional "show all" support.
/// </summary>
/// <typeparam name="T">The non-nullable type of each item in the browser.</typeparam>
public partial class Browser<T> : VerticalList<T> where T : notnull {
    protected float LastSearchedAt = 0f;
    protected string CurrentSearchString = "";
    protected string LastSearchedFor = "";
    private string? m_SearchBarControlName;
    protected Action<Action<IEnumerable<T>>>? ShowAllFunc = null;
    private bool m_ShowAllFuncCalled = false;
    protected bool ShowAll = false;
    private Task? m_DebounceTask;
    protected IEnumerable<T>? UnsearchedShowAllItems = null;
    protected IEnumerable<T> UnsearchedItems = [];
    protected Func<T, string> GetSearchKey;
    protected Func<T, string> GetSortKey;
    protected bool ShowSearchBar = true;
    protected ThreadedListSearcher<T> Searcher;
    /// <summary>
    /// Initializes a new instance of the <see cref="Browser{T}"/> class.
    /// </summary>
    /// <param name="sortKey">
    /// A function that, given an item, returns its sort key string.
    /// Used to order search results and pages.
    /// </param>
    /// <param name="searchKey">
    /// A function that, given an item, returns its searchable content string.
    /// Used to match against the current search query.
    /// </param>
    /// <param name="initialItems">
    /// Optional initial collection of items to populate the browser with.
    /// <para>
    /// If null, the browser starts empty until <see cref="RegisterShowAllItems"/> or
    /// <see cref="VerticalList{T}.QueueUpdateItems(IEnumerable{T}, int?)"/> is called.
    /// </para>
    /// </param>
    /// <param name="showAllFunc">
    /// Optional callback which is invoked the first time the user toggles "Show All".
    /// The callback is passed the function <see cref="RegisterShowAllItems(IEnumerable{T})"/>
    /// which you must call with your full, unfiltered item set.
    /// <para>
    /// If you pass <c>null</c>, the "Show All" toggle will be hidden entirely.
    /// </para>
    /// <para>
    /// For example, to load and display all <c>SimpleBlueprint</c>s, you might use:
    /// <code>
    /// (Action&lt;IEnumerable&lt;SimpleBlueprint&gt;&gt; func) => BPLoader.GetBlueprints(func)
    /// </code>
    /// </para>
    /// </param>
    /// <param name="showDivBetweenItems">
    /// Whether to draw a divider line between each item row.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <param name="overridePageWidth">
    /// Optional override for the width (in pixels) of the browser’s scrollable area.
    /// If null, uses the default from <see cref="VerticalList{T}"/>.
    /// </param>
    /// <param name="overridePageLimit">
    /// Optional override for the number of items per page.
    /// If null, uses the default from <see cref="VerticalList{T}"/>.
    /// </param>
    public Browser(Func<T, string> sortKey, Func<T, string> searchKey, IEnumerable<T>? initialItems = null, Action<Action<IEnumerable<T>>>? showAllFunc = null, bool showDivBetweenItems = true, int? overridePageWidth = null, int? overridePageLimit = null, bool showSearchBar = true)
        : base(initialItems, showDivBetweenItems, overridePageWidth, overridePageLimit) {
        ShowAllFunc = showAllFunc;
        GetSearchKey = searchKey;
        GetSortKey = sortKey;
        ShowSearchBar = showSearchBar;
        Searcher = new(this);
    }
    internal override void UpdateItems(IEnumerable<T> newItems, int? forcePage = null, bool onlyDisplayedItems = false) {
        if (!onlyDisplayedItems) {
            UnsearchedItems = newItems;
        }

        base.UpdateItems(newItems, forcePage, onlyDisplayedItems);
        if (!onlyDisplayedItems && LastSearchedAt > 0f) {
            RedoSearch();
        }
    }
    public void RedoSearch() {
        StartNewSearch(CurrentSearchString, true);
    }

    /// <summary>
    /// Provides the full "Show All" item set to the browser and immediately restarts the search.
    /// </summary>
    /// <param name="items">The complete collection of items to show when "Show All" is enabled.</param>
    public void RegisterShowAllItems(IEnumerable<T> items) {
        Main.ScheduleForMainThread(() => {
            UnsearchedShowAllItems = items;
            StartNewSearch(CurrentSearchString, true);
        });
    }
    /// <summary>
    /// Begins a new search over the items, optionally forcing even if the query string is unchanged.
    /// </summary>
    /// <param name="query">The search string to match against each item's search key.</param>
    /// <param name="force">
    /// If <c>true</c>, always restarts the search even when <paramref name="query"/> is the same
    /// as the previous search; otherwise, skips if unchanged.
    /// </param>
    public void StartNewSearch(string query, bool force = false) {
        if (!force && LastSearchedFor == query) {
            return;
        }
        var canOptimizeSearch = !query.IsNullOrEmpty() && query.StartsWith(LastSearchedFor) && !force;
        LastSearchedFor = query;
        LastSearchedAt = Time.time;
        m_DebounceTask = null;
        CurrentPage = 1;
        if (canOptimizeSearch) {
            Searcher.StartSearch(Items, query, GetSearchKey, GetSortKey);
        } else {
            Searcher.StartSearch((ShowAll && UnsearchedShowAllItems != null) ? UnsearchedShowAllItems : UnsearchedItems, query, GetSearchKey, GetSortKey);
        }
    }
    protected void SearchBarGUI() {
        if (!ShowSearchBar) {
            return;
        }

        void DebouncedSearch() {
            Thread.Sleep((int)(Settings.SearchDelay * 1000));
            if (!CurrentSearchString.Equals(LastSearchedFor)) {
                StartNewSearch(CurrentSearchString);
            }
        }
        m_SearchBarControlName ??= RuntimeHelpers.GetHashCode(this).ToString();
        Action<(string oldContent, string newContent)>? contentChangedAction = Settings.ToggleSearchAsYouType ? (((string oldContent, string newContent) pair) => {
            if ((Time.time - LastSearchedAt) > Settings.SearchDelay) {
                StartNewSearch(pair.newContent);
            } else {
                m_DebounceTask ??= Task.Run(DebouncedSearch);
            }
        }) : null;
        using (HorizontalScope()) {
            _ = UI.ActionTextField(ref CurrentSearchString, m_SearchBarControlName, contentChangedAction, (string query) => {
                StartNewSearch(query);
            });
            Space(5);
            _ = UI.Button(SharedStrings.SearchText, () => StartNewSearch(CurrentSearchString));
        }
    }
    protected override void HeaderGUI() {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                PageGUI();
                Space(30);
                if (ShowAllFunc != null) {
                    var newValue = GUILayout.Toggle(ShowAll, SharedStrings.ShowAllText.Cyan(), AutoWidth());
                    if (newValue != ShowAll) {
                        ShowAll = newValue;
                        if (UnsearchedShowAllItems == null && newValue && !m_ShowAllFuncCalled) {
                            m_ShowAllFuncCalled = true;
                            ShowAllFunc(RegisterShowAllItems);
                        } else {
                            RedoSearch();
                        }
                    }
                }
            }
            SearchBarGUI();
        }
    }
    /// <summary>
    /// Forcefully activate ShowAll, simulating the user clicking the ShowAll toggle
    /// </summary>
    public void ForceShowAll() {
        if (ShowAllFunc != null) {
            ShowAll = true;
            if (UnsearchedShowAllItems == null && !m_ShowAllFuncCalled) {
                m_ShowAllFuncCalled = true;
                ShowAllFunc(RegisterShowAllItems);
            }
        }
    }
}
