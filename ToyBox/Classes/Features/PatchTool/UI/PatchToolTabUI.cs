using Kingmaker.Blueprints;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToyBox.Features.PatchTool.Infrastructure;
using ToyBox.Features.PatchTool.Utils;
using ToyBox.Infrastructure.Inspector;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using static ToyBox.Infrastructure.UI;

namespace ToyBox.Features.PatchTool;

public partial class PatchToolTabUI {
    public PatchState? CurrentState;
    private readonly Dictionary<string, BlueprintPickerGUI> m_PickerGUIs = [];
    private readonly Dictionary<string, object> m_EditStates = [];
    private readonly Dictionary<string, Dictionary<FieldInfo, object?>> m_FieldsByObject = [];
    private readonly Dictionary<string, bool> m_ToggleStates = [];
    private readonly Dictionary<string, bool> m_ListToggleStates = [];
    private readonly Dictionary<string, bool> m_ParseFailed = [];
    private readonly Dictionary<string, Type> m_AssetTypeMismatch = [];
    internal Dictionary<string, AddItemState> AddItemStates = [];
    private readonly HashSet<object> m_Visited = new(new ReferenceEqualityComparer());
    private bool m_ShowBlueprintPicker = false;
    private bool m_ShowPatchManager = false;
    private bool m_ShowFieldsEditor = false;
    internal string Target = "";
    public int IndentPerLevel = 25;
    private static readonly HashSet<Type> m_PrimitiveTypes = [typeof(string), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort)];
    private enum FieldCategory { Enum, Unity, Reference, Primitive, List, Complex }
    public PatchToolTabUI() {
        m_PickerGUIs[""] = new();
    }
    public PatchToolTabUI(string id) : this() {
        Target = id;
    }
    public void SetTarget(string id) {
        CurrentState = null;
        ClearCache();
        Target = id;
        m_ShowBlueprintPicker = false;
    }
    public void OnGUI() {
        m_Visited.Clear();
        _ = DisclosureToggle(ref m_ShowBlueprintPicker, m_ShowBlueprintPickerLocalizedText);
        if (m_ShowBlueprintPicker) {
            m_PickerGUIs[""].OnGUI(SetTarget);
        }
        if ((CurrentState == null || CurrentState.IsDirty) && !Target.IsNullOrEmpty()) {
            if (Event.current.type == EventType.Layout) {
                ClearCache(Settings.CollapseAllPatchToolPathsOnPatch);
                var bp = ResourcesLibrary.TryGetBlueprint(Target);
                if (bp != null) {
                    CurrentState = new(bp);
                }
            }
        }
        if (CurrentState != null) {
            Space(15);
            #region PatchManager
            Div.DrawDiv();
            Space(15);
            _ = DisclosureToggle(ref m_ShowPatchManager, PatchToolStrings.ShowPatchManager);
            if (m_ShowPatchManager) {
                using (HorizontalScope()) {
                    Space(20);
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Label(string.Format(PatchToolStrings.CurrentPatchInfo, BPHelper.GetTitle(CurrentState.Blueprint).Cyan(), CurrentState.Blueprint.name ?? CurrentState.Blueprint.AssetGuid.ToString(), CurrentState.Operations.Count.ToString().Cyan()));
                            Space(30);
                            using (VerticalScope()) {
                                var count = 0;
                                foreach (var op in CurrentState.Operations.ToList()) {
                                    count++;
                                    using (HorizontalScope()) {
                                        Label(string.Format(PatchToolStrings.OperationRow, op.Kind), Width(200));
                                        Space(20);
                                        Label(string.Format(PatchToolStrings.FieldRow, op.FieldName), Width(300));
                                        Space(20);
                                        InspectorUI.InspectToggle(op, PatchToolStrings.Inspect, op, 0);
                                        Space(20);
                                        if (count == CurrentState.Operations.Count) {
                                            _ = Button(PatchToolStrings.Remove, () => {
                                                _ = CurrentState.Operations.Remove(op);
                                            });
                                        }
                                    }
                                    InspectorUI.InspectIfExpanded(op);
                                }
                            }
                        }
                        Space(10);
                        _ = Button(PatchToolStrings.ApplyChanges, () => {
                            CurrentState.CreateAndRegisterPatch();
                        });
                    }
                }
            }
            Space(15);
            #endregion
            #region Settings
            Div.DrawDiv();
            Space(15);
            Label(PatchToolStrings.ConfigureFieldsToShow);
            using (HorizontalScope()) {
                _ = Toggle(PatchToolStrings.Primitives, null, ref Settings.ShowPatchToolPrimitiveTypes);
                Space(10);
                _ = Toggle(PatchToolStrings.Enums, null, ref Settings.ShowPatchToolEnums);
                Space(10);
                _ = Toggle(PatchToolStrings.BlueprintReferences, null, ref Settings.ShowPatchToolBlueprintReferences);
                Space(10);
                _ = Toggle(PatchToolStrings.Collections, null, ref Settings.ShowPatchToolCollections);
                Space(10);
                _ = Toggle(PatchToolStrings.ComplexTypes, null, ref Settings.ShowPatchToolComplexTypes);
                Space(10);
                _ = Toggle(PatchToolStrings.ShowUnityObjects, null, ref Settings.ShowPatchToolUnityObjects);
            }

            Space(15);
            Label(PatchToolStrings.OtherSettings);
            using (HorizontalScope()) {
                _ = Toggle(PatchToolStrings.ShowDeleteButton, null, ref Settings.ShowPatchToolDeleteButtons);
                Space(10);
                _ = Toggle(PatchToolStrings.ShowCreateButton, null, ref Settings.ShowPatchToolCreateButtons);
                Space(10);
                _ = Toggle(PatchToolStrings.CollapseOnPatch, null, ref Settings.CollapseAllPatchToolPathsOnPatch);
                Space(10);
                if (!CurrentState.DangerousOperationsEnabled && Settings.EnableDangerousPatchToolPatches) {
                    _ = Button(PatchToolStrings.EnableDangerousForThisPatch, () => {
                        CurrentState.DangerousOperationsEnabled = true;
                    });
                }
            }
            Space(15);
            #endregion
            Div.DrawDiv();
            Space(15);
            _ = DisclosureToggle(ref m_ShowFieldsEditor, PatchToolStrings.ShowFieldsEditor);
            if (m_ShowFieldsEditor) {
                NestedGUI(CurrentState.Blueprint);
            }
        }
    }
    public void ClearCache(bool resetToggleStates = true) {
        m_PickerGUIs.Clear();
        m_PickerGUIs[""] = new();
        m_EditStates.Clear();
        m_ParseFailed.Clear();
        m_AssetTypeMismatch.Clear();
        m_FieldsByObject.Clear();
        AddItemStates.Clear();
        if (resetToggleStates) {
            m_ToggleStates.Clear();
            m_ListToggleStates.Clear();
        }
        AddItemState.m_CompatibleTypes.Clear();
        AddItemState.m_AllowedTypes.Clear();
    }

    private FieldCategory Categorize(Type type) {
        if (typeof(Enum).IsAssignableFrom(type)) {
            return FieldCategory.Enum;
        }
        if (PatchToolUtils.TypeOrBaseIsDirectlyInUnityDLL(type) && type != typeof(Kingmaker.Localization.SharedStringAsset) && !(CurrentState?.DangerousOperationsEnabled ?? false)) {
            return FieldCategory.Unity;
        }
        if (typeof(BlueprintReferenceBase).IsAssignableFrom(type)) {
            return FieldCategory.Reference;
        }
        if (m_PrimitiveTypes.Contains(type)) {
            return FieldCategory.Primitive;
        }
        if (PatchToolUtils.IsListOrArray(type)) {
            return FieldCategory.List;
        }
        return FieldCategory.Complex;
    }

    #region PerField
    private void NestedGUI(object? o, string path = "", PatchOperation? wouldBePatch = null, Type? overridenType = null) {
        if (m_Visited.Contains(o!)) {
            if (!(o?.GetType()?.IsValueType ?? false)) {
                Label(PatchToolStrings.AlreadyOpenedElsewhere.Green());
                return;
            }
        } else {
            _ = m_Visited.Add(o!);
        }
        var oType = o?.GetType();
        var type = overridenType ?? oType!;
        if (oType != null && overridenType != null && overridenType.IsAssignableFrom(oType)) {
            type = oType;
        }
        if (!m_FieldsByObject.ContainsKey(path)) {
            PopulateFieldsAndObjects(o, path, type);
        }
        var fbo = m_FieldsByObject[path];
        using (VerticalScope()) {
            foreach (var field in fbo) {
                if (!ShouldDisplayField(field.Key.FieldType)) {
                    continue;
                }
                var path2 = path + "/" + field.Key.Name;
                var isEnum = typeof(Enum).IsAssignableFrom(field.Key.FieldType);
                var isFlagEnum = field.Key.FieldType.IsDefined(typeof(FlagsAttribute), false);
                var generics = "";
                if (field.Key.FieldType.IsGenericType) {
                    generics = field.Key.FieldType.GetGenericArguments().Select(t => t.Name).ToContentString().Replace("\"", "");
                }
                using (HorizontalScope()) {
                    Space(IndentPerLevel);
                    if (Settings.ShowPatchToolDeleteButtons && field.Value != null) {
                        using (HorizontalScope(Width(100))) {
                            _ = Button(PatchToolStrings.Delete.Red().Bold(), () => {
                                var tmpOp = new NullFieldOperation(field.Key.Name, type, field.Key.FieldType);
                                var op = wouldBePatch.AddOperation(tmpOp);
                                CurrentState!.AddOp(op);
                                CurrentState.CreateAndRegisterPatch();
                            }, null, AutoWidth());
                        }
                    } else if (Settings.ShowPatchToolCreateButtons && field.Value == null && !PatchToolUtils.TypeOrBaseIsDirectlyInUnityDLL(field.Key.FieldType)) {
                        using (HorizontalScope(Width(100))) {
                            _ = Button(PatchToolStrings.Create.Green().Bold(), () => {
                                _ = AddItemState.CreateComplexOrList(o!, field.Key, wouldBePatch!, this, path2 + "#add");
                            }, null, AutoWidth());
                        }
                    }
                    var fieldLabel = $"{field.Key.Name} ({(isFlagEnum ? "Flag " : "")}{(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name}{generics})";
                    var painted = m_ToggleStates.TryGetValue(path2, out var shouldPaint) && shouldPaint;
                    Label(painted ? fieldLabel.Cyan() : fieldLabel, Width(500));
                    FieldSummaryGUI(o!, type, wouldBePatch, field.Key.FieldType, field.Value, field.Key, path2);
                }
                FieldDetailGUI(o!, type, wouldBePatch, field.Key.FieldType, field.Value, field.Key, path2);
                if (AddItemStates.TryGetValue(path2 + "#add", out var activeAddItemState)) {
                    using (HorizontalScope()) {
                        Space(IndentPerLevel * 2);
                        Label(PatchToolStrings.NewItem, Width(500));
                        activeAddItemState.AddItemGUI();
                    }
                }
            }
            // Gated on dangerous mode: these edits hit the shared prefab globally, and a GameObject can be
            // reached through a non-Unity-typed field (object, an interface) that would otherwise slip past.
            if (o is GameObject go && (CurrentState?.DangerousOperationsEnabled ?? false)) {
                GameObjectMembersGUI(go, path, wouldBePatch);
            }
        }
    }
    // Renders a GameObject's components and transform children as drillable rows (inspector-style). Gated
    // on dangerous mode by the caller.
    private void GameObjectMembersGUI(GameObject go, string path, PatchOperation? wouldBePatch) {
        var components = go.GetComponents<Component>();
        if (components.Length > 0) {
            using (HorizontalScope()) {
                Space(IndentPerLevel);
                Label(PatchToolStrings.ComponentsHeader.Bold(), Width(500));
            }
        }
        var typeCounter = new Dictionary<Type, int>();
        for (var i = 0; i < components.Length; i++) {
            var comp = components[i];
            if (comp == null) {
                continue;
            }
            var componentType = comp.GetType();
            if (!typeCounter.TryGetValue(componentType, out var typeIndex)) {
                typeIndex = 0;
            }
            typeCounter[componentType] = typeIndex + 1;
            var compPath = path + $"/co[{i}]";
            var state = m_ToggleStates.TryGetValue(compPath, out var s) && s;
            using (HorizontalScope()) {
                Space(IndentPerLevel);
                var label = string.Format(PatchToolStrings.ComponentRow, componentType.Name, typeIndex);
                Label(state ? label.Cyan() : label, Width(500));
                _ = DisclosureToggle(ref state, PatchToolStrings.ShowFields, null, null, Width(200));
                m_ToggleStates[compPath] = state;
            }
            if (state) {
                var compOp = wouldBePatch.AddOperation(new ModifyGameObjectComponentOperation(componentType.Name, typeof(GameObject), componentType, typeIndex));
                using (HorizontalScope()) {
                    Space(IndentPerLevel * 2);
                    using (VerticalScope()) {
                        NestedGUI(comp, compPath, compOp, componentType);
                    }
                }
            }
        }
        var transform = go.transform;
        if (transform.childCount > 0) {
            using (HorizontalScope()) {
                Space(IndentPerLevel);
                Label(PatchToolStrings.ChildrenHeader.Bold(), Width(500));
            }
        }
        for (var i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i).gameObject;
            var childPath = path + $"/ci[{i}]";
            var state = m_ToggleStates.TryGetValue(childPath, out var s) && s;
            using (HorizontalScope()) {
                Space(IndentPerLevel);
                var label = string.Format(PatchToolStrings.ChildRow, i, child.name);
                Label(state ? label.Cyan() : label, Width(500));
                _ = DisclosureToggle(ref state, PatchToolStrings.ShowFields, null, null, Width(200));
                m_ToggleStates[childPath] = state;
            }
            if (state) {
                var childOp = wouldBePatch.AddOperation(new ModifyGameObjectChildOperation($"child[{i}]", typeof(GameObject), i, child.name));
                using (HorizontalScope()) {
                    Space(IndentPerLevel * 2);
                    using (VerticalScope()) {
                        NestedGUI(child, childPath, childOp, typeof(GameObject));
                    }
                }
            }
        }
    }
    private bool ShouldDisplayField(Type fieldType) {
        if (m_PrimitiveTypes.Contains(fieldType)) {
            return Settings.ShowPatchToolPrimitiveTypes;
        } else if (typeof(Enum).IsAssignableFrom(fieldType)) {
            return Settings.ShowPatchToolEnums;
        } else if (typeof(BlueprintReferenceBase).IsAssignableFrom(fieldType)) {
            return Settings.ShowPatchToolBlueprintReferences;
        } else if (PatchToolUtils.IsListOrArray(fieldType)) {
            return Settings.ShowPatchToolCollections;
        } else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) {
            return Settings.ShowPatchToolUnityObjects;
        } else {
            return Settings.ShowPatchToolComplexTypes;
        }
    }
    #endregion
    #region PerRow
    private void FieldSummaryGUI(object parent, Type parentType, PatchOperation? wouldBePatch, Type? type, object? @object, FieldInfo info, string path) {
        if (type == null) {
            Label(PatchToolStrings.Null, Width(500));
            return;
        }
        switch (Categorize(type)) {
            case FieldCategory.Enum: {
                    var state = m_ToggleStates.TryGetValue(path, out var s) && s;
                    var text = @object?.ToString() ?? PatchToolStrings.Null;
                    Label(state ? text.Cyan() : text, Width(500));
                    _ = DisclosureToggle(ref state, PatchToolStrings.ShowValues, null, null, Width(200));
                    m_ToggleStates[path] = state;
                    break;
                }
            case FieldCategory.Unity: {
                    var state = m_ToggleStates.TryGetValue(path, out var s) && s;
                    string label;
                    if (@object == null) {
                        label = PatchToolStrings.Null;
                    } else {
                        try {
                            label = @object.ToString();
                        } catch (Exception ex) {
                            Log($"Error in FieldSummaryGUI ToString for field {info.Name}:\n{ex}");
                            label = PatchToolStrings.ExceptionInToString.Orange();
                        }
                    }
                    Label(state ? label.Cyan() : label, Width(500));
                    _ = DisclosureToggle(ref state, PatchToolStrings.ExchangeUnityObject, null, null, Width(200));
                    m_ToggleStates[path] = state;
                    if (@object is Sprite sprite) {
                        Space(20);
                        _ = Button(PatchToolStrings.DumpToToyBoxFolder, () => {
                            var dumpDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            sprite.texture.SaveTextureToFile(Path.Combine(dumpDir, sprite.name + ".png"), -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                            try {
                                Application.OpenURL($"file://{dumpDir}");
                            } catch { }
                        });
                    }
                    break;
                }
            case FieldCategory.Reference: {
                    var state = m_ToggleStates.TryGetValue(path, out var s) && s;
                    var guid = (@object as BlueprintReferenceBase)?.Guid?.ToString();
                    string label;
                    if (guid.IsNullOrEmpty()) {
                        label = "Null or Empty Reference";
                    } else {
                        var bp = (@object as BlueprintReferenceBase)?.GetBlueprint();
                        label = bp != null ? BPHelper.GetTitle(bp) + $" ({guid})" : "Invalid Reference".Orange() + $" ({guid})";
                    }
                    Label(state ? label.Cyan() : label, Width(500));
                    _ = DisclosureToggle(ref state, PatchToolStrings.EditReference, null, null, Width(200));
                    m_ToggleStates[path] = state;
                    break;
                }
            case FieldCategory.Primitive: {
                    var n = $"{RuntimeHelpers.GetHashCode(this)}{RuntimeHelpers.GetHashCode(parent)}{RuntimeHelpers.GetHashCode(info)}{RuntimeHelpers.GetHashCode(@object)}";
                    var focused = GUI.GetNameOfFocusedControl() == n;
                    var text = @object?.ToString() ?? "<Field is null>";
                    Label(focused ? text.Cyan() : text, Width(500));
                    m_ToggleStates[path] = focused;
                    if (!m_EditStates.TryGetValue(path, out var curValue)) {
                        curValue = "";
                    }
                    var tmp = (string)curValue;
                    _ = ActionTextField(ref tmp, n, _ => m_ParseFailed[path] = false, null, Width(300));
                    m_EditStates[path] = tmp;
                    Space(20);
                    _ = Button(PatchToolStrings.Change, () => {
                        if (TryParsePrimitive(type, tmp, out var result) && result != null) {
                            m_ParseFailed[path] = false;
                            var tmpOp = new ModifyPrimitiveOperation(info.Name, parentType, type, result);
                            var op = wouldBePatch.AddOperation(tmpOp);
                            CurrentState!.AddOp(op);
                            CurrentState.CreateAndRegisterPatch();
                        } else {
                            m_ParseFailed[path] = true;
                        }
                    });
                    if (m_ParseFailed.TryGetValue(path, out var parseFailed) && parseFailed) {
                        Space(20);
                        Label(string.Format(PatchToolStrings.FailedToParse, tmp, type.Name).Orange(), AutoWidth());
                    }
                    break;
                }
            case FieldCategory.List: {
                    if (@object == null) {
                        Label(PatchToolStrings.Null, Width(500));
                        return;
                    }
                    var state = m_ToggleStates.TryGetValue(path, out var s) && s;
                    var elementCount = ElementCount(@object);
                    var text = $"{elementCount} " + PatchToolStrings.Entries;
                    Label(state ? text.Cyan() : text, Width(500));
                    _ = DisclosureToggle(ref state, PatchToolStrings.ShowEntries, null, null, Width(200));
                    m_ToggleStates[path] = state;
                    break;
                }

            case FieldCategory.Complex:
            default: {
                    if (@object == null) {
                        Label(type.IsValueType ? PatchToolStrings.NullValue : PatchToolStrings.Null, Width(500));
                        return;
                    }
                    var state = m_ToggleStates.TryGetValue(path, out var s) && s;
                    string label;
                    try {
                        label = @object.ToString() ?? PatchToolStrings.NullValue;
                    } catch (Exception ex) {
                        Log($"Error in FieldSummaryGUI ToString for field {info.Name}:\n{ex}");
                        label = PatchToolStrings.ExceptionInToString.Orange();
                    }
                    Label(state ? label.Cyan() : label, Width(500));
                    _ = DisclosureToggle(ref state, PatchToolStrings.ShowFields, null, null, Width(200));
                    m_ToggleStates[path] = state;
                    break;
                }
        }
    }
    private void FieldDetailGUI(object parent, Type parentType, PatchOperation? wouldBePatch, Type? type, object? @object, FieldInfo info, string path) {
        if (type == null || !(m_ToggleStates.TryGetValue(path, out var state) && state)) {
            return;
        }
        var category = Categorize(type);
        if (category is FieldCategory.Primitive) {
            return;
        }
        if (@object == null && category is FieldCategory.Enum or FieldCategory.List or FieldCategory.Complex) {
            return;
        }
        using (HorizontalScope()) {
            Space(IndentPerLevel * 2);
            using (VerticalScope()) {
                switch (category) {
                    case FieldCategory.Enum:
                        EnumEditorGUI(parentType, wouldBePatch, type, @object!, info, path);
                        break;
                    case FieldCategory.Reference:
                        ReferenceEditorGUI(parentType, wouldBePatch, type, info, path);
                        break;
                    case FieldCategory.List:
                        ListEditorGUI(parent, wouldBePatch, type, @object!, info, path);
                        break;
                    case FieldCategory.Primitive:
                        throw new InvalidOperationException("How are we here?");
                    case FieldCategory.Unity:
                        UnityReferenceEditorGUI(parentType, wouldBePatch, type, @object, info, path);
                        break;
                    case FieldCategory.Complex:
                    default:
                        ComplexEditorGUI(parentType, wouldBePatch, type, @object!, info, path);
                        break;
                }
            }
        }
    }
    private void EnumEditorGUI(Type parentType, PatchOperation? wouldBePatch, Type type, object @object, FieldInfo info, string path) {
        var isFlagEnum = type.IsDefined(typeof(FlagsAttribute), false);
        if (!m_EditStates.TryGetValue(path, out var curValue)) {
            curValue = isFlagEnum ? (object)EnumToBits(@object, type) : 0;
        }
        var vals = Enum.GetValues(type).Cast<object>().ToList();
        var enumNames = vals.Select(val => val.ToString()).ToArray();
        var enumValues = vals.Select(v => EnumToBits(v, type)).ToArray();
        var cellsPerRow = Math.Min(4, enumNames.Length);
        if (isFlagEnum) {
            var tmp = Convert.ToInt64(curValue);
            var totalFlags = vals.Count;
            var rows = (totalFlags + cellsPerRow - 1) / cellsPerRow;
            var flagIndex = 0;
            for (var row = 0; row < rows; row++) {
                using (HorizontalScope()) {
                    for (var col = 0; col < cellsPerRow && flagIndex < totalFlags; col++, flagIndex++) {
                        var flagName = enumNames[flagIndex];
                        var flagValue = enumValues[flagIndex];
                        var isSet = (tmp & flagValue) != 0;
                        var newIsSet = GUILayout.Toggle(isSet, flagName, Width(200));
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
            m_EditStates[path] = tmp;
            _ = Button(PatchToolStrings.Change, () => {
                var newValue = Enum.ToObject(type, tmp);
                var tmpOp = new ModifyPrimitiveOperation(info.Name, parentType, type, newValue);
                var op = wouldBePatch.AddOperation(tmpOp);
                CurrentState!.AddOp(op);
                CurrentState.CreateAndRegisterPatch();
            });
        } else {
            using (HorizontalScope()) {
                var tmp = (int)curValue;
                tmp = GUILayout.SelectionGrid(tmp, enumNames, cellsPerRow, Width(200 * cellsPerRow));
                m_EditStates[path] = tmp;
                Space(20);
                _ = Button(PatchToolStrings.Change, () => {
                    var tmpOp = new ModifyPrimitiveOperation(info.Name, parentType, type, Enum.Parse(type, enumNames[tmp]));
                    var op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState!.AddOp(op);
                    CurrentState.CreateAndRegisterPatch();
                });
            }
        }
    }
    private void ReferenceEditorGUI(Type parentType, PatchOperation? wouldBePatch, Type type, FieldInfo info, string path) {
        if (!m_PickerGUIs.TryGetValue(path, out var gui)) {
            gui = new();
            m_PickerGUIs[path] = gui;
        }
        var kind = PatchToolUtils.GetBlueprintReferenceKind(type);
        if (kind != null) {
            gui.OnGUI(newGuid => {
                var tmpOp = new ModifyBlueprintReferenceOperation(info.Name, parentType, type, newGuid);
                var op = wouldBePatch.AddOperation(tmpOp);
                CurrentState!.AddOp(op);
                CurrentState.CreateAndRegisterPatch();
            }, kind);
        } else {
            Label(PatchToolStrings.NonGenericReference.Yellow().Bold());
        }
    }
    private void UnityReferenceEditorGUI(Type parentType, PatchOperation? wouldBePatch, Type type, object? @object, FieldInfo info, string path) {
        var current = @object as UnityEngine.Object;
        if (current != null) {
            var currentId = PatchToolUtils.GetUnityAssetId(current);
            if (currentId.HasValue) {
                Label(string.Format(PatchToolStrings.CurrentAsset, currentId.Value.AssetId, currentId.Value.FileId.ToString()).Green());
            } else {
                Label(PatchToolStrings.CurrentAssetUnknown.Green());
            }
        }
        // For a collection element, type is the element's runtime type; validate against the declared one.
        var validationType = PatchToolUtils.IsListOrArray(info.FieldType)
            ? PatchToolUtils.DeclaredElementType(info.FieldType) ?? type
            : type;
        var guidKey = path + "#uguid";
        var fileKey = path + "#ufile";
        if (!m_EditStates.TryGetValue(guidKey, out var guidObj)) {
            guidObj = "";
        }
        if (!m_EditStates.TryGetValue(fileKey, out var fileObj)) {
            fileObj = "0";
        }
        var guidStr = (string)guidObj;
        var fileStr = (string)fileObj;
        using (HorizontalScope()) {
            Label(PatchToolStrings.AssetGuid, Width(120));
            _ = ActionTextField(ref guidStr, guidKey, null, null, Width(400));
            Space(20);
            Label(PatchToolStrings.AssetFileId, Width(120));
            _ = ActionTextField(ref fileStr, fileKey, null, null, Width(200));
        }
        m_EditStates[guidKey] = guidStr;
        m_EditStates[fileKey] = fileStr;
        using (HorizontalScope()) {
            _ = Button(PatchToolStrings.Change, () => {
                UnityEngine.Object? resolved = null;
                if (!guidStr.IsNullOrEmpty()
                    && long.TryParse(fileStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var fileId)
                    && (resolved = PatchToolUtils.ResolveUnityAsset(guidStr, fileId)) != null) {
                    // Reject an unassignable asset here; the op would otherwise always throw on apply.
                    if (!validationType.IsAssignableFrom(resolved.GetType())) {
                        m_ParseFailed[path] = false;
                        m_AssetTypeMismatch[path] = resolved.GetType();
                        return;
                    }
                    m_ParseFailed[path] = false;
                    _ = m_AssetTypeMismatch.Remove(path);
                    var tmpOp = new ModifyUnityReferenceOperation(info.Name, parentType, guidStr, fileId);
                    var op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState!.AddOp(op);
                    CurrentState.CreateAndRegisterPatch();
                } else {
                    m_ParseFailed[path] = true;
                    _ = m_AssetTypeMismatch.Remove(path);
                }
            });
            if (m_ParseFailed.TryGetValue(path, out var parseFailed) && parseFailed) {
                Space(20);
                Label(PatchToolStrings.CouldNotResolveAsset.Orange(), AutoWidth());
            } else if (m_AssetTypeMismatch.TryGetValue(path, out var assetType)) {
                Space(20);
                Label(string.Format(PatchToolStrings.IncompatibleAssetType, assetType.Name, validationType.Name).Orange(), AutoWidth());
            }
        }
    }
    private void ListEditorGUI(object parent, PatchOperation? wouldBePatch, Type type, object collection, FieldInfo info, string path) {
        var defaultType = ElementType(type, collection);
        var localIndex = 0;
        foreach (var elem in (collection as IEnumerable)!.Cast<object>().ToList()) {
            ListItemGUI(wouldBePatch, parent, info, elem, localIndex, collection, path, defaultType);
            localIndex += 1;
        }
        _ = Button(PatchToolStrings.AddItem, () => {
            _ = AddItemState.CreateArrayElement(parent, info, collection, -1, wouldBePatch, this, path);
        });
        if (AddItemStates.TryGetValue(path, out var activeAddItemState)) {
            Label(PatchToolStrings.NewItem, Width(500));
            activeAddItemState.AddItemGUI();
        }
    }
    private void ComplexEditorGUI(Type parentType, PatchOperation? wouldBePatch, Type type, object @object, FieldInfo info, string path) {
        var tmpOp = new ModifyComplexOperation(info.Name, parentType);
        var op = wouldBePatch.AddOperation(tmpOp);
        NestedGUI(@object, path, op, type);
    }
    #endregion
    private void ListItemGUI(PatchOperation? wouldBePatch, object parent, FieldInfo info, object? elem, int index, object collection, string path, Type? defaultType = null) {
        var tmpOp = new ModifyCollectionElementOperation(info.Name, parent.GetType(), index);
        var op = wouldBePatch.AddOperation(tmpOp);
        var elemType = elem?.GetType() ?? defaultType;
        var itemPath = path + $"/[{index}]";
        using (HorizontalScope()) {
            var painted = m_ToggleStates.TryGetValue(path, out var shouldPaint) && shouldPaint;
            var indexLabel = $"[{index}] ({elem?.GetType().Name ?? "Null"})";
            Label(painted ? indexLabel.Cyan() : indexLabel, Width(500));
            FieldSummaryGUI(parent, parent.GetType(), op, elemType, elem, info, itemPath);
            Space(20);
            _ = Button(PatchToolStrings.AddBefore, () => {
                _ = AddItemState.CreateArrayElement(parent, info, collection, index, wouldBePatch, this, path);
            });
            Space(10);
            _ = Button(PatchToolStrings.AddAfter, () => {
                _ = AddItemState.CreateArrayElement(parent, info, collection, index + 1, wouldBePatch, this, path);
            });
            Space(10);
            _ = Button(PatchToolStrings.Remove, () => {
                var removeOp = new RemoveCollectionElementOperation(info.Name, parent.GetType(), index);
                var opRemove = wouldBePatch.AddOperation(removeOp);
                CurrentState!.AddOp(opRemove);
                CurrentState.CreateAndRegisterPatch();
            });
        }
        FieldDetailGUI(parent, parent.GetType(), op, elemType, elem, info, itemPath);
    }
    private static long EnumToBits(object value, Type enumType) {
        var underlying = Convert.ChangeType(value, Enum.GetUnderlyingType(enumType));
        return underlying is ulong u ? unchecked((long)u) : Convert.ToInt64(underlying);
    }
    private static bool TryParsePrimitive(Type type, string text, out object? result) {
        result = null;
        if (type == typeof(string)) {
            result = text;
            return true;
        }
        var invariantMethod = AccessTools.Method(type, "TryParse", [typeof(string), typeof(NumberStyles), typeof(IFormatProvider), type.MakeByRefType()]);
        MethodInfo? method;
        object[] parameters;
        if (invariantMethod != null) {
            method = invariantMethod;
            parameters = [text, NumberStyles.Any, CultureInfo.InvariantCulture, Activator.CreateInstance(type)];
        } else {
            method = AccessTools.Method(type, "TryParse", [typeof(string), type.MakeByRefType()]);
            parameters = [text, Activator.CreateInstance(type)];
        }
        if (method == null || (bool)(method.Invoke(null, parameters) ?? false) == false) {
            return false;
        }
        result = parameters[parameters.Length - 1];
        return true;
    }
    private static int ElementCount(object collection) {
        if (collection is Array array) {
            return array.Length;
        }
        if (collection is IList list) {
            return list.Count;
        }
        return (collection as IEnumerable<object>)?.Count() ?? (collection as IEnumerable)?.Cast<object>().Count() ?? 0;
    }
    private static Type? ElementType(Type type, object collection) {
        if (type.IsArray) {
            return type.GetElementType();
        }
        if (collection is IList) {
            return null;
        }
        return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0]
            ?? (collection as IEnumerable<object>)?.NotNull()?.FirstOrDefault()?.GetType();
    }
    private void PopulateFieldsAndObjects(object? o, string path, Type type) {
        Dictionary<FieldInfo, object?> result = [];
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
        if (type == typeof(Kingmaker.Localization.SharedStringAsset)) {
            var toRemove = result.FirstOrDefault(f => f.Key.Name == "m_CachedPtr").Key;
            if (toRemove != null) {
                _ = result.Remove(toRemove);
            }
        }
        m_FieldsByObject[path] = result;
    }

    [LocalizedString("ToyBox_Features_PatchTool_PatchToolTabUI_m_ShowBlueprintPickerLocalizedText", "Show Blueprint Picker")]
    private static partial string m_ShowBlueprintPickerLocalizedText { get; }
}
