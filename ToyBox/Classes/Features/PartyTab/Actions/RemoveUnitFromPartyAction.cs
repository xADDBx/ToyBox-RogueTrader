using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

[IsTested]
public partial class RemoveUnitFromPartyAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_Name", "Remove Unit from Party")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RemoveUnitFromPartyAction_Description", "Removes the specified unit or pet from the current party.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        if (parameter.UnitParam is BaseUnitEntity unit) {
            return Game.Instance.Player.ActiveCompanions.Contains(unit);
        } else {
            return false;
        }
    }
    public override void ExecuteAction(ActionParameter parameter) {
        LogExecution(parameter);
        var unit = parameter.UnitParam!;
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
        var parameter = new ActionParameter(unit);
        if (CanExecute(parameter)) {
            if (narrow) {
                if (UI.Button(m_RemoveLocalizedText, null, null, Width(m_WidthCache))) {
                    Main.ScheduleForMainThread(() => ExecuteAction(parameter));
                }
            } else {
                if (UI.Button(m_RemoveLocalizedText)) {
                    Main.ScheduleForMainThread(() => ExecuteAction(parameter));
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
