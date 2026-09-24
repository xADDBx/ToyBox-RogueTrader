using Kingmaker;
using Kingmaker.Code.UI.MVVM.Utils;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.Models;
using Kingmaker.Visual.LocalMap;
using Owlcat.Runtime.UniRx;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Runtime host for the minimap overlay: owns the DDOL overlay canvas, the
/// vanilla map material harvest, the per-area bake (+ shader globals), the
/// per-frame driver and the auto-hide bookkeeping. The feature class only
/// owns settings UI and the toggle.
///
/// Layout (layout v1, rotation redirect):
/// ToyBoxMapOverlay (Canvas, ScreenSpaceOverlay, sortingOrder 30000, DDOL, NO raycaster)
///  +- MapRoot (CanvasGroup: opacity setting)
///     +- MapBox (corner rect; RectMask2D = crop window)
///        +- MapSpin (rotation.z = +yaw in HeadingUp; the authored area
///        |   rotation R in NorthUp - the same image angle the vanilla
///        |   fullscreen map uses)
///        |   +- MapImage (RawImage, harvested vanilla map material, player-centered)
///        |   +- NorthPivot -> NorthLabel ("N" pinned to the authored north
///        |       on the square crop rim, spins with the map)
///        +- PlayerArrow (procedural triangle; fixed in HeadingUp,
///            R - yaw in NorthUp)
///
/// Per-frame cost while shown: 1 rotation write + 1 anchoredPosition write
/// (the arrow rotation replaces the spin write in NorthUp mode). The bake
/// runs once per area (deferred ~0.75 s past OnAreaDidLoad so it lands after
/// the area-entry spike) plus a 2 s staleness poll that catches
/// renderer-side re-bakes (the RT object is REPLACED, not updated, on graph
/// dirty); between area changes the poll re-bakes at most once per
/// MinRebakeSeconds so vanilla's lazy navmesh updates are not converted
/// into eager mid-gameplay rebakes.
/// </summary>
internal static class LocalMapOverlayRuntime {
    private const int MaxFailures = 3;
    private const float PollSeconds = 2f;
    private const int SortingOrder = 30000; // under UMM's 32767 blocker, above game HUD

    // Push the first post-load bake past the entry spike (streaming/spawn):
    // a synchronous bake costs 10-35 ms on big areas (R5 P2-2a).
    private const float BakeDeferSeconds = 0.75f;
    // Minimum interval between non-area-change re-bakes: the vanilla map
    // only pays graph-update bakes lazily on open; without this the 2 s poll
    // would pay them eagerly mid-gameplay (R5 P2-2b).
    private const float MinRebakeSeconds = 30f;
    // If the vanilla map view is still missing after this many area loads,
    // the harvest goes dormant instead of scanning forever (R5 P3).
    private const int MaxHarvestMissAreas = 10;

    private const string ColorTexGlobal = "_LocalMapColorTex";
    private const string FowScaleOffsetGlobal = "LocalMapFowScaleOffset";

    // Distinctive accent, matches the enhanced-map toggle button tint.
    private static readonly Color ArrowTint = new(1f, 0.85f, 0.25f, 1f);

    private sealed class OverlayHost : IAreaHandler, IFullScreenUIHandler {
        public void OnAreaBeginUnloading() {
            s_OpenFullScreenUIs.Clear();
        }

        public void OnAreaDidLoad() {
            // Rearm first (the UniRx subscription binds the dispatcher
            // instance alive at subscribe time and dies silently on scene
            // change - the v9 classifier lesson), then schedule a fresh poll
            // so the new area bakes shortly after the next tick (camera rig
            // is ready by then; an in-handler Draw could NRE mid-transition).
            s_OpenFullScreenUIs.Clear();
            s_BakeWarned = false;
            s_HarvestWarned = false;
            s_AreaNeedsBake = true;
            s_NextPollTime = Time.unscaledTime + BakeDeferSeconds;
            // The canvas is DDOL: markers from the unloaded area survive the
            // scene change. Re-arm the marker refresh so the first shown tick
            // prunes them instead of re-projecting their foreign-area world
            // positions onto the new map for up to PollSeconds.
            s_NextMarkerRefresh = 0f;
            if (s_MapMaterial == null) {
                // Harvest-miss budget: after enough area loads without ever
                // finding the view, stop paying the scan; a user toggle
                // re-arms it (Enable resets the dormancy).
                s_HarvestMissAreas++;
                if (!s_HarvestDormant && s_HarvestMissAreas >= MaxHarvestMissAreas) {
                    s_HarvestDormant = true;
                    Warn($"ToyBox EnhancedMap: vanilla map view never found after {MaxHarvestMissAreas} area loads - material harvest dormant (toggle the feature off/on to retry).");
                }
            } else {
                s_HarvestMissAreas = 0;
            }
            RearmDriver();
        }

        public void HandleFullScreenUiChanged(bool state, FullScreenUIType fullScreenUIType) {
            if (state) {
                _ = s_OpenFullScreenUIs.Add(fullScreenUIType);
            } else {
                _ = s_OpenFullScreenUIs.Remove(fullScreenUIType);
            }
        }
    }

    private static readonly OverlayHost s_Host = new();
    private static bool s_Subscribed;
    private static bool s_Enabled;

    private static bool s_DriverRunning;
    private static IDisposable? s_DriverSubscription;
    private static int s_Failures;
    private static bool s_BakeWarned;
    private static bool s_HarvestWarned;
    private static int s_HarvestMissAreas;
    private static bool s_HarvestDormant;
    private static bool s_AreaNeedsBake;
    private static float s_LastBakeTime;
    private static Vector4 s_LastFow;

    // Cached delegate: the pre-boot subscription retry re-schedules itself
    // once per UMM update and must not allocate per frame.
    private static readonly Action s_SubscribeRetry = SubscribeRetry;

    private static readonly HashSet<FullScreenUIType> s_OpenFullScreenUIs = new();

    private static GameObject? s_Root;
    private static Canvas? s_Canvas;
    private static CanvasGroup? s_RootGroup;
    private static RectTransform? s_Box;
    private static RectTransform? s_Spin;
    private static RawImage? s_MapImage;
    private static RectTransform? s_ImageRect;
    private static RectTransform? s_NorthPivot;
    private static readonly List<Component> s_NorthMarks = new();
    private static RectTransform? s_ArrowRect;

    private static Material? s_MapMaterial;
    private static RenderTexture? s_BoundRT;
    private static System.Numerics.Vector2 s_RtSize;
    private static float s_NextPollTime;
    private static float s_ManualRotation;
    private static float s_NextHarvestTry;
    private static Sprite? s_ArrowSprite;
    private static int s_LastScreenWidth;
    private static int s_LastScreenHeight;
    // Minimap NPC dot markers: children of the map image (rotate with it),
    // positioned at (npcUV - 0.5) * imageSize. Refreshed on the poll cadence.
    private static readonly List<MinimapMarker> s_Markers = new();
    private static float s_NextMarkerRefresh;

    private sealed class MinimapMarker {
        public RectTransform Rect = null!;
        public BaseUnitEntity Unit = null!;
        public Image Dot = null!;
    }

    internal static void Enable() {
        s_Enabled = true;
        // Re-enable resets the strike counter: only consecutive failures may
        // auto-disable.
        s_Failures = 0;
        s_BakeWarned = false;
        s_HarvestWarned = false;
        s_HarvestMissAreas = 0;
        // Toggle recovery also re-arms a harvest that went dormant.
        s_HarvestDormant = false;
        s_NextHarvestTry = 0f;
        if (!UnityEngine.Object.CurrentThreadIsMainThread()) {
            // Boot path (persisted ON + lazy init): ToggledFeature.Initialize
            // runs this on a background thread while every Unity API below
            // is main-thread-only; an exception there would unload the
            // feature and silently revert the user's setting (R2 P1-1).
            // Schedule the whole body (HighlightHiddenObjectsFeature
            // pattern) - the guards inside make double-scheduling safe.
            Main.ScheduleForMainThread(EnableOnMainThread);
            return;
        }
        EnableOnMainThread();
    }

    private static void EnableOnMainThread() {
        // A half-built canvas from a failed EnsureCanvas would satisfy the
        // s_Root != null early-out forever; tear it down so the next enable
        // retries from scratch, and let the failure latch count the strike
        // instead of escaping into the settings UI's toggle callback.
        try {
            EnsureSubscribed();
            if (!s_Subscribed) {
                // Pre-boot: Kingmaker's Game does not exist yet, so the EventBus
                // subscription cannot land here. Re-check once per UMM update
                // until it can; without it the driver's UniRx subscription dies
                // on the first scene change with no OnAreaDidLoad ever firing to
                // rearm it (R2 P1-2).
                Main.ScheduleForMainThread(s_SubscribeRetry);
            }
            EnsureCanvas();
            EnsureMaterial();
            ApplySettings();
            s_NextPollTime = 0f;
            EnsureDriverRunning();
        } catch (Exception ex) {
            DestroyCanvas();
            HandleFailure(ex);
        }
    }

    private static void SubscribeRetry() {
        EnsureSubscribed();
        if (s_Enabled && !s_Subscribed) {
            Main.ScheduleForMainThread(s_SubscribeRetry);
        }
    }

    internal static void Disable() {
        s_Enabled = false;
        StopDriver();
        try {
            EventBus.Unsubscribe(s_Host);
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: minimap EventBus unsubscribe failed: {ex.Message}");
        }
        if (UnityEngine.Object.CurrentThreadIsMainThread()) {
            DestroyCanvas();
        } else {
            // Same boot-thread hazard class as Enable: a feature unload
            // during initialization reaches Disable off the main thread.
            Main.ScheduleForMainThread(DestroyCanvas);
        }
    }

    /// <summary>Live settings application (called by the feature's OnGui rows).</summary>
    internal static void ApplySettings() {
        if (!s_Enabled) {
            return;
        }
        EnsureCanvas();
        EnsureMaterial();
        ApplyLayout();
    }

    private static void EnsureSubscribed() {
        if (s_Subscribed || Game.Instance == null) {
            return;
        }
        s_Subscribed = true;
        _ = EventBus.Subscribe(s_Host);
    }

    private static void EnsureDriverRunning() {
        if (s_DriverRunning || !s_Enabled) {
            return;
        }
        s_DriverRunning = true;
        s_DriverSubscription = MainThreadDispatcher.UpdateAsObservable().Subscribe((UniRx.Unit _) => Tick());
    }

    private static void StopDriver() {
        try {
            s_DriverSubscription?.Dispose();
        } catch {
            // A dispatcher destroyed by a scene change may throw on dispose;
            // the subscription is being dropped either way.
        }
        s_DriverSubscription = null;
        s_DriverRunning = false;
    }

    /// <summary>
    /// Dispose-and-resubscribe the per-frame driver (copy of the classifier
    /// RearmPump pattern): the subscription binds the dispatcher alive at
    /// subscribe time; if that dispatcher dies on a scene change the
    /// subscription dies silently while s_DriverRunning stays latched.
    /// </summary>
    private static void RearmDriver() {
        StopDriver();
        EnsureDriverRunning();
    }

    private static void Tick() {
        try {
            if (!s_Enabled || s_Canvas == null) {
                return;
            }
            var game = Game.Instance;
            var player = game?.Player?.MainCharacterEntity;
            var renderer = WarhammerLocalMapRenderer.Instance;
            var areaLoaded = game != null && game.CurrentlyLoadedAreaPart != null;
            // Game modes without a walkable local map (space combat, star
            // system map, global map) have no navmesh for the renderer to
            // draw - hide instead of showing the previous area's stale RT.
            var mode = game?.CurrentMode;
            var inLocalMode = mode == null
                || (mode != GameModeType.SpaceCombat
                    && mode != GameModeType.StarSystem
                    && mode != GameModeType.GlobalMap
                    && mode != GameModeType.CutsceneGlobalMap);
            var shouldShow = player != null && renderer != null && areaLoaded && inLocalMode
                && s_OpenFullScreenUIs.Count == 0
                && !CutsceneUIState.IsCutsceneActive.Value;
            if (s_Canvas.enabled != shouldShow) {
                s_Canvas.enabled = shouldShow;
            }
            if (!shouldShow || s_MapImage == null || s_ImageRect == null || s_Spin == null || s_ArrowRect == null) {
                // Nothing wrong - just idle; do not accumulate strikes.
                return;
            }
            if (Time.unscaledTime >= s_NextPollTime || Screen.width != s_LastScreenWidth || Screen.height != s_LastScreenHeight) {
                s_NextPollTime = Time.unscaledTime + PollSeconds;
                // Boot self-heal: a pre-boot Enable can leave the EventBus
                // subscription or the material harvest un-armed; both are
                // allocation-free no-ops once healthy, so re-invoke them on
                // the poll cadence (R2 P1-2 / R1 P1).
                EnsureSubscribed();
                EnsureMaterial();
                // Vanilla pays navmesh-graph bakes lazily (on map open); an
                // unconditional Draw() converts every graph update into an
                // eager 10-35 ms re-bake mid-gameplay (R5 P2-2b). Bake only
                // when a new area is pending, nothing is bound yet, or the
                // minimum re-bake interval has elapsed.
                if (s_AreaNeedsBake || s_BoundRT == null || Time.unscaledTime - s_LastBakeTime >= MinRebakeSeconds) {
                    if (TryBake()) {
                        s_LastBakeTime = Time.unscaledTime;
                        s_AreaNeedsBake = false;
                    }
                } else {
                    // Between bakes only re-pin the shader globals (a
                    // vanilla map session re-sets them while it is open).
                    Shader.SetGlobalTexture(ColorTexGlobal, s_BoundRT);
                    Shader.SetGlobalVector(FowScaleOffsetGlobal, s_LastFow);
                }
                if (Screen.width != s_LastScreenWidth || Screen.height != s_LastScreenHeight) {
                    ApplyLayout();
                }
            }
            var rotationMode = Settings.MinimapRotation;
            // HeadingUp follows the CAMERA yaw (Q/E rotation) — what the
            // player sees on screen — not the character's facing, which
            // often points elsewhere. NorthUp keeps the character yaw for
            // the arrow (the map stays fixed there).
            var yaw = rotationMode == MinimapRotation.HeadingUp && Camera.main != null
                ? Camera.main.transform.eulerAngles.y
                : player!.Orientation;
            var authored = AuthoredRotation();
            // Manual rotation (Q/E while the minimap is visible): offsets the
            // HeadingUp base in NorthUp mode; resets to 0 in HeadingUp (the
            // heading drives it). Smoothed 90°/s while the key is held.
            if (Input.GetKey(KeyCode.Q)) {
                s_ManualRotation += 90f * Time.unscaledDeltaTime;
            }
            if (Input.GetKey(KeyCode.E)) {
                s_ManualRotation -= 90f * Time.unscaledDeltaTime;
            }
            if (rotationMode == MinimapRotation.HeadingUp) {
                s_ManualRotation = 0f;
                s_Spin.localEulerAngles = new Vector3(0f, 0f, LocalMapOverlayMath.MapSpinAngle(rotationMode, yaw, authored));
            } else {
                s_ArrowRect.localEulerAngles = new Vector3(0f, 0f, LocalMapOverlayMath.ArrowAngle(rotationMode, yaw, authored));
                s_Spin.localEulerAngles = new Vector3(0f, 0f, LocalMapOverlayMath.MapSpinAngle(rotationMode, yaw, authored) + s_ManualRotation);
            }
            // ... and exactly one position write: the player UV lands on the
            // spin origin (= crop-window center), in the spin frame so the
            // rotation above can never move it off-center.
            // shouldShow above already gates on renderer/player, but keep the
            // null checks local so the compiler can prove them.
            if (renderer == null || player == null) {
                return;
            }
            var viewport = renderer.WorldToViewportPoint(player.Position);
            var imagePosition = LocalMapOverlayMath.ComputeMapImagePosition(
                new System.Numerics.Vector2(viewport.x, viewport.y),
                new System.Numerics.Vector2(s_ImageRect.sizeDelta.x, s_ImageRect.sizeDelta.y));
            s_ImageRect.anchoredPosition = new Vector2(imagePosition.X, imagePosition.Y);
            RefreshMinimapMarkers();
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    /// <summary>
    /// Bake the local map on demand (the game's own QA arbiter does exactly
    /// this outside the map window) and publish the two shader globals the
    /// harvested material samples. Clean Draws cost 4 raycasts; the poll
    /// cadence keeps that off the per-frame path.
    /// </summary>
    private static bool TryBake() {
        try {
            var renderer = WarhammerLocalMapRenderer.Instance;
            if (renderer == null || Game.Instance?.CurrentlyLoadedAreaPart == null) {
                return false;
            }
            var draw = renderer.Draw();
            if (draw.ColorRT == null) {
                return false;
            }
            Shader.SetGlobalTexture(ColorTexGlobal, draw.ColorRT);
            Shader.SetGlobalVector(FowScaleOffsetGlobal, draw.LocalMapFowScaleOffset);
            s_BoundRT = draw.ColorRT;
            s_LastFow = draw.LocalMapFowScaleOffset;
            s_RtSize = new System.Numerics.Vector2(draw.ColorRT.width, draw.ColorRT.height);
            // The renderer REPLACES the RT object on re-bakes (area change,
            // graph dirty): re-apply the image size, which depends on it.
            ApplyLayout();
            return true;
        } catch (Exception ex) {
            // Transient by nature (camera rig mid-transition etc.); the 2 s
            // poll retries forever, so this must not strike the feature out.
            if (!s_BakeWarned) {
                s_BakeWarned = true;
                Warn($"ToyBox EnhancedMap: minimap overlay bake failed (will retry): {ex.Message}");
            }
            return false;
        }
    }

    /// <summary>
    /// Harvest the vanilla fullscreen map's material (an asset - survives
    /// scene changes). The map view object exists-but-inactive from UI boot,
    /// so FindObjectsOfTypeAll (which finds inactive) can locate it without
    /// ever opening the map. The material samples the _LocalMapColorTex /
    /// _FogOfWarMask shader globals, giving us the vanilla look - fog
    /// included - without knowing the shader's internals.
    /// </summary>
    private static void EnsureMaterial() {
        if (s_MapMaterial == null) {
            if (s_HarvestDormant) {
                // The view type was never found after the area budget ran
                // out; stop paying the scan (a user toggle re-arms it).
                if (s_MapImage != null) {
                    s_MapImage.enabled = false;
                }
                return;
            }
            // FindObjectsOfTypeAll is not cheap: retry a failed harvest on
            // the poll cadence, not on every settings-window frame.
            if (Time.unscaledTime < s_NextHarvestTry) {
                if (s_MapImage != null) {
                    s_MapImage.enabled = false;
                }
                return;
            }
            s_NextHarvestTry = Time.unscaledTime + PollSeconds;
            foreach (var view in Resources.FindObjectsOfTypeAll<LocalMapBaseView>()) {
                var image = view != null ? view.m_Image : null;
                if (image != null && image.material != null) {
                    // Graphic.material returns the assigned asset here - the
                    // vanilla map never mutates it, so no instance is created.
                    s_MapMaterial = image.material;
                    break;
                }
            }
            if (s_MapMaterial == null && !s_HarvestWarned) {
                // Once per area (reset in OnAreaDidLoad): retrying is
                // harmless, but the user must not stare at a silent blank
                // box (R2 P2-1).
                s_HarvestWarned = true;
                Warn("ToyBox EnhancedMap: vanilla map material not found - minimap shows an empty frame (will keep retrying).");
            }
        }
        if (s_MapImage != null) {
            s_MapImage.material = s_MapMaterial;
            s_MapImage.enabled = s_MapMaterial != null && s_BoundRT != null;
        }
    }

    private static void EnsureCanvas() {
        if (s_Root != null) {
            return;
        }
        // 1:1 copy of UMM's own BlockGameUI canvas minus the raycaster: a
        // canvas without a GraphicRaycaster is invisible to the EventSystem,
        // so clicks and drags pass through to the game untouched.
        s_Root = new GameObject("ToyBoxMapOverlay", typeof(Canvas));
        s_Canvas = s_Root.GetComponent<Canvas>();
        s_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        s_Canvas.sortingOrder = SortingOrder;
        UnityEngine.Object.DontDestroyOnLoad(s_Root);

        // CanvasGroup does not require a RectTransform (unlike Canvas or any
        // Graphic) - the component must be requested explicitly or
        // GetComponent<RectTransform>() below returns null.
        var rootGroupGo = new GameObject("MapRoot", typeof(RectTransform), typeof(CanvasGroup));
        s_RootGroup = rootGroupGo.GetComponent<CanvasGroup>();
        SetFullStretch(rootGroupGo.GetComponent<RectTransform>(), s_Root.GetComponent<RectTransform>());

        var boxGo = new GameObject("MapBox", typeof(RectTransform), typeof(RectMask2D));
        s_Box = (RectTransform)boxGo.transform;
        s_Box.SetParent(rootGroupGo.transform, false);
        s_Box.pivot = new Vector2(0.5f, 0.5f);
        s_Box.anchorMin = new Vector2(0.5f, 0.5f);
        s_Box.anchorMax = new Vector2(0.5f, 0.5f);

        var spinGo = new GameObject("MapSpin", typeof(RectTransform));
        s_Spin = (RectTransform)spinGo.transform;
        s_Spin.SetParent(s_Box, false);
        SetCentered(s_Spin);

        var imageGo = new GameObject("MapImage", typeof(RawImage));
        s_MapImage = imageGo.GetComponent<RawImage>();
        s_MapImage.raycastTarget = false;
        s_ImageRect = (RectTransform)imageGo.transform;
        s_ImageRect.SetParent(s_Spin, false);
        SetCentered(s_ImageRect);

        var northGo = new GameObject("NorthPivot", typeof(RectTransform));
        s_NorthPivot = (RectTransform)northGo.transform;
        s_NorthPivot.SetParent(s_Spin, false);
        SetCentered(s_NorthPivot);
        CreateNorthMark();

        var arrowGo = new GameObject("PlayerArrow", typeof(Image));
        var arrow = arrowGo.GetComponent<Image>();
        arrow.raycastTarget = false;
        // Created once per session and cached: destroying the canvas kills
        // only the Image, so a per-toggle sprite would leak its texture.
        s_ArrowSprite ??= CreateArrowSprite();
        arrow.sprite = s_ArrowSprite;
        arrow.color = ArrowTint;
        s_ArrowRect = (RectTransform)arrowGo.transform;
        s_ArrowRect.SetParent(s_Box, false);
        SetCentered(s_ArrowRect);

        ApplyLayout();
    }

    private static void CreateNorthMark() {
        var font = LoadBuiltinFont();
        if (font != null) {
            var labelGo = new GameObject("NorthLabel", typeof(Text));
            var label = labelGo.GetComponent<Text>();
            label.font = font;
            label.text = "N";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(s_NorthPivot, false);
            SetCentered(labelRect);
            s_NorthMarks.Add(label);
            return;
        }
        // Font fallback (builtin resource name varies by Unity version): a
        // small white notch still marks north on the rim.
        var notchGo = new GameObject("NorthNotch", typeof(Image));
        var notch = notchGo.GetComponent<Image>();
        notch.raycastTarget = false;
        notch.color = Color.white;
        var notchRect = (RectTransform)notchGo.transform;
        notchRect.SetParent(s_NorthPivot, false);
        SetCentered(notchRect);
        s_NorthMarks.Add(notch);
    }

    private static Font? LoadBuiltinFont() {
        try {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        } catch {
            // Unity 2021 and older name the builtin font differently.
        }
        try {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        } catch {
            return null;
        }
    }

    private static Sprite? CreateArrowSprite() {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
            name = "ToyBoxMinimapArrow",
        };
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++) {
            // Apex at the top (v = 1), widening downward: an UP arrow.
            var t = 1f - y / (float)(size - 1);
            var half = (int)(size * 0.45f * t);
            for (var x = 0; x < size; x++) {
                var on = x >= size / 2 - half && x <= size / 2 + half;
                pixels[y * size + x] = on ? new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue) : new Color32(0, 0, 0, 0);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void ApplyLayout() {
        if (s_Canvas == null || s_Box == null || s_Spin == null || s_MapImage == null || s_ImageRect == null || s_ArrowRect == null || s_NorthPivot == null) {
            return;
        }
        s_LastScreenWidth = Screen.width;
        s_LastScreenHeight = Screen.height;
        var screen = new System.Numerics.Vector2(Screen.width, Screen.height);
        var box = LocalMapOverlayMath.ComputeBoxSize(screen);
        var margin = LocalMapOverlayMath.ComputeMargin(screen);
        var boxPosition = LocalMapOverlayMath.ComputeCornerPosition(Settings.OverlayCorner, screen, box, margin);

        s_Box.sizeDelta = new Vector2(box.X, box.Y);
        s_Box.anchoredPosition = new Vector2(boxPosition.X, boxPosition.Y);

        // NorthUp locks the map at the per-area authored rotation R (the
        // vanilla fullscreen map rotates its image by R too); HeadingUp
        // re-pins the arrow upright (whichever is unused is re-driven every
        // frame).
        var mode = Settings.MinimapRotation;
        if (mode == MinimapRotation.NorthUp) {
            s_Spin.localEulerAngles = new Vector3(0f, 0f, LocalMapOverlayMath.MapSpinAngle(mode, 0f, AuthoredRotation()));
        } else {
            s_ArrowRect.localEulerAngles = Vector3.zero;
        }

        var arrowEdge = box.X * 0.07f;
        s_ArrowRect.sizeDelta = new Vector2(arrowEdge < 10f ? 10f : arrowEdge, arrowEdge < 10f ? 10f : arrowEdge);

        if (s_RtSize.X > 0f && s_RtSize.Y > 0f) {
            var imageSize = LocalMapOverlayMath.ComputeOverlayImageSize(box, s_RtSize, Settings.OverlayZoom);
            s_ImageRect.sizeDelta = new Vector2(imageSize.X, imageSize.Y);
            s_MapImage.enabled = s_MapMaterial != null && s_BoundRT != null;
        } else {
            s_MapImage.enabled = false;
        }

        // The mark orbits with the spin frame; pin it at compass angle R
        // (the authored north) clamped to the square rim, not a circle, so
        // it always touches the rim.
        var northPosition = LocalMapOverlayMath.ComputeNorthMarkPosition(AuthoredRotation(), box, box.X * 0.09f);
        foreach (var mark in s_NorthMarks) {
            if (mark is Text label) {
                label.fontSize = (int)(box.X * 0.07f);
                var fontRect = (RectTransform)label.transform;
                fontRect.sizeDelta = new Vector2(box.X * 0.14f, box.X * 0.14f);
                fontRect.anchoredPosition = new Vector2(northPosition.X, northPosition.Y);
            } else if (mark != null) {
                var notchRect = (RectTransform)mark.transform;
                notchRect.sizeDelta = new Vector2(3f, box.X * 0.1f);
                notchRect.anchoredPosition = new Vector2(northPosition.X, northPosition.Y);
            }
        }

        if (s_RootGroup != null) {
            s_RootGroup.alpha = LocalMapOverlayMath.ClampOpacity(Settings.OverlayOpacity);
        }
    }

    private static void DestroyCanvas() {
        s_NorthMarks.Clear();
        // The dot GameObjects die with the canvas; the entries (units alive,
        // rects destroyed) would otherwise occupy the 40-dot cap and block
        // re-adding their units after the next Enable.
        s_Markers.Clear();
        if (s_Root != null) {
            UnityEngine.Object.Destroy(s_Root);
        }
        s_Root = null;
        s_Canvas = null;
        s_RootGroup = null;
        s_Box = null;
        s_Spin = null;
        s_MapImage = null;
        s_ImageRect = null;
        s_NorthPivot = null;
        s_ArrowRect = null;
        s_BoundRT = null;
        s_RtSize = default;
        // s_MapMaterial survives: it is a shared asset, not part of the canvas.
    }

    private static void SetFullStretch(RectTransform rect, RectTransform parent) {
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetCentered(RectTransform rect) {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Per-area authored map rotation R (0/90/180/270; 0 on most areas and
    /// at the main menu). The vanilla fullscreen map rotates its image by
    /// this amount; the overlay composes the same R into the NorthUp spin
    /// and the north-mark angle.
    /// </summary>
    private static float AuthoredRotation() {
        var part = Game.Instance?.CurrentlyLoadedAreaPart;
        return part == null ? 0f : (float)part.LocalMapRotationDeg;
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxFailures) {
            Warn($"ToyBox EnhancedMap: minimap overlay driver failed ({s_Failures}/{MaxFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling LocalMapOverlayFeature after {MaxFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<LocalMapOverlayFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }

    /// <summary>
    /// Refreshes NPC dot markers on the minimap: creates/destroys on the poll
    /// cadence (area change + 2s staleness), positions every frame (cheap
    /// anchoredPosition writes). Markers are children of the map image so
    /// they rotate with it. Uses the existing NpcDialogClassifier results.
    /// </summary>
    private static void RefreshMinimapMarkers() {
        if (s_ImageRect == null) {
            return;
        }
        // Rebuild marker list on the poll cadence (new/dead/classified units).
        if (Time.unscaledTime >= s_NextMarkerRefresh) {
            s_NextMarkerRefresh = Time.unscaledTime + PollSeconds;
            var game = Kingmaker.Game.Instance;
            var units = game?.State?.AllBaseUnits?.All;
            if (units == null) {
                return;
            }
            // Prune dead markers and area-change residue: AllBaseUnits still
            // enumerates units from unloaded areas, and the DDOL canvas keeps
            // their dots alive across the scene change (same IsInGame /
            // IsInCurrentArea filter as LocalMapNpcMarkersFeature).
            for (var i = s_Markers.Count - 1; i >= 0; i--) {
                var unit = s_Markers[i].Unit;
                if (unit == null || unit.LifeState.IsDead || !unit.IsInGame
                    || !LocalMapModel.IsInCurrentArea(unit.Position)) {
                    if (s_Markers[i].Rect != null) {
                        UnityEngine.Object.Destroy(s_Markers[i].Rect.gameObject);
                    }
                    s_Markers.RemoveAt(i);
                }
            }
            // Add new markers for classified NPCs (limit: 40 dots).
            foreach (var unit in units) {
                if (unit == null || unit.LifeState.IsDead || !unit.IsInGame
                    || !LocalMapModel.IsInCurrentArea(unit.Position)
                    || s_Markers.Count >= 40) {
                    continue;
                }
                if (s_Markers.Exists(m => m.Unit == unit)) {
                    continue;
                }
                var state = NpcDialogClassifierRuntime.GetFreshState(unit);
                if (state is not { } cls || cls == NpcDialogMarkerState.None) {
                    continue;
                }
                var dotGo = new GameObject("NpcDot", typeof(Image));
                var dot = dotGo.GetComponent<Image>();
                dot.raycastTarget = false;
                dot.color = cls switch {
                    NpcDialogMarkerState.New => new Color(1f, 0.85f, 0f, 0.9f),
                    NpcDialogMarkerState.Repeat => new Color(0.3f, 0.6f, 1f, 0.8f),
                    _ => new Color(0.7f, 0.7f, 0.7f, 0.6f),
                };
                var rect = (RectTransform)dotGo.transform;
                rect.SetParent(s_ImageRect, false);
                rect.sizeDelta = new Vector2(2.5f, 2.5f);
                s_Markers.Add(new MinimapMarker { Rect = rect, Unit = unit, Dot = dot });
            }
        }
        // Update positions every frame: NPC UV relative to the image center.
        var renderer = WarhammerLocalMapRenderer.Instance;
        if (renderer == null) {
            return;
        }
        var imgSize = s_ImageRect.sizeDelta;
        foreach (var marker in s_Markers) {
            if (marker.Unit == null || marker.Rect == null) {
                continue;
            }
            var uv = renderer.WorldToViewportPoint(marker.Unit.Position);
            marker.Rect.anchoredPosition = new Vector2(
                (uv.x - 0.5f) * imgSize.x,
                (uv.y - 0.5f) * imgSize.y);
        }
    }
}
