using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;

namespace ToyBox.Features.Etudes;

public class ConflictingGroupIdReferences {
    public string Name;
    public List<string> Etudes = new();
}
public class EtudeInfo {
    public enum EtudeState {
        NotStarted = 0,
        Started = 1,
        Active = 2,
        CompleteBeforeActive = 3,
        Completed = 4,
        CompletionBlocked = 5
    }

    public string? Name;
    public BlueprintEtude Blueprint;
    public string ParentId;
    public List<string> LinkedId = new();
    public List<string> ChainedId = new();
    public string LinkedTo;
    public string ChainedTo;
    public List<string> ChildrenId = new();
    public bool AllowActionStart;
    public EtudeState State;
    public string LinkedArea;
    public bool CompleteParent;
    public string Comment;
    public bool ShowChildren;
    public bool ShowElements;
    public bool ShowConflicts;
    public bool ShowActions;
    public bool hasSearchResults;
    public List<string> ConflictingGroups = new();
    public int Priority;
}
public class EtudeDrawerData {
    public bool ShowChildren;
    public Dictionary<string, EtudeInfo> ChainStarts = new Dictionary<string, EtudeInfo>();
    public bool NeedToPaint;
    public int Depth;
}
