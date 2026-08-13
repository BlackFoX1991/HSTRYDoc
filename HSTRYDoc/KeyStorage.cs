using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HSTRYDoc
{
    internal static class KeyStorage
    {
        public const string KeyFolderName = "HSTRY_KEY";
        public const string DefaultOwnerPrivateKeyFileName = "owner.hstrypriv";

        public static string NormalizeDriveRoot(string driveRoot)
        {
            if (string.IsNullOrWhiteSpace(driveRoot))
                return string.Empty;

            try
            {
                string root = Path.GetPathRoot(driveRoot.Trim()) ?? string.Empty;
                return root;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetKeyFolderForDrive(string driveRoot)
        {
            string root = NormalizeDriveRoot(driveRoot);
            if (string.IsNullOrWhiteSpace(root))
                return string.Empty;

            return Path.Combine(root, KeyFolderName);
        }

        public static string GetPrivateKeyPath(string driveRoot, string? privateKeyFileName)
        {
            string folder = GetKeyFolderForDrive(driveRoot);
            if (string.IsNullOrWhiteSpace(folder))
                return string.Empty;

            string fileName = string.IsNullOrWhiteSpace(privateKeyFileName)
                ? DefaultOwnerPrivateKeyFileName
                : Path.GetFileName(privateKeyFileName);

            return Path.Combine(folder, fileName);
        }

        public static string GetSigningPrivateKeyPath(string ecdhPrivateKeyPath)
            => Path.ChangeExtension(ecdhPrivateKeyPath, ".hstrysigpriv");

        public static string[] FindAvailableDriveRoots()
        {
            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch { return Array.Empty<string>(); }

            return drives
                .Where(d =>
                {
                    try { return d.IsReady; }
                    catch { return false; }
                })
                .Select(d => NormalizeDriveRoot(d.RootDirectory.FullName))
                .Where(root => !string.IsNullOrWhiteSpace(root))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(root => root, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool IsAvailableDriveRoot(string driveRoot)
        {
            string root = NormalizeDriveRoot(driveRoot);
            if (string.IsNullOrWhiteSpace(root))
                return false;

            try
            {
                var drive = new DriveInfo(root);
                return drive.IsReady;
            }
            catch
            {
                return false;
            }
        }

        public static string[] FindPrivateKeysInKeyFolder(string driveRoot)
        {
            string folder = GetKeyFolderForDrive(driveRoot);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return Array.Empty<string>();

            try
            {
                return Directory.GetFiles(folder, "*.hstrypriv", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static bool TryGetDriveRootAndFileNameFromKeyPath(string keyPath, out string driveRoot, out string privateKeyFileName)
        {
            driveRoot = string.Empty;
            privateKeyFileName = string.Empty;

            if (string.IsNullOrWhiteSpace(keyPath))
                return false;

            string fullPath;
            try { fullPath = Path.GetFullPath(keyPath); }
            catch { return false; }

            string? root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
                return false;

            string expectedFolder = GetKeyFolderForDrive(root);
            string? folder = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(folder) ||
                !string.Equals(Path.GetFullPath(folder), Path.GetFullPath(expectedFolder), StringComparison.OrdinalIgnoreCase))
                return false;

            string fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.EndsWith(".hstrypriv", StringComparison.OrdinalIgnoreCase))
                return false;

            driveRoot = NormalizeDriveRoot(root);
            privateKeyFileName = fileName;
            return true;
        }

        public static bool IsKeyPathInManagedFolder(string keyPath)
            => TryGetDriveRootAndFileNameFromKeyPath(keyPath, out _, out _);

        internal static string[] GetPrivateKeyFileNames(IEnumerable<string> keyPaths)
            => keyPaths
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
