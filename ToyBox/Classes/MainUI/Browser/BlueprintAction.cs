﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public abstract class BlueprintAction {
        public delegate void Perform(SimpleBlueprint bp, BaseUnitEntity? ch = null, int count = 1, int listValue = 0);

        public delegate bool CanPerform(SimpleBlueprint bp, BaseUnitEntity? ch = null, int listValue = 0);

        private static Dictionary<Type, BlueprintAction[]> actionsForType;

        public static BlueprintAction[] ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, BlueprintAction[]>();
                BlueprintActions.InitializeActions();
                BlueprintActions.InitializeActionsRT();
            }

            actionsForType.TryGetValue(type, out var result);

            if (result == null) {
                var baseType = type.BaseType;

                if (baseType != null) {
                    result = ActionsForType(baseType);
                }

                result ??= new BlueprintAction[] { };

                actionsForType[type] = result;
            }

            return result;
        }

        public static IEnumerable<BlueprintAction> ActionsForBlueprint(SimpleBlueprint bp) => ActionsForType(bp.GetType());
        public static void Register<T>(string? name, BlueprintAction<T>.Perform perform, BlueprintAction<T>.CanPerform? canPerform = null, bool isRepeatable = false, bool worksInMainMenu = false) where T : SimpleBlueprint {
            var action = new BlueprintAction<T>(name, perform, canPerform, isRepeatable, worksInMainMenu);
            var type = action.BlueprintType;
            actionsForType.TryGetValue(type, out var existing);
            existing ??= new BlueprintAction[] { };
            var list = existing.ToList();
            list.Add(action);
            actionsForType[type] = list.ToArray();
        }

        public string? name { get; protected set; }

        public Perform action;

        public CanPerform canPerform;

        protected BlueprintAction(string? name, bool isRepeatable) {
            this.name = name;
            this.isRepeatable = isRepeatable;
        }

        public bool isRepeatable;

        public abstract Type BlueprintType { get; }
    }

    public class BlueprintAction<BPType> : BlueprintAction where BPType : SimpleBlueprint {
        public new delegate void Perform(BPType bp, BaseUnitEntity ch, int count = 1, int listValue = 0);

        public new delegate bool CanPerform(BPType bp, BaseUnitEntity ch, int listValue = 0);

        public BlueprintAction(string? name, Perform action, CanPerform? canPerform = null, bool isRepeatable = false, bool worksInMainMenu = false) : base(name, isRepeatable) {
            this.action = (bp, ch, n, index) => action((BPType)bp, ch, n, index);
            this.canPerform = (bp, ch, index) => {
                try {
                    return (worksInMainMenu || Main.IsInGame) && bp is BPType bpt && (canPerform?.Invoke(bpt, ch, index) ?? true);
                } catch (Exception) {
                    return false;
                }
            };
        }

        public override Type BlueprintType => typeof(BPType);
    }

    public static partial class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this SimpleBlueprint bp) => BlueprintAction.ActionsForBlueprint(bp);

        private static Dictionary<BlueprintFeatureSelection_Obsolete, BlueprintFeature[]> featureSelectionItems = new();
        public static BlueprintFeature FeatureSelectionItems(this BlueprintFeatureSelection_Obsolete feature, int index) {
            if (featureSelectionItems.TryGetValue(feature, out var value)) return index < value.Length ? value[index] : null;
            value = feature.AllFeatures.OrderBy(x => x.NameSafe()).ToArray();
            if (value == null) return null;
            featureSelectionItems[feature] = value;
            return index < value.Length ? value[index] : null;
        }

        public static void InitializeActions() {
            BlueprintAction.Register<BlueprintAreaPreset>("Load Preset".localize(), (bp, ch, n, l) => {
                CheatsTransfer.StartNewGame(bp);
            }, null, false, true);
            BlueprintAction.Register<BlueprintItem>("Add".localize(),
                                                    (bp, ch, n, index) => {
                                                        Game.Instance.Player.Inventory.Add(bp, n);
                                                        OwlLogging.Log($"Add Item {n} x {bp}");
                                                    }, isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove".localize(),
                                                    (bp, ch, n, index) => {
                                                        Game.Instance.Player.Inventory.Remove(bp, n);
                                                        OwlLogging.Log($"Remove Item {n} x {bp}");
                                                    },
                                                    (bp, ch, index) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn".localize(),
                                                    (bp, ch, n, index) => {
                                                        Actions.SpawnUnit(bp, n);
                                                        OwlLogging.Log($"Spawn {n} x {bp}");
                                                    }, isRepeatable: true);

            // Facts
            BlueprintAction.Register<BlueprintMechanicEntityFact>("Add".localize(),
                                                       (bp, ch, n, index) => { 
                                                           ch.AddFact(bp);
                                                           OwlLogging.Log($"Add MechanicEntityFact {bp} to {ch}");
                                                       },
                                                       (bp, ch, index) => !ch.Facts.List.Select(f => f.Blueprint).Contains(bp));

            BlueprintAction.Register<BlueprintMechanicEntityFact>("Remove".localize(),
                                                        (bp, ch, n, index) => {
                                                            ch.Facts.Remove(bp);
                                                            OwlLogging.Log($"Remove MechanicEntityFact {bp} from {ch}");
                                                        },
                                                        (bp, ch, index) => ch.Facts.List.Select(f => f.Blueprint).Contains(bp));

            //BlueprintAction.Register<BlueprintArchetype>(
            //    "Add",
            //    (bp, ch, n, index) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, index) => ch.Progression.CanAddArchetype(ch.Progression.Classes.First().CharacterClass, bp)
            //    );
            //BlueprintAction.Register<BlueprintArchetype>("Remove",
            //    (bp, ch, n, index) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, index) => ch.Progression.Classes.First().Archetypes.Contains(bp)
            //    );

            // Teleport
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport".localize(), (enterPoint, ch, n, index) => {
                Teleport.To(enterPoint);
                OwlLogging.Log($"Teleport to {enterPoint}");
            });
            BlueprintAction.Register<BlueprintArea>("Teleport".localize(), (area, ch, n, index) => {
                Teleport.To(area);
                OwlLogging.Log($"Teleport to {area}");
            });

            // Quests
            BlueprintAction.Register<BlueprintQuest>("Start".localize(),
                                                     (bp, ch, n, index) => {
                                                         Game.Instance.Player.QuestBook.GiveObjective(bp.Objectives.First());
                                                         OwlLogging.Log($"Start Quest {bp} by giving first objective {bp.Objectives.First()}");
                                                     },
                                                     (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp) == null);

            BlueprintAction.Register<BlueprintQuest>("Complete".localize(),
                                                     (bp, ch, n, index) => {
                                                         OwlLogging.Log($"Complete Quest {bp} by completing all the following objectives");
                                                         foreach (var objective in bp.Objectives) {
                                                             Game.Instance.Player.QuestBook.CompleteObjective(objective);
                                                             OwlLogging.Log($"Complete Quest Objective {objective}");
                                                         }
                                                     }, (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp)?.State == QuestState.Started);

            // Quests Objectives
            BlueprintAction.Register<BlueprintQuestObjective>("Start".localize(),
                                                              (bp, ch, n, index) => {
                                                                  Game.Instance.Player.QuestBook.GiveObjective(bp);
                                                                  OwlLogging.Log($"Start Quest Objective {bp}");
                                                              },
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest) == null);

            BlueprintAction.Register<BlueprintQuestObjective>("Complete".localize(),
                                                              (bp, ch, n, index) => {
                                                                  Game.Instance.Player.QuestBook.CompleteObjective(bp);
                                                                  OwlLogging.Log($"Complete Quest Objective {bp}");
                                                              },
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest)?.State == QuestState.Started);

            // Etudes
            BlueprintAction.Register<BlueprintEtude>("Start".localize(),
                                                     (bp, ch, n, index) => {
                                                         Game.Instance.Player.EtudesSystem.StartEtude(bp);
                                                         OwlLogging.Log($"Start Etude {bp}");
                                                     },
                                                     (bp, ch, index) => Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            BlueprintAction.Register<BlueprintEtude>("Complete".localize(),
                                                     (bp, ch, n, index) => {
                                                         Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(bp);
                                                         OwlLogging.Log($"Complete Etude {bp}");
                                                     },
                                                     (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp) &&
                                                                        !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(bp));
            BlueprintAction.Register<BlueprintEtude>("Unstart".localize(),
                                         (bp, ch, n, index) => {
                                             Game.Instance.Player.EtudesSystem.UnstartEtude(bp);
                                             OwlLogging.Log($"Unstart Etude {bp}");
                                         },
                                         (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            // Flags
            BlueprintAction.Register<BlueprintUnlockableFlag>("Unlock".localize(),
                (bp, ch, n, index) => {
                    bp.Unlock();
                    OwlLogging.Log($"Unlock UnlockableFlag {bp}");
                },
                (bp, ch, index) => !bp.IsUnlocked);

            BlueprintAction.Register<BlueprintUnlockableFlag>("Lock".localize(),
                (bp, ch, n, index) => {
                    bp.Lock();
                    OwlLogging.Log($"Lock UnlockableFlag {bp}"); 
                },
                (bp, ch, index) => bp.IsUnlocked);

            BlueprintAction.Register<BlueprintUnlockableFlag>(">".localize(),
                (bp, ch, n, index) => {
                    bp.Value = bp.Value + n;
                    OwlLogging.Log($"Increase UnlockableFlag {bp} by {n}");
                },
                (bp, ch, index) => bp.IsUnlocked);

            BlueprintAction.Register<BlueprintUnlockableFlag>("<".localize(),
                (bp, ch, n, index) => {
                    bp.Value = bp.Value - n;
                    OwlLogging.Log($"Decrease UnlockableFlag {bp} by {n}");
                },
                (bp, ch, index) => bp.IsUnlocked);
            // Cutscenes
            BlueprintAction.Register<Cutscene>("Play".localize(), (bp, ch, n, index) => {
                OwlLogging.Log($"Play cutscene {bp}");
                Actions.ToggleModWindow();
                var cutscenePlayerData = CutscenePlayerData.Queue.FirstOrDefault(c => c.PlayActionId == bp.name);

                if (cutscenePlayerData != null) {
                    cutscenePlayerData.PreventDestruction = true;
                    cutscenePlayerData.Stop();
                    cutscenePlayerData.PreventDestruction = false;
                }
                SceneEntitiesState state = null; // TODO: do we need this?
                CutscenePlayerView.Play(bp, null, true, state).PlayerData.PlayActionId = bp.name;
            });
        }
    }
}