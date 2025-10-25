using ToyBox.Features.SettingsFeatures.Blueprints;
using ToyBox.Features.SettingsFeatures.BrowserSettings;
using ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;
using ToyBox.Features.SettingsTab.Inspector;

namespace ToyBox.Features.SettingsFeatures;
public partial class SettingsFeaturesTab : FeatureTab {
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_UpdateText", "Update")]
    private static partial string m_UpdateText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_VersionAndFileIntegrityCategory", "Version and File Integrity")]
    private static partial string m_VersionAndFileIntegrityText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_BlueprintsCategory", "Blueprints")]
    private static partial string m_BlueprintsText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_LanguageCategory", "Language")]
    private static partial string m_LanguageText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_ListsAndBrowsersText", "Lists and Browsers")]
    private static partial string m_ListsAndBrowsersText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_InspectorText", "Inspector")]
    private static partial string m_InspectorText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_OtherText", "Other")]
    private static partial string m_OtherText { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_SettingsFeaturesTab_SettingsText", "Settings")]
    public override partial string Name { get; }
    public SettingsFeaturesTab() {
        AddFeature(new UpdaterFeature(), m_UpdateText);

        AddFeature(new PageLimitSetting(), m_ListsAndBrowsersText);
        AddFeature(new SearchAsYouTypeFeature(), m_ListsAndBrowsersText);
        AddFeature(new SearchDelaySetting(), m_ListsAndBrowsersText);

        AddFeature(new IntegrityCheckerFeature(), m_VersionAndFileIntegrityText);
        AddFeature(new VersionCompatabilityFeature(), m_VersionAndFileIntegrityText);

        AddFeature(new PerformanceEnhancementFeatures(), m_BlueprintsText);
        AddFeature(new PreloadBlueprintsFeature(), m_BlueprintsText);
        AddFeature(new ShowDisplayAndInternalNamesSetting(), m_BlueprintsText);
        AddFeature(new ThreadedBlueprintsLoaderSetting(), m_BlueprintsText);
        AddFeature(new BlueprintsLoaderNumThreadSetting(), m_BlueprintsText);
        AddFeature(new BlueprintsLoaderNumShardSetting(), m_BlueprintsText);
        AddFeature(new BlueprintsLoaderChunkSizeSetting(), m_BlueprintsText);
        AddFeature(new BPIdCacheFeature(), m_BlueprintsText);

        AddFeature(new InspectorShowNullAndEmptyMembersSetting(), m_InspectorText);
        AddFeature(new InspectorShowEnumerableFieldsSetting(), m_InspectorText);
        AddFeature(new InspectorShowStaticMembersSetting(), m_InspectorText);
        AddFeature(new InspectorShowCompilerGeneratedFieldsSetting(), m_InspectorText);
        AddFeature(new InspectorSlimModeSetting(), m_InspectorText);
        AddFeature(new InspectorSearcherBatchSizeSetting(), m_InspectorText);
        AddFeature(new InspectorDrawLimitSetting(), m_InspectorText);
        AddFeature(new InspectorIndentWidthSetting(), m_InspectorText);
        AddFeature(new InspectorNameFractionOfWidthSetting(), m_InspectorText);

        AddFeature(new LogLevelSetting(), m_OtherText);

        AddFeature(new LanguagePickerFeature(), m_LanguageText);
    }

}
