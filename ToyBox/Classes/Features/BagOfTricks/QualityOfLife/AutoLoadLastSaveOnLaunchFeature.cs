using Kingmaker;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.UI.Common;
using UniRx;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.AutoLoadLastSaveOnLaunchFeature")]
public partial class AutoLoadLastSaveOnLaunchFeature : FeatureWithPatch {
    private static bool m_IsLaunch = true;
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableAutoLoadLastSaveOnLaunch;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_AutoLoadLastSaveOnLaunchFeature_Name", "Auto load Last Save on launch")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_AutoLoadLastSaveOnLaunchFeature_Description", "Hold down shift during launch to bypass.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.AutoLoadLastSaveOnLaunchFeature";
        }
    }
    [HarmonyPatch(typeof(MainMenuPCView), nameof(MainMenuPCView.BindViewImplementation)), HarmonyPostfix]
    private static void MainMenuPCView_BindViewImplementation_Patch(MainMenuPCView __instance) {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            m_IsLaunch = false;
            Warn("Auto Load Save on Launch disabled via Shift!");
            return;
        }
        if (m_IsLaunch) {
            m_IsLaunch = false;
            Game.Instance.SaveManager.UpdateSaveListIfNeeded();
            _ = MainThreadDispatcher.StartCoroutine(UIUtilityCheckSaves.WaitForSaveUpdated(__instance.ViewModel.LoadLastGame));
        }
    }
}
