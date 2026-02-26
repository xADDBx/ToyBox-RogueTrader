using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class TeleportBlueprintSectorMapPointStarSystemBA : BlueprintActionFeature, IBlueprintAction<BlueprintSectorMapPointStarSystem> {
    public bool CanExecute(BlueprintSectorMapPointStarSystem blueprint, ActionParameter parameter) {
        return IsInGame() && blueprint.StarSystemAreaPoint.GetBlueprint() != null;
    }

    public bool Execute(BlueprintSectorMapPointStarSystem blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        Game.Instance.LoadArea(blueprint.StarSystemAreaPoint, AutoSaveMode.None, null);
        return true;
    }
    public bool? OnGui(BlueprintSectorMapPointStarSystem blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_Teleport, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintSectorMapPointStarSystem? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintSectorMapPointStarSystemBA_TeleportText", "Teleport")]
    private static partial string m_Teleport { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintSectorMapPointStarSystemBA_Name", "Teleport to BlueprintSectorMapPointStarSystem")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintSectorMapPointStarSystemBA_Description", "Gets the first AreaEnterPoint corresponding to the specified BlueprintSectorMapPointStarSystem and loads it.")]
    public override partial string Description { get; }
}
