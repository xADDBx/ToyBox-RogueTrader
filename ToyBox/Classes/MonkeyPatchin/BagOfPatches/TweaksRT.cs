// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using DG.Tweening;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Cargo;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.View.LoadingScreen;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.Code.UI.MVVM.VM.MessageBox;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;
using Kingmaker.Code.UI.MVVM.VM.ShipCustomization;
using Kingmaker.Code.UI.MVVM.VM.Slots;
using Kingmaker.Code.UI.MVVM.VM.WarningNotification;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Designers;
using Kingmaker.DLC;
using Kingmaker.ElementsSystem.ContextData;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameCommands;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Networking;
using Kingmaker.Pathfinding;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.Legacy.MainMenuUI;
using Kingmaker.UI.Sound;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using Kingmaker.Visual.Sound;
using ModKit;
using Owlcat.Runtime.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniRx;
using UnityEngine;
using Warhammer.SpaceCombat.Blueprints;
using static Kingmaker.UnitLogic.Abilities.AbilityData;

namespace ToyBox.BagOfPatches {
    internal static partial class Tweaks {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(TurnController))]
        private static class TurnController_Patch {
            [HarmonyPatch(nameof(TurnController.IsPlayerTurn), MethodType.Getter)]
            [HarmonyPostfix]
            private static void IsPlayerTurn(TurnController __instance, ref bool __result) {
                if (__instance.CurrentUnit == null) return;
                if (Main.Settings.perSave.doOverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.HashKey(), out var maybeOverride)) {
                    if (maybeOverride.Item1) {
                        __result = !maybeOverride.Item2;
                    }
                }
            }
            [HarmonyPatch(nameof(TurnController.IsAiTurn), MethodType.Getter)]
            [HarmonyPostfix]
            private static void IsAiTurn(TurnController __instance, ref bool __result) {
                if (__instance.CurrentUnit == null) return;
                if (Main.Settings.perSave.doOverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.HashKey(), out var maybeOverride)) {
                    if (maybeOverride.Item1) {
                        __result = maybeOverride.Item2;
                    }
                }
            }
        }
        /*
        [HarmonyPatch(typeof(HandSlot))]
        public static class A {
            [HarmonyPatch(nameof(HandSlot.IsItemSupported)), HarmonyFinalizer]
            private static Exception Wow(ref bool __result, Exception __exception) {
                if (__exception != null) __result = false;
                return null;
            }
        }
        [HarmonyPatch(typeof(PartUnitBody))]
        private static class B {
            [HarmonyPatch(nameof(PartUnitBody.GetHandsEquipmentSet)), HarmonyPostfix]
            private static void Wow(HandsEquipmentSet __result, PartUnitBody __instance, HandSlot slot) {
                if (__result == null) {
                    for (int i = 0; i < __instance.m_HandsEquipmentSets.Length; i++) {
                        HandsEquipmentSet handsEquipmentSet = __instance.m_HandsEquipmentSets[i];
                        if (handsEquipmentSet.PrimaryHand == slot) {
                            Mod.Log($"Primary == Slot but: {handsEquipmentSet.IsOverridePrimaryHand}");
                        }
                        if (handsEquipmentSet.SecondaryHand == slot) {
                            Mod.Log($"Primary == Slot but: {handsEquipmentSet.IsOverrideSecondaryHand}");
                        }
                    }
                }
            }
        }
        */
        [HarmonyPatch(typeof(PartUnitCombatState))]
        private static class PartUnitCombatStatePatch {
            public static void MaybeKill(PartUnitCombatState unitCombatState) {
                if (Settings.togglekillOnEngage) {
                    List<BaseUnitEntity> partyUnits = Game.Instance.Player.m_PartyAndPets;
                    BaseUnitEntity unit = unitCombatState.Owner;
                    if (unit.CombatGroup.IsEnemy(GameHelper.GetPlayerCharacter())
                        && !partyUnits.Contains(unit)) {
                        CheatsCombat.KillUnit(unit);
                    }
                }
            }

            [HarmonyPatch(nameof(PartUnitCombatState.JoinCombat))]
            [HarmonyPostfix]
            public static void JoinCombat(PartUnitCombatState __instance, bool surprised) {
                MaybeKill(__instance);
            }
        }

        [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
        private static class GameHistoryLogHandlePartyCombatStateChangedPatch {
            private static void Postfix(ref bool inCombat) {
                if (!inCombat && Settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    var partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (var u in partyMembers) {
                        foreach (var resource in u.AbilityResources)
                            u.AbilityResources.Restore(resource);
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && Settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
            }
        }
        [HarmonyPatch(typeof(AbilityData))]
        private static class AbilityDataPatch {
            //[HarmonyPatch(nameof(AbilityData.CanTargetFromNode), new Type[] {typeof(CustomGridNodeBase), typeof(CustomGridNodeBase), typeof(TargetWrapper), typeof(int), typeof(LosCalculations.CoverType), typeof(UnavailabilityReasonType?)})]
            [HarmonyPatch(nameof(AbilityData.CanTargetFromNode),
                          new Type[] {
                              typeof(CustomGridNodeBase),
                              typeof(CustomGridNodeBase),
                              typeof(TargetWrapper),
                              typeof(int),
                              typeof(LosCalculations.CoverType),
                              typeof(UnavailabilityReasonType?),
                              typeof(int?)
                          },
                          new ArgumentType[] {
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Out,
                              ArgumentType.Out,
                              ArgumentType.Out,
                              ArgumentType.Normal
                          })]
            [HarmonyPostfix]
            public static void CanTargetFromNode(
                    CustomGridNodeBase casterNode,
                    CustomGridNodeBase targetNodeHint,
                    TargetWrapper target,
                    ref int distance,
                    ref LosCalculations.CoverType los,
                    ref UnavailabilityReasonType? unavailabilityReason,
                    AbilityData __instance,
                    ref bool __result
                ) {
                if (!Settings.toggleIgnoreAbilityAnyRestriction) return;
                if (!(__instance?.Caster?.IsPartyOrPet() ?? false)) return;
                if (__result) return;

                if (unavailabilityReason is UnavailabilityReasonType reason) {
                    switch (reason) {
                        case UnavailabilityReasonType.AreaEffectsCannotOverlap:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityAoeOverlap)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.HasNoLosToTarget:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityLineOfSight)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.TargetTooFar:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityTargetTooFar)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.TargetTooClose:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityTargetTooClose)
                                __result = true;
                            break;
                        default:
                            if (Settings.toggleIgnoreAbilityAnyRestriction)
                                __result = true;
                            break;
                    }
                } else if (Settings.toggleIgnoreAbilityAnyRestriction)
                    __result = true;
            }
        }
        [HarmonyPatch(typeof(MainMenuPCView))]
        private static class MainMenuPCViewPatch {
            [HarmonyPatch(nameof(MainMenuPCView.BindViewImplementation))]
            [HarmonyPostfix]
            private static void BindViewImplementation(MainMenuPCView __instance) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    Game.Instance.SaveManager.UpdateSaveListIfNeeded();
                    MainThreadDispatcher.StartCoroutine(UIUtilityCheckSaves.WaitForSaveUpdated(() => { __instance.ViewModel.LoadLastGame(); }));
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(LoadingScreenBaseView))]
        public static class LoadingScreenBaseViewPatch {
            [HarmonyPatch(nameof(LoadingScreenBaseView.ShowUserInputWaiting))]
            [HarmonyPrefix]
            private static bool ShowUserInputLayer(LoadingScreenBaseView __instance, bool state) {
                if (!Settings.toggleSkipAnyKeyToContinueWhenLoadingSaves) return true;
                if (!state)
                    return false;
                __instance.m_ProgressBarContainer.DOFade(0.0f, 1f).OnComplete(() => __instance.StartPressAnyKeyLoopAnimation()).SetUpdate(true);
                __instance.AddDisposable(MainThreadDispatcher.UpdateAsObservable()
                                                             .Subscribe(_ => {
                                                                 UISounds.Instance.Sounds.Buttons.ButtonClick.Play();
                                                                 if (PhotonManager.Lobby.IsLoading)
                                                                     PhotonManager.Instance.ContinueLoading();
                                                                 EventBus.RaiseEvent((Action<IContinueLoadingHandler>)(h => h.HandleContinueLoading()));
                                                             }));
                return false;
            }
        }
        public static class UnitEntityDataCanRollPerceptionExtension {
            public static bool TriggerReroll = false;
            public static bool CanRollPerception(BaseUnitEntity unit) {
                if (TriggerReroll) {
                    return true;
                }

                return unit.MovementAgent.Position.To2D() != unit.MovementAgent.m_PreviousPosition;
            }
        }
        [HarmonyPatch]
        public static class EquipDuringCombat_Transpiler_Patches {
            private static string[] InventoryHelperTargetMethodNames = ["TryDrop", "TryEquip", "TryMoveSlotInInventory", "TryMoveToCargo", "TryUnequip", "CanChangeEquipment", "CanEquipItem"];
            [HarmonyTargetMethods]
            public static IEnumerable<MethodBase> GetMethods() {
                foreach (var method in AccessTools.GetDeclaredMethods(typeof(InventoryHelper))) {
                    if (InventoryHelperTargetMethodNames.Contains(method.Name)) {
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
            public static IEnumerable<CodeInstruction> ChooseSlotToItem(IEnumerable<CodeInstruction> instructions) {
                bool skipNext = false;
                foreach (var inst in instructions) {
                    if (skipNext) {
                        skipNext = false;
                        continue;
                    }
                    if (inst.Calls(AccessTools.Method(typeof(Game), "get_Player"))) {
                        skipNext = true;
                        yield return CodeInstruction.Call((Game game) => ShouldPreventInsertion(game));
                        continue;
                    }
                    if (inst.Calls(AccessTools.Method(typeof(TurnController), "get_TurnBasedModeActive"))) {
                        yield return CodeInstruction.Call((TurnController controller) => ShouldPreventInsertion(controller));
                        continue;
                    }
                    if (inst.Calls(AccessTools.Method(typeof(MechanicEntity), "get_IsInCombat"))) {
                        yield return CodeInstruction.Call((MechanicEntity entity) => ShouldPreventInsertion(entity));
                        continue;
                    }
                    yield return inst;
                }
            }
            public static bool ShouldPreventInsertion(MechanicEntity entity) {
                if (Settings.toggleEquipItemsDuringCombat) return false;
                else return entity.IsInCombat;
            }
            public static bool ShouldPreventInsertion(TurnController controller) {
                if (Settings.toggleEquipItemsDuringCombat) return false;
                else return controller.TurnBasedModeActive;
            }
            public static bool ShouldPreventInsertion(Game game) {
                if (Settings.toggleEquipItemsDuringCombat) return false;
                else return game.Player.IsInCombat;
            }
        }
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter)]
        public static class ItemEntityIsUsableFromInventoryPatch {
            // Allow Item Use From Inventory During Combat
            public static bool Prefix(ItemEntity __instance, ref bool __result) {
                if (!Settings.toggleUseItemsDuringCombat) return true;
                return __instance.Blueprint is not BlueprintItemEquipmentUsable;
            }
        }

        [HarmonyPatch(typeof(PartyAwarenessController))]
        public static class PartyAwarenessControllerPatch {
#if false // TODO: why does this crash the game on load into area
            public static MethodInfo HasMotionThisSimulationTick_Method = AccessTools.DeclaredMethod(typeof(PartMovable), "get_HasMotionThisSimulationTick");
            public static MethodInfo CanRollPerception_Method = AccessTools.DeclaredMethod(typeof(UnitEntityData_CanRollPerception_Extension), "CanRollPerception");

            [HarmonyPatch(nameof(PartyAwarenessController.Tick))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                foreach (var instr in instructions) {
                    if (instr.Calls(HasMotionThisSimulationTick_Method)) {
                        Mod.Trace("Found HasMotionThisSimulationTick and modded it");
                        yield return new CodeInstruction(OpCodes.Call, CanRollPerception_Method);
                    }
                    else {
                        yield return instr;
                    }
                }
            }
#endif
            [HarmonyPatch(nameof(PartyAwarenessController.Tick))]
            [HarmonyPostfix]
            private static void Tick() => UnitEntityDataCanRollPerceptionExtension.TriggerReroll = false;
        }
        [HarmonyPatch]
        public static class MusicStateHandler_Patch {
            [HarmonyPatch(typeof(MusicStateHandler), nameof(MusicStateHandler.SetMusicState)), HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SetMusicState(IEnumerable<CodeInstruction> instructions) {
                foreach (var inst in instructions) {
                    if (inst.operand is MethodInfo mi && mi.Name.Contains("Reverse")) {
                        yield return CodeInstruction.Call((IEnumerable<BlueprintDlc> source) => MaybeReverse(source)).WithLabels(inst.ExtractLabels());
                    } else {
                        yield return inst;
                    }
                }
            }
            public static IEnumerable<BlueprintDlc> MaybeReverse(IEnumerable<BlueprintDlc> source) {
                if (Settings.toggleDLC1Theme) {
                    return source;
                } else {
                    return source.Reverse();
                }
            }
        }
        [HarmonyPatch]
        public static class SkipSplashScreen_Patch {
            [HarmonyPrepare]
            public static bool Prepare() => Settings.toggleSkipSplashScreen;
            [HarmonyTargetMethods]
            public static IEnumerable<MethodInfo> PatchTargets() {
                yield return AccessTools.Method(typeof(SplashScreenController), nameof(SplashScreenController.ShowSplashScreen));
                yield return AccessTools.Method(typeof(MainMenuLoadingScreen), nameof(MainMenuLoadingScreen.OnStart));
            }
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> instructions) {
                foreach (var inst in instructions) {
                    if (inst.Calls(AccessTools.Method(typeof(GameStarter), nameof(GameStarter.IsArbiterMode))) || inst.Calls(AccessTools.Method(typeof(GameStarter), nameof(GameStarter.IsSkippingMainMenu)))) {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(inst.labels);
                    } else {
                        yield return inst;
                    }
                }
            }
        }
    }
}
