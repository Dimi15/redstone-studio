using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using redstone_stone.ViewModels;
using redstone_stone.Views;

namespace redstone_stone;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Load the icon explicitly before showing the window.
            // On macOS this ensures the dock icon is set correctly by Avalonia
            // before NSApplication processes its event loop.
            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://redstoneStudio.UI/icon/redstone.png"));
                window.Icon = new WindowIcon(stream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Could not load app icon: " + ex.Message);
            }

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
