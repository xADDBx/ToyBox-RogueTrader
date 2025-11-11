using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class ChangeBuffRankBA : BlueprintActionFeature, IBlueprintAction<BlueprintBuff>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintBuff blueprint, params object[] parameter) {
        return CanExecute(blueprint, out _, out _, out _, parameter);
    }

    private bool CanExecute(BlueprintBuff blueprint, out bool canDecrease, out bool canIncrease, out int rank, params object[] parameter) {
        canDecrease = false;
        canIncrease = false;
        rank = 0;
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            if (unit.Facts.Get(blueprint) is { } fact && blueprint.MaxRank > 1) {
                rank = fact.GetRank();
                canDecrease = rank > 1;
                canIncrease = rank < blueprint.MaxRank;
                return true;
            }
        }
        return false;
    }
    private bool ExecuteIncrease(BlueprintBuff blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        ((BaseUnitEntity)parameter[0]).Facts.Get<Buff>(blueprint).AddRank();
        return true;
    }
    private bool ExecuteDecrease(BlueprintBuff blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        ((BaseUnitEntity)parameter[0]).Facts.Get<Buff>(blueprint).RemoveRank();
        return true;
    }
    public bool? OnGui(BlueprintBuff blueprint, bool isFeatureSearch, params object[] parameter) {
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
            UI.Label(m_ThisBuffHasNoRanksText.Red().Bold());
        }
        return result;
    }

    public bool GetContext(out BlueprintBuff? context) {
        return ContextProvider.Blueprint(out context);
    }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out BlueprintBuff? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeBuffRankBA_Name", "Modify Buff Rank")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeBuffRankBA_Description", "Increases or decreases the value of the specified BlueprintBuff on the BlueprintUnit.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeBuffRankBA_ThisBuffHasNoRanksText", "This buff has no ranks")]
    private static partial string m_ThisBuffHasNoRanksText { get; }
}
