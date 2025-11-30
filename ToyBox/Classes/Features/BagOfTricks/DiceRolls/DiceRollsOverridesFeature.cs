using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules.Starships;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using static Kingmaker.RuleSystem.RulebookEvent;

namespace ToyBox.Features.BagOfTricks.DiceRolls;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.DiceRolls.DiceRollsOverridesFeature")]
public partial class DiceRollsOverridesFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableDiceRollsOverrides;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_Name", "Dice Roll Overrides")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_Description", "Allows changing the results of various dice rolls across the game.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.DiceRolls.DiceRollsOverridesFeature";
        }
    }
    private readonly TimedCache<float> m_LabelWidth = new(() => CalculateLargestLabelWidth([
        m_AllAttacksHitLocalizedText, m_AllAttacksCritLocalizedText, m_RollWithAdvantageLocalizedText, m_RollWithDisadvantageLocalizedText,
        m_Initiative_AlwaysRoll10LocalizedText, m_Initiative_AlwaysRoll5LocalizedText, m_Initiative_AlwaysRoll1LocalizedText, m_NeverRoll100LocalizedText, 
        m_NeverRoll1LocalizedText, m_DamageRolls_Take1LocalizedText, m_DamageRolls_Take25LocalizedText, m_DamageRolls_Take50LocalizedText, 
        m_OutOfCombat_Take1LocalizedText, m_OutOfCombat_Take25LocalizedText, m_OutOfCombat_Take50LocalizedText, m_SkillChecks_Take1LocalizedText, 
        m_SkillChecks_Take25LocalizedText, m_SkillChecks_Take50LocalizedText, m_Take100LocalizedText, m_Take1LocalizedText, 
        m_Take50LocalizedText], GUI.skin.label));
    public override void OnGui() {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(25);
                using (VerticalScope()) {
                    var labelWidth = m_LabelWidth.Value + 50 * Main.UIScale;
                    using (HorizontalScope()) {
                        UI.Label(m_AllAttacksHitLocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsAllAttacksHit, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_AllAttacksCritLocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsAllAttacksCrit, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    UI.Label(m_AdvantageMeansDoingTwoRollsAndTaLocalizedText.Green());
                    using (HorizontalScope()) {
                        UI.Label(m_RollWithAdvantageLocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsRollWithAdvantage, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_RollWithDisadvantageLocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsRollWithDisadvantage, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_Take100LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsAlwaysRoll100, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_Take50LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsAlwaysRoll50, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_Take1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsAlwaysRoll1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_NeverRoll100LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsNeverRoll100, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_NeverRoll1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsNeverRoll1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_OutOfCombat_Take50LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsOutOfCombatTake50, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_OutOfCombat_Take25LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsOutOfCombatTake25, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_OutOfCombat_Take1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsOutOfCombatTake1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    UI.Label(m_Initiative__HigherIsBetterLocalizedText.Green());
                    using (HorizontalScope()) {
                        UI.Label(m_Initiative_AlwaysRoll10LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsInitiativeAlwaysRoll10, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_Initiative_AlwaysRoll5LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsInitiativeAlwaysRoll5, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_Initiative_AlwaysRoll1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsInitiativeAlwaysRoll1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    UI.Label(m_SkillChecks__LowerIsBetterLocalizedText.Green());
                    using (HorizontalScope()) {
                        UI.Label(m_SkillChecks_Take50LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsSkillChecksTake50, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_SkillChecks_Take25LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsSkillChecksTake25, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_SkillChecks_Take1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsSkillChecksTake1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    UI.Label(m_Damage__HigherIsBetterLocalizedText.Green());
                    using (HorizontalScope()) {
                        UI.Label(m_DamageRolls_Take50LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsDamageTake50, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_DamageRolls_Take25LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsDamageTake25, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_DamageRolls_Take1LocalizedText, Width(labelWidth));
                        UI.SelectionGrid(ref Settings.DiceRollsDamageTake1, 8, e => e.GetLocalized(), Width(0.7f * EffectiveWindowWidth()));
                    }
                }
            }
        }
    }
#warning DEBUG; REMOVE!
    [HarmonyPatch(typeof(Initiative), nameof(Initiative.Roll), MethodType.Setter), HarmonyPrefix]
    private static void Initiative_setRoll_Patch(Initiative __instance, float value) {
        Log($"Setting initiative {value}:\n{new System.Diagnostics.StackTrace()}");
    }
    [HarmonyPatch(typeof(RulePerformAttackRoll), nameof(RulePerformAttackRoll.OnTrigger)), HarmonyPostfix]
    private static void RulePerformAttackRoll_OnTrigger_Patch(RulePerformAttackRoll __instance) {
        if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsAllAttacksHit) && __instance.Result != AttackResult.Hit && __instance.Result != AttackResult.CoverHit && __instance.Result != AttackResult.RighteousFury) {
            __instance.Result = AttackResult.Hit;
            __instance.ResultIsHit = true;
        }
        if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsAllAttacksCrit) && !__instance.ShouldHaveBeenRighteousFury) {
            __instance.ResultIsRighteousFury = true;
            __instance.Result = AttackResult.RighteousFury;
            __instance.RighteousFuryAmount = Math.Max(1f, __instance.RighteousFuryAmount);
        }
    }
    [HarmonyPatch(typeof(RuleStarshipRollAttack), nameof(RuleStarshipRollAttack.OnTrigger)), HarmonyPostfix]
    private static void RuleStarshipRollAttack_OnTrigger_Patch(RuleStarshipRollAttack __instance) {
        if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsAllAttacksHit) && !__instance.ResultIsHit && !__instance.ResultIsCrit) {
            __instance.ResultIsHit = true;
        }
        if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsAllAttacksCrit) && !__instance.ResultIsCrit) {
            __instance.ResultIsCrit = true;
        }
    }
    [HarmonyPatch(typeof(RuleRollInitiative), nameof(RuleRollInitiative.ResultD10), MethodType.Getter), HarmonyPostfix]
    private static void RuleRollInitiative_getResultD10_Patch(ref RuleRollD10 __result, RuleRollInitiative __instance) {
        if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsInitiativeAlwaysRoll1)) {
            __result.m_Result = 1;
        } else if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsInitiativeAlwaysRoll5)) {
            __result.m_Result = 5;
        } else if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsInitiativeAlwaysRoll10)) {
            __result.m_Result = 10;
        } else if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsRollWithAdvantage)) {
            // Reroll Amount > 0 takes Minimum; but Initiative is better if larger, so we want maximum for advantage.
            __result.m_RerollAmount = 0;
            __result.Reroll();
        } else if (ToyBoxUnitHelper.IsOfSelectedType(__instance.InitiatorUnit, Settings.DiceRollsRollWithDisadvantage)) {
            __result.m_RerollAmount = 1;
            __result.Reroll();
        }
    }
    // Nullify damage rolls are done with the attacker as the initiator
    // E.g. Roll1 for Party => will also roll 1 for the TryNullifyDamage RuleRollChance, since the initiator (attacker) is a party member; meaning it would cause issues
    // Assuming no re-entry calls
    [ThreadStatic]
    private static BaseUnitEntity? m_OverrideInitiator;
    [HarmonyPatch(typeof(RuleRollDamage), nameof(RuleRollDamage.TryNullifyDamage)), HarmonyPrefix]
    private static void RuleRollDamage_TryNullifyDamage_PrePatch(RuleRollDamage __instance) {
        m_OverrideInitiator = __instance.TargetUnit;
    }
    [HarmonyPatch(typeof(RuleRollDamage), nameof(RuleRollDamage.TryNullifyDamage)), HarmonyPostfix]
    private static void RuleRollDamage_TryNullifyDamage_PostPatch() {
        m_OverrideInitiator = null;
    }
    [HarmonyPatch(typeof(RuleRollDice), nameof(RuleRollDice.Roll)), HarmonyPostfix]
    private static void RuleRollDice_Roll_Patch(RuleRollDice __instance) {
        var isDamageRule = false;
        var partyReplace = 0;
        if (Settings.DiceRollsSkillChecksTake1 != UnitSelectType.Off) {
            partyReplace = 1;
        } else if (Settings.DiceRollsSkillChecksTake25 != UnitSelectType.Off) {
            partyReplace = 25;
        } else if (Settings.DiceRollsSkillChecksTake50 != UnitSelectType.Off) {
            partyReplace = 50;
        }

        foreach (var evt in Rulebook.CurrentContext?.m_EventStack ?? []) {
            if (evt is RulePerformPartySkillCheck) {
                __instance.m_Result = partyReplace;
                return;
            } else if (evt is RulePerformSkillCheck skillCheck) {
                if (ToyBoxUnitHelper.IsOfSelectedType(skillCheck.InitiatorUnit, Settings.DiceRollsSkillChecksTake1)) {
                    __instance.m_Result = 1;
                } else if (ToyBoxUnitHelper.IsOfSelectedType(skillCheck.InitiatorUnit, Settings.DiceRollsSkillChecksTake25)) {
                    __instance.m_Result = 25;
                } else if (ToyBoxUnitHelper.IsOfSelectedType(skillCheck.InitiatorUnit, Settings.DiceRollsSkillChecksTake50)) {
                    __instance.m_Result = 50;
                } else if (ToyBoxUnitHelper.IsOfSelectedType(skillCheck.InitiatorUnit, Settings.DiceRollsRollWithAdvantage)) {
                    __instance.m_RerollAmount = 1;
                    __instance.Reroll();
                } else if (ToyBoxUnitHelper.IsOfSelectedType(skillCheck.InitiatorUnit, Settings.DiceRollsRollWithDisadvantage)) {
                    __instance.m_RerollAmount = 0;
                    __instance.Reroll();
                }
                return;
            } else if (evt is RuleDealDamage) {
                isDamageRule = true;
            }
        }
        var initiator = m_OverrideInitiator ?? __instance.InitiatorUnit;
        if (!initiator.IsInCombat) {
            if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsOutOfCombatTake1)) {
                __instance.m_Result = 1;
                return;
            } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsOutOfCombatTake25)) {
                __instance.m_Result = 25;
                return;
            } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsOutOfCombatTake50)) {
                __instance.m_Result = 50;
                return;
            }
        } else if (isDamageRule) {
            if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsDamageTake1)) {
                __instance.m_Result = 1;
                return;
            } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsDamageTake25)) {
                __instance.m_Result = 25;
                return;
            } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsDamageTake50)) {
                __instance.m_Result = 50;
                return;
            }
        }
        if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsAlwaysRoll1)) {
            __instance.m_Result = 1;
            return;
        } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsAlwaysRoll50)) {
            __instance.m_Result = 50;
            return;
        } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsAlwaysRoll100)) {
            __instance.m_Result = 100;
            return;
        }

        var minInclusive = 1;
        var maxExclusive = 101;
        if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsNeverRoll1)) {
            minInclusive = 2;
        }
        if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsNeverRoll100)) {
            maxExclusive = 100;
        }
        var currentAttempt = 0;
        do {
            currentAttempt++;
            if (__instance.m_Result < minInclusive || __instance.m_Result >= maxExclusive) {
                Roll(__instance);
            }
            if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsRollWithAdvantage)) {
                // Reroll Amount > 0 takes Minimum
                __instance.m_RerollAmount = isDamageRule ? 0 : 1;
                __instance.Reroll();
            } else if (ToyBoxUnitHelper.IsOfSelectedType(initiator, Settings.DiceRollsRollWithDisadvantage)) {
                __instance.m_RerollAmount = isDamageRule ? 1 : 0;
                __instance.Reroll();
            }
        } while (currentAttempt < MaxAttempts && (__instance.m_Result < minInclusive || __instance.m_Result >= maxExclusive));
    }
    // Calling __instance.Roll would cause recursion; so here is a simple reimplementation
    private static void Roll(RuleRollDice roll) {
        roll.m_Result = roll.m_PreRolledResult ?? Dice.D(roll.DiceFormula);
        roll.RollHistory.Add(roll.m_Result);
    }
    public const int MaxAttempts = 5;

    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_AdvantageMeansDoingTwoRollsAndTaLocalizedText", "Advantage means doing two rolls and taking the better result; which for Initiative and Damage rolls means the higher number and for Skill Checks the lower number.")]
    private static partial string m_AdvantageMeansDoingTwoRollsAndTaLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_AllAttacksHitLocalizedText", "All Attacks Hit")]
    private static partial string m_AllAttacksHitLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_AllAttacksCritLocalizedText", "All Attacks Crit")]
    private static partial string m_AllAttacksCritLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_RollWithAdvantageLocalizedText", "Roll with Advantage")]
    private static partial string m_RollWithAdvantageLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_RollWithDisadvantageLocalizedText", "Roll with Disadvantage")]
    private static partial string m_RollWithDisadvantageLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Take100LocalizedText", "Take 100")]
    private static partial string m_Take100LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Take50LocalizedText", "Take 50")]
    private static partial string m_Take50LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Take1LocalizedText", "Take 1")]
    private static partial string m_Take1LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_NeverRoll100LocalizedText", "Never Roll 100")]
    private static partial string m_NeverRoll100LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_NeverRoll1LocalizedText", "Never Roll 1")]
    private static partial string m_NeverRoll1LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_OutOfCombat_Take50LocalizedText", "Out of Combat: Take 50")]
    private static partial string m_OutOfCombat_Take50LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_OutOfCombat_Take25LocalizedText", "Out of Combat: Take 25")]
    private static partial string m_OutOfCombat_Take25LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_OutOfCombat_Take1LocalizedText", "Out of Combat: Take 1")]
    private static partial string m_OutOfCombat_Take1LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Initiative__HigherIsBetterLocalizedText", "Initiative -> Higher is better")]
    private static partial string m_Initiative__HigherIsBetterLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Initiative_AlwaysRoll10LocalizedText", "Initiative: Always Roll 10")]
    private static partial string m_Initiative_AlwaysRoll10LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Initiative_AlwaysRoll5LocalizedText", "Initiative: Always Roll 5")]
    private static partial string m_Initiative_AlwaysRoll5LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Initiative_AlwaysRoll1LocalizedText", "Initiative: Always Roll 1")]
    private static partial string m_Initiative_AlwaysRoll1LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_SkillChecks__LowerIsBetterLocalizedText", "Skill Checks -> Lower is better")]
    private static partial string m_SkillChecks__LowerIsBetterLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_SkillChecks_Take50LocalizedText", "Skill Checks: Take 50")]
    private static partial string m_SkillChecks_Take50LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_SkillChecks_Take25LocalizedText", "Skill Checks: Take 25")]
    private static partial string m_SkillChecks_Take25LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_SkillChecks_Take1LocalizedText", "Skill Checks: Take 1")]
    private static partial string m_SkillChecks_Take1LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_Damage__HigherIsBetterLocalizedText", "Damage -> Higher is better")]
    private static partial string m_Damage__HigherIsBetterLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_DamageRolls_Take50LocalizedText", "Damage Rolls: Take 50")]
    private static partial string m_DamageRolls_Take50LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_DamageRolls_Take25LocalizedText", "Damage Rolls: Take 25")]
    private static partial string m_DamageRolls_Take25LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_DiceRolls_DiceRollsOverridesFeature_m_DamageRolls_Take1LocalizedText", "Damage Rolls: Take 1")]
    private static partial string m_DamageRolls_Take1LocalizedText { get; }
}
