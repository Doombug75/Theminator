namespace Theminator;

using System.IO;
using System.Windows;
using Microsoft.Win32;
using Theminator.Models;

public partial class OptionsWindow : Window
{
    private readonly AppSettings _settings;
    public string SelectedFolder { get; private set; } = string.Empty;

    public OptionsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        FolderBox.Text = settings.ThemesFolder;
        SelectedFolder = settings.ThemesFolder;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Themes Folder",
            InitialDirectory = string.IsNullOrEmpty(_settings.ThemesFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : _settings.ThemesFolder
        };

        if (dlg.ShowDialog(this) == true)
        {
            FolderBox.Text = dlg.FolderName;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var folder = FolderBox.Text.Trim();
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
        {
            MessageBox.Show(this, "The specified folder does not exist.", "Invalid Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SelectedFolder = folder;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
