using ToyBox.Infrastructure.Inspector;

namespace ToyBox.Features.SettingsTab.Inspector;

[IsTested]
public partial class InspectorShowCompilerGeneratedFieldsSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleInspectorShowCompilerGeneratedFields;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowCompilerGeneratedFields_Name", "Show compiler generated fields")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowCompilerGeneratedFields_Description", "Show fields that are marked as compiler generated in Inspector (e.g. backing fields of properties)")]
    public override partial string Description { get; }
    public override void Enable() {
        base.Enable();
        InspectorUI.RebuildCurrent();
    }
    public override void Disable() {
        base.Disable();
        InspectorUI.RebuildCurrent();
    }
}
