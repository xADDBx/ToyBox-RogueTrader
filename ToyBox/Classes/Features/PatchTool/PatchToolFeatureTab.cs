namespace ToyBox.Features.PatchTool;

public partial class PatchToolFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_PatchTool_PatchToolFeatureTab_Name", "Patch Tool")]
    public override partial string Name { get; }
    public PatchToolFeatureTab() {
        AddFeature(new PatchToolApplyPatchesFeature());
    }
    public override void OnGui() {
        Feature.GetInstance<PatchToolApplyPatchesFeature>().OnGui();
        PatchToolUIManager.OnGUI();
    }
}
