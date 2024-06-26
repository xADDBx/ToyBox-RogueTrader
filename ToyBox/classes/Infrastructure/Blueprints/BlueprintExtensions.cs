﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Code.Blueprints.Quests;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.Utility;
using ModKit;
using Pathfinding.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public static Settings Settings => Main.Settings;

        private static ConditionalWeakTable<object, List<string>> _cachedCollationNames = new() { };
        private static readonly HashSet<BlueprintGuid> BadList = new();
        public static Dictionary<string, string> descriptionCache = new();
        internal static bool wasIncludeInternalNameForTitle = false;
        public static Dictionary<string, string> titleCache = new();
        internal static bool wasIncludeInternalNameForSearchKey = false;
        public static Dictionary<string, string> searchKeyCache = new();
        internal static bool wasIncludeInternalNameForSortKey = false;
        public static Dictionary<string, string> sortKeyCache = new();
        public static void ResetCollationCache() => _cachedCollationNames = new ConditionalWeakTable<object, List<string>> { };
        private static void AddOrUpdateCachedNames(SimpleBlueprint bp, List<string> names) {
            names = names.Distinct().ToList();
            if (_cachedCollationNames.TryGetValue(bp, out _)) {
                _cachedCollationNames.Remove(bp);
                //Mod.Log($"removing: {bp.NameSafe()}");
            }
            _cachedCollationNames.Add(bp, names);
            //Mod.Log($"adding: {bp.NameSafe()} - {names.Count} - {String.Join(", ", names)}");
        }

        public static string GetDisplayName(this SimpleBlueprint bp) => bp switch {
            BlueprintAbilityResource abilityResource => abilityResource.Name,
            BlueprintArchetype archetype => archetype.Name,
#pragma warning disable CS0612 // Type or member is obsolete
            BlueprintCharacterClass charClass => charClass.Name,
#pragma warning restore CS0612 // Type or member is obsolete
            BlueprintItem item => item.Name,
            BlueprintItemEnchantment enchant => enchant.Name,
            BlueprintMechanicEntityFact fact => fact.NameSafe(),
            SimpleBlueprint blueprint => blueprint.name,
            _ => "n/a"
        };
        public static string GetDisplayName(this BlueprintSpellbook bp) {
            var name = bp.DisplayName;
            if (string.IsNullOrEmpty(name)) name = bp.name.Replace("Spellbook", "");
            return name;
        }
        public static string GetTitle(SimpleBlueprint blueprint, Func<string, string> formatter = null) {
            if (titleCache.TryGetValue(blueprint.AssetGuid, out var ret) && (wasIncludeInternalNameForTitle == Settings.showDisplayAndInternalNames)) {
                return ret;
            } else {
                wasIncludeInternalNameForTitle = Settings.showDisplayAndInternalNames;
            }
            if (formatter == null) formatter = s => s;
            if (blueprint is IUIDataProvider uiDataProvider) {
                string name;
                bool isEmpty = true;
                try {
                    isEmpty = string.IsNullOrEmpty(uiDataProvider.Name);
                } catch (NullReferenceException) {
                    Mod.Debug($"Error while getting name for {uiDataProvider}");
                }
                if (isEmpty) {
                    name = blueprint.name;
                } else {
                    if (blueprint is BlueprintSpellbook spellbook) {
                        titleCache[blueprint.AssetGuid] = $"{spellbook.Name} - {spellbook.name}";
                        return $"{spellbook.Name} - {spellbook.name}";
                    }
                    name = formatter(uiDataProvider.Name);
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = formatter(blueprint.name);
                    } else if (Settings.showDisplayAndInternalNames) {
                        name += $" : {blueprint.name.color(RGBA.darkgrey)}";
                    }
                }
                titleCache[blueprint.AssetGuid] = name;
                return name;
            } else if (blueprint is BlueprintItemEnchantment enchantment) {
                string name;
                var isEmpty = string.IsNullOrEmpty(enchantment.Name);
                if (isEmpty) {
                    name = formatter(blueprint.name);
                } else {
                    name = formatter(enchantment.Name);
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = formatter(blueprint.name);
                    } else if (Settings.showDisplayAndInternalNames) {
                        name += $" : {blueprint.name.color(RGBA.darkgrey)}";
                    }
                }
                titleCache[blueprint.AssetGuid] = name;
                return name;
            }
            titleCache[blueprint.AssetGuid] = formatter(blueprint.name);
            return formatter(blueprint.name);
        }
        public static string GetSearchKey(SimpleBlueprint blueprint, bool forceDisplayInternalName = false) {
            if (searchKeyCache.TryGetValue(blueprint.AssetGuid, out var ret) && (wasIncludeInternalNameForSearchKey == (Settings.showDisplayAndInternalNames || forceDisplayInternalName))) {
                return ret;
            } else {
                wasIncludeInternalNameForSearchKey = Settings.showDisplayAndInternalNames || forceDisplayInternalName;
            }
            try {
                if (blueprint is IUIDataProvider uiDataProvider) {
                    string name;
                    bool isEmpty = true;
                    try {
                        isEmpty = string.IsNullOrEmpty(uiDataProvider.Name);
                    } catch (NullReferenceException) {
                        Mod.Debug($"Error while getting name for {uiDataProvider}");
                    }
                    if (isEmpty) {
                        name = blueprint.name;
                    } else {
                        if (uiDataProvider is BlueprintSpellbook spellbook) {
                            searchKeyCache[blueprint.AssetGuid] = $"{spellbook.Name} {spellbook.name} {spellbook.AssetGuid}";
                            return searchKeyCache[blueprint.AssetGuid];
                        }
                        name = uiDataProvider.Name;
                        if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                            name = blueprint.name;
                        } else if (Settings.showDisplayAndInternalNames || forceDisplayInternalName) {
                            name += $" : {blueprint.name}";
                        }
                    }
                    searchKeyCache[blueprint.AssetGuid] = name.StripHTML() + $" {blueprint.AssetGuid}";
                    return searchKeyCache[blueprint.AssetGuid];
                } else if (blueprint is BlueprintItemEnchantment enchantment) {
                    string name;
                    var isEmpty = string.IsNullOrEmpty(enchantment.Name);
                    if (isEmpty) {
                        name = blueprint.name;
                    } else {
                        name = enchantment.Name;
                        if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                            name = blueprint.name;
                        } else if (Settings.showDisplayAndInternalNames) {
                            name += $" : {blueprint.name}";
                        }
                    }
                    searchKeyCache[blueprint.AssetGuid] = name.StripHTML() + $" {blueprint.AssetGuid}";
                    return searchKeyCache[blueprint.AssetGuid];
                }
                searchKeyCache[blueprint.AssetGuid] = blueprint.name.StripHTML() + $" {blueprint.AssetGuid}";
                return searchKeyCache[blueprint.AssetGuid];
            } catch (Exception ex) {
                Mod.Debug(ex.ToString());
                Mod.Debug($"-------{blueprint}-----{blueprint.AssetGuid}");
                return "";
            }
        }
        public static string GetSortKey(SimpleBlueprint blueprint) {
            if (sortKeyCache.TryGetValue(blueprint.AssetGuid, out var ret) && (wasIncludeInternalNameForSortKey == Settings.showDisplayAndInternalNames)) {
                return ret;
            } else {
                wasIncludeInternalNameForSortKey = Settings.showDisplayAndInternalNames;
            }
            try {
                if (blueprint is IUIDataProvider uiDataProvider) {
                    string name;
                    bool isEmpty = true;
                    try {
                        isEmpty = string.IsNullOrEmpty(uiDataProvider.Name);
                    } catch (NullReferenceException) {
                        Mod.Debug($"Error while getting name for {uiDataProvider}");
                    }
                    if (isEmpty) {
                        name = blueprint.name;
                    } else {
                        if (blueprint is BlueprintSpellbook spellbook) {
                            sortKeyCache[blueprint.AssetGuid] = $"{spellbook.Name} - {spellbook.name}";
                            return $"{spellbook.Name} - {spellbook.name}";
                        }
                        name = uiDataProvider.Name;
                        if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                            name = blueprint.name;
                        } else if (Settings.showDisplayAndInternalNames) {
                            name += blueprint.name;
                        }
                    }
                    sortKeyCache[blueprint.AssetGuid] = name;
                    return name;
                } else if (blueprint is BlueprintItemEnchantment enchantment) {
                    string name;
                    var isEmpty = string.IsNullOrEmpty(enchantment.Name);
                    if (isEmpty) {
                        name = blueprint.name;
                    } else {
                        name = enchantment.Name;
                        if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                            name = blueprint.name;
                        } else if (Settings.showDisplayAndInternalNames) {
                            name += blueprint.name;
                        }
                    }
                    sortKeyCache[blueprint.AssetGuid] = name;
                    return name;
                }
                sortKeyCache[blueprint.AssetGuid] = blueprint.name;
                return blueprint.name;
            } catch (Exception ex) {
                Mod.Debug(ex.ToString());
                Mod.Debug($"-------{blueprint}-----{blueprint.AssetGuid}");
                return "";
            }
        }
        public static IEnumerable<string> Attributes(this SimpleBlueprint bp) {
            List<string> modifiers = new();
            if (BadList.Contains(bp.AssetGuid)) return modifiers;
            var traverse = Traverse.Create(bp);
            foreach (var property in Traverse.Create(bp).Properties().Where(property => property.StartsWith("Is") && !property.StartsWith("IsContinuous"))) {
                try {
                    var value = traverse?.Property<bool>(property)?.Value;
                    if (value.HasValue && value.GetValueOrDefault()) {
                        modifiers.Add(property); //.Substring(2));
                    }
                } catch (Exception e) {
                    Mod.Warn($"${bp.name}.{property} thew an exception: {e.Message}");
                    BadList.Add(bp.AssetGuid);
                    break;
                }
            }
            return modifiers;
        }
        private static List<string> DefaultCollationNames(this SimpleBlueprint bp, string[] extras) {
            _cachedCollationNames.TryGetValue(bp, out var names);
            if (names == null) {
                names = new List<string> { };
                var typeName = bp.GetType().Name.Replace("Blueprint", "");
                //var stripIndex = typeName.LastIndexOf("Blueprint");
                //if (stripIndex > 0) typeName = typeName.Substring(stripIndex + "Blueprint".Length);
                names.Add(typeName);
                foreach (var attribute in bp.Attributes())
                    names.Add(attribute.orange());
                _cachedCollationNames.Add(bp, names.Distinct().ToList());
            }

            if (extras != null) names = names.Concat(extras).ToList();
            return names;
        }
        public static List<string> CollationNames(this SimpleBlueprint bp, params string[] extras) => DefaultCollationNames(bp, extras);
        [Obsolete]
        public static List<string> CollationNames(this BlueprintCharacterClass bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.IsArcaneCaster) names.Add("Arcane");
            if (bp.IsDivineCaster) names.Add("Divine");
            if (bp.IsMythic) names.Add("Mythic");
            return names;
        }
        public static List<string> CollationNames(this BlueprintSpellbook bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.CharacterClass.IsDivineCaster) names.Add("Divine");
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static List<string> CollationNames(this BlueprintBuff bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.Harmful) names.Add("Harmful");
            if (bp.RemoveOnRest) names.Add("Rest Removes");
            if (bp.RemoveOnResurrect) names.Add("Res Removes");
            if (bp.Ranks > 0) names.Add($"{bp.Ranks} Ranks");

            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static List<string> CollationNames(this BlueprintArea bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            var typeName = bp.GetType().Name.Replace("Blueprint", "");
            if (typeName == "Area") names.Add($"Area CR{bp.m_CR}");
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static List<string> CollationNames(this BlueprintEtude bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            //foreach (var item in bp.ActivationCondition) {
            //    names.Add(item.name.yellow());
            //}
            //names.Add(bp.ValidationStatus.ToString().yellow());
            //if (bp.HasParent) names.Add($"P:".yellow() + bp.Parent.NameSafe());
            //foreach (var sibling in bp.StartsWith) {
            //    names.Add($"W:".yellow() + bp.Parent.NameSafe());
            //}
            //if (bp.HasLinkedAreaPart) names.Add($"area {bp.LinkedAreaPart.name}".yellow());
            //foreach (var condition in bp.ActivationCondition?.Conditions)
            //    names.Add(condition.GetCaption().yellow());
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static string[] CaptionNames(this SimpleBlueprint bp) => bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "")?.ToArray() ?? new string[] { };
        public static List<String> CaptionCollationNames(this SimpleBlueprint bp) => bp.CollationNames(bp.CaptionNames());
        // Custom Attributes that Owlcat uses 

        private static readonly Dictionary<Type, IEnumerable<SimpleBlueprint>> blueprintsByType = new();
        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.TryGetValue(type, out var ofType)) return ofType;
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<SimpleBlueprint>();
            var filtered = blueprints.Where((bp) => bp?.GetType().IsKindOf(type) == true).ToList();
            // FIXME - why do we get inconsistent partial results if we cache here
            //if (filtered.Count > 0)
            //    blueprintsByType[type] = filtered;
            return filtered;
        }

        public static IEnumerable<BPType> BlueprintsOfType<BPType>() where BPType : SimpleBlueprint {
            var type = typeof(BPType);
            if (blueprintsByType.TryGetValue(type, out var value)) return value.OfType<BPType>();
            var blueprints = BlueprintLoader.Shared.GetBlueprints<BPType>();
            if (blueprints == null) return new List<BPType>();
            var filtered = blueprints.Where((bp) => bp != null).ToList();
            // FIXME - why do we get inconsistent partial results if we cache here
            //if (filtered.Count > 0)
            //    blueprintsByType[type] = filtered;
            return filtered;
        }

        public static IEnumerable<T> GetBlueprints<T>() where T : SimpleBlueprint => BlueprintsOfType<T>();
        public static readonly HashSet<string> badBP = new() { "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082" };
        public static string GetDescription(this SimpleBlueprint bp)
            // borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
            {
            try {
                // avoid exceptions on known broken items
                var guid = bp.AssetGuid;
                if (descriptionCache.TryGetValue(guid, out var desc)) {
                    return desc;
                }
                if (badBP.Contains(guid)) return null;
                var associatedBlueprint = bp as IUIDataProvider;
                desc = associatedBlueprint?.Description?.StripHTML();
                descriptionCache[guid] = desc;
                return desc;

            } catch (Exception e) {
                Mod.Debug(e.ToString());
#if DEBUG
                return "ERROR".red().bold() + $": caught exception {e}";
#else
                return "";
#endif
            }
        }
        [HarmonyPatch(typeof(BlueprintQuestContract))]
        public static class BlueprintQuestContract_Patch {
            [HarmonyPatch(nameof(BlueprintQuestContract.Description), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool get_Description(BlueprintQuestContract __instance, ref string __result) {
                __result = __instance.GetDescription();
                return false;
            }
        }
    }
}