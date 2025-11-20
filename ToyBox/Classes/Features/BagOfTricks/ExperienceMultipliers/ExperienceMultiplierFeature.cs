using Kingmaker;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.GameModes;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.ExperienceMultipliers;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.ExperienceMultipliers.ExperienceMultiplierFeature")]
public partial class ExperienceMultiplierFeature : FeatureWithPatch {
    private static bool m_IsEnabled = false;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.AllExperienceMultiplier != 1f || Settings.UseCombatExperienceMultiplier || Settings.UseQuestExperienceMultiplier || Settings.UseSkillCheckMultiplier || Settings.UseChallengesMultiplier || Settings.UseSpaceCombatMultiplier;
            return ref m_IsEnabled;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_Name", "Experience Multipliers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_Description", "Provides a general experience multiplier and possible overrides for specific kinds of experience sources.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.ExperienceMultipliers.ExperienceMultiplierFeature";
        }
    }
    private void MaybeReset() {
        if (m_IsEnabled != IsEnabled) {
            Destroy();
            Initialize();
        }
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            UI.Label(m_AllExperienceLocalizedText.Cyan(), Width(250 * Main.UIScale));
            if (UI.LogSlider(ref Settings.AllExperienceMultiplier, 0f, 100f, 1f, 1)) {
                MaybeReset();
            }
        }
        using (HorizontalScope()) {
            if (UI.Toggle(m_OverrideForCombatLocalizedText, null, ref Settings.UseCombatExperienceMultiplier, null, null, 250 * Main.UIScale)) {
                MaybeReset();
            }
            if (Settings.UseCombatExperienceMultiplier) {
                UI.LogSlider(ref Settings.CombatExperienceMultiplier, 0f, 100f, 1f, 1);
            }
        }
        using (HorizontalScope()) {
            if (UI.Toggle(m_OverrideForQuestsLocalizedText, null, ref Settings.UseQuestExperienceMultiplier, null, null, 250 * Main.UIScale)) {
                MaybeReset();
            }
            if (Settings.UseQuestExperienceMultiplier) {
                UI.LogSlider(ref Settings.QuestExperienceMultiplier, 0f, 100f, 1f, 1);
            }
        }
        using (HorizontalScope()) {
            if (UI.Toggle(m_OverrideForSkillChecksLocalizedText, null, ref Settings.UseSkillCheckMultiplier, null, null, 250 * Main.UIScale)) {
                MaybeReset();
            }
            if (Settings.UseSkillCheckMultiplier) {
                UI.LogSlider(ref Settings.SkillCheckMultiplier, 0f, 100f, 1f, 1);
            }
        }
        using (HorizontalScope()) {
            if (UI.Toggle(m_OverrideForChallengesLocalizedText, null, ref Settings.UseChallengesMultiplier, null, null, 250 * Main.UIScale)) {
                MaybeReset();
            }
            if (Settings.UseChallengesMultiplier) {
                UI.LogSlider(ref Settings.ChallengeMultiplier, 0f, 100f, 1f, 1);
            }
        }
        using (HorizontalScope()) {
            if (UI.Toggle(m_OverrideForSpaceCombatLocalizedText, null, ref Settings.UseSpaceCombatMultiplier, null, null, 250 * Main.UIScale)) {
                MaybeReset();
            }
            if (Settings.UseSpaceCombatMultiplier) {
                UI.LogSlider(ref Settings.SpaceCombatMultiplier, 0f, 100f, 1f, 1);
            }
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_AllExperienceLocalizedText", "All Experience")]
    private static partial string m_AllExperienceLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_OverrideForCombatLocalizedText", "Override for Combat")]
    private static partial string m_OverrideForCombatLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_OverrideForQuestsLocalizedText", "Override for Quests")]
    private static partial string m_OverrideForQuestsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_OverrideForSkillChecksLocalizedText", "Override for Skill Checks")]
    private static partial string m_OverrideForSkillChecksLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_OverrideForChallengesLocalizedText", "Override for Challenges")]
    private static partial string m_OverrideForChallengesLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_ExperienceMultipliers_ExperienceMultiplierFeature_m_OverrideForSpaceCombatLocalizedText", "Override for Space Combat")]
    private static partial string m_OverrideForSpaceCombatLocalizedText { get; }

    #region Patches
    [HarmonyPatch(typeof(ExperienceHelper), nameof(ExperienceHelper.GetCheckExp)), HarmonyPostfix]
    private static void ExperienceHelper_GetCheckExp_Patch(ref int __result) {
        var mult = Settings.AllExperienceMultiplier;
        if (Settings.UseSkillCheckMultiplier) {
            mult = Settings.SkillCheckMultiplier;
        }
        if (mult != 1) {
            __result = Mathf.RoundToInt(__result * mult);
        }
    }
    [HarmonyPatch(typeof(ExperienceHelper), nameof(ExperienceHelper.GetCheckExpByDifficulty)), HarmonyPostfix]
    private static void ExperienceHelper_GetCheckExpByDifficulty_Patch(ref int __result) {
        var mult = Settings.AllExperienceMultiplier;
        if (Settings.UseSkillCheckMultiplier) {
            mult = Settings.SkillCheckMultiplier;
        }
        if (mult != 1) {
            __result = Mathf.RoundToInt(__result * mult);
        }
    }
    [HarmonyPatch(typeof(ExperienceHelper), nameof(ExperienceHelper.GetMobExp)), HarmonyPostfix]
    private static void ExperienceHelper_GetMobExp_Patch(ref int __result) {
        var mult = Settings.AllExperienceMultiplier;
        if (Settings.UseCombatExperienceMultiplier) {
            mult = Settings.CombatExperienceMultiplier;
        }
        if (mult != 1) {
            __result = Mathf.RoundToInt(__result * mult);
        }
    }
    [HarmonyPatch(typeof(ExperienceHelper), nameof(ExperienceHelper.GetXp)), HarmonyPostfix]
    private static void ExperienceHelper_GetXp_Patch(ref int __result, EncounterType type) {
        var mult = Settings.AllExperienceMultiplier;
        if (Game.Instance.CurrentMode == GameModeType.SpaceCombat) {
            if (Settings.UseSpaceCombatMultiplier) {
                mult = Settings.SpaceCombatMultiplier;
            }
        } else {
            switch (type) {
                case EncounterType.QuestNormal:
                case EncounterType.QuestMain: {
                        if (Settings.UseQuestExperienceMultiplier) {
                            mult = Settings.QuestExperienceMultiplier;
                        }
                    }
                    break;
                case EncounterType.Mob:
                case EncounterType.Boss: {
                        if (Settings.UseCombatExperienceMultiplier) {
                            mult = Settings.CombatExperienceMultiplier;
                        }
                    }
                    break;
                case EncounterType.ChallengeMinor:
                case EncounterType.ChallengeMajor: {
                        if (Settings.UseChallengesMultiplier) {
                            mult = Settings.ChallengeMultiplier;
                        }
                    }
                    break;
                case EncounterType.SkillCheck: {
                        if (Settings.UseSkillCheckMultiplier) {
                            mult = Settings.SkillCheckMultiplier;
                        }
                    }
                    break;
            }
        }
        if (mult != 1) {
            __result = Mathf.RoundToInt(__result * mult);
        }
    }
    #endregion
}
