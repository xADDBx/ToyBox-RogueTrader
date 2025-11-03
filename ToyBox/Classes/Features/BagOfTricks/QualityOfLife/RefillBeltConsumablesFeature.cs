using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using UniRx;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.RefillBeltConsumablesFeature")]
public partial class RefillBeltConsumablesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableRefillBeltConsumables;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_RefillBeltConsumablesFeature_Name", "Refill Belt Consumables")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_RefillBeltConsumablesFeature_Description", "Automatically refill consumables in belt slots if there are some in your inventory.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.RefillBeltConsumablesFeature";
        }
    }
    [HarmonyPatch(typeof(Kingmaker.Items.Slots.ItemSlot), nameof(Kingmaker.Items.Slots.ItemSlot.RemoveItem), [typeof(bool), typeof(bool), typeof(bool)]), HarmonyPrefix]
    private static void ItemSlot_RemoveItem_Pre(Kingmaker.Items.Slots.ItemSlot __instance, ref ItemEntity? __state) {
        if (Game.Instance.CurrentMode == GameModeType.Default) {
            __state = null;
            if (__instance.Owner is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
                var slot = unit.Body.QuickSlots.FirstOrDefault(s => s.HasItem && s.Item == __instance.m_ItemRef);
                if (slot != null) {
                    __state = __instance.m_ItemRef;
                }
            }
        }
    }


    [HarmonyPatch(typeof(Kingmaker.Items.Slots.ItemSlot), nameof(Kingmaker.Items.Slots.ItemSlot.RemoveItem), [typeof(bool), typeof(bool), typeof(bool)]), HarmonyPostfix]
    private static void ItemSlot_RemoveItem_Post(Kingmaker.Items.Slots.ItemSlot __instance, ref ItemEntity? __state) {
        if (Game.Instance.CurrentMode == GameModeType.Default) {
            if (__state != null && !(__state.Collection != null && __state.Collection != __instance.MaybeOwnerInventory?.Collection)) {
                var blueprint = __state.Blueprint;
                var item = Game.Instance.Player.Inventory.Items.FirstOrDefault(i => i.Blueprint.ItemType == ItemsItemType.Usable && i.Blueprint == blueprint);
                if (item != null) {
                    Main.ScheduleForMainThread(() => {
                        try {
                            Debug($"Refill {BPHelper.GetTitle(item.Blueprint)}");
                            __instance.InsertItem(item);
                        } catch (Exception ex) {
                            Error(ex);
                        }
                    });
                }
                __state = null;
            }
        }
    }
}
