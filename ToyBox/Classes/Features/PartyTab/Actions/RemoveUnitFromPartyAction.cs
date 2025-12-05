using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

public partial class RemoveUnitFromPartyAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_Name", "Remove Unit from Party")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_Description", "Removes the specified unit or pet from the current party.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return Game.Instance.Player.ActiveCompanions.Contains(unit);
        } else {
            return false;
        }
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        var unit = (BaseUnitEntity)parameter[0];
        Game.Instance.Player.RemoveCompanion(unit);
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

    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_RemoveLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_RemoveLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_RemoveLocalizedText)) {
                    ExecuteAction(unit);
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_UnitIsNotPartOfPartyLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_m_UnitIsNotPartOfPartyLocalizedText", "Unit is not part of the active Party")]
    private static partial string m_UnitIsNotPartOfPartyLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_m_RemoveLocalizedText", "Remove")]
    private static partial string m_RemoveLocalizedText { get; }
}
