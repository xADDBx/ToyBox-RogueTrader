namespace ToyBox.Features.Quests;

public partial class QuestsFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_Quests_QuestsFeatureTab_Name", "Quests")]
    public override partial string Name { get; }
    public QuestsFeatureTab() {
        AddFeature(new QuestEditorFeature());
    }
}
