using static ToyBox.Infrastructure.UI;

namespace ToyBox.Features.PatchTool;

public static class PatchToolUIManager {
    private static readonly List<PatchToolTabUI> m_Instances = new();
    private static int m_SelectedIndex = -1;
    private static bool m_ShowExistingPatchesUI = false;
    public static void OnGUI() {
        Action? pendingTabAction = null;
        Label(PatchToolStrings.WarningLabel.Yellow().Bold() + " " + PatchToolStrings.PowerfulFeatureWarning.Yellow() + " " + PatchToolStrings.PowerfulFeatureWarning2.Orange().Bold() + " " + PatchToolStrings.KeepABackup.Yellow());
        Label(PatchToolStrings.NoteLabel.Green().Bold() + " " + PatchToolStrings.RestartAdvised.Green());
        Label(PatchToolStrings.WarningLabel.Yellow().Bold() + " " + PatchToolStrings.NewFeatureWarning.Yellow());
        Space(15);
        Div.DrawDiv();
        Space(15);
        _ = DisclosureToggle(ref m_ShowExistingPatchesUI, PatchToolStrings.ManageExistingPatches);
        if (m_ShowExistingPatchesUI) {
            PatchListUI.OnGUI();
        }
        Space(15);
        Div.DrawDiv();
        Space(15);
        using (HorizontalScope()) {
            Label(PatchToolStrings.Tabs.Bold(), AutoWidth());
            Space(50);
            _ = Button(PatchToolStrings.CreateNewTab, () => {
                pendingTabAction = () => {
                    m_Instances.Add(new PatchToolTabUI());
                    m_SelectedIndex = m_Instances.Count - 1;
                };
            }, null, AutoWidth());
        }
        Label("");
        using (VerticalScope()) {
            for (var j = 0; j < m_Instances.Count; j += 4) {
                using (HorizontalScope()) {
                    for (var i = j; (i < m_Instances.Count) && (i < j + 4); i++) {
                        var index = i;
                        var tabName = string.IsNullOrEmpty(m_Instances[index].Target) ? PatchToolStrings.NewTab : m_Instances[index].Target;
                        if (index == m_SelectedIndex) {
                            Label(tabName, Width(300));
                        } else {
                            _ = Button(tabName, () => {
                                m_SelectedIndex = index;
                            }, null, Width(300));
                        }
                        _ = Button(PatchToolStrings.Close, () => {
                            pendingTabAction = () => {
                                m_Instances.RemoveAt(index);
                                if (m_SelectedIndex >= m_Instances.Count) {
                                    m_SelectedIndex = m_Instances.Count - 1;
                                }
                            };
                        }, null, Width(70));
                        Space(50);
                    }
                }
                Label("");
            }
        }
        Space(15);
        Div.DrawDiv();
        Space(15);
        if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Instances.Count) {
            m_Instances[m_SelectedIndex].OnGUI();
        } else {
            Label(PatchToolStrings.NoTabsOpen);
        }
        pendingTabAction?.Invoke();
    }
    public static void OpenBlueprintInTab(string blueprintGuid) {
        var existing = m_Instances.FirstOrDefault(tab => tab.Target.Equals(blueprintGuid, StringComparison.InvariantCultureIgnoreCase));
        if (existing != default) {
            m_SelectedIndex = m_Instances.IndexOf(existing);
        } else {
            m_Instances.Add(new PatchToolTabUI(blueprintGuid));
            m_SelectedIndex = m_Instances.Count - 1;
        }
    }
}
