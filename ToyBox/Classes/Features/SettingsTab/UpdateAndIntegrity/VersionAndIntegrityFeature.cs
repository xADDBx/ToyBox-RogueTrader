﻿using Kingmaker;
using UnityModManagerNet;

namespace ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsFeatures.UpdateAndIntegrity.VersionCompatabilityFeature")]
public partial class VersionCompatabilityFeature : FeatureWithPatch, INeedEarlyInitFeature {
    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_VersionCompatabilityFeature_Name", "Enable Version Compatibility Check")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_VersionCompatabilityFeature_Description", "Check whether the current mod version is compatible with the current game version")]
    public override partial string Description { get; }

    public override ref bool IsEnabled => ref Settings.EnableVersionCompatibilityCheck;

    protected override string HarmonyName => "ToyBox.Features.SettingsFeatures.UpdateAndIntegrity.VersionCompatabilityFeature";
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake)), HarmonyPrefix]
    private static void MainMenu_Awake_Prefix() {
        if (VersionChecker.ResultOfCheck.HasValue && !VersionChecker.ResultOfCheck.Value) {
            Main.ModEntry.Info.DisplayName = "ToyBox ".Yellow().SizePercent(20) + ModVersionIsNotCompatibleWithThi.Red().Bold().SizePercent(40);
            Main.ModEntry.mErrorOnLoading = true;
            Main.ModEntry.OnGUI = _ => UpdaterFeature.UpdaterGUI();
        }
    }
    [HarmonyPatch(typeof(UnityModManager.UI), MethodType.Constructor), HarmonyPostfix]
    private static void UnityModManager_UI_Postfix(UnityModManager.UI __instance) {
        if (VersionChecker.ResultOfCheck.HasValue && !VersionChecker.ResultOfCheck.Value) {
            __instance.ShowModSettings = UnityModManager.modEntries.FindIndex(mod => mod == Main.ModEntry);
            Main.ModEntry.OnUnload(Main.ModEntry);
        }
    }
    [LocalizedString("ToyBox_Features_SettingsFeatures_UpdateAndIntegrity_VersionCompatabilityFeature_ModVersionIsNotCompatibleWithThi", "Mod Version is not compatible with this game version!")]
    private static partial string ModVersionIsNotCompatibleWithThi { get; }
}
