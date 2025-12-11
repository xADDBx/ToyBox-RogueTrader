using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ToyBox.Features.Loot;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.Loot.MassLootShowHiddenItemsSetting")]
public partial class MassLootShowHiddenItemsSetting : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.MassLootShowHiddenItems;
        }
    }
    [LocalizedString("ToyBox_Features_Loot_MassLootShowHiddenItemsSetting_Name", "Show Hidden Items")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_MassLootShowHiddenItemsSetting_Description", "Shows items that are hidden or were not viewed. This might include quest items that the game might not intend for you to loot yet.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.Loot.MassLootShowHiddenItemsSetting";
        }
    }
    [HarmonyPatch]
    private static class MassLootHelper_CompilerGenerated_GetMassLootFromCurrentArea {
        [HarmonyTargetMethod]
        private static MethodInfo GetMethod() {
            return typeof(MassLootHelper).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic).First(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null).GetMethod("<GetMassLootFromCurrentArea>b__11_0", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [HarmonyTranspiler]
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
    [HarmonyPatch(typeof(MassLootHelper), nameof(MassLootHelper.GetMassLootFromCurrentArea)), HarmonyTranspiler]
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
    private static bool True(Entity _) {
        return true;
    }
    private static bool True(InteractionLootPart _) {
        return true;
    }
}
