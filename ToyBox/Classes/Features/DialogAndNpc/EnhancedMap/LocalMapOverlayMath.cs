using System.Numerics;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>Rotation mode of the minimap overlay .</summary>
public enum MinimapRotation {
    /// <summary>Map spins so the player's heading points up; the arrow stays fixed (D4-style). Default.</summary>
    HeadingUp,
    /// <summary>Map stays north-up; the arrow rotates with the heading.</summary>
    NorthUp,
}

/// <summary>Screen corner the minimap overlay is pinned to.</summary>
public enum MapOverlayCorner {
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
}

/// <summary>
/// Pure layout/angle math for the minimap overlay. Uses System.Numerics so
/// the offline harness can link it without Unity (same convention as
/// MarkerLayout); the runtime converts at the RectTransform boundary.
/// Deliberately avoids MathF/Math.Clamp so the same source compiles on the
/// game's net481 reference set.
///
/// Coordinate model: the crop box is square with center pivot; the map
/// image is cover-scaled so it is at least box*zoom in both axes (zoom Z
/// means the box shows 1/Z of the map's linear extent). MapImage position
/// is expressed in the SPIN frame (the rotating parent): the player UV maps
/// to the spin origin, which sits on the box center, so rotation never
/// moves the player off-center.
/// </summary>
public static class LocalMapOverlayMath {
    public const float MinOpacity = 0.2f;
    public const float MaxOpacity = 1f;
    public const float DefaultOpacity = 0.85f;
    public const float MinZoom = 0.25f;
    public const float MaxZoom = 4f;
    public const float DefaultZoom = 0.5f;

    // Box = fraction of the smaller screen axis, kept inside a sane pixel
    // band; margin = gap between box and screen edge.
    public const float BoxFraction = 0.23f;
    public const float MarginFraction = 0.02f;
    public const float MinBoxPixels = 120f;
    public const float MaxBoxPixels = 480f;

    /// <summary>Defensive clamp for opacity values arriving from edited settings files.</summary>
    public static float ClampOpacity(float value) => value < MinOpacity ? MinOpacity : value > MaxOpacity ? MaxOpacity : value;

    /// <summary>Defensive clamp for zoom values arriving from edited settings files.</summary>
    public static float ClampZoom(float value) => value < MinZoom ? MinZoom : value > MaxZoom ? MaxZoom : value;

    /// <summary>Square crop-box size for a screen: fraction of the smaller axis, clamped to a pixel band.</summary>
    public static Vector2 ComputeBoxSize(Vector2 screenSize, float fraction = BoxFraction) {
        var smaller = screenSize.X < screenSize.Y ? screenSize.X : screenSize.Y;
        var px = smaller * fraction;
        if (px < MinBoxPixels) {
            px = MinBoxPixels;
        }
        if (px > MaxBoxPixels) {
            px = MaxBoxPixels;
        }
        return new Vector2(px, px);
    }

    /// <summary>Margin between box and screen edges for a screen.</summary>
    public static float ComputeMargin(Vector2 screenSize) {
        var smaller = screenSize.X < screenSize.Y ? screenSize.X : screenSize.Y;
        var margin = smaller * MarginFraction;
        return margin < 8f ? 8f : margin;
    }

    /// <summary>
    /// Map-image size for a crop box: cover-scale the render texture so the
    /// image is at least box*zoom in BOTH axes (no letterbox inside the
    /// box), preserving the RT aspect ratio.
    /// </summary>
    public static Vector2 ComputeOverlayImageSize(Vector2 boxSize, Vector2 rtSize, float zoom) {
        var z = ClampZoom(zoom);
        var rtX = rtSize.X < 1f ? 1f : rtSize.X;
        var rtY = rtSize.Y < 1f ? 1f : rtSize.Y;
        var sx = boxSize.X * z / rtX;
        var sy = boxSize.Y * z / rtY;
        var s = sx > sy ? sx : sy;
        return new Vector2(rtX * s, rtY * s);
    }

    /// <summary>
    /// Map-image anchoredPosition inside the spin frame: puts the player UV
    /// at the spin origin (= box center). Player UV may lie outside [0,1]
    /// (player beyond LocalMapBounds) - the formula stays exact, the box
    /// simply shows the map edge.
    /// </summary>
    public static Vector2 ComputeMapImagePosition(Vector2 playerUV, Vector2 imageSize) => new((0.5f - playerUV.X) * imageSize.X, (0.5f - playerUV.Y) * imageSize.Y);

    /// <summary>
    /// Crop-box anchoredPosition within a center-anchored canvas so the box
    /// sits in the chosen corner with the given margin.
    /// </summary>
    public static Vector2 ComputeCornerPosition(MapOverlayCorner corner, Vector2 canvasSize, Vector2 boxSize, float margin) {
        var half = canvasSize / 2f - new Vector2(margin, margin) - boxSize / 2f;
        return corner switch {
            MapOverlayCorner.TopRight => new Vector2(half.X, half.Y),
            MapOverlayCorner.TopLeft => new Vector2(-half.X, half.Y),
            MapOverlayCorner.BottomRight => new Vector2(half.X, -half.Y),
            MapOverlayCorner.BottomLeft => new Vector2(-half.X, -half.Y),
            _ => half,
        };
    }

    /// <summary>
    /// Z rotation (degrees) of the map spin frame. The RT is world-aligned
    /// (+X is image-right, +Z image-up) and Unity z-rotation is CCW-positive,
    /// so bringing a heading displayed at CW-from-up = yaw back to up needs
    /// +yaw (the same convention the visually verified v9-v11 fullscreen
    /// rotation used). NorthUp instead applies the per-area authored
    /// LocalMapRotation R, matching the vanilla fullscreen presentation
    /// (SetMapRotation(R)); the heading term does not apply there.
    /// </summary>
    public static float MapSpinAngle(MinimapRotation mode, float yawDegrees, float areaRotationDeg = 0f) => mode == MinimapRotation.HeadingUp ? yawDegrees : areaRotationDeg;

    /// <summary>
    /// Z rotation (degrees) of the player arrow: fixed (0) in HeadingUp (the
    /// map rotates instead); in NorthUp it composes the authored rotation as
    /// R - yaw so it points along the heading on the rotated map (vanilla's
    /// fullscreen frame angle is the same R - camYaw composition).
    /// </summary>
    public static float ArrowAngle(MinimapRotation mode, float yawDegrees, float areaRotationDeg = 0f) => mode == MinimapRotation.HeadingUp ? 0f : areaRotationDeg - yawDegrees;

    /// <summary>
    /// Position of the north mark inside the (rotating) spin frame, in spin
    /// local space: the authored north sits at compass angle R on the
    /// world-aligned image, so pinning the mark there makes it land at
    /// screen-up in NorthUp (spin = R) and track the authored north in
    /// HeadingUp (R = 0 areas degenerate to the old top-of-rim pin). The
    /// orbit point is clamped to the SQUARE rim (max-norm normalization),
    /// not a circle, so the mark touches the border at every angle instead
    /// of floating inside it toward the diagonals.
    /// </summary>
    public static Vector2 ComputeNorthMarkPosition(float areaRotationDeg, Vector2 boxSize, float inset) {
        // Radian math in double so the cardinal angles (the enum is 0/90/
        // 180/270) stay visually exact after the float cast.
        var rad = areaRotationDeg * Math.PI / 180.0;
        var dir = new Vector2((float)Math.Sin(rad), (float)Math.Cos(rad));
        var half = boxSize.X * 0.5f;
        var dominant = Math.Abs(dir.X) > Math.Abs(dir.Y) ? Math.Abs(dir.X) : Math.Abs(dir.Y);
        if (dominant < 1e-4f) {
            return new Vector2(0f, half - inset);
        }
        return dir / dominant * (half - inset);
    }
}
