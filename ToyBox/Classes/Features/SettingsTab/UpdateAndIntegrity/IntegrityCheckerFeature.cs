using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;
[IsTested]
public partial class IntegrityCheckerFeature : ToggledFeature {
    private const string m_ChecksumFileName = "checksum";
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableFileIntegrityCheck;
        }
    }

    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdateCompatabilityFeature_EnableIntegrityCheckText", "Enable Integrity Check")]
    public override partial string Name { get; }

    [LocalizedString("ToyBox_Features_UpdateAndIntegrity_UpdateCompatabilityFeature_CheckTheIntegrityOfToyBoxFilesTe", "Check the integrity of ToyBox files")]
    public override partial string Description { get; }

    public static bool CheckFilesHealthy(string? specificLocation = null) {
        var timer = Stopwatch.StartNew();
        bool isValid = true;
        try {
            string curFile;
            if (specificLocation != null) {
                curFile = Path.Combine(specificLocation, Main.ModEntry.Info.AssemblyName);
            } else {
                curFile = Assembly.GetExecutingAssembly().Location;
            }
            Log($"Checkung checksum of {curFile}");
            var curDir = Path.GetDirectoryName(curFile);
            var file = Path.Combine(curDir, m_ChecksumFileName);
            var providedChecksum = File.ReadAllLines(file)[0];
            using var sha256 = SHA256.Create();
            var calculatedChecksum = BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(curFile))).Replace("-", "");
            isValid = providedChecksum.Equals(calculatedChecksum, StringComparison.OrdinalIgnoreCase);
            if (!isValid) {
                Log($"Checksum mismatch! expected: {providedChecksum}, calculated: {calculatedChecksum}");
            }
        } catch (Exception ex) {
            Warn($"Encountered exception while trying to verify checksum: {ex}");
        }
        Debug($"Finished ToyBox File Integrity Check in: {timer.ElapsedMilliseconds}ms");
        return isValid;
    }
}
