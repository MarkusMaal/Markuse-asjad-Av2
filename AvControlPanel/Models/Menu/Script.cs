using System;
using System.Threading;

namespace AvControlPanel.Models.Menu;

// <Script> tags in ScriptMenu.xml
public class Script
{
    public bool Wait { get; set; } = false;
    private Shell[]? Commands { get; set; }

    public void AddCommand(Shell command)
    {
        Program.Log($"Adding shell command to script: '{command.Command}'");
        Commands ??= [];
        var cmds = new Shell[Commands.Length + 1];
        Array.Copy(Commands, cmds, Commands.Length);
        cmds[Commands.Length] = command;
        Commands = cmds;
    }

    public void Run()
    {
        Program.Log("Running script");
        if (Commands == null) return;
        foreach (var cmd in Commands)
        {
            if (Wait) cmd.Run();
            else new Thread(cmd.Run).Start();
        }
        Program.Log("Script finished");
    }
}