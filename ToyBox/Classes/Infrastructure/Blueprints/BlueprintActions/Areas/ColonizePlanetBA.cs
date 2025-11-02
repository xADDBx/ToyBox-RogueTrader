using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Cheats;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
public partial class ColonizePlanetBA : BlueprintActionFeature, IBlueprintAction<BlueprintPlanet> {
    public bool CanExecute(BlueprintPlanet blueprint, params object[] parameter) {
        if (IsInGame()) {
            var system = blueprint.ConnectedAreas.FirstOrDefault(f => f is BlueprintStarSystemMap) as BlueprintStarSystemMap;
            return blueprint.GetComponent<ColonyComponent>() != null && Game.Instance.CurrentlyLoadedArea is BlueprintStarSystemMap && (system == null || Game.Instance.Player.CurrentStarSystem == system);
        } else {
            return false;
        }
    }

    private bool Execute(BlueprintPlanet blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        try {
            CheatsColonization.ColonizePlanet(blueprint);
            return true;
        } catch (Exception ex) {
            Warn($"Error trying to colonize Planet. Are you in the correct Star System?\n{ex}");
            return false;
        }
    }
    public bool? OnGui(BlueprintPlanet blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_Colonize, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintPlanet? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizePlanetBA_ColonizeText", "Colonize")]
    private static partial string m_Colonize { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizePlanetBA_Name", "Colonize BlueprintPlanet")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizePlanetBA_Description", "Tries to colonize the specified BlueprintPlanet in the current Star System.")]
    public override partial string Description { get; }
}
