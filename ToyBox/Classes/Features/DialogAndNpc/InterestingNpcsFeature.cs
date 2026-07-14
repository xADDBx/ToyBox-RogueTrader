using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Inspector;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc;

public partial class InterestingNpcsFeature : Feature {
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_Name", "Interesting NPCs in the local area")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_Description", "Shows a list of NPCs that may have quest objectives or other interesting interactions. (Warning: Spoilers)")]
    public override partial string Description { get; }

    private Browser<BaseUnitEntity>? m_Browser;
    private bool m_Show = false;
    private readonly HashSet<BaseUnitEntity> m_ExpandedBreakdowns = [];

    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        using (HorizontalScope()) {
            if (UI.DisclosureToggle(ref m_Show, Name.Cyan())) {
                if (m_Show) {
                    RebuildItems();
                }
            }
            Space(20);
            UI.Label(Description.Green());
        }
        if (!m_Show) {
            return;
        }
        m_Browser ??= new(u => u.CharacterName, u => u.CharacterName);
        using (HorizontalScope()) {
            Space(50);
            using (VerticalScope(GUI.skin.box)) {
                m_Browser.OnGUI(RowGUI, HeaderGUI);
            }
        }
    }

    private void RebuildItems() {
        m_Browser ??= new(u => u.CharacterName, u => u.CharacterName);
        var pool = Game.Instance?.State?.AllBaseUnits;
        if (pool == null) {
            m_Browser.UpdateItems([]);
            return;
        }
        var units = Settings.InterestingNpcsShowHidden ? pool.All : pool.ToList();
        m_Browser.UpdateItems(units.Where(u => u != null && u.InterestingnessCoefficent() >= 1));
    }

    private void HeaderGUI() {
        using (HorizontalScope()) {
            _ = UI.Toggle(m_ShowInactiveConditionsText, null, ref Settings.ShowInactiveInterestingNpcConditions);
            Space(25);
            if (UI.Toggle(m_ShowOtherVersionsText, null, ref Settings.InterestingNpcsShowHidden)) {
                RebuildItems();
            }
            Space(25);
            _ = UI.Button(m_RefreshText.Cyan(), RebuildItems, null, AutoWidth());
        }
    }

    private void RowGUI(BaseUnitEntity u) {
        var coefficient = u.InterestingnessCoefficent();
        var dialogs = u.GetDialog();
        using (VerticalScope()) {
            using (HorizontalScope()) {
                var expanded = m_ExpandedBreakdowns.Contains(u);
                if (UI.DisclosureToggle(ref expanded, "", null, null, Width(UI.DisclosureGlyphWidth.Value))) {
                    _ = expanded ? m_ExpandedBreakdowns.Add(u) : m_ExpandedBreakdowns.Remove(u);
                }
                var name = coefficient > 0 ? u.CharacterName.Orange() : u.CharacterName.Grey();
                UI.Label(name, Width(400));
                Space(20);
                UI.Label(m_InterestingnessCoefficientText.Grey() + coefficient.ToString().Cyan());
                Space(20);
                InspectorUI.InspectToggle(u, m_UnitText.Cyan());
                if (dialogs.Count > 0) {
                    Space(10);
                    InspectorUI.InspectToggle((u, "dialog"), m_DialogText.Cyan(), dialogs.Count == 1 ? dialogs[0] : dialogs);
                }
            }
            if (m_ExpandedBreakdowns.Contains(u)) {
                BreakdownGUI(u);
            }
            InspectorUI.InspectIfExpanded(u);
            if (dialogs.Count > 0) {
                InspectorUI.InspectIfExpanded((u, "dialog"), dialogs.Count == 1 ? dialogs[0] : dialogs);
            }
        }
    }

    private void BreakdownGUI(BaseUnitEntity u) {
        var showInactive = Settings.ShowInactiveInterestingNpcConditions;
        var entries = u.GetUnitInteractionConditions().ToList();
        var checkerEntries = entries.Where(e => e.HasConditions && (showInactive || SafeIsActive(e)));
        var conditions = checkerEntries
            .SelectMany(entry => entry.Checker!.Conditions.Select(condition => (condition, source: entry.Source)))
            .GroupBy(x => x.condition.GetCaption())
            .Select(g => (g.First().condition, sources: g.Select(x => x.source).ToList()))
            .ToList();
        var elementEntries = entries.Where(e => e.HasElements && (showInactive || SafeIsActive(e))).ToList();
        if (conditions.Count > 0) {
            using (HorizontalScope()) {
                Space(115);
                UI.Label(m_ConditionsText.Yellow());
            }
        }
        foreach (var (condition, sources) in conditions) {
            ElementGUI(condition, string.Join(", ", sources.Select(s => s.ToString())), 150);
        }
        if (elementEntries.Count > 0) {
            using (HorizontalScope()) {
                Space(115);
                UI.Label(m_ElementsText.Yellow());
            }
        }
        foreach (var entry in elementEntries) {
            foreach (var element in entry.Elements!.OrderBy(e => e.GetType().Name)) {
                ElementGUI(element, entry.Source);
            }
        }
    }

    private static bool SafeIsActive(InterestingnessEntry entry) {
        try {
            return entry.IsActive();
        } catch (Exception ex) {
            Debug(ex.ToString());
            return false;
        }
    }

    private void ElementGUI(Element element, object source, int indent = 150, bool forceShow = false) {
        if (!element.IsActive()
            && source is not ActionsHolder
            && !Settings.ShowInactiveInterestingNpcConditions
            && !forceShow) {
            return;
        }
        using (HorizontalScope()) {
            Space(indent);
            switch (element) {
                case ObjectiveStatus objectiveStatus:
                    ObjectiveStatusGUI(objectiveStatus, source);
                    break;
                case QuestStatus questStatus:
                    QuestStatusGUI(questStatus, source);
                    break;
                case EtudeStatus etudeStatus:
                    EtudeStatusGUI(etudeStatus, source);
                    break;
                case Conditional conditional:
                    ConditionalGUI(conditional, source);
                    break;
                case Condition condition:
                    ConditionGUI(condition, source);
                    break;
                default:
                    OtherElementGUI(element, source);
                    break;
            }
        }
    }

    private void ConditionsCheckerGUI(ConditionsChecker checker, object source, int indent = 150, bool forceShow = false) {
        foreach (var condition in checker.Conditions.OrderBy(c => c.GetType().Name)) {
            ElementGUI(condition, source, indent, forceShow);
        }
    }

    private void ConditionalGUI(Conditional conditional, object source) {
        if (conditional.ConditionsChecker.Conditions.Length == 0) {
            return;
        }
        UI.Label(m_ConditionalText.Cyan(), Width(150));
        UI.Label(conditional.Comment ?? "", Width(375));
        using (VerticalScope()) {
            ConditionsCheckerGUI(conditional.ConditionsChecker, source, 0, true);
        }
    }

    private void QuestStatusGUI(QuestStatus questStatus, object source) {
        UI.Label(m_QuestStatusText.Cyan(), Width(150));
        var quest = questStatus.Quest;
        var state = Game.Instance.Player.QuestBook.GetQuestState(quest);
        UI.Label(ColorByState(quest.Title.Text, state).Bold(), Width(500));
        Space(22);
        using (VerticalScope()) {
            UI.Label(quest.Description.Text.Green());
            UI.Label(m_StatusText.Cyan() + state.ToString());
            UI.Label(m_ConditionText.Cyan() + CaptionString(questStatus));
            UI.Label(m_SourceText.Cyan() + source.ToString().Yellow());
        }
    }

    private void ObjectiveStatusGUI(ObjectiveStatus objectiveStatus, object source) {
        UI.Label(m_ObjectiveStatusText.Cyan(), Width(150));
        var objectiveBP = objectiveStatus.QuestObjective;
        var objective = Game.Instance.Player.QuestBook.GetObjective(objectiveBP);
        var quest = objectiveBP.Quest;
        var state = objective?.State ?? QuestObjectiveState.None;
        var title = $"{quest.Title.Text.Orange().Bold()} : {ColorByState(objectiveBP.Title.Text, state)}";
        UI.Label(title, Width(500));
        Space(22);
        using (VerticalScope()) {
            UI.Label(objectiveBP.Description.Text.Green());
            UI.Label(m_StatusText.Cyan() + ColorByState(state.ToString(), state));
            UI.Label(m_ConditionText.Cyan() + CaptionString(objectiveStatus));
            UI.Label(m_SourceText.Cyan() + source.ToString().Yellow());
        }
    }

    private void EtudeStatusGUI(EtudeStatus etudeStatus, object source) {
        UI.Label(m_EtudeStatusText.Cyan(), Width(150));
        var etudeBP = etudeStatus.Etude;
        UI.Label(etudeBP.name.Orange(), Width(500));
        var etudeState = Game.Instance.Player.EtudesSystem.GetSavedState(etudeBP);
        var debugInfo = Game.Instance.Player.EtudesSystem.GetDebugInfo(etudeBP);
        Space(22);
        using (VerticalScope()) {
            UI.Label(debugInfo.Green());
            UI.Label(m_StatusText.Cyan() + etudeState.ToString());
            UI.Label(m_ConditionText.Cyan() + CaptionString(etudeStatus));
            UI.Label(m_SourceText.Cyan() + source.ToString().Yellow());
        }
    }

    private void ConditionGUI(Condition condition, object source) {
        UI.Label($"{condition.GetType().Name}:".Cyan(), Width(150));
        UI.Label(source.ToString().Yellow(), Width(500));
        Space(22);
        using (VerticalScope()) {
            UI.Label(m_ConditionText.Cyan() + CaptionString(condition));
        }
    }

    private void OtherElementGUI(Element element, object source) {
        UI.Label($"{element.GetType().Name}:".Cyan(), Width(150));
        UI.Label(source.ToString().Yellow(), Width(500));
        Space(22);
        using (VerticalScope()) {
            UI.Label(m_CaptionText.Cyan() + element.GetCaption().Orange());
        }
    }

    private static string CaptionString(Condition condition) {
        return $"{condition.GetCaption().Orange()} -> {(condition.CheckCondition() ? m_TrueText.Green() : m_FalseText.Yellow())}";
    }

    private static string ColorByState(string text, QuestObjectiveState state) {
        return state switch {
            QuestObjectiveState.None => text.Grey(),
            QuestObjectiveState.Started => text.Cyan(),
            QuestObjectiveState.Completed => text.Green(),
            QuestObjectiveState.Failed => text.Red(),
            QuestObjectiveState.Postponed => text.Yellow(),
            _ => text.Yellow(),
        };
    }

    private static string ColorByState(string text, QuestState state) {
        return state switch {
            QuestState.None => text.Grey(),
            QuestState.Started => text.Cyan(),
            QuestState.Completed => text.Green(),
            QuestState.Failed => text.Red(),
            QuestState.Updated => text.Yellow(),
            QuestState.Postponed => text.Yellow(),
            _ => text.Yellow(),
        };
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ShowInactiveConditionsText", "Show Inactive Conditions")]
    private static partial string m_ShowInactiveConditionsText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ShowOtherVersionsText", "Show other versions of NPCs")]
    private static partial string m_ShowOtherVersionsText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_RefreshText", "Refresh")]
    private static partial string m_RefreshText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_InterestingnessCoefficientText", "Interestingness Coefficient: ")]
    private static partial string m_InterestingnessCoefficientText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_UnitText", "Unit")]
    private static partial string m_UnitText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_DialogText", "Dialog")]
    private static partial string m_DialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ConditionsText", "Conditions")]
    private static partial string m_ConditionsText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ElementsText", "Elements")]
    private static partial string m_ElementsText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ConditionalText", "Conditional:")]
    private static partial string m_ConditionalText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_QuestStatusText", "Quest Status: ")]
    private static partial string m_QuestStatusText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ObjectiveStatusText", "Objective Status: ")]
    private static partial string m_ObjectiveStatusText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_EtudeStatusText", "Etude Status: ")]
    private static partial string m_EtudeStatusText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_StatusText", "status: ")]
    private static partial string m_StatusText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_ConditionText", "condition: ")]
    private static partial string m_ConditionText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_SourceText", "source: ")]
    private static partial string m_SourceText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_CaptionText", "caption: ")]
    private static partial string m_CaptionText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_TrueText", "True")]
    private static partial string m_TrueText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InterestingNpcsFeature_m_FalseText", "False")]
    private static partial string m_FalseText { get; }
}
