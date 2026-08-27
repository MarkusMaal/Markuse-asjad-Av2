using System;
using System.IO;
using System.Linq;

namespace AvIntegrationSoftware;

public class TaskScheduler
{
    public ScheduledTask[]? Tasks { get; set; }
    public TaskScheduler()
    {
        ReloadTasks();
    }

    public void ReloadTasks()
    {
        var taskData = "";
        if (File.Exists(Path.Join(App.MasRoot, "events.txt")))
        {
            var fileStream = File.OpenText(Path.Join(App.MasRoot, "events.txt"));
            taskData = fileStream.ReadToEnd();
            fileStream.Close();
        }

        var splitTaskData = taskData.Split(';');
        Tasks = new ScheduledTask[splitTaskData.Length - 1];
        foreach (var (i, splitTask) in splitTaskData.Take(splitTaskData.Length - 1).Index())
        {
            var subSplit = splitTask.Split('-');
            var triggerDate = new DateTime(int.Parse(subSplit[2]),  int.Parse(subSplit[1]), int.Parse(subSplit[0]), int.Parse(subSplit[3]), int.Parse(subSplit[4]), int.Parse(subSplit[5]));
            var script = subSplit[6];
             
            var close = subSplit.Length > 7 && subSplit[7] == "true";
            Tasks[i] = new ScheduledTask
            {
                TriggerTime = triggerDate,
                ShellScript = script,
                Shutdown = close
            };
        }
        Program.Log($"Initialized {Tasks.Length} scheduled tasks");
    }
}