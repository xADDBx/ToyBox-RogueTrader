using System.Reflection;

namespace ToyBox.Infrastructure.Patching;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ToyBoxPatchCategoryAttribute : Attribute {
    private static Dictionary<string, List<Type>>? m_HarmonyCategoryCache;
    public string Category;
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
            var fromType = HarmonyMethodExtensions.GetFromType(type);
            _ = HarmonyMethod.Merge(fromType);
            var attr = type.GetCustomAttribute<ToyBoxPatchCategoryAttribute>();
            if (!string.IsNullOrEmpty(attr?.Category)) {
                if (!m_HarmonyCategoryCache.TryGetValue(attr!.Category, out var typeList)) {
                    typeList ??= [];
                }
                typeList.Add(type);
                m_HarmonyCategoryCache[attr.Category] = typeList;
            }
        }
    }

    public string Category { get; }
}
