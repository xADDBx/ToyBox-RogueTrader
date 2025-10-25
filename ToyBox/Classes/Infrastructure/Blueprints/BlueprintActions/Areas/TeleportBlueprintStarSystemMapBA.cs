using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class TeleportBlueprintStarSystemMapBA : BlueprintActionFeature, IBlueprintAction<BlueprintStarSystemMap> {
    private static readonly Dictionary<BlueprintStarSystemMap, BlueprintAreaEnterPoint?> m_MappingCache = [];
    public bool CanExecute(BlueprintStarSystemMap blueprint, params object[] parameter) {
        if (!m_MappingCache.TryGetValue(blueprint, out var mapping)) {
            mapping = BPLoader.GetBlueprintsOfType<BlueprintAreaEnterPoint>().FirstOrDefault(bp => bp.Area == blueprint);
            m_MappingCache[blueprint] = mapping;
        }
        return IsInGame() && mapping != null;
    }

    private bool Execute(BlueprintStarSystemMap blueprint, params object[] parameter) {
        if (m_MappingCache.TryGetValue(blueprint, out var mapping)) {
            LogExecution(blueprint, mapping, parameter);
            Game.Instance.LoadArea(mapping, AutoSaveMode.None, null);
            return true;
        } else {
            return false;
        }
    }
    public bool? OnGui(BlueprintStarSystemMap blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_Teleport, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintStarSystemMap? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintStarSystemMapBA_TeleportText", "Teleport")]
    private static partial string m_Teleport { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintStarSystemMapBA_Name", "Teleport to BlueprintStarSystemMap")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintStarSystemMapBA_Description", "Gets the first AreaEnterPoint corresponding to the specified BlueprintStarSystemMap and loads it.")]
    public override partial string Description { get; }
}
