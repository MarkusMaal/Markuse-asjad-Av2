using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Threading;

namespace AvIntegrationSoftware;

public class MenuItemModel
{
    public string? MenuIdentifier { get; set; }
    public MenuItemModel[]? SubItems { get; set; }

    private string CurrentState { get; set; } = "Default";
    public required MenuState[] States { get; set; }
    
    public MenuState? GetDefault() => States.FirstOrDefault(p => p.StateIdentifier == "Default");
    public MenuState? GetState()  => States.FirstOrDefault(p => p.StateIdentifier == CurrentState);
    public MenuState[] GetAll() => States;
    
    public string? StatePoller { get; set; }

    public string? RequiredFeatures { get; set; }
    private static bool _alreadyLaunched = true; // set to true to avoid flash auto-launch when program first starts 
    
    public void Execute()
    {
        var actionExpression = GetState()?.Action;
        if (actionExpression == null) return;
        ((App?)Application.Current)?.ToggleBusy(true);
        var actionType = actionExpression.Split("::")[0];
        var actionRunnable = actionExpression.Split("::")[1];
        switch (actionType)
        {
            case "default":
                DefaultActions.ParseStr(actionRunnable);
                break;
            case "shell":
                Program.Log($"Running shell command '{actionRunnable}'");
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = actionRunnable.Split(' ')[0],
                        Arguments = string.Join(' ', actionRunnable.Split(' ').Skip(1).ToArray()),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = Path.Join(App.MasRoot, "Markuse asjad"),
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };
                p.Start();
                break;
            case "web":
                Program.Log($"Launching URL {actionRunnable}");
                try
                {
                    Process.Start(actionRunnable);
                }
                catch
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var url = actionRunnable.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", actionRunnable);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", actionRunnable);
                    }
                }
                break;
        }

        new Thread(() =>
        {
            Thread.Sleep(100);
            Dispatcher.UIThread.Post(() => ((App?)Application.Current)?.ToggleBusy(false));
        }).Start();
    }

    public bool HasRequiredFeatures() => (RequiredFeatures?.Split('-') ?? []).All(segment => App.Features.Contains(segment));
    
    public void PollState()
    {
        if (StatePoller == null) return;
        StatePoller = StatePoller.Replace("%MAS_ROOT%", App.MasRoot).Replace(" ? ", "?").Replace(" : ", ":");
        var checkable = StatePoller.Split('(')[1].Split(')')[0];
        var yesLabel = StatePoller.Split('?')[1].Split(':')[0];
        var noLabel = StatePoller.Split(':')[1];
        var previousState = CurrentState;
        switch (StatePoller.Split('(')[0])
        {
            case "FILE_EXISTS":
                CurrentState = File.Exists(checkable) ? yesLabel : noLabel;
                if ((previousState != CurrentState)) Program.Log($"State change - Polled FILE_EXISTS({checkable}), Result: {CurrentState}");
                return;
            case "IS_TRUE":
                CurrentState = checkable switch
                {
                    "AllowCode" => Program.AllowCode ? yesLabel : noLabel,
                    "CodeOpen" => Program.CodeOpen ? yesLabel : noLabel,
                    "FlashDrivesAvailable" => AreThereAnyFlashDrivesMounted() ? yesLabel : noLabel,
                    "FlashAutorun" => Program.FlashAutorun ? yesLabel : noLabel, 
                    _ => CurrentState
                };
                if ((previousState != CurrentState)) Program.Log($"State change - Polled IS_TRUE({checkable}), Result: {CurrentState}");
                return;
            case "PROCESS_RUNNING":
                CurrentState = Process.GetProcessesByName(checkable).Length > 0 ? yesLabel : noLabel;
                if ((previousState != CurrentState)) Program.Log($"State change - Polled PROCESS_RUNNING({checkable}), Result: {CurrentState}");
                return;
        }
    }

    private static bool AreThereAnyFlashDrivesMounted()
    {
        var result = DriveInfo.GetDrives().Any(di => File.Exists(Path.Join(di.RootDirectory.FullName, "E_INFO", "edition.txt")) && File.Exists(Path.Join(di.RootDirectory.FullName, "NTFS", "config.sys")));
        if (!Program.FlashAutorun) return result;
        switch (result)
        {
            case true when !_alreadyLaunched:
                Program.Log("Stopped searching for flash drives");
                DefaultActions.ParseStr("FlashAutorun");
                _alreadyLaunched = true;
                break;
            case false when _alreadyLaunched:
                Program.Log("Started searching for flash drives");
                _alreadyLaunched = false;
                break;
        }
        return result;
    }

    public void SetState(string newState)
    {
        CurrentState = newState;
    }
}

public class MenuState
{
    public required string StateIdentifier { get; set; }
    public required string Label { get; set; }
    public required string IconPath { get; set; }
    public required string Action { get; set; }
}
