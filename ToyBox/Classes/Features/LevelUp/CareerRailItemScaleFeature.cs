using Kingmaker.UI.MVVM.View.ServiceWindows.CharacterInfo.Sections.Careers.Common.CareerPathProgression.Items;
using UnityEngine;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// The career progression rail sizes every per-rank selection/feature widget
/// (the small circles) via RankEntryItemCommonView.CorrectItemSize:
/// defaultRadius * sizeReduceCoeff^index, applied once when the widget is
/// added. With multiplied picks a rank can hold many widgets and the rail
/// gets crowded; this toggle scales every widget to 25% of its vanilla size
/// (the vanilla per-index taper is preserved).
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.CareerRailItemScaleFeature")]
public partial class CareerRailItemScaleFeature : FeatureWithPatch {
    private const float Scale = 0.25f;

    public override ref bool IsEnabled {
        get {
            return ref Settings.ShrinkCareerRailItems;
        }
    }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.CareerRailItemScaleFeature";
        }
    }

    [LocalizedString("ToyBox_Features_LevelUp_CareerRailItemScaleFeature_Name", "Shrink Career Rail Items")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_CareerRailItemScaleFeature_Description", "Scales the per-rank selection circles on the career progression rail to 25% of their vanilla size (the vanilla taper is preserved). Useful when multiplied picks crowd the rail. Applies when the character screen is reopened.")]
    public override partial string Description { get; }

    [HarmonyPatch(typeof(RankEntryItemCommonView), nameof(RankEntryItemCommonView.CorrectItemSize)), HarmonyPostfix]
    private static void CorrectItemSize_Postfix(GameObject item) {
        try {
            var rect = item?.GetComponent<RectTransform>();
            if (rect != null) {
                rect.sizeDelta *= Scale;
            }
        } catch (Exception ex) {
            Warn($"CareerRailItemScale: {ex.Message}");
        }
    }
}
