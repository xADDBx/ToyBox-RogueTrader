namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public enum InteractableMarkerState {
    None,
    New,
    Repeat,
    Check,
}

/// <summary>
/// Pure, side-effect-free classification for the map-OBJECT marker family
/// (marker-less exits + dialog/skill-check interactables). No VM/View/
/// Harmony/Settings references so the offline test project can link this
/// file against hand-rolled contract doubles, exactly like
/// NpcDialogClassifier.
/// </summary>
public static class MapObjectMarkerClassifier {
    // Master-gate predicate, same shape as the NPC family's: master AND
    // per-feature toggle.
    public static bool InteractableMarkersEffective(bool master, bool enabled) => master && enabled;

    // ---- Marker-less exits (Feature: LocalMapExitMarkersFeature) ----

    /// <summary>
    /// Dedupe key logic for appending an exit marker: ONLY transitions whose
    /// scene data has AddMapMarker=0 AND no LocalMapMarkerPart (the vanilla
    /// path creates one at settings-set time for AddMapMarker=1, including
    /// scene-placed LocalMapMarker components on the same object). Etude/flag
    /// gates stay authoritative via ownerInGame - a hidden-by-etude exit must
    /// never be appended (footfall-atrium stays hidden by design).
    /// </summary>
    public static bool ShouldAppendExitMarker(bool settingsAddMapMarker, bool hasLocalMapMarkerPart, bool ownerInGame, bool suppressed, bool isInCurrentArea)
        => !settingsAddMapMarker && !hasLocalMapMarkerPart && ownerInGame && !suppressed && isInCurrentArea;

    // ---- Interactable objects (Feature: LocalMapInteractableMarkersFeature) ----

    /// <summary>
    /// Scope gate: dialog-opening interactions and skill checks are IN;
    /// devices, actions, barks and other passive text-only overlays are OUT.
    /// Exit owners are the exits feature's territory and are never
    /// duplicated here (even if such an object also carries an in-scope
    /// interaction part).
    /// </summary>
    public static bool IsInteractableScope(bool opensDialog, bool isSkillCheck, bool isExitOwner)
        => (opensDialog || isSkillCheck) && !isExitOwner;

    /// <summary>
    /// Dialog-object state. Disabled / not-in-game yields None (absence
    /// convention, same as the NPC markers: no "gone forever" markers - the
    /// object simply does not render).
    /// </summary>
    public static InteractableMarkerState ClassifyDialogObject(bool dialogSeen, bool enabled, bool ownerInGame)
        => enabled && ownerInGame
            ? (dialogSeen ? InteractableMarkerState.Repeat : InteractableMarkerState.New)
            : InteractableMarkerState.None;

    /// <summary>
    /// Skill-check state per the report's cheapest-honest-set: unpassed and
    /// still attemptable = Check (shown - this is what the user hunts for);
    /// passed, locked out by a failed one-shot (AlreadyUsed &amp;&amp;
    /// OnlyCheckOnce), disabled or gone = None (absence: nothing to come
    /// back for). A failed but retryable check stays shown.
    /// </summary>
    public static InteractableMarkerState ClassifySkillCheckObject(bool checkPassed, bool alreadyUsed, bool onlyCheckOnce, bool enabled, bool ownerInGame)
        => enabled && ownerInGame && !checkPassed && !(alreadyUsed && onlyCheckOnce)
            ? InteractableMarkerState.Check
            : InteractableMarkerState.None;

    // Interest ranking for objects carrying several in-scope interactions:
    // fresh dialog beats an open check beats an already-seen dialog.
    public static int InterestRank(InteractableMarkerState state) => state switch {
        InteractableMarkerState.New => 3,
        InteractableMarkerState.Check => 2,
        InteractableMarkerState.Repeat => 1,
        _ => 0,
    };

    public static InteractableMarkerState Best(InteractableMarkerState a, InteractableMarkerState b)
        => InterestRank(b) > InterestRank(a) ? b : a;

    // ---- Volume guard ----

    /// <summary>
    /// Tunable per-area append bound for the interactable feature. Skill
    /// checks number ~1400 game-wide (per-area counts are far smaller), but
    /// the cap keeps a degenerate area from flooding the map; exceeding it
    /// logs once per area.
    /// </summary>
    public const int DefaultMaxAppendsPerArea = 80;

    /// <summary>Volume-cap predicate: appends stop once the cap is reached.</summary>
    public static bool WithinVolumeCap(int appendedCount, int cap = DefaultMaxAppendsPerArea) => appendedCount < cap;
}
