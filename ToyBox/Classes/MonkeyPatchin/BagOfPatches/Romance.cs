using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ToyBox.BagOfPatches {
    internal static class Romance {
        public static Settings settings = Main.Settings;

        // Any Gender Any Romance Overrides
        // These modify the PcFemale/PcMale conditions for specific Owner blueprints 
        internal static readonly Dictionary<string, bool> PcFemaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0004
            { "5457755c30ac417d9279fd740b90f549", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0023
            { "8d6b7c53af134494a64a4de789759fb9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_11
            { "4b4b769261f04a8cb5726e111c3f7081", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_2
            { "cf1d7205cf854709b038db477db48ac9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Check_0011
            { "d2c500fbc1b5450c8663d453a33b0eee", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // World\Dialogs\Ch1\BridgeAndCabinet\Briefing\Answer_15
            { "02e0bc30b5a146708dd62d68ac7490bd", true },
            // World\Dialogs\Companions\CompanionDialogues\Interrogator\Cue_10
            { "2df6bd21ad5a45a9b1c5142d51d647dc", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_45
            { "0739ef639d774629a27d396cd733cfd4", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_67
            { "ea42722c44c84835b7363db2fc09b23b", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_47
            { "41897fd7a52249d3a53691fbcfcc9c19", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_89
            { "c5efaa0ace544ca7a81d439e7cfc6ae5", true }
        };
        internal static readonly Dictionary<string, bool> PcMaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_0017
            { "85b651edb4f74381bbe762999273c6ec", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_10
            { "56bbf1612e05489ba44bb4a52718e222", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_5
            { "eb76f93740824d16b1e1f54b82de21e0", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_8
            { "c292b399f4344a639ccb4df9ba66329e", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\CassFirstTimeBlushing_a
            { "95b0ba7d08e34f6c895b2fbeb53ea404", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q1\CassiaSeriousTalk\Answer_8
            { "966f0cc2defa42bd836950aa1ebcde72", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_24
            { "a903589840ba4ab683d6e6b9f985d458", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_11
            { "c051d0c9f2ba4c23bff1d1e6f2cfe13d", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_12
            { "3d24df76aacf4e2db047cf47ef3474d5", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_19
            { "b3601cd9e84d43dbb4078bf77c89d728", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_6
            { "17b34e1ae36443408805af3a3c2866f7", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Cue_29
            { "7f71e0b93dd9420d87151fc3e7114865", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_47
            { "588a3c2e96c6403ca2c7104949b066e4", true },
            // World\Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q2\Cassia_Q2_BE\Cue_0037
            { "bf7813b4ee3d49cdbc6305f454479db3", true }
        };

        // Path Romance Overrides
        // These modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverridesLoveIsFree = new() {
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverridesLoveIsFree = new() {
        };

        // Multiple Romances overrides
        // This modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverrides = new() {
            // Cue_43 (I assume Kibella) check for any active romance
            { ("f4b42351c500429ba9cfbccf352ddd3b", "6a8601e5d98a450f8ed8b644c1cf1fea"), false },
            // Cue_43 (I assume Kibella) check jealousy dialog seen or active romances = 1 
            { ("f4b42351c500429ba9cfbccf352ddd3b", "09f80fe401704413864b879f3bdfb970"), false }
        };
        // Multiple Romances overrides
        // This modifies all conditions of the blueprint with the specified id
        internal static readonly Dictionary<string, bool> AllConditionCheckOverrides = new() {
            // Kibellah romance after coming back (All)
            { "d269a5417ca646759584a2ab7bddf319", false }, { "5ca382ed53964851bd19ce07efb7bb8c", false },
            // Kibellah romance after coming back (Cas)
            { "683bf82b7663452a9fb92955b4b1d031", false }, { "aae351192ac24f84ab36e1839c1ab7c3", false },
            // Kibellah romance after coming back (Hein)
            { "6b08fa9121c54f3c811536fef69f12c9", false }, { "ec45728761064a27bfafca1fbfa65355", false },
            // Kibellah romance after coming back (Jae)
            { "25163b765a8442e2928f3423f080fa57", false }, { "83953390c5764c25a9d2d0b1f899f905", false },
            // Kibellah romance after coming back (Mar)
            { "7d30413d5b7a426aa891b1e282792134", false }, { "2b989a9de0f04c2c848269294a0a4452", false },
            // Kibellah romance after coming back (Yrl)
            { "c409b0626cce411ab6720916f310d9f2", false }, { "efc090e2c9794fac855be6c597637bda", false }
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverrides = new() {
        };
        internal static readonly Dictionary<string, bool> FlagInRangeOverrides = new() {
            // RomanceCount Flag, as conditioned in Jealousy_event Blueprint, Activated by Jealousy_preparation
            { "cbb219fcb46948fba48a8bed94663e5d", false }
        };


        [HarmonyPatch(typeof(PcFemale), nameof(PcFemale.CheckCondition))]
        public static class PcFemale_CheckCondition_Patch {
            public static void Postfix(PcFemale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                OwlLogging.Log($"checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcFemaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }
        [HarmonyPatch(typeof(PcMale), nameof(PcMale.CheckCondition))]
        public static class PcMale_CheckCondition_Patch {
            public static void Postfix(PcMale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                OwlLogging.Log($"checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcMaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }

        [HarmonyPatch(typeof(Condition), nameof(Condition.Check), [typeof(ConditionsChecker), typeof(IConditionDebugContext)])]
        public static class Condition_Check_Patch {
            public static void Postfix(Condition __instance, ref bool __result) {
                if (__instance?.Owner is null) return;

                var key = (__instance.Owner.AssetGuid, __instance.AssetGuid);
                if (settings.toggleAllowAnyGenderRomance) {
                    if (ConditionCheckOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { OwlLogging.Log($"overiding {(__instance.Owner.name, __instance.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (ConditionCheckOverrides.TryGetValue(key, out var value)) {
                        OwlLogging.Log($"overiding {(__instance.Owner.name, __instance.name)} to {value}");
                        __result = value;
                    }
                    if (AllConditionCheckOverrides.TryGetValue(__instance.Owner.AssetGuid, out value)) {
                        OwlLogging.Log($"overiding {(__instance.Owner.name, __instance.name)} to {value}");
                        __result = value;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EtudeStatus), nameof(EtudeStatus.CheckCondition))]
        public static class EtudeStatus_CheckCondition_Patch {
            public static void Postfix(EtudeStatus __instance, ref bool __result) {
                if (__instance?.Owner is null) return;

                var key = (__instance.Owner.AssetGuid.ToString());
                if (settings.toggleAllowAnyGenderRomance) {
                    if (EtudeStatusOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { OwlLogging.Log($"overiding {(__instance.Owner.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (EtudeStatusOverrides.TryGetValue(key, out var value)) { OwlLogging.Log($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                }
            }
        }

        [HarmonyPatch(typeof(FlagInRange), nameof(FlagInRange.CheckCondition))]
        public static class FlagInRange_CheckCondition_Patch {
            public static void Postfix(FlagInRange __instance, ref bool __result) {
                if (__instance?.Owner is null) return;
                if (settings.toggleMultipleRomance) {
                    if (FlagInRangeOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { OwlLogging.Log($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                }
            }
        }
        [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked), nameof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked.CheckCondition))]
        public static class RomanceLocked_CheckCondition_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMultipleRomance) {
                    if (__result) OwlLogging.Log("Overriding RomanceLocked.CheckCondition result to false");
                    __result = false;
                }
            }
        }
    }
}
