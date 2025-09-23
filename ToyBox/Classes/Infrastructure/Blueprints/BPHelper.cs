﻿using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints;
using Kingmaker.UI;
using System.Collections.Concurrent;
using Kingmaker.Blueprints.Items;

namespace ToyBox.Infrastructure.Blueprints;
public static class BPHelper {
    private static readonly ConcurrentDictionary<(SimpleBlueprint, Func<string, string>), string> m_TitleCache = new();
    private static readonly ConcurrentDictionary<SimpleBlueprint, string> m_SortKeyCache = new();
    private static readonly ConcurrentDictionary<SimpleBlueprint, string> m_SearchKeyCache = new();
    private static readonly ConcurrentDictionary<SimpleBlueprint, string> m_DescriptionCache = new();
    public static string GetTitle(SimpleBlueprint blueprint, Func<string, string>? formatter = null) {
        if (formatter == null) formatter = s => s;
        if (!m_TitleCache.TryGetValue((blueprint, formatter), out var title)) {
            title = CreateGetTitle(blueprint, formatter);
            m_TitleCache[(blueprint, formatter)] = title;
        }
        return title;
    }
    public static string GetSortKey(SimpleBlueprint blueprint) {
        if (!m_SortKeyCache.TryGetValue(blueprint, out var sortKey)) {
            sortKey = CreateSortKey(blueprint);
            m_SortKeyCache[blueprint] = sortKey;
        }
        return sortKey;
    }
    public static string GetSearchKey(SimpleBlueprint blueprint) {
        if (!m_SearchKeyCache.TryGetValue(blueprint, out var searchKey)) {
            searchKey = CreateSearchKey(blueprint);
            m_SearchKeyCache[blueprint] = searchKey;
        }
        return searchKey;
    }
    public static string? GetDescription(SimpleBlueprint blueprint) {
        if (!m_DescriptionCache.TryGetValue(blueprint, out var description)) {
            description = CreateDescription(blueprint);
            m_DescriptionCache[blueprint] = description;
        }
        return description;
    }
    private static string CreateGetTitle(SimpleBlueprint blueprint, Func<string, string> formatter) {
        string? ret = null;
        try {
            if (blueprint is IUIDataProvider uiDataProvider) {
                string Name = "";
                try {
                    Name = uiDataProvider.Name;
                } catch (Exception ex) {
                    Debug($"Error while getting name for {uiDataProvider}:\n{ex}");
                }
                ret = CheckNullName(blueprint, Name);
            } else if (blueprint is BlueprintItemEnchantment enchantment) {
                ret = CheckNullName(blueprint, enchantment.Name);
            }
            ret ??= blueprint.name;
        } catch (Exception ex) {
            Debug($"Error getting SortKey for BP: {blueprint} - {blueprint.AssetGuid}:\n{ex}");
            ret ??= "<ToyBox Error>";
        }
        return formatter(ret);
    }
    private static string CreateSearchKey(SimpleBlueprint blueprint) {
        string? ret = null;
        try {
            if (blueprint is IUIDataProvider uiDataProvider) {
                string Name = "";
                try {
                    Name = uiDataProvider.Name;
                } catch (Exception ex) {
                    Debug($"Error while getting name for {uiDataProvider}:\n{ex}");
                }
                ret = CheckNullName(blueprint, Name, true);
            } else if (blueprint is BlueprintItemEnchantment enchantment) {
                ret = CheckNullName(blueprint, enchantment.Name, true);
            }
            ret ??= blueprint.name;
        } catch (Exception ex) {
            Debug($"Error getting SearchKey for BP: {blueprint} - {blueprint.AssetGuid}:\n{ex}");
            ret ??= "<ToyBox Error>";
        }
        return (ret + $" {blueprint.AssetGuid} {blueprint.GetType()}").ToUpper();
    }
    private static string CreateSortKey(SimpleBlueprint blueprint) {
        string? ret = null;
        try {
            if (blueprint is IUIDataProvider uiDataProvider) {
                string Name = "";
                try {
                    Name = uiDataProvider.Name;
                } catch (Exception ex) {
                    Debug($"Error while getting name for {uiDataProvider}:\n{ex}");
                }
                ret = CheckNullName(blueprint, Name);
                if (Settings.SearchDescriptions) {
                    ret += " " + GetDescription(blueprint);
                }
            } else if (blueprint is BlueprintItemEnchantment enchantment) {
                ret = CheckNullName(blueprint, enchantment.Name);
                if (Settings.SearchDescriptions) {
                    ret += " " + GetDescription(blueprint);
                }
            }
            ret ??= blueprint.name;
        } catch (Exception ex) {
            Debug($"Error getting SortKey for BP: {blueprint} - {blueprint.AssetGuid}:\n{ex}");
            ret ??= "<ToyBox Error>";
        }
        return ret;
    }
    private static string CheckNullName(SimpleBlueprint bp, string name, bool forceInternalDisplay = false, bool colorInternal = false) {
        if (string.IsNullOrEmpty(name)) {
            return bp.name;
        }
        if (name == "<null>" || name.StartsWith("[unknown key: ")) {
            name = colorInternal ? bp.name.DarkGrey() : bp.name;
        } else if (Settings.ToggleBPsShowDisplayAndInternalName || forceInternalDisplay) {
            name += colorInternal ? bp.name.DarkGrey() : bp.name;
        }
        return name;
    }
    private static string? CreateDescription(SimpleBlueprint blueprint) {
        string? ret = null;
        try {
            if (blueprint is BlueprintItem item && !string.IsNullOrWhiteSpace(item.FlavorText)) {
                ret = item.FlavorText + "\n" + item.Description;
            } else if (blueprint is IUIDataProvider provider) {
                ret = provider.Description;
            }
        } catch (Exception ex) {
            Debug($"Error getting Description for BP: {blueprint} - {blueprint.AssetGuid}:\n{ex}");
            ret ??= "<ToyBox Error>";
        }
        return ret;
    }
}
