using ToyBox.Features.BagOfTricks.Camera;
using ToyBox.Features.BagOfTricks.Cheats;
using ToyBox.Features.BagOfTricks.Combat;
using ToyBox.Features.BagOfTricks.Common;
using ToyBox.Features.BagOfTricks.Dialog;
using ToyBox.Features.BagOfTricks.ExperienceMultipliers;
using ToyBox.Features.BagOfTricks.OtherMultipliers;
using ToyBox.Features.BagOfTricks.Preview;
using ToyBox.Features.BagOfTricks.QualityOfLife;
using ToyBox.Features.BagOfTricks.RTSpecific;
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
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_RTFactionReputationLocalizedText", "RT Faction Reputation")]
    private static partial string m_RTFactionReputationLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_RTResourcesLocalizedText", "RT Resources")]
    private static partial string m_RTResourcesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_RTTweaksLocalizedText", "RT Tweaks")]
    private static partial string m_RTTweaksLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_CameraLocalizedText", "Camera")]
    private static partial string m_CameraLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_ExperienceMultipliersLocalizedText", "Experience Multipliers")]
    private static partial string m_ExperienceMultipliersLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_BagOfTricksFeatureTab_m_OtherMultipliersLocalizedText", "Other Multipliers")]
    private static partial string m_OtherMultipliersLocalizedText { get; }
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
        AddFeature(new FixIncorrectMainCharacterFeature(), m_QualityOfLifeText);
        AddFeature(new GameTimeScaleFeature(), m_QualityOfLifeText);
        AddFeature(new GameAlternateTimeScaleFeature(), m_QualityOfLifeText);
        AddFeature(new DisableEndTurnKeybindFeature(), m_QualityOfLifeText);
        AddFeature(new LoadingWithBlueprintErrorsFeature(), m_QualityOfLifeText);

        AddFeature(new ModifyFactionReputationFeature(), m_RTFactionReputationLocalizedText);
        
        AddFeature(new ModifyResourcesFeature(), m_RTResourcesLocalizedText);

        AddFeature(new DisableRandomWarpEncounterFeature(), m_RTTweaksLocalizedText);
        AddFeature(new PreventPsychicPhenomenaFeature(), m_RTTweaksLocalizedText);
        AddFeature(new PreventVeilThicknessFromChangingFeature(), m_RTTweaksLocalizedText);
        AddFeature(new CustomizePsychicPhenomenaFeature(), m_RTTweaksLocalizedText);

        AddFeature(new AllowZoomOnAllMapsAndCutscenesFeature(), m_CameraLocalizedText);
        AddFeature(new AllowRotateOnAllMapsAndCutscenesFeature(), m_CameraLocalizedText);
        AddFeature(new FOVMultiplierFeature(), m_CameraLocalizedText);
        AddFeature(new FreeCamFeature(), m_CameraLocalizedText);
        AddFeature(new CameraElevationOffsetFeature(), m_CameraLocalizedText);
        AddFeature(new DragCameraElevationFeature(), m_CameraLocalizedText);

        AddFeature(new PreventTrapsFromTriggeringFeature(), m_CheatsText);
        AddFeature(new UnlimitedStackingOfModifiersFeature(), m_CheatsText);
        AddFeature(new HighlightHiddenObjectsFeature(), m_CheatsText);
        AddFeature(new FreeAbilitiesFeature(), m_CheatsText);
        AddFeature(new NoAbilityCooldownsFeature(), m_CheatsText);
        AddFeature(new PartialUnlimitedActionsPerTurnFeature(), m_CheatsText);
        AddFeature(new CompleteUnlimitedActionsPerTurnFeature(), m_CheatsText);
        AddFeature(new InfiniteChargesOnItemsFeature(), m_CheatsText);
        AddFeature(new IgnoreEquipmentRestrictionsFeature(), m_CheatsText);
        AddFeature(new RestoreSpellsAndSkillsAfterCombatFeature(), m_CheatsText);
        AddFeature(new InstantRestAfterCombatFeature(), m_CheatsText);
        AddFeature(new EquipmentChangeDuringCombatFeature(), m_CheatsText);
        AddFeature(new InventoryItemUseDuringCombatFeature(), m_CheatsText);
        AddFeature(new IgnoreAllAbilityRequirementsFeature(), m_CheatsText);
        AddFeature(new IgnoreAoeOverlapAbilityRequirementFeature(), m_CheatsText);
        AddFeature(new IgnoreLineOfSightAbilityRequirementFeature(), m_CheatsText);
        AddFeature(new IgnoreTargetTooFarAbilityRequirementFeature(), m_CheatsText);
        AddFeature(new IgnoreTargetTooCloseAbilityRequirementFeature(), m_CheatsText);

        AddFeature(new ExperienceMultiplierFeature(), m_ExperienceMultipliersLocalizedText);

        AddFeature(new MaxWalkDistanceFeature(), m_OtherMultipliersLocalizedText);
        AddFeature(new MinSprintDistanceFeature(), m_OtherMultipliersLocalizedText);
        AddFeature(new MovementSpeedMultiplierFeature(), m_OtherMultipliersLocalizedText);
    }
}
