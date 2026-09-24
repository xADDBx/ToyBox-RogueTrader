namespace ToyBox.Features.LevelUp;

/// <summary>
/// Toggle-only feature (no patches of its own — the hook lives in
/// TalentSelectionMultiplierFeature, which already owns the
/// IsAllSelectionsMadeAndValid postfix). When enabled, every unmade
/// selection passes the Finish gate, letting the user deliberately
/// skip unwanted options instead of being forced to pick.
/// </summary>
public partial class MakeSelectionsOptionalFeature : ToggledFeature {
    [LocalizedString("ToyBox_Features_LevelUp_MakeSelectionsOptionalFeature_Name", "Make Selections Optional")]
    public override partial string Name { get; }

    [LocalizedString(
        "ToyBox_Features_LevelUp_MakeSelectionsOptionalFeature_Description",
        "When enabled, you can Finish a level-up without selecting anything - empty slots pass the completion check, and Next skips the current page. Useful when the remaining options are unwanted. Requires the Talent Selection Multiplier feature enabled (its patches drive the gate). Default OFF.")]
    public override partial string Description { get; }

    public override ref bool IsEnabled {
        get {
            return ref Settings.MakeSelectionsOptional;
        }
    }

    public override void OnGui() {
        _ = UI.Toggle(Name, Description, ref Settings.MakeSelectionsOptional);
    }
}
