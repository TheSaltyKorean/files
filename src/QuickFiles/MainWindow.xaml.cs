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

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            if (IsVisible)
                PositionNearTaskbar();
        };
    }

    public void ShowFlyout()
    {
        RefreshList();
        Show();
        UpdateLayout();
        PositionNearTaskbar();
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

    private void PositionNearTaskbar()
    {
        // The OS work area excludes the taskbar, so the bottom-right corner of
        // it sits just above/beside the taskbar wherever it is docked.
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 4;
        Top = workArea.Bottom - ActualHeight - 4;
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
        try
        {
            Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
        }
        catch
        {
            TryShellOpen(file.Folder);
        }
        e.Handled = true;
        Hide();
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
