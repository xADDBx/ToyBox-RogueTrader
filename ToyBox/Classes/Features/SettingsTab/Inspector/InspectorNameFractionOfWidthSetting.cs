namespace ToyBox.Features.SettingsTab.Inspector;

[IsTested]
public partial class InspectorNameFractionOfWidthSetting : FeatureWithFloatSlider {
    public override bool IsEnabled {
        get {
            return true;
        }
    }

    public override ref float Value {
        get {
            return ref Settings.InspectorNameFractionOfWidth;
        }
    }

    public override float Min {
        get {
            return 0.01f;
        }
    }

    public override float Max {
        get {
            return 0.99f;
        }
    }

    public override float? Default {
        get {
            return 0.3f;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorNameFractionOfWidthSetting_Name", "Name section relative width")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorNameFractionOfWidthSetting_Description", "The fraction of the screen width the name part of inspector takes (0.3 means 30%).")]
    public override partial string Description { get; }
}
