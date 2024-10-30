using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using ModKit.Utility.Extensions;
using Owlcat.Runtime.Visual.Overrides.HBAO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToyBox.PatchTool; 
public static class PatchToolUI {
    public static PatchState CurrentState;
    private static Dictionary<(object, FieldInfo), object> _editStates = new();
    private static Dictionary<object, Dictionary<FieldInfo, object>> _fieldsByObject = new();
    // key: parent, containing field, object instance
    private static Dictionary<(object, FieldInfo, object), bool> _toggleStates = new();
    private static Dictionary<((object, FieldInfo), int), bool> _listToggleStates = new();
    private static HashSet<object> _visited = new();
    // private static string _target = "649ae43543fd4b47ae09a6547e67bcfc";
    private static string _target = "";
    private static string _pickerText = "";
    public static int IndentPerLevel = 25;
    private static readonly HashSet<Type> _primitiveTypes = [typeof(string), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double)];
    public static void SetTarget(string guid) {
        CurrentState = null;
        ClearCache();
        _target = guid;
    }
    public static void OnGUI() {
        _visited.Clear();
        using (HorizontalScope()) {
            Label("Enter target blueprint id", Width(200));
            TextField(ref _pickerText, null, Width(350));
            ActionButton("Pick Blueprint", () => {
                SetTarget(_pickerText);
            });
        }
        Div();
        if (CurrentState == null || CurrentState.IsDirty && !_target.IsNullOrEmpty()) {
            if (Event.current.type == EventType.Layout) {
                ClearCache();
                var bp = ResourcesLibrary.TryGetBlueprint(_target);
                if (bp != null) {
                    CurrentState = new(bp);
                }
            }
        }
        if (CurrentState != null) {
            using (HorizontalScope()) {
                Space(-IndentPerLevel);
                NestedGUI(CurrentState.Blueprint);
            }
        }
    }
    public static void ClearCache() {
        _editStates.Clear();
        _fieldsByObject.Clear();
        _toggleStates.Clear();
    }

    public static void NestedGUI(object o, PatchOperation wouldBePatch = null) {
        if (_visited.Contains(o)) {
            Label("Already opened on another level!".Green());
            return;
        }
        _visited.Add(o);
        if (!_fieldsByObject.ContainsKey(o)) {
            PopulateFieldsAndObjects(o);
        }
        using (VerticalScope()) {
            foreach (var field in _fieldsByObject[o]) {
                using (HorizontalScope()) {
                    bool isEnum = typeof(Enum).IsAssignableFrom(field.Key.FieldType);
                    string generics = "";
                    if (field.Key.FieldType.IsGenericType) {
                        generics = field.Key.FieldType.GetGenericArguments().ToContentString();
                    }
                    Space(IndentPerLevel);
                    Label($"{field.Key.Name} ({(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})", Width(500));
                    FieldGUI(o, wouldBePatch, field.Key.FieldType, field.Value, field.Key);
                }
            }
        }
    }
    public static void FieldGUI(object parent, PatchOperation wouldBePatch, Type type, object @object, FieldInfo info) {
        if (@object == null) {
            Label("Null", Width(500));
            return;
        }
        if (typeof(Enum).IsAssignableFrom(type)) {
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            Label(@object.ToString(), Width(500));
            DisclosureToggle("Show Values", ref state, 800);
            Space(-800);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                using (VerticalScope()) {
                    Label("");
                    using (HorizontalScope()) {
                        if (!_editStates.TryGetValue((parent, info), out var curValue)) {
                            curValue = 0;
                        }
                        var vals = Enum.GetValues(type).Cast<object>();
                        var enumNames = vals.Select(val => val.ToString()).ToArray();
                        var tmp = (int)curValue;
                        var cellsPerRow = Math.Min(4, enumNames.Length);
                        SelectionGrid(ref tmp, enumNames, cellsPerRow, Width(200 * cellsPerRow));
                        _editStates[(parent, info)] = tmp;
                        Space(20);
                        ActionButton("Change", () => {
                            PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, Enum.Parse(type, enumNames[tmp]), parent.GetType());
                            PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                            CurrentState.AddOp(op);
                            CurrentState.CreatePatchFromState().RegisterPatch();
                        });
                    }
                }
            }
        } else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
            Label(@object.ToString(), Width(500));
            Label("Unity Object");
        } else if (typeof(BlueprintReferenceBase).IsAssignableFrom(type)) {
            Label(@object.ToString(), Width(500));
            Label("Reference");
        } else if (_primitiveTypes.Contains(type)) {
            Label(@object.ToString(), Width(500));
            if (!_editStates.TryGetValue((parent, info), out var curValue)) {
                curValue = "";
            }
            string tmp = (string)curValue;
            TextField(ref tmp, null, Width(300));
            _editStates[(parent, info)] = tmp;
            Space(20);
            ActionButton("Change", () => {
                object result = null;
                if (type == typeof(string)) {
                    result = tmp;
                } else {
                    var method = AccessTools.Method(type, "TryParse", [typeof(string), type.MakeByRefType()]);
                    object[] parameters = [tmp, Activator.CreateInstance(type)];
                    bool success = (bool)(method?.Invoke(null, parameters) ?? false);
                    if (success) {
                        result = parameters[1];
                    } else {
                        Space(20);
                        Label($"Failed to parse value {tmp} to type {type.Name}".Orange());
                    }
                }
                if (result != null) {
                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, result, parent.GetType());
                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState.AddOp(op);
                    CurrentState.CreatePatchFromState().RegisterPatch();
                }
            });
        } else if (PatchToolUtils.IsListOrArray(type)) {
            int elementCount = 0;
            if (type.IsArray) {
                Array array = @object as Array;
                elementCount = array.Length;
            } else {
                IList list = @object as IList;
                elementCount = list.Count;
            }
            Label($"{elementCount} Elements", Width(500));
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            DisclosureToggle("Show Elements", ref state, 200);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                int index = 0;
                Space(-1200);
                using (VerticalScope()) {
                    Label("");
                    foreach (var elem in @object as IEnumerable) {
                        ListItemGUI(wouldBePatch, parent, info, elem, index);
                        index += 1;
                    }
                }
            }
        } else {
            Label(@object.ToString(), Width(500));
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            DisclosureToggle("Show fields", ref state, 200);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyComplex, info.Name, null, null, parent.GetType());
                PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                Space(-1200);
                using (VerticalScope()) {
                    Label("");
                    NestedGUI(@object, op);
                }
            }
        }
    }
    public static void ListItemGUI(PatchOperation wouldBePatch, object parent, FieldInfo info, object elem, int index) {
        PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyCollection, info.Name, null, null, parent.GetType(), PatchOperation.CollectionPatchOperationType.ModifyAtIndex, index);
        PatchOperation op = wouldBePatch.AddOperation(tmpOp);
        using (HorizontalScope()) {
            Space(-13);
            Label($"[{index}]", Width(500));
            FieldGUI(parent, op, elem.GetType(), elem, info);
        }
    }

    public static void PopulateFieldsAndObjects(object o) {
        Dictionary<FieldInfo, object> result = new();
        foreach (var field in PatchToolUtils.GetFields(o.GetType())) {
            result[field] = field.GetValue(o);
        }
        _fieldsByObject[o] = result;
    }
}
