namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.JealousyBegoneFeature")]
public partial class JealousyBegoneFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableJealousyBegone;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_JealousyBegoneFeature_Name", "Jealousy Begone!")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_JealousyBegoneFeature_Description", "Allow multiple romances at the same time")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.JealousyBegoneFeature";
        }
    }
    #region Overrides
    // This modify the EtudeStatus condition for specific Owner blueprints 
    private static readonly Dictionary<(string, string), bool> ConditionCheckOverrides = new() {
            // Cue_43 (I assume Kibella) check for any active romance
            { ("f4b42351c500429ba9cfbccf352ddd3b", "6a8601e5d98a450f8ed8b644c1cf1fea"), false },
            // Cue_43 (I assume Kibella) check jealousy dialog seen or active romances = 1 
            { ("f4b42351c500429ba9cfbccf352ddd3b", "09f80fe401704413864b879f3bdfb970"), false }
        };
    // This modifies all conditions of the blueprint with the specified id
    private static readonly Dictionary<string, bool> AllConditionCheckOverrides = new() {
            // Kibellah romance after coming back (All)
            { "d269a5417ca646759584a2ab7bddf319", false }, { "5ca382ed53964851bd19ce07efb7bb8c", false },
            // Kibellah romance after coming back (Cas)
            { "683bf82b7663452a9fb92955b4b1d031", false }, { "aae351192ac24f84ab36e1839c1ab7c3", false },
            // Kibellah romance after coming back (Hein)
            { "6b08fa9121c54f3c811536fef69f12c9", false }, { "ec45728761064a27bfafca1fbfa65355", false },
            // Kibellah romance after coming back (Jae)
            { "25163b765a8442e2928f3423f080fa57", false }, { "83953390c5764c25a9d2d0b1f899f905", false },
            // Kibellah romance after coming back (Mar)
            { "7d30413d5b7a426aa891b1e282792134", false }, { "2b989a9de0f04c2c848269294a0a4452", false },
            // Kibellah romance after coming back (Yrl)
            { "c409b0626cce411ab6720916f310d9f2", false }, { "efc090e2c9794fac855be6c597637bda", false }
        };
    private static readonly Dictionary<string, bool> FlagInRangeOverrides = new() {
            // RomanceCount Flag, as conditioned in Jealousy_event Blueprint, Activated by Jealousy_preparation
            { "cbb219fcb46948fba48a8bed94663e5d", false }
        };
    #endregion
}
