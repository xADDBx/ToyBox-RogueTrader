using Kingmaker.Utility.BuildModeUtils;

namespace ToyBox.Features.SettingsTab.Game;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsTab.Game.EnableGameDevelopmentModeSetting")]
public partial class EnableGameDevelopmentModeSetting : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableGameDevelopmentMode;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_EnableGameDevelopmentModeSetting_Name", "Enable Game Development Mode")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_EnableGameDevelopmentModeSetting_Description", "This turns on the developer console which lets you access cheat commands, access a console and more.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.SettingsTab.Game.EnableGameDevelopmentModeSetting";
        }
    }
    [HarmonyPatch(typeof(BuildModeUtility), nameof(BuildModeUtility.IsDevelopment), MethodType.Getter), HarmonyPostfix]
    private static void BuildModeUtility_IsDevelopment_Patch(ref bool __result) {
        __result = true;
    }
    [HarmonyPatch(typeof(BuildModeUtility), nameof(BuildModeUtility.CheatsEnabled), MethodType.Getter), HarmonyPostfix]
    private static void BuildModeUtility_CheatsEnabled_Patch(ref bool __result) {
        __result = true;
    }
}
