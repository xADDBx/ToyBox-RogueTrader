using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;

namespace ToyBox.Features.Inventory;

/// <summary>
/// Pure ESF-to-II translation table (R14). All constants mirror Inventory
/// Improved's SlotGroup enum values (All=0, Head=1, Neck=2, Accessory=3,
/// Cloak=4, Body=5, Hands=6, Legs=7, Melee1H=8, Melee2H=9, Ranged1H=10,
/// Ranged2H=11, Augment=12, Ship=13, Familiar=14, Unequippable=15).
/// Verified against II 1.5.3 / ESF 1.1.0.
/// </summary>
public static class SlotFilterBridgeMapper {
    /// <summary>Slot has no honest II equivalent; the bridge must no-op.</summary>
    public const int NoOp = -1;

    public const int SlotHead = 1;
    public const int SlotNeck = 2;
    public const int SlotAccessory = 3;
    public const int SlotCloak = 4;
    public const int SlotBody = 5;
    public const int SlotHands = 6;
    public const int SlotLegs = 7;
    public const int SlotMelee1H = 8;
    public const int SlotMelee2H = 9;
    public const int SlotRanged1H = 10;
    public const int SlotRanged2H = 11;
    public const int SlotAugment = 12;
    public const int SlotFamiliar = 14;

    /// <summary>
    /// II parks every category's sub-dropdown at "All": each sub list is
    /// built with All at index 0 (armor: [null, Light, Medium, Heavy,
    /// Power]; weapons: [null, families..., Shields]; augment: [null, Eyes,
    /// Items, Torso, Arms, Systems, Misc, Legs]) and RebuildSubFilter
    /// constructs the VM with initial index 0. The bridge therefore pins the
    /// sub filter to this index for every pushed category, so the user sees
    /// the whole category and picks subs manually.
    /// </summary>
    public const int SubAll = 0;

    /// <summary>
    /// Doll equipment slot to II SlotGroup. Weapon hands are a deliberate
    /// no-op: II splits weapons into Melee1H/Melee2H/Ranged1H/Ranged2H by
    /// weapon, so any single choice would wrongly hide valid candidates that
    /// ESF's own IsItemSupported correctly shows. QuickSlots have no "usable"
    /// group in II either.
    /// </summary>
    public static int MapEquipSlot(EquipSlotType slot) {
        return slot switch {
            EquipSlotType.Armor or EquipSlotType.Shirt => SlotBody,
            EquipSlotType.Head or EquipSlotType.Glasses => SlotHead,
            EquipSlotType.Neck => SlotNeck,
            EquipSlotType.Belt or EquipSlotType.Ring1 or EquipSlotType.Ring2 or EquipSlotType.Wrist => SlotAccessory,
            EquipSlotType.Gloves => SlotHands,
            EquipSlotType.Feet => SlotLegs,
            EquipSlotType.Shoulders => SlotCloak,
            EquipSlotType.PetProtocol => SlotFamiliar,
            EquipSlotType.Augment => SlotAugment,
            _ => NoOp,
        };
    }

    /// <summary>
    /// II builds and shows a sub-dropdown only for Augment (12), Body (5)
    /// and the four weapon groups 8-11 (its own category handler gates on
    /// i == 12 || i == 5 || IsWeaponSlot(i)); for exactly those groups it
    /// also auto-expands the dropdown (SubFilterDropdown.SetState(true)),
    /// which the bridge collapses after pushing.
    /// </summary>
    public static bool HasSubFilter(int slotGroup) {
        return slotGroup is SlotBody or SlotAugment
            or (>= SlotMelee1H and <= SlotRanged2H);
    }

    /// <summary>
    /// Clear-propagation gate: an ESF-side Clear must reach II only when the
    /// bridge is live AND the clear was NOT caused by the bridge's own write
    /// (II's forced native-filter reset re-triggers ESF's StashTracker clear -
    /// without this guard a click would leave II filtered and ESF silently
    /// off).
    /// </summary>
    public static bool ShouldPropagateClear(bool suppressed, bool settingEnabled, bool attached) {
        return !suppressed && settingEnabled && attached;
    }
}
