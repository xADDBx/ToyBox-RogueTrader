using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.Mechanics.Entities;
using Kingmaker.Utility;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ToyBox.Features.Loot;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.Loot.MassLootShowLivingNPCItemsSetting")]
public partial class MassLootShowLivingNPCItemsSetting : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.MassLootShowLivingNPCItems;
        }
    }
    [LocalizedString("ToyBox_Features_Loot_MassLootShowLivingNPCItemsSetting_Name", "Steal from living NPCs")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_MassLootShowLivingNPCItemsSetting_Description", "Includes items from living NPCs. This might include items you are not supposed to get.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.Loot.MassLootShowLivingNPCItemsSetting";
        }
    }
    [HarmonyTargetMethod]
    private static MethodInfo GetMethod() {
        return typeof(MassLootHelper).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic).First(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null).GetMethod("<GetMassLootFromCurrentArea>b__11_0", BindingFlags.Instance | BindingFlags.NonPublic);
    }
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MassLootHelper_CompilerGenerated_GetMassLootFromCurrentArea_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var isDeadAndHasLoot = AccessTools.PropertyGetter(typeof(AbstractUnitEntity), nameof(AbstractUnitEntity.IsDeadAndHasLoot));
        var isRevealed = AccessTools.PropertyGetter(typeof(Entity), nameof(Entity.IsRevealed));
        foreach (var inst in instructions) {
            if (inst.Calls(isDeadAndHasLoot)) {
                inst.operand = ((Func<BaseUnitEntity, bool>)IsDeadAndHasLoot).Method;
            } else if (inst.Calls(isRevealed)) {
                inst.operand = ((Func<Entity, bool>)True).Method;
            }
            yield return inst;
        }
    }
    private static bool IsDeadAndHasLoot(BaseUnitEntity unit) {
        return unit.IsDeadAndHasLoot || unit.Inventory.HasLoot;
    }
    private static bool True(Entity _) {
        return true;
    }
}
