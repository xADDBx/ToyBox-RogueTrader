using Kingmaker;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Owlcat.Runtime.UniRx;
using UniRx;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// The classifier runtime host: owns the cache, the defer queue pump and the
/// EventBus subscriptions that invalidate cached classifications. The marker
/// and nameplate features never classify directly; they call GetFreshState
/// (cheap, may defer) or ClassifyNow (one-shot, budgeted).
/// </summary>
public sealed class NpcDialogClassifierRuntime : IDialogFinishHandler, IEtudesUpdateHandler, IAreaHandler {
    private static readonly NpcDialogClassifierRuntime s_Instance = new();
    private static bool m_IsSubscribed;
    private static bool m_IsPumpRunning;
    private static IDisposable? s_PumpSubscription;
    private static BaseUnitEntity? s_Initiator;
    private static int s_PumpWarnCount;

    public static readonly NpcClassificationCache Cache = new();
    public static readonly MarkerScheduler Scheduler = new();

    private NpcDialogClassifierRuntime() {
    }

    /// <summary>Called by the nameplate features on Tab edges to force re-evaluation.</summary>
    public static void Invalidate(BaseUnitEntity unit) => Cache.Invalidate(unit);

    public static void InvalidateAll() => Cache.InvalidateAll();

    private static BaseUnitEntity? Initiator => s_Initiator ??= Game.Instance?.Player?.MainCharacterEntity;

    /// <summary>
    /// Fresh cached state, or null when (re)evaluation would be needed: the
    /// caller then defers the unit via Scheduler so per-frame budgets hold.
    /// A missing initiator (prologue/main menu churn) is a stable None, not
    /// a miss - callers must not re-enqueue every frame for it.
    /// </summary>
    public static NpcDialogMarkerState? GetFreshState(BaseUnitEntity unit) {
        var initiator = Initiator;
        if (initiator == null) {
            return NpcDialogMarkerState.None;
        }
        return Cache.TryGetFresh(unit, initiator, out var state) ? state : null;
    }

    /// <summary>Immediate classification (map-open one-shot path); refreshes the cache.</summary>
    public static NpcDialogMarkerState ClassifyNow(BaseUnitEntity unit) {
        var initiator = Initiator;
        if (initiator == null) {
            return NpcDialogMarkerState.None;
        }
        return Cache.GetOrAdd(unit, initiator, () => NpcDialogClassifier.Classify(unit, initiator));
    }

    public static void EnsureSubscribed() {
        if (m_IsSubscribed || Game.Instance == null) {
            return;
        }
        m_IsSubscribed = true;
        _ = EventBus.Subscribe(s_Instance);
    }

    public static void EnsurePumpStarted() {
        if (m_IsPumpRunning) {
            return;
        }
        m_IsPumpRunning = true;
        // The pump is the only per-frame cost of the classifier family and is
        // gated on queue-non-empty: zero steady-state cost.
        s_PumpSubscription = MainThreadDispatcher.UpdateAsObservable().Subscribe((UniRx.Unit _) => {
            if (Scheduler.PendingCount == 0) {
                return;
            }
            try {
                int pending = Scheduler.PendingCount;
                var pumped = Scheduler.Pump();
                if (pumped.Count > 0 && TmProbe.OncePerArea("pump-heartbeat")) {
                    // One heartbeat per area proving the pump is alive on the
                    // CURRENT dispatcher instance (v9 probe: it silently died
                    // on the first scene change and markers/nameplates both
                    // went dark for the rest of the session).
                    Log($"ToyBox EnhancedMap: classification pump tick (pending={pending})");
                }
                foreach (var unit in pumped) {
                    ClassifyNow(unit);
                }
                Pumped?.Invoke(null, EventArgs.Empty);
            } catch (Exception ex) {
                // An exception escaping into the observable pipeline would
                // kill deferred classification for the whole session; dispose
                // the live subscription BEFORE clearing the latch (the same
                // dispose-first order RearmPump uses) so the next
                // EnsurePumpStarted re-arm can never stack a second parallel
                // pump beside this one.
                try {
                    s_PumpSubscription?.Dispose();
                } catch {
                    // The dispatcher itself may be mid-teardown; the latch
                    // clear below still lets EnsurePumpStarted resubscribe.
                }
                s_PumpSubscription = null;
                m_IsPumpRunning = false;
                if (s_PumpWarnCount < 3) {
                    s_PumpWarnCount++;
                    Warn($"ToyBox EnhancedMap: classification pump failed: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// Re-arm the defer-queue pump. The one-shot subscription binds to the
    /// MainThreadDispatcher instance alive at subscribe time; if that
    /// dispatcher dies on a scene change the subscription dies silently while
    /// m_IsPumpRunning stays latched true, so nothing ever re-subscribes (the
    /// game's own views self-heal by re-subscribing per bind; we do not).
    /// Dispose the old subscription (even a dead one), clear the latch and
    /// subscribe fresh on the current dispatcher.
    /// </summary>
    private static void RearmPump() {
        try {
            s_PumpSubscription?.Dispose();
        } catch {
            // A dispatcher destroyed by a scene change may throw on dispose;
            // the subscription is being replaced either way.
        }
        s_PumpSubscription = null;
        m_IsPumpRunning = false;
        EnsurePumpStarted();
    }

    /// <summary>Raised after a pump batch so the marker feature can append widgets.</summary>
    public static event EventHandler? Pumped;

    public void HandleDialogFinished(BlueprintDialog dialog, bool success) {
        if (!success) {
            // Vanilla also reports FAILED starts here (busy/cutscene-blocked
            // NPC clicks); wiping the whole area cache per failed click would
            // discard fresh work the nameplate/marker families depend on.
            return;
        }
        InvalidateAll();
        s_Initiator = null;
    }

    public void OnEtudesUpdate() => InvalidateAll();

    public void OnAreaDidLoad() {
        InvalidateAll();
        Scheduler.Clear();
        s_Initiator = null;
        // FontMod may load/enable after our first lookup attempt; let the
        // provider retry on each area load until it has an asset.
        WenKaiFontProvider.ResetForAreaLoad();
        // Prewarm the font bake behind the loading screen so the first
        // nameplate bind never pays the multi-ms CreateFontAsset in-frame.
        WenKaiFontProvider.Prewarm();
        // Fresh dispatcher instance per scene: the old subscription must not
        // survive the area change, and the latch must not block the re-arm.
        RearmPump();
    }

    public void OnAreaBeginUnloading() {
    }
}
