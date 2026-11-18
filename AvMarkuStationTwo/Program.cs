using Avalonia;
using System;
using System.Diagnostics;
using System.IO;

namespace AvMarkuStationTwo;

internal abstract class Program
{
    
    private static readonly bool VerboseLogging = Debugger.IsAttached || File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".MAS_MS2_VERBOSE"));

    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    public static void Log(string message)
    {
        if (!VerboseLogging) return;
        var logFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".MAS_MS2_VERBOSE");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var dateStr = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}";
        Console.Write($"\r[{dateStr}] ");
        Console.ResetColor();
        Console.WriteLine(message);
        if (Debugger.IsAttached) Debug.WriteLine($"[{System.Reflection.Assembly.GetExecutingAssembly().EntryPoint?.DeclaringType?.Namespace}] {message}");
        if (File.Exists(logFile))
        {
            File.AppendAllText(logFile, $"[{dateStr}] {message}\n");
        }
    }
}