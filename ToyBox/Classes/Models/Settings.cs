// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using ModKit;
using ModKit.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox {
    public class PerSaveSettings : EntityPart {
        internal const string ID = "ToyBox.PerSaveSettings";
        internal delegate void Changed(PerSaveSettings perSave);
        [JsonIgnore]
        internal static Changed observers;

        // schema for storing multiclass settings
        //      Dictionary<CharacterName, 
        //          Dictionary<ClassID, HashSet<ArchetypeIDs>

        // Dictionary<Character Hashcode, Dictionary<bodyPartName, universalParameter>>
        [JsonProperty]
        public Dictionary<string, Dictionary<string, float>> characterSkeletonReplacers = new();
        // This is the scaling modifier which is applied to the visual model of each character
        [JsonProperty]
        public Dictionary<string, float> characterModelSizeMultiplier = new();
        // Dictionary<Character Hashcode,
        //              { doOverride, OverrideValue }
        [JsonProperty]
        public Dictionary<string, Tuple<bool, bool>> doOverrideEnableAiForCompanions = new();
        // Dictioanry<Character Hashcode,
        //              Kingmaker.Enums.Size which will override default size
        [JsonProperty]
        public Dictionary<string, Size> characterSizeModifier = new();
    }

    public class Settings : UnityModManager.ModSettings {
        private static PerSaveSettings cachedPerSave = null;
        internal const string PerSaveKey = "ToyBox";
        public static void ClearCachedPerSave() => cachedPerSave = null;
        public static void SetPerSaveSettings(InGameSettings settings) => ReloadPerSaveSettings(settings);
        public static void ReloadPerSaveSettings(InGameSettings? maybeSettings = null) {
            var settingsList = maybeSettings?.List ?? Game.Instance?.State?.InGameSettings?.List;
            if (settingsList == null) {
                return;
            }
            Mod.Debug($"reloading per save settings from Player.SettingsList[{PerSaveKey}]");
            if (settingsList.TryGetValue(PerSaveKey, out var obj) && obj is string json) {
                try {
                    cachedPerSave = JsonConvert.DeserializeObject<PerSaveSettings>(json);
                    Mod.Debug($"read successfully from Player.SettingsList[{PerSaveKey}]");
                } catch (Exception e) {
                    Mod.Error($"failed to read from Player.SettingsList[{PerSaveKey}]");
                    Mod.Error(e);
                }
            }
            if (cachedPerSave == null) {
                Mod.Warn("per save settings not found, creating new...");
                cachedPerSave = new PerSaveSettings();
                SavePerSaveSettings();
            }
        }
        public static void SavePerSaveSettings() {
            var player = Game.Instance?.Player;
            if (player == null) return;
            if (cachedPerSave == null)
                return;
            var json = JsonConvert.SerializeObject(cachedPerSave);
            Shodan.GetInGameSettingsList()[PerSaveKey] = json;
            try {
                Mod.Debug($"saved to Player.SettingsList[{PerSaveKey}]");
                if (PerSaveSettings.observers is MulticastDelegate mcdel) {
                    var doomed = new List<PerSaveSettings.Changed>();
                    foreach (var inv in mcdel.GetInvocationList()) {
                        if (inv.Target == null && inv is PerSaveSettings.Changed changed)
                            doomed.Add(changed);
                    }
                    foreach (var del in doomed) {
                        Mod.Debug("removing observer: {del} from PerSaveSettings");
                        PerSaveSettings.observers -= del;
                    }
                }
                if (cachedPerSave)
                    PerSaveSettings.observers?.Invoke(cachedPerSave);
            } catch (Exception e) {
                Mod.Error(e);
            }
        }
        internal PerSaveSettings perSave {
            get {
                if (cachedPerSave != null) return cachedPerSave;
                ReloadPerSaveSettings();
                return cachedPerSave;
            }
        }

        // Main
        public int selectedTab = 0;
        public int increment = 10000;

        // Quality of Life
        public bool toggleContinueAudioOnLostFocus = false;
        public bool highlightObjectsToggle = false;
        public bool toggleShiftClickToUseInventorySlot = false;
        public bool toggleShiftClickToFastTransfer = false;
        // TODO: Public Interface? UI?
        public bool enableLoadWithMissingBlueprints = false;
        public bool toggleZoomableLocalMaps = false;
        public bool toogleShowInterestingNPCsOnQuestTab = false;
        public bool toggleShowInterestingNPCsOnLocalMap = false;
        public bool toggleSkipAnyKeyToContinueWhenLoadingSaves = false;

        // Camera
        public bool toggleZoomOnAllMaps = false;
        public bool toggleRotateOnAllMaps = false;
        // TODO: Public Interface? UI?
        public bool toggleScrollOnAllMaps = false;
        public bool toggleCameraPitch = false;
        public bool toggleCameraElevation = false;
        public bool toggleFreeCamera = false;
        public bool toggleInvertXAxis = false;
        public bool toggleInvertKeyboardXAxis = false;
        public bool toggleInvertYAxis = false;
        public bool toggleOffsetCameraHeight = false;
        public float CameraElevationOffset = 0f;
        public float fovMultiplier = 1;
        internal float AdjustedFovMultiplier => Math.Max(fovMultiplier, toggleZoomableLocalMaps ? 1.25f : 0.4f);

        // Tweaks
        public bool toggleNoPsychicPhenomena = false;
        public bool customizePsychicPhenomena = false;
        public bool toggleInfiniteAbilities = false;
        public bool toggleNoAttackCooldowns = false;
        public bool toggleUnlimitedActionsPerTurn = false;
        public bool toggleReallyUnlimitedActionsPerTurn = false;
        public bool toggleEquipmentRestrictions = false;
        public bool toggleDialogRestrictions = false;
        // TODO: Should this stay experimental?
        public bool toggleDialogRestrictionsEverything = false;
        public bool toggleRestoreSpellsAbilitiesAfterCombat = false;
        public bool toggleInstantRestAfterCombat = false;
        public bool toggleInfiniteItems = false;
        public bool toggleAutomaticallyLoadLastSave = false;
        public bool toggleAllowAchievementsDuringModdedGame = true;
        public bool toggleSkipSplashScreen = false;
        public bool toggleDLC1Theme = false;
        public bool togglAutoEquipConsumables = false;
        public bool toggleEquipItemsDuringCombat = false;
        public bool toggleUseItemsDuringCombat = false;
        public bool toggleTeleportKeysEnabled = false;
        public bool highlightHiddenObjects = false;
        public bool highlightHiddenObjectsInFog = false;
        public bool toggleUnlimitedStatModifierStacking = false;
        public bool disableTraps = false;
        public bool togglekillOnEngage = false;
        public bool disableWarpRandomEncounter = false;
        public HashSet<string> excludedRandomPhenomena = new();
        public HashSet<string> excludedPerilsMinor = new();
        public HashSet<string> excludedPerilsMajor = new();
        public bool freezeVeilThickness = false;
        public bool disableEndTurnHotkey = false;

        // Loot 
        public bool toggleColorLootByRarity = false;
        public bool toggleShowRarityTags = false;
        public bool UsingLootRarity => toggleColorLootByRarity || toggleShowRarityTags;
        public RarityType minRarityToColor = 0;
        public bool toggleMassLootEverything = false;
        public bool toggleLootAliveUnits = false;
        public bool toggleShowHiddenLoot = false;
        public bool toggleLootChecklistFilterBlueprint = false;
        public bool toggleLootChecklistFilterDescription = false;
        public RarityType lootChecklistFilterRarity = RarityType.None;
        public RarityType maxRarityToHide = RarityType.None;

        // Enhanced Inventory
        // TODO: Public Interface? UI?
        public FilterCategories SearchFilterCategories = FilterCategories.Default;
        public ItemSortCategories InventoryItemSorterOptions = ItemSortCategories.Default;

        // level up
        public bool toggleIgnorePrerequisiteStatValue = false;
        public bool toggleIgnorePrerequisiteClassLevel = false;
        public bool toggleIgnoreCareerPrerequisites = false;
        public bool toggleFeaturesIgnorePrerequisites = false;
        public bool toggleSetDefaultRespecLevelZero = false;
        public bool toggleSetDefaultRespecLevelFifteen = false;
        public bool toggleSetDefaultRespecLevelThirtyfive = false;

        // Multipliers
        public float experienceMultiplier = 1;
        public float experienceMultiplierCombat = 1;
        public float experienceMultiplierQuests = 1;
        public float experienceMultiplierSkillChecks = 1;
        public float experienceMultiplierChallenges = 1;
        public float experienceMultiplierSpace = 1;
        public bool useCombatExpSlider = false;
        public bool useQuestsExpSlider = false;
        public bool useSkillChecksExpSlider = false;
        public bool useChallengesExpSlider = false;
        public bool useSpaceExpSlider = false;
        public float fowMultiplier = 1;
        public float walkRangeMultiplier = 1;
        public float sprintRangeMultiplier = 1;
        public float partyMovementSpeedMultiplier = 1;
        public float buffDurationMultiplierValue = 1;
        public float timeScaleMultiplier = 1;
        public float alternateTimeScaleMultiplier = 3;
        public bool useAlternateTimeScaleMultiplier = false;

        // Dice Rolls
        public UnitSelectType allAttacksHit = UnitSelectType.Off;
        public UnitSelectType allHitsCritical = UnitSelectType.Off;
        public UnitSelectType rollWithAdvantage = UnitSelectType.Off;
        public UnitSelectType rollWithDisadvantage = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1 = UnitSelectType.Off;
        public UnitSelectType neverRoll1 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1OutOfCombat = UnitSelectType.Off;
        public UnitSelectType alwaysRoll50 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll100 = UnitSelectType.Off;
        public UnitSelectType neverRoll100 = UnitSelectType.Off;
        public UnitSelectType roll1Initiative = UnitSelectType.Off;
        public UnitSelectType roll5Initiative = UnitSelectType.Off;
        public UnitSelectType roll10Initiative = UnitSelectType.Off;
        public UnitSelectType skillsTake1 = UnitSelectType.Off;
        public UnitSelectType skillsTake25 = UnitSelectType.Off;
        public UnitSelectType skillsTake50 = UnitSelectType.Off;

        // Party Editor
        public int selectedPartyFilter = 0;
        public HashSet<string> namesToDisableVoiceOver = new();

        // Blueprint Browser
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";
        public bool searchDescriptions = true;
        public bool showAttributes = false;
        public bool showAssetIDs = false;
        public bool showComponents = false;
        public bool showElements = false;
        public bool showDisplayAndInternalNames = false;
        public bool sortCollationByEntries = false;

        // Enchantment (Sandal)
        public bool showRatingForEnchantmentInventoryItems = true;

        // Dialog & Previews (Dialogs, Events ,etc)
        public bool previewDialogResults = false;
        public bool previewDialogConditions = false;
        public bool previewAlignmentRestrictedDialog = false;
        public bool toggleAllowAnyGenderRomance = false;
        public bool toggleMultipleRomance = false;
        public bool toggleRemoteCompanionDialog = false;
        public bool toggleExCompanionDialog = false;
        public bool toggleOverrideOccupation = false;
        public HashSet<string> usedOccupations = new();
        public bool toggleShowAnswersForEachConditionalResponse = false;
        public bool toggleMakePreviousAnswersMoreClear = false;

        // Etudes
        public bool showEtudeComments = true;

        // Quests
        public bool toggleQuestHideCompleted = true;
        public bool toggleQuestsShowUnrevealedObjectives = false;
        public bool toggleQuestInspector = false;
        public bool toggleIntrestingNPCsShowFalseConditions = false;
        public bool toggleInterestingNPCsShowHidden = false;

        // Saves 
        public bool toggleShowGameIDs = false;

        public bool toggleIgnoreAbilityAnyRestriction = false;
        public bool toggleIgnoreAbilityAoeOverlap = false;
        public bool toggleIgnoreAbilityLineOfSight = false;
        public bool toggleIgnoreAbilityTargetTooFar = false;
        public bool toggleIgnoreAbilityTargetTooClose = false;

        public HashSet<string> buffsToIgnoreForDurationMultiplier = new(SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier);
        public bool toggleAddFlatEnemyMods = false;
        public bool toggleAddMultiplierEnemyMods = false;
        public SerializableDictionary<StatType, float> flatEnemyMods = SettingsDefaults.DefaultEnemyStatMods(0);
        public SerializableDictionary<StatType, float> multiplierEnemyMods = SettingsDefaults.DefaultEnemyStatMods(1);

        // Development
        public LogLevel loggingLevel = LogLevel.Info;
        public bool stripHtmlTagsFromNativeConsole = true;
        public bool stripHtmlTagsFromUMMLogsTab = true;
        public bool toggleDevopmentMode = false;
        public bool toggleAlwaysUpdate = false;
        public bool toggleGuidsClipboard = false;
        public bool toggleEnableDangerousPatchToolPatches = false;
        public bool toggleRiskyToggles = false;
        public bool onlyShowLanguagesWithFiles = true;
        public int BlueprintsLoaderNumThreads = 3;
        public int BlueprintsLoaderChunkSize = 100;
        public int BlueprintsLoaderNumShards = 32;
        public bool togglePreloadBlueprints = false;
        public bool toggleUseBPIdCache = true;
        public bool toggleAutomaticallyBuildBPIdCache = true;
        public bool shouldTryUpdate = true;
        public bool updateOnChecksumFail = true;
        public bool disableOnChecksumFail = false;
        public bool hasSeenUpdatePage = false;
        public bool toggleVersionCompatability = true;
        public bool toggleIntegrityCheck = true;

        // Patch Tool
        public HashSet<string> disabledPatches = new();
        public bool showPatchToolEnums = true;
        public bool showPatchToolComplexTypes = true;
        public bool showPatchToolBlueprintReferences = true;
        public bool showPatchToolCollections = true;
        public bool showPatchToolPrimitiveTypes = true;
        public bool showPatchToolUnityObjects = false;
        public bool showPatchToolDeleteButtons = false;
        public bool showPatchToolCreateButtons = false;
        public bool togglePatchToolCollapseAllPathsOnPatch = false;


        // Save
        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);

    }
}

