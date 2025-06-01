﻿using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace ToyBox.Infrastructure;
public static class ToyBoxUnitHelper {
    private static readonly Dictionary<UnitEntityData, bool> m_PartyOrPetCache = new();
    private static bool m_IsInitialized = false;
    internal static void Initialize() {
        if (m_IsInitialized) return;
        Main.HarmonyInstance.Patch(AccessTools.Method(typeof(Player), nameof(Player.InvalidateCharacterLists)), new(AccessTools.Method(typeof(ToyBoxUnitHelper), nameof(ToyBoxUnitHelper.ClearCache))));
        m_IsInitialized = true;
    }
    private static void ClearCache() {
        m_PartyOrPetCache.Clear();
    }
    public static bool IsPartyOrPet(UnitEntityData? unit) {
        if (unit == null) return false;
        if (m_PartyOrPetCache.TryGetValue(unit, out bool result)) return result;
        if (unit.OriginalBlueprint == null || Game.Instance?.Player?.AllCharacters is { Count: 0 }) {
            return false;
        }
        var isPartyOrPet = Game.Instance!.Player!.AllCharacters!.Any(x => x.OriginalBlueprint == unit.OriginalBlueprint
                             && (x.Master == null || x.Master.OriginalBlueprint == null
                             || Game.Instance.Player.AllCharacters.Any(y => y.OriginalBlueprint == x.Master.OriginalBlueprint)));
        m_PartyOrPetCache[unit] = isPartyOrPet;
        return isPartyOrPet;
    }
    private static bool IsEnemy(UnitEntityData unit) {
        UnitAttackFactions uaf = unit.Descriptor.AttackFactions;
        return uaf.m_Owner.Faction.EnemyForEveryone || uaf.m_Factions.Contains(BlueprintRoot.Instance.PlayerFaction);
    }
    public static bool IsOfSelectedType(UnitEntityData? unit, UnitSelectType type) {
        if (unit == null) {
            return false;
        }
        return type switch {
            UnitSelectType.Everyone => true,
            UnitSelectType.Party => unit.IsPlayerFaction,
            UnitSelectType.You => unit.IsMainCharacter,
            UnitSelectType.Friendly => !IsEnemy(unit),
            UnitSelectType.Enemies => IsEnemy(unit),
            _ => false,
        };
    }
    public static string GetUnitName(UnitEntityData? unit, bool includeId = false) {
        if (unit == null) {
            return "!!Null Unit!!";
        }
        try {
            string name = unit.CharacterName;
            if (string.IsNullOrWhiteSpace(name)) {
                name = unit.Blueprint.name;
            }
            if (includeId) {
                name += $" ({unit.UniqueId})";
            }
            return name;
        } catch (Exception ex) {
            var id = (unit.Blueprint?.AssetGuid.ToString() ?? "??NULL??");
            Warn($"Encountered exception while getting name for unit with bp {id}: \n{ex}");
            return $"AssetId: {id}";
        }
    }
}
