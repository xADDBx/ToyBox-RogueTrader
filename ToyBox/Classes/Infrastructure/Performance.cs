using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    //[HarmonyPatch]
    public static class Performance {
        [HarmonyPatch]
        internal static class ReflectionBasedSerializer_PerformancePatches {
            private static ConcurrentDictionary<(Type, Type), bool> HasAttributeCache = new();
            private static ConcurrentDictionary<(Type, Type), bool> IsListOfCache = new();
            private static ConcurrentDictionary<(Type, Type), bool> IsListCache = new();
            private static ConcurrentDictionary<(Type, Type), bool> IsOrSubclassOfCache = new();
            private static ConcurrentDictionary<Type, Func<object>> TypeConstructorCache = new();
            private static ConcurrentDictionary<Type, Func<int, Array>> ArrayTypeConstructorCache = new();
            private static MethodInfo Activator_CreateInstance = AccessTools.Method(typeof(Activator), nameof(Activator.CreateInstance), [typeof(Type)]);
            private static MethodInfo Array_CreateInstance = AccessTools.Method(typeof(Array), nameof(Array.CreateInstance), [typeof(Type), typeof(int)]);
            private static MethodInfo ReflectionBasedSerializer_CreateObject = AccessTools.Method(typeof(ReflectionBasedSerializer), nameof(ReflectionBasedSerializer.CreateObject));
            [HarmonyTargetMethods]
            internal static IEnumerable<MethodBase> GetMethods() {
                var ret = new List<MethodBase>();
                foreach (var method in typeof(ReflectionBasedSerializer).GetMethods(AccessTools.all).Concat(typeof(PrimitiveSerializer).GetMethods(AccessTools.all)).Concat(typeof(BlueprintFieldsTraverser).GetMethods(AccessTools.all)).Concat(typeof(FieldsContractResolver).GetMethods(AccessTools.all))) {
                    try {
                        foreach (var instruction in PatchProcessor.GetCurrentInstructions(method) ?? new()) {
                            if (instruction.Calls(Activator_CreateInstance)) {
                                ret.Add(method);
                                break;
                            } else if (instruction.Calls(Array_CreateInstance)) {
                                ret.Add(method);
                                break;
                            } else if (instruction.Calls(ReflectionBasedSerializer_CreateObject)) {
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
                var method1 = AccessTools.Method(typeof(ReflectionBasedSerializer_PerformancePatches), nameof(HasAttribute));
                var method2 = AccessTools.Method(typeof(ReflectionBasedSerializer_PerformancePatches), nameof(IsListOf));
                var method3 = AccessTools.Method(typeof(ReflectionBasedSerializer_PerformancePatches), nameof(IsList));
                var method4 = AccessTools.Method(typeof(ReflectionBasedSerializer_PerformancePatches), nameof(IsOrSubclassOf));
                foreach (var instruction in instructions) {
                    if (instruction.Calls(Activator_CreateInstance)) {
                        yield return CodeInstruction.Call((Type t) => CreateInstance(t));
                        continue;
                    } else if (instruction.Calls(Array_CreateInstance)) {
                        yield return CodeInstruction.Call((Type t, int n) => CreateArrayInstance(t, n));
                        continue;
                    } else if (instruction.Calls(ReflectionBasedSerializer_CreateObject)) {
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
                if (!TypeConstructorCache.TryGetValue(type, out var f)) {
                    if (type.GetConstructor(Type.EmptyTypes) is { } constructor) {
                        f = TypeConstructorCache[type] =
                            Expression.Lambda<Func<object>>(
                                Expression.Convert(
                                    Expression.New(constructor), typeof(object)))
                            .Compile();
                    } else {
                        f = TypeConstructorCache[type] = null;
                    }
                }
                if (f == null) return Activator.CreateInstance(type);
                return f();
            }
            public static object CreateObject(Type type) {
                if (!TypeConstructorCache.TryGetValue(type, out var f)) {
                    if (type.GetConstructor(Type.EmptyTypes) is { } constructor) {
                        f = TypeConstructorCache[type] =
                            Expression.Lambda<Func<object>>(
                                Expression.Convert(
                                    Expression.New(constructor), typeof(object)))
                            .Compile();
                    } else {
                        f = TypeConstructorCache[type] = null;
                    }
                }
                if (f == null) return FormatterServices.GetUninitializedObject(type);
                return f();
            }
            public static object CreateArrayInstance(Type type, int length) {
                if (!ArrayTypeConstructorCache.TryGetValue(type, out var f)) {
                    var param = Expression.Parameter(typeof(int), "length");
                    var newArrayExpression = Expression.NewArrayBounds(type, param);
                    var lambda = Expression.Lambda<Func<int, Array>>(newArrayExpression, param);

                    f = ArrayTypeConstructorCache[type] = lambda.Compile();
                }
                if (f == null) return Array.CreateInstance(type, length);
                return f(length);
            }
            public static bool HasAttribute(Type t, Type T) {
                if (!HasAttributeCache.TryGetValue((t, T), out var hasAttr)) {
                    hasAttr = t.GetCustomAttributes(T, true).Length != 0;
                    HasAttributeCache[(t, T)] = hasAttr;
                }
                return hasAttr;
            }
            public static bool IsListOf(Type t, Type T) {
                if (!IsListOfCache.TryGetValue((t, T), out var isListOf)) {
                    isListOf = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) && t.GenericTypeArguments[0] == T;
                    IsListOfCache[(t, T)] = isListOf;
                }
                return isListOf;
            }
            public static bool IsList(Type t, Type T) {
                if (!IsListCache.TryGetValue((t, T), out var isList)) {
                    isList = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
                    IsListCache[(t, T)] = isList;
                }
                return isList;
            }
            public static bool IsOrSubclassOf(Type t, Type T) {
                if (!IsOrSubclassOfCache.TryGetValue((t, T), out var isSubclassOf)) {
                    isSubclassOf = t == T || t.IsSubclassOf(T);
                    IsOrSubclassOfCache[(t, T)] = isSubclassOf;
                }
                return isSubclassOf;
            }
        }
    }
}
