namespace ToyBox.Features.Etudes;

public partial class LegacyEtudesEditorFeature : Feature {
    [LocalizedString("ToyBox_Features_Etudes_LegacyEtudesEditorFeature_Name", "Etudes Editor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Etudes_LegacyEtudesEditorFeature_Description", "Allows inspecting the games Etudes/Flags/Elements in a tree view.")]
    public override partial string Description { get; }

    public override void OnGui() {
        UI.Label(m_ThisFeatureIsAtTheMomentMostlyCoLocalizedText.Orange().Bold());
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        EtudesEditor.OnGUI();
    }

    [LocalizedString("ToyBox_Features_Etudes_LegacyEtudesEditorFeature_m_ThisFeatureIsAtTheMomentMostlyCoLocalizedText", "This feature is at the moment mostly copied from previous ToyBox, so there might be issues. It also does not yet support localization. The UI will get a complete rewrite eventually.")]
    private static partial string m_ThisFeatureIsAtTheMomentMostlyCoLocalizedText { get; }
}
