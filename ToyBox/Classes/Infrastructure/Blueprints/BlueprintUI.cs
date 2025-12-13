using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.Utility.UnityExtensions;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToyBox.Features.SettingsFeatures.Blueprints;
using ToyBox.Features.SettingsFeatures.BrowserSettings;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;
using ToyBox.Infrastructure.Inspector;
using UnityEngine;

namespace ToyBox.Infrastructure.Blueprints;

public static partial class BlueprintUI {
    private class Widths {
        public float TitleWidth;
        public float TypeWidth;
        public float AssetIdWidth;
    }
    private static readonly ConditionalWeakTable<IPagedList, Widths> m_CachedWidths = new();
    private static EntityFact? DefaultItemGetter(object bp, BaseUnitEntity? ch) {
        if (bp is BlueprintUnitFact fact && ch != null) {
            return ch.Facts.Get(fact);
        }
        return null;
    }
    private static readonly Dictionary<Type, Func<object, object, bool, BaseUnitEntity?, bool?>> m_ActionInvokerCache = [];
    private static bool? InvokeAction(object action, object bp, bool isSearch, BaseUnitEntity? unit) {
        var bpType = bp.GetType();
        if (!m_ActionInvokerCache.TryGetValue(bpType, out var invoker)) {
            m_ActionInvokerCache[bpType] = invoker = CreateActionInvoker(bpType);
        }
        return invoker(action, bp, isSearch, unit);
    }
    private static Func<object, object, bool, BaseUnitEntity?, bool?> CreateActionInvoker(Type bpType) {
        var ifaceType = typeof(IExecutableAction<>).MakeGenericType(bpType);
        var method = ifaceType.GetMethod("OnGui", BindingFlags.Public | BindingFlags.Instance);

        var actionParam = Expression.Parameter(typeof(object), "actionObj");
        var blueprintParam = Expression.Parameter(typeof(object), "blueprintObj");
        var isSearchParam = Expression.Parameter(typeof(bool), "isSearch");
        var chParam = Expression.Parameter(typeof(BaseUnitEntity), "ch");

        var castAction = Expression.Convert(actionParam, ifaceType);

        var castBlueprint = Expression.Convert(blueprintParam, bpType);

        var paramsArray = Expression.NewArrayInit(typeof(object), Expression.Convert(chParam, typeof(object)));

        var call = Expression.Call(castAction, method, castBlueprint, isSearchParam, paramsArray);

        var lambda = Expression.Lambda<Func<object, object, bool, BaseUnitEntity?, bool?>>(Expression.Convert(call, typeof(bool?)), actionParam, blueprintParam, isSearchParam, chParam);

        return lambda.Compile();
    }
    public static void BlueprintRowGUI<TBlueprint>(Browser<TBlueprint> browser, TBlueprint blueprint, BaseUnitEntity? ch, Type? overrideForActions = null, Func<TBlueprint, BaseUnitEntity?, object?>? maybeItemGetter = null) where TBlueprint : SimpleBlueprint {
        if (!m_CachedWidths.TryGetValue(browser, out var widths) || (!browser.IsCachedValid && browser.PagedItems.Count > 0)) {
            var wasDefault = widths == null;
            widths ??= new();
            widths.TitleWidth = Math.Min(0.3f * EffectiveWindowWidth(), CalculateLargestLabelWidth(browser.PagedItems.Select(bp => BPHelper.GetTitle(bp).Cyan().Bold())));
            widths.TypeWidth = Math.Min(0.2f * EffectiveWindowWidth(), CalculateLargestLabelWidth(browser.PagedItems.Select(bp => bp.GetType().Name.Orange())) + 5 * Main.UIScale);
            widths.AssetIdWidth = Math.Min(0.3f * EffectiveWindowWidth(), CalculateLargestLabelWidth(browser.PagedItems.Select(bp => bp.AssetGuid.ToString()), GUI.skin.textField));
            if (wasDefault) {
                m_CachedWidths.Add(browser, widths);
            }
            browser.SetCacheValid();
        }
        var maybeItem = (maybeItemGetter ?? DefaultItemGetter).Invoke(blueprint, ch);
        var name = BPHelper.GetTitle(blueprint);
        if (maybeItem != null) {
            name = name.Cyan().Bold();
        }
        using (VerticalScope()) {
            using (HorizontalScope()) {
                InspectorUI.InspectToggle(blueprint);
                UI.Label(name, Width(widths.TitleWidth));
                if (Feature.GetInstance<ShowBlueprintTypeSetting>().IsEnabled) {
                    Space(5);
                    UI.Label(blueprint.GetType().Name.Orange(), Width(widths.TypeWidth));
                }
                if (Feature.GetInstance<ShowBlueprintAssetIdsSetting>().IsEnabled) {
                    Space(5);
                    var tmp = blueprint.AssetGuid.ToString();
                    _ = UI.TextField(ref tmp, null, Width(widths.AssetIdWidth));
                }
                Space(5);
                foreach (var action in BlueprintActionFeature.GetActionsForBlueprintType(blueprint.GetType(), overrideForActions)) {
                    _ = InvokeAction(action, blueprint, false, ch);
                }
                Space(5);
                var desc = BPHelper.GetDescription(blueprint);
                if (!desc.IsNullOrEmpty()) {
                    UI.Label(desc!.Green());
                }
            }
            InspectorUI.InspectIfExpanded(blueprint, maybeItem ?? blueprint);
        }
    }
    private static bool m_DoShowSettings = false;
    public static void BlueprintHeaderGUI() {
        using (VerticalScope()) {
            UI.DisclosureToggle(ref m_DoShowSettings, m_ShowSettingsLocalizedText.Green().Bold());
            if (m_DoShowSettings) {
                Feature.GetInstance<ShowDisplayAndInternalNamesSetting>().OnGui();
                Feature.GetInstance<ShowBlueprintAssetIdsSetting>().OnGui();
                Feature.GetInstance<ShowBlueprintTypeSetting>().OnGui();
                Feature.GetInstance<SearchDescriptionsSetting>().OnGui();
            }
        }
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintUI_m_ShowSettingsLocalizedText", "Show Settings")]
    private static partial string m_ShowSettingsLocalizedText { get; }
}
