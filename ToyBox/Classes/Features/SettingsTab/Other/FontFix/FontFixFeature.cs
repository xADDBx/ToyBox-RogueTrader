using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace ToyBox.Features.FontFix;

public record struct FontInfo(string Family, string Style);

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.FontFix.FontFixFeature")]
public partial class FontFixFeature : FeatureWithPatch, INeedEarlyInitFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableFontFix;
        }
    }
    [LocalizedString("ToyBox_Features_FontFix_FontFixFeature_Name", "Fix game fonts")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_FontFix_FontFixFeature_Description", "There is a Unity bug that causes some systen fonts to become weird in IMGUI (the mod GUI). This feature fixes that.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.FontFix.FontFixFeature";
        }
    }

    #region Patches
    private static FontAsset Replacement(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport) {
        if (font?.fontNames?.Length > 0 && TryFind(font.fontNames[0], FontCache.CacheInstance.CachedFontInfo, out var result)) {
            return FontAsset.CreateFontAsset(result.Family, result.Style, 90);
        } else {
            return FontAsset.CreateFontAsset(font, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);
        }
    }
    [HarmonyPatch(typeof(FontAssetFactory), nameof(FontAssetFactory.ConvertFontToFontAsset)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Patch(IEnumerable<CodeInstruction> instructions) {
        var meth = AccessTools.Method(typeof(FontAsset), nameof(FontAsset.CreateFontAsset), [typeof(Font), typeof(int), typeof(int), typeof(GlyphRenderMode), typeof(int), typeof(int), typeof(AtlasPopulationMode), typeof(bool)]);
        var meth2 = AccessTools.Method(typeof(FontFixFeature), nameof(FontFixFeature.Replacement));
        foreach (var inst in instructions) {
            if (inst.Calls(meth)) {
                inst.operand = meth2;
            }
            yield return inst;
        }
    }
    private static (string Display, bool IsRegular) Normalize(string family, string style) {
        var parts = style.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        var isRegular = parts.Any(p => p == "Regular");
        var cleanedStyle = string.Join(" ", [.. parts.Where(p => p != "Regular")]);

        var display = string.IsNullOrEmpty(cleanedStyle) ? family : $"{family} {cleanedStyle}";

        return (display, isRegular);
    }

    public static bool TryFind(string query, IEnumerable<FontInfo> fonts, out FontInfo result) {
        var best = fonts.AsParallel().AsOrdered().Select((f, index) => {
            var (display, isRegular) = Normalize(f.Family, f.Style);

            var score = int.MinValue;

            if (query == f.Family) {
                score = 3000 + f.Family.Length + (isRegular ? 100 : 0);
            } else if (query == display) {
                score = 4000 + f.Family.Length;
            } else if (query.StartsWith(f.Family + " ", StringComparison.OrdinalIgnoreCase)) {
                score = 1000 + f.Family.Length;
            }

            return new {
                Index = index,
                Font = new FontInfo(f.Family, f.Style),
                Score = score
            };
        }).OrderByDescending(x => x.Score).ThenBy(x => x.Index).FirstOrDefault();

        if (best is null || best.Score < 0) {
            result = default;
            return false;
        }

        result = best.Font;
        return true;
    }
    #endregion
}
