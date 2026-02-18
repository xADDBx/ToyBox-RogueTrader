using ToyBox.Infrastructure.Inspector;

namespace ToyBox.Features.SettingsTab.Inspector;

[IsTested]
public partial class InspectorShowStaticMembersSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleInspectorShowStaticMembers;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowStaticMembersSetting_Name", "Show static fields and properties")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowStaticMembersSetting_Description", "Whether to show static members of types.")]
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
