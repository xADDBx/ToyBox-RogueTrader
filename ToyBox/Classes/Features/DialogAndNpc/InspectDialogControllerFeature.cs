using Kingmaker;
using ToyBox.Infrastructure.Inspector;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc;

public partial class InspectDialogControllerFeature : Feature {
    [LocalizedString("ToyBox_Features_DialogAndNpc_InspectDialogControllerFeature_Name", "Inspect Dialog Controller")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_InspectDialogControllerFeature_Description", "Allows inspecting the current Dialog Controller to e.g. debug issues.")]
    public override partial string Description { get; }
    private TimedCache<float>? m_WidthCache;
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        m_WidthCache ??= new(() => CalculateLargestLabelWidth([Name], GUI.skin.toggle));
        InspectorUI.InspectToggle(Game.Instance.DialogController, Name.Green(), options: Width(m_WidthCache));
        InspectorUI.InspectIfExpanded(Game.Instance.DialogController);
    }
}
