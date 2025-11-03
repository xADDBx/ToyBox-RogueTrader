using ToyBox.Features.BagOfTricks.Cheats;
using ToyBox.Features.BagOfTricks.Combat;
using ToyBox.Features.BagOfTricks.Common;
using ToyBox.Features.BagOfTricks.Dialog;
using ToyBox.Features.BagOfTricks.Preview;
using ToyBox.Features.BagOfTricks.QualityOfLife;
using ToyBox.Features.BagOfTricks.Teleport;

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
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_TeleportLocalizedText", "Teleport")]
    private static partial string m_TeleportLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_CommonLocalizedText", "Common")]
    private static partial string m_CommonLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_PreviewLocalizedText", "Preview")]
    private static partial string m_PreviewLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_DialogLocalizedText", "Dialog")]
    private static partial string m_DialogLocalizedText { get; }
    public BagOfTricksFeatureTab() {
        AddFeature(new RestAllFeature(), m_CombatLocalizedText);
        AddFeature(new RestSelectedFeature(), m_CombatLocalizedText);
        AddFeature(new ImmortalityBuffFeature(), m_CombatLocalizedText);
        AddFeature(new RemoveBuffsFeature(), m_CombatLocalizedText);
        AddFeature(new KillAllEnemiesFeature(), m_CombatLocalizedText);
        AddFeature(new LobotomizeEnemiesFeature(), m_CombatLocalizedText);
        AddFeature(new MurderHoboFeature(), m_CombatLocalizedText);

        AddFeature(new TeleportPartyToYouFeature(), m_TeleportLocalizedText);
        AddFeature(new TeleportMainToCursorFeature(), m_TeleportLocalizedText);
        AddFeature(new TeleportSelectedToCursorFeature(), m_TeleportLocalizedText);
        AddFeature(new TeleportPartyToCursorFeature(), m_TeleportLocalizedText);

        AddFeature(new GoToGlobalMapFeature(), m_CommonLocalizedText);
        AddFeature(new ChangePartyFeature(), m_CommonLocalizedText);
        AddFeature(new RerollPerceptionFeature(), m_CommonLocalizedText);
        AddFeature(new ResetInteractablesFeature(), m_CommonLocalizedText);
        AddFeature(new ChangeWeatherFeature(), m_CommonLocalizedText);
        AddFeature(new GiveAllItemsFeature(), m_CommonLocalizedText);
        AddFeature(new OpenReputationTradeWindowFeature(), m_CommonLocalizedText);

        AddFeature(new PreviewDialogResultsFeature(), m_PreviewLocalizedText);
        AddFeature(new PreviewDialogConditionsFeature(), m_PreviewLocalizedText);

        AddFeature(new JealousyBegoneFeature(), m_DialogLocalizedText);
        AddFeature(new LoveIsFreeFeature(), m_DialogLocalizedText);
        AddFeature(new RemoteCompanionDialogFeature(), m_DialogLocalizedText);
        AddFeature(new ExCompanionDialogFeature(), m_DialogLocalizedText);
        AddFeature(new OverrideStoryOccupationFeature(), m_DialogLocalizedText);
        AddFeature(new IgnoreDialogRestrictionsSoulMarkFeature(), m_DialogLocalizedText);

        AddFeature(new EnableAchievementsFeature(), m_QualityOfLifeText);
        AddFeature(new SkipSplashScreenFeature(), m_QualityOfLifeText);
        AddFeature(new ObjectHighlightToggleFeature(), m_QualityOfLifeText);
        AddFeature(new AutoLoadLastSaveOnLaunchFeature(), m_QualityOfLifeText);
        AddFeature(new RefillBeltConsumablesFeature(), m_QualityOfLifeText);
        AddFeature(new ClickToTransferEntireStackFeature(), m_QualityOfLifeText);

        AddFeature(new HighlightHiddenObjectsFeature(), m_CheatsText);
    }
}
