﻿namespace ToyBox.Features.SettingsTab.Inspector;

public partial class InspectorNameFractionOfWidthSetting : FeatureWithFloatSlider {
    public override bool IsEnabled => true;
    public override ref float Value => ref Settings.InspectorNameFractionOfWidth;
    public override float Min => 0.01f;
    public override float Max => 0.99f;
    public override float? Default => 0.3f;
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorNameFractionOfWidthSetting_Name", "Name section relative width")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorNameFractionOfWidthSetting_Description", "The fraction of the screen width the name part of inspector takes (0.3 means 30%).")]
    public override partial string Description { get; }
}
