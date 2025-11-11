namespace ToyBox.Infrastructure.Utilities;

public static class ToyBoxReflectionHelper {
    public static readonly HashSet<Type> PrimitiveTypes = [
        typeof(DBNull), typeof(bool), typeof(char),
        typeof(sbyte), typeof(byte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float), typeof(double),
        typeof(decimal), typeof(string),
        typeof(IntPtr), typeof(UIntPtr)
        ];
    public static bool IsNullableT(Type typeToCheck, out Type? underlying) {
        var maybeUnderlying = Nullable.GetUnderlyingType(typeToCheck);
        if (maybeUnderlying == null && typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            // Field type is generic type definition Nullable<T>
            // I don't know if this will work; I won't catch since if it fails it should fail loudly
            maybeUnderlying = typeToCheck.GetGenericArguments()[0];
        }
        underlying = maybeUnderlying;
        return underlying != null;
    }
    private static readonly Dictionary<Type, string> m_ResolvedNamesCache = [];
    public static string GetNameWithGenericsResolved(Type type) {
        if (!m_ResolvedNamesCache.TryGetValue(type, out var name)) {
            if (type.IsGenericType) {
                var baseName = type.Name;
                var backtick = baseName.IndexOf('`');
                if (backtick > 0) {
                    baseName = baseName.Substring(0, backtick);
                }
                var args = type.GetGenericArguments().Select(GetNameWithGenericsResolved);

                name = $"{baseName}<{string.Join(", ", args)}>";
            } else if (type.IsArray) {
                name = $"{GetNameWithGenericsResolved(type.GetElementType())}[]";
            } else {
                name = type.Name;
            }
            m_ResolvedNamesCache[type] = name;
        }
        return name;
    }
}
