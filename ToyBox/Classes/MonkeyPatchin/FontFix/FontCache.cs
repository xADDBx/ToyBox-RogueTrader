using ModKit;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ToyBox.FontFix;

internal class FontCache : AbstractSettings {
    public List<FontInfo> CachedFontInfo = [];
    protected override string Name => "FontCache.json";
    private static readonly Lazy<FontCache> _instance = new(() => {
        var instance = new FontCache();
        instance.Load();
        return instance;
    });
    public static FontCache CacheInstance {
        get {
            if (_instance.Value.CachedFontInfo.Count < 1 && !m_IsRebuilding) {
                RebuildCache();
            }
            return _instance.Value;
        }
    }
    private static bool m_IsRebuilding = false;
    internal static void RebuildCache() {
        Mod.Log($"Rebuilding Font Cache...");
        m_IsRebuilding = true;

        var paths = Font.GetPathsToOSFonts();
        FontEngine.InitializeFontEngine();
        CacheInstance.CachedFontInfo = [];
        foreach (var path in paths) {
            if (FontEngine.LoadFontFace(path, 90) != FontEngineError.Success) {
                continue;
            }

            var info = FontEngine.GetFaceInfo();
            CacheInstance.CachedFontInfo.Add(new(info.familyName, info.styleName));
            FontEngine.UnloadFontFace();
        }

        CacheInstance.Save();
        m_IsRebuilding = false;
    }
}