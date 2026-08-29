using System;
using System.Diagnostics;

namespace AvControlPanel.Models.Menu;

// <Shell> tags in ScriptMenu.xml
public class Shell
{
    public string Platform { get; set; } = "Agnostic";
    public string Status { get; set; } = "";
    public bool Detach { get; set; } = false;

    public string Directory
    {
        get;
        set
        {
            value = value.Replace("%MAS_ROOT%",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas");
            field = value;
        }
    } = Environment.CurrentDirectory;

    public string? Command { get; set; }

    public void Run()
    {
        if (Command == null) return;
        if (!OperatingSystem.IsLinux()) Platform = "Agnostic";
        var actualPlatform = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
        if (actualPlatform == null && Platform != "Agnostic") return;
        if (actualPlatform != Platform && Platform != "Agnostic") return;

        var cmd = Command;
        var args = "";
        if (cmd.Contains(' '))
        {
            cmd = cmd.Split(' ')[0];
            args = string.Join(' ', Command.Split(' ')[1..]);
        }
        if (Command.StartsWith('"') && (!OperatingSystem.IsLinux() || !Detach))
        {
            cmd = Command.Split('"')[1];
            var sToken = Command.Split('"')[2];
            if (sToken.Contains(' '))
            {
                args = sToken;
            }
        }

        if (cmd.Contains(' '))
        {
            cmd = "\"" + cmd + "\"";
        }

        if (OperatingSystem.IsLinux() && Detach)
        {
            var actualArgs = $"-c \"nohup {cmd.Replace("\"", "\\\"")} {args.Replace("\"", "\\\"")} > /dev/null 2>&1 &\"";
            cmd = "/usr/bin/bash";
            args = actualArgs;
        }

        Program.Log($"Running shell command: '{cmd}' with arguments '{args}'");
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = args,
                WorkingDirectory = Directory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }
        };
        p.Start();
        if (!Detach)
        {
            p.WaitForExit();
        }
    }
}