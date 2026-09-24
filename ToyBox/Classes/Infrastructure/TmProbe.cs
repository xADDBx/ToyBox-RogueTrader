using Kingmaker;

namespace ToyBox.Infrastructure;

/// <summary>
/// Minimal runtime probes ("ToyBox TM-probe:" lines in the game log) kept in
/// the v10 test build to confirm the five runtime fixes against real game
/// data. Settings-hidden on purpose: the switch is this const, default OFF
/// for release builds (probe lines carry unit names and invite noise
/// reports); flip it to true in a test build without touching call sites.
/// Every helper is one-shot/once-per-area and swallows its own exceptions -
/// a probe must never disturb the feature it observes. Area identity is
/// checked lazily against the currently loaded blueprint area, so the probes
/// add no EventBus subscription of their own.
/// </summary>
internal static class TmProbe {
    /// <summary>
    /// enabled for the on-machine diagnostic build that hunts the
    /// stuck level-up gate (blank "null selection" slots). Flip back to false
    /// for release builds.
    /// </summary>
    internal const bool Enabled = false;

    private static object? s_AreaKey;
    private static readonly HashSet<string> s_FiredThisArea = [];
    private static readonly Dictionary<string, int> s_FiresThisArea = [];

    private static bool AreaRolledOver() {
        var area = (object?)Game.Instance?.CurrentlyLoadedArea;
        if (ReferenceEquals(area, s_AreaKey)) {
            return false;
        }
        s_AreaKey = area;
        s_FiredThisArea.Clear();
        s_FiresThisArea.Clear();
        return true;
    }

    /// <summary>True exactly once per area for this key (false when probes are off).</summary>
#pragma warning disable CS0162 // Enabled is a const that is OFF by default in release builds; the on-branches stay compiled for test builds
    public static bool OncePerArea(string key) {
        if (!Enabled) {
            return false;
        }
        try {
            _ = AreaRolledOver();
            return s_FiredThisArea.Add(key);
        } catch {
            return false;
        }
    }

    /// <summary>True while this key has fired fewer than limit times this area.</summary>
    public static bool UnderAreaCap(string key, int limit) {
        if (!Enabled) {
            return false;
        }
        try {
            _ = AreaRolledOver();
            if (!s_FiresThisArea.TryGetValue(key, out int count)) {
                count = 0;
            }
            if (count >= limit) {
                return false;
            }
            s_FiresThisArea[key] = count + 1;
            return true;
        } catch {
            return false;
        }
    }

    public static void Log(string message) {
        if (!Enabled) {
            return;
        }
        try {
            Logging.Log($"ToyBox TM-probe: {message}");
        } catch {
            // Probes must never throw into game code.
        }
    }
#pragma warning restore CS0162
}
