using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Parts;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Infrastructure;

public static class ToyBoxUnitHelper {
    private static readonly Dictionary<AbstractUnitEntity, bool> m_PartyOrPetCache = new();
    private static bool m_IsInitialized = false;
    internal static void Initialize() {
        if (m_IsInitialized) {
            return;
        }
        _ = Main.HarmonyInstance.Patch(AccessTools.Method(typeof(Player), nameof(Player.InvalidateCharacterLists)), new(AccessTools.Method(typeof(ToyBoxUnitHelper), nameof(ToyBoxUnitHelper.ClearCache))));
        m_IsInitialized = true;
    }
    private static void ClearCache() {
        m_PartyOrPetCache.Clear();
    }
    public static bool IsPartyOrPet(AbstractUnitEntity? unit) {
        if (unit == null) {
            return false;
        }
        if (m_PartyOrPetCache.TryGetValue(unit, out bool result)) {
            return result;
        }
        if (unit.OriginalBlueprint == null || Game.Instance?.Player?.AllCharacters is { Count: 0 }) {
            return false;
        }
        var isPartyOrPet = Game.Instance!.Player!.AllCharacters!.Any(x => x.OriginalBlueprint == unit.OriginalBlueprint
                             && (x.Master == null || x.Master.OriginalBlueprint == null
                             || Game.Instance.Player.AllCharacters.Any(y => y.OriginalBlueprint == x.Master.OriginalBlueprint)));
        m_PartyOrPetCache[unit] = isPartyOrPet;
        return isPartyOrPet;
    }
    private static bool IsEnemy(BaseUnitEntity unit) {
        var maybeFaction = unit.GetFactionOptional();
        if (maybeFaction != null) {
            return maybeFaction.AlwaysEnemy || maybeFaction.IsPlayerEnemy;
        }
        return false;
    }
    public static bool IsOfSelectedType(BaseUnitEntity? unit, UnitSelectType type) {
        if (unit == null) {
            return false;
        }
        return type switch {
            UnitSelectType.Everyone => true,
            UnitSelectType.Party => unit.IsPlayerFaction,
            UnitSelectType.You => unit.IsMainCharacter,
            UnitSelectType.Friendly => !IsEnemy(unit),
            UnitSelectType.Enemies => IsEnemy(unit),
            UnitSelectType.Ship => unit.IsStarship() && !IsEnemy(unit),
            UnitSelectType.EnemyShip => unit.IsStarship() && IsEnemy(unit),
            UnitSelectType.Off => false,
            _ => false,
        };
    }
    public static string GetUnitName(AbstractUnitEntity? unit, bool includeId = false) {
        if (unit == null) {
            return "!!Null Unit!!";
        }
        try {
            var name = unit.CharacterName;
            if (string.IsNullOrWhiteSpace(name)) {
                name = unit.Blueprint.name;
            }
            if (includeId) {
                name += $" ({unit.UniqueId})";
            }
            return name;
        } catch (Exception ex) {
            var id = unit.Blueprint?.AssetGuid.ToString() ?? "??NULL??";
            Warn($"Encountered exception while getting name for unit with bp {id}: \n{ex}");
            return $"AssetId: {id}";
        }
    }
}
