using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;

// ReSharper disable UnusedMember.Global

namespace AvIntegrationSoftware;

public abstract class DefaultActions
{
    public static void ParseStr(string str)
    {
        Program.Log($"Execute default action {str}");
        switch (str)
        {
            case "ToggleDesktopNotes":
                ToggleDesktopNotes();
                break;
            case "OpenHomeDir":
                OpenHomeDir();
                break;
            case "Exit":
                App.Exit();
                break;
            case "ToggleAllowCode":
                Program.AllowCode = !Program.AllowCode;
                break;
            case "MenuEditor":
                new MenuEditor()
                {
                    Background = new SolidColorBrush(App.Scheme[0]),
                    Foreground = new SolidColorBrush(App.Scheme[1]),
                    RootPanel =
                    {
                        Background = new ImageBrush(new Bitmap(Path.Join(App.MasRoot, "bg_common.png")))
                        {
                            Stretch = Stretch.UniformToFill,
                        },
                    }
                }.Show();
                break;
            case "FlashAutorun":
                foreach (var di in DriveInfo.GetDrives())
                {
                    var driveRoot = di.RootDirectory.FullName;
                    if (!di.IsReady || !File.Exists(Path.Join(driveRoot, "E_INFO", "edition.txt"))) continue;
                    var processorArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
                    var os = OperatingSystem.IsWindows() ? "win" :
                        OperatingSystem.IsMacOS() ? "osx" :
                        OperatingSystem.IsLinux() ? "linux" : string.Empty;
                    var osExec = os switch
                    {
                        "win" => "Markuse mälupulk 2.0.exe",
                        "linux" => "Markuse mälupulk 2.0",
                        "osx" => "Markuse mälupulk 2.0.app",
                        _ => ""
                    };
                    var fullPath = Path.Join(driveRoot, "Mälupulga juhtpaneel",
                        $"{os}-{processorArchitecture.ToString().ToLower()}", osExec);
                    var p = new Process();
                    if (File.Exists(fullPath) && OperatingSystem.IsWindows())
                    {
                        p.StartInfo.FileName = Path.Join(fullPath);
                    }
                    else if (File.Exists(fullPath) && OperatingSystem.IsLinux())
                    {
                        File.Copy(fullPath, Path.Join(App.MasRoot, osExec), true);
                        File.SetUnixFileMode(Path.Join(App.MasRoot, osExec), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.UserExecute | UnixFileMode.GroupExecute);
                        p.StartInfo.FileName = Path.Join(App.MasRoot, osExec);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        p.StartInfo.FileName = "open";
                        p.StartInfo.Arguments = $"-a \"{fullPath}\"";
                    }
                    
                    Program.Log($"Attempting to start process \"{p.StartInfo.FileName}\" with args \"{p.StartInfo.Arguments}\"");

                    if (File.Exists(fullPath) || Directory.Exists(fullPath))
                    {
                        p.Start();
                    }
                    break;
                }
                break;
            case "ShowAbout":
                App.ShowAbout();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private static void OpenHomeDir()
    {
        Program.Log($"Opening home directory");
        var p = new Process();
        p.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }

    private static void ToggleDesktopNotes()
    {
        var suff = OperatingSystem.IsWindows() ? ".exe" : "";
        if (File.Exists(Path.Join(App.MasRoot, "noteopen.txt")))
        {
            Program.Log($"Closing desktop notes");
            File.Delete(Path.Join(App.MasRoot, "noteopen.txt"));
            File.WriteAllText(Path.Join(App.MasRoot, "closenote.log"), "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
            return;
        }
        Program.Log($"Opening desktop notes");
        File.WriteAllText(Path.Join(App.MasRoot, "noteopen.txt"), "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
        Process.Start(Path.Join(App.MasRoot, "Markuse asjad", "TöölauaMärkmed" + suff));
    }
}