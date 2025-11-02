using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DLC;
using Kingmaker.UnitLogic.Progression.Features;

namespace ToyBox.Features.BagOfTricks.Dialog;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Dialog.OverrideStoryOccupationFeature")]
public partial class OverrideStoryOccupationFeature : FeatureWithPatch {
    //                                                    AstraMilitarum                   ,  Commissar                        ,  Criminal
    private static readonly string[] m_OccupationIds = [ "4b908491051a4f36b9703b95e048a5a3", "00b183680643424abe015263aac81c5b", "8fab55c9130a4ae0a745f4fa1674c5df",
                                  // MinistorumCrusader               ,  NavyOfficer                      ,  Nobility                         ,  SanctionedPsyker
                                    "d840a5dc947546e0b4ac939287191fd8", "962c310fd1664ae996c759e4d11a2d88", "06180233245249eea90d222bb1c13f00", "1518d1434ed646039215da3fdda6b096",
                                  // Arbitrator                       
                                    "cd1baf99dad544168bbf4962b3389d94"];
    private static Browser<BlueprintFeature>? m_OccupationBrowser;
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableOverrideStoryOccupation;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_OverrideStoryOccupationFeature_Name", "Override Story Occupation")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_OverrideStoryOccupationFeature_Description", "Make the game recognize you as e.g. having the Sanctioned Psyker instead of whatever you actually picked.")]
    public override partial string Description { get; }

    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_OverrideStoryOccupationFeature_m_RemoveLocalizedText", "Remove")]
    private static partial string m_RemoveLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Dialog_OverrideStoryOccupationFeature_m_AddLocalizedText", "Add")]
    private static partial string m_AddLocalizedText { get; }
    private static bool m_ShowOccupationBrowser = false;
    public override void OnGui() {
        if (IsEnabled) {
            if (m_OccupationBrowser == null) {
                var occupations = BPLoader.GetBlueprintsByGuids<BlueprintFeature>(m_OccupationIds).Where(bp => !bp.IsDlcRestricted());
                m_OccupationBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, occupations);
            }
            using (HorizontalScope()) {
                _ = UI.DisclosureToggle(ref m_ShowOccupationBrowser);
                base.OnGui();
            }
            if (m_ShowOccupationBrowser) {
                using (HorizontalScope()) {
                    Space(25);
                    m_OccupationBrowser.OnGUI(o => {
                        using (HorizontalScope()) {
                            if (Settings.OverridenOccupations.Contains(o.AssetGuid)) {
                                UI.Label(BPHelper.GetTitle(o).Cyan(), Width(500));
                                _ = UI.Button(m_RemoveLocalizedText, () => Settings.OverridenOccupations.Remove(o.AssetGuid));
                            } else {
                                UI.Label(BPHelper.GetTitle(o), Width(500));
                                _ = UI.Button(m_AddLocalizedText, () => Settings.OverridenOccupations.Add(o.AssetGuid));
                            }
                        }
                    });
                }
            }
        } else {
            base.OnGui();
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Dialog.OverrideStoryOccupationFeature";
        }
    }
    [HarmonyPatch(typeof(HasFact), nameof(HasFact.CheckCondition)), HarmonyPostfix]
    private static void HasFact_CheckCondition_Occupation_Patch(HasFact __instance, ref bool __result) {
        if (__instance.Unit.GetValue() != Game.Instance.Player.MainCharacterEntity) {
            return;
        }
        if (Settings.OverridenOccupations.Contains(__instance.Fact.AssetGuid)) {
            __result = true;
        }
    }
}
