using Kingmaker;
using Kingmaker.View.MapObjects;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Common;

[IsTested]
public partial class ResetInteractablesFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ResetInteractablesFeature_Name", "Reset Interactables")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ResetInteractablesFeature_Description", "Re-enables skill checks on interactable objects in the area.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            LogExecution(parameter);
            foreach (var obj in Game.Instance.State.MapObjects) {
                foreach (var part in obj.Parts.GetAll<InteractionSkillCheckPart>()) {
                    if (part.AlreadyUsed && !part.CheckPassed) {
                        part.AlreadyUsed = false;
                        part.Enabled = true;
                    }
                }
            }
        }
    }
}
