using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility.UnityExtensions;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;

namespace ToyBox.Infrastructure.Blueprints;

public static class BlueprintUI {
    public static void BlueprintRowGUI<TBlueprint>(TBlueprint blueprint, BaseUnitEntity ch) where TBlueprint : SimpleBlueprint {
        object? maybeItem = null;
        if (blueprint is BlueprintUnitFact fact) {
            maybeItem = ch.Facts.Get(fact);
        }
        var name = BPHelper.GetTitle(blueprint);
        if (maybeItem != null) {
            name = name.Cyan().Bold();
        }
        using (VerticalScope()) {
            using (HorizontalScope()) {
                UI.Label(name, Width(CalculateTitleWidth()));
#warning Proper Width?
#warning AssetGuid
                foreach (var action in BlueprintActionFeature.GetActionsForBlueprintType<TBlueprint>()) {
                    _ = action.OnGui(blueprint, false, ch);
                }

                Space(10);
#warning Type?
                var desc = BPHelper.GetDescription(blueprint);
                if (!desc.IsNullOrEmpty()) {
                    UI.Label(desc!.Green());
                }
            }
        }
    }
    private static float CalculateTitleWidth() {
        return Math.Min(300, EffectiveWindowWidth() * 0.2f);
    }
}
