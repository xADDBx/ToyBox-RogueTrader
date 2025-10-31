using ToyBox.Features.BagOfTricks.Cheats;
using ToyBox.Features.BagOfTricks.Combat;
using ToyBox.Features.BagOfTricks.QualityOfLife;

namespace ToyBox.Features.BagOfTricks;
public partial class BagOfTricksFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_BagOfTricksText", "Bag of Tricks")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_QualityOfLifeText", "Quality of Life")]
    private static partial string m_QualityOfLifeText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_CheatsText", "Cheats")]
    private static partial string m_CheatsText { get; }

    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_CombatLocalizedText", "Combat")]
    private static partial string m_CombatLocalizedText { get; }
    public BagOfTricksFeatureTab() {
        AddFeature(new RestAllFeature(), m_CombatLocalizedText);
        AddFeature(new RestSelectedFeature(), m_CombatLocalizedText);
        AddFeature(new ImmortalityBuffFeature(), m_CombatLocalizedText);
        AddFeature(new RemoveBuffsFeature(), m_CombatLocalizedText);
        AddFeature(new KillAllEnemiesFeature(), m_CombatLocalizedText);
        AddFeature(new LobotomizeEnemiesFeature(), m_CombatLocalizedText);
        AddFeature(new MurderHoboFeature(), m_CombatLocalizedText);

        AddFeature(new EnableAchievementsFeature(), m_QualityOfLifeText);

        AddFeature(new HighlightHiddenObjectsFeature(), m_CheatsText);
    }
}
