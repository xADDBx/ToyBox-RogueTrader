using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Parts;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

public partial class AddUnitToPartyAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_AddUnitToPartyAction_Name", "Add Unit to Party")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_AddUnitToPartyAction_Description", "Adds the specified unit or pet to the current party and teleports it to you if necessary.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return !Game.Instance.Player.PartyAndPets.Contains(unit) && Game.Instance.Player.AllCharacters.Contains(unit) && unit.GetCompanionOptional() != null;
        } else {
            return false;
        }
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        var unit = (BaseUnitEntity)parameter[0];
        var currentMode = Game.Instance.CurrentMode;
        Game.Instance.Player.AddCompanion(unit);
        if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
            unit.IsInGame = true;
            unit.Position = Game.Instance.Player.MainCharacter.Entity.Position;
            unit.CombatState.LeaveCombat();
            if (unit.IsDetached) {
                Game.Instance.Player.AttachPartyMember(unit);
            }
        }
        Game.Instance.Player.FixPartyAfterChange();
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!, true);
            }
        }
    }

    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_AddLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_AddLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_AddLocalizedText)) {
                    ExecuteAction(unit);
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_UnitIsAlreadyPartOfPartyLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_AddUnitToPartyAction_m_UnitIsAlreadyPartOfPartyLocalizedText", "Unit is already part of the active party or not recruited")]
    private static partial string m_UnitIsAlreadyPartOfPartyLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_AddUnitToPartyAction_m_AddLocalizedText", "Add")]
    private static partial string m_AddLocalizedText { get; }
}
