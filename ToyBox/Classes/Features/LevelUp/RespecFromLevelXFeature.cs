using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem.ContextData;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Progression.Paths;
using System.Reflection.Emit;
using UnityEngine;

namespace ToyBox.Features.LevelUp;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.RespecFromLevelXFeature")]
public partial class RespecFromLevelXFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableRespecFromLevelX;
        }
    }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_Name", "Respec From Level X")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_Description", "Allows changing the base respec level for units (including companions). Setting this to level 35 allows repicking the third archetype. Level 15 allows changing both the second and third arche type. Level 0 allows changing all three archetypes.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.RespecFromLevelXFeature";
        }
    }
    private bool m_ShowDisclaimer = false;
    private int m_CustomLevel = 1;
    public override void OnGui() {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(25);
                using (VerticalScope()) {
                    UI.DisclosureToggle(ref m_ShowDisclaimer, m_TryToKeepThisFeatureActivatedAftLocalizedText.Orange());
                    if (m_ShowDisclaimer) {
                        using (HorizontalScope()) {
                            Space(35);
                            UI.Label(m_IfAUnitIsRespeccedWhileThisFeatuLocalizedText.Cyan(), Width(0.5f * EffectiveWindowWidth()));
                        }
                    }
                    var currentLabelText = Settings.CurrentRespecLevelSetting.HasValue ? Settings.CurrentRespecLevelSetting.Value.ToString() : m_NoneLocalizedText;
                    using (HorizontalScope()) {
                        Space(12);
                        UI.Label(m_CurrentRespecLevelOverrideLocalizedText.Cyan() + ": " + currentLabelText.Green());
                    }
                    var is0 = Settings.CurrentRespecLevelSetting == 0;
                    if (UI.Toggle(m_RespecFromLevel0LocalizedText, null, ref is0)) {
                        if (is0) {
                            Settings.CurrentRespecLevelSetting = 0;
                        } else {
                            Settings.CurrentRespecLevelSetting = null;
                        }
                    }
                    var is15 = Settings.CurrentRespecLevelSetting == 15;
                    if (UI.Toggle(m_RespecFromLevel15LocalizedText, null, ref is15)) {
                        if (is15) {
                            Settings.CurrentRespecLevelSetting = 15;
                        } else {
                            Settings.CurrentRespecLevelSetting = null;
                        }
                    }
                    var is35 = Settings.CurrentRespecLevelSetting == 35;
                    if (UI.Toggle(m_RespecFromLevel35LocalizedText, null, ref is35)) {
                        if (is35) {
                            Settings.CurrentRespecLevelSetting = 35;
                        } else {
                            Settings.CurrentRespecLevelSetting = null;
                        }
                    }
                    var isCustom = Settings.CurrentRespecLevelSetting.HasValue && Settings.CurrentRespecLevelSetting != 0
                        && Settings.CurrentRespecLevelSetting != 15 && Settings.CurrentRespecLevelSetting != 35;
                    using (HorizontalScope()) {
                        if (UI.Toggle(m_RespecFromCustomLevelLocalizedText, null, ref isCustom, null, null, 200 * Main.UIScale)) {
                            if (isCustom) {
                                Settings.CurrentRespecLevelSetting = m_CustomLevel;
                            } else {
                                Settings.CurrentRespecLevelSetting = null;
                            }
                        }
                        Space(5);
                        UI.TextField(ref m_CustomLevel, null, GUILayout.MinWidth(100 * Main.UIScale), GUILayout.MaxWidth(300 * Main.UIScale));
                        Space(5);
                        if (UI.Button(m_ApplyCustomLevelLocalizedText)) {
                            Settings.CurrentRespecLevelSetting = m_CustomLevel;
                        }
                    }
                }
            }
        }
    }
    private static int GetRespecLevel(PartUnitProgression progression) {
        if (ContextData<UnitHelper.PreviewUnit>.Current) {
            if (InSaveSettings?.LastRespecLevelForUnit?.TryGetValue(progression.Owner.Blueprint.AssetGuid, out var level) ?? false) {
                return level;
            }
        } else {
            if (Settings.CurrentRespecLevelSetting.HasValue) {
                var level = Math.Min(Settings.CurrentRespecLevelSetting.Value, progression.CharacterLevel);
                InSaveSettings?.LastRespecLevelForUnit?[progression.Owner.Blueprint.AssetGuid] = level;
                InSaveSettings?.Save();
                return level;
                ;
            } else {
                if (InSaveSettings?.LastRespecLevelForUnit?.Remove(progression.Owner.Blueprint.AssetGuid) ?? false) {
                    InSaveSettings.Save();
                }
            }
        }
        return progression.Owner.Blueprint.GetDefaultLevel();
    }
    public static ValueTuple<BlueprintCareerPath?, int> GetNextCareerNullable(PartUnitProgression _instance) {
        ValueTuple<BlueprintCareerPath?, int> ret;
        try {
            ret = _instance.AllCareerPaths.Last<ValueTuple<BlueprintCareerPath?, int>>();
        } catch (Exception) {
            ret = new(null, 1);
        }
        Trace($"Respec Career returned: {ret}");
        return ret;

    }
    [HarmonyPatch(typeof(PartUnitProgression), nameof(PartUnitProgression.Respec)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PartUnitProgression_Respec_Patch(IEnumerable<CodeInstruction> instructions) {
        var shouldSkipNextInstruction = false;
        foreach (var instruction in instructions) {
            if (shouldSkipNextInstruction) {
                shouldSkipNextInstruction = false;
                continue;
            }
            if (instruction.Calls(AccessTools.Method(typeof(BlueprintUnit), nameof(BlueprintUnit.GetDefaultLevel)))) {
                yield return new CodeInstruction(OpCodes.Pop).WithLabels(instruction.labels);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RespecFromLevelXFeature), nameof(GetRespecLevel)));
                continue;
            } else if (instruction.Calls(AccessTools.PropertyGetter(typeof(PartUnitProgression), nameof(PartUnitProgression.AllCareerPaths)))) {
                instruction.operand = AccessTools.Method(typeof(RespecFromLevelXFeature), nameof(GetNextCareerNullable));
                shouldSkipNextInstruction = true;
            }
            yield return instruction;
        }

    }
    [HarmonyPatch(typeof(BlueprintUnit), nameof(BlueprintUnit.CreateEntity)), HarmonyPostfix]
    private static void BlueprintUnit_CreateEntity_Patch(BlueprintUnit __instance, BaseUnitEntity __result) {
        // Don't do this for e.g. Mercenaries
        if (__instance.GetDefaultLevel() > 0) {
            if (ContextData<UnitHelper.PreviewUnit>.Current && (InSaveSettings?.LastRespecLevelForUnit?.TryGetValue(__instance.AssetGuid, out _) ?? false)) {
                __result.Progression.Respec();
            }
        }
    }

    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_TryToKeepThisFeatureActivatedAftLocalizedText", "Try to keep this feature activated after using it (Click for Explanation)")]
    private static partial string m_TryToKeepThisFeatureActivatedAftLocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_IfAUnitIsRespeccedWhileThisFeatuLocalizedText", "If a unit is respecced while this feature is enabled and the feature (or ToyBox) is later disabled, minor level-up UI glitches may occur. During level-up the game uses a base (original) copy of the unit. ToyBox must respec that copy when it's created (which only happens while the feature is active). If this is not done, there can be slight issues like the game thinking you have a feature despite not having it (because the base/original unit was supposed to have that feature, making the new copy having it too). This is in no way breaking as any issues can be fixed by respeccing the unit after disabling this feature/ToyBox.")]
    private static partial string m_IfAUnitIsRespeccedWhileThisFeatuLocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_NoneLocalizedText", "None")]
    private static partial string m_NoneLocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_CurrentRespecLevelOverrideLocalizedText", "Current Respec Level Override")]
    private static partial string m_CurrentRespecLevelOverrideLocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_RespecFromLevel0LocalizedText", "Respec from Level 0")]
    private static partial string m_RespecFromLevel0LocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_RespecFromLevel15LocalizedText", "Respec from Level 15")]
    private static partial string m_RespecFromLevel15LocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_RespecFromLevel35LocalizedText", "Respec from Level 35")]
    private static partial string m_RespecFromLevel35LocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_RespecFromCustomLevelLocalizedText", "Respec from Custom Level")]
    private static partial string m_RespecFromCustomLevelLocalizedText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_RespecFromLevelXFeature_m_ApplyCustomLevelLocalizedText", "Apply Custom Level")]
    private static partial string m_ApplyCustomLevelLocalizedText { get; }
}
