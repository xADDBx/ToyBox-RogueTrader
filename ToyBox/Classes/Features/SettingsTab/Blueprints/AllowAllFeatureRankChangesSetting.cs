namespace ToyBox.Features.SettingsFeatures.Blueprints;

public partial class AllowAllFeatureRankChangesSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleAllowAllFeatureRankChanges;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_AllowAllFeatureRankChangesSetting_Name", "Allow Rank Changes For All Features")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_AllowAllFeatureRankChangesSetting_Description", "Allows changing the rank of every feature instead of only those which have multiple ranks or scale with their rank.")]
    public override partial string Description { get; }
}
