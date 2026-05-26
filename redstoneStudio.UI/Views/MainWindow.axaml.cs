using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    // Toolbar

    private void OnToolClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            Vm.SelectToolCommand.Execute(tag);
        }
    }

    private void OnDirectionClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            if (Enum.TryParse<Direction>(tag, out var dir))
            {
                Vm.SelectedDirection = dir;
            }
        }
    }

    // Simulation bar

    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        Vm.PlayCommand.Execute(null);
    }

    private void OnPauseClicked(object? sender, RoutedEventArgs e)
    {
        Vm.PauseCommand.Execute(null);
    }
}
