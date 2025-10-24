using Kingmaker.Blueprints;
using System.Collections;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Infrastructure.Inspector;
public class InspectorNode : IComparable {
    private static readonly HashSet<string> m_ContainerMembers = [GameObjectChildPrefix, GameObjectComponentPrefix, EnumerableItemPrefix];
    public const string GameObjectChildPrefix = "ci";
    public const string GameObjectComponentPrefix = "co";
    public const string EnumerableItemPrefix = "i";
    public const string FieldPrefix = "f";
    public const string PropertyPrefix = "p";
    private readonly Type m_FieldType;
    private readonly string m_ContainerPrefix;
    internal object? Value;
    public readonly string Path;
    public readonly Type ConcreteType;
    public readonly InspectorNode? Parent;
    public readonly bool IsStatic = false;
    public readonly bool IsPublic = false;
    public readonly bool IsPrivate = false;
    public readonly bool IsNullable = false;
    public readonly bool IsEnumerable = false;
    public readonly bool IsGameObject = false;
    public readonly bool IsNull = false;
    public readonly bool IsCompilerGenerated = false;
    public bool IsExpanded = false;
    public Exception? Exception;
    public bool IsSelfMatched = false;
    public bool IsChildMatched = false;
    public bool IsMatched {
        get {
            return IsSelfMatched || IsChildMatched;
        }
    }
    public List<InspectorNode> Children {
        get {
            if (field == null) {
                InspectorTraverser.BuildChildren(this);
            }
            return field!;
        }
        internal set;
    }
    public Color? ColorOverride;
    public IEnumerable AsEnumerableSafe() {
        if (Value is IEnumerable enumerable) {
            return enumerable;
        } else {
            return Array.Empty<object>();
        }
    }
    public float? ChildNameTextMaxLength {
        get {
            if (Children.Count == 0) {
                field ??= 0;
            } else {
                field ??= CalculateLargestLabelSize(Children.Select(node => node.LabelText));
            }
            return field;
        }
    }
    public float? OwnTextLength {
        get {
            field ??= CalculateLargestLabelSize([LabelText]);
            return field;
        }
    }
    public int? ElementCount {
        get {
            field ??= Children.Where(x => m_ContainerMembers.Contains(x.m_ContainerPrefix))?.Count() ?? 0;
            return field;
        }
    }
    public string ValueText {
        get {
            if (field == null) {
                string? valueText;
                if (Exception != null) {
                    valueText = "<exception>";
                    ColorOverride = Color.red;
                } else {
                    if (IsNull) {
                        valueText = "<null>";
                        ColorOverride = Color.gray;
                    } else {
                        try {
                            valueText = Value!.ToString();
                            if (valueText is null) {
                                valueText = "<ToString returned null>";
                                ColorOverride = Color.red;
                            }
                        } catch (Exception ex) {
                            Exception = ex;
                            valueText = "<exception>";
                            ColorOverride = Color.red;
                        }
                    }
                }
                field = valueText;
            }
            return field;
        }
    }
    public string TypeNameText {
        get {
            field ??= ToyBoxReflectionHelper.GetNameWithGenericsResolved(m_FieldType);
            if (IsNullable) {
                field += "?";
            }
            return field;
        }
    }
    public string NameText { get; }
    public string LabelText {
        get {
            if (field == null) {
                var nameText = $"[{m_ContainerPrefix}] ".Grey();
                if (IsStatic) {
                    nameText += "[s] ".Magenta();
                }
                nameText += NameText;

                if (IsGameObject || IsEnumerable) {
                    nameText += " " + $"[{ElementCount}]".Yellow();
                }
                string typeName;
                if (ToyBoxReflectionHelper.PrimitiveTypes.Contains(ConcreteType)) {
                    typeName = TypeNameText.Grey();
                } else if (IsGameObject) {
                    typeName = TypeNameText.Magenta();
                } else if (IsEnumerable) {
                    typeName = TypeNameText.Cyan();
                } else {
                    typeName = TypeNameText.Orange();
                }
                nameText += " : " + typeName;
                field = nameText;
            }
            return field;
        }
    }
    public string AfterText {
        get {
            if (field == null) {
                if (ConcreteType != m_FieldType) {
                    field = ToyBoxReflectionHelper.GetNameWithGenericsResolved(ConcreteType).Yellow();
                } else {
                    field = "";
                }
            }
            return field;
        }
    }
    public InspectorNode(string name, string path, Type type, object? value, InspectorNode? parent, string containerPrefix, bool isStatic = false, bool isPublic = true, bool isPrivate = false, bool isCompilerGenerated = false) {
        NameText = name;
        Value = value;
        Path = path + name + "/";
        if (Value is BlueprintReferenceBase bpRef) {
            _ = bpRef.GetBlueprint();
        }
        m_FieldType = type;
        if (Value != null) {
            type = Value.GetType();
        }
        Parent = parent;
        IsStatic = isStatic;
        IsPublic = isPublic;
        IsPrivate = isPrivate;
        IsNull = value is null;
        if (ToyBoxReflectionHelper.IsNullableT(type, out var maybeUnderlying)) {
            ConcreteType = maybeUnderlying!;
            IsNullable = true;
        } else {
            ConcreteType = type;
            IsNullable = false;
        }
        IsGameObject = typeof(GameObject).IsAssignableFrom(ConcreteType);
        IsEnumerable = typeof(IEnumerable).IsAssignableFrom(ConcreteType) && ConcreteType != typeof(string);
        m_ContainerPrefix = containerPrefix;
        IsCompilerGenerated = isCompilerGenerated;
    }

    internal InspectorNode(InspectorNode node) {
        NameText = node.NameText;
        Value = node.Value;
        Path = node.Path;
        m_FieldType = node.m_FieldType;
        Parent = node.Parent;
        IsStatic = node.IsStatic;
        IsPublic = node.IsPublic;
        IsPrivate = node.IsPrivate;
        IsNull = node.IsNull;
        ConcreteType = node.ConcreteType;
        IsNullable = node.IsNullable;
        IsGameObject = node.IsGameObject;
        IsEnumerable = node.IsEnumerable;
        m_ContainerPrefix = node.m_ContainerPrefix;
        IsCompilerGenerated = node.IsCompilerGenerated;
    }

    public int CompareTo(object obj) {
        if (obj is not InspectorNode other) {
            return 1;
        }
        return (m_ContainerPrefix + NameText).CompareTo(other.m_ContainerPrefix + other.NameText);
    }
}
