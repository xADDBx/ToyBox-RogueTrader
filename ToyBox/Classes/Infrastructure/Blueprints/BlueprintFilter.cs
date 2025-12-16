using Kingmaker.AI.Blueprints;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Infrastructure.Blueprints;

public interface IBlueprintFilter<out T> where T : SimpleBlueprint {
    string Name { get; }
    bool IsCollating { get; }
    IEnumerable<T>? GetCollatedBlueprints(string category);
    int? GetCountForCategory(string category);
    List<string>? GetAllCollationCategories();
    int GetCollationCategoryWidth();
}
public static partial class BlueprintFilters {
    private static string SubstringBetweenCharacters(string input, char charFrom, char charTo) {
        var posFrom = input.IndexOf(charFrom);
        if (posFrom != -1) {
            var posTo = input.IndexOf(charTo, posFrom + 1);
            if (posTo != -1) {
                return input.Substring(posFrom + 1, posTo - posFrom - 1);
            }
        }
        return string.Empty;
    }
    private static IEnumerable<string> CaptionNames(SimpleBlueprint bp) {
        return bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "") ?? [];
    }
    private static string GetCostString(BlueprintItem bp) {
        return CreateBetweenValuesString(bp.ProfitFactorCost, "⊙".Yellow());
    }
    private static string CreateBetweenValuesString(float v, string? units = "", float binSize = 2f) {
        if (v < 0) {
            return "< 0";
        }
        binSize = Mathf.Clamp(binSize, 1.1f, 20f);
        var logv = Mathf.Log(v) / Mathf.Log(binSize);
        var floorLogV = Mathf.Floor(logv);
        var min = Mathf.Pow(binSize, floorLogV);
        var minStr = AddSuffix(min, units);
        var max = Mathf.Pow(binSize, floorLogV + 1);
        if (min == max) return $"{min:0}{units}";
        var maxStr = AddSuffix(max, units);
        return $"{minStr} - {maxStr}";
    }
    private static string AddSuffix(float v, string? units = "") {
        if (v < 1000) {
            return $"{v:0}{units}";
        } else if (v < 1000000) {
            v = Mathf.Floor(v / 1000);
            return $"{v:0.#}k{units}";
        }
        v = Mathf.Floor(v / 1000000);
        return $"{v:0.#}m{units}";
    }
    public static readonly IBlueprintFilter<SimpleBlueprint>[] Filters = [
        new BlueprintFilter<SimpleBlueprint>(m_AllLocalizedText),
        new BlueprintFilter<BlueprintFact>(m_FactsLocalizedText),
        new BlueprintFilter<BlueprintFeature>(m_FeaturesLocalizedText),
        new BlueprintFilter<BlueprintCareerPath>(m_CareersLocalizedText),
        new BlueprintFilter<BlueprintProgression>(m_ProgressionLocalizedText, bp => [.. bp.Classes.Where(c => c is not null).Select(cl => cl.Name)]),
        new BlueprintFilter<BlueprintAbility>(m_AbilitiesLocalizedText),
        new BlueprintFilter<BlueprintBrain>(m_BrainsLocalizedText),
        new BlueprintFilter<BlueprintAbilityResource>(m_AbilityRsrc_LocalizedText),
        new BlueprintFilter<BlueprintBuff>(m_BuffsLocalizedText),
        new BlueprintFilter<BlueprintItem>(m_ItemsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintItem>.DefaultCollator(bp), bp.ItemType.ToString()];
        }),
        new BlueprintFilter<BlueprintItemEquipment>(m_EquipmentLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintItemEquipment>.DefaultCollator(bp), bp.ItemType.ToString(), GetCostString(bp)];
        }),
        new BlueprintFilter<BlueprintItemWeapon>(m_WeaponsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintItemWeapon>.DefaultCollator(bp), bp.Family.ToString(), bp.Category.ToString(), GetCostString(bp)];
        }),
        new BlueprintFilter<BlueprintItemArmor>(m_ArmorsLocalizedText, bp => {
            if (bp.Type != null) {
                return [..BlueprintFilter<BlueprintItemArmor>.DefaultCollator(bp), bp.Type.DefaultName, GetCostString(bp)];
            } else {
                return [..BlueprintFilter<BlueprintItemArmor>.DefaultCollator(bp), GetCostString(bp)];
            }
        }),
        new BlueprintFilter<BlueprintItemEquipmentUsable>(m_UsableLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintItemEquipmentUsable>.DefaultCollator(bp), bp.SubtypeName, GetCostString(bp)];
        }),
        new BlueprintFilter<BlueprintUnit>(m_UnitsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintUnit>.DefaultCollator(bp), bp.Race?.Name ?? "?", m_CRLocalizedText + $" {bp.CR}"];
        }),
        new BlueprintFilter<BlueprintRace>(m_RacesLocalizedText),
        new BlueprintFilter<BlueprintArea>(m_AreasLocalizedText),
        new BlueprintFilter<BlueprintAreaEnterPoint>(m_AreaEntryLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintAreaEnterPoint>.DefaultCollator(bp), bp.m_Area.NameSafe()];
        }),
        new BlueprintFilter<BlueprintStarSystemMap>(m_SystemMapLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintStarSystemMap>.DefaultCollator(bp), ..bp.Stars.Select(r => $"☀ {r.Star.Get().NameForAcronym}"), ..bp.Planets.Select(p => $"◍ {p.Get().Name}")];
        }),
        new BlueprintFilter<BlueprintSectorMapPoint>(m_SectorMapPointsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintSectorMapPoint>.DefaultCollator(bp), ..bp.Name.Split(' ')];
        }),
        new BlueprintFilter<Cutscene>(m_CutscenesLocalizedText, bp => {
            return [..BlueprintFilter<Cutscene>.DefaultCollator(bp), bp.Priority.ToString()];
        }),
        new BlueprintFilter<BlueprintQuest>(m_QuestsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintQuest>.DefaultCollator(bp), bp.m_Type.ToString()];
        }),
        new BlueprintFilter<BlueprintQuestObjective>(m_QuestObj_LocalizedText, bp => {
            return [..BlueprintFilter<BlueprintQuestObjective>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintEtude>(m_EtudesLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintEtude>.DefaultCollator(bp), bp.Parent?.GetBlueprint().NameSafe() ?? ""];
        }),
        new BlueprintFilter<BlueprintUnlockableFlag>(m_FlagsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintUnlockableFlag>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintDialog>(m_DailogsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintDialog>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintAnswer>(m_AnswersLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintAnswer>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintCue>(m_CuesLocalizedText, bp => {
            if (bp.Conditions.HasConditions) {
                return [..BlueprintFilter<BlueprintCue>.DefaultCollator(bp), SubstringBetweenCharacters(bp.Conditions.Conditions.First().NameSafe(), '$', '$')];
            } else {
                return [..BlueprintFilter<BlueprintCue>.DefaultCollator(bp), "-"];
            }
        }),
        new BlueprintFilter<BlueprintPlanet>(m_PlanetsLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintPlanet>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintColony>(m_ColoniesLocalizedText, bp => {
            return [..BlueprintFilter<BlueprintColony>.DefaultCollator(bp), ..CaptionNames(bp)];
        }),
        new BlueprintFilter<BlueprintAreaPreset>(m_AreaPresetsLocalizedText),
    ];

    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AllLocalizedText", "All")]
    private static partial string m_AllLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_FactsLocalizedText", "Facts")]
    private static partial string m_FactsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_FeaturesLocalizedText", "Features")]
    private static partial string m_FeaturesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_CareersLocalizedText", "Careers")]
    private static partial string m_CareersLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_ProgressionLocalizedText", "Progression")]
    private static partial string m_ProgressionLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AbilitiesLocalizedText", "Abilities")]
    private static partial string m_AbilitiesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_SpellsLocalizedText", "Spells")]
    private static partial string m_SpellsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_BrainsLocalizedText", "Brains")]
    private static partial string m_BrainsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AbilityRsrc_LocalizedText", "Ability Rsrc.")]
    private static partial string m_AbilityRsrc_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_BuffsLocalizedText", "Buffs")]
    private static partial string m_BuffsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_ItemsLocalizedText", "Items")]
    private static partial string m_ItemsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_EquipmentLocalizedText", "Equipment")]
    private static partial string m_EquipmentLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_WeaponsLocalizedText", "Weapons")]
    private static partial string m_WeaponsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_ArmorsLocalizedText", "Armors")]
    private static partial string m_ArmorsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_UsableLocalizedText", "Usable")]
    private static partial string m_UsableLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_UnitsLocalizedText", "Units")]
    private static partial string m_UnitsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_UnitsCRLocalizedText", "Units CR")]
    private static partial string m_UnitsCRLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_CRLocalizedText", "CR")]
    private static partial string m_CRLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_RacesLocalizedText", "Races")]
    private static partial string m_RacesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AreasLocalizedText", "Areas")]
    private static partial string m_AreasLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AreaEntryLocalizedText", "Area Entry")]
    private static partial string m_AreaEntryLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_SystemMapLocalizedText", "System Map")]
    private static partial string m_SystemMapLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_SectorMapPointsLocalizedText", "Sector Map Points")]
    private static partial string m_SectorMapPointsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_CutscenesLocalizedText", "Cutscenes")]
    private static partial string m_CutscenesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_QuestsLocalizedText", "Quests")]
    private static partial string m_QuestsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_QuestObj_LocalizedText", "Quest Obj.")]
    private static partial string m_QuestObj_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_EtudesLocalizedText", "Etudes")]
    internal static partial string m_EtudesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_FlagsLocalizedText", "Flags")]
    private static partial string m_FlagsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_DailogsLocalizedText", "Dailogs")]
    private static partial string m_DailogsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AnswersLocalizedText", "Answers")]
    private static partial string m_AnswersLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_PlanetsLocalizedText", "Planets")]
    private static partial string m_PlanetsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_ColoniesLocalizedText", "Colonies")]
    private static partial string m_ColoniesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_AreaPresetsLocalizedText", "Area Presets")]
    private static partial string m_AreaPresetsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_BlueprintFilters_m_CuesLocalizedText", "Cues")]
    private static partial string m_CuesLocalizedText { get; }
}
public partial class BlueprintFilter<T> : IBlueprintFilter<T> where T : SimpleBlueprint {
    public string Name { get; }
    private bool m_StartedCollating = false;
    public bool IsCollating {
        get;
        private set;
    } = false;
    public readonly Func<T, List<string>> Collator;
    public readonly Func<T, bool>? ShouldUseBpFilter;
    public readonly Func<IEnumerable<T>>? BlueprintSource;
    private ConditionalWeakTable<T, List<string>> m_BlueprintToCollationCache = new();
    private Dictionary<string, List<T>>? m_CollatedBlueprintsCache;
    private List<string>? m_CollationCategories;
    public BlueprintFilter(string name, Func<T, List<string>>? collator = null, Func<T, bool>? maybeFilter = null, Func<IEnumerable<T>>? maybeSource = null) {
        Name = name;
        Collator = collator ?? DefaultCollator;
        ShouldUseBpFilter = maybeFilter;
        BlueprintSource = maybeSource;
        Main.OnLocaleChanged += () => m_BlueprintToCollationCache = new();
    }
    private static readonly Dictionary<Type, List<(Func<SimpleBlueprint, bool>, string)>> m_PropertyAccessors = [];
    private static List<(Func<SimpleBlueprint, bool>, string)> CacheTypeProperties(Type type) {
        var accessors = new List<(Func<SimpleBlueprint, bool>, string)>();
        // When get_IsContinuous is called, this will cause a chain rection which crashes the game...
        foreach (var prop in type.GetProperties(AccessTools.allDeclared).Where(p => p.Name.StartsWith("Is") && p.PropertyType == typeof(bool) && !p.Name.StartsWith("IsContinuous"))) {
            var mi = prop.GetGetMethod(true);
            if (mi == null) {
                continue;
            }
            if (mi.IsStatic) {
                var staticDelegate = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), mi);
                accessors.Add((bp => staticDelegate(), prop.Name));
            } else {
                var parameter = Expression.Parameter(typeof(SimpleBlueprint), "bp");
                var propertyAccess = Expression.Property(Expression.Convert(parameter, type), prop);
                var lambda = Expression.Lambda<Func<SimpleBlueprint, bool>>(propertyAccess, parameter);
                Func<SimpleBlueprint, bool> compiled = lambda.Compile();
                accessors.Add((compiled, prop.Name));
            }
        }

        return m_PropertyAccessors[type] = accessors;
    }
    public static List<string> DefaultCollator(T bp) {
        var type = bp.GetType();
        HashSet<string> namesSet = [type.Name.Replace("Blueprint", "")];
        if (!m_PropertyAccessors.TryGetValue(type, out var accessors)) {
            accessors = CacheTypeProperties(type);
        }
        foreach (var accessor in accessors) {
            try {
                if (accessor.Item1(bp)) {
                    namesSet.Add(accessor.Item2);
                }
            } catch (Exception ex) {
                Log($"Error while collating bp {bp}: \n{ex}");
            }
        }
        return [.. namesSet];
    }
    private List<string> GetCollationCategories(T bp) {
        if (!m_BlueprintToCollationCache.TryGetValue(bp, out var collated)) {
            collated = [.. Collator(bp).Where(s => !string.IsNullOrWhiteSpace(s))];
            m_BlueprintToCollationCache.Add(bp, collated);
        }
        return collated;
    }
    public List<string>? GetAllCollationCategories() {
        lock (this) {
            if (m_CollatedBlueprintsCache == null) {
                if (!IsCollating) {
                    Collate();
                }
                return null;
            } else {
                if (IsCollating) {
                    return null;
                }
            }
        }
        return m_CollationCategories;
    }
    public IEnumerable<T>? GetBlueprints() {
        IEnumerable<T>? bps;
        if (BlueprintSource != null) {
            bps = BlueprintSource();
        } else {
            bps = BPLoader.GetBlueprintsOfType<T>();
        }
        if (ShouldUseBpFilter != null && bps != null) {
            bps = bps.Where(ShouldUseBpFilter);
        }
        return bps;
    }
#warning SortDirection
    public enum SortDirection {
        Ascending = 1,
        Descending = -1
    };
    private const SortDirection Direction = SortDirection.Ascending;
    public static readonly NaturalSortComparer Sorter = new(StringComparison.CurrentCultureIgnoreCase);
    private TimedCache<int>? m_CategoryWidth;
    public int GetCollationCategoryWidth() {
        if (m_CollationCategories?.Count > 0) {
            m_CategoryWidth ??= new(() => (int)(10 * Main.UIScale + CalculateLargestLabelWidth(m_CollationCategories.Select(s => s + $" ({GetCountForCategory(s)!.Value})"), UI.LeftAlignedButtonStyle)));
            return m_CategoryWidth;
        } else {
            return (int)(100 * Main.UIScale);
        }
    }
    private void Collate() {
        var bps = GetBlueprints();
        if (bps?.Any() ?? false) {
            if (!m_StartedCollating) {
                m_StartedCollating = true;
                Main.ScheduleForMainThread(() => {
                    IsCollating = true;
                    m_CollatedBlueprintsCache = [];
                    Task.Run(() => {
                        foreach (var bp in bps) {
                            try {
                                foreach (var key in GetCollationCategories(bp)) {
                                    if (m_CollatedBlueprintsCache.TryGetValue(key, out var group)) {
                                        group.Add(bp);
                                    } else {
                                        m_CollatedBlueprintsCache[key] = [bp];
                                    }
                                }
                            } catch (Exception ex) {
                                Error($"Error collating blueprint {bp}:\n{ex}");
                            }
                        }
                        var cat = m_CollatedBlueprintsCache.Keys.ToList();
/*
#warning Sort Order
                        cat.Sort((a, b) => {
                            return (int)Direction * Sorter.Compare(a, b);
                        });
*/
                        cat.Insert(0, AllLocalizedText);
                        m_CollationCategories = cat;
                        m_CollatedBlueprintsCache[AllLocalizedText] = [.. bps];
 /*
                        foreach (var group in m_CollatedBlueprintsCache.Values) {
                            group.Sort((x, y) => {
                                return (int)Direction * BPHelper.GetSortKey(x).CompareTo(BPHelper.GetSortKey(y));
                            });
                        }
 */
                        Main.ScheduleForMainThread(() => {
                            IsCollating = false;
                        });
                    });
                });
            }
        }
    }
    public int? GetCountForCategory(string category) {
        if (string.IsNullOrEmpty(category)) {
            category = AllLocalizedText;
        }
        if (m_CollatedBlueprintsCache?.TryGetValue(category, out var collated) ?? false) {
            return collated.Count;
        }
        return null;
    }
    public IEnumerable<T>? GetCollatedBlueprints(string category) {
        lock (this) {
            if (m_CollatedBlueprintsCache == null) {
                if (!IsCollating) {
                    Collate();
                }
                return null;
            } else {
                if (IsCollating) {
                    return null;
                }
            }
        }
        m_CollatedBlueprintsCache.TryGetValue(category, out var collated);
        return collated;
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintFilter_AllLocalizedText", "All")]
    internal static partial string AllLocalizedText { get; }
}
