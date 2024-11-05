using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.VM.NavigatorResource;
using Kingmaker.Controllers;
using Kingmaker.Designers.WarhammerSurfaceCombatPrototype;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using ModKit;
using ModKit.DataViewer;
using System;
using System.Collections.Generic;
using ToyBox.BagOfPatches;
using UnityEngine;

namespace ToyBox {
    // Made for Rogue Trader specific stuff which I have no idea where to put
    public static class RogueCheats {
        public static Settings Settings => Main.Settings;
        private static int selectedFaction = 0;
        public static NamedFunc<FactionType>[] factionsToPick;
        private static Browser<BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData, BlueprintPsychicPhenomenaRoot.PsychicPhenomenaData> PsychicPhenomenaBrowser = new(true, true) { DisplayShowAllGUI = false };
        private static Browser<BlueprintAbilityReference, BlueprintAbilityReference> PerilsOfTheWarpMinorBrowser = new(true, true) { DisplayShowAllGUI = false };
        private static Browser<BlueprintAbilityReference, BlueprintAbilityReference> PerilsOfTheWarpMajorBrowser = new(true, true) { DisplayShowAllGUI = false };
        private static int reputationAdjustment = 100;
        private static int navigatorInsightAdjustment = 100;
        private static int scrapAdjustment = 100;
        private static int profitFactorAdjustment = 1;
        private static int veilThicknessAdjustment = 1;
        private static int startingWidth = 250;
        public static void OnGUI() {
            if (factionsToPick == null) {
                List<NamedFunc<FactionType>> tmp = new();
                foreach (FactionType @enum in Enum.GetValues(typeof(FactionType))) {
                    tmp.Add(new NamedFunc<FactionType>(@enum.ToString(), () => @enum));
                }
                factionsToPick = tmp.ToArray();
            }
            if (Game.Instance?.Player == null) {
                Label("Load a save to find options like modifying Faction Reputation, Scrap and more.".localize());
                return;
            }
            var selected = TypePicker("Faction Selector".localize().Bold(), ref selectedFaction, factionsToPick, true);
            15.space();
            var faction = selected.func();
            using (HorizontalScope()) {
                Label("Current Reputation".localize().Bold() + ": ", Width(startingWidth));
                using (VerticalScope()) {
                    using (HorizontalScope()) {
                        Label("Level".localize() + ": ", Width(100));
                        Label(ReputationHelper.GetCurrentReputationLevel(faction).ToString());
                    }
                    using (HorizontalScope()) {
                        Label("Experience".localize() + ": ", Width(100));
                        Label($"{ReputationHelper.GetCurrentReputationPoints(faction)}/{ReputationHelper.GetNextLevelReputationPoints(faction)}");
                    }
                    using (HorizontalScope()) {
                        Label("Adjust Reputation by the following amount:".localize());
                        IntTextField(ref reputationAdjustment, null, MinWidth(200), AutoWidth());
                        reputationAdjustment = Math.Max(0, reputationAdjustment);
                        10.space();
                        ActionButton("Add".localize(), () => ReputationHelper.GainFactionReputation(faction, reputationAdjustment));
                        10.space();
                        ActionButton("Remove".localize(), () => ReputationHelper.GainFactionReputation(faction, -reputationAdjustment));
                    }
                }
            }
            15.space();
            Div();
            15.space();
            bool warpInit = Game.Instance.Player.WarpTravelState?.IsInitialized ?? false;
            VStack("Resources".localize().Bold(),
                () => {
                    if (warpInit) {
                        using (HorizontalScope()) {
                            Label("Current Navigator Insight".localize().Bold() + ": ", Width(startingWidth));
                            using (VerticalScope()) {
                                Label(Game.Instance.Player.WarpTravelState.NavigatorResource.ToString());
                                using (HorizontalScope()) {
                                    Label("Adjust Navigator Insight by the following amount:".localize());
                                    IntTextField(ref navigatorInsightAdjustment, null, MinWidth(200), AutoWidth());
                                    navigatorInsightAdjustment = Math.Max(0, navigatorInsightAdjustment);
                                    10.space();
                                    ActionButton("Add".localize(), () => { CheatsGlobalMap.AddNavigatorResource(navigatorInsightAdjustment); SectorMapBottomHudVM.Instance?.SetCurrentValue(); });
                                    10.space();
                                    ActionButton("Remove".localize(), () => { CheatsGlobalMap.AddNavigatorResource(-navigatorInsightAdjustment); SectorMapBottomHudVM.Instance?.SetCurrentValue(); });
                                }
                            }
                        }
                    }
                },
            () => {
                using (HorizontalScope()) {
                    Label("Current Scrap".localize().Bold() + ": ", Width(startingWidth));
                    using (VerticalScope()) {
                        Label(Game.Instance.Player.Scrap.m_Value.ToString());
                        using (HorizontalScope()) {
                            Label("Adjust Scrap by the following amount:".localize());
                            IntTextField(ref scrapAdjustment, null, MinWidth(200), AutoWidth());
                            scrapAdjustment = Math.Max(0, scrapAdjustment);
                            10.space();
                            ActionButton("Add".localize(), () => Game.Instance.Player.Scrap.Receive(scrapAdjustment));
                            10.space();
                            ActionButton("Remove".localize(), () => Game.Instance.Player.Scrap.Receive(-scrapAdjustment));
                        }
                    }
                }
            },
            () => {
                using (HorizontalScope()) {
                    Label("Current Profit Factor".localize().Bold() + ": ", Width(startingWidth));
                    using (VerticalScope()) {
                        Label(Game.Instance.Player.ProfitFactor.Total.ToString());
                        using (HorizontalScope()) {
                            Label("Adjust Profit Factor by the following amount:".localize());
                            IntTextField(ref profitFactorAdjustment, null, MinWidth(200), AutoWidth());
                            profitFactorAdjustment = Math.Max(0, profitFactorAdjustment);
                            10.space();
                            ActionButton("Add".localize(), () => CheatsColonization.AddPF(profitFactorAdjustment));
                            10.space();
                            ActionButton("Remove".localize(), () => CheatsColonization.AddPF(-profitFactorAdjustment));
                        }
                    }
                }
            },
            () => {
                using (HorizontalScope()) {
                    var VeilThicknessCounter = Game.Instance.TurnController?.VeilThicknessCounter;
                    if (VeilThicknessCounter != null && Game.Instance.LoadedAreaState?.AreaVailPart != null) {
                        Label("Current Veil Thickness".localize().Bold() + ": ", Width(startingWidth));
                        using (VerticalScope()) {
                            Label(VeilThicknessCounter.Value.ToString());
                            using (HorizontalScope()) {
                                Label("Set Veil Thickness to the following amount:".localize());
                                IntTextField(ref veilThicknessAdjustment, null, MinWidth(200), AutoWidth());
                                veilThicknessAdjustment = Math.Max(0, veilThicknessAdjustment);
                                10.space();
                                ActionButton("Set".localize(), () => VeilThicknessCounter.Value = veilThicknessAdjustment);
                            }
                        }
                    }
                }
            });
            VStack("Tweaks".localize().Bold(),
                () => {
                    using (HorizontalScope()) {
                        if (Toggle("Disable Random Encounters in Warp".localize().Bold(), ref Settings.disableWarpRandomEncounter, Width(startingWidth))) {
                            if (warpInit && !Settings.disableWarpRandomEncounter) {
                                CheatsRE.TurnOnRandomEncounters();
                            }
                        }
                        if (warpInit && Settings.disableWarpRandomEncounter) {
                            if (!Game.Instance.Player.WarpTravelState.ForbidRE.Value) {
                                CheatsRE.TurnOffRandomEncounters();
                            }
                        }
                    }
                },
                () => Toggle("Prevent Psychic Phenomena".localize(), ref Settings.toggleNoPsychicPhenomena),
                () => Toggle("Prevent Veil Thickness from changing".localize(), ref Settings.freezeVeilThickness),
                () => {
                    if (Toggle("Customize Psychic Phenomena/Perils of the Warp".localize(), ref Settings.customizePsychicPhenomena)) {
                        PatchPsychicTranspiler(!Settings.customizePsychicPhenomena);
                    }
                });
            if (Settings.customizePsychicPhenomena) {
                15.space();
                Div();
                15.space();
                VStack("Customization".localize().Bold(),
                    () => {
                        Label("Psychic Phenomena".localize());
                        PsychicPhenomenaBrowser.OnGUI(RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PsychicPhenomena, () => RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PsychicPhenomena,
                            i => i, i => i.Bark.Entries[0].Text.String + i.Bark.Entries[0].Text.name, i => [i.Bark.Entries[0].Text.String.ToString(), i.Bark.Entries[0].Text.name], null,
                            (psychicphenomena, _) => {
                                using (HorizontalScope()) {
                                    var internalName = psychicphenomena.Bark.Entries[0].Text.name;
                                    string desc = psychicphenomena.Bark.Entries[0].Text.String;
                                    Label(internalName.Cyan(), Width(200));
                                    Space(50);
                                    Label(desc.Green(), Width(400));
                                    if (Settings.excludedRandomPhenomena.Contains(internalName)) {
                                        ActionButton("Allow".localize(), () => {
                                            Settings.excludedRandomPhenomena.Remove(internalName);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    } else {
                                        ActionButton("Disable".localize(), () => {
                                            Settings.excludedRandomPhenomena.Add(internalName);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    }
                                }
                                ReflectionTreeView.DetailToggle("", psychicphenomena, psychicphenomena, 0);
                            },
                            (psychicphenomena, _) => { ReflectionTreeView.OnDetailGUI(psychicphenomena); });
                    },
                    () => {
                        Label("MinorPerils".localize());
                        PerilsOfTheWarpMinorBrowser.OnGUI(RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PerilsOfTheWarpMinor, () => RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PerilsOfTheWarpMinor,
                            i => i, i => BlueprintExtensions.GetSearchKey(i.Get(), true), i => [BlueprintExtensions.GetSortKey(i.Get())], null,
                            (minorPeril, _) => {
                                using (HorizontalScope()) {
                                    Label(BlueprintExtensions.GetSearchKey(minorPeril.Get(), true).Cyan(), Width(650));
                                    if (Settings.excludedPerilsMinor.Contains(minorPeril.guid)) {
                                        ActionButton("Allow".localize(), () => {
                                            Settings.excludedPerilsMinor.Remove(minorPeril.guid);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    } else {
                                        ActionButton("Disable".localize(), () => {
                                            Settings.excludedPerilsMinor.Add(minorPeril.guid);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    }
                                }
                                ReflectionTreeView.DetailToggle("", minorPeril, minorPeril, 0);
                            },
                            (minorPeril, _) => { ReflectionTreeView.OnDetailGUI(minorPeril); });
                    },
                    () => {
                        Label("MajorPerils".localize());
                        PerilsOfTheWarpMajorBrowser.OnGUI(RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PerilsOfTheWarpMajor, () => RuleCalculatePsychicPhenomenaEffect.PsychicPhenomenaRoot.PerilsOfTheWarpMajor,
                            i => i, i => BlueprintExtensions.GetSearchKey(i.Get(), true), i => [BlueprintExtensions.GetSortKey(i.Get())], null,
                            (majorPeril, _) => {
                                using (HorizontalScope()) {
                                    Label(BlueprintExtensions.GetSearchKey(majorPeril.Get(), true).Cyan(), Width(650));
                                    if (Settings.excludedPerilsMajor.Contains(majorPeril.guid)) {
                                        ActionButton("Allow".localize(), () => {
                                            Settings.excludedPerilsMajor.Remove(majorPeril.guid);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    } else {
                                        ActionButton("Disable".localize(), () => {
                                            Settings.excludedPerilsMajor.Add(majorPeril.guid);
                                            Misc.InvalidateFilteredWarpPhenomenaArrays();
                                        }, AutoWidth());
                                    }
                                }
                                ReflectionTreeView.DetailToggle("", majorPeril, majorPeril, 0);
                            },
                            (majorPeril, _) => { ReflectionTreeView.OnDetailGUI(majorPeril); });
                    });
            }
        }
        public static void PatchPsychicTranspiler(bool unpatch) {
            var target = AccessTools.Method(typeof(RuleCalculatePsychicPhenomenaEffect), nameof(RuleCalculatePsychicPhenomenaEffect.OnTrigger));
            if (unpatch) {
                Main.HarmonyInstance.Unpatch(target, HarmonyPatchType.Transpiler, Main.ModEntry.Info.Id);
            } else {
                Main.HarmonyInstance.Patch(target, transpiler: new(AccessTools.Method(typeof(Misc), nameof(Misc.RuleCalculatePsychicPhenomenaEffect_OnTrigger_Transpiler))));
            }
        }
    }
}