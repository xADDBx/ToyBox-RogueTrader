using Kingmaker;
using Kingmaker.Code.UI.MVVM;
using Kingmaker.Code.UI.MVVM.VM.Loot;
using Kingmaker.Utility;
using ToyBox.Classes.Infrastructure.Features;
using UnityEngine;
using static Kingmaker.Code.UI.MVVM.VM.Loot.LootContextVM;

namespace ToyBox.Features.Loot;

public partial class LootAoEFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_Loot_LootAoEFeature_Name", "AoE Loot")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_LootAoEFeature_Description", "Opens a loot window containing all the loot within a configurable range (default 15m).")]
    public override partial string Description { get; }

    public override bool CanExecute(ActionParameter parameter) {
        return IsInGameNoDialog();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            LogExecution(parameter);
            var loot = MassLootHelper.GetMassLootFromCurrentArea().Where(wrapper => {
                var isNear = false;
                foreach (var unit in Game.Instance.Player.PartyAndPets) {
                    Vector3 pos = default;
                    if (wrapper.Unit is { } targetUnit) {
                        pos = targetUnit.Position;
                    } else if (wrapper.InteractionLoot is { } targetPart) {
                        pos = targetPart.Owner.Position;
                    }
                    isNear |= Vector3.Distance(pos, unit.Position) < GetInstance<LootAoERangeFeature>().Value;
                    if (isNear) {
                        return isNear;
                    }
                }
                return isNear;
            });
            if (loot == null || !loot.Any()) {
                Warn("AoE Loot null or empty, aborting...");
                return;
            }
            var contextVm = RootUIContext.Instance.SurfaceVM?.StaticPartVM?.LootContextVM;
            if (contextVm == null) {
                Warn("Surface LootContextVM is null (???), aborting...");
                return;
            }

            contextVm.LootVM.Value = new LootVM(LootWindowMode.StandardChest, loot, null, contextVm.DisposeLoot);
        }
    }
}
