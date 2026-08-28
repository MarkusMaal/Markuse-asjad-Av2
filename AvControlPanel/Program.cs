using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MasCommon;

namespace AvControlPanel;

class Program
{
    private static readonly Verifile Vf = new();

    public static string Status = "BYPASS";

    public static List<string> AvailableIcons { get; set; } = [];
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
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
        var p = new Process();
        p.StartInfo.FileName = "cmd";
        p.StartInfo.Arguments = "/c start " + filename;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.Start();
    }
    
    
    public static void RunCommand(string command, string args, bool waitForExit = true) {
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
        DesktopCommand cmd = new()
        {
            Arguments = args,
            Type = type
        };
        cmd.Send(App.MasRoot);
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