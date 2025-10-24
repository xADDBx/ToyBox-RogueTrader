namespace ToyBox.Features.SettingsTab.Inspector;

public partial class InspectorShowNullAndEmptyMembersSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleInspectorShowNullAndEmptyMembers;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowNullAndEmptyMembersSetting_Name", "Show members that are null or empty")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Inspector_InspectorShowNullAndEmptyMembersSetting_Description", "Shows Fields/Properties that are either null or an empty collection")]
    public override partial string Description { get; }
}
