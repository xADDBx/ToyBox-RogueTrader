// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
using HarmonyLib;
using Kingmaker;
using Kingmaker.AI.BehaviourTrees.Nodes;
using Kingmaker.GameInfo;
using Kingmaker.GameModes;
using Kingmaker.Localization;
using Kingmaker.UI.Common;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.LifeEvents;
using Kingmaker.UI.Models.Log.Enums;
using Kingmaker.UI.Models.Log.GameLogCntxt;
using ModKit;
using ModKit.DataViewer;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Threading;
using ToyBox.classes.Infrastructure;
using ToyBox.classes.MainUI;
using ToyBox.classes.MonkeyPatchin;
using ToyBox.PatchTool;
using UniRx;
using UnityEngine;
using UnityModManagerNet;
using LocalizationManager = ModKit.LocalizationManager;

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    internal static class Main {
        private static Thread UnityThread;
        public static bool IsOnUnityThread => Thread.CurrentThread == UnityThread;
        internal const string LinkToIncompatibilitiesFile = "https://raw.githubusercontent.com/xADDBx/ToyBox-RogueTrader/main/ToyBox/ModDetails/Incompatibilities.json";
        internal static Harmony HarmonyInstance;
        internal static UnityModManager.ModEntry ModEntry;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        public static Settings Settings;
        public static NamedAction[] tabs = {
                    new NamedAction("Bag of Tricks", BagOfTricks.OnGUI),
                    new NamedAction("Enhanced UI", EnhancedUI.OnGUI),
                    new NamedAction("Level Up", LevelUp.OnGUI),
                    new NamedAction("Party", PartyEditor.OnGUI),
                    new NamedAction("Loot", PhatLoot.OnGUI),
#if false
                    new NamedAction("Enchantment", EnchantmentEditor.OnGUI),
                    new NamedAction("Playground", () => Playground.OnGUI()),
#endif
                    new NamedAction("Search 'n Pick", SearchAndPick.OnGUI),
                    new NamedAction("Colonies", ColonyEditor.OnGUI),
                    new NamedAction("Etudes", EtudesEditor.OnGUI),
                    new NamedAction("Quests", QuestEditor.OnGUI),
                    new NamedAction("Dialog & NPCs", DialogAndNPCs.OnGUI),
                    new NamedAction("Saves", GameSavesBrowser.OnGUI),
                    new NamedAction("Achievements", AchievementsUnlocker.OnGUI),
                    new NamedAction("Patch Tool", PatchToolUIManager.OnGUI),
                    new NamedAction("Settings", SettingsUI.OnGUI)
        };
        private static int partyTabID = -1;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool NeedsActionInit = true;
        private static bool _needsResetGameUI = false;
        private static bool _resetRequested = false;
        private static DateTime _resetRequestTime = DateTime.Now;
        public static bool resetExtraCameraAngles = false;
        internal static string path;
        public static void SetNeedsResetGameUI() {
            _resetRequested = true;
            _resetRequestTime = DateTime.Now;
            Mod.Debug($"resetRequested - {_resetRequestTime}");
        }
        public static bool IsInGame => Game.Instance.Player?.Party.Any() ?? false;
        private static Exception _caughtException = null;

        public static List<GameObject> Objects;
        private static void FindPatchesWithoutGuardClause() {
            return;
            foreach (var type in typeof(Main).Assembly.GetTypes()) {
                if (type.GetCustomAttributes<HarmonyPatch>().FirstOrDefault() != null) {
                    foreach (var method in type.GetMethods()) {
                        System.Attribute CustomAttribute = method.GetCustomAttributes<HarmonyPrefix>().FirstOrDefault();
                        CustomAttribute ??= method.GetCustomAttributes<HarmonyPostfix>().FirstOrDefault();
                        CustomAttribute ??= method.GetCustomAttributes<HarmonyFinalizer>().FirstOrDefault();
                        CustomAttribute ??= method.GetCustomAttributes<HarmonyTranspiler>().FirstOrDefault();
                        List<string> names = ["Prefix", "Postfix", "Transpiler", "Finalizer"];
                        if (CustomAttribute != null || names.Contains(method.Name)) {
                            var foundSettings = false;
                            foreach (var inst in PatchProcessor.GetCurrentInstructions(method)) {
                                if (inst.opcode == OpCodes.Ldfld
                                    && (inst.operand is FieldInfo field && field.DeclaringType == typeof(Settings))) {
                                    foundSettings = true;
                                    break;
                                }
                            }
                            if (!foundSettings) {
                                Mod.Log($"Found unconditional patch method: {type.Name}.{method.Name}");
                            }
                        }
                    }
                }
            }
        }
        private static bool Load(UnityModManager.ModEntry modEntry) {
            try {
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

                if (Settings.toggleIntegrityCheck) {
                    modEntry.Logger.Log("Starting Integrity Check.");
                    if (IntegrityChecker.Check(modEntry.Logger)) {
                        modEntry.Logger.Log("Integrity Check succeeded.");
                    } else {
                        modEntry.Info.DisplayName = "ToyBox" + " Checksum verification failed!".localize().Yellow().Bold().SizePercent(80) + "\nMod files are likely corrupted...".localize().Yellow().Bold().SizePercent(50);
                        if (Settings.updateOnChecksumFail) {
                            if (Updater.Update(modEntry, true, true)) {
                                modEntry.Info.DisplayName = "ToyBox" + " Restart the game to finish the update!".localize().Green().Bold().SizePercent(80);
                                return false;
                            }
                        }
                        if (Settings.disableOnChecksumFail) {
                            modEntry.Info.DisplayName = "ToyBox" + " Checksum verification failed!".localize().Red().Bold().SizePercent(80);
                            return false;
                        }
                    }
                }

                if (Settings.toggleVersionCompatability) {
                    if (VersionChecker.IsGameVersionSupported(modEntry.Version, modEntry.Logger, LinkToIncompatibilitiesFile)) {
                        modEntry.Logger.Log("Compatability Check succeeded");
                    } else {
                        modEntry.Logger.Log("Fatal! The current Game Version has known incompatabilities with your current ToyBox version! Please Update.");
                        if (Settings.shouldTryUpdate) {
                            modEntry.Info.DisplayName = "ToyBox" + " Trying to update the mod...".localize().Red().Bold().SizePercent(80);
                            if (Updater.Update(modEntry, true)) {
                                modEntry.Info.DisplayName = "ToyBox" + " Restart the game to finish the update!".localize().Green().Bold().SizePercent(80);
                                return false;
                            }
                        }
                        modEntry.Info.DisplayName = "ToyBox" + " Update the mod manually!".localize().Red().Bold().SizePercent(100);
                        return false;
                    }
                }

                if (Settings.toggleAlwaysUpdate) {
                    modEntry.Logger.Log("Auto Updater enabled, trying to update...");
                    if (Updater.Update(modEntry)) {
                        modEntry.Info.DisplayName = "ToyBox" + " Restart the game to finish the update!".localize().Green().Bold().SizePercent(40);
                    }
                }
                Main.ModEntry = modEntry;
#if DEBUG
                modEntry.OnUnload = OnUnload;
                _modId = modEntry.Info.Id;
#endif

                Mod.OnLoad(modEntry);
                path = modEntry.Path;
                SettingsDefaults.InitializeDefaultDamageTypes();
                EventBus.Subscribe(new HighlightObjectToggle());
                _ = SharedStringAssetPool.Instance;

                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                LocalizationManager.Enable();

                modEntry.OnToggle = OnToggle;
                modEntry.OnShowGUI = OnShowGUI;
                modEntry.OnHideGUI = OnHideGUI;
                modEntry.OnGUI = OnGUI;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGUI;
                Objects = new List<GameObject>();
                KeyBindings.OnLoad(modEntry);
                HumanFriendlyStats.EnsureFriendlyTypesContainAll();
                Mod.logLevel = Settings.loggingLevel;
                Mod.InGameTranscriptLogger = text => {
                    Mod.Log("CombatLog - " + text);
                    var messageText = "ToyBox".Blue() + " - " + text;
                    var message = new CombatLogMessage(messageText, Color.black, PrefixIcon.RightArrow);
                    var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Dialog].FirstOrDefault(x => x is DialogLogThread);
                    using (GameLogContext.Scope) {
                        messageLog?.AddMessage(message);
                    }
                };
                RogueCheats.PatchPsychicTranspiler(!Settings.customizePsychicPhenomena);
                FindPatchesWithoutGuardClause();
            } catch (Exception e) {
                Mod.Error(e.ToString());
                HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
                throw;
            }
            return true;
        }
#if DEBUG
        internal static string _modId;
        private static bool OnUnload(UnityModManager.ModEntry modEntry) {
            foreach (var obj in Objects) {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            BlueprintExtensions.ResetCollationCache();
            HarmonyInstance.UnpatchAll(_modId);
            EnhancedInventory.OnUnload();
            NeedsActionInit = true;
            return true;
        }
#endif
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            Enabled = value;
            return true;
        }

        private static void ResetGUI(UnityModManager.ModEntry modEntry) {
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Settings.searchText = "";
            Settings.searchLimit = 100;
            Mod.ModKitSettings.browserSearchLimit = 25;
            ModKitSettings.Save();
            BagOfTricks.ResetGUI();
            EnhancedCamera.ResetGUI();
            LevelUp.ResetGUI();
            PartyEditor.ResetGUI();
            CharacterPicker.ResetGUI();
            SearchAndPick.ResetGUI();
            QuestEditor.ResetGUI();
            BlueprintExtensions.ResetCollationCache();
            _caughtException = null;
        }
        private static bool IsFirstOnGUI = true;
        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            if (IsFirstOnGUI) {
                UnityThread = Thread.CurrentThread;
                IsFirstOnGUI = false;
                Glyphs.CheckGlyphSupport();
            }
            if (!Enabled) return;
            IsModGUIShown = true;
            if (Settings.hasSeenUpdatePage) {
                if (!IsInGame) {
                    Label("ToyBox has limited functionality from the main menu".localize().Yellow().Bold());
                }
                if (!IsWide) {
                    using (HorizontalScope()) {
                        ActionButton("Maximize Window".localize(), Actions.MaximizeModWindow);
                        Label(("Note ".Magenta().Bold() + "ToyBox was designed to offer the best user experience at widths of 1920 or higher. Please consider increasing your resolution up of at least 1920x1080 (ideally 4k) and go to Unity Mod Manager 'Settings' tab to change the mod window width to at least 1920.  Increasing the UI scale is nice too when running at 4k".Orange().Bold()).localize());
                    }
                }
                try {
                    var e = Event.current;
                    userHasHitReturn = e.keyCode == KeyCode.Return;
                    focusedControlName = GUI.GetNameOfFocusedControl();
                    if (_caughtException != null) {
                        Label("ERROR".Red().Bold() + $": caught exception {_caughtException}");
                        ActionButton("Reset".Orange().Bold(), () => { ResetGUI(modEntry); }, AutoWidth());
                        return;
                    }
#if false
                using (UI.HorizontalScope()) {
                    UI.Label("Suggestions or issues click ".green(), UI.AutoWidth());
                    UI.LinkButton("here", "https://github.com/cabarius/ToyBox/issues");
                    UI.Space(50);
                    UI.Label("Chat with the Authors, Narria et all on the ".green(), UI.AutoWidth());
                    UI.LinkButton("WoTR Discord", "https://discord.gg/wotr");
                }
#endif
                    TabBar(ref Settings.selectedTab,
                        () => {
                            if (BlueprintLoader.Shared.IsLoading) {
                                Label("Blueprints".Orange().Bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").Cyan().Bold());
                            } else Space(25);
                        },
                        (oldTab, newTab) => {
                            if (partyTabID == -1) {
                                for (int i = 0; i < tabs.Length; i++) {
                                    if (tabs[i].action == PartyEditor.OnGUI) {
                                        partyTabID = i;
                                        break;
                                    }
                                }
                            }
                            if (partyTabID != -1) {
                                if (oldTab == partyTabID) {
                                    PartyEditor.UnloadPortraits();
                                }
                            }
                        },
                        s => s.localize(),
                        tabs
                        );
                } catch (Exception e) {
                    Console.Write($"{e}");
                    _caughtException = e;
                    ReflectionSearch.Shared.Stop();
                }
            } else {
                Label("This mod will automatically conntect to the internet for various tasks. Here are the respective options (in the future found in the Settings tab).".localize().Green().Bold(), AutoWidth());
                SettingsUI.UpdateAndVerificationGUI();
                Label("");
                bool shouldChange = false;
                Button("I understand".localize().Green().Bold(), ref shouldChange);
                Settings.hasSeenUpdatePage = shouldChange;
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            Settings.Save(modEntry);
            ModKitSettings.Save();
        }
        private static void OnShowGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = true;
            EnchantmentEditor.OnShowGUI();
            AchievementsUnlocker.OnShowGUI();
            EtudesEditor.OnShowGUI();
            Mod.OnShowGUI();
        }

        private static void OnHideGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = false;
            PartyEditor.UnloadPortraits();
            BlueprintExtensions.descriptionCache.Clear();
            BlueprintExtensions.titleCache.Clear();
            BlueprintExtensions.sortKeyCache.Clear();
            BlueprintExtensions.searchKeyCache.Clear();
            OwlLogging.OnHideGUI();
        }
        private static IEnumerator ResetGUI() {
            _needsResetGameUI = false;
            Game.ResetUI();
            Mod.InGameTranscriptLogger?.Invoke("ResetUI");
            yield return null;
        }
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
            if (Game.Instance?.Player != null) {
            }
            Mod.logLevel = Settings.loggingLevel;
            if (NeedsActionInit) {
                EnhancedCamera.OnLoad();
                BagOfTricks.OnLoad();
                PhatLoot.OnLoad();
                EnhancedInventory.OnLoad();
                NeedsActionInit = false;
            }
            //if (resetExtraCameraAngles) {
            //    Game.Instance.UI.GetCameraRig().TickRotate(); // Kludge - TODO: do something better...
            //}
            if (_resetRequested) {
                var timeSinceRequest = DateTime.Now.Subtract(_resetRequestTime).TotalMilliseconds;
                //Main.Log($"timeSinceRequest - {timeSinceRequest}");
                if (timeSinceRequest > 1000) {
                    Mod.Debug($"resetExecuted - {timeSinceRequest}".Cyan());
                    _needsResetGameUI = true;
                    _resetRequested = false;
                }
            }
            if (_needsResetGameUI) {
                MainThreadDispatcher.StartCoroutine(ResetGUI());
            }
            var currentMode = Game.Instance.CurrentMode;
            if (IsModGUIShown || Event.current == null || !Event.current.isKey) return;
            KeyBindings.OnUpdate();
            if (IsInGame
                && Settings.toggleTeleportKeysEnabled
                && (currentMode == GameModeType.Default
                    || currentMode == GameModeType.Pause
                    || currentMode == GameModeType.GlobalMap
                    )
                ) {
                if (KeyBindings.IsActive("TeleportMain"))
                    Teleport.TeleportUnit(Shodan.MainCharacter, Utils.PointerPosition());
                if (KeyBindings.IsActive("TeleportSelected"))
                    Teleport.TeleportSelected();
                if (KeyBindings.IsActive("TeleportParty"))
                    Teleport.TeleportParty();
            }
        }
    }
}