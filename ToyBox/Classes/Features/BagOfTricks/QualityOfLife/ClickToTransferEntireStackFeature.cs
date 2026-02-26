using Kingmaker.Code.UI.MVVM.View.ServiceWindows.Inventory;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.ClickToTransferEntireStackFeature")]
public partial class ClickToTransferEntireStackFeature : FeatureWithPatch, IToggleWithPseudoBinding {
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
    public override void Enable() {
        base.Enable();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.ClickToTransferEntireStackFeature";
        }
    }
    public void ExecuteAction(ActionParameter parameter) {
        throw new NotImplementedException("By Design");
    }

    public void LogExecution(ActionParameter parameter) {
        throw new NotImplementedException("By Design");
    }
    [HarmonyPatch(typeof(InventorySlotView), nameof(InventorySlotView.OnClick)), HarmonyPrefix]
    public static bool InventorySlotView_OnClick_Patch(InventorySlotView __instance) {
        if (GetInstance<ClickToTransferEntireStackFeature>().Keybind?.IsActive() ?? false) {
            __instance.OnDoubleClick();
            return false;
        }
        return true;
    }
}
