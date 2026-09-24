using System.Runtime.CompilerServices;
using Owlcat.Runtime.Visual.Waaagh.Data;
using Owlcat.Runtime.Visual.Waaagh.RendererFeatures.Highlighting;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// Runtime half of the highlight enhancement (R6 dual knob): locates the
/// Waaagh pipeline's HighlightingFeature instances and applies/reverts the
/// outline thickness mutation and fill ladder rungs 2/3 (rung 1 lives in the
/// ConstantOn prefixes - no material work). Locator copied verbatim from
/// FogOfWarControllerData.GetFogOfWarFeature with the type swapped.
///
/// Revert completeness: thickness snapshots (BlurIterations/BlurMinSpread/
/// BlurSpread) are per instance AND m_CutMaterial.shader is snapshotted
/// before the first mutation; everything is restored together, and the
/// features are re-located on every sync because Create() rebuilds all
/// materials on pipeline reload.
/// </summary>
internal static class HighlightFillRuntime {
    // 1 = alpha clamp-up in the ConstantOn prefixes (classic-system
    // semantics; verified there is no C# alpha clamp downstream).
    // 2 = Cut/Composite shader property probe (set scalar if one exists).
    // 3 = cutMaterial shader swap to BlurShader with blur keywords off.
    // Rung selection procedure (in-game session, ~15 min, DESIGN v5 10.4):
    // build rung 1 alone, toggle EnableHighlightFill, hold Tab, screenshot;
    // judge by INTERIOR-PIXEL SAMPLING against a vanilla screenshot (not
    // eyeballing - a brighter rim is a false positive; flashers such as
    // ToyBox's hidden-object flash use FlashingOn and never pass through
    // ConstantOn, so they are excluded from rung-1 judgment). If rung 1 does
    // not fill, enable the rung-2 probe log (area load) and set the
    // discovered scalar; only if no scalar exists switch to rung 3 and tune
    // the blur params. The winning rung becomes this constant.
    // Selected via a method (not a literal) so the rung-2/3 branches below
    // stay compiled and reachable; SelectedRung() is the single
    // code-level selection site.
    internal static readonly int FillRung = SelectedRung();

    // DESIGN v5 locks rung 1 (classic alpha clamp-up) as the shipped default;
    // the session protocol above may move this to 2/3 ONLY if rung 1 fails
    // interior-pixel sampling. 0 is not a rung: it dead-codes the fill while
    // the setting, its description and the changelog still promise it.
    private static int SelectedRung() => 1;

    // Rung 1 floor: alpha is only ever clamped UP (a<=0 makes
    // Highlighter.GetRendererInfos return null and highlights vanish).
    internal const float FillFloorAlpha = 0.55f;

    private static readonly string[] BlurKeywords = ["STRAIGHT_DIRECTIONS", "ALL_DIRECTIONS"];
    private static readonly string[] CutPropertyHints = ["cut", "fill", "strength", "amount"];

    private sealed class Snapshot {
        public bool Taken;
        public Shader? CutShader;
        public string? CutPropertyName;
        public float CutPropertyValue;
    }

    // Thickness originals per feature instance: the pipeline may stack
    // several renderer data blocks each carrying its own HighlightingFeature
    // (v9 probe mutated the first one only), and pipeline reloads swap the
    // instances wholesale. A CWT keeps each snapshot alive exactly as long as
    // its instance, so restore always writes back the instance's OWN
    // originals.
    private sealed class ThicknessSnapshot {
        public int BlurIterations;
        public float BlurMinSpread;
        public float BlurSpread;
    }

    private static readonly ConditionalWeakTable<HighlightingFeature, ThicknessSnapshot> s_Thickness = new();

    private static readonly Snapshot s_Snapshot = new();
    private static bool s_ProbeLogged;
    private static int s_SyncWarnCount;

    // LocateAll result cached per pipeline asset: the walk allocates a list
    // and iterates every renderer data block, but the instance set only
    // changes when the pipeline asset is swapped. Destroyed features
    // (Unity-null without an asset swap) invalidate the cache.
    private static List<HighlightingFeature>? s_LocatedInstances;
    private static WaaaghPipelineAsset? s_LocatedPipeline;

    public static HighlightingFeature? Locate() {
        var instances = LocateAllCached();
        return instances.Count > 0 ? instances[0] : null;
    }

    /// <summary>
    /// Every HighlightingFeature across the pipeline asset's renderer feature
    /// lists - not just the first found. Thickness must reach all of them or
    /// the outline keeps rendering through the untouched instance.
    /// </summary>
    public static List<HighlightingFeature> LocateAll() {
        var found = new List<HighlightingFeature>();
        var asset = GraphicsSettings.defaultRenderPipeline as WaaaghPipelineAsset;
        if (asset == null) {
            return found;
        }
        foreach (var rendererData in asset.RendererDataList) {
            if (rendererData == null) {
                continue;
            }
            foreach (var rendererFeature in rendererData.RendererFeatures) {
                if (rendererFeature is HighlightingFeature feature) {
                    found.Add(feature);
                }
            }
        }
        return found;
    }

    private static List<HighlightingFeature> LocateAllCached() {
        var asset = GraphicsSettings.defaultRenderPipeline as WaaaghPipelineAsset;
        if (asset != null && s_LocatedInstances is { Count: > 0 } cached && ReferenceEquals(asset, s_LocatedPipeline)) {
            var alive = true;
            foreach (var feature in cached) {
                if (feature == null) {
                    // A feature died without the pipeline asset changing:
                    // re-scan instead of serving a dead instance.
                    alive = false;
                    break;
                }
            }
            if (alive) {
                return cached;
            }
        }
        s_LocatedPipeline = asset;
        s_LocatedInstances = LocateAll();
        return s_LocatedInstances;
    }

    /// <summary>Idempotent: applies or removes every knob per current settings.</summary>
    public static void SyncFromSettings() {
        try {
            SyncThickness();
            SyncFill();
        } catch (Exception ex) {
            // Runs on every HighlightOn postfix - cap the spam.
            if (s_SyncWarnCount < 3) {
                s_SyncWarnCount++;
                Warn($"ToyBox EnhancedMap: highlight sync failed: {ex.Message}");
            }
        }
    }

    private static void TakeSnapshot(HighlightingFeature feature) {
        if (s_Snapshot.Taken) {
            return;
        }
        s_Snapshot.CutShader = feature.m_CutMaterial?.shader;
        s_Snapshot.Taken = true;
    }

    private static void SyncThickness() {
        List<HighlightingFeature> instances = LocateAllCached();
        if (instances.Count == 0) {
            return;
        }
        var thickness = Settings.HighlightOutlineThickness;
        // The feature toggle gates BOTH knobs (settings contract: one
        // checkbox owns fill and thickness): OFF restores every snapshot even
        // with the slider above 1, exactly like thickness <= 1.
        var apply = Settings.EnableHighlightFill && thickness > 1f;
        // Consume the once-per-area key BEFORE building any probe string so
        // the (usually discarded) parts cost nothing on ordinary syncs.
        List<string>? probeParts = TmProbe.OncePerArea("thickness-sync") ? [] : null;
        foreach (HighlightingFeature feature in instances) {
            ThicknessSnapshot snapshot = s_Thickness.GetValue(feature, f => new ThicknessSnapshot {
                BlurIterations = f.BlurIterations,
                BlurMinSpread = f.BlurMinSpread,
                BlurSpread = f.BlurSpread,
            });
            if (!apply) {
                feature.BlurIterations = snapshot.BlurIterations;
                feature.BlurMinSpread = snapshot.BlurMinSpread;
                feature.BlurSpread = snapshot.BlurSpread;
                continue;
            }
            // Spread alone is a no-op while the game default runs ZERO blur
            // iterations (v9 probe: 0.10 -> 0.40 mutated with no visual
            // change). Scale iterations with the multiplier so k>1 actually
            // widens the outline: 2x -> 2 iterations, 3x -> 4, 4x -> 6, and
            // never below the instance's own original. Clamped to the game's
            // 0..50 range.
            feature.BlurIterations = Mathf.Clamp(
                Math.Max(snapshot.BlurIterations, Mathf.RoundToInt(2f * (thickness - 1f))),
                0, 50);
            feature.BlurMinSpread = Mathf.Min(snapshot.BlurMinSpread * thickness, 3f);
            feature.BlurSpread = Mathf.Min(snapshot.BlurSpread * thickness, 3f);
            probeParts?.Add(
                $"iters {snapshot.BlurIterations}->{feature.BlurIterations}, " +
                $"minSpread {snapshot.BlurMinSpread:0.00}->{feature.BlurMinSpread:0.00}, " +
                $"spread {snapshot.BlurSpread:0.00}->{feature.BlurSpread:0.00}");
        }
        if (probeParts != null) {
            // Once per area: how many instances exist (count log) plus the
            // per-instance iteration/spread mutations, proving the fix live.
            TmProbe.Log(
                $"thickness sync instances={instances.Count} k={thickness:0.0}" +
                (probeParts.Count > 0 ? ": " + string.Join("; ", probeParts) : string.Empty));
        }
    }

    private static void SyncFill() {
        var feature = Locate();
        if (feature == null) {
            return;
        }
        if (!Settings.EnableHighlightFill || FillRung == 1) {
            // Rung 1 is pure prefix work; any material-level rung state
            // from an earlier experiment must be reverted.
            RevertFillMaterials(feature);
            return;
        }
        TakeSnapshot(feature);
        switch (FillRung) {
            case 2:
                ApplyFillProperty(feature);
                break;
            case 3:
                ApplyCutShaderSwap(feature);
                break;
        }
    }

    private static void ApplyFillProperty(HighlightingFeature feature) {
        var cut = feature.m_CutMaterial;
        var shader = feature.Shaders?.CutShader;
        if (cut == null || shader == null) {
            return;
        }
        ProbeOnce(feature);
        var name = FindCutProperty(shader);
        if (name == null) {
            return;
        }
        if (s_Snapshot.CutPropertyName == null) {
            s_Snapshot.CutPropertyName = name;
            s_Snapshot.CutPropertyValue = cut.GetFloat(name);
        }
        cut.SetFloat(name, 1f);
    }

    private static void ApplyCutShaderSwap(HighlightingFeature feature) {
        var cut = feature.m_CutMaterial;
        var blurShader = feature.Shaders?.BlurShader;
        if (cut == null || blurShader == null || cut.shader == blurShader) {
            return;
        }
        foreach (var keyword in BlurKeywords) {
            cut.DisableKeyword(keyword);
        }
        // Both shaders are _MainTex blits, so the reassignment is compatible
        // (benign property-mismatch warnings at worst).
        cut.shader = blurShader;
    }

    private static void RevertFillMaterials(HighlightingFeature feature) {
        if (!s_Snapshot.Taken) {
            return;
        }
        var cut = feature.m_CutMaterial;
        if (cut == null) {
            return;
        }
        if (s_Snapshot.CutPropertyName != null && feature.Shaders?.CutShader != null && cut.HasProperty(s_Snapshot.CutPropertyName)) {
            cut.SetFloat(s_Snapshot.CutPropertyName, s_Snapshot.CutPropertyValue);
            s_Snapshot.CutPropertyName = null;
        }
        if (s_Snapshot.CutShader != null && cut.shader != s_Snapshot.CutShader) {
            cut.shader = s_Snapshot.CutShader;
        }
    }

    private static void ProbeOnce(HighlightingFeature feature) {
        if (s_ProbeLogged) {
            return;
        }
        s_ProbeLogged = true;
        foreach (var shader in new[] { feature.Shaders?.CutShader, feature.Shaders?.CompositeShader }) {
            if (shader == null) {
                continue;
            }
            var count = shader.GetPropertyCount();
            var properties = new string[count];
            for (var i = 0; i < count; i++) {
                properties[i] = $"{shader.GetPropertyName(i)}:{shader.GetPropertyType(i)}";
            }
            Log($"ToyBox EnhancedMap highlight probe {shader.name}: {string.Join(", ", properties)}");
        }
    }

    private static string? FindCutProperty(Shader shader) {
        for (var i = 0; i < shader.GetPropertyCount(); i++) {
            var name = shader.GetPropertyName(i);
            if (shader.GetPropertyType(i) == ShaderPropertyType.Float
                && CutPropertyHints.Any(hint => name.ToUpperInvariant().Contains(hint))) {
                return name;
            }
        }
        return null;
    }
}
