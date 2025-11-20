using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.InfiniteChargesOnItemsFeature")]
public partial class InfiniteChargesOnItemsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableInfiniteChargesOnItems;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InfiniteChargesOnItemsFeature_Name", "Infinite Charges on Items")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InfiniteChargesOnItemsFeature_Description", "Prevents charges on usable items from being spent.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.InfiniteChargesOnItemsFeature";
        }
    }
    [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.SpendCharges), [typeof(MechanicEntity)]), HarmonyPrefix]
    private static bool ItemEntity_SpendCharges_Patch(MechanicEntity user, ItemEntity __instance, ref bool __result) {
        if (user is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit) && __instance.Blueprint is BlueprintItemEquipment item) {
            __result = item.GainAbility;
            return false;
        }
        return true;
    }
}
