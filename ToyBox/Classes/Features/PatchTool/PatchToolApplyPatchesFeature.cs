using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using ToyBox.Features.PatchTool.Infrastructure;

namespace ToyBox.Features.PatchTool;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PatchTool.PatchToolApplyPatchesFeature")]
public partial class PatchToolApplyPatchesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ApplyBlueprintPatchesOnLoad;
        }
    }
    [LocalizedString("ToyBox_Features_PatchTool_PatchToolApplyPatchesFeature_Name", "Apply blueprint patches on load")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PatchTool_PatchToolApplyPatchesFeature_Description", "When enabled, blueprint patches created with the Patch Tool are applied when the game loads its blueprints. Disable to run with unmodified blueprints without deleting your patches.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PatchTool.PatchToolApplyPatchesFeature";
        }
    }
    public override void Enable() {
        base.Enable();
        if (ResourcesLibrary.BlueprintsCache.m_resourceReplacementProvider != null) {
            Main.ScheduleForMainThread(() => {
                try {
                    Patcher.PatchAll();
                } catch (Exception ex) {
                    Error($"Failed to apply blueprint patches on enable:\n{ex}");
                }
            });
        }
    }
    public override void Disable() {
        base.Disable();
        Main.ScheduleForMainThread(() => {
            try {
                foreach (var guid in Patcher.AppliedPatches.Keys.ToList()) {
                    Patcher.RestoreOriginal(guid);
                }
            } catch (Exception ex) {
                Error($"Failed to restore blueprints on disable:\n{ex}");
            }
        });
    }

    #region Patches
    [HarmonyPatch(typeof(StartGameLoader), nameof(StartGameLoader.LoadPackTOC)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
    private static void LoadPackTOC_Postfix() {
        try {
            Log("Applying blueprint patches after LoadPackTOC.");
            Patcher.PatchAll();
        } catch (Exception ex) {
            Error($"Failed to apply blueprint patches:\n{ex}");
        }
    }
    #endregion
}
