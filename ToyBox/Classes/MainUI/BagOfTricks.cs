﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Dialog;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Mechanics.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using Kingmaker.Visual.Sound;
using ModKit;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.BagOfPatches;
using ToyBox.classes.MainUI;
using UnityEngine;
using UnityModManagerNet;
namespace ToyBox {
    public static class BagOfTricks {
        public static Settings Settings => Main.Settings;
                                                         // AstraMilitarum                   ,  Commissar                        ,  Criminal
        private static readonly string[] OccupationIds = [ "4b908491051a4f36b9703b95e048a5a3", "00b183680643424abe015263aac81c5b", "8fab55c9130a4ae0a745f4fa1674c5df",
                                  // MinistorumCrusader               ,  NavyOfficer                      ,  Nobility                         ,  SanctionedPsyker
                                    "d840a5dc947546e0b4ac939287191fd8", "962c310fd1664ae996c759e4d11a2d88", "06180233245249eea90d222bb1c13f00", "1518d1434ed646039215da3fdda6b096",
                                  // Arbitrator                       
                                    "cd1baf99dad544168bbf4962b3389d94"];
        private static IEnumerable<BlueprintUnitFact> Occupations;
        private static Browser<BlueprintUnitFact, BlueprintUnitFact> OccupationBrowser = new(true, true, false, false) { DisplayShowAllGUI = false };
        // cheats combat
        private const string RestAll = "Rest All";
        private const string RestSelected = "Rest Selected";
        private const string Empowered = "Empowered";
        private const string FullBuffPlease = "Common Buffs";
        private const string GoddesBuffs = "Buff Like A Goddess";
        private const string RemoveBuffs = "Remove Buffs";
        private const string RemoveDeathsDoor = "Remove Deaths Door";
        private const string KillAllEnemies = "Kill All Enemies";
        //private const string SummonZoo = "Summon Zoo"
        private const string LobotomizeAllEnemies = "Lobotomize Enemies";
        private const string ToggleMurderHobo = "Toggle Murder Hobo";

        // cheats common
        private const string TeleportPartyToYou = "Teleport Party To You";
        private const string GoToGlobalMap = "Go To Global Map";
        private const string RerollPerception = "Reroll Perception";
        private const string RerollInteractionSkillChecks = "Reset Interactables";
        private const string ChangeParty = "Change Party";
        private const string ChangWeather = "Change Weather";
        private const string GoToTradeWindow = "Open Trade Window";

        // other
        private const string TimeScaleMultToggle = "Main/Alt Timescale";
        private const string PreviewDialogResults = "Preview Results";


        //For buffs exceptions
        private static bool showBuffDurationExceptions = false;

        public static void OnLoad() {
            // Combat
            KeyBindings.RegisterAction(RestAll, () => CheatsCombat.RestAll());
            KeyBindings.RegisterAction(RestSelected, () => Actions.RestSelected());
            KeyBindings.RegisterAction(Empowered, () => CheatsCombat.Empowered(""));
            KeyBindings.RegisterAction(FullBuffPlease, () => CheatsCombat.FullBuffPlease(""));
            KeyBindings.RegisterAction(GoddesBuffs, () => CheatsCombat.Iddqd(""));
            KeyBindings.RegisterAction(RemoveBuffs, () => Actions.RemoveAllBuffs());
            //KeyBindings.RegisterAction(RemoveDeathsDoor, () => CheatsCombat.DetachAllBuffs());
            KeyBindings.RegisterAction(KillAllEnemies, () => Actions.KillAll());
            //KeyBindings.RegisterAction(SummonZoo, () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""));
            KeyBindings.RegisterAction(LobotomizeAllEnemies, () => Actions.LobotomizeAllEnemies());
            // Common
            KeyBindings.RegisterAction(TeleportPartyToYou, () => Teleport.TeleportPartyToPlayer());
            KeyBindings.RegisterAction(GoToGlobalMap, () => Teleport.TeleportToGlobalMap());
            KeyBindings.RegisterAction(RerollPerception, () => Actions.RunPerceptionTriggers());
            KeyBindings.RegisterAction(RerollInteractionSkillChecks, () => Actions.RerollInteractionSkillChecks());
            KeyBindings.RegisterAction(ChangeParty, () => { Actions.ChangeParty(); });
            KeyBindings.RegisterAction(ChangWeather, () => CheatsCommon.ChangeWeather(""));
            // Other
            KeyBindings.RegisterAction(TimeScaleMultToggle,
                                       () => {
                                           Settings.useAlternateTimeScaleMultiplier = !Settings.useAlternateTimeScaleMultiplier;
                                           Actions.ApplyTimeScale();
                                       },
                                       title => ToggleTranscriptForState(title, Settings.useAlternateTimeScaleMultiplier)
                );
            KeyBindings.RegisterAction(PreviewDialogResults, () => {
                Settings.previewDialogResults = !Settings.previewDialogResults;
                var dialogController = Game.Instance.DialogController;
            });
            KeyBindings.RegisterAction(ToggleMurderHobo,
                                       () => Settings.togglekillOnEngage = !Settings.togglekillOnEngage,
                                       title => ToggleTranscriptForState(title, Settings.togglekillOnEngage)
                                       );
            KeyBindings.RegisterAction(GoToTradeWindow, () => { Actions.GoToTradeWindow(); });
        }
        public static void ResetGUI() { }

        public static void OnGUI() {
#if BUILD_CRUI
            ActionButton("Demo crUI", () => ModKit.crUI.Demo());
#endif
            if (Main.IsInGame) {
                using (HorizontalScope()) {
                    Space(25);
                    Label("increment".localize().Cyan(), AutoWidth());
                    IntTextField(ref Settings.increment, null, Width(150));
                }
                var increment = Settings.increment;
                var mainChar = Game.Instance.Player.MainCharacter.Entity;
                HStack("Resources".localize(),
                       1,
                       () => {
                           var money = Game.Instance.Player.Money;
                           Label("Gold".localize().Cyan(), Width(150));
                           Label(money.ToString().Orange().Bold(), Width(200));
                           ActionButton("Gain ".localize() + $"{increment}", () => Game.Instance.Player.GainMoney(increment), AutoWidth());
                           ActionButton("Lose ".localize() + $"{increment}",
                                        () => {
                                            var loss = Math.Min(money, increment);
                                            Game.Instance.Player.GainMoney(-loss);
                                        },
                                        AutoWidth());
                       },
                       () => {
                           var exp = ((BaseUnitEntity)mainChar).Progression.Experience;
                           Label("Experience".localize().Cyan(), Width(150));
                           Label(exp.ToString().Orange().Bold(), Width(200));
                           ActionButton("Gain ".localize() + $"{increment}", () => { Game.Instance.Player.GainPartyExperience(increment); }, AutoWidth());
                       },
                       () => { }
                    );
                Div(0, 25);
            }
            HStack("Combat".localize(),
                   2,
                   () => BindableActionButton(RestAll, true),
                   () => BindableActionButton(RestSelected, true),
                   () => BindableActionButton(FullBuffPlease, true),
                   () => BindableActionButton(Empowered, true),
                   () => BindableActionButton(GoddesBuffs, true),
                   () => BindableActionButton(RemoveBuffs, true),
                   () => BindableActionButton(RemoveDeathsDoor, true),
                   () => BindableActionButton(KillAllEnemies, true),
                   () => BindableActionButton(LobotomizeAllEnemies, true),
                   () => { },
                   () => {
                       using (VerticalScope()) {
                           using (HorizontalScope()) {
                               using (VerticalScope(220.width())) {
                                   using (HorizontalScope()) {
                                       Toggle(("Be a " + "Murder".Red().Bold() + " Hobo".Orange()).localize(), ref Settings.togglekillOnEngage, 222.width());
                                       KeyBindPicker(ToggleMurderHobo, "", 50);
                                   }
                               }
                               158.space();
                               Label(("If ticked, this will " + "MURDER".Red().Bold() + " all who dare to engage you!".Green()).localize(), AutoWidth());
                           }
                           using (HorizontalScope()) {
                               if (Toggle("Log ToyBox Keyboard Commands In Game".localize(), ref Mod.ModKitSettings.toggleKeyBindingsOutputToTranscript, 450.width()))
                                   ModKitSettings.Save();
                               50.space();
                               HelpLabel("When ticked this shows ToyBox commands in the combat log which is helpful for you to know when you used the shortcut".localize());
                           }
                       }
                   }
                );
            Div(0, 25);
            HStack("Teleport".localize(),
                   2,
                   () => BindableActionButton(TeleportPartyToYou, true),
                   () => {
                       Toggle("Enable Teleport Keys".localize(), ref Settings.toggleTeleportKeysEnabled);
                       Space(100);
                       if (Settings.toggleTeleportKeysEnabled) {
                           using (VerticalScope()) {
                               KeyBindPicker("TeleportMain", "Main Character".localize(), 0, 200);
                               KeyBindPicker("TeleportSelected", "Selected Chars".localize(), 0, 200);
                               KeyBindPicker("TeleportParty", "Whole Party".localize(), 0, 200);
                           }
                       }
                       Space(25);
                       Label("You can enable hot keys to teleport members of your party to your mouse cursor on Area or the Global Map".localize().Green());
                   });
            Div(0, 25);
            HStack("Common".localize(),
                   2,
                   () => BindableActionButton(GoToGlobalMap, true),
                   () => {
                       BindableActionButton(ChangeParty, true);
                       Space(-75);
                       HelpLabel("Change the party without advancing time (good to bind)".localize());
                   },
                   () => BindableActionButton(RerollPerception, true),
                   () => {
                       BindableActionButton(RerollInteractionSkillChecks, true);
                       Space(-75);
                       Label("This resets all the skill check rolls for all interactable objects in the area".localize().Green());
                   },
                   () => BindableActionButton(ChangWeather, true),
                   () => NonBindableActionButton("Give All Items".localize(), () => CheatsUnlock.CreateAllItems("")),
                   () => NonBindableActionButton("Identify All".localize(), () => Actions.IdentifyAll()),
                   () => BindableActionButton(GoToTradeWindow, true), 
                   () => { }
                );
            Div(0, 25);
            HStack("Preview".localize(),
                   0,
                   () => {
                       Toggle("Dialog Results".localize(), ref Settings.previewDialogResults);
                       25.space();
                       Toggle("Dialog Conditions".localize(), ref Settings.previewDialogConditions);
                       25.space();
                       Toggle("Dialog Alignment".localize(), ref Settings.previewAlignmentRestrictedDialog);
                       BindableActionButton(PreviewDialogResults, true);
                   });
            Div(0, 25);
            HStack("Dialog".localize(),
                   1,
                   () => {
                       Toggle(("♥♥ ".Red() + "Love is Free".Bold() + " ♥♥".Red()).localize(), ref Settings.toggleAllowAnyGenderRomance, 300.width());
                       25.space();
                       Label(("Allow ".Green() + "any gender".Color(RGBA.purple) + " " + "for any ".Green() + "R".Color(RGBA.red) + "o".Orange() + "m".Yellow() + "a".Green() + "n".Cyan() + "c".Color(RGBA.blue) + "e".Color(RGBA.purple)).localize());
                   },
                   () => {
                       Toggle("Jealousy Begone!".localize().Bold(), ref Settings.toggleMultipleRomance, 300.width());
                       25.space();
                       Label(("Allow ".Green() + "multiple".Color(RGBA.purple) + " romances at the same time".Green()).localize());
                   },
                   () => {
                       Toggle("Previously Chosen Dialog Is Smaller ".localize(), ref Settings.toggleMakePreviousAnswersMoreClear, 300.width());
                       200.space();
                       Label("Draws dialog choices that you have previously selected in smaller type".localize().Green());
                   },
                   () => {
                       Toggle("Expand Dialog To Include Remote Companions".localize(), ref Settings.toggleRemoteCompanionDialog, 300.width());
                       200.space();
                       Label(" Allow remote companions to make comments on dialog you are having.".localize().Green());
                   },
                   () => {
                       if (Settings.toggleRemoteCompanionDialog) {
                           50.space();
                           Toggle("Include Former Companions".localize(), ref Settings.toggleExCompanionDialog, 300.width());
                           150.space();
                           Label("This also includes companions who left the party".localize().Green());
                       }
                   },
                   () => {
                       Toggle("Override Story Occupation (e.g. Sanctioned Psyker)".localize(), ref Settings.toggleOverrideOccupation, 300.width());
                   },
                   () => {
                       if (Settings.toggleOverrideOccupation) {
                           if ((Occupations?.Count() ?? 0) == 0) {
                               Occupations = BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintUnitFact>(OccupationIds);
                           }
                           if (Occupations?.Count() > 0) {
                               using (VerticalScope()) {
                                   OccupationBrowser.OnGUI(Occupations, () => Occupations, o => o, o => BlueprintExtensions.GetSearchKey(o), o => [BlueprintExtensions.GetSortKey(o)], null, (o, maybeO) => {
                                       Label(BlueprintExtensions.GetSortKey(o), Width(500));
                                       if (Settings.usedOccupations.Contains(o.AssetGuid)) {
                                           ActionButton("Remove", () => Settings.usedOccupations.Remove(o.AssetGuid));
                                       } else {
                                           ActionButton("Add", () => Settings.usedOccupations.Add(o.AssetGuid));
                                       }
                                   });
                               }
                           }
                       }
                   },
                   () => {
                       // TODO: BROKEN!
                       Settings.toggleShowAnswersForEachConditionalResponse = false;
                       /*
                       using (VerticalScope(300.width())) {
                           Toggle("Expand Answers For Conditional Responses".localize(), ref Settings.toggleShowAnswersForEachConditionalResponse, 300.width());
                       }
                       200.space();
                       Label("Some responses such as comments about your mythic powers will always choose the first one by default. This will show a copy of the answer and the condition for each possible response that an NPC might make to you based on".localize().green());
                       */
                   },
                   () => Toggle("Disable Dialog Restrictions (SoulMark)".localize(), ref Settings.toggleDialogRestrictions),
                   () => {
                       if (Settings.toggleRiskyToggles) {
                           Toggle("Disable Dialog Restrictions (Everything, Experimental)".localize(), ref Settings.toggleDialogRestrictionsEverything);
                       }
                   },
                   () => { }
                );
            Div(0, 25);
            HStack("Quality of Life".localize(),
                   1,
                   () => {
                       Toggle("Allow Achievements While Using Mods".localize(), ref Settings.toggleAllowAchievementsDuringModdedGame, 500.width());
                       Label("This is intended for you to be able to enjoy the game while using mods that enhance your quality of life.  Please be mindful of the player community and avoid using this mod to trivialize earning prestige achievements like Sadistic Gamer.".localize().Orange());
                   },
                   () => {
                       Toggle("Skip Splash Screen".localize(), ref Settings.toggleSkipSplashScreen, 500.width());
                       Label("This skips the splash screen that appears when the game starts. Helpful if you need to frequently restart the game".localize().Green());
                   },
                   () => {
                       if (Toggle("Make DLC1 theme play again in main menu".localize(), ref Settings.toggleDLC1Theme, 500.width())) {
                           if (SoundState.Instance.State == SoundStateType.MainMenu) {
                               SoundState.Instance.MusicStateHandler.SetMusicState(MusicStateHandler.MusicState.MainMenu);
                           }
                       }
                   },
                   () => Toggle("Object Highlight Toggle Mode (Out of Combat!)".localize(), ref Settings.highlightObjectsToggle),
                   () => {
                       Toggle("Mark Interesting NPCs".localize(), ref Settings.toggleShowInterestingNPCsOnLocalMap, 500.width());
                       HelpLabel("This will change the color of NPC names on the highlike makers and change the color map markers to indicate that they have interesting or conditional interactions".localize());
                   },
                   () => {
                       Toggle("Auto load Last Save on launch".localize(), ref Settings.toggleAutomaticallyLoadLastSave, 500.width());
                       HelpLabel("Hold down shift during launch to bypass".localize());
                   },
                   () => {
                       Toggle("Don't wait for keypress when loading saves".localize(), ref Settings.toggleSkipAnyKeyToContinueWhenLoadingSaves, 500.width());
                       HelpLabel("When loading a game this will go right into the game without having to 'Press any key to continue'".localize());
                   },
                   () => Toggle("Refill consumables in belt slots if in inventory".localize(), ref Settings.togglAutoEquipConsumables),
                   () => {
                       var modifier = KeyBindings.GetBinding("InventoryUseModifier");
                       var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                       Toggle("Allow ".localize() + $"{modifierText}".Cyan() + (" + Click".Cyan() + " To Use Items In Inventory").localize(), ref Settings.toggleShiftClickToUseInventorySlot, 470.width());
                       if (Settings.toggleShiftClickToUseInventorySlot) {
                           ModifierPicker("InventoryUseModifier", "", 0);
                       }
                   },
                   () => {
                       var modifier = KeyBindings.GetBinding("ClickToTransferModifier");
                       var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                       Toggle("Allow ".localize() + $"{modifierText}".Cyan() + (" + Click".Cyan() + " To Transfer Entire Stack").localize(), ref Settings.toggleShiftClickToFastTransfer, 470.width());
                       if (Settings.toggleShiftClickToFastTransfer) {
                           ModifierPicker("ClickToTransferModifier", "", 0);
                       }
                   },
                   () => ActionButton("Fix Incorrect Main Character".localize(),
                                      () => {
                                          var probablyPlayer = Game.Instance.Player?.Party?
                                                                   .Where(x => !x.IsCustomCompanion())
                                                                   .Where(x => !x.IsStoryCompanion())
                                                                   .ToList();
                                          if (probablyPlayer is { Count: 1 }) {
                                              var newMainCharacter = probablyPlayer.First();
                                              Mod.Warn($"Promoting {newMainCharacter.CharacterName} to main character!");
                                              if (Game.Instance != null) Game.Instance.Player.MainCharacter = new UnitReference(newMainCharacter);
                                          }
                                      },
                                      AutoWidth()),
                   () => {
                       using (VerticalScope()) {
                           Div(0, 25, 1280);
                           var useAlt = Settings.useAlternateTimeScaleMultiplier;
                           var mainTimeScaleTitle = "Game Time Scale".localize();
                           if (useAlt) mainTimeScaleTitle = mainTimeScaleTitle.Grey();
                           var altTimeScaleTitle = "Alternate Time Scale".localize();
                           if (!useAlt) altTimeScaleTitle = altTimeScaleTitle.Grey();
                           using (HorizontalScope()) {
                               LogSlider(mainTimeScaleTitle, ref Settings.timeScaleMultiplier, 0f, 20, 1, 1, "", Width(450));
                               Space(25);
                               Label("Speeds up or slows down the entire game (movement, animation, everything)".localize().Green());
                           }
                           using (HorizontalScope()) {
                               LogSlider(altTimeScaleTitle, ref Settings.alternateTimeScaleMultiplier, 0f, 20, 5, 1, "", Width(450));
                           }
                           using (HorizontalScope()) {
                               BindableActionButton(TimeScaleMultToggle, true);
                               Space(-95);
                               Label("Bindable hot key to swap between main and alternate time scale multipliers".localize().Green());
                           }
                           Div(0, 25, 1280);
                       }
                   },
                   () => Toggle("Disable end turn HotKey".localize(), ref Settings.disableEndTurnHotkey, 500.width()),
                   () => {
                       Toggle("Enable Loading with Blueprint Errors".localize().Color(RGBA.maroon), ref Settings.enableLoadWithMissingBlueprints);
                       25.space();
                       Label($"This {"incredibly dangerous".Bold()} setting overrides the default behavior of failing to load saves depending on missing blueprint mods. This desperate action can potentially enable you to recover your saved game, though you'll have to respec at minimum.".localize().Orange());
                   },
                   () => {
                       if (Settings.enableLoadWithMissingBlueprints) {
                           Label("To permanently remove these modded blueprint dependencies, load the damaged saved game, change areas, and then save the game. You can then respec any characters that were impacted.".localize().Orange());
                       }
                   },
                   () => { }
                );

            Div(0, 25);
            HStack("RT Specific".localize(),
                   1,
                   () => {
                       using (VerticalScope()) {
                           RogueCheats.OnGUI();
                       }
                   }
                );
            Div(0, 25);
            EnhancedCamera.OnGUI();
            Div(0, 25);
            HStack("Cheats".localize(), 1,
                   () => {
                       Toggle("Prevent Traps from triggering".localize(), ref Settings.disableTraps, 500.width());
                       Label("Enterint a Trap Zone while having Traps disabled will prevent that Trap from triggering even if you deactivate this option in the future".localize().Green());
                   },
                   () => Toggle("Unlimited Stacking of Modifiers (Stat/AC/Hit/Damage/Etc)".localize(), ref Settings.toggleUnlimitedStatModifierStacking),
                   () => {
                       using (HorizontalScope()) {
                           ToggleCallback("Highlight Hidden Objects".localize(), ref Settings.highlightHiddenObjects, Actions.UpdateHighlights);
                           if (Settings.highlightHiddenObjects) {
                               Space(100);
                               ToggleCallback("In Fog Of War ".localize(), ref Settings.highlightHiddenObjectsInFog, Actions.UpdateHighlights);
                           }
                       }
                   },
                   () => Toggle("Infinite Abilities (No cooldowns no cost)".localize(), ref Settings.toggleInfiniteAbilities),
                   () => Toggle("No attack/spell cooldowns".localize(), ref Settings.toggleNoAttackCooldowns),
                   () => {
                       using (HorizontalScope()) {
                           Toggle("Don't use AP (except abilities which consume all AP) During Turn".localize(), ref Settings.toggleUnlimitedActionsPerTurn);
                           if (Settings.toggleUnlimitedActionsPerTurn) {
                               Space(100);
                               Toggle("Don't use any AP during your turn.".localize(), ref Settings.toggleReallyUnlimitedActionsPerTurn);
                           }
                       }
                   },
                   () => Toggle("Infinite Charges On Items".localize(), ref Settings.toggleInfiniteItems),
                   () => Toggle("ignore Equipment Restrictions".localize(), ref Settings.toggleEquipmentRestrictions),
                   () => Toggle("Restore Spells & Skills After Combat".localize(), ref Settings.toggleRestoreSpellsAbilitiesAfterCombat),
                   () => Toggle("Instant Rest After Combat".localize(), ref Settings.toggleInstantRestAfterCombat),
                () => Toggle("Allow Equipment Change During Combat".localize(), ref Settings.toggleEquipItemsDuringCombat),
                () => Toggle("Allow Item Use From Inventory During Combat".localize(), ref Settings.toggleUseItemsDuringCombat),
                () => Toggle("Ignore all Requirements for Abilities".localize(), ref Settings.toggleIgnoreAbilityAnyRestriction),
                () => Toggle("Ignore Ability Requirement - AOE Overlap".localize(), ref Settings.toggleIgnoreAbilityAoeOverlap),
                () => Toggle("Ignore Ability Requirement - Line of Sight".localize(), ref Settings.toggleIgnoreAbilityLineOfSight),
                () => Toggle("Ignore Ability Requirement - Max Range".localize(), ref Settings.toggleIgnoreAbilityTargetTooFar),
                () => Toggle("Ignore Ability Requirement - Min Range".localize(), ref Settings.toggleIgnoreAbilityTargetTooClose),
                () => { }
                );
            Div(0, 25);
            HStack("Experience Multipliers".localize(), 1,
                () => LogSlider("All Experience".localize(), ref Settings.experienceMultiplier, 0f, 100f, 1, 1, "", AutoWidth()),
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Combat".localize(), ref Settings.useCombatExpSlider, Width(275));
                        if (Settings.useCombatExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref Settings.experienceMultiplierCombat, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Quests".localize(), ref Settings.useQuestsExpSlider, Width(275));
                        if (Settings.useQuestsExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref Settings.experienceMultiplierQuests, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Skill Checks".localize(), ref Settings.useSkillChecksExpSlider, Width(275));
                        if (Settings.useSkillChecksExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref Settings.experienceMultiplierSkillChecks, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Challenges".localize(), ref Settings.useChallengesExpSlider, Width(275));
                        if (Settings.useChallengesExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref Settings.experienceMultiplierChallenges, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Space Combat".localize(), ref Settings.useSpaceExpSlider, Width(275));
                        if (Settings.useSpaceExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref Settings.experienceMultiplierSpace, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                }
                );
            Div(0, 25);
            HStack("Other Multipliers".localize(), 1,
                () => {
                    LogSlider("Vision Range".localize(), ref Settings.fowMultiplier, 0f, 100f, 1, 1, "", AutoWidth());
                    List<BaseUnitEntity> units = Game.Instance?.Player?.m_PartyAndPets;
                    if (units != null) {
                        foreach (var unit in units) {
                            // TODO: do we need this for RT?
                            // TODO: who knows?
                            FogOfWarRevealerSettings revealer = unit.View?.FogOfWarRevealer;
                            if (revealer != null) {
                                if (Settings.fowMultiplier == 1) {
                                    revealer.DefaultRadius = true;
                                    revealer.Radius = 1.0f;
                                } else {
                                    revealer.DefaultRadius = false;
                                    // TODO: is this right?
                                    revealer.Radius = Settings.fowMultiplier;
                                }
                            }
                        }
                    }
                },
                () => {
                    LogSlider("Max walk distance".localize(), ref Settings.walkRangeMultiplier, 0, 100, 1, 1, "", Width(600));
                    Space(25);
                    Label("Adjusts how far of your character you can click and still cause your character to walk instead of run".localize().Green());
                },
                () => {
                    LogSlider("Min sprint distance".localize(), ref Settings.sprintRangeMultiplier, 0, 100, 1, 1, "", Width(600));
                    Space(25);
                    Label("Adjusts how far of your character you have to click and still cause your character to spring. If this area overlaps with walk distance then this has priority.".localize().Green());
                },
                () => {
                    LogSlider("Movement Speed".localize(), ref Settings.partyMovementSpeedMultiplier, 0f, 20, 1, 1, "", Width(600));
                    Space(25);
                    Label("Adjusts the movement speed of your party in area maps".localize().Green());
                },
                () => LogSlider("Buff Duration".localize(), ref Settings.buffDurationMultiplierValue, 0f, 9999, 1, 1, "", AutoWidth()),
                () => DisclosureToggle("Exceptions to Buff Duration Multiplier (Advanced; will cause blueprints to load)".localize(), ref showBuffDurationExceptions),
                () => {
                    if (!showBuffDurationExceptions) return;

                    BuffExclusionEditor.OnGUI();
                },
                () => { }
                );
            Actions.ApplyTimeScale();
            Div(0, 25);
            DiceRollsGUI.OnGUI();
            Div(0, 25);
            HStack("Enemy Stat Multipliers".localize(), 1,
                () => Toggle("Enable Multipliers (e.g. Enemy Health x2)".localize(), ref Main.Settings.toggleAddMultiplierEnemyMods),
                () => {
                    if (Main.Settings.toggleAddMultiplierEnemyMods) {
                        using (VerticalScope()) {
                            foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                                Slider(Enum.GetName(typeof(StatType), stat), () => Settings.multiplierEnemyMods[stat], newStat => Settings.multiplierEnemyMods[stat] = newStat, -100, 100, 1, 0);
                            }
                        }
                    }
                }
                );
            Div(0, 25);
            HStack("Flat Enemy Stat Boosts".localize(), 1,
                () => Toggle("Enable Flat Boosts (e.g. Enemy Health +20)".localize(), ref Main.Settings.toggleAddFlatEnemyMods),
                () => {
                    if (Main.Settings.toggleAddFlatEnemyMods) {
                        using (VerticalScope()) {
                            foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                                Slider(Enum.GetName(typeof(StatType), stat), () => Settings.flatEnemyMods[stat], newStat => Settings.flatEnemyMods[stat] = newStat, -100, 100, 0, 0);
                            }
                        }
                    }
                }
                );
        }
    }
}
