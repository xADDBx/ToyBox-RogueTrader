using Kingmaker;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.UI.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ToyBox.Features.BagOfTricks.Cheats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Cheats.EquipmentChangeDuringCombatFeature")]
public partial class EquipmentChangeDuringCombatFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableEquipmentChangeDuringCombat;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_EquipmentChangeDuringCombatFeature_Name", "Equipment Change during Combat")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_EquipmentChangeDuringCombatFeature_Description", "Allows changing your equipment while in combat.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Cheats.EquipmentChangeDuringCombatFeature";
        }
    }
    private static readonly string[] m_InventoryHelperTargetMethodNames = ["TryDrop", "TryEquip", "TryMoveSlotInInventory", "TryMoveToCargo", "TryUnequip", "CanChangeEquipment", "CanEquipItem"];
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> GetMethods() {
        foreach (var method in AccessTools.GetDeclaredMethods(typeof(InventoryHelper))) {
            if (m_InventoryHelperTargetMethodNames.Contains(method.Name)) {
                yield return method;
            }
        }
        yield return AccessTools.Method(typeof(InventoryDollVM), nameof(InventoryDollVM.ChooseSlotToItem));
        yield return typeof(InventoryDollVM).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)).First(m => m.Name.Contains("TryInsertItem"));
        yield return AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.IsPossibleInsertItems));
        yield return AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.IsPossibleRemoveItems));
        yield return AccessTools.Method(typeof(ArmorSlot), nameof(ArmorSlot.IsItemSupported));
        yield return AccessTools.Method(typeof(ArmorSlot), nameof(ArmorSlot.CanRemoveItem));
    }
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> EquipmentChangeDuringCombatTranspiler(IEnumerable<CodeInstruction> instructions) {
        var skipNext = false;
        foreach (var inst in instructions) {
            if (skipNext) {
                skipNext = false;
                continue;
            }
            if (inst.Calls(AccessTools.PropertyGetter(typeof(Game), nameof(Game.Player)))) {
                skipNext = true;
                yield return new CodeInstruction(OpCodes.Pop).WithLabels(inst.labels);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                continue;
            }
            if (inst.Calls(AccessTools.PropertyGetter(typeof(TurnController), nameof(TurnController.TurnBasedModeActive)))) {
                yield return new CodeInstruction(OpCodes.Pop).WithLabels(inst.labels);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                continue;
            }
            if (inst.Calls(AccessTools.PropertyGetter(typeof(MechanicEntity), nameof(MechanicEntity.IsInCombat)))) {
                yield return new CodeInstruction(OpCodes.Pop).WithLabels(inst.labels);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                continue;
            }
            yield return inst;
        }
    }
}
