using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Infrastructure;

public static partial class CharacterPicker {
    private const int m_CacheDuration = 1;
    private static readonly Dictionary<CharacterListType, TimedCache<List<BaseUnitEntity>>> m_Lists = new() {
        [CharacterListType.Party] = new(() => Game.Instance.Player.Party ?? [], m_CacheDuration),
        [CharacterListType.PartyAndPets] = new(() => Game.Instance.Player.PartyAndPets ?? [], m_CacheDuration),
        [CharacterListType.AllCharacters] = new(() => Game.Instance.Player.AllCharacters ?? [], m_CacheDuration),
        [CharacterListType.Active] = new(() => Game.Instance.Player.ActiveCompanions ?? [], m_CacheDuration),
        [CharacterListType.Remote] = new(() => Game.Instance.Player.RemoteCompanions?.ToList() ?? [], m_CacheDuration),
        [CharacterListType.CustomCompanions] = new(() => Game.Instance.Player.AllCharacters.Where(u => u.IsCustomCompanion())?.ToList() ?? [], m_CacheDuration),
        [CharacterListType.Pets] = new(() => Game.Instance.Player.AllCharacters.Where(u => u.IsPet)?.ToList() ?? [], m_CacheDuration),
        [CharacterListType.Nearby] = new(() => {
            var player = GameHelper.GetPlayerCharacter();
            return GameHelper.GetTargetsAround(player.Position, Settings.NearbyRange, false, false)?.ToList() ?? [];
        }, m_CacheDuration),
        [CharacterListType.Friendly] = new(() => {
            var player = GameHelper.GetPlayerCharacter();
            return Game.Instance.State.AllBaseUnits.Where(u => u != null && !u.IsEnemy(player))?.ToList() ?? [];
        }, m_CacheDuration),
        [CharacterListType.Enemies] = new(() => {
            var player = GameHelper.GetPlayerCharacter();
            return Game.Instance.State.AllBaseUnits.Where(u => u != null && u.IsEnemy(player))?.ToList() ?? [];
        }, m_CacheDuration),
        [CharacterListType.AllUnits] = new(() => Game.Instance.State.AllBaseUnits?.ToList() ?? [], m_CacheDuration)
    };
    private static CharacterListType m_CurrentList;
    private static WeakReference<BaseUnitEntity>? m_CurrentUnit;
    public static BaseUnitEntity? CurrentUnit {
        get {
            if (m_CurrentUnit is not null && m_CurrentUnit.TryGetTarget(out var unit) && !unit.IsDisposed && !unit.IsDisposingNow && unit.IsInGame) {
                return unit;
            } else {
                return null;
            }
        }
    }
    public static List<BaseUnitEntity> CurrentUnits {
        get {
            return m_Lists[m_CurrentList];
        }
    }
    public static bool OnFilterPickerGUI(int? xcols = null, params GUILayoutOption[] options) {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red());
            return false;
        }
        if (UI.SelectionGrid(ref m_CurrentList, xcols ?? Math.Min(11, m_Lists.Count), type => type.GetLocalized(), options)) {
            m_CurrentUnit = null;
            return true;
        }
        return false;
    }
    public static bool OnCharacterPickerGUI(int? xcols = null, params GUILayoutOption[] options) {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red());
            return false;
        }
        var charactersList = CurrentUnits;
        if (charactersList.Count == 0) {
            UI.Label(m_ThereAreNoCharactersInThisList.Orange(), options);
        } else {
            var tmp = CurrentUnit;
            if (UI.SelectionGrid(ref tmp, charactersList, xcols ?? Math.Min(8, charactersList.Count + 1), unit => ToyBoxUnitHelper.GetUnitName(unit), options)) {
                if (tmp != null) {
                    m_CurrentUnit = new(tmp);
                } else {
                    m_CurrentUnit = null;
                }
                return true;
            }
        }
        return false;
    }

    [LocalizedString("ToyBox_Infrastructure_CharacterPicker_ThereAreNoCharactersInThisList", "There are no characters in this list!")]
    private static partial string m_ThereAreNoCharactersInThisList { get; }
}
