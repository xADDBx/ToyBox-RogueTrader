using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry.Feature;
using Owlcat.Runtime.UI.MVVM;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// Pure hide criterion for owned picker items (R13): an entry is hidden only
/// when it is BOTH greyed out (NotSelectable = cannot take it now) AND owned
/// with no further rank available (UnitCanTakeFeature == false). Owned items
/// that can still be ranked up stay visible (they are Selectable), and
/// not-owned prerequisite failures stay visible-greyed (the game's own
/// ShowUnavailableFeatures toggle governs those). Stat advancements
/// (RankEntrySelectionStatVM) override IsRepeatable=true, which vanilla folds
/// into UnitCanTakeFeature - so they can never satisfy the criterion.
///
/// Display layer only: callers filter the fresh List copies returned by
/// RankEntryFeatureGroupVM.GetAll/GetFiltered, never the group's FeatureList
/// and never SelectionStateFeature.Items, so prerequisites, commit, replay and
/// validation behave exactly like vanilla.
/// </summary>
public static class OwnedItemsFilter {
    public static bool ShouldHide(RankEntrySelectionFeatureVM vm) {
        return vm.FeatureState.Value == RankFeatureState.NotSelectable
            && !vm.UnitCanTakeFeature;
    }

    /// <summary>
    /// Removes hidden entries from a result list in place and returns how many
    /// were removed. Emptying a group makes the picker drop its header
    /// naturally (Add*DropdownGroup skips count-0 groups) and an empty
    /// FilteredGroupList shows the game's own "no features" text.
    /// </summary>
    public static int RemoveHidden(IList<VirtualListElementVMBase> items) {
        int removed = 0;
        for (int i = items.Count - 1; i >= 0; i--) {
            if (items[i] is RankEntrySelectionFeatureVM vm && ShouldHide(vm)) {
                items.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }
}
