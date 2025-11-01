using Kingmaker;
using Kingmaker.Utility.DotNetExtensions;
using System.Diagnostics;

namespace ToyBox.Infrastructure;
public static partial class LazyInit {
    internal static Stopwatch Stopwatch = new();
    public static void EnsureFinish() {
#if DEBUG
        Task.Run(() => {
            Task.WaitAll([.. Main.LateInitTasks]);
            Debug($"Lazy Init finished after {Stopwatch.ElapsedMilliseconds}ms");
        });
#endif
        var original = AccessTools.Method(typeof(GameMainMenu), nameof(GameMainMenu.Awake));
        var patch = AccessTools.Method(typeof(LazyInit), nameof(LazyInit.GameMainMenu_Awake_Postfix));
        Main.HarmonyInstance.Patch(original, postfix: new(patch));
    }
    public static void GameMainMenu_Awake_Postfix() {
        Debug($"Lazy init had {Stopwatch.ElapsedMilliseconds}ms before waiting");
        var sw = Stopwatch.StartNew();
        Task.WaitAll([.. Main.LateInitTasks]);
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
