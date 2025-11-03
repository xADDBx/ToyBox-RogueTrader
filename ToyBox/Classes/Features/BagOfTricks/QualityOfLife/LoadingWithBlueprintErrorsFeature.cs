using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Newtonsoft.Json;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.LoadingWithBlueprintErrorsFeature")]
public partial class LoadingWithBlueprintErrorsFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLoadingWithBlueprintErrors;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_LoadingWithBlueprintErrorsFeature_Name", "Enable Loading with Blueprint Errors")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_LoadingWithBlueprintErrorsFeature_Description", "This dangerous allows loading saves even with missing blueprint mods. This can potentially allow you to recover a save, though you'll have to respec at minimum.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.LoadingWithBlueprintErrorsFeature";
        }
    }
    [HarmonyPatch(typeof(BlueprintConverter), nameof(BlueprintConverter.ReadJson)), HarmonyPrefix]
    private static bool BlueprintConverter_ReadJson_Patch(ref object? __result, JsonReader reader) {
        var text = reader.Value as string;
        if (string.IsNullOrEmpty(text) || text == "null") {
            __result = null; // We still can't look up a blueprint without a valid id
            return false;
        }
        SimpleBlueprint? retrievedBlueprint;
        try {
            retrievedBlueprint = ResourcesLibrary.TryGetBlueprint(text);
        } catch {
            retrievedBlueprint = null;
        }
        if (retrievedBlueprint == null) {
            Warn($"Failed to load blueprint by guid '{text}' but continued with null blueprint.");
            OwlLog($"Failed to load blueprint by guid '{text}' but continued with null blueprint.");
        }
        __result = retrievedBlueprint;

        return false;
    }
}
