namespace ToyBox.Features.SettingsTab.Inspector;

[IsTested]
public partial class InspectorIndentWidthSetting : FeatureWithFloatSlider {
    public override bool IsEnabled {
        get {
            return true;
        }
    }

    public override ref float Value {
        get {
            return ref Settings.InspectorIndentWidth;
        }
    }

    public override float Min {
        get {
            return 0f;
        }
    }

    public override float Max {
        get {
            return 200f;
        }
    }

    public override float? Default {
        get {
            return 20f;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorIndentWidthSetting_Name", "Indent Width")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorIndentWidthSetting_Description", "Amount of space that is indented for each nested level in the inspector")]
    public override partial string Description { get; }
}
