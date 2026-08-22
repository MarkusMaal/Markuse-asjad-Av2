using Avalonia;
using System;
using System.Diagnostics;
using System.IO;

namespace AvIntegrationSoftware;

internal abstract class Program
{
    public static bool CodeOpen = false;

    public static bool AllowCode
    {
        get => App.Features.Contains("RD") && field;
        set;
    } = true;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        } catch (Exception ex) when (!Debugger.IsAttached)
        {
            var exePath = Environment.ProcessPath;
            if (!OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo(exePath!) { UseShellExecute = true, Arguments = "/e" });
            }
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/mas_error.log", $"----------------------------------------------\nMarkuse arvuti integratsioonitarkvara\n----------------------------------------------\n\nPeatasime Markuse arvuti integratsioonitarkvara probleemi tõttu. Palun käivitage see programm siluriga, et asja täpsemalt uurida.\n\nTehniline info:\n\nRakendus: {exePath ?? "?"}\nKuupäev ja kellaaeg: {DateTime.Now}\nErand: {ex.Message}\nKuhila jälg:\n{ex.StackTrace}");
            Environment.Exit(0);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions { ShowInDock = false, DisableDefaultApplicationMenuItems = true })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}