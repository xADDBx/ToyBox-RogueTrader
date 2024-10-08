﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.View;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using ModKit;
//using Owlcat.Runtime.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
namespace ToyBox {
    public static partial class Actions {
        public static Settings Settings => Main.Settings;
        public static void KillAll() {
            foreach (BaseUnitEntity unit in Shodan.AllBaseUnits) {
                if (unit.CombatState.IsInCombat && Shodan.IsEnemy(unit)) {
                    Shodan.KillUnit(unit);
                }
            }
            if (Game.Instance.IsPaused) {
                Game.Instance.StopMode(GameModeType.Pause);
            }
            if (!Game.Instance.IsPaused)
                return;
            Game.Instance.StopMode(GameModeType.Pause);
        }
        public static void RemoveAllBuffs() {
            foreach (var target in Game.Instance.Player.PartyAndPets) {
                foreach (var buff in new List<Buff>(target.Descriptor().Buffs.Enumerable)) {
                    if (buff.Blueprint.IsHiddenInUI) {
                        continue;
                    }

                    if (buff.Blueprint.StayOnDeath) { // Not a spell and persists through death, generally seems to be items
                        continue;
                    }

                    target.Descriptor().Facts.Remove(buff);
                }
            }
        }
        public static void LobotomizeAllEnemies() {
            foreach (var unit in Shodan.AllBaseUnits) {
                if (unit.CombatState.IsInCombat && Shodan.IsEnemy(unit)) {
                    // removing the brain works better in RTWP, but gets stuck in turn based
                    //AccessTools.DeclaredProperty(descriptor.GetType(), "Brain")?.SetValue(descriptor, null);
                    // add a bunch of conditions and hope for the best
                    //var currentCharacter = WrathExtensions.GetCurrentCharacter();
                    var fact = new EntityFact();
                    unit.State.AddCondition(Kingmaker.UnitLogic.Enums.UnitCondition.DisableAttacksOfOpportunity, fact);
                    unit.State.AddCondition(Kingmaker.UnitLogic.Enums.UnitCondition.CantAct, fact);
                    unit.State.AddCondition(Kingmaker.UnitLogic.Enums.UnitCondition.CantMove, fact);
                }
            }
        }

        public static void MaximizeModWindow() {
            var modUI = BagOfPatches.ModUI.UnityModManagerUIPatch.UnityModMangerUI;
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            modUI.mWindowSize = new Vector2(screenWidth, screenHeight);
            modUI.mWindowSize = modUI.ClampWindowSize(modUI.mWindowSize);
            modUI.mExpectedWindowSize = modUI.mWindowSize;
            modUI.mWindowRect = new Rect(
                    (screenWidth - modUI.mWindowSize.x) / 2.0f,
                    (screenHeight - modUI.mWindowSize.y) / 2.0f,
                    0.0f,
                    0.0f
                );
            var newScale = screenWidth switch {
                >= 3840 => 1.8f,
                >= 2560 => 1.5f,
                >= 1920 => 1.25f,
                _ => 1.0f
            };
            modUI.mUIScale = newScale;
            modUI.mExpectedUIScale = newScale;
            modUI.mUIScaleChanged = true;
            UnityModManager.Params.WindowWidth = modUI.mWindowSize.x;
            UnityModManager.Params.WindowHeight = modUI.mWindowSize.y;
            UnityModManager.Params.UIScale = newScale;
            UnityModManager.SaveSettingsAndParams();
        }
        public static void ToggleModWindow() => UnityModManager.UI.Instance.ToggleWindow();
        public static void IdentifyAll() {
            var inventory = Game.Instance?.Player?.Inventory;
            if (inventory == null) return;
            foreach (var item in inventory) {
                item.Identify();
            }
            foreach (var ch in Game.Instance.Player.AllCharacters) {
                foreach (var item in ch.Body.GetAllItemsInternal()) {
                    item.Identify();
                    //Main.Log($"{ch.CharacterName} - {item.Name} - {item.IsIdentified}");
                }
            }
        }
    }
}
