using System.Reflection;
using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.Common.Dropdown;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;
using Kingmaker.Code.UI.MVVM.VM.Slots;
using Kingmaker.Code.UI.MVVM.View.Common.Dropdown;
using Kingmaker.PubSubSystem.Core;

namespace ToyBox.Features.Inventory;

/// <summary>
/// One-way bridge EquipSlotFilter (UMM) -> Inventory Improved (OMM), R14.
/// Clicking a doll equipment slot (ESF's SlotFilterState.Toggle) also applies
/// the matching slot filter in II by driving II's own dropdown VM
/// (_slotVM.SetIndex), so label, state, native-filter reset and grid refresh
/// all happen through II's own pipeline. The sub filter is pinned to its
/// "All" entry (every II sub list starts with All at index 0) and the sub
/// dropdown II auto-expands on category selection is collapsed again.
///
/// ESF and II patch different methods of the vanilla filter pipeline (AND
/// composition), so this bridge adds no filtering of its own. Both target
/// mods are resolved by reflection over loaded assemblies (the
/// WenKaiFontProvider pattern): no compile-time references, and an absent
/// mod leaves the bridge dormant. ToyBox loads before both mods, so
/// resolution is lazy with one retry per area load until both resolve.
/// Weapon hands and quickslots are deliberate no-ops (II has no honest
/// equivalent group); ESF already filters those precisely on its own.
/// </summary>
// No attribute-declared patch methods by design - the patch targets
// live in other mods' assemblies and are bound at runtime via reflection
// (see TryAttach). The analyzer-required attributes above stay for HAR001.
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.Inventory.SlotFilterBridgeFeature")]
public partial class SlotFilterBridgeFeature : FeatureWithPatch {
    private const string EsfStateTypeName = "EquipSlotFilter.SlotFilterState";
    private const string IiViewTypeName = "InventoryImproved.SlotFilterViewPatch";
    private const string IiMainTypeName = "InventoryImproved.Main";
    private const int MaxFailures = 3;

    // Resolved ESF members (statics on EquipSlotFilter.SlotFilterState).
    private static MethodInfo? s_EsfToggle;
    private static MethodInfo? s_EsfClear;
    private static MethodInfo? s_EsfSet;
    private static PropertyInfo? s_EsfActiveSlot;
    private static PropertyInfo? s_EsfIsActive;

    // Resolved II members.
    private static FieldInfo? s_IiSlotVM;
    private static FieldInfo? s_IiSubVM;
    private static FieldInfo? s_IiSubDropdown;
    private static MethodInfo? s_IiReset;
    private static FieldInfo? s_IiSelectedSlot;
    private static FieldInfo? s_IiSelectedAugmentType;
    private static FieldInfo? s_IiCurrentStashVM;

    private static readonly AreaRetry s_AreaRetry = new();
    private static bool s_AreaRetrySubscribed;
    private static bool s_Attached;
    private static bool s_WarnedAbsent;
    private static bool s_Suppress;
    private static int s_Failures;

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.Inventory.SlotFilterBridgeFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableSlotFilterBridge;
        }
    }

    [LocalizedString("ToyBox_Features_Inventory_SlotFilterBridgeFeature_Name", "Bridge Equip Slot Filter to Inventory Improved")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Inventory_SlotFilterBridgeFeature_Description", "Clicking a doll equipment slot (Equip Slot Filter) also selects the matching slot filter in Inventory Improved - one click filters both. Weapon and quickslot clicks are left to Equip Slot Filter alone (Inventory Improved has no matching group). Requires both mods; does nothing otherwise.")]
    public override partial string Description { get; }

    // No attribute-discovered patches: the patch targets live in other mods'
    // assemblies and are resolved by reflection, so Patch()/Unpatch() would
    // find nothing. Manual attach/detach in Enable/Disable instead.
    public override void Enable() {
        s_Failures = 0;
        TryAttach();
        if (!s_Attached && !s_AreaRetrySubscribed && Game.Instance != null) {
            _ = EventBus.Subscribe(s_AreaRetry);
            s_AreaRetrySubscribed = true;
        }
    }

    public override void Disable() {
        if (s_AreaRetrySubscribed) {
            try {
                EventBus.Unsubscribe(s_AreaRetry);
            } catch {
                // EventBus teardown during shutdown; the latch below still holds.
            }
            s_AreaRetrySubscribed = false;
        }
        Detach();
    }

    private sealed class AreaRetry : IAreaHandler {
        public void OnAreaDidLoad() {
            if (Settings.EnableSlotFilterBridge) {
                TryAttach();
            }
        }

        public void OnAreaBeginUnloading() {
        }
    }

    private static void TryAttach() {
        if (s_Attached) {
            return;
        }
        try {
            var esf = FindType(EsfStateTypeName);
            var ii = FindType(IiViewTypeName);
            if (esf == null || ii == null) {
                if (!s_WarnedAbsent) {
                    s_WarnedAbsent = true;
                    Log($"ToyBox SlotFilterBridge: {(esf == null ? "EquipSlotFilter" : "Inventory Improved")} not loaded - bridge stays dormant (re-checked on area load).");
                }
                return;
            }

            const BindingFlags staticAll = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            s_EsfToggle = esf.GetMethod("Toggle", staticAll, null, [typeof(ItemSlotVM)], null)
                ?? throw new MissingMemberException(EsfStateTypeName, "Toggle(ItemSlotVM)");
            s_EsfClear = esf.GetMethod("Clear", staticAll)
                ?? throw new MissingMemberException(EsfStateTypeName, "Clear()");
            s_EsfSet = esf.GetMethod("Set", staticAll, null, [typeof(ItemSlotVM)], null);
            s_EsfActiveSlot = esf.GetProperty("ActiveSlot", staticAll)
                ?? throw new MissingMemberException(EsfStateTypeName, "ActiveSlot");
            s_EsfIsActive = esf.GetProperty("IsActive", staticAll);

            s_IiSlotVM = ii.GetField("_slotVM", staticAll)
                ?? throw new MissingMemberException(IiViewTypeName, "_slotVM");
            s_IiSubVM = ii.GetField("_subVM", staticAll);
            s_IiSubDropdown = ii.GetField("SubFilterDropdown", staticAll);
            s_IiReset = ii.GetMethod("ResetSlotToAll", staticAll)
                ?? throw new MissingMemberException(IiViewTypeName, "ResetSlotToAll()");

            var iiMain = FindType(IiMainTypeName);
            if (iiMain != null) {
                // Fallback write path (no PC filter view built): optional.
                s_IiSelectedSlot = iiMain.GetField("SelectedSlot", staticAll);
                s_IiSelectedAugmentType = iiMain.GetField("SelectedAugmentType", staticAll);
                s_IiCurrentStashVM = iiMain.GetField("CurrentStashVM", staticAll);
            }

            var postfixToggle = typeof(SlotFilterBridgeFeature).GetMethod(nameof(TogglePostfix), BindingFlags.NonPublic | BindingFlags.Static);
            var postfixClear = typeof(SlotFilterBridgeFeature).GetMethod(nameof(ClearPostfix), BindingFlags.NonPublic | BindingFlags.Static);
            var harmony = Feature.GetInstance<SlotFilterBridgeFeature>().HarmonyInstance;
            harmony.Patch(s_EsfToggle, postfix: new HarmonyMethod(postfixToggle));
            harmony.Patch(s_EsfClear, postfix: new HarmonyMethod(postfixClear));
            s_Attached = true;
            s_Failures = 0;
            Log("ToyBox SlotFilterBridge: attached (EquipSlotFilter <-> Inventory Improved).");
        } catch (Exception ex) {
            if (!s_WarnedAbsent) {
                s_WarnedAbsent = true;
                Warn($"ToyBox SlotFilterBridge: attach failed, bridge disabled for this session: {ex.Message}");
            }
            Detach();
        }
    }

    private static void Detach() {
        if (!s_Attached) {
            return;
        }
        try {
            Feature.GetInstance<SlotFilterBridgeFeature>().HarmonyInstance.UnpatchAll("ToyBox.Features.Inventory.SlotFilterBridgeFeature");
        } catch {
            // Nothing to unpatch or instance gone during teardown.
        }
        s_Attached = false;
    }

    private static Type? FindType(string fullName) {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            var type = assembly.GetType(fullName, throwOnError: false);
            if (type != null) {
                return type;
            }
        }
        return null;
    }

    // Postfix on EquipSlotFilter.SlotFilterState.Toggle: after any
    // click-driven transition, mirror the new ESF state into II.
    private static void TogglePostfix() {
        try {
            if (s_Suppress || !s_Attached || !Settings.EnableSlotFilterBridge) {
                return;
            }
            var active = s_EsfActiveSlot?.GetValue(null) as ItemSlotVM;
            if (active == null) {
                // The toggle cycled ESF off: mirror the clear into II.
                ResetII();
            } else {
                int group = active switch {
                    EquipSlotVM equip => SlotFilterBridgeMapper.MapEquipSlot(equip.SlotType),
                    AugmentationsSlotVM => SlotFilterBridgeMapper.SlotAugment,
                    _ => SlotFilterBridgeMapper.NoOp,
                };
                if (group != SlotFilterBridgeMapper.NoOp) {
                    PushToII(active, group);
                }
            }
            // Success: reset only AFTER the work completed, so consecutive
            // failures can actually reach the 3-strike detach (R5 P2-1) -
            // the same placement the minimap overlay's Tick uses.
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Postfix on EquipSlotFilter.SlotFilterState.Clear: user-driven clears
    // (re-click, search, native filter change, stash close, ESF disabled)
    // funnel here and reset II too. II-driven feedback clears are skipped by
    // the suppress guard.
    private static void ClearPostfix() {
        try {
            if (!SlotFilterBridgeMapper.ShouldPropagateClear(s_Suppress, Settings.EnableSlotFilterBridge, s_Attached)) {
                return;
            }
            ResetII();
            // Success: reset only AFTER the work completed (R5 P2-1).
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void PushToII(ItemSlotVM active, int group) {
        s_Suppress = true;
        try {
            if (s_IiSlotVM?.GetValue(null) is OwlcatDropdownVM slotVM) {
                // Full II pipeline: label, state, native-filter reset and grid
                // refresh all run through II's own Index subscription. The
                // same subscription already parks the sub filter at All (it
                // nulls the sub state fields and rebuilds the sub list with
                // initial index 0) and auto-expands the sub dropdown for
                // sub-filter groups - collapsed again by SubFilterToAll below.
                slotVM.SetIndex(group);
            } else {
                // No PC filter view (e.g. augmentations screen): write II's
                // state fields directly and refresh its stash group. The sub
                // filter stays at All (null) for every category.
                s_IiSelectedSlot?.SetValue(null, group);
                s_IiSelectedAugmentType?.SetValue(null, null);
                if (s_IiCurrentStashVM?.GetValue(null) is InventoryStashVM stash) {
                    stash.ItemSlotsGroup.UpdateVisibleCollection();
                }
            }
            // Feedback loop: II's forced native-filter reset re-triggers ESF's
            // StashTracker clear while we are mid-write (only when the native
            // filter was non-All). Re-apply through ESF's private Set so the
            // click's intent survives; Set does not recurse into Toggle.
            var isActive = s_EsfIsActive?.GetValue(null) as bool? ?? true;
            if (!isActive && group != 0) {
                s_EsfSet?.Invoke(null, [active]);
            }
            // Sub-filter default: pin to All after the category push and the
            // suppress-guard re-apply, so the user always sees the whole
            // category and picks subs manually.
            SubFilterToAll(group);
        } finally {
            s_Suppress = false;
        }
    }

    /// <summary>
    /// Pins II's sub filter to its All entry and collapses the sub dropdown.
    /// A category CHANGE already left the sub list at All (II rebuilds it
    /// with initial index 0), but re-pushing the SAME category - Index is a
    /// ReactiveProperty and only fires on change - would keep a manually
    /// chosen sub, so force index 0 through II's own sub handler. The
    /// collapse undoes the SetState(true) auto-expansion II's category
    /// handler performs; immediately: true skips the fade so the blocker and
    /// input layer it created cannot outlive the click.
    ///
    /// Known cosmetic (R4 P2, accepted): the inline expand plays the vanilla
    /// DropdownMenuShow click and our instant collapse plays
    /// DropdownMenuHide, so every bridged push to a sub-filter group emits
    /// an open+close click pair within one frame. II has no play-sound guard
    /// and both sounds are unconditional vanilla calls inside
    /// OwlcatDropdown.Show/Hide - suppressing them would require patching
    /// the game's audio path, which this bridge deliberately does not do.
    /// </summary>
    private static void SubFilterToAll(int group) {
        if (!SlotFilterBridgeMapper.HasSubFilter(group)) {
            return;
        }
        try {
            if (s_IiSubVM?.GetValue(null) is OwlcatDropdownVM subVM) {
                subVM.SetIndex(SlotFilterBridgeMapper.SubAll);
            }
            if (s_IiSubDropdown?.GetValue(null) is OwlcatDropdown dropdown && dropdown) {
                dropdown.SetState(false, immediately: true);
            }
        } catch {
            // View-layer only (the sub state is already at All through the
            // VM write above); a dropdown dying mid-close must not trip the
            // bridge's failure latch.
        }
    }

    private static void ResetII() {
        // II's own reset routine: dropdown label, state and sub-filters.
        s_IiReset?.Invoke(null, null);
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxFailures) {
            Warn($"ToyBox SlotFilterBridge: bridge patch failed ({s_Failures}/{MaxFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxFailures) {
            Warn($"ToyBox SlotFilterBridge: auto-detaching after {MaxFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            Detach();
        }
    }
}
