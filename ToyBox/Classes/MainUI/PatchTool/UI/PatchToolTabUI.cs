using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
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
    private Dictionary<(object, FieldInfo), BlueprintPickerGUI> pickerGUIs = new();
    private Dictionary<(object, FieldInfo, int), object> editStates = new();
    private Dictionary<object, Dictionary<FieldInfo, object>> fieldsByObject = new();
    // key: parent, containing field, object instance
    private Dictionary<(object, FieldInfo, int), bool> toggleStates = new();
    private Dictionary<((object, FieldInfo), int), bool> listToggleStates = new();
    private Dictionary<(object, FieldInfo), AddItemState> addItemStates = new();
    private static Dictionary<Type, List<Type>> compatibleTypes = new();
    private static Dictionary<Type, List<Type>> allowedTypes = new();
    private HashSet<object> visited = new();
    private bool showBlueprintPicker = false;
    private bool showPatchManager = false;
    private bool showFieldsEditor = false;
    internal string Target = "";
    public int IndentPerLevel = 25;
    private static readonly HashSet<Type> primitiveTypes = new() { typeof(string), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) };
    public class AddItemState {
        public Browser<Type, Type> ToAddBrowser = new(true, true, false, false) { DisplayShowAllGUI = false };
        public static AddItemState Create(object parent, FieldInfo info, object @object, int index, PatchOperation wouldBePatch, PatchToolTabUI ui) {
            Type elementType = null;
            Type type = info.FieldType;
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
                ui = ui
            };
            ui.addItemStates[(parent, info)] = state;

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
                ToAddBrowser.OnGUI(allowedTypes[ElementType] ?? compatibleTypes[ElementType], () => compatibleTypes[ElementType], d => d, t => $"{t.Name}", t => [$"{t.Name}"], null,
                    (type, maybeType) => {
                        Label(type.Name, Width(400));
                        Space(200);
                        ActionButton("Add as new entry".localize(), () => {
                            Confirm(type);
                        });
                    }
                );
            }
        }
        public void Confirm(Type type) {
            PatchOperation op = new(PatchOperation.PatchOperationType.ModifyCollection, Info.Name, type, null, Parent.GetType(), PatchOperation.CollectionPatchOperationType.AddAtIndex, Index);
            ui.CurrentState.AddOp(WouldBePatch.AddOperation(op));
            ui.CurrentState.CreateAndRegisterPatch();
            ui.addItemStates.Remove((Parent, Info));
        }
        public object Parent;
        private PatchToolTabUI ui;
        public FieldInfo Info;
        public int Index;
        public object Collection;
        public Type ElementType;
        public object Item;
        public bool IsExpanded;
        public PatchOperation WouldBePatch;
    }
    public PatchToolTabUI() {
        pickerGUIs[(this, null)] = new();
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
            pickerGUIs[(this, null)].OnGUI(SetTarget);
        }
        if ((CurrentState == null || CurrentState.IsDirty) && !Target.IsNullOrEmpty()) {
            if (Event.current.type == EventType.Layout) {
                ClearCache();
                var bp = ResourcesLibrary.TryGetBlueprint(Target);
                if (bp != null) {
                    CurrentState = new(bp);
                }
            }
        }
        if (CurrentState != null) {
            Space(15);
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
    public void ClearCache() {
        pickerGUIs.Clear();
        pickerGUIs[(this, null)] = new();
        editStates.Clear();
        fieldsByObject.Clear();
        toggleStates.Clear();
        addItemStates.Clear();
        listToggleStates.Clear();
        compatibleTypes.Clear();
        allowedTypes.Clear();
    }
    private void NestedGUI(object o, PatchOperation wouldBePatch = null) {
        if (visited.Contains(o)) {
            Label("Already opened on another level!".localize().Green());
            return;
        }
        visited.Add(o);
        if (!fieldsByObject.ContainsKey(o)) {
            PopulateFieldsAndObjects(o);
        }
        using (VerticalScope()) {
            foreach (var field in fieldsByObject[o]) {
                using (HorizontalScope()) {
                    if (ShouldDisplayField(field.Key.FieldType)) {
                        bool isEnum = typeof(Enum).IsAssignableFrom(field.Key.FieldType);
                        bool isFlagEnum = field.Key.FieldType.IsDefined(typeof(FlagsAttribute), false);
                        string generics = "";
                        if (field.Key.FieldType.IsGenericType) {
                            generics = field.Key.FieldType.GetGenericArguments().ToContentString();
                        }
                        Space(IndentPerLevel);
                        if (toggleStates.TryGetValue((o, field.Key, -1), out var shouldPaint) && shouldPaint) {
                            Label($"{field.Key.Name} ({(isFlagEnum ? "Flag " : "")}{(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})".Cyan(), Width(500));
                        } else {
                            Label($"{field.Key.Name} ({(isFlagEnum ? "Flag " : "")}{(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})", Width(500));
                        }
                        FieldGUI(o, wouldBePatch, field.Key.FieldType, field.Value, field.Key);
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
    private void FieldGUI(object parent, PatchOperation wouldBePatch, Type type, object @object, FieldInfo info, int index = -1) {
        if (typeof(Enum).IsAssignableFrom(type)) {
            var isFlagEnum = type.IsDefined(typeof(FlagsAttribute), false);
            if (!toggleStates.TryGetValue((parent, info, index), out var state)) {
                state = false;
            }
                if (state) {
                Label(@object.ToString().Cyan(), Width(500));
            } else {
                Label(@object.ToString(), Width(500));
            }
            DisclosureToggle("Show Values".localize(), ref state, 800);
            Space(-800);
            toggleStates[(parent, info, index)] = state;
            if (state) {
                using (VerticalScope()) {
                    Label("");
                    using (HorizontalScope()) {
                        if (!editStates.TryGetValue((parent, info, index), out var curValue)) {
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
                                editStates[(parent, info, index)] = tmp;
                                ActionButton("Change".localize(), () => {
                                    var underlyingType = Enum.GetUnderlyingType(type);
                                    var convertedValue = Convert.ChangeType(tmp, underlyingType);
                                    var newValue = Enum.ToObject(type, convertedValue);
                                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, newValue, parent.GetType());
                                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                                    CurrentState.AddOp(op);
                                    CurrentState.CreateAndRegisterPatch();
                                });
                            }
                        } else {
                            var tmp = (int)curValue;
                            SelectionGrid(ref tmp, enumNames, cellsPerRow, Width(200 * cellsPerRow));
                            editStates[(parent, info, index)] = tmp;
                            Space(20);
                            ActionButton("Change".localize(), () => {
                                PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, Enum.Parse(type, enumNames[tmp]), parent.GetType());
                                PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                                CurrentState.AddOp(op);
                                CurrentState.CreateAndRegisterPatch();
                            });
                        }
                    }
                }
            }
        } else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
            if (@object == null) {
                Label("Null", Width(500));
                return;
            }
            Label(@object.ToString(), Width(500));
            Label("Unity Object".localize());
        } else if (typeof(BlueprintReferenceBase).IsAssignableFrom(type)) {
            if (!toggleStates.TryGetValue((parent, info, index), out var state)) {
                state = false;
            }
            var guid = (@object as BlueprintReferenceBase)?.Guid;
            if (guid.IsNullOrEmpty()) guid = "Null or Empty Reference";
            if (state) {
                Label(guid.Cyan(), Width(500));
            } else {
                Label(guid, Width(500));
            }
            DisclosureToggle("Edit Reference".localize(), ref state, 200);
            toggleStates[(parent, info, index)] = state;
            if (state) {
                if (!pickerGUIs.TryGetValue((parent, info), out var gui)) {
                    gui = new();
                    pickerGUIs[(parent, info)] = gui;
                }
                var t = PatchToolUtils.GetBlueprintReferenceKind(type);
                if (t != null) {
                    Space(-1200);
                    using (VerticalScope()) {
                        Label("");
                        gui.OnGUI(newGuid => {
                            PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyBlueprintReference, info.Name, type, newGuid, parent.GetType());
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
                toggleStates[(parent, info, index)] = true;
            } else {
                Label(@object?.ToString() ?? "<Field is null>", Width(500));
                toggleStates[(parent, info, index)] = false;
            }
            if (!editStates.TryGetValue((parent, info, index), out var curValue)) {
                curValue = "";
            }
            string tmp = (string)curValue;
            TextField(ref tmp, n, Width(300));
            editStates[(parent, info, index)] = tmp;
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
                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, result, parent.GetType());
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
            if (!toggleStates.TryGetValue((parent, info, index), out var state)) {
                state = false;
            }
            int elementCount = 0;
            if (type.IsArray) {
                Array array = @object as Array;
                elementCount = array.Length;
            } else {
                IList list = @object as IList;
                elementCount = list.Count;
            }
            if (state) {
                Label(($"{elementCount} " + "Entries".localize()).Cyan(), Width(500));
            } else {
                Label($"{elementCount} " + "Entries".localize(), Width(500));
            }
            DisclosureToggle("Show Entries".localize(), ref state, 200);
            toggleStates[(parent, info, index)] = state;
            if (state) {
                int localIndex = 0;
                Space(-1200);
                using (VerticalScope()) {
                    Label("");
                    foreach (var elem in @object as IEnumerable) {
                        ListItemGUI(wouldBePatch, parent, info, elem, localIndex, @object);
                        localIndex += 1;
                    }
                    using (HorizontalScope()) {
                        Space(1220);
                        ActionButton("Add Item".localize(), () => {
                            AddItemState.Create(parent, info, @object, -1, wouldBePatch, this);
                        });
                    }
                    if (addItemStates.TryGetValue((parent, info), out var activeAddItemState)) {
                        Label("New Item:".localize(), Width(500));
                        activeAddItemState.AddItemGUI();
                    }
                }
            }
        } else {
            if (@object == null) {
                Label("Null", Width(500));
                return;
            }
            if (!toggleStates.TryGetValue((parent, info, index), out var state)) {
                state = false;
            }
            if (state) {
                Label(@object.ToString().Cyan(), Width(500));
            } else {
                Label(@object.ToString(), Width(500));
            }
            DisclosureToggle("Show fields".localize(), ref state, 200);
            toggleStates[(parent, info, index)] = state;
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
    private void ListItemGUI(PatchOperation wouldBePatch, object parent, FieldInfo info, object elem, int index, object collection) {
        PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyCollection, info.Name, null, null, parent.GetType(), PatchOperation.CollectionPatchOperationType.ModifyAtIndex, index);
        PatchOperation op = wouldBePatch.AddOperation(tmpOp);
        using (HorizontalScope()) {
            Space(-13);
            if (toggleStates.TryGetValue((parent, info, index), out var shouldPaint) && shouldPaint) {
                Label($"[{index}] ({elem?.GetType().Name ?? "Null"})".Cyan(), Width(500));
            } else {
                Label($"[{index}] ({elem?.GetType().Name ?? "Null"})", Width(500));
            }
            FieldGUI(parent, op, elem.GetType(), elem, info, index);

            Space(20);
            ActionButton("Add Before".localize(), () => {
                AddItemState.Create(parent, info, collection, index, wouldBePatch, this);
            });
            Space(10);
            ActionButton("Add After".localize(), () => {
                AddItemState.Create(parent, info, collection, index+1, wouldBePatch, this);
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
    private void PopulateFieldsAndObjects(object o) {
        Dictionary<FieldInfo, object> result = new();
        foreach (var field in PatchToolUtils.GetFields(o.GetType())) {
            result[field] = field.GetValue(o);
        }
        fieldsByObject[o] = result;
    }
}
