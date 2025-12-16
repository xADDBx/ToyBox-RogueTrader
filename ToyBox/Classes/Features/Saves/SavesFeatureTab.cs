using ToyBox.Features.BagOfTricks.QualityOfLife;

namespace ToyBox.Features.Saves;

public partial class SavesFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_Saves_SavesFeatureTab_Name", "Saves")]
    public override partial string Name { get; }
    public SavesFeatureTab() {
        AddFeature(new ChangeGameIdFeature());
        AddFeature(new BrowseSavesFeature());
    }
    public override void OnGui() {
        Feature.GetInstance<AutoLoadLastSaveOnLaunchFeature>().OnGui();
        base.OnGui();
    }
}
