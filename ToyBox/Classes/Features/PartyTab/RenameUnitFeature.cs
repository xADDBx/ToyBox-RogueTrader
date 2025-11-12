using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using ToyBox.Features.PartyTab;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Classes.Features.PartyTab;

public partial class RenameUnitFeature : ModFeature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Classes_Features_PartyTab_RenameUnitFeature_Name", "Rename Unit")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Classes_Features_PartyTab_RenameUnitFeature_Description", "Allows renaming the specified BaseUnitEntity")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        UI.EditableLabel(unit.CharacterName, unit.UniqueId, newName => {
            unit.Description.CustomName = newName;
            EventBus.RaiseEvent<IUnitNameHandler>(handler => handler.OnUnitNameChanged());
            Main.ScheduleForMainThread(FeatureTab.GetInstance<PartyFeatureTab>().NameSectionWidth.ForceRefresh);
        });
    }
}
