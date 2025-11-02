namespace ToyBox.Features.BagOfTricks.Dialog;
public partial class ExCompanionDialogFeature : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableExCompanionDialog;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_ExCompanionDialogFeature_Name", "Include Ex-Companions In Remote Companion Dialog")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_ExCompanionDialogFeature_Description", "This makes Remote Companion Dialog also include companions who left the party.")]
    public override partial string Description { get; }
    public override bool ShouldHide {
        get {
            return !GetInstance<RemoteCompanionDialogFeature>().IsEnabled;
        }
    }
}
