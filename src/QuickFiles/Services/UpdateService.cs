using Velopack;
using Velopack.Sources;

namespace QuickFiles.Services;

public static class UpdateService
{
    private const string RepoUrl = "https://github.com/TheSaltyKorean/files";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(4);

    private static UpdateManager? _manager;

    public static string CurrentVersionDisplay =>
        _manager?.CurrentVersion?.ToString() ?? "dev build (not installed)";

    /// <summary>
    /// Checks GitHub Releases in the background every few hours. When an update
    /// has been downloaded, waits until the flyout is hidden and silently
    /// restarts into the new version (with --hidden, so nothing pops up).
    /// </summary>
    public static void Start(App app)
    {
        UpdateManager manager;
        try
        {
            manager = new UpdateManager(new GithubSource(RepoUrl, null, false));
        }
        catch
        {
            return;
        }
        if (!manager.IsInstalled)
            return; // running from a dev build; updates don't apply

        _manager = manager;

        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var update = await manager.CheckForUpdatesAsync();
                    if (update != null)
                    {
                        await manager.DownloadUpdatesAsync(update);
                        await WhenFlyoutHiddenAsync(app);
                        manager.ApplyUpdatesAndRestart(update, new[] { "--hidden" });
                        return;
                    }
                }
                catch
                {
                    // Offline or GitHub unreachable; try again next cycle.
                }
                await Task.Delay(CheckInterval);
            }
        });
    }

    /// <summary>
    /// Manual check from the settings window. Returns a status message; if an
    /// update is found it is applied a moment later with a silent restart.
    /// </summary>
    public static async Task<string> CheckNowAsync()
    {
        var manager = _manager;
        if (manager == null)
            return "Updates are only available in the installed app.";

        try
        {
            var update = await manager.CheckForUpdatesAsync();
            if (update == null)
                return $"You're up to date (v{manager.CurrentVersion}).";

            await manager.DownloadUpdatesAsync(update);
            var newVersion = update.TargetFullRelease.Version;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                manager.ApplyUpdatesAndRestart(update, new[] { "--hidden" });
            });
            return $"Updating to v{newVersion}… QuickFiles will restart in the background.";
        }
        catch (Exception ex)
        {
            return "Update check failed: " + ex.Message;
        }
    }

    private static async Task WhenFlyoutHiddenAsync(App app)
    {
        while (true)
        {
            var visible = await app.Dispatcher.InvokeAsync(() => app.IsFlyoutVisible);
            if (!visible)
                return;
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }
}
