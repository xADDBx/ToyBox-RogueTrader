using System.Reflection;
using ToyBox.Infrastructure.Localization;

namespace ToyBox.Features.FeatureSearch;

public partial class FeatureSearchFeature : Feature {
    [LocalizedString("ToyBox_Features_FeatureSearch_FeatureSearchFeature_FeatureSearchNotImplementedForTh", "Feature search not implemented for this feature!")]
    private static partial string m_FeatureSearchNotImplementedForTh { get; }
    [LocalizedString("ToyBox_Features_FeatureSearch_FeatureSearchFeature_ShowGUIText", "Show GUI")]
    private static partial string m_ShowGUIText { get; }
    [LocalizedString("ToyBox_Features_FeatureSearch_FeatureSearchFeature_Name", "Default Name")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_FeatureSearch_FeatureSearchFeature_Description", "Default Description")]
    public override partial string Description { get; }
    private bool m_IsInitialized = false;
    private readonly Browser<Feature> m_FeatureBrowser = new(f => f.SortKey, f => f.SearchKey, null, null, true, (int)(EffectiveWindowWidth() / 1.03f));
    private readonly Dictionary<Feature, bool> m_DisclosureStates = [];
    private bool m_OnlyFeaturesThatNeedTesting = false;
    public override void OnGui() {
        if (!m_IsInitialized) {
            List<Feature> features = [];
            foreach (var tab in Main.m_FeatureTabs) {
                if (tab.Name != LocalizationManager.CurrentLocalization.ToyBox_Features_FeatureSearch_FeatureSearchTab_Name.Translated) {
                    features = [.. features, .. tab.GetFeatures()];
                }
            }
            m_FeatureBrowser.UpdateItems(features);
            m_IsInitialized = true;
        }
        if (UI.Toggle("Filter for untested features", null, ref m_OnlyFeaturesThatNeedTesting)) {
            List<Feature> features = [];
            foreach (var tab in Main.m_FeatureTabs) {
                if (tab.Name != LocalizationManager.CurrentLocalization.ToyBox_Features_FeatureSearch_FeatureSearchTab_Name.Translated) {
                    features = [.. features, .. tab.GetFeatures()];
                }
            }
            if (m_OnlyFeaturesThatNeedTesting) {
                features = [.. features.Where(f => f.GetType().GetCustomAttribute<NeedsTestingAttribute>() != null)];
            }
            m_FeatureBrowser.UpdateItems(features);
        }
        m_FeatureBrowser.OnGUI(feature => {
            using (VerticalScope()) {
                if (!m_DisclosureStates.TryGetValue(feature, out var showNested)) {
                    showNested = false;
                    m_DisclosureStates[feature] = showNested;
                }
                using (HorizontalScope()) {
                    if (UI.DisclosureToggle(ref showNested, m_ShowGUIText)) {
                        m_DisclosureStates[feature] = showNested;
                    }
                    Space(15);
                    UI.Label(feature.Name.Orange());
                    Space(15);
                    UI.Label(feature.Description.Yellow());
                }
                using (HorizontalScope()) {
                    if (showNested) {
                        Space(15);
                        using (VerticalScope()) {
                            if (feature is INeedContextFeature contextFeature) {
                                // Currently context needs are handled in the OnGui directly
                                feature.OnGui();
                            } else {
                                feature.OnGui();
                            }
                        }
                    }
                }
            }
        });
    }
}
