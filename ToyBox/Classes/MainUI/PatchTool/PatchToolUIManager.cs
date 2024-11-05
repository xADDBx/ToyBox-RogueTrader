using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class PatchToolUIManager {
    private static List<PatchToolTabUI> instances = new();
    private static int selectedIndex = -1;
    private static bool showExistingPatchesUI = false;
    public static void OnGUI() {
        Label("Note:".localize().Green().Bold() + " " + "As with Etudes Editor, this is a very powerful feature. You naturally won't break your game by simply changing the damage of a weapon, but this feature allows a lot of things that could potentially causes issues. Beware of that and always work on a backup save.".localize().Green());
        Label("Warning:".localize().Yellow().Bold() + " " + "After finishing creating a patch, it is advised to restart the game before playing on a proper save.".localize().Yellow());
        DisclosureToggle("Manage existing patches".localize(), ref showExistingPatchesUI, 200);
        if (showExistingPatchesUI) {
            PatchListUI.OnGUI();
            Div();
            Space(20);
        }
        Label("Tabs".localize().Bold(), AutoWidth());
        using (HorizontalScope()) {
            Space(50);
            for (int i = 0; i < instances.Count; i++) {
                using (HorizontalScope()) {
                    var tabName = instances[i].Target.IsNullOrEmpty() ? "New Tab".localize() : instances[i].Target;
                    if (i == selectedIndex) {
                        Label($"[{tabName}]", AutoWidth());
                    } else {
                        ActionButton(tabName, () => {
                            selectedIndex = i;
                        }, AutoWidth());
                    }
                    ActionButton("Close".localize(), () => {
                        instances.RemoveAt(i);
                        if (selectedIndex >= instances.Count) {
                            selectedIndex = instances.Count - 1;
                        }
                    }, AutoWidth());
                }
            }
            ActionButton("+", () => {
                instances.Add(new PatchToolTabUI());
                selectedIndex = instances.Count - 1;
            }, AutoWidth());
        }
        Div();
        Space(20);
        if (selectedIndex >= 0 && selectedIndex < instances.Count) {
            instances[selectedIndex].OnGUI();
        } else {
            Label("No tabs open.".localize());
        }
    }
}
