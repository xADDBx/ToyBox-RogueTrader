using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.ContextData;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility.DotNetExtensions;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public enum NpcDialogMarkerState {
    None,
    Inactive,
    New,
    Repeat,
    Unknown,
}

/// <summary>
/// Pure, side-effect-free dialog classification for the Enhanced Map family.
/// No VM/View/Harmony/Settings references so the offline test project can link
/// this file against hand-rolled contract doubles.
/// </summary>
public static class NpcDialogClassifier {
    // Master-gate predicates, pure so the offline harness can exercise the
    // truth table. The marker family is master AND per-feature-toggle.
    public static bool NpcMarkersEffective(bool master, bool enabled) => master && enabled;
    public static bool ExitMarkersEffective(bool master, bool enabled) => master && enabled;
    public static bool LootHidingEffective(bool master, bool hideLooted) => master && hideLooted;

    // Used to pick the most interesting state when a unit carries several dialogs.
    public static int InterestRank(NpcDialogMarkerState state) => state switch {
        NpcDialogMarkerState.New => 3,
        NpcDialogMarkerState.Repeat => 2,
        NpcDialogMarkerState.Inactive => 1,
        _ => 0,
    };

    public static bool IsExcluded(BaseUnitEntity unit) {
        if (unit == null) {
            return true;
        }
        if (unit.Suppressed) {
            return true;
        }
        if (unit.LifeState is { IsDead: true } or { IsDeadOrUnconscious: true }) {
            return true;
        }
        if (unit.Faction is { IsPlayer: true } or { IsPlayerEnemy: true }) {
            return true;
        }
        if (unit.IsPet || unit.GetOptional<UnitPartSummonedMonster>() != null) {
            return true;
        }
        if (unit.GetOptional<PartUnitState>() is { IsHelpless: true }) {
            return true;
        }
        return false;
    }

    // All three verified attachment routes; etude-injected dialogs do not set
    // HasDialogInteractions, which is why the explicit scan exists.
    public static BlueprintDialog? GetDialog(IUnitInteraction interaction) => interaction switch {
        DialogOnClick d => d.Dialog,
        SpawnerInteractionPart.Wrapper { Source: SpawnerInteractionDialog s } => s.Dialog,
        EtudeBracketOverrideUnitInteraction { Source: EtudeBracketOverrideDialog e } => (BlueprintDialog?)e.Dialog?.GetBlueprint(),
        _ => null,
    };

    public static List<BlueprintDialog> GetDialogs(BaseUnitEntity unit) {
        var result = new List<BlueprintDialog>();
        var part = unit?.GetOptional<UnitPartInteractions>();
        if (part == null) {
            return result;
        }
        foreach (var interaction in part.Interactions) {
            var dialog = GetDialog(interaction);
            if (dialog != null && !result.Contains(dialog)) {
                result.Add(dialog);
            }
        }
        return result;
    }

    public static bool IsAttached(BaseUnitEntity unit) {
        var part = unit?.GetOptional<UnitPartInteractions>();
        if (part == null) {
            return false;
        }
        return part.HasDialogInteractions
            || part.Interactions.Any(i => i is EtudeBracketOverrideUnitInteraction);
    }

    /// <summary>
    /// Replicates the game's dialog start gate (root conditions + any first cue
    /// passing CanShow) WITHOUT calling CueSelection.Select: Select draws from
    /// PFStatefulRandom.DialogSystem on Random-strategy cue lists and would
    /// corrupt save RNG. Context wrapping is verbatim from
    /// UnitInteractionComponent.IsAvailable.
    /// </summary>
    public static bool WouldStart(BaseUnitEntity unit, BaseUnitEntity initiator, BlueprintDialog dialog) {
        using (ContextData<InteractingUnitData>.Request().Setup(initiator))
        using (ContextData<ClickedUnitData>.Request().Setup(unit)) {
            if (dialog.Conditions != null && !dialog.Conditions.Check(null)) {
                return false;
            }
            var cues = dialog.FirstCue?.Cues;
            if (cues == null || cues.Count == 0) {
                return false;
            }
            return cues.Dereference().Any(c => c != null && c.CanShow());
        }
    }

    public static bool HasSeen(BlueprintDialog dialog) => Game.Instance?.Player?.Dialog?.ShownDialogsContains(dialog) ?? false;

    public static NpcDialogMarkerState Classify(BaseUnitEntity unit, BaseUnitEntity initiator) {
        if (unit == null || initiator == null || IsExcluded(unit)) {
            return NpcDialogMarkerState.None;
        }
        var dialogs = GetDialogs(unit);
        if (dialogs.Count == 0) {
            return NpcDialogMarkerState.None;
        }
        if (Game.Instance?.DialogController?.Dialog != null) {
            // A dialog is playing: availability conditions lie; render plain.
            return NpcDialogMarkerState.Unknown;
        }
        var best = NpcDialogMarkerState.None;
        foreach (var dialog in dialogs) {
            try {
                if (!WouldStart(unit, initiator, dialog)) {
                    best = Best(best, NpcDialogMarkerState.Inactive);
                    continue;
                }
                best = Best(best, HasSeen(dialog) ? NpcDialogMarkerState.Repeat : NpcDialogMarkerState.New);
            } catch (Exception ex) {
                // Any condition-tree failure yields UNKNOWN: never a negative
                // marker. Broken blueprints can fail every unit in an area,
                // so the warning is capped like the patch guards.
                if (s_WarnCount < MaxWarnCount) {
                    s_WarnCount++;
                    Warn($"{nameof(NpcDialogClassifier)}: classification failed for {unit}: {ex.Message}");
                }
                return NpcDialogMarkerState.Unknown;
            }
        }
        return best;
    }

    private static NpcDialogMarkerState Best(NpcDialogMarkerState a, NpcDialogMarkerState b) =>
        InterestRank(b) > InterestRank(a) ? b : a;

    private const int MaxWarnCount = 3;
    private static int s_WarnCount;
}

/// <summary>
/// Stale-while-revalidate classification cache, logically keyed by
/// (unit, initiator): the unit is the dictionary key and the initiator is
/// stored per entry, so invalidation by unit is an O(1) remove instead of a
/// key scan (edges fire per unit, per Tab press). The clock is injectable so
/// staleness is testable offline; production uses wall-clock ticks and a 1 s
/// bound.
/// </summary>
public sealed class NpcClassificationCache {
    private sealed class Entry {
        public NpcDialogMarkerState State;
        public long Stamp;
        public BaseUnitEntity? Initiator;
    }

    private readonly Dictionary<BaseUnitEntity, Entry> m_Entries = new();

    public Func<long> Clock { get; set; } = DefaultClock;
    public long StalenessTicks { get; set; } = TimeSpan.TicksPerSecond;

    public int Count => m_Entries.Count;

    public NpcDialogMarkerState GetOrAdd(BaseUnitEntity unit, BaseUnitEntity initiator, Func<NpcDialogMarkerState> evaluate) {
        if (TryGetFresh(unit, initiator, out var state)) {
            return state;
        }
        state = evaluate();
        Store(unit, initiator, state);
        return state;
    }

    public bool TryGetFresh(BaseUnitEntity unit, BaseUnitEntity initiator, out NpcDialogMarkerState state) {
        if (m_Entries.TryGetValue(unit, out var entry)
            && entry.Initiator == initiator
            && Clock() - entry.Stamp < StalenessTicks) {
            state = entry.State;
            return true;
        }
        state = NpcDialogMarkerState.None;
        return false;
    }

    public void Store(BaseUnitEntity unit, BaseUnitEntity initiator, NpcDialogMarkerState state) {
        m_Entries[unit] = new Entry { State = state, Stamp = Clock(), Initiator = initiator };
    }

    public bool Invalidate(BaseUnitEntity unit) => m_Entries.Remove(unit);

    public void InvalidateAll() => m_Entries.Clear();

    private static long DefaultClock() => DateTime.UtcNow.Ticks;
}
