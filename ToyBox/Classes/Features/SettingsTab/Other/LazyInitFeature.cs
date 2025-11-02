using Kingmaker;
using Kingmaker.Utility.DotNetExtensions;
using System.Diagnostics;

namespace ToyBox.Features.SettingsTab.Other;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsTab.Other.LazyInitFeature")]
public partial class LazyInitFeature : FeatureWithPatch, INeedEarlyInitFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLazyInit;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_LazyInitFeature_Name", "Lazy Init")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_LazyInitFeature_Description", "The mod tries to speed up initial game load time by only doing important initialization on the main thread and doing the rest on a separate thread.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.SettingsTab.Other.LazyInitFeature";
        }
    }
    public static Stopwatch Stopwatch = new();
    public override void Initialize() {
        base.Initialize();
#if DEBUG
        if (IsEnabled) {
            Task.Run(() => {
                Task.WaitAll([.. Main.LateInitTasks]);
                Debug($"Lazy Init finished after {Stopwatch.ElapsedMilliseconds}ms");
            });
        }
#endif
    }
    [HarmonyPatch(typeof(GameMainMenu), nameof(GameMainMenu.Awake)), HarmonyPostfix]
    private static void GameMainMenu_Awake_Postfix() {
        EnsureFinished();
    }
    public static void EnsureFinished() {
        Debug($"Lazy init had {Stopwatch.ElapsedMilliseconds}ms before waiting");
        var sw = Stopwatch.StartNew();
        if (Main.LateInitTasks.Count > 0) {
            Task.WaitAll([.. Main.LateInitTasks]);
        }
        Main.LateInitTasks.Where(t => t.IsFaulted).ForEach(t => {
            Critical($"Late init task IsFaulted: {t}\n{t.Exception?.ToString() ?? "Null Exception?"}");
        });
        Main.SuccessfullyInitialized = true;
        Debug($"Waited {sw.ElapsedMilliseconds}ms for lazy init finish");

        if (FeatureTab.FailedFeatures.Count > 0) {
            Main.ModEntry.Info.DisplayName += ($" {FeatureTab.FailedFeatures.Count} " + m_FeaturesFailedInitialization_LocalizedText).Orange().Bold();
            ToggleModWindow();
        }
    }
    [LocalizedString("ToyBox_Infrastructure_LazyInit_X_Amount_Of_FeaturesFailedInitialization_LocalizedText", "features failed initialization!")]
    private static partial string m_FeaturesFailedInitialization_LocalizedText { get; }
}
