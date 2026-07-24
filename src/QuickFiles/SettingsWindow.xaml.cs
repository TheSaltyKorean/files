using System.Windows;
using QuickFiles.Services;

namespace QuickFiles;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var settings = AppSettings.Load();
        MaxItemsSlider.Value = settings.MaxItems;
        SortCombo.SelectedIndex = (int)settings.SortOrder;
        IncludeDownloadsCheck.IsChecked = settings.IncludeDownloads;
        IncludeRecentCheck.IsChecked = settings.IncludeRecent;
        VersionText.Text = "QuickFiles " + UpdateService.CurrentVersionDisplay;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            MaxItems = (int)MaxItemsSlider.Value,
            SortOrder = (SortOrder)Math.Max(0, SortCombo.SelectedIndex),
            IncludeDownloads = IncludeDownloadsCheck.IsChecked == true,
            IncludeRecent = IncludeRecentCheck.IsChecked == true,
        };
        settings.Save();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "Checking for updates…";
        UpdateStatusText.Text = await UpdateService.CheckNowAsync();
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
