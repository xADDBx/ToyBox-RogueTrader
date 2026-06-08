using Kingmaker.AreaLogic.Etudes;
using Kingmaker.ElementsSystem;

namespace ToyBox.Features.Etudes;
public partial class EtudeRecord {
    public BlueprintEtude Blueprint { get; }
    public string Name {
        get {
            return BPHelper.GetTitle(Blueprint);
        }
    }

    public EtudeRecord? Parent;
    public readonly List<EtudeRecord> Children = [];

    public readonly List<EtudeRecord> LinkedEtudes = [];
    public readonly List<EtudeRecord> ChainedEtudes = [];
    public EtudeRecord? LinkedTo;
    public EtudeRecord? ChainedTo;

    public readonly List<BlueprintEtudeConflictingGroup> ConflictingGroups = [];

    public string? LinkedAreaId {
        get {
            return Blueprint.LinkedAreaPart?.AssetGuid ?? "";
        }
    }

    public HashSet<string> DescendantAreaIds = [];
    public HashSet<string> AncestorAreaIds = [];
    public bool IsExpanded;
    public bool IsElementsExpanded;
    public bool IsConflictsExpanded;
    public bool IsRoot;
    public bool IsFlagLike;
    public bool IsDescendantFlagLike;
    public bool IsMatched;
    public bool IsDescendantMatched;
    public void ExpandChildren(bool expanded) {
        Stack<EtudeRecord> stack = [];
        stack.Push(this);
        var visited = new HashSet<EtudeRecord>();

        while (stack.Count > 0) {
            var current = stack.Pop();
            current.IsExpanded = expanded;

            if (!visited.Add(current)) {
                continue;
            }

            foreach (var child in current.Children) {
                stack.Push(child);
            }
        }
    }

    public string? ValidationProblem {
        get {
            if (ChainedTo == null && LinkedTo == null) {
                // return "Chained/Linked to Nothing";
                return null;
            }

            foreach (var chained in ChainedEtudes) {
                if (chained.Parent != Parent) {
                    return string.Format(m_ChainedEtude_0___1__HasDifferentLocalizedText, chained.Name, chained.Name, chained.Parent?.Blueprint.AssetGuid ?? m_NoneLocalizedText, Name, Parent?.Blueprint.AssetGuid ?? m_NoneLocalizedText);
                    // return $"Chained etude {chained.Name} ({chained.Blueprint.AssetGuid}) has different parent: {chained.Parent?.Blueprint.AssetGuid ?? "None"} than {Name} parent: {Parent?.Blueprint.AssetGuid ?? "None"}";
                }
            }

            foreach (var linked in LinkedEtudes) {
                if (linked.Parent != Parent && linked.Parent != this) {
                    return string.Format(m_LinkedToChild_0___1__WithDiffereLocalizedText, linked.Name, linked.Blueprint.AssetGuid, linked.Parent?.Blueprint.AssetGuid ?? m_NoneLocalizedText, Name, Parent?.Blueprint.AssetGuid ?? m_NoneLocalizedText);
                    // return $"Linked to child {linked.Name} ({linked.Blueprint.AssetGuid}) with different parent: {linked.Parent?.Blueprint.AssetGuid ?? "None"} than {Name} parent {Parent?.Blueprint.AssetGuid ?? "None"}";
                }
            }

            return null;
        }
    }

    public List<EtudeRecord> GetConflictingEtudes(EtudeSnapshot snapshot) {
        var result = new HashSet<EtudeRecord>();
        foreach (var groupId in ConflictingGroups) {
            if (snapshot.ConflictingGroups.TryGetValue(groupId, out var g)) {
                foreach (var etude in g) {
                    _ = result.Add(etude);
                }
            }
        }

        return [.. result.OrderBy(rec => {
            var activeBoost = rec.State == EtudeState.Active ? 100500 : 0;
            return -(rec.Priority + activeBoost);
        })];
    }
    public bool IsIndirectlyLinkedToArea(string id) {
        return LinkedAreaId == id || DescendantAreaIds.Contains(id) || AncestorAreaIds.Contains(id);
    }
    public bool IsIndirectlyFlagLike() {
        return IsFlagLike || IsDescendantFlagLike;
    }
    public string Comment {
        get {
            return Blueprint.Comment;
        }
    }

    public int Priority {
        get {
            return Blueprint.Priority;
        }
    }

    public bool CompletesParent {
        get {
            return Blueprint.m_CompletesParent;
        }
    }

    public EtudeState State;

    public List<Element> Elements {
        get {
            return Blueprint.m_AllElements;
        }
    }

    public EtudeRecord(BlueprintEtude blueprint) {
        Blueprint = blueprint;
    }

    [LocalizedString("ToyBox_Features_Etudes_EtudeRecord_m_ChainedEtude_0___1__HasDifferentLocalizedText", "Chained etude {0} ({1}) has different parent: {2} than {3} parent: {4}")]
    private static partial string m_ChainedEtude_0___1__HasDifferentLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Etudes_EtudeRecord_m_LinkedToChild_0___1__WithDiffereLocalizedText", "Linked to child {0} ({1}) with different parent: {2} than {3} parent {4}")]
    private static partial string m_LinkedToChild_0___1__WithDiffereLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Etudes_EtudeRecord_m_NoneLocalizedText", "None")]
    private static partial string m_NoneLocalizedText { get; }
}
