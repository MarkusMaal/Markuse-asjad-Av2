using System;
using System.Diagnostics;
using System.Threading;

namespace AvIntegrationSoftware;

public class ScheduledTask
{
    public DateTime TriggerTime { get; init; }
    public required string ShellScript { get; init; }
    public bool Shutdown { get; init; }

    public bool HasRun { get; set; }
    private bool HasExecuted { get; set; }

    public void RunTask()
    {
        if (HasRun) return;
        HasRun = true;
        Program.Log("Triggered scheduled task");
        new Thread(() =>
        {
            while (DateTime.Now < TriggerTime)
            {
                Program.Log("Scheduled task is not supposed to run yet, wait an additional second");
                Thread.Sleep(1000);
            }

            if (HasExecuted) return; // this is fine, because scheduled tasks never repeat unless you update the events.txt file at which point this flag gets reset
            HasExecuted = true;
            var p = new Process();
            p.StartInfo.FileName = ShellScript;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Program.Log($"Attempting to run action tied to scheduled task: new process '{p.StartInfo.FileName}'");
            p.Start();
            if (Shutdown) App.Exit();
        }).Start();
    }
}