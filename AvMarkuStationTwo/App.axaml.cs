using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvMarkuStationTwo;

public class App : Application
{
    private MainWindow? MainWindow { get; set; }
    public override void Initialize()
    {
        Program.Log("Running Avalonia XAML loader");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Program.Log("Initializing main window");
        MainWindow = new MainWindow();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void NativeMenuItem_OnClick(object? sender, EventArgs e)
    {
        MainWindow?.Close();
    }
}