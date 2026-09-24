namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

public enum StarMapPlanetColorState {
    /// <summary>Leave/restore the vanilla color and name visibility.</summary>
    Vanilla,
    /// <summary>Unscanned; name revealed in white instead of the vanilla "?" sprite.</summary>
    Unscanned,
    /// <summary>Active quest or rumour objective targets this planet.</summary>
    Quest,
    /// <summary>Scanned; at least one visible unexplored point of interest.</summary>
    Poi,
    /// <summary>Scanned; player colony present.</summary>
    Colony,
    /// <summary>Scanned; extractor (miner) placed on at least one resource.</summary>
    Extractor,
    /// <summary>Scanned; at least one resource without an extractor.</summary>
    Resource,
    /// <summary>Scanned; fully explored and no visible signals - nothing to come back for.</summary>
    Done,
    /// <summary>Scanned; no visible signals but not fully explored (hidden remainder).</summary>
    Hidden,
}

/// <summary>
/// Pure, side-effect-free state/color rules for the star system map planet
/// name coloring (REPORT-starmap-planets matrix). No VM/View/Harmony/Unity
/// references so the offline test project can link this file against plain
/// bools; the feature file converts the RGBA tuples to UnityEngine.Color.
/// Sub-toggle fall-through: a state whose sub-toggle is off is SKIPPED and
/// the next lower-precedence state wins (e.g. quest coloring off + POI
/// present -> orange), and Vanilla never writes anything.
/// </summary>
public static class StarMapPlanetColorRules {
    // Palette (hex in comments; components are exact byte/255 fractions).
    // Chosen against the dark star map background - vanilla's prefab colors
    // occupy the greenish/greyish band, these hues stay distinguishable.
    public static (float R, float G, float B, float A) ColorOf(StarMapPlanetColorState state) => state switch {
        StarMapPlanetColorState.Unscanned => (1f, 1f, 1f, 1f),                    // #FFFFFF white
        StarMapPlanetColorState.Quest => (1f, 183f / 255f, 0f, 1f),               // #FFB700 amber
        StarMapPlanetColorState.Poi => (1f, 136f / 255f, 0f, 1f),                 // #FF8800 orange
        StarMapPlanetColorState.Colony => (68f / 255f, 204f / 255f, 68f / 255f, 1f),  // #44CC44 green
        StarMapPlanetColorState.Extractor => (0f, 170f / 255f, 170f / 255f, 1f),  // #00AAAA teal
        StarMapPlanetColorState.Resource => (0f, 204f / 255f, 1f, 1f),            // #00CCFF cyan
        StarMapPlanetColorState.Done => (176f / 255f, 176f / 255f, 176f / 255f, 1f), // #B0B0B0 light grey
        StarMapPlanetColorState.Hidden => (160f / 255f, 144f / 255f, 128f / 255f, 1f), // #A09080 grey-brown
        _ => (0f, 0f, 0f, 0f), // Vanilla: not a color; the feature restores the snapshot instead.
    };

    /// <summary>
    /// Precedence order (first match wins), from REPORT-starmap-planets:
    /// hidden -> vanilla skip; unscanned -> white (opt-in reveal);
    /// quest/rumour -> amber; unexplored POI -> orange; colony -> green;
    /// extractor -> teal; unmined resource -> cyan; scanned+fully explored ->
    /// light grey; scanned+not fully explored (no visible signals) ->
    /// grey-brown. All inputs are cheap persisted/refreshed bools already
    /// computed by OvertipEntityPlanetVM (quest/rumour/POI/colony/extractor/
    /// resource) plus PlanetIsVisible/PlanetIsScanned and the entity's
    /// IsFullyExplored.
    /// </summary>
    public static StarMapPlanetColorState Classify(
        bool isVisible,
        bool isScanned,
        bool hasQuestOrRumour,
        bool hasPoi,
        bool hasColony,
        bool hasExtractor,
        bool hasResource,
        bool isFullyExplored,
        bool showUnscannedNames = true,
        bool colorQuest = true,
        bool colorResources = true,
        bool colorDone = true) {
        if (!isVisible) {
            return StarMapPlanetColorState.Vanilla;
        }
        if (!isScanned) {
            // Vanilla shows the "?" sprite and no name text; the reveal is an
            // explicit opt-in because it spoils planet identities.
            return showUnscannedNames ? StarMapPlanetColorState.Unscanned : StarMapPlanetColorState.Vanilla;
        }
        if (colorQuest && hasQuestOrRumour) {
            return StarMapPlanetColorState.Quest;
        }
        if (hasPoi) {
            return StarMapPlanetColorState.Poi;
        }
        if (hasColony) {
            return StarMapPlanetColorState.Colony;
        }
        // Extractor and resource share the resources sub-toggle: both are
        // resource-economy signals on the same planet set.
        if (colorResources && hasExtractor) {
            return StarMapPlanetColorState.Extractor;
        }
        if (colorResources && hasResource) {
            return StarMapPlanetColorState.Resource;
        }
        if (colorDone) {
            return isFullyExplored ? StarMapPlanetColorState.Done : StarMapPlanetColorState.Hidden;
        }
        return StarMapPlanetColorState.Vanilla;
    }
}
