using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using static UnityModManagerNet.UnityModManager.Param;

namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.LoveIsFreeFeature")]
public partial class LoveIsFreeFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableLoveIsFree;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_LoveIsFreeFeature_Name", "Love Is Free")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_LoveIsFreeFeature_Description", "Allow any gender for any Romance.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.LoveIsFreeFeature";
        }
    }
    #region Overrides
    // Any Gender Any Romance Overrides
    // These modify the PcFemale/PcMale conditions for specific Owner blueprints 
    private static readonly Dictionary<string, bool> m_PcFemaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0004
            { "5457755c30ac417d9279fd740b90f549", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0023
            { "8d6b7c53af134494a64a4de789759fb9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_11
            { "4b4b769261f04a8cb5726e111c3f7081", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_2
            { "cf1d7205cf854709b038db477db48ac9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Check_0011
            { "d2c500fbc1b5450c8663d453a33b0eee", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // World\Dialogs\Ch1\BridgeAndCabinet\Briefing\Answer_15
            { "02e0bc30b5a146708dd62d68ac7490bd", true },
            // World\Dialogs\Companions\CompanionDialogues\Interrogator\Cue_10
            { "2df6bd21ad5a45a9b1c5142d51d647dc", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_45
            { "0739ef639d774629a27d396cd733cfd4", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_67
            { "ea42722c44c84835b7363db2fc09b23b", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_47
            { "41897fd7a52249d3a53691fbcfcc9c19", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_89
            { "c5efaa0ace544ca7a81d439e7cfc6ae5", true }
        };
    private static readonly Dictionary<string, bool> m_PcMaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_0017
            { "85b651edb4f74381bbe762999273c6ec", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_10
            { "56bbf1612e05489ba44bb4a52718e222", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_5
            { "eb76f93740824d16b1e1f54b82de21e0", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_8
            { "c292b399f4344a639ccb4df9ba66329e", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\CassFirstTimeBlushing_a
            { "95b0ba7d08e34f6c895b2fbeb53ea404", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q1\CassiaSeriousTalk\Answer_8
            { "966f0cc2defa42bd836950aa1ebcde72", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_24
            { "a903589840ba4ab683d6e6b9f985d458", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_11
            { "c051d0c9f2ba4c23bff1d1e6f2cfe13d", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_12
            { "3d24df76aacf4e2db047cf47ef3474d5", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_19
            { "b3601cd9e84d43dbb4078bf77c89d728", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_6
            { "17b34e1ae36443408805af3a3c2866f7", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Cue_29
            { "7f71e0b93dd9420d87151fc3e7114865", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_47
            { "588a3c2e96c6403ca2c7104949b066e4", true },
            // World\Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q2\Cassia_Q2_BE\Cue_0037
            { "bf7813b4ee3d49cdbc6305f454479db3", true }
        };
    #endregion
    [HarmonyPatch(typeof(PcFemale), nameof(PcFemale.CheckCondition)), HarmonyPostfix]
    private static void PcFemale_CheckCondition_Patch(PcFemale __instance, ref bool __result) {
        if (__instance.Owner is not null) {
            if (m_PcFemaleOverrides.TryGetValue(__instance.Owner.AssetGuid, out var value)) {
                Debug($"Overiding {__instance.Owner.name} from {__result} to {value}");
                __result = value;
            }
        }
    }
    [HarmonyPatch(typeof(PcMale), nameof(PcFemale.CheckCondition)), HarmonyPostfix]
    private static void PcMale_CheckCondition_Patch(PcMale __instance, ref bool __result) {
        if (__instance.Owner is not null) {
            if (m_PcMaleOverrides.TryGetValue(__instance.Owner.AssetGuid, out var value)) {
                Debug($"Overiding {__instance.Owner.name} from {__result} to {value}");
                __result = value;
            }
        }
    }
}
