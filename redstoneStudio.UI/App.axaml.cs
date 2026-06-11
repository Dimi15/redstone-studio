using System;
using System.IO;
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
            MainWindow window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            try
            {
                using (Stream iconStream = AssetLoader.Open(new Uri("avares://redstoneStudio.UI/icon/redstone.png")))
                {
                    window.Icon = new WindowIcon(iconStream);
                }
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
