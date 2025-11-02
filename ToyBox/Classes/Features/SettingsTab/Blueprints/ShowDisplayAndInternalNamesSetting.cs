namespace ToyBox.Features.SettingsFeatures.Blueprints;

[IsTested]
public partial class ShowDisplayAndInternalNamesSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleBPsShowDisplayAndInternalName;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowDisplayAndInternalNamesSetting_Name", "Show Display And Internal Names")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowDisplayAndInternalNamesSetting_Description", "Blueprints have both internal names and display names. By default, ToyBox will try to show the Display name and fall back to the Internal one in case of issues. This feature will display both at the same time.")]
    public override partial string Description { get; }
    public override void Initialize() {
        base.Initialize();
        BPHelper.ClearNameCaches();
    }
    public override void Destroy() {
        base.Destroy();
        BPHelper.ClearNameCaches();
    }
}
