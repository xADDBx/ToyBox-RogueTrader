using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class SpawnUnitBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnit> {
    public bool CanExecute(BlueprintUnit blueprint, ActionParameter parameter) {
        return IsInGame();
    }

    public bool Execute(BlueprintUnit blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        BaseUnitEntity? spawned = null;
        for (var i = 0; i < parameter.IntParam; i++) {
            var spawnPosition = Game.Instance.ClickEventsController.WorldPosition;
            var offset = 5f * UnityEngine.Random.insideUnitSphere;
            spawnPosition = new(spawnPosition.x + offset.x, spawnPosition.y, spawnPosition.z + offset.z);
            spawned = Game.Instance.EntitySpawner.SpawnUnit(blueprint, spawnPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
        }
        return spawned != null;
    }
    public bool? OnGui(BlueprintUnit blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_SpawnText + $" {parameter.IntParam}", isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
        }
        return result;
    }
    public bool GetContext(out BlueprintUnit? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_SpawnUnitBA_Spawn_x", "Spawn")]
    private static partial string m_SpawnText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_SpawnUnitBA_Name", "Spawn Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_SpawnUnitBA_Description", "Spawns the specified BlueprintUnit in the vicinity.")]
    public override partial string Description { get; }
}
