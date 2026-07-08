using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ToyBox.Features.FontFix;

// Persisted list of the OS fonts (family + style), so FontFixFeature can match against them without re-scanning the disk every launch.
internal class FontCache : AbstractJsonSettings {
    public List<FontInfo> CachedFontInfo = [];
    protected override string Name {
        get {
            return "FontCache.json";
        }
    }
    private static readonly Lazy<FontCache> m_Instance = new(() => {
        var instance = new FontCache();
        instance.Load();
        return instance;
    });
    public static FontCache CacheInstance {
        get {
            if (m_Instance.Value.CachedFontInfo.Count < 1 && !m_IsRebuilding) {
                RebuildCache();
            }
            return m_Instance.Value;
        }
    }
    private static bool m_IsRebuilding = false;
    internal static void RebuildCache() {
        Log("Rebuilding Font Cache...");
        m_IsRebuilding = true;
        var paths = Font.GetPathsToOSFonts();
        _ = FontEngine.InitializeFontEngine();
        CacheInstance.CachedFontInfo = [];
        foreach (var path in paths) {
            if (FontEngine.LoadFontFace(path, 90) != FontEngineError.Success) {
                continue;
            }
            var info = FontEngine.GetFaceInfo();
            CacheInstance.CachedFontInfo.Add(new(info.familyName, info.styleName));
            _ = FontEngine.UnloadFontFace();
        }
        CacheInstance.Save();
        m_IsRebuilding = false;
    }
}
