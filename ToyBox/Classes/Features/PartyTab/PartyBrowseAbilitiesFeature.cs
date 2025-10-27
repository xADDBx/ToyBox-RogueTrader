﻿using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab;
[NeedsTesting]
public partial class PartyBrowseAbilitiesFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_PartyBrowseAbilitiesFeature_Name", "Browse Unit Abilities")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_PartyBrowseAbilitiesFeature_Description", "Displays a Browser containing all the abilities of the unit in question and optionally allows removing them or adding new ones.")]
    public override partial string Description { get; }
    private readonly Dictionary<BaseUnitEntity, Browser<BlueprintAbility>> m_CachedBrowsers = [];
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }
    public void ClearFeatureCache() {
        m_CachedBrowsers.Clear();
    }
    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        if (!m_CachedBrowsers.TryGetValue(unit, out var browser)) {
            browser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)EffectiveWindowWidth() - 40);
            browser.UpdateItems(unit.Abilities.Enumerable.Select(f => f.Blueprint));
            m_CachedBrowsers[unit] = browser;
        }
        browser.OnGUI(feature => {
            BlueprintUI.BlueprintRowGUI(feature, unit);
        });
    }
}
