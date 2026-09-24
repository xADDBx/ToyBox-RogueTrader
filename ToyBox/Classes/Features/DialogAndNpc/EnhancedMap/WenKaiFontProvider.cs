using System.Reflection;
using TMPro;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// WenKai font for nameplates (R8). FontMod hooks
/// TextMeshProUGUI.LoadFontAsset, which never fires for pooled nameplate
/// prefabs with serialized font refs - they escape FontMod. We are the last
/// writer after bind and assign .font directly.
///
/// Primary path (all public in FontMod):
/// FontMod.FontSwap.FontMapper.Instance.DefaultFontMapping.TMP_FontAsset,
/// reusing FontMod's own MaterialReferenceManager registration (never
/// re-registering: the duplicate AddFontAsset throws and used to discard the
/// whole lookup - v9 probe). Fallback (production-proven by FontMod itself):
/// build the asset from FontMod's on-disk LXGWWenKai-Medium.ttf via
/// TMP_FontAsset.CreateFontAsset(new Font(path)). No font redistribution.
///
/// FontMod presence is proven by its assembly (FontMod.Main type) - never
/// by the UMM entry table: Rogue Trader's UMM fork does not expose the
/// mod-entry statics the previous scan relied on, and a failed scan must
/// not veto a working font.
///
/// Retry latch: s_Tried admits at most ONE lookup attempt per area load
/// (re-armed by ResetForAreaLoad while no asset exists), so an absent
/// FontMod costs one assembly scan per area and never rescans mid-area.
/// </summary>
public static class WenKaiFontProvider {
    private const string MapperTypeName = "FontMod.FontSwap.FontMapper";
    private const string FontModMainTypeName = "FontMod.Main";
    // Preferred order: non-Mono Medium > Regular > Light > Mono variants.
    // Any LXGW WenKai file is acceptable for nameplates; the weight
    // difference at 10-14px is cosmetic.
    private static readonly string[] FontFilePreference = {
        "LXGWWenKai-Medium.ttf",
        "LXGWWenKai-Regular.ttf",
        "LXGWWenKai-Light.ttf",
        "LXGWWenKaiMono-Medium.ttf",
        "LXGWWenKaiMono-Regular.ttf",
        "LXGWWenKaiMono-Light.ttf",
    };

    private static TMP_FontAsset? s_Asset;
    private static Font? s_SourceFont;
    private static bool s_Tried;

    /// <summary>
    /// FontMod may be present but not yet loaded/enabled when we first look
    /// (or appear later in the session): allow a retry on each area load
    /// until an asset exists.
    /// </summary>
    public static void ResetForAreaLoad() {
        if (s_Asset == null) {
            s_Tried = false;
        }
    }

    /// <summary>
    /// Bakes the font asset at area load (behind the loading screen) so the
    /// first nameplate bind never pays the multi-ms CreateFontAsset in-frame;
    /// respects the nameplate sub-option and the per-area retry latch.
    /// </summary>
    public static void Prewarm() {
        if (Settings.EnableOvertipNameColors && Settings.OvertipNameUseWenKai) {
            _ = GetAsset();
        }
    }

    public static TMP_FontAsset? GetAsset() {
        if (s_Tried) {
            return s_Asset;
        }
        s_Tried = true;
        // The mapped path and the disk fallback are tried INDEPENDENTLY: an
        // exception (or null) in FontMod's mapper must never skip FontMod's
        // own on-disk font (v10 audit: the old single try wrapped both).
        try {
            s_Asset = TryGetMappedAsset();
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: WenKai: mapped lookup threw ({ex.GetType().Name}: {ex.Message}), falling back to disk");
        }
        if (s_Asset != null) {
            Log("ToyBox EnhancedMap: WenKai: mapped asset OK");
            return s_Asset;
        }
        try {
            s_Asset = TryLoadFromDisk();
        } catch (Exception ex) {
            Warn($"ToyBox EnhancedMap: WenKai: disk fallback failed: {ex.Message}");
        }
        return s_Asset;
    }

    /// <summary>Assigns the font with a per-text glyph guard for non-CJK names.</summary>
    public static void ApplyFont(TextMeshProUGUI text) {
        if (text == null) {
            return;
        }
        var asset = GetAsset();
        if (asset == null) {
            Diagnose(text, "no-asset");
            return;
        }
        if (text.font == asset) {
            return;
        }
        // The guard protects names containing glyphs the SOURCE TTF cannot
        // render (checked via the FreeType-backed Font, not the atlas-bound
        // TMP asset whose HasCharacter only knows already-rendered glyphs).
        var source = s_SourceFont;
        if (source != null) {
            foreach (var ch in text.text) {
                if (!source.HasCharacter(ch)) {
                    Diagnose(text, $"missing glyph '{ch}' (U+{(int)ch:X4})");
                    return;
                }
            }
        }
        text.font = asset;
        Diagnose(text, "applied");
    }

    // Capped one-shot diagnostics (max 10 per session): which plates got the
    // font, and which were skipped and why - the party-plates question.
    private static int s_DiagCount;
    private static void Diagnose(TextMeshProUGUI text, string outcome) {
        if (s_DiagCount >= 10) {
            return;
        }
        s_DiagCount++;
        var label = text.text;
        if (label.Length > 20) {
            label = label.Substring(0, 20) + "…";
        }
        Log($"ToyBox EnhancedMap: WenKai apply [{outcome}] text='{label}' currentFont={(text.font != null ? text.font.name : "null")}");
    }

    private static TMP_FontAsset? TryGetMappedAsset() {
        var mapperType = FindType(MapperTypeName);
        if (mapperType == null) {
            // FontMod absent: the disk fallback logs the same verdict via the
            // FontMod.Main probe - no duplicate line here.
            return null;
        }
        var instance = mapperType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (instance == null) {
            Warn("ToyBox EnhancedMap: WenKai: FontMapper.Instance returned null");
            return null;
        }
        var mapping = instance.GetType().GetProperty("DefaultFontMapping")?.GetValue(instance);
        if (mapping == null) {
            Warn("ToyBox EnhancedMap: WenKai: FontMod DefaultFontMapping is null");
            return null;
        }
        var asset = mapping.GetType().GetProperty("TMP_FontAsset")?.GetValue(mapping) as TMP_FontAsset;
        if (asset == null) {
            // The observed live miss (v10 audit): FontMod running but its
            // default mapping carries no usable asset (empty-ignored or
            // failed CreateFontAsset) - one line per area, then disk.
            Warn("ToyBox EnhancedMap: WenKai: FontMod DefaultFontMapping.TMP_FontAsset is null, falling back to disk");
            return null;
        }
        // Deliberately NO MaterialReferenceManager.AddFontAsset here: FontMod
        // already registered this exact asset when it built the mapping, and
        // the second registration throws "An item with the same key has
        // already been added", which discarded the whole lookup (v9 probe).
        // The game resolves the material through FontMod's registration.
        return asset;
    }

    /// <summary>
    /// Derives FontMod's folder from the FontMod ASSEMBLY location, so no
    /// UMM entry lookup is needed at all.
    /// </summary>
    private static TMP_FontAsset? TryLoadFromDisk() {
        var assemblyLocation = FindType(FontModMainTypeName)?.Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation)) {
            Log("ToyBox EnhancedMap: WenKai: FontMod assembly not found");
            return null;
        }
        var modFolder = System.IO.Path.GetDirectoryName(assemblyLocation);
        var fontsFolder = System.IO.Path.Combine(modFolder ?? string.Empty, "Fonts");
        string? fontPath = null;
        string? fontFile = null;
        foreach (var candidate in FontFilePreference) {
            var path = System.IO.Path.Combine(fontsFolder, candidate);
            if (System.IO.File.Exists(path)) {
                fontPath = path;
                fontFile = candidate;
                break;
            }
        }
        // Accept ANY LXGW*.ttf the user dropped in, not just the known names.
        if (fontPath == null && System.IO.Directory.Exists(fontsFolder)) {
            var any = System.IO.Directory.GetFiles(fontsFolder, "LXGW*.ttf").FirstOrDefault();
            if (any != null) {
                fontPath = any;
                fontFile = System.IO.Path.GetFileName(any);
            }
        }
        if (fontPath == null) {
            Log($"ToyBox EnhancedMap: WenKai: no LXGW*.ttf found in {fontsFolder}");
            return null;
        }
        var font = new Font(fontPath) {
            name = System.IO.Path.GetFileNameWithoutExtension(fontPath),
        };
        // The dynamic TMP asset answers HasCharacter() only for glyphs already
        // rendered into its atlas, so the apply-time guard must ask the source
        // TTF instead (FreeType-backed) - otherwise common CJK like 拉/阿 are
        // "missing" before first render and the font can never be assigned.
        s_SourceFont = font;
#pragma warning disable CA1508 // CreateFontAsset is not null-annotated but FontMod itself guards a null return here
        var asset = TMP_FontAsset.CreateFontAsset(font);
        if (asset == null) {
#pragma warning restore CA1508
            Warn($"ToyBox EnhancedMap: WenKai: CreateFontAsset failed for {fontPath}");
            return null;
        }
        try {
            MaterialReferenceManager.AddFontAsset(asset);
        } catch {
            // A same-session FontMod registration can already hold this key;
            // the material entry existing at all is the outcome we wanted.
        }
        Log($"ToyBox EnhancedMap: WenKai: disk asset OK from {fontPath}");
        return asset;
    }

    private static Type? FindType(string fullName) {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            var type = assembly.GetType(fullName, throwOnError: false);
            if (type != null) {
                return type;
            }
        }
        return null;
    }
}
