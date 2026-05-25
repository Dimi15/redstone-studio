using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using redstone_stone.Domain.Enums;
using redstone_stone.ViewModels;

namespace redstone_stone.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    // ── Toolbar ──────────────────────────────────────────────────────────────

    private void OnToolClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            Vm.SelectToolCommand.Execute(tag);
    }

    private void OnDirectionClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag &&
            Enum.TryParse<Direction>(tag, out var dir))
        {
            Vm.SelectedDirection = dir;
        }
    }

    // ── Simulation bar ───────────────────────────────────────────────────────

    private void OnPlayClicked(object? sender, RoutedEventArgs e)  => Vm.PlayCommand.Execute(null);
    private void OnPauseClicked(object? sender, RoutedEventArgs e) => Vm.PauseCommand.Execute(null);

    // ── File dialogs ─────────────────────────────────────────────────────────

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = "Save Circuit",
            DefaultExtension  = "json",
            SuggestedFileName = "circuit",
            FileTypeChoices   = new[] { new FilePickerFileType("JSON Circuit") { Patterns = new[] { "*.json" } } }
        });

        if (file is not null)
            await Vm.SaveCircuit(file.Path.LocalPath);
    }

    private async void OnLoadClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Load Circuit",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON Circuit") { Patterns = new[] { "*.json" } } }
        });

        if (files.Count > 0)
            await Vm.LoadCircuit(files[0].Path.LocalPath);
    }
}
