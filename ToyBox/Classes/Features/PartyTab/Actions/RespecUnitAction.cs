using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.CharacterInfo;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Levelup.Components;
using ToyBox.Features.LevelUp;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Actions;

[IsTested]
public partial class RespecUnitAction : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RespecUnitAction_Name", "Respec Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RespecUnitAction_Description", "Respecs the specified unit and closes the mod UI.")]
    public override partial string Description { get; }
    public bool CanExecute(params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            if (unit.LifeState.IsDead || unit.IsPet) {
                return false;
            }
            CharacterLevelLimit component = unit.OriginalBlueprint.GetComponent<CharacterLevelLimit>();
            var num = (component != null) ? component.LevelLimit : 0;
            if (GetInstance<RespecFromLevelXFeature>().IsEnabled) {
                return unit.Progression.CharacterLevel > Settings.CurrentRespecLevelSetting;
            } else {
                return unit.Progression.CharacterLevel > num;
            }
        } else {
            return false;
        }
    }
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
        ToggleModWindow();
        var unit = (BaseUnitEntity)parameter[0];
        var pet = unit.Pet;
        unit.Progression.Respec();
        EventBus.RaiseEvent(unit, delegate (IRespecHandler h) {
            h.HandleRespecFinished();
        }, true);
        EventBus.RaiseEvent(delegate (INewServiceWindowUIHandler h) {
            h.HandleOpenCharacterInfoPage(CharInfoPageType.LevelProgression, unit);
        }, true);
        if (pet != null && unit.Pet == null) {
            // Rogue Trader Code:
            // Not doing the following lines will cause saving coroutine to fail after respeccing a unit with a pet, kicking the user back to main menu.
            Game.Instance.Player.CrossSceneState.RemoveEntityData(pet);
            Game.Instance.Player.InvalidateCharacterLists();
            Game.Instance.SelectionCharacter.UpdateSelectedUnits();
        }
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

    private static readonly TimedCache<float> m_WidthCache = new(() => CalculateLargestLabelWidth([m_RespecLocalizedText], GUI.skin.button));
    public void OnGui(BaseUnitEntity unit, bool isFeatureSearch = false, bool narrow = false) {
        if (CanExecute(unit)) {
            if (narrow) {
                if (UI.Button(m_RespecLocalizedText, null, null, Width(m_WidthCache))) {
                    ExecuteAction(unit);
                }
            } else {
                if (UI.Button(m_RespecLocalizedText)) {
                    ExecuteAction(unit);
                }
            }
        } else if (isFeatureSearch) {
            UI.Label(m_Can_tRespecUnitLocalizedText.Red().Bold());
        } else if (narrow) {
            UnscaledSpace(m_WidthCache);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Actions_RespecUnitAction_m_Can_tRespecUnitLocalizedText", "Can't respec unit. This is either because unit is dead, a pet or below it's original level limit (e.g. the recruit level for companions).")]
    private static partial string m_Can_tRespecUnitLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Actions_RespecUnitAction_m_RespecLocalizedText", "Respec")]
    private static partial string m_RespecLocalizedText { get; }
}
