using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.OtherMultipliers.MovementSpeedMultiplierFeature")]
public partial class MovementSpeedMultiplierFeature : FeatureWithPatch {
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MovementSpeedMultiplierFeature_Name", "Movement Speed")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MovementSpeedMultiplierFeature_Description", "Adjusts the movement speed of your party in area maps.")]
    public override partial string Description { get; }
    private bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.MovementSpeedMultiplier != null;
            return ref m_IsEnabled;
        }
    }
    public override void OnGui() {
        var tmp = Settings.MovementSpeedMultiplier ?? 1f;
        using (HorizontalScope()) {
            if (UI.LogSlider(ref tmp, 0f, 20f, 1f, 2, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MinWidth(150))) {
                if (tmp == 1f) {
                    Settings.MovementSpeedMultiplier = null;
                    Destroy();
                } else {
                    Settings.MovementSpeedMultiplier = tmp;
                    Initialize();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.OtherMultipliers.MovementSpeedMultiplierFeature";
        }
    }
    [HarmonyPatch(typeof(PartMovable), nameof(PartMovable.ModifiedSpeedMps), MethodType.Getter), HarmonyPostfix]
    private static void PartMovable_getModifiedSpeedMps_Patch(PartMovable __instance, ref float __result) {
        if (__instance.Owner is BaseUnitEntity unit && !unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            __result *= Settings.MovementSpeedMultiplier ?? 1f;
        }
    }
    [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.CreateMoveCommandUnit)), HarmonyPostfix]
    private static void UnitHelper_CreateMoveCommandUnit_Patch(AbstractUnitEntity unit, ref UnitMoveToProperParams __result) {
        if (!unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            if (__result.OverrideSpeed != null) {
                __result.OverrideSpeed *= Settings.MovementSpeedMultiplier ?? 1;
            }
        }
    }
    [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.CreateMoveCommandParamsRT)), HarmonyPostfix]
    private static void UnitHelper_CreateMoveCommandParamsRT_Patch(BaseUnitEntity unit, ref UnitMoveToParams __result) {
        if (!unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            if (__result.OverrideSpeed != null) {
                __result.OverrideSpeed *= Settings.MovementSpeedMultiplier ?? 1;
            }
        }
    }
    [HarmonyPatch(typeof(PartMovable), nameof(PartMovable.CalculateCurrentSpeed)), HarmonyPostfix]
    private static void UnitHelper_CreateMoveCommandParamsRT_Patch(PartMovable __instance , ref float __result) {
        if (__instance.Owner is BaseUnitEntity unit && !unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            __result *= Settings.MovementSpeedMultiplier ?? 1;
        }
    }
}
