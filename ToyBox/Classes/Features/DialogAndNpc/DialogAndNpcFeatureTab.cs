namespace ToyBox.Features.DialogAndNpc;

public partial class DialogAndNpcFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogAndNpcFeatureTab_Name", "Dialog & NPCs")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogAndNpcFeatureTab_m_MapAndNamesText", "Map & Names")]
    private static partial string m_MapAndNamesText { get; }
    public DialogAndNpcFeatureTab() {
        AddFeature(new DialogEditorFeature());
        AddFeature(new InspectDialogControllerFeature());
        AddFeature(new InterestingNpcsFeature());
        AddFeature(new EnhancedMap.EnhancedMapToggleFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.LocalMapNpcMarkersFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.LocalMapExitMarkersFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.LocalMapInteractableMarkersFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.OvertipNameColorsFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.StarMapPlanetColorsFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.HighlightEnhancementFeature(), m_MapAndNamesText);
        AddFeature(new EnhancedMap.LocalMapOverlayFeature(), m_MapAndNamesText);
    }
}
