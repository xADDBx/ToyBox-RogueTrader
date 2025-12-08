using Kingmaker.Code.UI.MVVM;
using Kingmaker.Code.UI.MVVM.VM.Loot;
using Kingmaker.Utility;

namespace ToyBox.Features.Loot;

public partial class OpenMassLootAction : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_Loot_OpenMassLootAction_Name", "Open Mass Loot Window")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_OpenMassLootAction_Description", "Lets you open up the area's mass loot screen to grab goodies whenever you want. Normally shown only when you exit the area. Only opens if there is actually any loot available.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);

        var loot = MassLootHelper.GetMassLootFromCurrentArea();
        if (loot == null || !loot.Any()) {
            Warn("Mass Loot null or empty, aborting...");
            return;
        }
        var contextVm = RootUIContext.Instance.SurfaceVM?.StaticPartVM?.LootContextVM;
        if (contextVm == null) {
            Warn("Surface LootContextVM is null (maybe in space?), aborting...");
            return;
        }

        var lootVM = new LootVM(LootContextVM.LootWindowMode.ZoneExit, loot, null, () => contextVm.DisposeAndRemove(contextVm.LootVM));
        contextVm.LootVM.Value = lootVM;
    }
}
