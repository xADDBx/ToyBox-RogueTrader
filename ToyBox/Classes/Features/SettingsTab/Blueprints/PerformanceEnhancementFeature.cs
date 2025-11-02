using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ToyBox.Features.SettingsFeatures.Blueprints;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsFeatures.Blueprints.PerformanceEnhancementFeatures")]
public partial class PerformanceEnhancementFeatures : FeatureWithPatch {
    [LocalizedString("ToyBox_Features_SettingsFeatures_BlueprintLoaderSettings_PerformanceEnhancementFeatures_PerformanceEnhancementText", "Performance Enhancement")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_BlueprintLoaderSettings_PerformanceEnhancementFeatures_EnhancesBlueprintLoadingPerforma", "Enhances Blueprint loading performance")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.SettingsFeatures.Blueprints.PerformanceEnhancementFeatures";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableBlueprintPerformancePatches;
        }
    }

    private static readonly ConcurrentDictionary<(Type, Type), bool> m_HasAttributeCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), bool> m_IsListOfCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), bool> m_IsListCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), bool> m_IsOrSubclassOfCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object>?> m_TypeConstructorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<int, Array>> m_ArrayTypeConstructorCache = new();
    private static readonly MethodInfo m_Activator_CreateInstance = AccessTools.Method(typeof(Activator), nameof(Activator.CreateInstance), [typeof(Type)]);
    private static readonly MethodInfo m_Array_CreateInstance = AccessTools.Method(typeof(Array), nameof(Array.CreateInstance), [typeof(Type), typeof(int)]);
    private static readonly MethodInfo m_ReflectionBasedSerializer_CreateObject = AccessTools.Method(typeof(ReflectionBasedSerializer), nameof(ReflectionBasedSerializer.CreateObject));
    [HarmonyTargetMethods]
    internal static IEnumerable<MethodBase> GetMethods() {
        var ret = new List<MethodBase>();
        foreach (var method in typeof(ReflectionBasedSerializer).GetMethods(AccessTools.all).Concat(typeof(PrimitiveSerializer).GetMethods(AccessTools.all)).Concat(typeof(BlueprintFieldsTraverser).GetMethods(AccessTools.all)).Concat(typeof(FieldsContractResolver).GetMethods(AccessTools.all))) {
            try {
                foreach (var instruction in PatchProcessor.GetCurrentInstructions(method) ?? []) {
                    if (instruction.Calls(m_Activator_CreateInstance)) {
                        ret.Add(method);
                        break;
                    } else if (instruction.Calls(m_Array_CreateInstance)) {
                        ret.Add(method);
                        break;
                    } else if (instruction.Calls(m_ReflectionBasedSerializer_CreateObject)) {
                        ret.Add(method);
                        break;
                    } else if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo) {
                        if (methodInfo.Name == "HasAttribute" && methodInfo.IsGenericMethod) {
                            ret.Add(method);
                            break;
                        } else if (methodInfo.Name == "IsListOf" && methodInfo.IsGenericMethod) {
                            ret.Add(method);
                            break;
                        } else if (methodInfo.Name == "IsList" && methodInfo.IsGenericMethod) {
                            ret.Add(method);
                            break;
                        } else if (methodInfo.Name == "IsOrSubclassOf" && methodInfo.IsGenericMethod) {
                            ret.Add(method);
                            break;
                        }
                    }
                }
            } catch { }
        }
        return ret;
    }
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var method1 = AccessTools.Method(typeof(PerformanceEnhancementFeatures), nameof(HasAttribute));
        var method2 = AccessTools.Method(typeof(PerformanceEnhancementFeatures), nameof(IsListOf));
        var method3 = AccessTools.Method(typeof(PerformanceEnhancementFeatures), nameof(IsList));
        var method4 = AccessTools.Method(typeof(PerformanceEnhancementFeatures), nameof(IsOrSubclassOf));
        foreach (var instruction in instructions) {
            if (instruction.Calls(m_Activator_CreateInstance)) {
                yield return CodeInstruction.Call((Type t) => CreateInstance(t));
                continue;
            } else if (instruction.Calls(m_Array_CreateInstance)) {
                yield return CodeInstruction.Call((Type t, int n) => CreateArrayInstance(t, n));
                continue;
            } else if (instruction.Calls(m_ReflectionBasedSerializer_CreateObject)) {
                yield return CodeInstruction.Call((Type t) => CreateObject(t));
                continue;
            } else if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo) {
                if (methodInfo.Name == "HasAttribute" && methodInfo.IsGenericMethod) {
                    var genericArguments = methodInfo.GetGenericArguments();
                    yield return new CodeInstruction(OpCodes.Ldtoken, genericArguments[0]);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle)));
                    yield return new CodeInstruction(OpCodes.Call, method1);
                    continue;
                } else if (methodInfo.Name == "IsListOf" && methodInfo.IsGenericMethod) {
                    var genericArguments = methodInfo.GetGenericArguments();
                    yield return new CodeInstruction(OpCodes.Ldtoken, genericArguments[0]);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle)));
                    yield return new CodeInstruction(OpCodes.Call, method2);
                    continue;
                } else if (methodInfo.Name == "IsList" && methodInfo.IsGenericMethod) {
                    var genericArguments = methodInfo.GetGenericArguments();
                    yield return new CodeInstruction(OpCodes.Ldtoken, genericArguments[0]);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle)));
                    yield return new CodeInstruction(OpCodes.Call, method3);
                    continue;
                } else if (methodInfo.Name == "IsOrSubclassOf" && methodInfo.IsGenericMethod) {
                    var genericArguments = methodInfo.GetGenericArguments();
                    yield return new CodeInstruction(OpCodes.Ldtoken, genericArguments[0]);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle)));
                    yield return new CodeInstruction(OpCodes.Call, method4);
                    continue;
                }
            }
            yield return instruction;
        }
    }
    public static object CreateInstance(Type type) {
        if (!m_TypeConstructorCache.TryGetValue(type, out var f)) {
            if (type.GetConstructor(Type.EmptyTypes) is { } constructor) {
                f = m_TypeConstructorCache[type] =
                    Expression.Lambda<Func<object>>(
                        Expression.Convert(
                            Expression.New(constructor), typeof(object)))
                    .Compile();
            } else {
                f = m_TypeConstructorCache[type] = null;
            }
        }
        if (f == null) {
            return Activator.CreateInstance(type);
        }
        return f();
    }
    public static object CreateObject(Type type) {
        if (!m_TypeConstructorCache.TryGetValue(type, out var f)) {
            if (type.GetConstructor(Type.EmptyTypes) is { } constructor) {
                f = m_TypeConstructorCache[type] =
                    Expression.Lambda<Func<object>>(
                        Expression.Convert(
                            Expression.New(constructor), typeof(object)))
                    .Compile();
            } else {
                f = m_TypeConstructorCache[type] = null;
            }
        }
        if (f == null) {
            return FormatterServices.GetUninitializedObject(type);
        }
        return f();
    }
    public static object CreateArrayInstance(Type type, int length) {
        if (!m_ArrayTypeConstructorCache.TryGetValue(type, out var f)) {
            var param = Expression.Parameter(typeof(int), "length");
            var newArrayExpression = Expression.NewArrayBounds(type, param);
            var lambda = Expression.Lambda<Func<int, Array>>(newArrayExpression, param);

            f = m_ArrayTypeConstructorCache[type] = lambda.Compile();
        }
        if (f == null) {
            return Array.CreateInstance(type, length);
        }
        return f(length);
    }
    public static bool HasAttribute(Type t, Type T) {
        if (!m_HasAttributeCache.TryGetValue((t, T), out var hasAttr)) {
            hasAttr = t.GetCustomAttributes(T, true).Length != 0;
            m_HasAttributeCache[(t, T)] = hasAttr;
        }
        return hasAttr;
    }
    public static bool IsListOf(Type t, Type T) {
        if (!m_IsListOfCache.TryGetValue((t, T), out var isListOf)) {
            isListOf = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) && t.GenericTypeArguments[0] == T;
            m_IsListOfCache[(t, T)] = isListOf;
        }
        return isListOf;
    }
    public static bool IsList(Type t, Type T) {
        if (!m_IsListCache.TryGetValue((t, T), out var isList)) {
            isList = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
            m_IsListCache[(t, T)] = isList;
        }
        return isList;
    }
    public static bool IsOrSubclassOf(Type t, Type T) {
        if (!m_IsOrSubclassOfCache.TryGetValue((t, T), out var isSubclassOf)) {
            isSubclassOf = t == T || t.IsSubclassOf(T);
            m_IsOrSubclassOfCache[(t, T)] = isSubclassOf;
        }
        return isSubclassOf;
    }
}
