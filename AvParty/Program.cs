using Avalonia;
using System;
using System.Diagnostics;
using System.IO;

namespace AvParty;

internal abstract class Program
{
    /// <summary>
    /// If enabled, this app will write current playing song name to *mas_root*/songname.txt 
    /// </summary>
    public static bool StreamMode = false;


    private static readonly bool VerboseLogging = Debugger.IsAttached || File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".MAS_PARTY_VERBOSE"));


    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Log("Log start");
        Log("Initializing");
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

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
            ".MAS_PARTY_VERBOSE");
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