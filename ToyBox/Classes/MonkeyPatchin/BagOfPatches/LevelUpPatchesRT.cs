// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using Code.GameCore.ElementsSystem;
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.MVVM.VM.CharGen.Phases.BackgroundBase;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.CareerPath;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Levelup;
using Kingmaker.UnitLogic.Progression;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.UnitLogic.Progression.Prerequisites;
using ModKit;
using Owlcat.Runtime.UI.MVVM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ToyBox;
using ToyBox.classes.Infrastructure;
using UnityEngine;
//using ToyBox.Multiclass;

namespace ToyBox.BagOfPatches {
    internal static class LevelUp {
        public static Settings Settings = Main.Settings;
        public static Player player => Game.Instance.Player;

        [HarmonyPatch(typeof(PartUnitProgression))]
        public static class UnitProgressionData_Patch {
            public static int getMaybeZero(PartUnitProgression _instance) {
                if (Settings.toggleSetDefaultRespecLevelZero) {
                    return int.MinValue;
                } else if (Settings.toggleSetDefaultRespecLevelFifteen) {
                    return 15;
                } else if (Settings.toggleSetDefaultRespecLevelThirtyfive) {
                    return 35;
                } else {
                    var tmp = _instance.Owner.Blueprint.GetDefaultLevel();
                    return tmp;
                }
            }
            public static ValueTuple<BlueprintCareerPath, int> maybeGetNextCareer(PartUnitProgression _instance) {
                ValueTuple<BlueprintCareerPath, int> ret;
                try {
                    ret = _instance.AllCareerPaths.Last<ValueTuple<BlueprintCareerPath, int>>();
                } catch (Exception) {
                    ret = new(null, 1);
                }
                Mod.Debug($"Respec Career returned: {ret}");
                return ret;

            }
            [HarmonyPatch(nameof(PartUnitProgression.Respec))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Respec(IEnumerable<CodeInstruction> instructions) {
                bool shouldSkipNextInstruction = false;
                foreach (var instruction in instructions) {
                    if (shouldSkipNextInstruction) {
                        shouldSkipNextInstruction = false;
                        continue;
                    }
                    if (instruction.Calls(AccessTools.Method(typeof(BlueprintUnit), nameof(BlueprintUnit.GetDefaultLevel)))) {
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(UnitProgressionData_Patch), nameof(UnitProgressionData_Patch.getMaybeZero));
                    } else if (instruction.Calls(AccessTools.PropertyGetter(typeof(PartUnitProgression), nameof(PartUnitProgression.AllCareerPaths)))) {
                        instruction.operand = AccessTools.Method(typeof(UnitProgressionData_Patch), nameof(UnitProgressionData_Patch.maybeGetNextCareer));
                        shouldSkipNextInstruction = true;
                    }
                    yield return instruction;
                }

            }
        }
        [HarmonyPatch(typeof(PrerequisiteLevel), nameof(PrerequisiteLevel.MeetsInternal))]
        public static class PrerequisiteLevelPatch {
            [HarmonyPostfix]
            public static void MeetsInternal(PrerequisiteLevel __instance, IBaseUnitEntity unit, ref bool __result) {
                if (!unit.IsPartyOrPetInterface()) return; // don't give extra feats to NPCs
                if (!(__result ^ __instance.Not) && Settings.toggleIgnorePrerequisiteClassLevel) {
                    OwlLogging.Log($"PrerequisiteLevel.MeetsInternal - {unit.CharacterName} - {__instance.GetCaptionInternal()} -{__result} -> {true} ");
                    __result = !__instance.Not;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteFact), nameof(PrerequisiteFact.MeetsInternal))]
        public static class PrerequisiteFactPatch {
            [HarmonyPostfix]
            public static void MeetsInternal(PrerequisiteFact __instance, IBaseUnitEntity unit, ref bool __result) {
                if (!unit.IsPartyOrPetInterface()) return; // don't give extra feats to NPCs
                if (!(__result ^ __instance.Not) && Settings.toggleFeaturesIgnorePrerequisites) {
                    if (!new StackTrace().ToString().Contains("Kingmaker.UI.MVVM.VM.CharGen")) {
                        OwlLogging.Log($"PrerequisiteFact.MeetsInternal - {unit.CharacterName} - {__instance.GetCaptionInternal()} - {__result} -> {true} (Not: {__instance.Not}");
                        __result = !__instance.Not;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteStat), nameof(PrerequisiteStat.MeetsInternal))]
        public static class PrerequisiteStatPatch {
            [HarmonyPostfix]
            public static void MeetsInternal(PrerequisiteStat __instance, IBaseUnitEntity unit, ref bool __result) {
                if (!unit.IsPartyOrPetInterface()) return; // don't give extra feats to NPCs
                if (!(__result ^ __instance.Not) && Settings.toggleIgnorePrerequisiteStatValue) {
                    OwlLogging.Log($"PrerequisiteStat.MeetsInternal - {unit.CharacterName} - {__instance.GetCaptionInternal()} -{__result} -> {true} ");
                    __result = !__instance.Not;
                }
            }
        }
        [HarmonyPatch(typeof(PartUnitProgression), nameof(PartUnitProgression.CanUpgradePath))]
        public static class PartUnitProgression_CanUpgradePath_Patch {
            [HarmonyPostfix]
            public static void Meet(PartUnitProgression __instance, BlueprintPath path, ref bool __result) {
                if (Settings.toggleIgnoreCareerPrerequisites) {
                    OwlLogging.Log($"PartUnitProgression_CanUpgradePath - {__instance.Owner.CharacterName} - {path.Name} - {__result} -> {true}");
                    __result = __instance.GetPathRank(path) < path.Ranks;
                }

            }
        }
        [HarmonyPatch(typeof(CareerPathsListVM), nameof(CareerPathsListVM.GetPrerequisitesCareers))]
        public static class CareerPathsListVM_GetPrerequisitesCareers_Patch {
            [HarmonyPrefix]
            public static void GetPrerequisitesCareers(CareerPathsListVM __instance, ref List<BlueprintCareerPath> careerPaths, ref List<BlueprintFeature> features) {
                if (Settings.toggleIgnoreCareerPrerequisites) {
                    OwlLogging.Log($"CareerPathsListVM_GetPrerequisitesCareers - {__instance} -> remove all prerequisites");
                    careerPaths = [];
                    features = [];
                }

            }
        }
        // This is needed because when leveling up, Owlcode creates a copy of the BlueprintUnit and applies all feats for the Preview.
        // When we change stats the BaseValue is modified. That one is taken from the Blueprint in the Preview so our modifications aren't correctly saved there.
        // This messes up stat previews. The internal stat changes still work, but coloring and +- would be off in a level up preview. This patch fixes that.
        [HarmonyPatch(typeof(UnitHelper))]
        public static class UnitHelper_Patch {
            [HarmonyPatch(nameof(UnitHelper.CreatePreview))]
            [HarmonyPostfix]
            public static void UnitHelper_CreatePreview(BaseUnitEntity _this, bool createView, ref BaseUnitEntity __result) {
                if (new StackTrace().ToString().Contains($"{typeof(LevelUpManager).FullName}.{nameof(LevelUpManager.CreatePreviewUnit)}")) {
                    foreach (var obj in HumanFriendlyStats.StatTypes) {
                        try {
                            var modifiableValue = _this.Stats.GetStatOptional(obj);
                            var modifiableValue2 = __result.Stats.GetStatOptional(obj);
                            if (modifiableValue.BaseValue != modifiableValue2.BaseValue) {
                                modifiableValue2.BaseValue = modifiableValue.BaseValue;
                            }
                        } catch (NullReferenceException) {

                        }
                    }
                }
            }
        }
        // In addition to the above; the preview unit would also have the feats that the unit should have. That prevents picking them on level up. This patch basically respecs the Preview to prevent that.
        [HarmonyPatch(typeof(BlueprintUnit))]
        public static class BlueprintUnit_Patch {
            [HarmonyPatch(nameof(BlueprintUnit.CreateEntity))]
            [HarmonyPostfix]
            public static void CreateEntity(BaseUnitEntity __result) {
                if (Settings.toggleSetDefaultRespecLevelZero || Settings.toggleSetDefaultRespecLevelFifteen || Settings.toggleSetDefaultRespecLevelThirtyfive) {
                    if (new StackTrace().ToString().Contains($"{typeof(LevelUpManager).FullName}.{nameof(LevelUpManager.CreatePreviewUnit)}")) {
                        __result.Progression.Respec();
                    }
                }
            }
        }
    }
}
