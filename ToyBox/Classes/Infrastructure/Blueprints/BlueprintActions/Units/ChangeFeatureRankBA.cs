using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Progression.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class ChangeFeatureRankBA : BlueprintActionFeature, IBlueprintAction<BlueprintFeature>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintFeature blueprint, params object[] parameter) {
        return CanExecute(blueprint, out _, out _, out _, parameter);
    }

    private bool CanExecute(BlueprintFeature blueprint, out bool canDecrease, out bool canIncrease, out int rank, params object[] parameter) {
        canDecrease = false;
        canIncrease = false;
        rank = 0;
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            if (unit.Facts.Get(blueprint) is { } fact && blueprint.Ranks > 1) {
                rank = fact.GetRank();
                canDecrease = rank > 1;
                canIncrease = rank < blueprint.Ranks;
                return true;
            }
        }
        return false;
    }
    private bool ExecuteIncrease(BlueprintFeature blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        ((BaseUnitEntity)parameter[0]).Facts.Get<Kingmaker.UnitLogic.Feature>(blueprint).AddRank();
        return true;
    }
    private bool ExecuteDecrease(BlueprintFeature blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        ((BaseUnitEntity)parameter[0]).Facts.Get<Kingmaker.UnitLogic.Feature>(blueprint).RemoveRank();
        return true;
    }
    public bool? OnGui(BlueprintFeature blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, out var canDecrease, out var canIncrease, out var rank, parameter)) {
            if (canDecrease) {
                _ = UI.Button(StyleActionString("<", isFeatureSearch), () => {
                    result = ExecuteDecrease(blueprint, parameter);
                });
            }
            UI.Label(StyleActionString($" {rank} ".Bold().Orange(), isFeatureSearch));
            if (canIncrease) {
                _ = UI.Button(StyleActionString(">", isFeatureSearch), () => {
                    result = ExecuteIncrease(blueprint, parameter);
                });
            }
        } else if (isFeatureSearch) {
            UI.Label(m_ThisFeatureHasNoRanksText.Red().Bold());
        }
        return result;
    }

    public bool GetContext(out BlueprintFeature? context) {
        return ContextProvider.Blueprint(out context);
    }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out BlueprintFeature? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFeatureRankBA_Name", "Modify Feature Rank")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFeatureRankBA_Description", "Increases or decreases the value of the specified BlueprintFeature on the BlueprintUnit.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFeatureRankBA_ThisFeatureHasNoRanksText", "This feature has no ranks")]
    private static partial string m_ThisFeatureHasNoRanksText { get; }
}
