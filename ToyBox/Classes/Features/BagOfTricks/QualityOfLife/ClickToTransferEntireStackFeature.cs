using Kingmaker.Code.UI.MVVM.View.ServiceWindows.Inventory;
using ToyBox.Infrastructure.Keybinds;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.ClickToTransferEntireStackFeature")]
public partial class ClickToTransferEntireStackFeature : FeatureWithPatch, IToggledWithBinding {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableClickToTransferEntireStack;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_ClickToTransferEntireStackFeature_Name", "Click to Transfer Entire Stack")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_ClickToTransferEntireStackFeature_Description", "Enabling this will let you do KEYBIND + Click an item to shift its entire stack.")]
    public override partial string Description { get; }

    public Hotkey? Keybind {
        get;
        set;
    }
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.ClickToTransferEntireStackFeature";
        }
    }
    public void ExecuteAction(params object[] parameter) {
        throw new NotImplementedException();
    }

    public void LogExecution(params object[] parameter) {
        throw new NotImplementedException();
    }
    [HarmonyPatch(typeof(InventorySlotView), nameof(InventorySlotView.OnClick)), HarmonyPrefix]
    public static bool InventorySlotView_OnClick_Patch(InventorySlotView __instance) {
        if (GetInstance<ClickToTransferEntireStackFeature>().Keybind?.IsActive(GetCurrentMask()) ?? false) {
            __instance.OnDoubleClick();
            return false;
        }
        return true;
    }
}
