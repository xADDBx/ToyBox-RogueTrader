namespace ToyBox.Features.SettingsFeatures;

[IsTested]
public partial class LogLevelSetting : ModFeature {
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogLevelSetting_Name", "Log Level")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogLevelSetting_Description", "Decides what type of messages get written to the log file.")]
    public override partial string Description { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                Space(27);
                UI.Label(Name);
                Space(10);
                UI.Label(Description.Green());
                Space(10);
                _ = UI.SelectionGrid(ref Settings.LogLevel, 6, (type) => type.GetLocalized(), AutoWidth());
            }
        }
    }
}
