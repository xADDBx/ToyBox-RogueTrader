using ToyBox.Infrastructure.Inspector;

namespace ToyBox.Features.SettingsTab.Inspector;

public partial class InspectorShowEnumerableFieldsSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleInspectorShowFieldsOnEnumerable;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowEnumerableFieldsSetting_Name", "Show fields on enumerable")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowEnumerableFieldsSetting_Description", "Whether to also show other fields besides the actual collection items of IEnumerable types.")]
    public override partial string Description { get; }
    public override void Initialize() {
        base.Initialize();
        InspectorUI.RebuildCurrent();
    }
    public override void Destroy() {
        base.Destroy();
        InspectorUI.RebuildCurrent();
    }
}
