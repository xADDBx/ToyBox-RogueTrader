using System.Reflection;

namespace ToyBox;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LocalizedStringAttribute : Attribute {
    private static IEnumerable<(FieldInfo, LocalizedStringAttribute)>? _fieldsWithAttribute;
    public string Key { get; }
    public string DefaultValue { get; }
    public LocalizedStringAttribute(string key, string defaultValue) {
        Key = key;
        DefaultValue = defaultValue;
    }
    public static IEnumerable<(FieldInfo, LocalizedStringAttribute)> GetFieldsWithAttribute() {
        _fieldsWithAttribute ??= Assembly.GetExecutingAssembly().GetTypes().SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        .Select(f => (f, f.GetCustomAttribute<LocalizedStringAttribute>())).Where(pair => pair.Item2 != null);
        return _fieldsWithAttribute;
    }
}
