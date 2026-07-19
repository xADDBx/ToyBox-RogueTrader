using Kingmaker.Cheats;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

[IsTested]
public partial class KillUnitAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_Name", "Kill Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_Description", "Kills the specified unit by marking it for death.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return parameter.UnitParam != null && !ToyBoxUnitHelper.IsOfSelectedType(parameter.UnitParam, UnitSelectType.Ship);
    }
    public override void ExecuteAction(ActionParameter parameter) {
        LogExecution(parameter);
        CheatsCombat.KillUnit(parameter.UnitParam!);
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
    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_KillLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        var parameter = new ActionParameter(unit);
        if (CanExecute(parameter)) {
            if (narrow) {
                if (UI.Button(m_KillLocalizedText, null, null, Width(m_WidthCache))) {
                    Main.ScheduleForMainThread(() => ExecuteAction(parameter));
                }
            } else {
                if (UI.Button(m_KillLocalizedText)) {
                    Main.ScheduleForMainThread(() => ExecuteAction(parameter));
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_WhatHappenedHereLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_m_WhatHappenedHereLocalizedText", "Something went wrong. You should not be able to see this.")]
    private static partial string m_WhatHappenedHereLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_m_KillLocalizedText", "Kill")]
    private static partial string m_KillLocalizedText { get; }
}
