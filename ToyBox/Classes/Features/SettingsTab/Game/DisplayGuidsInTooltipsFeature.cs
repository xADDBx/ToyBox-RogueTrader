using Kingmaker.Code.UI.MVVM.View.Slots;
using Kingmaker.Code.UI.MVVM.VM.ActionBar;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Bricks;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Templates;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.Models.UnitSettings;
using Owlcat.Runtime.UI.Tooltips;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;
using UnityEngine;

namespace ToyBox.Features.SettingsTab.Game;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.SettingsTab.Game.DisplayGuidsInTooltipsFeature")]
public partial class DisplayGuidsInTooltipsFeature : FeatureWithPatch, IToggleWithPseudoBinding {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableDisplayGuidsInTooltips;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_DisplayGuidsInTooltipsFeature_Name", "Display GUIDs in most Tooltips")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_DisplayGuidsInTooltipsFeature_Description", "Displays the guids of the items etc. in their tooltips and allows copying them by pressing LMB + Hotkey.")]
    public override partial string Description { get; }
    public override void Enable() {
        base.Enable();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public Hotkey? Keybind {
        get;
        set;
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.SettingsTab.Game.DisplayGuidsInTooltipsFeature";
        }
    }

    public void ExecuteAction(ActionParameter parameter) {
        throw new NotImplementedException("By Design");
    }

    public void LogExecution(ActionParameter parameter) {
        throw new NotImplementedException("By Design");
    }
    private static TooltipBrickText GetTooltip(string text) {
        return new TooltipBrickText(text.Grey().SizePercent(105), TooltipTextType.Simple | TooltipTextType.Italic);
    }
    private static void CopyToClipboard(string guid) {
        GUIUtility.systemCopyBuffer = guid;
        EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(m_CopiedGuidToClipboard_LocalizedText + " " + guid, false));
    }
    [HarmonyPatch(typeof(TooltipTemplateAbility), nameof(TooltipTemplateAbility.GetBody)), HarmonyPostfix]
    private static void TooltipTemplateAbility_GetBody_Patch(TooltipTemplateAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
        var guid = __instance.BlueprintAbility?.AssetGuid;
        if (guid != null) {
            __result = [GetTooltip($"guid: {guid}"), .. __result];
        }
    }
    [HarmonyPatch(typeof(TooltipTemplateActivatableAbility), nameof(TooltipTemplateActivatableAbility.GetBody)), HarmonyPostfix]
    private static void TooltipTemplateActivatableAbility_GetBody_Patch(TooltipTemplateActivatableAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
        var guid = __instance.BlueprintActivatableAbility?.AssetGuid;
        var guid2 = __instance.BlueprintActivatableAbility?.m_Buff?.guid;
        if (guid != null && guid2 != null) {
            __result = [GetTooltip($"{guid}\nbuff: {guid2}"), .. __result];
        }
    }
    [HarmonyPatch(typeof(TooltipTemplateItem), nameof(TooltipTemplateItem.GetBody)), HarmonyPostfix]
    private static void TooltipTemplateItem_GetBody_Patch(TooltipTemplateItem __instance, ref IEnumerable<ITooltipBrick> __result) {
        var guid = __instance.m_BlueprintItem?.AssetGuid ?? __instance.m_Item?.Blueprint?.AssetGuid;
        if (guid != null) {
            __result = [GetTooltip(guid), .. __result];
        }
    }
    [HarmonyPatch(typeof(TooltipTemplateBuff), nameof(TooltipTemplateBuff.GetBody)), HarmonyPostfix]
    public static void TooltipTemplateBuff_GetBody_Patch(TooltipTemplateBuff __instance, ref IEnumerable<ITooltipBrick> __result) {
        var guid = __instance.Buff?.Blueprint?.AssetGuid;
        if (guid != null) {
            __result = [GetTooltip(guid), .. __result];
        }
    }
    [HarmonyPatch(typeof(ActionBarSlotVM), nameof(ActionBarSlotVM.OnMainClick)), HarmonyPrefix]
    private static bool LeftClickToolbar(ActionBarSlotVM __instance) {
        if (GetInstance<DisplayGuidsInTooltipsFeature>().Keybind?.IsActive() ?? false) {
            switch (__instance.MechanicActionBarSlot) {
                case MechanicActionBarSlotAbility ab:
                    CopyToClipboard(ab.Ability.Blueprint.AssetGuidThreadSafe);
                    return false;
                case MechanicActionBarSlotActivableAbility act:
                    CopyToClipboard(act.ActivatableAbility.Blueprint.AssetGuidThreadSafe);
                    return false;
                case MechanicActionBarSlotItem item:
                    CopyToClipboard(item.Item.Blueprint.AssetGuidThreadSafe);
                    return false;
                case MechanicActionBarSlotSpell spell:
                    CopyToClipboard(spell.Spell.Blueprint.AssetGuidThreadSafe);
                    return false;
                case MechanicActionBarSlotSpontaneusConvertedSpell cspell:
                    CopyToClipboard(cspell.Spell.Blueprint.AssetGuidThreadSafe);
                    return false;
                default:
                    throw new NotSupportedException("Missing SlotType Case?");
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(ItemSlotPCView), nameof(ItemSlotPCView.OnClick)), HarmonyPostfix]
    private static void LeftClickItem(ItemSlotPCView __instance) {
        if (GetInstance<DisplayGuidsInTooltipsFeature>().Keybind?.IsActive() ?? false) {
            var guid = __instance.ViewModel?.Item?.Value?.Blueprint?.AssetGuid;
            if (guid != null) {
                CopyToClipboard(guid);
            }
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Game_DisplayGuidsInTooltipsFeature_m_CopiedGuidToClipboard_LocalizedText", "Copied Guid to clipboard:")]
    private static partial string m_CopiedGuidToClipboard_LocalizedText { get; }
}
