using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ToyBox.Features.Loot;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.Loot.LootChecklistShowHiddenLootSetting")]
public partial class LootChecklistShowHiddenLootSetting : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.LootChecklistShowHiddenLoot;
        }
    }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistShowHiddenLootSetting_Name", "Show Hidden Loot in Checklist")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistShowHiddenLootSetting_Description", "Also includes containers that are still hidden in the list.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.Loot.LootChecklistShowHiddenLootSetting";
        }
    }
    [HarmonyPatch]
    private static class MassLootHelper_CompilerGenerated_GetMassLootFromCurrentArea {
        [HarmonyTargetMethod]
        private static MethodInfo GetMethod() {
            return typeof(MassLootHelper).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic).First(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null).GetMethod("<GetMassLootFromCurrentArea>b__11_0", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [HarmonyTranspiler, HarmonyPriority(Priority.LowerThanNormal)]
        private static IEnumerable<CodeInstruction> MassLootHelper_CompilerGenerated_GetMassLootFromCurrentArea_Transpiler(IEnumerable<CodeInstruction> instructions) {
            var isRevealed = AccessTools.PropertyGetter(typeof(Entity), nameof(Entity.IsRevealed));
            foreach (var inst in instructions) {
                if (inst.Calls(isRevealed)) {
                    inst.operand = ((Func<Entity, bool>)True).Method;
                }
                yield return inst;
            }
        }
    }
    [HarmonyPatch(typeof(MassLootHelper), nameof(MassLootHelper.GetMassLootFromCurrentArea)), HarmonyTranspiler, HarmonyPriority(Priority.LowerThanNormal)]
    private static IEnumerable<CodeInstruction> MassLootHelper_GetMassLootFromCurrentArea_Patch(IEnumerable<CodeInstruction> instructions) {
        var isRevealed = AccessTools.PropertyGetter(typeof(Entity), nameof(Entity.IsRevealed));
        var lootViewed = AccessTools.PropertyGetter(typeof(InteractionLootPart), nameof(InteractionLootPart.LootViewed));
        foreach (var inst in instructions) {
            if (inst.Calls(isRevealed)) {
                inst.operand = ((Func<Entity, bool>)True).Method;
            } else if (inst.Calls(lootViewed)) {
                inst.operand = ((Func<InteractionLootPart, bool>)True).Method;
            }
            yield return inst;
        }
    }
    private static bool True(Entity unit) {
        return unit.IsRevealed || GetInstance<LootChecklistFeature>().IsGatheringLoot;
    }
    private static bool True(InteractionLootPart part) {
        return part.LootViewed || GetInstance<LootChecklistFeature>().IsGatheringLoot;
    }
}
