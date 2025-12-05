namespace ToyBox.Features.SettingsFeatures.Blueprints;

[IsTested]
public partial class ShowBlueprintTypeSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleShowBlueprintType;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowBlueprintTypeSetting_Name", "Show Blueprint Type")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowBlueprintTypeSetting_Description", "Whether Browsers should display the type of a blueprint.")]
    public override partial string Description { get; }
}
