namespace ToyBox.Features.DialogAndNpc;

public partial class DialogAndNpcFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogAndNpcFeatureTab_Name", "Dialog & NPCs")]
    public override partial string Name { get; }
    public DialogAndNpcFeatureTab() {
        AddFeature(new InspectDialogControllerFeature());
    }
}
