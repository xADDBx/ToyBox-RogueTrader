using UnityEngine;

namespace ToyBox.Features.SettingsTab.Other;

public partial class TB1SettingsImporterFeature : Feature {
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_Name", "Import ToyBox 1 Settings")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_Description", "Imports settings from an installed ToyBox 1 (the Settings.xml in its mod folder) into ToyBox 2. Settings whose meaning changed or that no longer exist are skipped.")]
    public override partial string Description { get; }

    private string m_TB1Path = "";
    private string m_Result = "";
    private bool m_LastImportFailed = false;
    private List<string> m_Warnings = [];

    public override void OnGui() {
        if (string.IsNullOrEmpty(m_TB1Path)) {
            m_TB1Path = TB1SettingsPorter.DefaultTB1SettingsPath();
        }
        using (VerticalScope()) {
            using (HorizontalScope()) {
                Space(27);
                UI.Label(Name);
                Space(10);
                UI.Label(Description.Green());
            }
            using (HorizontalScope()) {
                Space(27);
                UI.Label(m_PathText.Cyan(), Width(200 * Main.UIScale));
                _ = UI.TextField(ref m_TB1Path, null, Width(700 * Main.UIScale));
            }
            using (HorizontalScope()) {
                Space(27);
                _ = UI.Button(m_ImportText.Orange().Bold(), Import, null, Width(300 * Main.UIScale));
            }
            if (m_Result.Length > 0) {
                using (HorizontalScope()) {
                    Space(27);
                    UI.Label(m_LastImportFailed ? m_Result.Red().Bold() : m_Result.Green().Bold());
                }
                foreach (var warning in m_Warnings) {
                    using (HorizontalScope()) {
                        Space(27);
                        UI.Label(warning.Yellow());
                    }
                }
            }
        }
    }

    private void Import() {
        try {
            var result = TB1SettingsPorter.Port(m_TB1Path);
            m_Warnings = result.Warnings;
            if (!result.Success) {
                m_LastImportFailed = true;
                m_Result = result.Error ?? m_FailedText;
                return;
            }

            var perSave = TB1SettingsPorter.PortPerSave();
            Settings.Save();
            if (perSave.Applied > 0) {
                InSaveSettings?.Save();
            }
            if (perSave.Error != null) {
                m_Warnings = [.. m_Warnings, perSave.Error];
            }

            m_LastImportFailed = false;
            var message = string.Format(m_ImportedText, result.Applied);
            if (perSave.NotInGame) {
                message += " " + m_PerSaveSkippedText;
            } else if (perSave.Found) {
                message += " " + string.Format(m_PerSaveImportedText, perSave.Applied);
            }
            m_Result = message;
            Log($"Imported {result.Applied} global settings (+{perSave.Applied} per-save) from ToyBox 1 at {m_TB1Path}");
        } catch (Exception ex) {
            Error(ex);
            m_LastImportFailed = true;
            m_Result = $"{m_FailedText} {ex.Message}";
            m_Warnings = [];
        }
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_PathText", "ToyBox 1 Settings.xml")]
    private static partial string m_PathText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_ImportText", "Import")]
    private static partial string m_ImportText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_ImportedText", "Imported {0} settings from ToyBox 1. Reload the mod (or restart the game) so every change takes full effect.")]
    private static partial string m_ImportedText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_PerSaveImportedText", "Also imported {0} save-specific settings (unit size / AI overrides) from the loaded save.")]
    private static partial string m_PerSaveImportedText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_PerSaveSkippedText", "Load a ToyBox 1 save and import again to also bring over its save-specific settings (unit size / AI overrides).")]
    private static partial string m_PerSaveSkippedText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_TB1SettingsImporterFeature_FailedText", "Import failed.")]
    private static partial string m_FailedText { get; }
}
