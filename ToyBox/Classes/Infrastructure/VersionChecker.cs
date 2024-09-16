using Kingmaker.GameInfo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace ToyBox {
    public static class VersionChecker {
        // Find first entry where Mod Version of entry >= current mod version
        // Using that entry, the mod is compatible if the current game version is < than the game version in the entry
        // Meaning, an entry [x, y] will, for every mod with version <= x mark every version >= y as incompatible
        public static bool IsGameVersionSupported(Version modVersion, UnityModManager.ModEntry.ModLogger logger, string linkToIncompatibilitiesFile) {
            try {
                using var web = new WebClient();
                var raw = web.DownloadString(linkToIncompatibilitiesFile);
                var definition = new[] { new[] { "", "" } };
                var versions = JsonConvert.DeserializeAnonymousType(raw, definition);
                var currentOrNewer = versions.FirstOrDefault(v => new Version(v[0]) >= modVersion);
                if (currentOrNewer == null) return true;
                return new Version(GetNumifiedVersion(logger, currentOrNewer[1])) > new Version(GetNumifiedVersion(logger, GameVersion.GetVersion()));
            } catch (Exception ex) {
                logger.Log(ex.ToString());
            }
            return true;
        }
        public static string GetNumifiedVersion(UnityModManager.ModEntry.ModLogger logger, string version) {
            var comps = version.Split('.');
            var newComps = new List<string>();
            foreach (var comp in comps) {
                uint num = 0;
                foreach (var c in comp) {
                    uint newNum = num;
                    try {
                        checked {
                            if (uint.TryParse(c.ToString(), out var n)) {
                                newNum = newNum * 10u + n;
                            } else {
                                int signedCharNumber = char.ToUpper(c) - ' ';
                                uint unsignedCharNumber = (uint)Math.Max(0, Math.Min(signedCharNumber, 99));
                                newNum = newNum * 100u + unsignedCharNumber;
                            }
                            num = newNum;
                        }
                    } catch (OverflowException) {
                        logger.Log($"Encountered uint overflow while parsing version component {comp}, continuing with {num}");
                        break;
                    }
                }
                newComps.Add(num.ToString());
            }
            return string.Join(".", newComps);
        }
    }
}
