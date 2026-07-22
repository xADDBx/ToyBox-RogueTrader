using System.Globalization;
using System.Xml.Linq;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.Enums;
using Newtonsoft.Json.Linq;

namespace ToyBox.Features.SettingsTab.Other;

internal static class TB1SettingsPorter {
    internal sealed class PortResult {
        public bool Success;
        public int Applied;
        public List<string> Warnings = [];
        public string? Error;
    }

    internal static string DefaultTB1SettingsPath() {
        var tb2Dir = Main.ModEntry.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(tb2Dir, "Settings.xml");
    }

    internal static PortResult Port(string xmlPath) {
        var result = new PortResult();
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath)) {
            result.Error = $"No ToyBox 1 Settings.xml found at: {xmlPath}";
            return result;
        }

        XElement? root;
        try {
            root = XDocument.Load(xmlPath).Root;
        } catch (Exception ex) {
            result.Error = $"Failed to parse ToyBox 1 Settings.xml: {ex.Message}";
            return result;
        }
        if (root == null) {
            result.Error = "ToyBox 1 Settings.xml was empty.";
            return result;
        }

        var applied = 0;
        var anyDiceActive = false;

        bool TryBool(string name, out bool val) {
            val = false;
            var e = root.Element(name);
            return e != null && bool.TryParse(e.Value, out val);
        }
        bool TryFloat(string name, out float val) {
            val = 0f;
            var e = root.Element(name);
            return e != null && float.TryParse(e.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
        }
        bool TryInt(string name, out int val) {
            val = 0;
            var e = root.Element(name);
            return e != null && int.TryParse(e.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
        }
        void MapBool(string name, ref bool target) {
            if (TryBool(name, out var v)) {
                target = v;
                applied++;
            }
        }
        void MapFloat(string name, ref float target) {
            if (TryFloat(name, out var v)) {
                target = v;
                applied++;
            }
        }
        void MapInt(string name, ref int target) {
            if (TryInt(name, out var v)) {
                target = v;
                applied++;
            }
        }
        void MapNullableFloat(string name, ref float? target, float neutral) {
            if (TryFloat(name, out var v) && v != neutral) {
                target = v;
                applied++;
            }
        }
        void MapDice(string name, ref UnitSelectType target) {
            var str = root.Element(name)?.Value;
            if (str != null && Enum.TryParse<UnitSelectType>(str, out var v)) {
                target = v;
                applied++;
                if (v != UnitSelectType.Off) { anyDiceActive = true; }
            }
        }
        void MapSet(string name, HashSet<string> target) {
            var e = root.Element(name);
            if (e == null) { return; }
            target.Clear();
            foreach (var item in e.Elements("string")) {
                if (!string.IsNullOrEmpty(item.Value)) { _ = target.Add(item.Value); }
            }
            applied++;
        }
        void MapStatDict(string name, Action clear, Action<StatType, float> assign) {
            var e = root.Element(name);
            if (e == null) { return; }
            clear();
            foreach (var item in e.Elements("item")) {
                var keyStr = item.Element("key")?.Elements().FirstOrDefault()?.Value;
                var valStr = item.Element("value")?.Elements().FirstOrDefault()?.Value;
                if (keyStr != null && valStr != null
                    && Enum.TryParse<StatType>(keyStr, out var stat)
                    && float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) {
                    assign(stat, f);
                }
            }
            applied++;
        }

        // Quality of Life / cheats
        MapBool("highlightObjectsToggle", ref Settings.EnableObjectHighlightToggle);
        MapBool("toggleShiftClickToFastTransfer", ref Settings.EnableClickToTransferEntireStack);
        MapBool("togglAutoEquipConsumables", ref Settings.EnableRefillBeltConsumables);
        MapBool("enableLoadWithMissingBlueprints", ref Settings.EnableLoadingWithBlueprintErrors);
        MapBool("toggleAllowAchievementsDuringModdedGame", ref Settings.EnableModdedAchievements);
        MapBool("toggleSkipSplashScreen", ref Settings.EnableSkipSplashScreen);
        MapBool("toggleAutomaticallyLoadLastSave", ref Settings.EnableAutoLoadLastSaveOnLaunch);
        MapBool("disableEndTurnHotkey", ref Settings.DisableEndTurnKeybindFeature);

        // Camera
        MapBool("toggleZoomOnAllMaps", ref Settings.EnableAllowZoomOnAllMapsAndCutscenes);
        MapBool("toggleRotateOnAllMaps", ref Settings.EnableAllowRotateOnAllMapsAndCutscenes);
        MapBool("toggleCameraElevation", ref Settings.EnableDragCameraElevation);
        MapBool("toggleFreeCamera", ref Settings.EnableFreeCam);
        MapFloat("CameraElevationOffset", ref Settings.CameraElevationOffset);
        MapFloat("fovMultiplier", ref Settings.FOVMultiplierSetting);

        // Tweaks / cheats
        MapBool("toggleNoPsychicPhenomena", ref Settings.EnablePreventPsychicPhenomena);
        MapBool("customizePsychicPhenomena", ref Settings.EnableCustomizePsychicPhenomena);
        MapBool("freezeVeilThickness", ref Settings.EnablePreventVeilThicknessFromChanging);
        MapBool("disableWarpRandomEncounter", ref Settings.DisableRandomWarpEncounters);
        MapBool("toggleInfiniteAbilities", ref Settings.EnableFreeAbilities);
        MapBool("toggleNoAttackCooldowns", ref Settings.EnableNoAbilityCooldowns);
        MapBool("toggleUnlimitedActionsPerTurn", ref Settings.EnablePartialUnlimitedActionsPerTurn);
        MapBool("toggleReallyUnlimitedActionsPerTurn", ref Settings.EnableCompleteUnlimitedActionsPerTurn);
        MapBool("toggleEquipmentRestrictions", ref Settings.EnableIgnoreEquipmentRestrictions);
        MapBool("toggleRestoreSpellsAbilitiesAfterCombat", ref Settings.EnableRestoreSpellsAndSkillsAfterCombat);
        MapBool("toggleInstantRestAfterCombat", ref Settings.EnableInstantRestAfterCombat);
        MapBool("toggleInfiniteItems", ref Settings.EnableInfiniteChargesOnItems);
        MapBool("toggleEquipItemsDuringCombat", ref Settings.EnableEquipmentChangeDuringCombat);
        MapBool("toggleUseItemsDuringCombat", ref Settings.EnableIInventorytemUseDuringCombat);
        MapBool("highlightHiddenObjects", ref Settings.HighlightHiddenObjects);
        MapBool("highlightHiddenObjectsInFog", ref Settings.HighlightInFogOfWar);
        MapBool("toggleUnlimitedStatModifierStacking", ref Settings.EnableUnlimitedStackingOfModifiers);
        MapBool("disableTraps", ref Settings.PreventTrapsFromTriggering);
        MapBool("togglekillOnEngage", ref Settings.EnableMurderHobo);
        MapSet("excludedRandomPhenomena", Settings.ExcludedRandomPhenomena);
        MapSet("excludedPerilsMinor", Settings.ExcludedPerilsMinor);
        MapSet("excludedPerilsMajor", Settings.ExcludedPerilsMajor);

        // Ability requirement bypasses
        MapBool("toggleIgnoreAbilityAnyRestriction", ref Settings.EnableIgnoreAllAbilityRequirements);
        MapBool("toggleIgnoreAbilityAoeOverlap", ref Settings.EnableIgnoreAoeOverlapAbilityRequirement);
        MapBool("toggleIgnoreAbilityLineOfSight", ref Settings.EnableIgnoreLineOfSightAbilityRequirement);
        MapBool("toggleIgnoreAbilityTargetTooFar", ref Settings.EnableIgnoreTargetTooFarAbilityRequirement);
        MapBool("toggleIgnoreAbilityTargetTooClose", ref Settings.EnableIgnoreTargetTooCloseAbilityRequirement);

        // Enemy stat modifiers
        MapBool("toggleAddFlatEnemyMods", ref Settings.EnableEnemyFlatStatModifier);
        MapBool("toggleAddMultiplierEnemyMods", ref Settings.EnableEnemyStatMultiplier);
        MapStatDict("flatEnemyMods", Settings.FlatEnemyMods.Clear, (k, v) => Settings.FlatEnemyMods[k] = (int)Math.Round(v));
        MapStatDict("multiplierEnemyMods", Settings.MultiplierEnemyMods.Clear, (k, v) => Settings.MultiplierEnemyMods[k] = v);

        // Loot
        MapBool("toggleLootAliveUnits", ref Settings.MassLootShowLivingNPCItems);
        if (TryBool("toggleShowHiddenLoot", out var showHiddenLoot)) {
            Settings.MassLootShowHiddenItems = showHiddenLoot;
            Settings.LootChecklistShowHiddenLoot = showHiddenLoot;
            applied++;
        }

        // Level up
        MapBool("toggleIgnorePrerequisiteStatValue", ref Settings.EnableIgnoreStatPrerequisites);
        MapBool("toggleIgnorePrerequisiteClassLevel", ref Settings.EnableIgnoreClassLevelsPrerequisites);
        MapBool("toggleIgnoreCareerPrerequisites", ref Settings.EnableIgnoreArchetypePrerequisites);
        MapBool("toggleFeaturesIgnorePrerequisites", ref Settings.EnableIgnoreTalentPrerequisites);
        // TB1 had three preset respec toggles; TB2 has one toggle plus a level.
        var respecZero = TryBool("toggleSetDefaultRespecLevelZero", out var rz) && rz;
        var respecFifteen = TryBool("toggleSetDefaultRespecLevelFifteen", out var rf) && rf;
        var respecThirtyFive = TryBool("toggleSetDefaultRespecLevelThirtyfive", out var rt) && rt;
        if (respecZero || respecFifteen || respecThirtyFive) {
            Settings.EnableRespecFromLevelX = true;
            Settings.CurrentRespecLevelSetting = respecZero ? 0 : respecFifteen ? 15 : 35;
            applied++;
        }

        // Experience & other multipliers
        MapFloat("experienceMultiplier", ref Settings.AllExperienceMultiplier);
        MapFloat("experienceMultiplierCombat", ref Settings.CombatExperienceMultiplier);
        MapFloat("experienceMultiplierQuests", ref Settings.QuestExperienceMultiplier);
        MapFloat("experienceMultiplierSkillChecks", ref Settings.SkillCheckMultiplier);
        MapFloat("experienceMultiplierChallenges", ref Settings.ChallengeMultiplier);
        MapFloat("experienceMultiplierSpace", ref Settings.SpaceCombatMultiplier);
        MapBool("useCombatExpSlider", ref Settings.UseCombatExperienceMultiplier);
        MapBool("useQuestsExpSlider", ref Settings.UseQuestExperienceMultiplier);
        MapBool("useSkillChecksExpSlider", ref Settings.UseSkillCheckMultiplier);
        MapBool("useChallengesExpSlider", ref Settings.UseChallengesMultiplier);
        MapBool("useSpaceExpSlider", ref Settings.UseSpaceCombatMultiplier);
        MapNullableFloat("partyMovementSpeedMultiplier", ref Settings.MovementSpeedMultiplier, 1f);
        MapNullableFloat("buffDurationMultiplierValue", ref Settings.BuffDurationMultiplier, 1f);
        var walkBase = BlueprintRoot.Instance?.MaxWalkDistance;
        if (Settings.MaxWalkDistanceSetting == null && walkBase.HasValue
            && TryFloat("walkRangeMultiplier", out var walkMult) && walkMult != 1f) {
            Settings.MaxWalkDistanceSetting = (int)Math.Round(walkMult * walkBase.Value);
            applied++;
        }
        var sprintBase = BlueprintRoot.Instance?.MinSprintDistance;
        if (Settings.MinSprintDistanceSetting == null && sprintBase.HasValue
            && TryFloat("sprintRangeMultiplier", out var sprintMult) && sprintMult != 1f) {
            Settings.MinSprintDistanceSetting = (int)Math.Round(sprintMult * sprintBase.Value);
            applied++;
        }
        MapFloat("timeScaleMultiplier", ref Settings.GameTimeScaleMultiplier);
        MapFloat("alternateTimeScaleMultiplier", ref Settings.GameAlternateTimeScaleMultiplier);
        MapBool("useAlternateTimeScaleMultiplier", ref Settings.EnableGameAlternateTimeScale);
        MapSet("buffsToIgnoreForDurationMultiplier", Settings.BuffDurationMultiplierExclusions);

        // Dice rolls
        MapDice("allAttacksHit", ref Settings.DiceRollsAllAttacksHit);
        MapDice("allHitsCritical", ref Settings.DiceRollsAllAttacksCrit);
        MapDice("rollWithAdvantage", ref Settings.DiceRollsRollWithAdvantage);
        MapDice("rollWithDisadvantage", ref Settings.DiceRollsRollWithDisadvantage);
        MapDice("alwaysRoll100", ref Settings.DiceRollsAlwaysRoll100);
        MapDice("alwaysRoll50", ref Settings.DiceRollsAlwaysRoll50);
        MapDice("alwaysRoll1", ref Settings.DiceRollsAlwaysRoll1);
        MapDice("neverRoll100", ref Settings.DiceRollsNeverRoll100);
        MapDice("neverRoll1", ref Settings.DiceRollsNeverRoll1);
        MapDice("roll10Initiative", ref Settings.DiceRollsInitiativeAlwaysRoll10);
        MapDice("roll5Initiative", ref Settings.DiceRollsInitiativeAlwaysRoll5);
        MapDice("roll1Initiative", ref Settings.DiceRollsInitiativeAlwaysRoll1);
        MapDice("skillsTake50", ref Settings.DiceRollsSkillChecksTake50);
        MapDice("skillsTake25", ref Settings.DiceRollsSkillChecksTake25);
        MapDice("skillsTake1", ref Settings.DiceRollsSkillChecksTake1);
        if (anyDiceActive) { Settings.EnableDiceRollsOverrides = true; }

        // Party
        MapSet("namesToDisableVoiceOver", Settings.DisableVoiceoverForCharacterName);

        // Blueprint browser / Search 'n Pick
        MapInt("searchLimit", ref Settings.PageLimit);
        MapBool("searchDescriptions", ref Settings.ToggleSearchDescriptions);
        MapBool("showAssetIDs", ref Settings.ToggleShowBlueprintAssetIds);
        MapBool("showDisplayAndInternalNames", ref Settings.ToggleBPsShowDisplayAndInternalName);
        MapBool("sortCollationByEntries", ref Settings.SortCollationCategoriesByCount);

        // Dialog & previews
        MapBool("previewDialogResults", ref Settings.EnablePreviewDialogResults);
        MapBool("previewDialogConditions", ref Settings.EnablePreviewDialogConditions);
        MapBool("toggleAllowAnyGenderRomance", ref Settings.EnableLoveIsFree);
        MapBool("toggleMultipleRomance", ref Settings.EnableJealousyBegone);
        MapBool("toggleRemoteCompanionDialog", ref Settings.EnableRemoteCompanionDialog);
        MapBool("toggleExCompanionDialog", ref Settings.EnableExCompanionDialog);
        MapBool("toggleOverrideOccupation", ref Settings.EnableOverrideStoryOccupation);
        MapSet("usedOccupations", Settings.OverridenOccupations);

        // Etudes
        MapBool("showEtudeComments", ref Settings.ShowEtudeComments);

        // Quests & interesting NPCs
        MapBool("toggleQuestHideCompleted", ref Settings.QuestsHideCompleted);
        MapBool("toggleQuestsShowUnrevealedObjectives", ref Settings.QuestsShowUnrevealedObjectives);
        MapBool("toggleQuestInspector", ref Settings.QuestsShowInspector);
        MapBool("toggleIntrestingNPCsShowFalseConditions", ref Settings.ShowInactiveInterestingNpcConditions);
        MapBool("toggleInterestingNPCsShowHidden", ref Settings.InterestingNpcsShowHidden);

        // Development / diagnostics
        MapBool("toggleDevopmentMode", ref Settings.EnableGameDevelopmentMode);
        MapBool("toggleGuidsClipboard", ref Settings.EnableDisplayGuidsInTooltips);
        MapBool("toggleRiskyToggles", ref Settings.EnableShowRiskyToggles);
        MapBool("toggleVersionCompatability", ref Settings.EnableVersionCompatibilityCheck);
        MapBool("toggleIntegrityCheck", ref Settings.EnableFileIntegrityCheck);
        MapInt("BlueprintsLoaderNumThreads", ref Settings.BlueprintsLoaderNumThreads);
        MapInt("BlueprintsLoaderChunkSize", ref Settings.BlueprintsLoaderChunkSize);
        MapInt("BlueprintsLoaderNumShards", ref Settings.BlueprintsLoaderNumShards);
        MapBool("togglePreloadBlueprints", ref Settings.PreloadBlueprints);
        MapBool("toggleUseBPIdCache", ref Settings.UseBPIdCache);
        MapBool("toggleAutomaticallyBuildBPIdCache", ref Settings.AutomaticallyBuildBPIdCache);
        var logStr = root.Element("loggingLevel")?.Value;
        if (logStr != null) {
            if (Enum.TryParse<LogLevel>(logStr, out var level)) {
                Settings.LogLevel = level;
                applied++;
            } else {
                result.Warnings.Add($"Could not map ToyBox 1 log level '{logStr}'; left unchanged.");
            }
        }

        // Patch tool
        MapBool("toggleEnableDangerousPatchToolPatches", ref Settings.EnableDangerousPatchToolPatches);
        MapSet("disabledPatches", Settings.DisabledPatches);
        MapBool("showPatchToolEnums", ref Settings.ShowPatchToolEnums);
        MapBool("showPatchToolComplexTypes", ref Settings.ShowPatchToolComplexTypes);
        MapBool("showPatchToolBlueprintReferences", ref Settings.ShowPatchToolBlueprintReferences);
        MapBool("showPatchToolCollections", ref Settings.ShowPatchToolCollections);
        MapBool("showPatchToolPrimitiveTypes", ref Settings.ShowPatchToolPrimitiveTypes);
        MapBool("showPatchToolUnityObjects", ref Settings.ShowPatchToolUnityObjects);
        MapBool("showPatchToolDeleteButtons", ref Settings.ShowPatchToolDeleteButtons);
        MapBool("showPatchToolCreateButtons", ref Settings.ShowPatchToolCreateButtons);
        MapBool("togglePatchToolCollapseAllPathsOnPatch", ref Settings.CollapseAllPatchToolPathsOnPatch);

        result.Success = true;
        result.Applied = applied;
        return result;
    }

    internal sealed class PerSaveResult {
        public bool NotInGame;
        public bool Found;
        public int Applied;
        public string? Error;
    }

    private const string m_TB1PerSaveKey = "ToyBox";

    internal static PerSaveResult PortPerSave() {
        var result = new PerSaveResult();
        var list = Kingmaker.Game.Instance?.State?.InGameSettings?.List;
        if (list == null) {
            result.NotInGame = true;
            return result;
        }
        if (!list.TryGetValue(m_TB1PerSaveKey, out var obj) || obj is not string json) {
            return result;
        }
        if (string.IsNullOrWhiteSpace(json)) {
            return result;
        }

        JObject data;
        try {
            data = JObject.Parse(json);
        } catch (Exception ex) {
            result.Error = $"Failed to parse ToyBox 1 per-save data: {ex.Message}";
            return result;
        }
        result.Found = true;

        var inSave = InSaveSettings;
        if (inSave == null) {
            result.Error = "Could not access ToyBox 2 save-specific settings.";
            return result;
        }

        var applied = 0;
        if (data["characterModelSizeMultiplier"] is JObject modelSizes) {
            foreach (var p in modelSizes.Properties()) {
                inSave.VisualSizeOverrides[p.Name] = p.Value.Value<float>();
                applied++;
            }
        }
        if (data["characterSizeModifier"] is JObject sizeModifiers) {
            foreach (var p in sizeModifiers.Properties()) {
                Size size;
                if (p.Value.Type == JTokenType.Integer) {
                    size = (Size)p.Value.Value<int>();
                } else if (Enum.TryParse<Size>(p.Value.Value<string>(), out var parsed)) {
                    size = parsed;
                } else {
                    continue;
                }
                inSave.MechanicalSizeOverrides[p.Name] = size;
                applied++;
            }
        }
        if (data["characterSkeletonReplacers"] is JObject skeletonReplacers) {
            foreach (var p in skeletonReplacers.Properties()) {
                if (p.Value is not JObject parts) {
                    continue;
                }
                var partMap = new Dictionary<string, float>();
                foreach (var part in parts.Properties()) {
                    partMap[part.Name] = part.Value.Value<float>();
                }
                if (partMap.Count > 0) {
                    inSave.SkeletonBoneOverrides[p.Name] = partMap;
                    applied++;
                }
            }
        }
        if (data["doOverrideEnableAiForCompanions"] is JObject aiOverrides) {
            foreach (var p in aiOverrides.Properties()) {
                if (p.Value["Item1"]?.Value<bool>() ?? false) {
                    Settings.OverrideEnableAiForCompanions[p.Name] = p.Value["Item2"]?.Value<bool>() ?? false;
                    applied++;
                }
            }
        }
        result.Applied = applied;
        return result;
    }
}
