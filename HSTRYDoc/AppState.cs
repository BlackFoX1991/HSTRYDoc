using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HSTRYDoc
{
    public sealed class AppState
    {
        public WindowPlacement MainWindow { get; set; } = new WindowPlacement();
        public List<RecentFileEntry> RecentFiles { get; set; } = new List<RecentFileEntry>();

        private const int MaxRecent = 20;

        private static readonly string StateFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "HSTRYDoc", "appstate.json");

        public static AppState Load()
        {
            try
            {
                if (!File.Exists(StateFilePath))
                    return new AppState();

                var json = File.ReadAllText(StateFilePath);
                var state = JsonSerializer.Deserialize<AppState>(json) ?? new AppState();

                state.TrimAndCleanup();
                return state;
            }
            catch
            {
                return new AppState();
            }
        }

        public void RemoveRecentFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return;

            try { fullPath = Path.GetFullPath(fullPath); } catch { /* ignore */ }

            RecentFiles = RecentFiles
                .Where(x => !string.Equals(x.FilePath, fullPath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Save();
        }

        public void Save()
        {
            try
            {
                TrimAndCleanup();

                var dir = Path.GetDirectoryName(StateFilePath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFilePath, json);
            }
            catch
            {
                // ignore persistence failures
            }
        }

        public IReadOnlyList<RecentFileEntry> GetRecentExisting()
        {
            TrimAndCleanup();
            return RecentFiles
                .OrderByDescending(x => x.LastUsedUtcTicks)
                .Take(MaxRecent)
                .ToList();
        }

        public void TouchRecentFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return;

            try { fullPath = Path.GetFullPath(fullPath); } catch { /* ignore */ }

            // Only store existing files (avoid dead entries)
            if (!File.Exists(fullPath))
                return;

            long nowUtcTicks = DateTimeOffset.UtcNow.UtcTicks;

            var existing = RecentFiles.FirstOrDefault(x =>
                string.Equals(x.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.LastUsedUtcTicks = nowUtcTicks;
            }
            else
            {
                RecentFiles.Add(new RecentFileEntry
                {
                    FilePath = fullPath,
                    LastUsedUtcTicks = nowUtcTicks
                });
            }

            TrimAndCleanup();
        }

        private void TrimAndCleanup()
        {
            RecentFiles = RecentFiles
                .Where(x => !string.IsNullOrWhiteSpace(x.FilePath) && File.Exists(x.FilePath))
                .OrderByDescending(x => x.LastUsedUtcTicks)
                .Take(MaxRecent)
                .ToList();
        }
    }

    public sealed class WindowPlacement
    {
        public int X { get; set; } = int.MinValue;
        public int Y { get; set; } = int.MinValue;
        public int Width { get; set; } = int.MinValue;
        public int Height { get; set; } = int.MinValue;

        // store as int to avoid enum serialization quirks
        public int WindowState { get; set; } = 0; // 0=Normal, 1=Minimized, 2=Maximized
    }

    public sealed class RecentFileEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public long LastUsedUtcTicks { get; set; }

        public string FileName
        {
            get
            {
                try { return Path.GetFileName(FilePath) ?? string.Empty; }
                catch { return string.Empty; }
            }
        }



        public string LastUsedLocalFormatted
        {
            get
            {
                try
                {
                    var dto = new DateTimeOffset(LastUsedUtcTicks, TimeSpan.Zero).ToLocalTime();
                    return dto.ToString("dd.MM.yyyy HH:mm:ss");
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}
