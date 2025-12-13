using UnityEngine;

namespace ToyBox.Infrastructure;

/// <summary>
/// A vertical paginated UI list for displaying and interacting with a collection of items of type <typeparamref name="T"/>.
/// Supports optional detail toggling and customizable pagination settings.
/// </summary>
/// <typeparam name="T">The type of items to display. Must be non-nullable.</typeparam>
public partial class VerticalList<T> : IPagedList where T : notnull {
    protected int PageWidth = 600;
    protected int CurrentPage = 1;
    protected int PagedItemsCount = 0;
    protected int TotalPages = 1;
    protected int ItemCount = 0;
    protected bool ShowDivBetweenItems = true;
    public List<T> PagedItems = [];
    protected IEnumerable<T> Items = [];
    protected int? OverridePageLimit;
    protected int EffectivePageLimit {
        get {
            return OverridePageLimit ?? Settings.PageLimit;
        }
    }
    private bool m_IsCached = false;
    /// <summary>
    /// Marks data as cached
    /// </summary>
    public void SetCacheValid() {
        m_IsCached = true;
    }
    /// <summary>
    /// Marks cached data as invalid
    /// </summary>
    public void SetCacheInvalid() {
        m_IsCached = false;
    }
    /// <summary>
    /// Shows whether previously cached data is still valid
    /// </summary>
    /// <returns>Validity of cached data</returns>
    public bool IsCachedValid {
        get {
            return m_IsCached;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalList{T}"/> class.
    /// </summary>
    /// <param name="initialItems">
    /// Optional initial collection of items to populate the browser with.
    /// <para>
    /// If null, the browser starts empty until <see cref="RegisterShowAllItems"/> or
    /// <see cref="VerticalList{T}.QueueUpdateItems(IEnumerable{T}, int?)"/> is called.
    /// </para>
    /// </param>
    /// <param name="showDivBetweenItems">Whether to draw a divider between items in the list.</param>
    /// <param name="overridePageWidth">Optional override for the width of the list. Default width is 600.</param>
    /// <param name="overridePageLimit">Optional override for the number of items per page. Default PageLimit is a setting with 25 as initial value.</param>
    public VerticalList(IEnumerable<T>? initialItems = null, bool showDivBetweenItems = true, int? overridePageWidth = null, int? overridePageLimit = null) {
        if (overridePageWidth.HasValue) {
            PageWidth = overridePageWidth.Value;
        }
        OverridePageLimit = overridePageLimit;
        if (initialItems != null) {
            UpdateItems(initialItems);
        }
        ShowDivBetweenItems = showDivBetweenItems;
        Main.m_VerticalLists.Add(new(this));
    }
    /// <summary>
    /// Queues an update to replace the current item list with a new collection. Runs on the main thread.
    /// </summary>
    /// <param name="newItems">The new items to display.</param>
    /// <param name="forcePage">If provided, forces the list to jump to the specified page after update.</param>
    /// <param name="onlyDisplayedItems">Whether the update actually changes the base item collection (or just restricts it to a subset due e.g. a search</param>
    public virtual void QueueUpdateItems(IEnumerable<T> newItems, int? forcePage = null, bool onlyDisplayedItems = false) {
        Main.ScheduleForMainThread(new(() => {
            UpdateItems(newItems, forcePage, onlyDisplayedItems);
        }));
    }
    /// <summary>
    /// Runs an update to replace the current item list with a new collection. Prefer the usage of <see cref="QueueUpdateItems(IEnumerable{T}, int?)"./>
    /// </summary>
    /// <param name="newItems">The new items to display.</param>
    /// <param name="forcePage">If provided, forces the list to jump to the specified page after update.</param>
    /// <param name="onlyDisplayedItems">Whether the update actually changes the base item collection (or just restricts it to a subset due e.g. a search</param>
    internal virtual void UpdateItems(IEnumerable<T> newItems, int? forcePage = null, bool onlyDisplayedItems = false) {
        if (forcePage != null) {
            CurrentPage = 1;
        }
        Items = newItems;
        ItemCount = Items.Count();
        UpdatePages();
    }
    /// <summary>
    /// Recalculate the current amount of pages and thereby update the currently displayed items
    /// </summary>
    public virtual void UpdatePages() {
        if (EffectivePageLimit > 0) {
            TotalPages = (int)Math.Ceiling((double)ItemCount / EffectivePageLimit);
            CurrentPage = Math.Max(Math.Min(CurrentPage, TotalPages), 1);
        } else {
            CurrentPage = 1;
            TotalPages = 1;
        }
        UpdatePagedItems();
    }
    protected virtual void UpdatePagedItems() {
        var offset = Math.Min(ItemCount, (CurrentPage - 1) * EffectivePageLimit);
        PagedItemsCount = Math.Min(EffectivePageLimit, ItemCount - offset);
        PagedItems = [.. Items.Skip(offset).Take(PagedItemsCount)];
        SetCacheInvalid();
    }
    protected void PageGUI() {
        if (PageWidth < 300 * Main.UIScale) {
            using (VerticalScope()) {
                UI.Label($"{(SharedStrings.MatchesText + ":").Orange()} {ItemCount.ToString().Cyan()} => {PagedItemsCount.ToString().Cyan()},");
                using (HorizontalScope()) {
                    UI.Label($"{(SharedStrings.PageText + ":").Orange()} {CurrentPage.ToString().Cyan()} / {Math.Max(1, TotalPages).ToString().Cyan()}");
                    SwapPageGUI();
                }
            }
        } else {
            UI.Label($"{(SharedStrings.MatchesText + ":").Orange()} {ItemCount.ToString().Cyan()} => {PagedItemsCount.ToString().Cyan()}, " +
                $"{(SharedStrings.PageText + ":").Orange()} {CurrentPage.ToString().Cyan()} / {Math.Max(1, TotalPages).ToString().Cyan()}");
            SwapPageGUI();
        }
    }
    private void SwapPageGUI() {
        var prev = GUI.enabled;
        GUI.enabled = TotalPages > 1;
        Space(25);
        if (UI.Button("-")) {
            if (CurrentPage <= 1) {
                CurrentPage = TotalPages;
            } else {
                CurrentPage -= 1;
            }
            UpdatePagedItems();
        }
        if (UI.Button("+")) {
            if (CurrentPage >= TotalPages) {
                CurrentPage = 1;
            } else {
                CurrentPage += 1;
            }
            UpdatePagedItems();
        }
        GUI.enabled = prev;
    }
    protected virtual void HeaderGUI(Action? onHeaderGui = null) {
        onHeaderGui?.Invoke();
        using (HorizontalScope()) {
            PageGUI();
        }
    }
    public bool CurrentlyIsLastElement {
        get;
        private set;
    }
    /// <summary>
    /// Renders the paged list using the provided item GUI rendering callback.
    /// </summary>
    /// <param name="onItemGUI">A delegate that renders an individual item of type <typeparamref name="T"/>.</param>
    /// <param name="onHeaderGui">An optional delegate to render a header GUI e.g. for settings <typeparamref name="T"/>.</param>
    public virtual void OnGUI(Action<T> onItemGUI, Action? onHeaderGui = null) {
        using (VerticalScope(PageWidth)) {
            HeaderGUI(onHeaderGui);
            for (var i = 0; i < PagedItems.Count; i++) {
                if (ShowDivBetweenItems) {
                    Div.DrawDiv();
                }
                if (i == PagedItems.Count - 1) {
                    CurrentlyIsLastElement = true;
                }
                onItemGUI(PagedItems[i]);
                CurrentlyIsLastElement = false;
            }
        }
    }
}
