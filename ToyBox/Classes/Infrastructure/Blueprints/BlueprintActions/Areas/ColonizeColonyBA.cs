using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Cheats;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class ColonizeColonyBA : BlueprintActionFeature, IBlueprintAction<BlueprintColony> {
    private static Dictionary<BlueprintColony, BlueprintPlanet>? m_ColonyToPlanet = null;
    public bool CanExecute(BlueprintColony blueprint, params object[] parameter) {
        if (IsInGame()) {
            if (m_ColonyToPlanet == null) {
                var bps = BPLoader.GetBlueprintsOfType<BlueprintPlanet>();
                if (bps != null) {
                    m_ColonyToPlanet = [];
                    foreach (var planet in bps) {
                        var colonyComponent = planet.GetComponent<ColonyComponent>();
                        if (colonyComponent != null) {
                            if (colonyComponent.ColonyBlueprint != null) {
                                m_ColonyToPlanet[colonyComponent.ColonyBlueprint] = planet;
                            }
                        }
                    }
                }
            }
            if (m_ColonyToPlanet?.TryGetValue(blueprint, out var maybePlanet) ?? false) {
                var system = maybePlanet.ConnectedAreas.FirstOrDefault(f => f is BlueprintStarSystemMap) as BlueprintStarSystemMap;
                return maybePlanet && Game.Instance.CurrentlyLoadedArea is BlueprintStarSystemMap && (system == null || Game.Instance.Player.CurrentStarSystem == system);
            }
        }
        return false;
    }

    private bool Execute(BlueprintColony blueprint, params object[] parameter) {
        try {
            if (m_ColonyToPlanet?.TryGetValue(blueprint, out var planet) ?? false) {
                LogExecution(blueprint, planet, parameter);
                CheatsColonization.ColonizePlanet(planet);
            }
            return true;
        } catch (Exception ex) {
            Warn($"Error trying to colonize Colony. Are you in the correct Star System?\n{ex}");
            return false;
        }
    }
    public bool? OnGui(BlueprintColony blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_Colonize, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintColony? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizeColonyBA_ColonizeText", "Colonize")]
    private static partial string m_Colonize { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizeColonyBA_Name", "Colonize BlueprintColony")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ColonizeColonyBA_Description", "Tries to colonize the specified BlueprintColony in the current Star System.")]
    public override partial string Description { get; }
}
