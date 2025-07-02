using HarmonyLib;
using Kingmaker.UnitLogic.FactLogic;
using System;

namespace ToyBox.BagOfPatches {
    [HarmonyPatch]
    internal static class Difficulty {
        [HarmonyPatch(typeof(NPCDifficultyModifiersManager), nameof(NPCDifficultyModifiersManager.UpdateModifiers)), HarmonyPostfix]
        public static void DifficultyPresetsController_SetValues(NPCDifficultyModifiersManager __instance) {
            if (__instance.Owner.IsPlayerEnemy) {
                if (Main.Settings.toggleAddFlatEnemyMods) {
                    foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                        var flat = Main.Settings.flatEnemyMods[stat];
                        if (flat != 0) {
                            __instance.AddModifier(stat, (int)flat);
                        }
                    }
                }
                if (Main.Settings.toggleAddMultiplierEnemyMods) {
                    foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                        var mult = Main.Settings.multiplierEnemyMods[stat];
                        if (mult != 1) {
                            __instance.AddPercentModifier(stat, (int)(mult*100) - 1);
                        }
                    }
                }
            }
        }
    }
}
