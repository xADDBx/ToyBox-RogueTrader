using System.Reflection;
using ToyBox.Features.PatchTool.Infrastructure;
using ToyBox.Features.PatchTool.Utils;
using ToyBox.Infrastructure.Utilities;
using static ToyBox.Infrastructure.UI;

namespace ToyBox.Features.PatchTool;

public class AddItemState {
    internal static Dictionary<(Type element, Type parent), List<Type>> m_CompatibleTypes = [];
    internal static Dictionary<(Type element, Type parent), List<Type>?> m_AllowedTypes = [];
    public Browser<Type>? ToAddBrowser;
    private Action<Type> m_ConfirmAction = null!;
    public static AddItemState CreateComplexOrList(object parent, FieldInfo info, PatchOperation? wouldBePatch, PatchToolTabUI ui, string path) {
        var elementType = info.FieldType;
        var state = new AddItemState() {
            Parent = parent,
            Info = info,
            ElementType = elementType,
            Item = null,
            IsExpanded = true,
            WouldBePatch = wouldBePatch,
            Path = path
        };
        state.m_ConfirmAction = (Type t) => {
            var op = new InitializeFieldOperation(state.Info.Name, parent.GetType(), t);
            ui.CurrentState!.AddOp(state.WouldBePatch.AddOperation(op));
            ui.CurrentState.CreateAndRegisterPatch();
            _ = ui.AddItemStates.Remove(state.Path);
        };
        ui.AddItemStates[path] = state;

        var cacheKey = (elementType, parent.GetType());
        if (!m_CompatibleTypes.ContainsKey(cacheKey)) {
            (var all, var allowed) = PatchToolUtils.GetInstantiableTypes(elementType, parent);
            m_AllowedTypes[cacheKey] = allowed?.ToList();
            m_CompatibleTypes[cacheKey] = [.. all];
        }

        return state;
    }
    public static AddItemState? CreateArrayElement(object parent, FieldInfo info, object collection, int index, PatchOperation? wouldBePatch, PatchToolTabUI ui, string path) {
        Type? elementType = null;
        var type = collection.GetType();
        if (type.IsArray) {
            elementType = type.GetElementType();
        } else {
            try {
                var interfaces = type.GetInterfaces();
                var listInterface = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
                    ?? interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                elementType = listInterface?.GetGenericArguments()[0]
                    ?? (type.IsGenericType ? type.GetGenericArguments().FirstOrDefault() : null);
            } catch (Exception ex) {
                Log($"Error while trying to create AddItemProcess:\n{ex}");
            }
        }
        if (elementType == null) {
            Log($"Error while trying to create AddItemProcess:\nCan't find element type for type {type}");
            return null;
        }
        var state = new AddItemState() {
            Parent = parent,
            Info = info,
            Index = index,
            ElementType = elementType,
            Collection = collection,
            Item = null,
            IsExpanded = true,
            WouldBePatch = wouldBePatch,
            Path = path
        };
        state.m_ConfirmAction = (Type t) => {
            var op = new AddCollectionElementOperation(state.Info.Name, state.Parent.GetType(), t, state.Index);
            ui.CurrentState!.AddOp(state.WouldBePatch.AddOperation(op));
            ui.CurrentState.CreateAndRegisterPatch();
            _ = ui.AddItemStates.Remove(state.Path);
        };
        ui.AddItemStates[path] = state;

        var cacheKey = (elementType, parent.GetType());
        if (!m_CompatibleTypes.ContainsKey(cacheKey)) {
            (var all, var allowed) = PatchToolUtils.GetInstantiableTypes(elementType, parent);
            m_AllowedTypes[cacheKey] = allowed?.ToList();
            m_CompatibleTypes[cacheKey] = [.. all];
        }

        return state;
    }
    private void EnsureBrowser() {
        if (ToAddBrowser != null) {
            return;
        }
        var cacheKey = (ElementType, Parent.GetType());
        var all = m_CompatibleTypes[cacheKey];
        var allowed = m_AllowedTypes[cacheKey];
        var initial = allowed ?? all;
        Action<Action<IEnumerable<Type>>>? showAll = allowed != null ? (register => register(all)) : null;
        ToAddBrowser = new(t => t.ToString(), t => $"{t} {t.Name}", initial, showAll, false);
    }
    public void AddItemGUI() {
        EnsureBrowser();
        using (VerticalScope()) {
            ToAddBrowser!.OnGUI(type => {
                var generics = "";
                if (type.IsGenericType) {
                    generics = type.GetGenericArguments().Select(t => t.Name).ToContentString().Replace("\"", "");
                }
                Label($"{type.Name}{generics}", Width(500));
                Space(200);
                _ = Button(PatchToolStrings.AddAsNewEntry, () => {
                    Confirm(type);
                });
            });
        }
    }
    public void Confirm(Type type) {
        m_ConfirmAction(type);
    }
    public object Parent = null!;
    public FieldInfo Info = null!;
    public int Index;
    public object Collection = null!;
    public Type ElementType = null!;
    public object? Item;
    public bool IsExpanded;
    public PatchOperation? WouldBePatch;
    public string Path = null!;
}
