using TMPro;
using UnityEngine;

namespace ToyBox.Infrastructure;

/// <summary>
/// TMPro's MaterialReferenceManager.AddFontAsset guards only the font-asset
/// dictionary; the follow-up material-dictionary Add is unguarded, so a second
/// font asset whose materialHashCode collides throws "An item with the same
/// key has already been added" out to the caller. Font mods that bulk-create
/// dynamic TMP assets at library load (e.g. FontMod) hit this on every font
/// after the first: FontMod's FontDataModel swallows the throw and keeps a
/// null TMP_FontAsset, silently killing all but one mapped font (observed:
/// only the title font applied, body/hint/screen fonts unchanged). Re-run
/// both inserts guarded so registration can never throw.
/// Fail-open by design: anything we cannot verify (missing reflection
/// targets, unexpected insert state) falls back to the vanilla method
/// instead of skipping it.
/// </summary>
public static class TMPFontAssetCompat {
    private static bool m_IsInitialized = false;
    private static bool m_WarnedReflectionFailure = false;
    private static bool m_WarnedInsertFailure = false;

    private static readonly System.Reflection.FieldInfo? s_FontLookupField = AccessTools.Field(typeof(MaterialReferenceManager), "m_FontAssetReferenceLookup");
    private static readonly System.Reflection.FieldInfo? s_MaterialLookupField = AccessTools.Field(typeof(MaterialReferenceManager), "m_FontMaterialReferenceLookup");

    internal static void Initialize() {
        if (m_IsInitialized) {
            return;
        }
        try {
            if (s_FontLookupField == null || s_MaterialLookupField == null) {
                // A TMP update renamed the lookup fields: patching would only
                // ever hit the fail-open path, so stay unpatched entirely.
                Warn("TMP AddFontAsset guard skipped: lookup fields not found (TMP update?)");
                return;
            }
            var target = AccessTools.Method(typeof(MaterialReferenceManager), nameof(MaterialReferenceManager.AddFontAsset), [typeof(TMP_FontAsset)]);
            var prefix = new HarmonyMethod(AccessTools.Method(typeof(TMPFontAssetCompat), nameof(AddFontAssetGuarded)));
            _ = Main.HarmonyInstance.Patch(target, prefix: prefix);
            m_IsInitialized = true;
            Debug("TMP MaterialReferenceManager.AddFontAsset guard patch applied");
        } catch (Exception ex) {
            Warn($"TMP AddFontAsset guard patch failed to apply: {ex}");
        }
    }

    private static bool AddFontAssetGuarded(TMP_FontAsset fontAsset) {
        if (fontAsset == null) {
            return true;
        }
        try {
            var manager = MaterialReferenceManager.instance;
            var fontLookup = s_FontLookupField!.GetValue(manager) as Dictionary<int, TMP_FontAsset>;
            var materialLookup = s_MaterialLookupField!.GetValue(manager) as Dictionary<int, Material>;
            if (fontLookup == null || materialLookup == null) {
                if (!m_WarnedReflectionFailure) {
                    m_WarnedReflectionFailure = true;
                    Warn("TMP AddFontAsset guard could not reach lookup dictionaries; falling back to vanilla behavior");
                }
                return true;
            }
            // First-wins like the vanilla font-asset branch, but the material
            // insert is guarded too so a shared materialHashCode can't throw.
            // Benign divergence from vanilla: when the font hash already
            // exists (vanilla no-ops entirely) we still backfill a missing
            // material entry, which is the more complete registration.
            if (!fontLookup.ContainsKey(fontAsset.hashCode)) {
                fontLookup[fontAsset.hashCode] = fontAsset;
            }
            if (!materialLookup.ContainsKey(fontAsset.materialHashCode)) {
                materialLookup[fontAsset.materialHashCode] = fontAsset.material;
            }
        } catch (Exception ex) {
            if (!m_WarnedInsertFailure) {
                m_WarnedInsertFailure = true;
                Warn($"TMP AddFontAsset guarded insert failed (reported once): {ex}");
            }
            return true;
        }
        return false;
    }
}
