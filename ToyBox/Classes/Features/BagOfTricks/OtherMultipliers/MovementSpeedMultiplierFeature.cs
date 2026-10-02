using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using System.Reflection.Emit;
using UnityEngine;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[IsTested]
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
            if (UI.LogSlider(ref tmp, 0.01f, 20f, 1f, 2, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MinWidth(150))) {
                if (tmp == 1f) {
                    Settings.MovementSpeedMultiplier = null;
                    Disable();
                } else {
                    Settings.MovementSpeedMultiplier = tmp;
                    Enable();
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
    [HarmonyPatch(typeof(PartMovable), nameof(PartMovable.CalculateCurrentSpeed)), HarmonyPostfix]
    private static void PartMovable_CalculateCurrentSpeed_Patch(PartMovable __instance, ref float __result) {
        if (__instance.Owner is BaseUnitEntity unit && !unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            var command = __instance.ConcreteOwner?.GetOptional<PartUnitCommands>()?.Current;
            if (command?.OverrideSpeed.HasValue == true || command?.Executor.AnimationManager?.NewSpeed >= 0f) {
                __result *= Settings.MovementSpeedMultiplier ?? 1;
            }
        }
    }
    [HarmonyPatch(typeof(UnitFollowUnitController), nameof(UnitFollowUnitController.HandleMoveCommand)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UnitFollowUnitController_HandleMoveCommand_Patch(IEnumerable<CodeInstruction> instructions) {
        var getSpeed = AccessTools.PropertyGetter(typeof(PartMovable), nameof(PartMovable.CurrentSpeedMps));
        var foundCalls = 0;
        foreach (var instruction in instructions) {
            yield return instruction;
            if (instruction.Calls(getSpeed)) {
                foundCalls++;
                if (foundCalls == 3) {
                    yield return new(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call((float speed, AbstractUnitEntity unit) => UnscaleFollowSpeed(speed, unit));
                }
            }
        }
        ThrowIfTrue(foundCalls != 3);
    }
    private static float UnscaleFollowSpeed(float speed, AbstractUnitEntity unit) {
        var multiplier = Settings.MovementSpeedMultiplier ?? 1f;
        if (unit is BaseUnitEntity && !unit.IsStarship() && ToyBoxUnitHelper.IsPartyOrPet(unit) && multiplier > 0f) {
            return speed / multiplier;
        }
        return speed;
    }
}
