using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.UnitLogic.Parts;

namespace ToyBox.Features.DialogAndNpc;

public class InterestingnessEntry(BaseUnitEntity unit, object source, ConditionsChecker? checker, List<Element>? elements = null) {
    public BaseUnitEntity Unit = unit;
    public object Source = source;
    public ConditionsChecker? Checker = checker;
    public List<Element>? Elements = elements;
    public bool HasConditions {
        get {
            return Checker?.Conditions.Length > 0;
        }
    }

    public bool HasElements {
        get {
            return Elements?.Count > 0;
        }
    }
}

public static class InterestingNpcUtils {
    public static bool IsActive(this InterestingnessEntry entry) {
        return (entry.Checker?.IsActive() ?? false)
        || (entry.Elements?.Any(element => {
            try {
                return element.IsActive();
            } catch (Exception ex) {
                Debug(ex.ToString());
                return false;
            }
        }) ?? false)
        || (entry.Elements?.Count > 0 && entry.Source is ActionsHolder);
    }

    public static bool IsActive(this Element element) {
        return element switch {
            Conditional conditional => conditional.ConditionsChecker.Check(),
            Condition condition => condition.CheckCondition(),
            _ => false,
        };
    }

    public static bool IsActive(this ConditionsChecker checker) {
        return checker.Conditions.Any(c => c.CheckCondition());
    }

    public static bool IsQuestRelated(this Element element) {
        return element is GiveObjective or SetObjectiveStatus or StartEtude or CompleteEtude or UnlockFlag or ObjectiveStatus or ItemsEnough or Conditional;
    }

    public static int InterestingnessCoefficent(this MechanicEntity entity) {
        return entity is BaseUnitEntity unit ? unit.InterestingnessCoefficent() : 0;
    }

    public static int InterestingnessCoefficent(this BaseUnitEntity unit) {
        return unit.GetUnitInteractionConditions().Count(entry => {
            try {
                return entry.IsActive();
            } catch (Exception ex) {
                Debug(ex.ToString());
                return false;
            }
        });
    }

    private static List<SpawnerInteraction> GetSpawnerInteractions(this BaseUnitEntity unit) {
        return unit.GetOptional<UnitPartInteractions>()?.Interactions
            .OfType<SpawnerInteractionPart.Wrapper>()
            .Select(w => w.Source)
            .ToList() ?? [];
    }

    public static List<BlueprintDialog> GetDialog(this BaseUnitEntity unit) {
        return [.. unit.GetSpawnerInteractions()
            .OfType<SpawnerInteractionDialog>()
            .Select(sid => sid.Dialog)
            .Where(d => d != null)];
    }

    public static IEnumerable<InterestingnessEntry> GetUnitInteractionConditions(this BaseUnitEntity unit) {
        var spawnInteractions = unit.GetSpawnerInteractions();
        var result = new HashSet<InterestingnessEntry>();
        var elements = new HashSet<InterestingnessEntry>();

        // dialog
        var dialogInteractions = spawnInteractions.OfType<SpawnerInteractionDialog>().Where(di => di.Dialog != null).ToList();
        // dialog interaction conditions
        result.UnionWith(dialogInteractions
                            .Where(di => di.Conditions?.Get() != null)
                            .Select(di => new InterestingnessEntry(unit, di.Dialog, di.Conditions.Get().Conditions)));
        // dialog conditions
        result.UnionWith(dialogInteractions
                            .Select(di => new InterestingnessEntry(unit, di.Dialog, di.Dialog.Conditions)));
        // dialog elements
        elements.UnionWith(dialogInteractions
                            .Select(di => new InterestingnessEntry(unit, di.Dialog, null, di.Dialog.ElementsArray.Where(e => e.IsQuestRelated()).ToList())));
        // dialog cue conditions
        result.UnionWith(dialogInteractions
                            .Where(di => di.Dialog.FirstCue != null)
                            .SelectMany(di => di.Dialog.FirstCue.Cues
                                                .Where(cueRef => cueRef.Get() != null)
                                                .Select(cueRef => new InterestingnessEntry(unit, cueRef.Get(), cueRef.Get().Conditions))));

        // actions
        var actionInteractions = spawnInteractions.OfType<SpawnerInteractionActions>().ToList();
        // action interaction conditions
        result.UnionWith(actionInteractions
                            .Where(ai => ai.Conditions?.Get() != null)
                            .Select(ai => new InterestingnessEntry(unit, ai, ai.Conditions.Get().Conditions)));
        // action conditions
        result.UnionWith(actionInteractions
                            .SelectMany(ai => ai.ActionHolders)
                            .Where(ai => ai?.Get() != null)
                            .SelectMany(ai => ai.Get().Actions.Actions
                                                .OfType<Conditional>()
                                                .Select(a => new InterestingnessEntry(unit, ai.Get(), a.ConditionsChecker))));
        // action elements
        elements.UnionWith(actionInteractions
                            .SelectMany(ai => ai.ActionHolders)
                            .Where(ai => ai?.Get() != null)
                            .Select(ai => new InterestingnessEntry(unit, ai.Get(), null, ai.Get().ElementsArray.Where(e => e.IsQuestRelated()).ToList())));

        // Pull Conditionals out of element lists into their own condition entries so they show under "Conditions".
        foreach (var entry in elements) {
            var conditionals = entry.Elements!.OfType<Conditional>().ToList();
            if (conditionals.Count > 0) {
                foreach (var conditional in conditionals) {
                    _ = result.Add(new InterestingnessEntry(entry.Unit, conditional, conditional.ConditionsChecker));
                }
                entry.Elements = entry.Elements.Where(element => element is not Conditional).ToList();
            }
        }
        result.UnionWith(elements);
        return result;
    }
}
