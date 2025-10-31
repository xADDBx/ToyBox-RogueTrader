namespace ToyBox.Features.SettingsFeatures;
[NeedsTesting]
public partial class CharacterPickerNearbyRangeSetting : FeatureWithLogIntSlider {
    [LocalizedString("ToyBox_Features_SettingsFeatures_CharacterPickerNearbyRangeSetting_Name", "Nearby Range")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_CharacterPickerNearbyRangeSetting_Description", "Modifies the range for the Nearby Character Picker category")]
    public override partial string Description { get; }
    public override bool IsEnabled {
        get {
            return true;
        }
    }

    public override ref int Value {
        get {
            return ref Settings.NearbyRange;
        }
    }

    public override int Min {
        get {
            return 1;
        }
    }

    public override int Max {
        get {
            return 100000;
        }
    }

    public override int? Default {
        get {
            return 25;
        }
    }
}
