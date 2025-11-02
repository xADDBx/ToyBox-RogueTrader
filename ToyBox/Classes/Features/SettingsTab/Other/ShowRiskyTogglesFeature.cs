namespace ToyBox.Features.SettingsTab.Other;
public partial class ShowRiskyTogglesFeature : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableShowRiskyToggles;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_ShowRiskyTogglesFeature_Name", "Display Risky Options")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_ShowRiskyTogglesFeature_Description", "ToyBox hides some options by default. Activating this will unhide those features.")]
    public override partial string Description { get; }
}
