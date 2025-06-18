﻿namespace ToyBox.Features.SettingsTab.Inspector;

public partial class InspectorSlimModeSetting : ToggledFeature {
    public override ref bool IsEnabled => ref Settings.ToggleInspectorSlimMode;
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorSlimModeSetting_Name", "Slim Mode")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorSlimModeSetting_Description", "If you hate whitespace and alignment")]
    public override partial string Description { get; }
}
