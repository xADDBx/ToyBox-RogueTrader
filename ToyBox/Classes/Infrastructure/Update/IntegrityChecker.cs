using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace ToyBox {
    public static class IntegrityChecker {
        private const string ChecksumFileName = "checksum";
        public static bool Check(UnityModManager.ModEntry.ModLogger logger) {
            try {
                var curFile = Assembly.GetExecutingAssembly().Location;
                var curDir = Path.GetDirectoryName(curFile);
                var providedChecksum = File.ReadAllLines(Path.Combine(curDir, ChecksumFileName))[0];
                using var sha256 = SHA256.Create();
                var calculatedChecksum = BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(curFile))).Replace("-", "");

                bool isValid = providedChecksum.Equals(calculatedChecksum, StringComparison.OrdinalIgnoreCase);
                if (!isValid) {
                    logger.Log($"Checksum mismatch! expected: {providedChecksum}, calculated: {calculatedChecksum}");
                }
                return isValid;
            } catch (Exception ex) {
                logger.Log($"Encountered exception while trying to verify checksum: {ex.ToString()}");
                return false;
            }
        }
    }
}
