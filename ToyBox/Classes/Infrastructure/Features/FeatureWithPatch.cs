namespace ToyBox;
public abstract class FeatureWithPatch : ToggledFeature {
    protected Harmony HarmonyInstance = null!;
    protected virtual string HarmonyName {
        get {
            return $"ToyBox.Feature.{Name}";
        }
    }

    public FeatureWithPatch() {
        HarmonyInstance = new(HarmonyName);
    }
    public void Patch() {
        if (IsEnabled) {
            ToyBoxPatchCategoryAttribute.PatchCategory(HarmonyName, HarmonyInstance);
        }
    }
    public void Unpatch() => HarmonyInstance.UnpatchAll(HarmonyName);
    public override void Initialize() => Patch();
    public override void Destroy() => Unpatch();
}
