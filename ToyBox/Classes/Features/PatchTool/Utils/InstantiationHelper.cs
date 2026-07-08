using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using System.Reflection;

namespace ToyBox.Features.PatchTool.Utils;

public static partial class PatchToolUtils {
    public static (HashSet<Type>, HashSet<Type>?) GetInstantiableTypes(Type elementType, object? maybeParent, bool skipRecursion = false, bool canBeNonInstantiable = false) {
        if (!skipRecursion) {
            if (elementType.IsArray) {
                return (GetInstantiableArrayTypes(elementType), null);
            }
            if (elementType.IsGenericType) {
                return (GetInstantiableGenericTypes(elementType), null);
            }
        }
        HashSet<Type>? allowedinstantiableTypes = typeof(BlueprintComponent).IsAssignableFrom(elementType) ? [] : null;
        HashSet<Type> allinstantiableTypes = [];
        var parentType = maybeParent?.GetType();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = [.. ex.Types.Where(t => t != null)];
            }

            foreach (var type in types) {
                if (type == null) {
                    continue;
                }

                if (elementType.IsAssignableFrom(type) && ((!type.IsAbstract && !type.IsInterface && !TypeOrBaseIsDirectlyInUnityDLL(type)) || canBeNonInstantiable)) {
                    if (parentType != null && allowedinstantiableTypes != null) {
                        var attributes = type.GetCustomAttributes(typeof(AllowedOnAttribute), inherit: false);
                        if (attributes.Length > 0) {
                            foreach (var attr in attributes.Cast<AllowedOnAttribute>()) {
                                if (attr.Type.IsAssignableFrom(parentType)) {
                                    _ = allowedinstantiableTypes.Add(type);
                                }
                            }
                        }
                    }
                    _ = allinstantiableTypes.Add(type);
                }
            }
        }
        return (allinstantiableTypes, allowedinstantiableTypes);
    }
    public static HashSet<Type> GetInstantiableArrayTypes(Type elementType) {
        if (!elementType.IsArray) {
            return [];
        }

        var arrayElementType = elementType.GetElementType();
        if (arrayElementType == null) {
            return [];
        }

        var elementInstantiableTypes = GetInstantiableTypes(arrayElementType, null, false, true).Item1;
        HashSet<Type> arrayTypes = [];
        foreach (var type in elementInstantiableTypes) {
            _ = arrayTypes.Add(type.MakeArrayType());
        }

        return arrayTypes;
    }
    public static HashSet<Type> GetInstantiableGenericTypes(Type elementType) {
        if (!elementType.IsGenericType) {
            return [];
        }

        HashSet<Type> genericTypes = [];

        var genericArguments = elementType.GetGenericArguments();
        List<HashSet<Type>> possibleArguments = [];

        foreach (var arg in genericArguments) {
            possibleArguments.Add(GetValidTypesForGenericParameter(arg));
        }

        var typeToModify = elementType.IsGenericTypeDefinition ? elementType : elementType.GetGenericTypeDefinition();
        foreach (var combination in GenerateCombinations(possibleArguments)) {
            try {
                var concreteType = typeToModify.MakeGenericType([.. combination]);
                if (concreteType != null) {
                    _ = genericTypes.Add(concreteType);
                }
            } catch {
            }
        }

        return genericTypes;
    }
    private static HashSet<Type> GetValidTypesForGenericParameter(Type genericParameter) {
        HashSet<Type> validTypes = [];

        if (genericParameter.IsGenericParameter) {
            var constraints = genericParameter.GetGenericParameterConstraints();

            foreach (var type in GetInstantiableTypes(typeof(object), null, true, true).Item1) {

                var satisfiesConstraints = true;
                foreach (var constraint in constraints) {
                    if (!constraint.IsAssignableFrom(type)) {
                        satisfiesConstraints = false;
                        break;
                    }
                }

                if (satisfiesConstraints) {
                    _ = validTypes.Add(type);
                }
            }

            return validTypes;
        } else {
            return GetInstantiableTypes(genericParameter, null, false, true).Item1;
        }
    }
    private static IEnumerable<List<Type>> GenerateCombinations(List<HashSet<Type>> lists) {
        if (lists.Count == 0) {
            yield return new List<Type>();
        } else {
            var first = lists[0];
            var rest = lists.Skip(1).ToList();

            foreach (var type in first) {
                foreach (var combination in GenerateCombinations(rest)) {
                    yield return [type, .. combination];
                }
            }
        }
    }
}
