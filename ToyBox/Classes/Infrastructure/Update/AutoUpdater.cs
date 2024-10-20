using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace ToyBox {
    public static class AutoUpdater {
        public static bool Update(UnityModManager.ModEntry.ModLogger logger, string repoLink, string repositoryJsonLink, string releaseName, string curVersion) {
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
                var raw = web.DownloadString(repositoryJsonLink);
                var result = JsonConvert.DeserializeAnonymousType(raw, definition);
                string version = result.Releases[0].Version;
                if (new Version(VersionChecker.GetNumifiedVersion(logger, version)) > new Version(VersionChecker.GetNumifiedVersion(logger, curVersion))) {
                    string downloadUrl = $"{repoLink}/releases/download/v{version}/{releaseName}-{version}.zip";
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

                    logger.Log($"Successfully updated mod to version {version}!");
                    return true;
                } else {
                    logger.Log($"Detected remote version {version} is not newer than local version {curVersion}");
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