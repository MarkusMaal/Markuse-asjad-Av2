using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AvControlPanel.Models;
using MasCommon;

namespace AvControlPanel;

class Program
{
    private static readonly Verifile Vf = new();

    public static string Status = "BYPASS";

    
    private static readonly bool VerboseLogging = Debugger.IsAttached || File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".MAS_CPANEL_LOG_VERBOSE"));
    public static List<string> AvailableIcons { get; set; } = [];
    
    public static Stopwatch InitStopwatch { get; set; } = new();
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        InitStopwatch.Start();
        if (VerboseLogging)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("      \u25CF");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("     \u25CF ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\u25CF");
            Console.ResetColor();
            Console.Write("  juhtpaneel");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" {Edition.CpanelVersion}");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("      \u25CF");
            Console.ResetColor();
            Console.WriteLine();
        }

        Log("System information");
        var osVer = Environment.OSVersion.ToString();
        if (OperatingSystem.IsLinux()) osVer = osVer.Replace("Unix", "Linux"); // Linux is not Unix lol
        Log($"  Operating system: {osVer}");
        Log($"  Username: {Environment.UserName}");
        Log($"  Machine name: {Environment.MachineName}");
        Log($"  Process ID: {Environment.ProcessId}");
        Log($"  Runtime version: {Environment.Version}");
        Log($"  Logical processors: {Environment.ProcessorCount}");
        Log($"  Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLower()}");
        Log($"  Command line: {Environment.CommandLine}");
        Log($"  Working directory: {Environment.CurrentDirectory}");
        Log($"  Build date: {GetAssemblyBuildDateTime()}");
        Log("Running Avalonia app builder");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
    
    public static DateTime? GetAssemblyBuildDateTime()
    {
        var buildDateString = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDateTime")?.Value;

        DateTime? buildDate = DateTime.TryParseExact(buildDateString, "s",
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var dt) ? dt : null;

        return buildDate;
    }

    public static void MakeAttestation()
    {
        Status = Vf.MakeAttestation();
    }


    public static bool CheckFiles(Verifile.FileScope scope)
    {
        return Verifile.CheckFiles(scope);
    }
    // Windows-only, will not work on other operating systems
    public static void StartWin32Process(string filename)
    {
        if (!OperatingSystem.IsWindows()) return;
        Log($"Starting Win32 process: {filename}");
        var p = new Process();
        p.StartInfo.FileName = "cmd";
        p.StartInfo.Arguments = "/c start " + filename;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.Start();
    }
    
    
    public static void RunCommand(string command, string args, bool waitForExit = true) {
        Log($"Running command: '{command}' with args '{args}' (wait for exit: {waitForExit}");
        var p = new Process();
        p.StartInfo.FileName = command;
        p.StartInfo.Arguments = args;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.Start();
        if (waitForExit) {
            p.WaitForExit();
        }
    }
    public static void SendDesktopIconCommand(string type, string args = "")
    {
        if (Process.GetProcessesByName("DesktopIcons").Length == 0) return;
        Log($"Sending a command to desktop manager: {type} {args}");
        DesktopCommand cmd = new()
        {
            Arguments = args,
            Type = type
        };
        cmd.Send(App.MasRoot);
    }

    public static void Log(string message)
    {
        if (!VerboseLogging) return;
        var logFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".MAS_CPANEL_LOG_VERBOSE");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var dateStr = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}]";
        Console.Write($"\r{dateStr} ");
        Console.ResetColor();
        Console.WriteLine(message);
        if (File.Exists(logFile))
        {
            File.AppendAllText(logFile, $"{dateStr} {message}\n");
        }
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}