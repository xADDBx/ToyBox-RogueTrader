using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.OtherMultipliers.MaxWalkDistanceFeature")]
public partial class MaxWalkDistanceFeature : FeatureWithPatch {
    private int? m_OriginalMaxWalkDistance;
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MaxWalkDistanceFeature_Name", "Max Walk distance")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_MaxWalkDistanceFeature_Description", "Adjusts how far of your character you can click and still cause your character to walk instead of run.")]
    public override partial string Description { get; }
    private bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.MaxWalkDistanceSetting.HasValue;
            return ref m_IsEnabled;
        }
    }
    public override void Initialize() {
        base.Initialize();
        if (IsEnabled && m_OriginalMaxWalkDistance.HasValue) {
            BlueprintRoot.Instance.MaxWalkDistance = Settings.MaxWalkDistanceSetting!.Value;
        }
    }
    public override void Destroy() {
        base.Destroy();
        if (m_OriginalMaxWalkDistance.HasValue) {
            BlueprintRoot.Instance.MaxWalkDistance = m_OriginalMaxWalkDistance.Value;
        }
    }
    public override void OnGui() {
        if (!m_OriginalMaxWalkDistance.HasValue) {
            var maybeCached = BlueprintRootReferenceHelper.RootRef.Cached as BlueprintRoot;
            if (maybeCached != null) {
                m_OriginalMaxWalkDistance = maybeCached.MaxWalkDistance;
            } else {
                return;
            }
        }
        var tmp = Settings.MaxWalkDistanceSetting ?? m_OriginalMaxWalkDistance.Value;
        using (HorizontalScope()) {
            if (UI.LogSlider(ref tmp, 0, 1000, m_OriginalMaxWalkDistance.Value, null, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
                if (tmp == m_OriginalMaxWalkDistance.Value) {
                    Settings.MaxWalkDistanceSetting = null;
                    Destroy();
                } else {
                    Settings.MaxWalkDistanceSetting = tmp;
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
            return "ToyBox.Features.BagOfTricks.OtherMultipliers.MaxWalkDistanceFeature";
        }
    }
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void InitializePatch() {
        var feature = GetInstance<MaxWalkDistanceFeature>();
        feature.m_OriginalMaxWalkDistance = BlueprintRoot.Instance.MaxWalkDistance;
        if (feature.IsEnabled) {
            BlueprintRoot.Instance.MaxWalkDistance = Settings.MaxWalkDistanceSetting!.Value;
        }
    }
}
