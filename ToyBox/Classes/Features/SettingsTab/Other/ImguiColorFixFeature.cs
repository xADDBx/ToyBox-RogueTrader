using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.Features.SettingsTab.Other;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsTab.Other.ImguiColorFixFeature")]
public partial class ImguiColorFixFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableImguiColorFix;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_ImguiColorFixFeature_Name", "Fix IMGUI Colors")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_ImguiColorFixFeature_Description", "A Unity Engine Update somehow switched the Color Space of the Mod UI, making colors look washed out. This is a bandaid fix for that issue.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.SettingsTab.Other.ImguiColorFixFeature";
        }
    }
    private static Color m_Cache;
    private static Color m_Cache2;
    private static Color m_Cache3;
    private static Color m_Cache4;
    private static Color m_Cache5;
    private static Color m_Cache6;
    private static Color m_Cache7;
    [HarmonyPatch(typeof(UnityModManager.UI), nameof(UnityModManager.UI.DrawTab)), HarmonyPrefix]
    private static void UnityModManager_UI_DrawTab_PrePatch() {
        m_Cache = GUI.skin.label.normal.textColor;
        m_Cache2 = GUI.skin.toggle.normal.textColor;
        m_Cache3 = GUI.skin.box.normal.textColor;
        m_Cache4 = GUI.skin.button.normal.textColor;
        m_Cache5 = GUI.skin.verticalSlider.normal.textColor;
        m_Cache6 = GUI.skin.textArea.normal.textColor;
        m_Cache7 = GUI.skin.textField.normal.textColor;
        if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
            GUI.skin.label.normal.textColor = m_Cache.gamma;
            GUI.skin.toggle.normal.textColor = m_Cache2.gamma;
            GUI.skin.box.normal.textColor = m_Cache3.gamma;
            GUI.skin.button.normal.textColor = m_Cache4.gamma;
            GUI.skin.verticalSlider.normal.textColor = m_Cache5.gamma;
            GUI.skin.textArea.normal.textColor = m_Cache6.gamma;
            GUI.skin.textField.normal.textColor = m_Cache7.gamma;
        }
    }
    [HarmonyPatch(typeof(UnityModManager.UI), nameof(UnityModManager.UI.DrawTab)), HarmonyPostfix]
    private static void UnityModManager_UI_DrawTab_PostPatch() {
        if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
            GUI.skin.label.normal.textColor = m_Cache;
            GUI.skin.toggle.normal.textColor = m_Cache2;
            GUI.skin.box.normal.textColor = m_Cache3;
            GUI.skin.button.normal.textColor = m_Cache4;
            GUI.skin.verticalSlider.normal.textColor = m_Cache5;
            GUI.skin.textArea.normal.textColor = m_Cache6;
            GUI.skin.textField.normal.textColor = m_Cache7;
        }
    }
}
