using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace ToyBox.PatchTool;
public class PatchToolTabUI {
    public PatchState CurrentState;
    private Dictionary<string, BlueprintPickerGUI> pickerGUIs = new();
    private Dictionary<string, object> editStates = new();
    private Dictionary<string, Dictionary<FieldInfo, object>> fieldsByObject = new();
    private Dictionary<string, bool> toggleStates = new();
    private Dictionary<string, bool> listToggleStates = new();
    internal Dictionary<string, AddItemState> addItemStates = new();
    private HashSet<object> visited = new();
    private bool showBlueprintPicker = false;
    private bool showPatchManager = false;
    private bool showFieldsEditor = false;
    internal string Target = "";
    public int IndentPerLevel = 25;
    internal static readonly HashSet<Type> primitiveTypes = new() { typeof(string), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort) };
    public PatchToolTabUI() {
        pickerGUIs[""] = new();
    }
    public PatchToolTabUI(string guid) : this() {
        Target = guid;
    }
    public void SetTarget(string guid) {
        CurrentState = null;
        ClearCache();
        Target = guid;
        showBlueprintPicker = false;
    }
    public void OnGUI() {
        visited.Clear();
        DisclosureToggle("Show Blueprint Picker", ref showBlueprintPicker);
        if (showBlueprintPicker) {
            pickerGUIs[""].OnGUI(SetTarget);
        }
        if ((CurrentState == null || CurrentState.IsDirty) && !Target.IsNullOrEmpty()) {
            if (Event.current.type == EventType.Layout) {
                ClearCache(Main.Settings.togglePatchToolCollapseAllPathsOnPatch);
                var bp = ResourcesLibrary.TryGetBlueprint(Target);
                if (bp != null) {
                    CurrentState = new(bp);
                }
            }
        }
        if (CurrentState != null) {
            Space(15);
            #region PatchManager
            Div();
            Space(15);
            DisclosureToggle("Show Patch Manager".localize(), ref showPatchManager);
            if (showPatchManager) {
                using (HorizontalScope()) {
                    Space(20);
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Label($"Current Patch targets bp: {BlueprintExtensions.GetTitle(CurrentState.Blueprint).Cyan()}({CurrentState.Blueprint.name ?? CurrentState.Blueprint.AssetGuid}) and has {CurrentState.Operations.Count.ToString().Cyan()} operations.");
                            Space(30);
                            using (VerticalScope()) {
                                int count = 0;
                                foreach (var op in CurrentState.Operations.ToList()) {
                                    count++;
                                    using (HorizontalScope()) {
                                        Label($"Operation: {op.OperationType}", Width(200));
                                        Space(20);
                                        Label($"Field: {op.FieldName}", Width(300));
                                        Space(20);
                                        ReflectionTreeView.DetailToggle("Inspect".localize(), op, op, 0);
                                        Space(20);
                                        if (count == CurrentState.Operations.Count) {
                                            ActionButton("Remove".localize(), () => {
                                                CurrentState.Operations.Remove(op);
                                            });
                                        }
                                    }
                                    ReflectionTreeView.OnDetailGUI(op);
                                }
                            }
                        }
                        Space(10);
                        ActionButton("Apply Changes".localize(), () => {
                            CurrentState.CreateAndRegisterPatch();
                        });
                    }
                }
            }
            Space(15);
            #endregion
            #region Settings
            Div();
            Space(15);
            Label("Configure which types of fields to show:".localize());
            using (HorizontalScope()) {
                Toggle("Primitives".localize(), ref Main.Settings.showPatchToolPrimitiveTypes);
                Space(10);
                Toggle("Enums".localize(), ref Main.Settings.showPatchToolEnums);
                Space(10);
                Toggle("Blueprint References".localize(), ref Main.Settings.showPatchToolBlueprintReferences);
                Space(10);
                Toggle("Collections".localize(), ref Main.Settings.showPatchToolCollections);
                Space(10);
                Toggle("Complex Types".localize(), ref Main.Settings.showPatchToolComplexTypes);
                Space(10);
                Toggle("Show Unity Objects".localize(), ref Main.Settings.showPatchToolUnityObjects);
            }

            Space(15);
            Label("Other Settings:".localize());
            using (HorizontalScope()) {
                Toggle("Show Delete Button".localize(), ref Main.Settings.showPatchToolDeleteButtons);
                Space(10);
                Toggle("Show Create Button".localize(), ref Main.Settings.showPatchToolCreateButtons);
                Space(10);
                Toggle("Close all opened fields on patch".localize(), ref Main.Settings.togglePatchToolCollapseAllPathsOnPatch);
                Space(10);
                if (!CurrentState.DangerousOperationsEnabled && Main.Settings.toggleEnableDangerousPatchToolPatches) {
                    ActionButton("Enable Dangerous Patches for this Patch".localize(), () => {
                        CurrentState.DangerousOperationsEnabled = true;
                    });
                }
            }
            Space(15);
            #endregion
            Div();
            Space(15);
            DisclosureToggle("Show Fields Editor".localize(), ref showFieldsEditor);
            if (showFieldsEditor) {
                using (HorizontalScope()) {
                    Space(20);
                    Space(-IndentPerLevel);
                    NestedGUI(CurrentState.Blueprint);
                }
            }
        }
    }
    public void ClearCache(bool resetToggleStates = true) {
        pickerGUIs.Clear();
        pickerGUIs[""] = new();
        editStates.Clear();
        fieldsByObject.Clear();
        addItemStates.Clear();
        if (resetToggleStates) {
            toggleStates.Clear();
            listToggleStates.Clear();
        }
        AddItemState.compatibleTypes.Clear();
        AddItemState.allowedTypes.Clear();
    }
    #region PerField
    private void NestedGUI(object o, string path = "", PatchOperation wouldBePatch = null, Type overridenType = null) {
        if (visited.Contains(o)) {
            if (!(o?.GetType()?.IsValueType ?? false)) {
                Label("Already opened on another level!".localize().Green());
                return;
            }
        } else {
            visited.Add(o);
        }
        Dictionary<FieldInfo, object> fbo;
        var oType = o?.GetType();
        Type type = overridenType ?? oType;
        if (oType != null && overridenType != null && overridenType.IsAssignableFrom(oType)) {
            type = oType;
        }
        if (!fieldsByObject.ContainsKey(path)) {
            PopulateFieldsAndObjects(o, path, type);
        }
        fbo = fieldsByObject[path];
        using (VerticalScope()) {
            foreach (var field in fbo) {
                var path2 = path + field.Key.Name;
                using (HorizontalScope()) {
                    if (ShouldDisplayField(field.Key.FieldType)) {
                        bool isEnum = typeof(Enum).IsAssignableFrom(field.Key.FieldType);
                        bool isFlagEnum = field.Key.FieldType.IsDefined(typeof(FlagsAttribute), false);
                        string generics = "";
                        if (field.Key.FieldType.IsGenericType) {
                            generics = field.Key.FieldType.GetGenericArguments().Select(t => t.Name).ToContentString().Replace("\"", "");
                        }
                        Space(IndentPerLevel);
                        if (Main.Settings.showPatchToolDeleteButtons && field.Value != null) {
                            using (HorizontalScope(Width(100))) {
                                ActionButton("Delete".localize().Red().Bold(), () => {
                                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.NullField, field.Key.Name, field.Key.FieldType, null, type);
                                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                                    CurrentState.AddOp(op);
                                    CurrentState.CreateAndRegisterPatch();
                                }, AutoWidth());
                            }
                        } else if (Main.Settings.showPatchToolCreateButtons && field.Value == null && !PatchToolUtils.TypeOrBaseIsDirectlyInUnityDLL(field.Key.FieldType)) {
                            using (HorizontalScope(Width(100))) {
                                ActionButton("Create".localize().Green().Bold(), () => {
                                    AddItemState.CreateComplexOrList(o, field.Key, wouldBePatch, this, path2 + "2");
                                }, AutoWidth());
                            }
                        }
                        if (toggleStates.TryGetValue(path2, out var shouldPaint) && shouldPaint) {
                            Label($"{field.Key.Name} ({(isFlagEnum ? "Flag " : "")}{(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})".Cyan(), Width(500));
                        } else {
                            Label($"{field.Key.Name} ({(isFlagEnum ? "Flag " : "")}{(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})", Width(500));
                        }
                        FieldGUI(o, type, wouldBePatch, field.Key.FieldType, field.Value, field.Key, path2);
                    }
                }
                if (addItemStates.TryGetValue(path2 + "2", out var activeAddItemState)) {
                    using (HorizontalScope()) {
                        Label("New Item:".localize(), Width(500));
                        activeAddItemState.AddItemGUI();
                    }
                }
            }
        }
    }
    private bool ShouldDisplayField(Type fieldType) {
        if (primitiveTypes.Contains(fieldType)) {
            return Main.Settings.showPatchToolPrimitiveTypes;
        } else if (typeof(Enum).IsAssignableFrom(fieldType)) {
            return Main.Settings.showPatchToolEnums;
        } else if (typeof(BlueprintReferenceBase).IsAssignableFrom(fieldType)) {
            return Main.Settings.showPatchToolBlueprintReferences;
        } else if (PatchToolUtils.IsListOrArray(fieldType)) {
            return Main.Settings.showPatchToolCollections;
        } else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) {
            return Main.Settings.showPatchToolUnityObjects;
        } else {
            return Main.Settings.showPatchToolComplexTypes;
        }
    }
    #endregion
    #region PerRow
    private void FieldGUI(object parent, Type parentType, PatchOperation wouldBePatch, Type type, object @object, FieldInfo info, string path) {
        if (typeof(Enum).IsAssignableFrom(type)) {
            var isFlagEnum = type.IsDefined(typeof(FlagsAttribute), false);
            if (!toggleStates.TryGetValue(path, out var state)) {
                state = false;
            }
            if (state) {
                Label(@object.ToString().Cyan(), Width(500));
            } else {
                Label(@object.ToString(), Width(500));
            }
            DisclosureToggle("Show Values".localize(), ref state, 800);
            Space(-800);
            toggleStates[path] = state;
            if (state) {
                using (VerticalScope()) {
                    Label("");
                    using (HorizontalScope()) {
                        if (!editStates.TryGetValue(path, out var curValue)) {
                            if (isFlagEnum) curValue = Convert.ChangeType(@object, Enum.GetUnderlyingType(type));
                            else curValue = 0;
                        }
                        var vals = Enum.GetValues(type).Cast<object>();
                        var enumNames = vals.Select(val => val.ToString()).ToArray();
                        var enumValues = vals.Select(Convert.ToInt64).ToArray();
                        var cellsPerRow = Math.Min(4, enumNames.Length);
                        if (isFlagEnum) {
                            var tmp = Convert.ToInt64(curValue);
                            int totalFlags = vals.Count();
                            int rows = (totalFlags + cellsPerRow - 1) / cellsPerRow;

                            using (VerticalScope()) {
                                int flagIndex = 0;
                                for (int row = 0; row < rows; row++) {
                                    using (HorizontalScope()) {
                                        for (int col = 0; col < cellsPerRow && flagIndex < totalFlags; col++, flagIndex++) {
                                            var flagName = enumNames[flagIndex];
                                            var flagValue = enumValues[flagIndex];
                                            var isSet = (tmp & flagValue) != 0;
                                            bool newIsSet = isSet;

                                            Toggle(flagName, ref newIsSet, Width(200));

                                            if (newIsSet != isSet) {
                                                if (newIsSet) {
                                                    tmp |= flagValue;
                                                } else {
                                                    tmp &= ~flagValue;
                                                }
                                            }
                                        }
                                    }
                                }
                                editStates[path] = tmp;
                                ActionButton("Change".localize(), () => {
                                    var underlyingType = Enum.GetUnderlyingType(type);
                                    var convertedValue = Convert.ChangeType(tmp, underlyingType);
                                    var newValue = Enum.ToObject(type, convertedValue);
                                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, newValue, parentType);
                                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                                    CurrentState.AddOp(op);
                                    CurrentState.CreateAndRegisterPatch();
                                });
                            }
                        } else {
                            var tmp = (int)curValue;
                            SelectionGrid(ref tmp, enumNames, cellsPerRow, Width(200 * cellsPerRow));
                            editStates[path] = tmp;
                            Space(20);
                            ActionButton("Change".localize(), () => {
                                PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, Enum.Parse(type, enumNames[tmp]), parentType);
                                PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                                CurrentState.AddOp(op);
                                CurrentState.CreateAndRegisterPatch();
                            });
                        }
                    }
                }
            }
        } else if (PatchToolUtils.TypeOrBaseIsDirectlyInUnityDLL(type) && type != typeof(SharedStringAsset) && !CurrentState.DangerousOperationsEnabled) {
            if (@object == null) {
                Label("Null", Width(500));
                return;
            }
            string label;
            try {
                label = @object.ToString();
            } catch (Exception ex) {
                Mod.Trace($"Error in FieldGUI ToString for field {info.Name}:\n{ex.ToString()}");
                label = "Exception in ToString".Orange();
            }
            Label(label, Width(500));
            Label("Unity Object".localize());
        } else if (typeof(BlueprintReferenceBase).IsAssignableFrom(type)) {
            if (!toggleStates.TryGetValue(path, out var state)) {
                state = false;
            }
            var label = (@object as BlueprintReferenceBase)?.Guid?.ToString();
            if (label.IsNullOrEmpty()) label = "Null or Empty Reference";
            else {
                var bp = (@object as BlueprintReferenceBase)?.GetBlueprint();
                if (bp != null) {
                    label = BlueprintExtensions.GetTitle(bp) + $" ({label})";
                } else {
                    label = "Invalid Reference".Orange() + $" ({label})";
                }
            }
            if (state) {
                Label(label.Cyan(), Width(500));
            } else {
                Label(label, Width(500));
            }
            DisclosureToggle("Edit Reference".localize(), ref state, 200);
            toggleStates[path] = state;
            if (state) {
                if (!pickerGUIs.TryGetValue(path, out var gui)) {
                    gui = new();
                    pickerGUIs[path] = gui;
                }
                var t = PatchToolUtils.GetBlueprintReferenceKind(type);
                if (t != null) {
                    Space(-1200);
                    using (VerticalScope()) {
                        Label("");
                        gui.OnGUI(newGuid => {
                            PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyBlueprintReference, info.Name, type, newGuid, parentType);
                            PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                            CurrentState.AddOp(op);
                            CurrentState.CreateAndRegisterPatch();
                        }, t);
                    }
                } else {
                    Label("Non-Generic Reference. If you're seeing this please report to mod author!".Yellow().Bold());
                }
            }
        } else if (primitiveTypes.Contains(type)) {
            var n = $"{RuntimeHelpers.GetHashCode(this)}{RuntimeHelpers.GetHashCode(parent)}{RuntimeHelpers.GetHashCode(info)}{RuntimeHelpers.GetHashCode(@object)}";
            if (GUI.GetNameOfFocusedControl() == n) {
                Label((@object?.ToString() ?? "<Field is null>").Cyan(), Width(500));
                toggleStates[path] = true;
            } else {
                Label(@object?.ToString() ?? "<Field is null>", Width(500));
                toggleStates[path] = false;
            }
            if (!editStates.TryGetValue(path, out var curValue)) {
                curValue = "";
            }
            string tmp = (string)curValue;
            TextField(ref tmp, n, Width(300));
            editStates[path] = tmp;
            Space(20);
            ActionButton("Change".localize(), () => {
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
                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, result, parentType);
                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState.AddOp(op);
                    CurrentState.CreateAndRegisterPatch();
                }
            });
        } else if (PatchToolUtils.IsListOrArray(type)) {
            if (@object == null) {
                Label("Null", Width(500));
                return;
            }
            Type defaultType = null;
            if (!toggleStates.TryGetValue(path, out var state)) {
                state = false;
            }
            int elementCount = 0;
            if (type.IsArray) {
                Array array = @object as Array;
                elementCount = array.Length;
                defaultType = type.GetElementType();
            } else {
                IList list = @object as IList;
                if (list != null) {
                    elementCount = list.Count;
                } else {
                    var list2 = @object as IEnumerable<object>;
                    elementCount = list2.Count();
                    defaultType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0]
                                    ?? list2?.NotNull()?.FirstOrDefault()?.GetType();
                }
            }
            if (state) {
                Label(($"{elementCount} " + "Entries".localize()).Cyan(), Width(500));
            } else {
                Label($"{elementCount} " + "Entries".localize(), Width(500));
            }
            DisclosureToggle("Show Entries".localize(), ref state, 200);
            toggleStates[path] = state;
            if (state) {
                int localIndex = 0;
                Space(-1200);
                using (VerticalScope()) {
                    Label("");
                    foreach (var elem in @object as IEnumerable) {
                        ListItemGUI(wouldBePatch, parent, info, elem, localIndex, @object, path, defaultType);
                        localIndex += 1;
                    }
                    using (HorizontalScope()) {
                        Space(1220);
                        ActionButton("Add Item".localize(), () => {
                            AddItemState.CreateArrayElement(parent, info, @object, -1, wouldBePatch, this, path);
                        });
                    }
                    if (addItemStates.TryGetValue(path, out var activeAddItemState)) {
                        Label("New Item:".localize(), Width(500));
                        activeAddItemState.AddItemGUI();
                    }
                }
            }
        } else {
            if (@object == null) {
                if (!type.IsValueType) {
                    Label("Null", Width(500));
                } else {
                    Label("Null (value)", Width(500));
                }
                return;
            }
            if (!toggleStates.TryGetValue(path, out var state)) {
                state = false;
            }
            string label;
            try {
                label = @object?.ToString() ?? "Null (value)";
            } catch (Exception ex) {
                Mod.Trace($"Error in FieldGUI ToString for field {info.Name}:\n{ex.ToString()}");
                label = "Exception in ToString".Orange();
            }
            Label(label, Width(500));
            if (state) {
                Label(label.Cyan(), Width(500));
            } else {
                Label(label, Width(500));
            }
            DisclosureToggle("Show fields".localize(), ref state, 200);
            toggleStates[path] = state;
            if (state) {
                PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyComplex, info.Name, null, null, parentType);
                PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                Space(-1200);
                using (VerticalScope()) {
                    Label("");
                    NestedGUI(@object, path, op, type);
                }
            }
        }
    }
    #endregion
    private void ListItemGUI(PatchOperation wouldBePatch, object parent, FieldInfo info, object elem, int index, object collection, string path, Type defaultType = null) {
        PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyCollection, info.Name, null, null, parent.GetType(), PatchOperation.CollectionPatchOperationType.ModifyAtIndex, index);
        PatchOperation op = wouldBePatch.AddOperation(tmpOp);
        using (HorizontalScope()) {
            Space(-13);
            if (toggleStates.TryGetValue(path, out var shouldPaint) && shouldPaint) {
                Label($"[{index}] ({elem?.GetType().Name ?? "Null"})".Cyan(), Width(500));
            } else {
                Label($"[{index}] ({elem?.GetType().Name ?? "Null"})", Width(500));
            }
            FieldGUI(parent, parent.GetType(), op, elem?.GetType() ?? defaultType, elem, info, path + $"[{index}]");

            Space(20);
            ActionButton("Add Before".localize(), () => {
                AddItemState.CreateArrayElement(parent, info, collection, index, wouldBePatch, this, path);
            });
            Space(10);
            ActionButton("Add After".localize(), () => {
                AddItemState.CreateArrayElement(parent, info, collection, index + 1, wouldBePatch, this, path);
            });
            Space(10);
            ActionButton("Remove".localize(), () => {
                PatchOperation removeOp = new(PatchOperation.PatchOperationType.ModifyCollection, info.Name, null, null, parent.GetType(), PatchOperation.CollectionPatchOperationType.RemoveAtIndex, index);
                PatchOperation opRemove = wouldBePatch.AddOperation(removeOp);
                CurrentState.AddOp(opRemove);
                CurrentState.CreateAndRegisterPatch();
            });
        }
    }
    private void PopulateFieldsAndObjects(object o, string path, Type type) {
        Dictionary<FieldInfo, object> result = new();
        if (PatchToolUtils.IsNullableStruct(type)) {
            foreach (var field in PatchToolUtils.GetFields(type)) {
                if (field.Name == "value") {
                    if (o == null) {
                        result[field] = null;
                    } else {
                        result[field] = field.GetValue(o);
                    }
                }
            }
        } else {
            foreach (var field in PatchToolUtils.GetFields(type)) {
                result[field] = field.GetValue(o);
            }
        }
        if (type == typeof(SharedStringAsset)) {
            var toRemove = result.Where(f => f.Key.Name == "m_CachedPtr").FirstOrDefault().Key;
            if (toRemove != null) {
                result.Remove(toRemove);
            }
        }
        fieldsByObject[path] = result;
    }
}
