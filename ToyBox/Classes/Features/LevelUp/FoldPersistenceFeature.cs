using System.Reflection;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// Fold persistence in the level-up feature picker (R12). The game already
/// keeps expanded/collapsed state per RankEntrySelectionVM (in
/// m_GroupExpansionState / m_SourceGroupExpansionState), but every freshly
/// opened slot VM starts with empty dictionaries, so groups re-expand each
/// time the next slot opens (N-1 full re-collapses per rank with the talent
/// multiplier). This feature copies the dictionaries into a session store
/// keyed by FeatureGroup and reseeds each new VM before the game reads them -
/// exactly the persistence pattern FeaturesFilterVM.ThisSessionFilter already
/// uses for the filter itself. Session ends when the progression window is
/// disposed; Commit (RefreshData) keeps folds across multi-level sprees.
/// Favourites mode has no headers and the ship picker has no folds: the seed
/// prefix is a harmless no-op in both.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.FoldPersistenceFeature")]
public partial class FoldPersistenceFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;
    private static int s_Failures;

    private static readonly PickerFoldStore s_Store = new();
    private static readonly HeaderOwnerRegistry<AvailableTalentsDropDownVM, RankEntrySelectionVM> s_HeaderOwners = new();

    private static readonly FieldInfo? s_ByTypeField = typeof(RankEntrySelectionVM)
        .GetField("m_GroupExpansionState", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? s_BySourceField = typeof(RankEntrySelectionVM)
        .GetField("m_SourceGroupExpansionState", BindingFlags.Instance | BindingFlags.NonPublic);

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.FoldPersistenceFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableFoldStatePersistence;
        }
    }

    [LocalizedString("ToyBox_Features_LevelUp_FoldPersistenceFeature_Name", "Keep Picker Group Folds")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_FoldPersistenceFeature_Description", "Remembers expanded/collapsed groups in the level-up feature picker across slot switches, picks and commits for the whole progression-window session (the same lifetime the picker's filter already uses). Favourites view and ship pickers are unaffected.")]
    public override partial string Description { get; }

    public override void Enable() {
        base.Enable();
        // Re-enable resets the strike counter: only consecutive failures may
        // auto-disable.
        s_Failures = 0;
    }

    // Seed before the game reads the dictionaries. Single funnel in front of
    // every list build (slot click, filter, grouping mode, favourites change).
    [HarmonyPatch(typeof(RankEntrySelectionVM), "HandleFilterChange"), HarmonyPrefix]
    private static void RankEntrySelectionVM_HandleFilterChange_Prefix(RankEntrySelectionVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            if (s_ByTypeField?.GetValue(__instance) is not Dictionary<FeaturesFilter.FeatureFilterType, bool> byType
                || s_BySourceField?.GetValue(__instance) is not Dictionary<string, bool> bySource) {
                return;
            }
            _ = s_Store.TrySeed(__instance.FeatureGroup, byType, bySource);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // The header just appended to FilteredGroupList is the last one at this
    // moment; bind it to its creator so a later toggle knows where to record.
    [HarmonyPatch(typeof(RankEntrySelectionVM), "AddSourceDropdownGroup"), HarmonyPostfix]
    private static void RankEntrySelectionVM_AddSourceDropdownGroup_Postfix(RankEntrySelectionVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            BindLastHeader(__instance);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    [HarmonyPatch(typeof(RankEntrySelectionVM), "AddDropdownGroup"), HarmonyPostfix]
    private static void RankEntrySelectionVM_AddDropdownGroup_Postfix(RankEntrySelectionVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            BindLastHeader(__instance);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Vanilla's own IsExpanded subscription has already written the owner's
    // dictionary synchronously before the postfix runs; snapshot both grouping
    // modes so a switch of grouping mode keeps the other mode's folds too.
    [HarmonyPatch(typeof(AvailableTalentsDropDownVM), nameof(AvailableTalentsDropDownVM.Switch)), HarmonyPostfix]
    private static void AvailableTalentsDropDownVM_Switch_Postfix(AvailableTalentsDropDownVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            RecordOwnerFold(__instance);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    [HarmonyPatch(typeof(AvailableTalentsDropDownVM), nameof(AvailableTalentsDropDownVM.Expand)), HarmonyPostfix]
    private static void AvailableTalentsDropDownVM_Expand_Postfix(AvailableTalentsDropDownVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            RecordOwnerFold(__instance);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    [HarmonyPatch(typeof(AvailableTalentsDropDownVM), nameof(AvailableTalentsDropDownVM.Collapse)), HarmonyPostfix]
    private static void AvailableTalentsDropDownVM_Collapse_Postfix(AvailableTalentsDropDownVM __instance) {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            RecordOwnerFold(__instance);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Session clear: the progression window (level-up / char screen) closed.
    [HarmonyPatch(typeof(UnitProgressionVM), "DisposeImplementation"), HarmonyPostfix]
    private static void UnitProgressionVM_DisposeImplementation_Postfix() {
        try {
            if (s_Failures > 0) {
                s_Failures = 0;
            }
            s_Store.Clear();
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void BindLastHeader(RankEntrySelectionVM owner) {
        var header = owner.FilteredGroupList.OfType<AvailableTalentsDropDownVM>().LastOrDefault();
        if (header != null) {
            s_HeaderOwners.Bind(header, owner);
        }
    }

    private static void RecordOwnerFold(AvailableTalentsDropDownVM header) {
        if (!s_HeaderOwners.TryGetOwner(header, out var owner) || owner == null) {
            return;
        }
        if (s_ByTypeField?.GetValue(owner) is not Dictionary<FeaturesFilter.FeatureFilterType, bool> byType
            || s_BySourceField?.GetValue(owner) is not Dictionary<string, bool> bySource) {
            return;
        }
        s_Store.Record(owner.FeatureGroup, byType, bySource);
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox LevelUp: fold persistence patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox LevelUp: auto-disabling FoldPersistenceFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<FoldPersistenceFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
