using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Items;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.InventoryItemUseDuringCombatFeature")]
public partial class InventoryItemUseDuringCombatFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIInventorytemUseDuringCombat;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InventoryItemUseDuringCombatFeature_Name", "Inventory Item Use during Combat")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InventoryItemUseDuringCombatFeature_Description", "Allows using items in inventory during combat.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.InventoryItemUseDuringCombatFeature";
        }
    }
    [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter), HarmonyPostfix]
    public static void ItemEntity_IsUsableFromInventory_Patch(ItemEntity __instance, ref bool __result) {
        if (__instance.Blueprint is BlueprintItemEquipmentUsable) {
            __result = true;
        }
    }
}
