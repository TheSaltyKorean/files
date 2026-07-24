using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using QuickFiles.Models;
using QuickFiles.Services;

namespace QuickFiles;

public partial class MainWindow : Window
{
    private SettingsWindow? _settingsWindow;
    private ShellInterop.FlyoutAnchor? _anchor;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            if (IsVisible)
                PositionFlyout();
        };
    }

    public void ShowFlyout()
    {
        // Capture the cursor now: the user just clicked the taskbar icon, so
        // the cursor marks where the flyout should be anchored.
        _anchor = ShellInterop.GetFlyoutAnchor();
        RefreshList();
        Show();
        UpdateLayout();
        PositionFlyout();
        Activate();
    }

    private void RefreshList()
    {
        var settings = AppSettings.Load();
        var files = FileScanner.Scan(settings);
        foreach (var file in files)
            file.Icon = ShellInterop.GetFileIcon(file.FullPath);

        FilesList.ItemsSource = files;
        EmptyText.Visibility = files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // The window is 12 DIPs larger than the visible card on every side (the
    // shadow margin), so offsets compensate to leave a small visible gap.
    private const double ShadowMarginDip = 12;
    private const double VisibleGapDip = 4;

    private void PositionFlyout()
    {
        if (_anchor is not { } anchor
            || PresentationSource.FromVisual(this)?.CompositionTarget is not { } target)
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - ActualWidth - 4;
            Top = wa.Bottom - ActualHeight - 4;
            return;
        }

        var scale = target.TransformToDevice;
        double widthPx = ActualWidth * scale.M11;
        double heightPx = ActualHeight * scale.M22;
        double edgePadX = (VisibleGapDip - ShadowMarginDip) * scale.M11;
        double edgePadY = (VisibleGapDip - ShadowMarginDip) * scale.M22;

        var work = anchor.Work;
        var mon = anchor.Monitor;
        double leftPx, topPx;

        // The side where the work area is inset from the monitor bounds is
        // where the taskbar is docked. Center the flyout on the cursor along
        // that edge, i.e. directly above/beside the icon that was clicked.
        if (work.Bottom < mon.Bottom)
        {
            topPx = work.Bottom - heightPx - edgePadY;
            leftPx = anchor.Cursor.X - widthPx / 2;
        }
        else if (work.Top > mon.Top)
        {
            topPx = work.Top + edgePadY;
            leftPx = anchor.Cursor.X - widthPx / 2;
        }
        else if (work.Left > mon.Left)
        {
            leftPx = work.Left + edgePadX;
            topPx = anchor.Cursor.Y - heightPx / 2;
        }
        else if (work.Right < mon.Right)
        {
            leftPx = work.Right - widthPx - edgePadX;
            topPx = anchor.Cursor.Y - heightPx / 2;
        }
        else
        {
            // Auto-hidden taskbar: no inset anywhere; assume bottom.
            topPx = work.Bottom - heightPx - edgePadY;
            leftPx = anchor.Cursor.X - widthPx / 2;
        }

        leftPx = Math.Clamp(leftPx, work.Left + edgePadX,
            Math.Max(work.Left + edgePadX, work.Right - widthPx - edgePadX));
        topPx = Math.Clamp(topPx, work.Top + edgePadY,
            Math.Max(work.Top + edgePadY, work.Bottom - heightPx - edgePadY));

        var toDip = target.TransformFromDevice;
        Left = leftPx * toDip.M11;
        Top = topPx * toDip.M22;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow();
        _settingsWindow.Show();
    }

    private void File_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not RecentFile file)
            return;
        TryShellOpen(file.FullPath);
        Hide();
    }

    private void Folder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Hyperlink)?.Tag is not RecentFile file)
            return;
        e.Handled = true;
        RevealInExplorer(file);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not RecentFile file)
            return;
        e.Handled = true;
        RevealInExplorer(file);
    }

    private void RevealInExplorer(RecentFile file)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
        }
        catch
        {
            TryShellOpen(file.Folder);
        }
        Hide();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as Button)?.Tag is not RecentFile file)
            return;
        ShellInterop.MoveToRecycleBin(file.FullPath);
        RefreshList();
    }

    private static void TryShellOpen(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // File may have been deleted or has no associated app; nothing to do.
        }
    }
}
