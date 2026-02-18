namespace ToyBox;

public abstract class FeatureWithPatch : ToggledFeature {
    protected Harmony HarmonyInstance = null!;
    protected bool IsPatched = false;
    protected virtual string HarmonyName {
        get {
            return $"ToyBox.Feature.{Name}";
        }
    }

    protected FeatureWithPatch() {
        HarmonyInstance = new(HarmonyName);
    }
    public void Patch() {
        if (IsEnabled && !IsPatched) {
            ToyBoxPatchCategoryAttribute.PatchCategory(HarmonyName, HarmonyInstance);
            IsPatched = true;
        }
    }
    public void Unpatch() {
        if (IsPatched) {
            HarmonyInstance.UnpatchAll(HarmonyName);
            IsPatched = false;
        }
    }

    public override void Enable() {
        Patch();
    }

    public override void Disable() {
        Unpatch();
    }
}
