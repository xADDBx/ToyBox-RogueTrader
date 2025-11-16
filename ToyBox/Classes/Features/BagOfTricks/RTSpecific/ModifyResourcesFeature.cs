using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.VM.NavigatorResource;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[IsTested]
public partial class ModifyResourcesFeature : Feature {
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_Name", "Modify RT Resources")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_Description", "Allows modifying Scrap, Profit Factor, Navigators Insight and possibly current Veil Thickness.")]
    public override partial string Description { get; }
    private int m_NavigatorAdjustment = 100;
    private int m_ScrapAdjustment = 100;
    private int m_ProfitFactorAdjustment = 1;
    private int m_VeilThicknessAdjustment = 1;
    public override void OnGui() {
        using (HorizontalScope()) {
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }

        var isWarpInit = Game.Instance.Player.WarpTravelState?.IsInitialized ?? false;
        if (isWarpInit) {
            using (HorizontalScope()) {
                UI.Label(m_CurrentNavigatorInsightLocalizedText.Bold() + ": ", Width(250 * Main.UIScale));
                using (VerticalScope()) {
                    UI.Label(Game.Instance.Player.WarpTravelState!.NavigatorResource.ToString());
                    using (HorizontalScope()) {
                        UI.Label(m_AdjustNavigatorInsightByTheFolloLocalizedText + ":");
                        if (UI.TextField(ref m_NavigatorAdjustment, null, GUILayout.MinWidth(200), AutoWidth())) {
                            m_NavigatorAdjustment = m_NavigatorAdjustment < 1 ? 1 : m_NavigatorAdjustment;
                        }
                        Space(10);
                        if (UI.Button(m_AddLocalizedText)) {
                            CheatsGlobalMap.AddNavigatorResource(m_NavigatorAdjustment);
                            SectorMapBottomHudVM.Instance?.SetCurrentValue();
                        }
                        Space(10);
                        if (UI.Button(m_RemoveLocalizedText)) {
                            CheatsGlobalMap.AddNavigatorResource(-m_NavigatorAdjustment);
                            SectorMapBottomHudVM.Instance?.SetCurrentValue();
                        }
                    }
                }
            }
        }
        using (HorizontalScope()) {
            UI.Label(m_CurrentScrapLocalizedText.Bold() + ": ", Width(250 * Main.UIScale));
            using (VerticalScope()) {
                UI.Label(Game.Instance.Player.Scrap.m_Value.ToString());
                using (HorizontalScope()) {
                    UI.Label(m_AdjustScrapByTheFollowingAmountLocalizedText + ":");
                    if (UI.TextField(ref m_ScrapAdjustment, null, GUILayout.MinWidth(200), AutoWidth())) {
                        m_ScrapAdjustment = m_ScrapAdjustment < 1 ? 1 : m_ScrapAdjustment;
                    }
                    Space(10);
                    if (UI.Button(m_AddLocalizedText)) {
                        Game.Instance.Player.Scrap.Receive(m_ScrapAdjustment);
                    }
                    Space(10);
                    if (UI.Button(m_RemoveLocalizedText)) {
                        Game.Instance.Player.Scrap.Receive(-m_ScrapAdjustment);
                    }
                }
            }
        }
        using (HorizontalScope()) {
            UI.Label(m_CurrentProfitFactorLocalizedText.Bold() + ": ", Width(250 * Main.UIScale));
            using (VerticalScope()) {
                UI.Label(Game.Instance.Player.ProfitFactor.Total.ToString());
                using (HorizontalScope()) {
                    UI.Label(m_AdjustProfitFactorByTheFollowingLocalizedText + ":");
                    if (UI.TextField(ref m_ProfitFactorAdjustment, null, GUILayout.MinWidth(200), AutoWidth())) {
                        m_ProfitFactorAdjustment = m_ProfitFactorAdjustment < 1 ? 1 : m_ProfitFactorAdjustment;
                    }
                    Space(10);
                    if (UI.Button(m_AddLocalizedText)) {
                        CheatsColonization.AddPF(m_ProfitFactorAdjustment);
                    }
                    Space(10);
                    if (UI.Button(m_RemoveLocalizedText)) {
                        CheatsColonization.AddPF(-m_ProfitFactorAdjustment);
                    }
                }
            }
        }
        using (HorizontalScope()) {
            var veilThicknessCounter = Game.Instance.TurnController?.VeilThicknessCounter;
            if (veilThicknessCounter != null && Game.Instance.LoadedAreaState?.AreaVailPart != null) {
                UI.Label(m_CurrentVeilThicknessLocalizedText.Bold() + ": ", Width(250 * Main.UIScale));
                using (VerticalScope()) {
                    UI.Label(veilThicknessCounter.Value.ToString());
                    using (HorizontalScope()) {
                        UI.Label(m_SetVeilThicknessToTheFollowingAmLocalizedText + ":");
                        if (UI.TextField(ref m_VeilThicknessAdjustment, null, GUILayout.MinWidth(200), AutoWidth())) {
                            m_VeilThicknessAdjustment = m_VeilThicknessAdjustment < 0 ? 0 : m_VeilThicknessAdjustment;
                        }
                        Space(10);
                        if (UI.Button(m_SetLocalizedText)) {
                            veilThicknessCounter.Value = m_VeilThicknessAdjustment;
                        }
                    }
                }
            }
        }
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_CurrentNavigatorInsightLocalizedText", "Current Navigator Insight")]
    private static partial string m_CurrentNavigatorInsightLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_AdjustNavigatorInsightByTheFolloLocalizedText", "Adjust Navigator Insight by the following amount")]
    private static partial string m_AdjustNavigatorInsightByTheFolloLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_AddLocalizedText", "Add")]
    private static partial string m_AddLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_RemoveLocalizedText", "Remove")]
    private static partial string m_RemoveLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_CurrentScrapLocalizedText", "Current Scrap")]
    private static partial string m_CurrentScrapLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_AdjustScrapByTheFollowingAmountLocalizedText", "Adjust Scrap by the following amount")]
    private static partial string m_AdjustScrapByTheFollowingAmountLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_CurrentProfitFactorLocalizedText", "Current Profit Factor")]
    private static partial string m_CurrentProfitFactorLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_AdjustProfitFactorByTheFollowingLocalizedText", "Adjust Profit Factor by the following amount")]
    private static partial string m_AdjustProfitFactorByTheFollowingLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_CurrentVeilThicknessLocalizedText", "Current Veil Thickness")]
    private static partial string m_CurrentVeilThicknessLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_SetVeilThicknessToTheFollowingAmLocalizedText", "Set Veil Thickness to the following amount")]
    private static partial string m_SetVeilThicknessToTheFollowingAmLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyResourcesFeature_m_SetLocalizedText", "Set")]
    private static partial string m_SetLocalizedText { get; }
}
