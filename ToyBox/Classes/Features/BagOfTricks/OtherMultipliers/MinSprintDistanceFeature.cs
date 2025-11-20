using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.OtherMultipliers.MinSprintDistanceFeature")]
public partial class MinSprintDistanceFeature : FeatureWithPatch {
    private int? m_OriginalMinSprintDistance;
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MinSprintDistanceFeature_Name", "Min Sprint distance")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MinSprintDistanceFeature_Description", "Adjusts how far of your character you have to click and still cause your character to spring. If this area overlaps with walk distance then this has priority.")]
    public override partial string Description { get; }
    private bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.MinSprintDistanceSetting.HasValue;
            return ref m_IsEnabled;
        }
    }
    public override void Initialize() {
        base.Initialize();
        if (IsEnabled && m_OriginalMinSprintDistance.HasValue) {
            BlueprintRoot.Instance.MinSprintDistance = Settings.MinSprintDistanceSetting!.Value;
        }
    }
    public override void Destroy() {
        base.Destroy();
        if (m_OriginalMinSprintDistance.HasValue) {
            BlueprintRoot.Instance.MinSprintDistance = m_OriginalMinSprintDistance.Value;
        }
    }
    public override void OnGui() {
        if (!m_OriginalMinSprintDistance.HasValue) {
            var maybeCached = BlueprintRootReferenceHelper.RootRef.Cached as BlueprintRoot;
            if (maybeCached != null) {
                m_OriginalMinSprintDistance = maybeCached.MinSprintDistance;
            } else {
                return;
            }
        }
        var tmp = Settings.MinSprintDistanceSetting ?? m_OriginalMinSprintDistance.Value;
        using (HorizontalScope()) {
            if (UI.LogSlider(ref tmp, 0, 1000, m_OriginalMinSprintDistance.Value, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MinWidth(150))) {
                if (tmp == m_OriginalMinSprintDistance.Value) {
                    Settings.MinSprintDistanceSetting = null;
                    Destroy();
                } else {
                    Settings.MinSprintDistanceSetting = tmp;
                    Initialize();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.OtherMultipliers.MinSprintDistanceFeature";
        }
    }
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void InitializePatch() {
        var feature = GetInstance<MinSprintDistanceFeature>();
        feature.m_OriginalMinSprintDistance = BlueprintRoot.Instance.MinSprintDistance;
        if (feature.IsEnabled) {
            BlueprintRoot.Instance.MinSprintDistance = Settings.MinSprintDistanceSetting!.Value;
        }
    }
}
