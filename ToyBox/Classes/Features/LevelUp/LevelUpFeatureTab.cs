namespace ToyBox.Features.LevelUp;

public partial class LevelUpFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_LevelUp_LevelUpFeatureTab_Name", "Level Up")]
    public override partial string Name { get; }
    public LevelUpFeatureTab() {
        AddFeature(new IgnoreArchetypePrerequisitesFeature());
        AddFeature(new IgnoreTalentPrerequisitesFeature());
        AddFeature(new IgnoreStatPrerequisitesFeature());
        AddFeature(new IgnoreClassLevelsPrerequisitesFeature());
    }
}
