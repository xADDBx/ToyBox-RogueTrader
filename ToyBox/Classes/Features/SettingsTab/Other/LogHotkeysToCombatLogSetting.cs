namespace ToyBox.Features.SettingsFeatures;

public partial class LogHotkeysToCombatLogSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLogHotkeysToCombatLog;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogHotkeysToCombatLogSetting_Name", "Log Hotkey Execution")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogHotkeysToCombatLogSetting_Description", "When ticked this shows ToyBox commands in the combat log which is helpful for you to know when you used the shortcut.")]
    public override partial string Description { get; }
}
