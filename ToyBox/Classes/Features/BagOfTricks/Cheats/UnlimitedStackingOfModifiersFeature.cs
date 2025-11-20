using Kingmaker.EntitySystem.Entities;
using static Kingmaker.EntitySystem.Stats.ModifiableValue;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.UnlimitedStackingOfModifiersFeature")]
public partial class UnlimitedStackingOfModifiersFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableUnlimitedStackingOfModifiers;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_UnlimitedStackingOfModifiersFeature_Name", "Unlimited Stacking of Modifiers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_UnlimitedStackingOfModifiersFeature_Description", "Makes modifiers like Stats or Damage stack for party members even if they shouldn't.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.UnlimitedStackingOfModifiersFeature";
        }
    }
    [HarmonyPatch(typeof(Modifier), nameof(Modifier.Stacks), MethodType.Getter), HarmonyPostfix]
    public static void Modifier_Stacks_Patch(Modifier __instance, ref bool __result) {
        if (__instance?.AppliedTo?.Owner is BaseUnitEntity unit && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            __result = true;
        }
    }
}
