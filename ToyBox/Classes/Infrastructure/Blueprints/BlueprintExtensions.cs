// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Code.Blueprints.Quests;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public static Settings Settings => Main.Settings;

        private static ConditionalWeakTable<object, List<string>> _cachedCollationNames = new() { };
        private static readonly HashSet<string> BadList = new();
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
                } catch (Exception ex) {
                    Mod.Debug($"Error while getting name for {uiDataProvider}: {ex}");
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
                        name += $" : {blueprint.name.Color(RGBA.darkgrey)}";
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
                        name += $" : {blueprint.name.Color(RGBA.darkgrey)}";
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
                    } catch (Exception ex) {
                        Mod.Debug($"Error while getting name for {uiDataProvider}:\n{ex}");
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
                    } catch (Exception ex) {
                        Mod.Debug($"Error while getting name for {uiDataProvider}:\n{ex}");
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
        private static Dictionary<Type, List<(Func<SimpleBlueprint, bool>, string)>> PropertyAccessors = new();
        private static Dictionary<Type, string> TypeNamesCache = new();
        public static void CacheTypeProperties(Type type) {
            var accessors = new List<(Func<SimpleBlueprint, bool>, string)>();
            // When get_IsContinuous is called, this will cause a chain rection which crashes the game...
            foreach (var prop in type.GetProperties(AccessTools.allDeclared).Where(p => p.Name.StartsWith("Is") && p.PropertyType == typeof(bool) && !p.Name.StartsWith("IsContinuous"))) {
                var mi = prop.GetGetMethod(true);
                if (mi == null) continue;
                if (mi.IsStatic) {
                    Func<bool> staticDelegate = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), mi);
                    accessors.Add((bp => staticDelegate(), prop.Name));
                } else {
                    var parameter = Expression.Parameter(typeof(SimpleBlueprint), "bp");
                    var propertyAccess = Expression.Property(Expression.Convert(parameter, type), prop);
                    var lambda = Expression.Lambda<Func<SimpleBlueprint, bool>>(propertyAccess, parameter);
                    Func<SimpleBlueprint, bool> compiled = lambda.Compile();
                    accessors.Add((compiled, prop.Name));
                }
            }

            PropertyAccessors[type] = accessors;
        }
        public static IEnumerable<string> Attributes(this SimpleBlueprint bp) {
            if (BadList.Contains(bp.AssetGuid)) return Enumerable.Empty<string>();
            if (!PropertyAccessors.TryGetValue(bp.GetType(), out var accessors)) {
                CacheTypeProperties(bp.GetType());
                accessors = PropertyAccessors[bp.GetType()];
            }

            List<string> modifiers = new List<string>();
            foreach (var accessor in accessors) {
                try {
                    if (accessor.Item1(bp)) {
                        modifiers.Add(accessor.Item2);
                    }
                } catch (Exception e) {
                    Mod.Warn($"Error accessing property on {bp.name}: {e.Message}");
                    BadList.Add(bp.AssetGuid);
                    break;
                }
            }
            return modifiers;
        }
        private static List<string> DefaultCollationNames(this SimpleBlueprint bp, string[] extras) {
            _cachedCollationNames.TryGetValue(bp, out var names);
            if (names == null) {
                var namesSet = new HashSet<string>();
                string typeName;
                var type = bp.GetType();
                if (!TypeNamesCache.TryGetValue(type, out typeName)) {
                    typeName = type.Name;
                    typeName = typeName.Replace("Blueprint", "");

                    TypeNamesCache[type] = typeName;
                }
                namesSet.Add(typeName);

                foreach (var attribute in bp.Attributes()) {
                    namesSet.Add(attribute.Orange());
                }
                names = namesSet.ToList();
                _cachedCollationNames.Add(bp, names);
            }

            return [.. names, .. extras];
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
                return "ERROR".Red().Bold() + $": caught exception {e}";
#else
                return "";
#endif
            }
        }
    }
}