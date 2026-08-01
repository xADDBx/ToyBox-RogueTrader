using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View.Mechanics.Entities;
using Kingmaker.Visual.CharacterSystem;
using System.Collections.Concurrent;
using System.Text;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitOverrideSkeletonFeature")]
public partial class UnitOverrideSkeletonFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableUnitOverrideSkeleton;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_Name", "Enable Skeleton / Bone Editor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_Description", "If this is disabled, none of the previously set bone overrides will take effect. Lets you resize and offset individual bones (body parts) and equipment attach points per character.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitOverrideSkeletonFeature";
        }
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    private static readonly ConcurrentDictionary<string, SkeletonReplacer> m_Replacers = new();
    private static readonly Dictionary<string, string> m_LabelCache = [];

    private bool m_ShowGlobalOffsets;
    private bool m_ShowGlobalScales;
    private bool m_ShowLocalSizes;
    private bool m_ShowEquipmentOffsets;
    private bool m_ShowEquipmentSizes;

    private static SkeletonReplacer? GetReplacer(BaseUnitEntity unit) {
        if (!m_Replacers.TryGetValue(unit.UniqueId, out var replacer) || !replacer.IsFor(unit)) {
            replacer = new SkeletonReplacer(unit);
            if (!replacer.IsValid) {
                return null;
            }
            m_Replacers[unit.UniqueId] = replacer;
            if (InSaveSettings?.SkeletonBoneOverrides.ContainsKey(unit.UniqueId) ?? false) {
                replacer.ApplyBonesModification(unit);
            }
        }
        return replacer;
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        base.OnGui();
        if (!IsEnabled) {
            return;
        }
        var replacer = GetReplacer(unit);
        if (replacer == null) {
            using (HorizontalScope()) {
                Space(20);
                UI.Label(m_NoAvatarText.Green());
            }
            return;
        }
        using (HorizontalScope()) {
            Space(20);
            using (VerticalScope()) {
                DrawGroup(unit, replacer, ref m_ShowGlobalOffsets, m_GlobalOffsetsText, replacer.GroupOF, false);
                DrawGroup(unit, replacer, ref m_ShowGlobalScales, m_GlobalScalesText, replacer.GroupSC, true);
                DrawGroup(unit, replacer, ref m_ShowLocalSizes, m_LocalSizesText, replacer.GroupSZ, true);
                DrawGroup(unit, replacer, ref m_ShowEquipmentOffsets, m_EquipmentOffsetsText, replacer.GroupIO, false);
                DrawGroup(unit, replacer, ref m_ShowEquipmentSizes, m_EquipmentSizesText, replacer.GroupIS, true);
            }
        }
    }
    private static void DrawGroup(BaseUnitEntity unit, SkeletonReplacer replacer, ref bool show, string header, Dictionary<string, SkeletonReplacer.BodyPart> group, bool log) {
        _ = UI.DisclosureToggle(ref show, header);
        if (!show) {
            return;
        }
        Space(6);
        foreach (var kvp in group) {
            var key = kvp.Key;
            var part = kvp.Value;
            using (HorizontalScope()) {
                UI.Label(FormatLabel(key), Width(325 * Main.UIScale));
                var changed = log
                    ? UI.LogSlider(ref part.Parameter, part.Min, part.Max, 1f, 2, null, Width(300 * Main.UIScale))
                    : UI.Slider(ref part.Parameter, part.Min, part.Max, 0f, 2, null, null, Width(300 * Main.UIScale));
                if (changed) {
                    replacer.ApplyBonesModification(unit, false, key);
                    InSaveSettings?.Save();
                }
            }
        }
        Space(10);
    }
    private static string FormatLabel(string key) {
        if (m_LabelCache.TryGetValue(key, out var cached)) {
            return cached;
        }
        var body = key.Length > 3 && key[2] == '_' ? key.Substring(3) : key;
        var sb = new StringBuilder(body.Length + 4);
        for (var i = 0; i < body.Length; i++) {
            var c = body[i];
            if (c == '_') {
                _ = sb.Append(' ');
            } else {
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(body[i - 1])) {
                    _ = sb.Append(' ');
                }
                _ = sb.Append(c);
            }
        }
        var words = sb.ToString().Split(' ');
        for (var i = 0; i < words.Length; i++) {
            if (words[i].Length > 0) {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
        }
        cached = string.Join(" ", words);
        m_LabelCache[key] = cached;
        return cached;
    }

    #region Patches
    private static void ScheduleReapply(BaseUnitEntity unit) {
        Main.ScheduleForMainThread(() => GetReplacer(unit)?.ApplyBonesModification(unit));
    }

    [HarmonyPatch(typeof(AbstractUnitEntityView), nameof(AbstractUnitEntityView.SetupCharacterAvatar)), HarmonyPostfix]
    private static void AbstractUnitEntityView_SetupCharacterAvatar_Postfix(AbstractUnitEntityView __instance) {
        if (__instance.EntityData is not BaseUnitEntity unit || !(InSaveSettings?.SkeletonBoneOverrides.ContainsKey(unit.UniqueId) ?? false)) {
            return;
        }
        ScheduleReapply(unit);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.UpdateCharacter)), HarmonyPostfix]
    private static void Character_UpdateCharacter_Postfix(Character __instance) {
        if (__instance.GetComponentInParent<AbstractUnitEntityView>()?.EntityData is not BaseUnitEntity unit
            || !(InSaveSettings?.SkeletonBoneOverrides.ContainsKey(unit.UniqueId) ?? false)) {
            return;
        }
        ScheduleReapply(unit);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnAreaLoaded)), HarmonyPostfix]
    private static void Player_OnAreaLoaded_Postfix() {
        try {
            m_Replacers.Clear();
            var overrides = InSaveSettings?.SkeletonBoneOverrides;
            if (overrides == null) {
                return;
            }
            foreach (var id in overrides.Keys.ToList()) {
                foreach (var unit in Game.Instance.State.AllBaseUnits.Where(u => u.UniqueId == id)) {
                    var replacer = new SkeletonReplacer(unit);
                    if (replacer.IsValid) {
                        m_Replacers[unit.UniqueId] = replacer;
                        replacer.ApplyBonesModification(unit);
                    }
                }
            }
        } catch (Exception ex) {
            Error($"Failed to reapply skeleton overrides on area load:\n{ex}");
        }
    }
    #endregion

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_GlobalOffsetsText", "Body parts global offsets")]
    private static partial string m_GlobalOffsetsText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_GlobalScalesText", "Body parts global scales")]
    private static partial string m_GlobalScalesText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_LocalSizesText", "Body parts local sizes")]
    private static partial string m_LocalSizesText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_EquipmentOffsetsText", "Equipment elements offsets")]
    private static partial string m_EquipmentOffsetsText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_EquipmentSizesText", "Equipment elements sizes")]
    private static partial string m_EquipmentSizesText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideSkeletonFeature_m_NoAvatarText", "This unit has no editable character avatar / skeleton.")]
    private static partial string m_NoAvatarText { get; }
}
