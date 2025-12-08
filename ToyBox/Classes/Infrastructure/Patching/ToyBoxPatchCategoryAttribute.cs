using System.Reflection;
using System.Runtime.CompilerServices;

namespace ToyBox.Infrastructure.Patching;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ToyBoxPatchCategoryAttribute : Attribute {
    private static Dictionary<string, HashSet<Type>>? m_HarmonyCategoryCache;
    public string Category { get; }
    public ToyBoxPatchCategoryAttribute(string category) {
        Category = category;
    }
    public static void PatchCategory(string categoryName, Harmony harmony) {
        if (m_HarmonyCategoryCache == null) {
            CreateHarmonyCategoryCache();
        }
        if (m_HarmonyCategoryCache!.TryGetValue(categoryName, out var toPatch)) {
            try {
                toPatch.Do(type => {
                    _ = harmony.CreateClassProcessor(type).Patch();
                });
            } catch {
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
        }
    }
    public static void CreateHarmonyCategoryCache() {
        m_HarmonyCategoryCache = [];
        foreach (var type in AccessTools.GetTypesFromAssembly(typeof(FeatureWithPatch).Assembly)) {
            // Look at type itself or declaring type (for inner types)
            var attr = type.GetCustomAttribute<ToyBoxPatchCategoryAttribute>();
            if (attr == null && type.GetCustomAttribute<CompilerGeneratedAttribute>() == null) {
                attr = type.DeclaringType?.GetCustomAttribute<ToyBoxPatchCategoryAttribute>();
                if (attr != null) {
                    Debug($"Category attribute null for {type.Name}; found on declaring type {type.DeclaringType!.Name} instead.");
                }
            }
            if (!string.IsNullOrEmpty(attr?.Category)) {
                if (!m_HarmonyCategoryCache.TryGetValue(attr!.Category, out var typeList)) {
                    typeList ??= [];
                }
                typeList.Add(type);
                m_HarmonyCategoryCache[attr.Category] = typeList;
            } else if (type.GetCustomAttribute<HarmonyPatch>() != null) {
                Error($"If you see this please report to mod author! Found type {type.FullName} with patches but no assigned category. This means the patch probably won't be applied.", false);
            }
        }
    }
}
