﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Facts;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.Utility;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using ModKit.Utility.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;
using static ToyBox.BlueprintExtensions;

namespace ToyBox {
    public static class SearchAndPick {
        public static Settings Settings => Main.Settings;

        public static IEnumerable<SimpleBlueprint> bps = null;
        public static bool hasRepeatableAction;
        public static int maxActions = 0;
        public static int collationPickerPageSize = 30;
        // Need to cache the collators; if not then certain Category changes can lead to Cast Errors
        // Example All -> Spellbooks
        public static Dictionary<Type, Func<SimpleBlueprint, List<string>>> collatorCache = new();
        public static Dictionary<string, string> keyToDisplayName = new();
        public static int collationPickerPageCount => (int)Math.Ceiling((double)collationKeys?.Count / collationPickerPageSize);
        public static int collationPickerCurrentPage = 1;
        public static int repeatCount = 1;
        public static int selectedCollationIndex = 0;
        public static string collationSearchText = "";
        public static string parameter = "";
        public static bool needsRedoKeys = true;
        public static int bpCount = 0;
        public static List<string> collationKeys;
        public static BaseUnitEntity selectedUnit;
        public static Browser<SimpleBlueprint, SimpleBlueprint> SearchAndPickBrowser = new(Mod.ModKitSettings.searchAsYouType);
        public static int[] ParamSelected = new int[1000];
        public static Dictionary<BlueprintFeatureSelection_Obsolete, string[]> selectionBPValuesNames = new() { };

        private static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(
#if DEBUG
                // Whatever is collated here results in roughly 14k Collation Keys in Wrath.
                bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "")?.ToArray() ?? new string[] {}
#endif
                )),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.Collat`ionNames(bp.m_AllElements?.Select(e => e.GetType().Name).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.ToString().TrimEnd(digits)).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.name.Split('$')[1].TrimEnd(digits)).ToArray() ?? new string[] {})),
            new NamedTypeFilter<BlueprintFact>("Facts", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationNames(
                                                      )),
            new NamedTypeFilter<BlueprintCareerPath>("Careers", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintProgression>("Progression", null, bp => bp.Classes.NotNull().Select(cl => cl.Name).ToList()),
            new NamedTypeFilter<BlueprintArchetype>("Archetypes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.CollationNames(bp.School.ToString())),
            new NamedTypeFilter<BlueprintBrain>("Brains", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbilityResource>("Ability Rsrc", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationNames(bp.CharacterClass.Name.ToString())),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintItem>("Item", null,  (bp) => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.CollationNames(bp.m_NonIdentifiedNameText);
                return bp.CollationNames(bp.ItemType.ToString());
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, (bp) =>  bp.CollationNames(bp.ItemType.ToString(), $"{bp.GetCost().ToBinString(RichText.Yellow("⊙"))}")),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (rarity)", null, (bp) => new List<string?> {bp.Rarity().GetString() }),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, (bp) => {
                var family = bp.Family;
                var category = bp.Category;
                return bp.CollationNames(family.ToString(), category.ToString(), $"{bp.GetCost().ToBinString(RichText.Yellow("⊙"))}");
                // return bp.CollationNames("?", $"{bp.GetCost().ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemArmor>("Armor", null, (bp) => {
                var type = bp.Type;
                if (type != null) return bp.CollationNames(type.DefaultName, $"{bp.GetCost().ToBinString(RichText.Yellow("⊙"))}");
                return bp.CollationNames("?", $"{bp.GetCost().ToBinString(RichText.Yellow("⊙"))}");
                }),
            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", null, bp => bp.CollationNames(bp.SubtypeName, $"{bp.GetCost().ToBinString(RichText.Yellow("⊙"))}")),
            new NamedTypeFilter<BlueprintUnit>("Units", null, bp => bp.CollationNames(bp.Race?.Name ?? "?", $"CR{bp.CR}")),
            new NamedTypeFilter<BlueprintUnit>("Units CR", null, bp => bp.CollationNames($"CR {bp.CR}")),
            new NamedTypeFilter<BlueprintRace>("Races", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintArea>("Areas", null, bp => bp.CollationNames()),
            //new NamedTypeFilter<BlueprintAreaPart>("Area Parts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", null, bp =>bp.CollationNames(bp.m_Area.NameSafe())),
            //new NamedTypeFilter<BlueprintAreaEnterPoint>("AreaEntry ", null, bp => bp.m_Tooltip.ToString()),
            new NamedTypeFilter<BlueprintStarSystemMap>(
                    "System Map", null,
                    bp => {
                        var starNames = bp.Stars.Select(r => $"☀ {r.Star.Get().NameForAcronym}");
                        var planetNames = bp.Planets.Select(p => $"◍ {p.Get().Name}");
                        return bp.CollationNames(starNames.Concat(planetNames).ToArray());
                    }
                ),
            new NamedTypeFilter<BlueprintSectorMapPoint>("Sector Map Points", null,  bp => bp.CollationNames(bp.Name.Split(' '))),

            new NamedTypeFilter<Cutscene>("Cut Scenes", null, bp => bp.CollationNames(bp.Priority.ToString())),
            //new NamedTypeFilter<BlueprintMythicInfo>("Mythic Info"),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.CollationNames(bp.m_Type.ToString())),
            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp =>bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? "" )),
            new NamedTypeFilter<BlueprintUnlockableFlag>("Flags", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintDialog>("Dialog",null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintCue>("Cues", null, bp => {
                if (bp.Conditions.HasConditions) {
                    return bp.CollationNames(bp.Conditions.Conditions.First().NameSafe().SubstringBetweenCharacters('$', '$'));
                }
                return new List<string?> { "-" };
                }),
            new NamedTypeFilter<BlueprintAnswer>("Answer", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintPlanet>("Planets", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintColony>("Colonies", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintAreaPreset>("AreaPresets", null, bp =>  bp.CollationNames()),
#if false
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (ench)", null, (bp) => {
                try {
                    var enchants = bp.CollectEnchantments();
                    var value = enchants.Sum((e) => e.EnchantmentCost);
                    return new List<string> { value.ToString() };
                }
                catch {
                    return new List<string> { "0" };
                }
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (cost)", null, (bp) => new List<string> {bp.Cost.ToBinString() }),  
#endif
            //new NamedTypeFilter<SimpleBlueprint>("In Memory", null, bp => bp.CollationName(), () => ResourcesLibrary.s_LoadedBlueprints.Values.Where(bp => bp != null)),

        };

        public static NamedTypeFilter selectedTypeFilter = null;

        public static IEnumerable<SimpleBlueprint> blueprints = null;
        public static bool showCharacterFilterCategories = false;

        public static void RedoLayout() {
            if (bps == null) return;
            if (selectedUnit?.IsDisposed ?? true) selectedUnit = Shodan.MainCharacter;
            foreach (var blueprint in bps) {
                var actions = blueprint.GetActions();
                if (actions.Any(a => a.isRepeatable)) hasRepeatableAction = true;
                var actionCount = actions.Sum(action => action.canPerform(blueprint, selectedUnit) ? 1 : 0);
                maxActions = Math.Max(actionCount, maxActions);
            }
        }

        public static void OnGUI() {
            if (blueprints == null) {
                SearchAndPickBrowser.DisplayShowAllGUI = false;
                SearchAndPickBrowser.doCollation = true;
                if (Event.current.type == EventType.Layout) {
                    blueprints = BlueprintLoader.Shared.GetBlueprints();
                    if (blueprints != null) {
                        InitType();
                    }
                }
            }
            if (Event.current.type == EventType.Layout && needsRedoKeys) {
                needsRedoKeys = SearchAndPickBrowser.isCollating || SearchAndPickBrowser._needsRedoCollation;
                var count = SearchAndPickBrowser.collatedDefinitions.Keys.Count;
                var tmp = new string[(int)(1.1 * count) + 10];
                SearchAndPickBrowser.collatedDefinitions.Keys.CopyTo(tmp, 0);
                collationKeys = tmp.Where(s => !string.IsNullOrEmpty(s) && SearchAndPickBrowser.collatedDefinitions[s].Count > 0).ToList();
                if (Settings.sortCollationByEntries) {
                    collationKeys.Sort(Comparer<string>.Create((x, y) => {
                        return SearchAndPickBrowser.collatedDefinitions[y].Count.CompareTo(SearchAndPickBrowser.collatedDefinitions[x].Count);
                    }));
                } else {
                    collationKeys.Sort(Comparer<string>.Create((x, y) => {
                        if (char.IsNumber(x[x.Length - 1]) && char.IsNumber(y[y.Length - 1])) {
                            int numberOfDigitsAtEndx = 0;
                            int numberOfDigitsAtEndy = 0;
                            for (var i = x.Length - 1; i >= 0; i--) {
                                if (!char.IsNumber(x[i])) {
                                    break;
                                }

                                numberOfDigitsAtEndx++;
                            }
                            for (var i = y.Length - 1; i >= 0; i--) {
                                if (!char.IsNumber(y[i])) {
                                    break;
                                }

                                numberOfDigitsAtEndy++;
                            }
                            var result = x.Take(x.Length - numberOfDigitsAtEndx).ToString().CompareTo(y.Take(y.Length - numberOfDigitsAtEndy).ToString());
                            if (result != 0) return result;
                            var resultx = int.Parse(string.Join("", x.TakeLast(numberOfDigitsAtEndx)));
                            var resulty = int.Parse(string.Join("", y.TakeLast(numberOfDigitsAtEndy)));
                            return resultx.CompareTo(resulty);

                        }
                        return x.CompareTo(y);
                    }));
                }
                keyToDisplayName.Clear();
                collationKeys.ForEach(s => keyToDisplayName[s] = $"{s} ({SearchAndPickBrowser.collatedDefinitions[s]?.Count})");
                collationKeys.Insert(0, "All");
                keyToDisplayName["All"] = $"All ({bpCount})";
            }
            using (HorizontalScope(Width(350))) {
                var remainingWidth = ummWidth;
                using (VerticalScope(GUI.skin.box)) {
                    ActionSelectionGrid(ref Settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name.localize()).ToArray(),
                        1,
                        (selected) => {
                            InitType();
                        },
                        buttonStyle,
                        Width(200));
                }
                remainingWidth -= 350;
                using (VerticalScope()) {
                    if (Toggle("Sort By Count".localize(), ref Settings.sortCollationByEntries)) {
                        needsRedoKeys = true;
                    }
                    if (collationKeys?.Count > 0) {
                        if (PagedVPicker("Categories".localize(), ref SearchAndPickBrowser.collationKey, collationKeys.ToList(), null, s => keyToDisplayName[s], ref collationSearchText, ref collationPickerPageSize, ref collationPickerCurrentPage, Width(300))) {
                            Mod.Debug($"collationKey: {SearchAndPickBrowser.collationKey}");
                        }
                        remainingWidth -= 450;
                    }
                }
                using (VerticalScope(MinWidth(remainingWidth))) {
                    List<Action> todo = new();
                    int count = 0;
                    if (selectedTypeFilter != null) {
                        collatorCache[selectedTypeFilter.type] = selectedTypeFilter.collator;
                    }
                    using (HorizontalScope()) {
                        50.space();
                        using (VerticalScope()) {
                            Toggle("Show Character filter choices".localize(), ref showCharacterFilterCategories);
                            if (showCharacterFilterCategories) {
                                CharacterPicker.OnFilterPickerGUI();
                            }
                            CharacterPicker.OnCharacterPickerGUI();
                            var tmp = CharacterPicker.GetSelectedCharacter();
                            if (tmp != selectedUnit) {
                                selectedUnit = tmp;
                                RedoLayout();
                            }
                        }
                    }
                    SearchAndPickBrowser.OnGUI(bps, () => bps, bp => bp, bp => GetSearchKey(bp) + (Settings.searchDescriptions ? bp.GetDescription() : ""), bp => new[] { GetSortKey(bp) },
                        () => {
                            using (VerticalScope()) {
                                using (HorizontalScope()) {
                                    if (hasRepeatableAction) {
                                        Label(RichText.Cyan("Parameter".localize()) + ": ", ExpandWidth(false));
                                        ActionIntTextField(
                                            ref repeatCount,
                                            "repeatCount",
                                            (limit) => { },
                                            () => { },
                                            Width(100));
                                        Space(40);
                                        repeatCount = Math.Max(1, repeatCount);
                                        repeatCount = Math.Min(100, repeatCount);
                                    }
                                    25.space();
                                    if (Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                    25.space();
                                    if (Toggle("Attributes".localize(), ref Settings.showAttributes, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                    25.space();
                                    Toggle("Show GUIDs".localize(), ref Settings.showAssetIDs, AutoWidth());
                                    25.space();
                                    Toggle("Components".localize(), ref Settings.showComponents, AutoWidth());
                                    25.space();
                                    Toggle("Elements".localize(), ref Settings.showElements, AutoWidth());
                                    25.space();
                                    if (Toggle("Show Display & Internal Names".localize(), ref Settings.showDisplayAndInternalNames, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                }
                            }
                        },
                        (bp, maybeBP) => {
                            GetTitle(bp);
                            Func<string, string> titleFormatter = (t) => RichText.Bold(RichText.Orange(t));
                            if (remainingWidth == 0) remainingWidth = ummWidth;
                            var description = bp.GetDescription().MarkedSubstring(Settings.searchText);
                            if (bp is BlueprintItem itemBlueprint && itemBlueprint.FlavorText?.Length > 0)
                                description = $"{itemBlueprint.FlavorText.StripHTML().Color(RGBA.notable).MarkedSubstring(Settings.searchText)}\n{description}";
                            float titleWidth = 0;
                            var remWidth = remainingWidth;
                            using (HorizontalScope()) {
                                var actions = bp.GetActions()
                                    .Where(action => action.canPerform(bp, selectedUnit));
                                var titles = actions.Select(a => a.name);
                                string title = null;

                                // FIXME - perf bottleneck 
                                var actionCount = actions != null ? actions.Count() : 0;
                                // FIXME - horrible perf bottleneck 
                                // I mean it's an improvement?
                                int removeIndex = -1;
                                int lockIndex = -1;
                                int actionIndex = 0;
                                foreach (var action in titles) {
                                    if (action == "Remove".localize()) {
                                        removeIndex = actionIndex;
                                    }
                                    if (action == "Lock".localize()) {
                                        lockIndex = actionIndex;
                                    }
                                    actionIndex++;
                                }
                                // var removeIndex = titles.IndexOf("Remove".localize());
                                // var lockIndex = titles.IndexOf("Lock".localize());
                                if (removeIndex > -1 || lockIndex > -1) {
                                    title = GetTitle(bp, name => RichText.Bold(RichText.Cyan(name)));
                                } else {
                                    title = GetTitle(bp, name => RichText.Bold(RichText.Orange(name)));
                                }
                                titleWidth = (remainingWidth / (IsWide ? 3 : 4));
                                var text = title.MarkedSubstring(Settings.searchText);
                                if (bp is BlueprintFeatureSelection_Obsolete featureSelection
    ) {
                                    if (Browser.DetailToggle(text, bp, bp, (int)titleWidth))
                                        SearchAndPickBrowser.ReloadData();
                                } else
                                    Label(text, Width((int)titleWidth));
                                remWidth -= titleWidth;

                                if (bp is BlueprintUnlockableFlag flagBP) {
                                    // special case this for now
                                    if (lockIndex >= 0) {
                                        var lockAction = actions.ElementAt(lockIndex);
                                        ActionButton("<", () => { flagBP.Value = flagBP.Value - 1; }, Width(50));
                                        Space(25);
                                        Label(RichText.Bold(RichText.Orange($"{flagBP.Value}")), MinWidth(50));
                                        ActionButton(">", () => { flagBP.Value = flagBP.Value + 1; }, Width(50));
                                        Space(50);
                                        ActionButton(lockAction.name, () => { lockAction.action(bp, selectedUnit, repeatCount); }, Width(120));
                                        Space(100);
#if DEBUG
                                        Label(flagBP.GetDescription().Green());
#endif
                                    } else {
                                        // FIXME - perf bottleneck 
                                        var unlockIndex = titles.IndexOf("Unlock".localize());
                                        if (unlockIndex >= 0) {
                                            var unlockAction = actions.ElementAt(unlockIndex);
                                            Space(240);
                                            ActionButton(unlockAction.name, () => { unlockAction.action(bp, selectedUnit, repeatCount); }, Width(120));
                                            Space(100);
                                        }
                                    }
                                    remWidth -= 300;
                                } else {
                                    for (var ii = 0; ii < maxActions; ii++) {
                                        if (ii < actionCount) {
                                            var action = actions.ElementAt(ii);
                                            // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                                            if (action.name == "<" || action.name == ">") {
                                                Space(174); continue;
                                            }
                                            var actionName = action.name;
                                            float extraSpace = 0;
                                            if (action.isRepeatable) {
                                                actionName += action.isRepeatable ? $" {repeatCount}" : "";
                                                extraSpace = 20 * (float)Math.Ceiling(Math.Log10((double)repeatCount));
                                            }
                                            ActionButton(actionName, () => todo.Add(() => action.action(bp, selectedUnit, repeatCount)), Width(160 + extraSpace));
                                            Space(10);
                                            remWidth -= 174.0f + extraSpace;

                                        } else {
                                            Space(174);
                                        }
                                    }
                                }
                                Space(10);
                                var type = bp.GetType();
                                var typeString = type.Name;
                                Func<SimpleBlueprint, List<string>> collator;
                                collatorCache.TryGetValue(type, out collator);
                                if (collator != null) {
                                    List<string> names = new();
                                    try {
                                        names = collator(bp);
                                    } catch (Exception ex) {
                                        Mod.Warn($"Error trying to collate for Blueprint: {bp?.name ?? "null"} (Id: {bp?.AssetGuid.ToString() ?? "null"}):\n{ex.ToString()}");
                                    }
                                    if (names.Count > 0) {
                                        var collatorString = names.First();
                                        if (bp is BlueprintItem itemBP) {
                                            var rarity = itemBP.Rarity();
                                            typeString = $"{typeString} - {rarity}".Rarity(rarity);
                                        }
                                        if (!typeString.Contains(collatorString)) {
                                            typeString += RichText.Yellow($" : {collatorString}");
                                        }
                                    }
                                }
                                var attributes = "";
                                if (Settings.showAttributes) {
                                    var attr = string.Join(" ", bp.Attributes());
                                    if (!typeString.Contains(attr))
                                        attributes = attr;
                                }

                                if (attributes.Length > 1) typeString += $" - {RichText.Orange(attributes)}";

                                if (description != null && description.Length > 0) description = $"{description}";
                                else description = "";
                                if (bp is BlueprintScriptableObject bpso) {
                                    if (Settings.showComponents && bpso.ComponentsArray?.Length > 0) {
                                        var componentStr = string.Join<object>(", ", bpso.ComponentsArray).Color(RGBA.brown);
                                        if (description.Length == 0) description = componentStr;
                                        else description = description + "\n" + componentStr;
                                    }
                                    if (Settings.showElements && bpso.ElementsArray?.Count > 0) {
                                        var elementsStr = RichText.Yellow(string.Join<object>("\n", bpso.ElementsArray.Select(e => $"{e.GetType().Name.Cyan()} {e.GetCaption()}")));
                                        if (description.Length == 0) description = elementsStr;
                                        else description = description + "\n" + elementsStr;
                                    }
                                }
                                using (VerticalScope(Width(remWidth))) {
                                    using (HorizontalScope(Width(remWidth))) {
                                        ReflectionTreeView.DetailToggle("", bp, bp, 0);
                                        Space(-17);
                                        if (Settings.showAssetIDs) {
                                            Label(typeString, rarityButtonStyle);
                                            ClipboardLabel(bp.AssetGuid.ToString(), ExpandWidth(false));
                                        } else Label(typeString, rarityButtonStyle);
                                        Space(17);
                                    }
                                    if (description.Length > 0) Label(RichText.Green(description), Width(remWidth));
                                }
                            }
                            count++;
                        },
                        (bp, maybeBP) => {
                            ReflectionTreeView.OnDetailGUI(bp);
                            if (bp is BlueprintMechanicEntityFact buf) {
                                FactsEditor.BlueprintDetailGUI<MechanicEntityFact, BlueprintMechanicEntityFact, SimpleBlueprint, SimpleBlueprint>(buf, null, selectedUnit, SearchAndPickBrowser);
                            }
                        }, 50, true, true, 100, 300, "", false, selectedTypeFilter?.collator);
                    foreach (var action in todo) {
                        action();
                    }
                }
                Space(25);
            }
        }

        public static void InitType() {
            collationPickerCurrentPage = 1;
            selectedTypeFilter = blueprintTypeFilters[Settings.selectedBPTypeFilter];
            if (selectedTypeFilter.blueprintSource != null) bps = selectedTypeFilter.blueprintSource();
            else bps = from bp in BlueprintLoader.BlueprintsOfType(selectedTypeFilter.type)
                       where selectedTypeFilter.filter(bp)
                       select bp;
            RedoLayout();
            bpCount = bps.Count();
            SearchAndPickBrowser.RedoCollation();
            needsRedoKeys = true;
        }

        public static void ResetGUI() {
            RedoLayout();
            SearchAndPickBrowser.RedoCollation();
            needsRedoKeys = true;
        }
    }
}