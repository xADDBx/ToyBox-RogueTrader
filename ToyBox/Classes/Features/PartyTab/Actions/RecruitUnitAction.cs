using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Parts;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

[IsTested]
public partial class RecruitUnitAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RecruitUnitAction_Name", "Recruit Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RecruitUnitAction_Description", "Recruits the specified unit and teleports it to you if possible.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            var state = unit.GetCompanionOptional();
            return state == null || state.State == CompanionState.None || state.State == CompanionState.ExCompanion;
        } else {
            return false;
        }
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        var unit = (BaseUnitEntity)parameter[0];
        var currentMode = Game.Instance.CurrentMode;
        GameHelper.RecruitNPC(unit, unit.Blueprint);
        if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
            unit.Position = Game.Instance.Player.MainCharacter.Entity.Position;
            unit.CombatState.LeaveCombat();
            if (unit.IsDetached) {
                Game.Instance.Player.AttachPartyMember(unit);
            }
        }
        foreach (var pet in Game.Instance.Player.PartyAndPets.Where(u => u.IsPet && u.OwnerEntity == unit)) {
            pet.Position = unit.Position;
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

    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_RecruitLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_RecruitLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_RecruitLocalizedText)) {
                    ExecuteAction(unit);
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_UnitIsAlreadyRecruitedLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_RecruitUnitAction_m_UnitIsAlreadyRecruitedLocalizedText", "Unit is already recruited")]
    private static partial string m_UnitIsAlreadyRecruitedLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RecruitUnitAction_m_RecruitLocalizedText", "Recruit")]
    private static partial string m_RecruitLocalizedText { get; }
}
