namespace ToyBox.Features.SettingsFeatures;
[NeedsTesting]
public partial class LogHotkeysToCombatLog : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLogHotkeysToCombatLog;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogHotkeysToCombatLog_Name", "Log Hotkey Execution")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LogHotkeysToCombatLog_Description", "When ticked this shows ToyBox commands in the combat log which is helpful for you to know when you used the shortcut.")]
    public override partial string Description { get; }
}
