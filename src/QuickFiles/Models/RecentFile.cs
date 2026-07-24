using System.Windows.Media;

namespace QuickFiles.Models;

public enum FileOrigin
{
    Downloaded,
    Opened,
}

public sealed class RecentFile
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string Folder { get; init; }
    public required DateTime Timestamp { get; init; }
    public required FileOrigin Origin { get; init; }

    public ImageSource? Icon { get; set; }

    public string OriginDisplay => Origin == FileOrigin.Downloaded ? "Downloaded" : "Opened";

    public string TimeDisplay
    {
        get
        {
            var now = DateTime.Now;
            if (Timestamp.Date == now.Date)
                return Timestamp.ToString("t");
            if (Timestamp.Date == now.Date.AddDays(-1))
                return "Yesterday " + Timestamp.ToString("t");
            if (Timestamp > now.AddDays(-6))
                return Timestamp.ToString("ddd t");
            if (Timestamp.Year == now.Year)
                return Timestamp.ToString("MMM d");
            return Timestamp.ToString("MMM d, yyyy");
        }
    }
}
