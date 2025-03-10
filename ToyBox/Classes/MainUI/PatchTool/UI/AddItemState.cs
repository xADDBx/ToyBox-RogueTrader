using Kingmaker.Utility;
using ModKit;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

namespace ToyBox.PatchTool;
public class AddItemState {
    internal static Dictionary<Type, List<Type>> compatibleTypes = new();
    internal static Dictionary<Type, List<Type>> allowedTypes = new();
    public Browser<Type, Type> ToAddBrowser = new(true, true, false, false) { DisplayShowAllGUI = false };
    private Action<Type> confirmAction;
    public static AddItemState CreateComplexOrList(object parent, FieldInfo info, PatchOperation wouldBePatch, PatchToolTabUI ui, string path) {
        Type elementType = info.FieldType;
        var state = new AddItemState() {
            Parent = parent,
            Info = info,
            ElementType = elementType,
            Item = null,
            IsExpanded = true,
            WouldBePatch = wouldBePatch,
            Path = path
        };
        state.confirmAction = (Type t) => {
            PatchOperation op = new(PatchOperation.PatchOperationType.InitializeField, state.Info.Name, t, null, parent?.GetType());
            ui.CurrentState.AddOp(state.WouldBePatch.AddOperation(op));
            ui.CurrentState.CreateAndRegisterPatch();
            ui.addItemStates.Remove(state.Path);
        };
        ui.addItemStates[path] = state;

        if (!compatibleTypes.ContainsKey(elementType)) {
            (var all, var allowed) = PatchToolUtils.GetInstantiableTypes(elementType, parent);
            if (allowed != null) {
                state.ToAddBrowser.DisplayShowAllGUI = true;
            }
            allowedTypes[elementType] = allowed?.ToList();
            compatibleTypes[elementType] = all.ToList();
        }

        return state;
    }
    public static AddItemState CreateArrayElement(object parent, FieldInfo info, object @object, int index, PatchOperation wouldBePatch, PatchToolTabUI ui, string path) {
        Type elementType = null;
        Type type = @object.GetType() ?? info.FieldType;
        if (type.IsArray) {
            elementType = type.GetElementType();
        } else {
            try {
                elementType = type.GetInterfaces()?.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>).GetGenericArguments()?[0]);
                elementType ??= type.GetGenericArguments()?[0];
            } catch (Exception ex) {
                Mod.Log($"Error while trying to create AddItemProcess:\n{ex.ToString()}");
            }
        }
        if (elementType == null) {
            Mod.Log($"Error while trying to create AddItemProcess:\nCan't find element type for type {type}");
            return null;
        }
        var state = new AddItemState() {
            Parent = parent,
            Info = info,
            Index = index,
            ElementType = elementType,
            Collection = @object,
            Item = null,
            IsExpanded = true,
            WouldBePatch = wouldBePatch,
            Path = path
        };
        state.confirmAction = (Type t) => {
            PatchOperation op = new(PatchOperation.PatchOperationType.ModifyCollection, state.Info.Name, t, null, state.Parent.GetType(), PatchOperation.CollectionPatchOperationType.AddAtIndex, state.Index);
            ui.CurrentState.AddOp(state.WouldBePatch.AddOperation(op));
            ui.CurrentState.CreateAndRegisterPatch();
            ui.addItemStates.Remove(state.Path);
        };
        ui.addItemStates[path] = state;

        if (!compatibleTypes.ContainsKey(elementType)) {
            (var all, var allowed) = PatchToolUtils.GetInstantiableTypes(elementType, parent);
            if (allowed != null) {
                state.ToAddBrowser.DisplayShowAllGUI = true;
            }
            allowedTypes[elementType] = allowed?.ToList();
            compatibleTypes[elementType] = all.ToList();
        }

        return state;
    }
    public void AddItemGUI() {
        using (VerticalScope()) {
            ToAddBrowser.OnGUI(allowedTypes[ElementType] ?? compatibleTypes[ElementType], () => compatibleTypes[ElementType], d => d, t => $"{t.ToString()}", t => [$"{t.ToString()} {t.Name}"], null,
                (type, maybeType) => {
                    string generics = "";
                    if (type.IsGenericType) {
                        generics = type.GetGenericArguments().Select(t => t.Name).ToContentString().Replace("\"", "");
                    }
                    Label($"{type.Name}{generics}", Width(500));
                    Space(200);
                    ActionButton("Add as new entry".localize(), () => {
                        Confirm(type);
                    });
                }
            );
        }
    }
    public void Confirm(Type type) {
        confirmAction(type);
    }
    public object Parent;
    public FieldInfo Info;
    public int Index;
    public object Collection;
    public Type ElementType;
    public object Item;
    public bool IsExpanded;
    public PatchOperation WouldBePatch;
    public string Path;
}