using Kingmaker;
using Kingmaker.UI.Legacy.MainMenuUI;
using System.Reflection;
using System.Reflection.Emit;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;
[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.SkipSplashScreenFeature")]
public partial class SkipSplashScreenFeature : FeatureWithPatch, INeedEarlyInitFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableSkipSplashScreen;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_SkipSplashScreenFeature_Name", "Skip Splash Screen")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_SkipSplashScreenFeature_Description", "This skips the splash screen that appears when the game starts. Helpful if you need to frequently restart the game.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.SkipSplashScreenFeature";
        }
    }
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> PatchTargets() {
        yield return AccessTools.Method(typeof(SplashScreenController), nameof(SplashScreenController.ShowSplashScreen));
        yield return AccessTools.Method(typeof(MainMenuLoadingScreen), nameof(MainMenuLoadingScreen.OnStart));
    }
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> instructions) {
        foreach (var inst in instructions) {
            if (inst.Calls(AccessTools.Method(typeof(GameStarter), nameof(GameStarter.IsSkippingMainMenu)))) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(inst.labels);
            } else if (inst.LoadsConstant("Logo Show Requested")) {
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call((string _, MainMenuLoadingScreen screen) => Helper(_, screen));
                yield return new(OpCodes.Ret);
                break;
            } else {
                yield return inst;
            }
        }
    }
    private static void Helper(string _, MainMenuLoadingScreen screen) {
        screen.gameObject.SetActive(false);
        GameStarter.Instance.StartGame();
    }
}
