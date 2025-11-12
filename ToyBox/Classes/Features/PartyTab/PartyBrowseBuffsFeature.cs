using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab;

public partial class PartyBrowseBuffsFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_PartyBrowseBuffsFeature_Name", "Browse Unit Buffs")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_PartyBrowseBuffsFeature_Description", "Displays a Browser containing all the buffs of the unit in question and optionally allows removing them or adding new ones.")]
    public override partial string Description { get; }
    private readonly Dictionary<BaseUnitEntity, Browser<BlueprintBuff>> m_CachedBrowsers = [];
    private readonly HashSet<BaseUnitEntity> m_IsValid = [];
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }
    public void ClearFeatureCache() {
        m_CachedBrowsers.Clear();
    }
    public void MarkInvalid() {
        m_IsValid.Clear();
    }
    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        if (!m_CachedBrowsers.TryGetValue(unit, out var browser) || !m_IsValid.Contains(unit)) {
            browser ??= new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)EffectiveWindowWidth() - 40);
            browser.UpdateItems(unit.Buffs.Enumerable.Select(f => f.Blueprint));
            _ = m_IsValid.Add(unit);
            m_CachedBrowsers[unit] = browser;
        }
        browser.OnGUI(feature => {
            BlueprintUI.BlueprintRowGUI(feature, unit);
        });
    }
}
