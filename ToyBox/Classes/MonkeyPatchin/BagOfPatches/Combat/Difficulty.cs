using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using System;
using System.Collections.Generic;

namespace ToyBox.BagOfPatches {
    [HarmonyPatch]
    internal static class Difficulty {
        internal static readonly HashSet<StatType> BadStats = [StatType.TemporaryHitPoints, StatType.Unknown, StatType.AttackOfOpportunityCount,
                                                                StatType.Crew, StatType.TurretRadius, StatType.TurretRating, StatType.MilitaryRating,
                                                                StatType.PsyRating, StatType.Evasion, StatType.MachineTrait, StatType.ArmourFore,
                                                                StatType.ArmourPort, StatType.ArmourStarboard, StatType.ArmourAft, StatType.Inertia,
                                                                StatType.Power, StatType.Aiming, StatType.RevealRadius, StatType.DetectionRadius,
                                                                StatType.ShieldsAmount, StatType.ShieldsRegeneration, StatType.Morale, StatType.Discipline, 
                                                                StatType.DamageNonLethal];
        [HarmonyPatch(typeof(ModifiableValue), nameof(ModifiableValue.ModifiedValue), MethodType.Getter), HarmonyPostfix]
        public static void get_ModifiableValue_ModifiedValue(ModifiableValue __instance, ref int __result) {
            if (BadStats.Contains(__instance.OriginalType)) {
                return;
            }
            if (__instance.Owner is BaseUnitEntity entity && entity is not StarshipEntity && entity.IsPlayerEnemy) {
                var stat = __instance.OriginalType;
                if (Main.Settings.toggleAddFlatEnemyMods) {
                    var flat = Main.Settings.flatEnemyMods[stat];
                    if (flat != 0) {
                        __result += (int)flat;
                    }
                }
                if (Main.Settings.toggleAddMultiplierEnemyMods) {
                    var mult = Main.Settings.multiplierEnemyMods[stat];
                    if (mult != 1) {
                        __result = (int)(mult * __result);
                    }
                }
            }
        }
    }
}
