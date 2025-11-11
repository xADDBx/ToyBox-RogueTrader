using Kingmaker.GameInfo;
using Newtonsoft.Json;
using System.Net;
using System.Numerics;

namespace ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;

public static class VersionChecker {
    public static bool? ResultOfCheck = null;
    // Find first entry where Mod Version of entry >= current mod version
    // Using that entry, the mod is compatible if the current game version is < than the game version in the entry
    // Meaning, an entry [x, y] will, for every mod with version <= x mark every version >= y as incompatible
    public static void IsGameVersionSupported() {
        try {
            using var web = new WebClient();
            var raw = web.DownloadString(Constants.LinkToIncompatibilitiesFile);
            var definition = new[] { new[] { "", "" } };
            var versions = JsonConvert.DeserializeAnonymousType(raw, definition);
            var currentOrNewer = versions.FirstOrDefault(v => new Version(v[0]) >= Main.ModEntry.Version);
            if (currentOrNewer == null) {
                ResultOfCheck = true;
            } else {
                ResultOfCheck = IsVersionGreaterThan(GetNumifiedVersion(currentOrNewer[1]), GetNumifiedVersion(GameVersion.GetVersion()));
            }
        } catch (Exception ex) {
            Warn(ex.ToString());
        }
    }
    internal static bool IsVersionGreaterThan(List<BigInteger> a, List<BigInteger> b) {
        int maxLen = Math.Max(a.Count, b.Count);
        for (var i = 0; i < maxLen; i++) {
            var t = (i < a.Count) ? a[i] : 0;
            var g = (i < b.Count) ? b[i] : 0;
            if (t > g) {
                return true;
            }
            if (t < g) {
                return false;
            }
        }
        return false;
    }
    internal static List<BigInteger> GetNumifiedVersion(string version) {
        var comps = version.Split('.');
        var newComps = new List<BigInteger>();
        foreach (var comp in comps) {
            BigInteger num = 0;
            foreach (var c in comp) {
                try {
                    if (uint.TryParse(c.ToString(), out var n)) {
                        num = (num * 10u) + n;
                    } else {
                        var signedCharNumber = char.ToUpper(c) - ' ';
                        var unsignedCharNumber = (uint)Math.Max(0, Math.Min(signedCharNumber, 99));
                        num = (num * 100u) + unsignedCharNumber;
                    }
                } catch (Exception ex) {
                    Warn($"Error while trying to numify version component {comp}, continuing with {num}.\n{ex}");
                    break;
                }
            }
            newComps.Add(num);
        }
        return newComps;
    }
}
