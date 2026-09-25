using Kingmaker.Controllers.Optimization;
using UnityEngine.SceneManagement;

namespace ToyBox.Infrastructure;

/// <summary>
/// The game's EntityBoundsController lazily creates a local-physics scene
/// named "EntityBounds" per controller instance. On an area transition where
/// the PREVIOUS scene was never fully unloaded (observed
/// after loading an old save while unit-spawning mods were active), the new
/// controller's CreateScene throws ArgumentException "scene already exists"
/// and every Simulation tick retries — an observed 33k exceptions in 12
/// minutes, wedging downstream systems (combat mode exit, interaction skill
/// checks). This finalizer makes the getter self-heal: on that exception,
/// adopt the existing scene instead of throwing forever.
/// </summary>
public static class EntityBoundsCompat {
    private static bool m_IsInitialized = false;
    private static bool m_WarnedHeal = false;

    internal static void Initialize() {
        if (m_IsInitialized) {
            return;
        }
        try {
            var target = AccessTools.Property(typeof(EntityBoundsController), nameof(EntityBoundsController.Scene))?.GetGetMethod(true);
            var finalizer = new HarmonyMethod(AccessTools.Method(typeof(EntityBoundsCompat), nameof(SceneGetterFinalizer)));
            if (target == null) {
                Warn("EntityBoundsCompat: Scene getter not found; patch skipped");
                return;
            }
            _ = Main.HarmonyInstance.Patch(target, finalizer: finalizer);
            m_IsInitialized = true;
            Debug("EntityBoundsCompat: EntityBoundsController.Scene self-heal patch applied");
        } catch (Exception ex) {
            Warn($"EntityBoundsCompat failed to apply: {ex}");
        }
    }

    private static Exception? SceneGetterFinalizer(Exception __exception, ref Scene __result, ref Scene ___m_Scene) {
        if (__exception is not ArgumentException) {
            return __exception;
        }
        try {
            var existing = SceneManager.GetSceneByName("EntityBounds");
            if (existing.IsValid()) {
                ___m_Scene = existing;
                __result = existing;
                if (!m_WarnedHeal) {
                    m_WarnedHeal = true;
                    Warn("EntityBoundsCompat: adopted an already-existing EntityBounds scene (stale unload after area transition) — exception storm stopped");
                }
                return null;
            }
        } catch {
            // Fall through: rethrow the original.
        }
        return __exception;
    }
}
