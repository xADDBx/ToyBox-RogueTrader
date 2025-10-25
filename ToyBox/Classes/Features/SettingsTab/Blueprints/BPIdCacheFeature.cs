namespace ToyBox.Features.SettingsFeatures.Blueprints;
[NeedsTesting]
public partial class BPIdCacheFeature : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.UseBPIdCache;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BPIdCacheFeature_Name", "Enable BlueprintId Cache")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BPIdCacheFeature_Description", "When enabled, ToyBox will create a cache for ids of different blueprint types. Certain menus like Search 'n Pick will then try to load only blueprints of the specified type (e.g. BlueprintFeature), thereby speeding up load times.")]
    public override partial string Description { get; }
    public override void OnGui() {
        using (VerticalScope()) {
            base.OnGui();
            _ = UI.Button(m_ResetCacheinCaseOfIssuesText, BlueprintIdCache.Delete);
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BPIdCacheFeature_ResetCache_inCaseOfIssues_Text", "Reset BlueprintId Cache (in case of issues)")]
    private static partial string m_ResetCacheinCaseOfIssuesText { get; }
}
