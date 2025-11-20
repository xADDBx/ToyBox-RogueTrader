using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Items;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.IgnoreEquipmentRestrictionsFeature")]
public partial class IgnoreEquipmentRestrictionsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableIgnoreEquipmentRestrictions;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreEquipmentRestrictionsFeature_Name", "Ignore Equipment Restrictions")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_IgnoreEquipmentRestrictionsFeature_Description", "Allows equipping items regardless of class, stat, etc. requirements.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.IgnoreEquipmentRestrictionsFeature";
        }
    }
    [HarmonyPatch(typeof(EquipmentRestrictionClass), nameof(EquipmentRestrictionClass.CanBeEquippedBy)), HarmonyPostfix]
    private static void EquipmentRestrictionClass_CanBeEquippedBy_Patch(ref bool __result) {
        __result = true;
    }
    [HarmonyPatch(typeof(EquipmentRestrictionStat), nameof(EquipmentRestrictionStat.CanBeEquippedBy)), HarmonyPostfix]
    private static void EquipmentRestrictionStat_CanBeEquippedBy_Patch(ref bool __result) {
        __result = true;
    }
    [HarmonyPatch(typeof(ItemEntityArmor), nameof(ItemEntityArmor.CanBeEquippedInternal)), HarmonyPostfix]
    private static void ItemEntityArmor_CanBeEquippedInternal_Patch(ref bool __result) {
        __result = true;
    }
    [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.CanBeEquippedInternal)), HarmonyPostfix]
    private static void ItemEntity_CanBeEquippedInternal_Patch(ref bool __result) {
        __result = true;
    }

}
