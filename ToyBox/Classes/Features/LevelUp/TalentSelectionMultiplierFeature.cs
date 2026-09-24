using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameCommands;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.CareerPath;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Levelup;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Levelup.Selections.Feature;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;
using System.Reflection;
using System.Reflection.Emit;

namespace ToyBox.Features.LevelUp;

// NOTE: this Harmony category is shared with LevelUpNavigationPatch.cs (optional
// selections / skip navigation / auto-advance), so those patches toggle
// together with this feature.
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.TalentSelectionMultiplierFeature")]
public partial class TalentSelectionMultiplierFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableTalentSelectionMultiplier;
        }
    }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.LevelUp.TalentSelectionMultiplierFeature";
        }
    }

    [LocalizedString("ToyBox_Features_LevelUp_TalentSelectionMultiplierFeature_Name", "Talent Selection Multiplier")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_LevelUp_TalentSelectionMultiplierFeature_Description", "Multiplies career talent, ability and voidship upgrade selections (1 = normal each). Learned picks remain after disabling, but extra choices are no longer shown in the career progression after reopening the character screen. Don't change this setting mid-levelup.")]
    public override partial string Description { get; }

    [LocalizedString("ToyBox_Features_LevelUp_TalentSelectionMultiplierFeature_TalentsText", "Talents")]
    private static partial string m_TalentsText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_TalentSelectionMultiplierFeature_AbilitiesText", "Abilities")]
    private static partial string m_AbilitiesText { get; }
    [LocalizedString("ToyBox_Features_LevelUp_TalentSelectionMultiplierFeature_ShipUpgradesText", "Ship Upgrades")]
    private static partial string m_ShipUpgradesText { get; }

    public override void OnGui() {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(25);
                UI.Label(m_TalentsText, Width(150));
                _ = UI.Slider(ref Settings.TalentSelectionMultiplier, 1, 10, 1, null, null, Width(150));
            }
            using (HorizontalScope()) {
                Space(25);
                UI.Label(m_AbilitiesText, Width(150));
                _ = UI.Slider(ref Settings.AbilityMultiplier, 1, 10, 1, null, null, Width(150));
            }
            using (HorizontalScope()) {
                Space(25);
                UI.Label(m_ShipUpgradesText, Width(150));
                _ = UI.Slider(ref Settings.ShipUpgradeMultiplier, 1, 10, 1, null, null, Width(150));
            }
        }
    }

    private static bool IsTalentGroup(FeatureGroup group) {
        return group is FeatureGroup.Talent
            or FeatureGroup.CommonTalent
            or FeatureGroup.AscensionTalent
            or FeatureGroup.FirstCareerTalent
            or FeatureGroup.SecondCareerTalent
            or FeatureGroup.FirstOrSecondCareerTalent;
    }

    // Character rank ability picks (能力/额外能力). The production enum has no
    // single 'Ability' member; these four are exactly the ability groups the
    // game's own RankEntryUtils.GroupsWithFilter lists for career rank entries.
    private static bool IsAbilityGroup(FeatureGroup group) {
        return group is FeatureGroup.ActiveAbility
            or FeatureGroup.FirstCareerAbility
            or FeatureGroup.SecondCareerAbility
            or FeatureGroup.FirstOrSecondCareerAbility;
    }

    // EXACTLY the set the game's own starship mapping recognizes:
    // TutorialTriggerLevelUpByFeatureGroupExtensions.ToStarshipFeatureGroup
    // maps ShipUpgrade, ShipActiveAbility, ShipUltimateAbility and
    // AdvancedAbility. ShipTotalAbility is NOT in that mapping, so it stays
    // unmanaged.
    private static bool IsShipGroup(FeatureGroup group) {
        return group is FeatureGroup.ShipUpgrade
            or FeatureGroup.ShipActiveAbility
            or FeatureGroup.ShipUltimateAbility
            or FeatureGroup.AdvancedAbility;
    }

    private static int ClampMultiplier(int value) {
        return Math.Max(1, Math.Min(10, value));
    }

    private static bool IsManaged(BaseUnitEntity unit, BlueprintSelectionFeature selection) {
        return ToyBoxUnitHelper.IsPartyOrPet(unit) && (IsTalentGroup(selection.Group) || IsAbilityGroup(selection.Group))
            || ToyBoxUnitHelper.IsPlayerStarship(unit) && IsShipGroup(selection.Group);
    }

    private static int GetMultiplier(BaseUnitEntity unit, BlueprintSelectionFeature selection) {
        if (!IsManaged(unit, selection)) {
            return 1;
        }
        if (IsShipGroup(selection.Group)) {
            return ClampMultiplier(Settings.ShipUpgradeMultiplier);
        }
        return ClampMultiplier(IsAbilityGroup(selection.Group) ? Settings.AbilityMultiplier : Settings.TalentSelectionMultiplier);
    }

    private static int GetOccurrence(RankEntrySelectionVM row) {
        return row.CareerPathVM.RankEntries[row.Rank - 1].Selections.TakeWhile(other => other != row).Count(other => other.m_SelectionFeature == row.m_SelectionFeature);
    }

    private static SelectionState? GetSelectionState(LevelUpManager manager, BlueprintPath path, BlueprintSelection selection,
        int rank, RankEntrySelectionVM row) {
        return manager.Selections.Where(s => s.Path == path && s.Blueprint == selection && s.PathRank == rank).Skip(GetOccurrence(row)).FirstOrDefault();
    }

    private static (BlueprintFeature Feature, int Rank)? GetSelectedFeature(PartUnitProgression progression, BlueprintPath path,
        int rank, BlueprintSelectionFeature selection, RankEntrySelectionVM row) {
        var data = progression.GetSelectionsByPath(path).Where(s => s.Level == rank && s.Selection == selection).Skip(GetOccurrence(row)).FirstOrDefault();
        return data.Feature == null ? null : (data.Feature, data.Rank);
    }

    private static SelectionEntry? TakeSelection(IList<SelectionEntry> selections, Func<SelectionEntry, bool> predicate,
        CommitLevelUpGameCommand command, ref Queue<SelectionEntry>? pending) {
        if (command.m_CareerPath is not BlueprintCareerPath) {
            return selections.FirstOrDefault(predicate);
        }
        pending ??= new(selections);
        return pending.Count > 0 && predicate(pending.Peek()) ? pending.Dequeue() : null;
    }

    #region Patches
    // Add extra career selections for each multiplied group
    [HarmonyPatch(typeof(LevelUpManager), nameof(LevelUpManager.CreatePathSelections)), HarmonyPostfix]
    private static IEnumerable<SelectionState> LevelUpManager_CreatePathSelections_Patch(IEnumerable<SelectionState> __result, LevelUpManager __instance) {
        foreach (var state in __result) {
            yield return state;
            if (!__instance.AutoCommit && state.Path is BlueprintCareerPath && state.Blueprint is BlueprintSelectionFeature selection) {
                var multiplier = GetMultiplier(__instance.TargetUnit, selection);
                for (var i = 1; i < multiplier; i++) {
                    yield return new SelectionStateFeature(__instance, selection, state.Path, state.PathRank);
                }
            }
        }
    }

    // Match selection rows to upcoming and learned selections
    [HarmonyPatch(typeof(CareerPathVM), nameof(CareerPathVM.InitializeRankEntries)), HarmonyPostfix]
    private static void CareerPathVM_InitializeRankEntries_Patch(CareerPathVM __instance) {
        var history = __instance.Unit.Progression.GetSelectionsByPath(__instance.CareerPath).ToLookup(s => (s.Level, s.Selection));
        var (min, max) = __instance.GetCurrentLevelupRange();
        foreach (var rank in __instance.RankEntries) {
            foreach (var group in rank.Selections.GroupBy(row => row.m_SelectionFeature)) {
                if (group.Key == null || !IsManaged(__instance.Unit, group.Key)) { continue; }
                var multiplier = GetMultiplier(__instance.Unit, group.Key);
                var count = Math.Max(1, history[(rank.Rank, group.Key)].Count());
                count = Math.Max(count, rank.Rank >= min && rank.Rank <= max ? multiplier : 1);
                if (count == group.Count()) { continue; }
                __instance.m_AddedOnLevelUpFeatures = null;
                foreach (var row in group.Skip(count)) {
                    if (__instance.UnitProgressionVM.CurrentRankEntryItem.Value == row) {
                        __instance.UnitProgressionVM.CurrentRankEntryItem.Value = null;
                    }
                    rank.Selections.RemoveAndDispose(row);
                }
                var index = rank.Selections.IndexOf(group.First()) + group.Count();
                for (var i = group.Count(); i < count; i++) {
                    rank.Selections.Insert(index++, new RankEntrySelectionVM(rank.Rank, __instance, group.Key, rank.SelectRankEntryItem));
                }
            }
        }
        __instance.UpdateFirstLastEntriesToUpgrade();
    }

    // Resolve each row's own selection and history
    [HarmonyPatch(typeof(RankEntrySelectionVM), nameof(RankEntrySelectionVM.UpdateState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RankEntrySelectionVM_UpdateState_Patch(IEnumerable<CodeInstruction> instructions) {
        var stateLookup = AccessTools.Method(typeof(LevelUpManager), nameof(LevelUpManager.GetSelectionState));
        var historyLookup = AccessTools.Method(typeof(PartUnitProgression), nameof(PartUnitProgression.GetSelectedFeature));
        var foundState = false;
        var foundHistory = false;
        foreach (var instruction in instructions) {
            MethodInfo? replacement = null;
            if (instruction.Calls(stateLookup)) {
                foundState = true;
                replacement = AccessTools.Method(typeof(TalentSelectionMultiplierFeature), nameof(GetSelectionState));
            } else if (instruction.Calls(historyLookup)) {
                foundHistory = true;
                replacement = AccessTools.Method(typeof(TalentSelectionMultiplierFeature), nameof(GetSelectedFeature));
            }
            if (replacement != null) {
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
        ThrowIfTrue(!foundState || !foundHistory);
    }

    // Consume each recorded choice once when committing
    [HarmonyPatch(typeof(CommitLevelUpGameCommand), nameof(CommitLevelUpGameCommand.ExecuteInternal)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CommitLevelUpGameCommand_ExecuteInternal_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var pending = generator.DeclareLocal(typeof(Queue<SelectionEntry>));
        var found = false;
        foreach (var instruction in instructions) {
            if (instruction.operand is MethodInfo method && method.Name == "FirstItem" && method.ReturnType == typeof(SelectionEntry)) {
                found = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                yield return new CodeInstruction(OpCodes.Ldloca, pending);
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(TalentSelectionMultiplierFeature), nameof(TakeSelection));
            }
            yield return instruction;
        }
        ThrowIfTrue(!found);
    }
    #endregion
}
