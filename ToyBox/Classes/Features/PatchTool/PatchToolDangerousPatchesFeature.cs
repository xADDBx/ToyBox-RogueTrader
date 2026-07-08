namespace ToyBox.Features.PatchTool;

public partial class PatchToolDangerousPatchesFeature : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableDangerousPatchToolPatches;
        }
    }
    [LocalizedString("ToyBox_Features_PatchTool_PatchToolDangerousPatchesFeature_Name", "Allow dangerous PatchTool patches")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PatchTool_PatchToolDangerousPatchesFeature_Description", "Master switch for dangerous patches. These edit shared prefabs/GameObjects globally and cannot be recommended. Off by default; enable only if you know what you're doing.")]
    public override partial string Description { get; }
}
