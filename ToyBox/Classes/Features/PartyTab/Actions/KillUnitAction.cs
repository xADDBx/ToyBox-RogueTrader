using Kingmaker.Cheats;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

public partial class KillUnitAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_Name", "Kill Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_KillUnitAction_Description", "Kills the specified unit by marking it for death.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity) {
            return true;
        } else {
            return false;
        }
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        CheatsCombat.KillUnit((BaseUnitEntity)parameter[0]);
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
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_KillLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_KillLocalizedText)) {
                    ExecuteAction(unit);
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
