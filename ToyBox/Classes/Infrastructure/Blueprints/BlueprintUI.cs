using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Models.Tooltip.Base;
using Kingmaker.Utility.UnityExtensions;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;

namespace ToyBox.Infrastructure.Blueprints;
public static class BlueprintUI {
    public static void BlueprintRowGUI<Blueprint>(Blueprint blueprint, BaseUnitEntity ch, object? parent = null) where Blueprint : BlueprintScriptableObject, IUIDataProvider {
        parent ??= ch;
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

                foreach (var action in BlueprintActionFeature.GetActionsForBlueprintType<Blueprint>()) {
                    _ = action.OnGui(blueprint, false, ch);
                }

                Space(10);

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
