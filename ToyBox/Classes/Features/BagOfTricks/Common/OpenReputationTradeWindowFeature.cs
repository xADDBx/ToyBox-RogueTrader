using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UI.Models.UnitSettings;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.View.Spawners;

namespace ToyBox.Features.BagOfTricks.Common;

public partial class OpenReputationTradeWindowFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_OpenReputationTradeWindowFeature_Name", "Open Trade Window")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_OpenReputationTradeWindowFeature_Description", "Opens the faction reputation trade window (when available).")]
    public override partial string Description { get; }
    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            //Trade window should not be available in the Dark City and in Chapter 5. The game already disables it in the prologue.
            string[] blockedEtudes = ["725db1ff1322445c8185506f4f6d242e", "6571856eb6c0459cba30e13adc5c6314"];
            foreach (var blockedId in blockedEtudes) {
                if (ResourcesLibrary.TryGetBlueprint(blockedId) is BlueprintEtude maybeBlocked) {
                    if (Game.Instance.Player.EtudesSystem.GetSavedState(maybeBlocked).HasFlag(EtudesSystem.EtudeState.Started)) {
                        return;
                    }
                }
            }
            LogExecution(parameter);
            var bridgeArea = ResourcesLibrary.TryGetBlueprint<BlueprintArea>("255859109cec4a042ade1613d80b25a4");
            var factotum = ResourcesLibrary.TryGetBlueprint<BlueprintAnswer>("d9307ba41f354ad2be00085eca5d0264");
            var openVendorWindow = ((factotum.OnSelect.Actions[0] as Conditional)!.IfTrue.Actions[0] as OpenVendorSelectingWindow)!;
            // No null checks; If the blueprints here don't exist then the feature needs a rewrite
            if (Game.Instance.State.LoadedAreaState.Blueprint == bridgeArea) {
                openVendorWindow.Run();
            } else {
                List<Entity> entities = [];
                List<UnitSpawnerBase.MyData> spawners = [];
                List<AbstractUnitEntity> units = [];
                try {
                    var areaState = ResourcesLibrary.TryGetBlueprint<BlueprintArea>("255859109cec4a042ade1613d80b25a4");
                    var state = Game.Instance.State.GetStateForArea(areaState);
                    AreaPersistentState areaPersistentState;
                    using (var jsonStreamForArea = AreaDataStash.GetJsonStreamForArea(state, state.MainState)) {
                        areaPersistentState = AreaDataStash.Serializer.Deserialize<AreaPersistentState>(jsonStreamForArea)!;
                    }

                    var vendorScene = state.GetStateForScene("VoidshipBridge_Vendors_Mechanics");
                    using (var jsonStreamForArea2 = AreaDataStash.GetJsonStreamForArea(state, vendorScene)) {
                        if (jsonStreamForArea2 != null) {
                            var deserializedSceneState = AreaDataStash.Serializer.Deserialize<SceneEntitiesState>(jsonStreamForArea2)!;
                            areaPersistentState.SetDeserializedSceneState(deserializedSceneState);
                            spawners.AddRange(deserializedSceneState.AllEntityData.OfType<UnitSpawnerBase.MyData>());
                            units.AddRange(deserializedSceneState.AllEntityData.OfType<AbstractUnitEntity>());
                        }
                    }

                    foreach (var data in spawners) {
                        var reference = data.SpawnedUnit;
                        var Id = reference.GetId();
                        var unit = units.FirstOrDefault(u => u.UniqueId == Id);
                        reference.m_Proxy.Entity = unit;
                        if (unit != null) {
                            var parts = unit.Parts;
                            if (parts != null && parts.Owner == null) {
                                parts.Owner = unit;
                            }
                            var vendor = unit.Parts.GetOptional<PartVendor>();
                            vendor.SetSharedInventory(vendor.m_SharedInventory);

                            var uiSettings = unit.Parts.GetOptional<PartUnitUISettings>();
                            (uiSettings as EntityPart).Owner = unit;
                        }
                        Game.Instance.State.LoadedAreaState.MainState.AllEntityData.Add(data);
                        entities.Add(data);
                    }
                    openVendorWindow.Run();

                } finally {
                    foreach (var data in spawners.NotNull()) {
                        _ = Game.Instance.State.LoadedAreaState.MainState.m_EntityData.Remove(data);
                    }
                }
            }
            ToggleModWindow();
        }
    }
}
