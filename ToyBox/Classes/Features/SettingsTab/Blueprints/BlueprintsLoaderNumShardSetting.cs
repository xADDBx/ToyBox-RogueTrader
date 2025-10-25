namespace ToyBox.Features.SettingsFeatures.Blueprints;

public partial class BlueprintsLoaderNumShardSetting : FeatureWithIntSlider {
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BlueprintsLoaderNumShardSetting_Name", "Amount of Shards")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BlueprintsLoaderNumShardSetting_Description", "This affects the amount of dictionaries that will be used to reduce synchronization overhead when threaded loading.")]
    public override partial string Description { get; }
    public override bool IsEnabled {
        get {
            return true;
        }
    }

    public override ref int Value {
        get {
            return ref Settings.BlueprintsLoaderNumShards;
        }
    }

    public override int Min {
        get {
            return 1;
        }
    }

    public override int Max {
        get {
            return 4096;
        }
    }

    public override int? Default {
        get {
            return 32;
        }
    }
    public override bool ShouldHide {
        get {
            return !GetInstance<ThreadedBlueprintsLoaderSetting>().IsEnabled;
        }
    }
}
