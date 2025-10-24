using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using System.Reflection;

namespace ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;
public partial class UpdaterFeature : Feature {
    private static bool m_EnqueuedStart = false;
    public static bool IsDoingUpdate = false;
    private static double m_DownloadProgress = 0f;
    public static void UpdaterGUI() {
        using (VerticalScope()) {
            if (IsDoingUpdate) {
                UI.ProgressBar(m_DownloadProgress, m_DownloadProgress_Text);
            }
            using (HorizontalScope()) {
                if (UI.Button(m_TryUpdatingToNewestVersionText.Cyan())) {
                    if (!IsDoingUpdate && !m_EnqueuedStart) {
                        m_EnqueuedStart = true;
                        new Action(() => {
                            IsDoingUpdate = true;
                            _ = Task.Run(() => Update(false, false));
                        }).ScheduleForMainThread();
                    }
                }
            }
            using (HorizontalScope()) {
                if (UI.Button(m_TryReinstallCurrentVersionText.Cyan())) {
                    if (!IsDoingUpdate && !m_EnqueuedStart) {
                        m_EnqueuedStart = true;
                        new Action(() => {
                            IsDoingUpdate = true;
                            _ = Task.Run(() => Update(true, false));
                        }).ScheduleForMainThread();
                    }
                }
            }
        }
    }
    private static string GetReleaseName(string version) {
        return $"ToyBox-{version}.zip";
    }

    private static string GetDownloadLink(string repoLink, string version) {
        return $"{repoLink}/releases/download/v{version}/{GetReleaseName(version)}";
    }

    public static string GetLatestVersion() {
        using var web = new WebClient();
        var definition = new {
            Releases = new[] {
                    new {
                        Id = "",
                        Version = ""
                    }
            }
        };

        var raw = web.DownloadString(Main.ModEntry.Info.Repository);
        var result = JsonConvert.DeserializeAnonymousType(raw, definition);
        return result!.Releases[0].Version;
    }
    public static bool Update(bool reinstallCurrentVersion = false, bool onlyUpdateIfRemoteIsNewer = true) {
        m_DownloadProgress = 0;
        FileInfo? file = null;
        DirectoryInfo? tmpDir = null;
        var updated = false;
        try {
            var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            file = new FileInfo(Path.Combine(curDir, "TmpUpdate.zip"));
            tmpDir = new DirectoryInfo(Path.Combine(curDir, "TmpExtract"));
            if (file.Exists) {
                file.Delete();
            }
            if (tmpDir.Exists) {
                tmpDir.Delete(true);
            }
            var repoHasNewVersion = false;
            string? remoteVersion = null;
            if (!reinstallCurrentVersion) {
                remoteVersion = GetLatestVersion();
                repoHasNewVersion = VersionChecker.IsVersionGreaterThan(VersionChecker.GetNumifiedVersion(remoteVersion), VersionChecker.GetNumifiedVersion(Main.ModEntry.Info.Version));
            }

            if (reinstallCurrentVersion || repoHasNewVersion || !onlyUpdateIfRemoteIsNewer) {
                var version = reinstallCurrentVersion ? Main.ModEntry.Info.Version : remoteVersion!;
                string downloadUrl = GetDownloadLink(Main.ModEntry.Info.HomePage, version);
                Log($"Downloading: {downloadUrl}");
                using var web = new WebClient();
                web.DownloadProgressChanged += (sender, e) => {
                    m_DownloadProgress = e.ProgressPercentage / 100.0;
                };
                web.DownloadFileTaskAsync(downloadUrl, file.FullName).GetAwaiter().GetResult();
                using var zipFile = ZipFile.OpenRead(file.FullName);

                // Dry run
                foreach (var entry in zipFile.Entries) {
                    var fullPath = Path.GetFullPath(Path.Combine(tmpDir.FullName, entry.FullName));

                    if (Path.GetFileName(fullPath).Length == 0) {
                        _ = Directory.CreateDirectory(fullPath);
                    } else {
                        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        entry.ExtractToFile(fullPath, overwrite: true);
                    }
                }

                var filesHealthy = IntegrityCheckerFeature.CheckFilesHealthy(tmpDir.FullName);
                if (filesHealthy) {
                    // Extract successfully? => Then do it again for real
                    // Note: At this point in time I only remember that I added the dry run to counter Exceptions while unpacking. I don't know why I didn't just copy the files from the dry run if it was successful.
                    // Note2: Probably because I didn't want to write a Directory Copy Helper method?
                    foreach (var entry in zipFile.Entries) {
                        var fullPath = Path.GetFullPath(Path.Combine(curDir, entry.FullName));

                        if (Path.GetFileName(fullPath).Length == 0) {
                            _ = Directory.CreateDirectory(fullPath);
                        } else {
                            _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            entry.ExtractToFile(fullPath, overwrite: true);
                        }
                    }

                    Log($"Successfully updated mod to version {remoteVersion}!");
                    updated = true;
                } else {
                    Warn("Extracted files failed checksum verification; aborting update.");
                }
            } else {
                Log($"Already up-to-data! Remote ({remoteVersion}) <= Local ({Main.ModEntry.Info.Version})");
            }
        } catch (Exception ex) {
            Warn($"Error trying to update mod: \n{ex}");
        } finally {
            // Using FileInfo.Delete/DirectoryInfo.Delete here won't work
            if (file != null && File.Exists(file.FullName)) {
                File.Delete(file.FullName);
            }
            if (tmpDir != null && Directory.Exists(tmpDir.FullName)) {
                Directory.Delete(tmpDir.FullName, true);
            }
        }
        if (updated) {
            Main.ModEntry.Info.DisplayName = "ToyBox ".Yellow().SizePercent(20) + m_RestartToFinishUpdateText.Green().Bold().SizePercent(40);
        }
        new Action(() => {
            IsDoingUpdate = false;
            m_DownloadProgress = 0;
        }).ScheduleForMainThread();
        return updated;
    }

    public override void OnGui() {
        UpdaterGUI();
    }

    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdaterFeature_RestartToFinishUpdateText", "Restart to finish update")]
    private static partial string m_RestartToFinishUpdateText { get; }
    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdaterFeature_TryReinstallCurrentVersionText", "Try reinstall current version")]
    private static partial string m_TryReinstallCurrentVersionText { get; }
    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdaterFeature_TryUpdatingToNewestVersionText", "Try updating to newest version")]
    private static partial string m_TryUpdatingToNewestVersionText { get; }
    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdaterFeature_DownloadProgress_Text", "Download Progress:")]
    private static partial string m_DownloadProgress_Text { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_UpdateAndIntegrity_UpdaterFeature_UpdateModText", "Update Mod")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_UpdateAndIntegrity_UpdaterFeature_ReinstallTheCurrentModVersionOrU", "Reinstall the current mod version or update to the latest available version")]
    public override partial string Description { get; }
}
