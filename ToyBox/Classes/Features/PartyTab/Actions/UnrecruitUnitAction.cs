using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

public partial class UnrecruitUnitAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_UnrecruitUnitAction_Name", "Unrecruit Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_UnrecruitUnitAction_Description", "Unrecruits the specified companion.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit && Game.Instance.Player.AllCharacters.Contains(unit) && !unit.IsMainCharacter && !unit.IsStoryCompanion()) {
            var state = unit.GetCompanionOptional();
            return state != null && state.State != CompanionState.Remote && state.State != CompanionState.InParty && state.State != CompanionState.InPartyDetached;
        }
        return false;
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        var unit = (BaseUnitEntity)parameter[0];
        unit.GetCompanionOptional()?.SetState(CompanionState.None);
        unit.Remove<UnitPartCompanion>();
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

    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_UnrecruitLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_UnrecruitLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_UnrecruitLocalizedText)) {
                    ExecuteAction(unit);
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_UnitIsNotRecruitedLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_UnrecruitUnitAction_m_UnitIsNotRecruitedLocalizedText", "Unit is not recruited")]
    private static partial string m_UnitIsNotRecruitedLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_UnrecruitUnitAction_m_UnrecruitLocalizedText", "Unrecruit")]
    private static partial string m_UnrecruitLocalizedText { get; }
}
