using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace ToyBox {
    public static class Updater {
        private static string GetReleaseName(string version) => $"ToyBox-{version}.zip";
        private static string GetDownloadLink(string repoLink, string version) => $"{repoLink}/releases/download/v{version}/{GetReleaseName(version)}";
        public static bool Update(UnityModManager.ModEntry modEntry, bool force = false) {
            var logger = modEntry.Logger;
            var curVersion = modEntry.Info.Version;
            FileInfo file = null;
            DirectoryInfo tmpDir = null;
            try {
                using var web = new WebClient();
                var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                file = new FileInfo(Path.Combine(curDir, "TmpUpdate.zip"));
                tmpDir = new DirectoryInfo(Path.Combine(curDir, "TmpExtract"));
                if (file.Exists) {
                    file.Delete();
                }
                if (tmpDir.Exists) {
                    tmpDir.Delete(true);
                }
                var definition = new {
                    Releases = new[] {
                        new {
                            Id = "",
                            Version = ""
                        }
                    }
                };

                var raw = web.DownloadString(modEntry.Info.Repository);
                var result = JsonConvert.DeserializeAnonymousType(raw, definition);
                string remoteVersion = result.Releases[0].Version;
                bool repoHasNewVersion = new Version(VersionChecker.GetNumifiedVersion(logger, remoteVersion)) > new Version(VersionChecker.GetNumifiedVersion(logger, curVersion));

                if (force || repoHasNewVersion) {
                    string downloadUrl = GetDownloadLink(modEntry.Info.HomePage, remoteVersion);
                    logger.Log($"Downloading: {downloadUrl}");
                    web.DownloadFile(downloadUrl, file.FullName);
                    using var zipFile = ZipFile.OpenRead(file.FullName);

                    // Dry run
                    foreach (ZipArchiveEntry entry in zipFile.Entries) {
                        string fullPath = Path.GetFullPath(Path.Combine(tmpDir.FullName, entry.FullName));

                        if (Path.GetFileName(fullPath).Length == 0) {
                            Directory.CreateDirectory(fullPath);
                        } else {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            entry.ExtractToFile(fullPath, overwrite: true);
                        }
                    }

                    // Extract successfully? Then do it again for real
                    foreach (ZipArchiveEntry entry in zipFile.Entries) {
                        string fullPath = Path.GetFullPath(Path.Combine(curDir, entry.FullName));

                        if (Path.GetFileName(fullPath).Length == 0) {
                            Directory.CreateDirectory(fullPath);
                        } else {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            entry.ExtractToFile(fullPath, overwrite: true);
                        }
                    }

                    zipFile.Dispose();

                    file.Delete();
                    tmpDir.Delete(true);

                    logger.Log($"Successfully updated mod to version {remoteVersion}!");
                    return true;
                } else {
                    logger.Log($"Already up-to-data! Remote ({remoteVersion}) <= Local ({curVersion})");
                }
            } catch (Exception ex) {
                logger.Log($"Error trying to update mod: \n{ex.ToString()}");
            } finally {
                if (file?.Exists ?? false) {
                    file.Delete();
                }
                if (tmpDir?.Exists ?? false) {
                    tmpDir.Delete(true);
                }
            }
            return false;
        }
    }
}
