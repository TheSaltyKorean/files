using System.IO;
using QuickFiles.Models;

namespace QuickFiles.Services;

public static class FileScanner
{
    private static readonly string[] IgnoredExtensions =
    {
        ".tmp", ".crdownload", ".partial", ".part", ".download", ".lnk", ".url", ".ini",
    };

    /// <summary>
    /// Merges the Downloads folder and Windows Recent items, dedupes by path,
    /// keeps the newest MaxItems entries, and orders them per the settings.
    /// </summary>
    public static List<RecentFile> Scan(AppSettings settings)
    {
        var byPath = new Dictionary<string, RecentFile>(StringComparer.OrdinalIgnoreCase);

        if (settings.IncludeDownloads)
        {
            foreach (var file in ScanDownloads())
                Merge(byPath, file);
        }

        if (settings.IncludeRecent)
        {
            foreach (var file in ScanRecentItems())
                Merge(byPath, file);
        }

        var newest = byPath.Values
            .OrderByDescending(f => f.Timestamp)
            .Take(settings.MaxItems)
            .ToList();

        return settings.SortOrder switch
        {
            SortOrder.OldestFirst => newest.OrderBy(f => f.Timestamp).ToList(),
            SortOrder.NameAscending => newest.OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            _ => newest,
        };
    }

    private static void Merge(Dictionary<string, RecentFile> byPath, RecentFile file)
    {
        if (!byPath.TryGetValue(file.FullPath, out var existing) || file.Timestamp > existing.Timestamp)
            byPath[file.FullPath] = file;
    }

    private static List<RecentFile> ScanDownloads()
    {
        var results = new List<RecentFile>();
        var downloadsDir = ShellInterop.GetDownloadsPath();
        if (downloadsDir == null || !Directory.Exists(downloadsDir))
            return results;

        foreach (var fileInfo in new DirectoryInfo(downloadsDir).EnumerateFiles())
        {
            try
            {
                if ((fileInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;
                if (IgnoredExtensions.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
                    continue;

                // CreationTime is when the file landed in Downloads; LastWriteTime
                // can be older (it is preserved from the source for some downloads).
                var timestamp = fileInfo.CreationTime > fileInfo.LastWriteTime
                    ? fileInfo.CreationTime
                    : fileInfo.LastWriteTime;

                results.Add(new RecentFile
                {
                    FullPath = fileInfo.FullName,
                    Name = fileInfo.Name,
                    Folder = downloadsDir,
                    Timestamp = timestamp,
                    Origin = FileOrigin.Downloaded,
                });
            }
            catch
            {
                // Skip files we cannot stat.
            }
        }
        return results;
    }

    private static List<RecentFile> ScanRecentItems()
    {
        var results = new List<RecentFile>();
        var recentDir = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        if (string.IsNullOrEmpty(recentDir) || !Directory.Exists(recentDir))
            return results;

        foreach (var lnk in new DirectoryInfo(recentDir).EnumerateFiles("*.lnk"))
        {
            try
            {
                var target = ShellInterop.ResolveShortcut(lnk.FullName);
                if (target == null || !File.Exists(target))
                    continue; // skip folders, unresolvable links, and deleted files

                results.Add(new RecentFile
                {
                    FullPath = target,
                    Name = Path.GetFileName(target),
                    Folder = Path.GetDirectoryName(target) ?? string.Empty,
                    // The shortcut's write time is when Windows last recorded the
                    // file being opened.
                    Timestamp = lnk.LastWriteTime,
                    Origin = FileOrigin.Opened,
                });
            }
            catch
            {
                // Skip anything malformed.
            }
        }
        return results;
    }
}
