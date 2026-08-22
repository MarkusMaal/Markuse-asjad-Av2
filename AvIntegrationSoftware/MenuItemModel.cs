using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

    public void Execute()
    {
        var actionExpression = GetState()?.Action;
        if (actionExpression == null) return;
        var actionType = actionExpression.Split("::")[0];
        var actionRunnable = actionExpression.Split("::")[1];
        switch (actionType)
        {
            case "default":
                DefaultActions.ParseStr(actionRunnable);
                break;
            case "shell":
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
    }

    public bool HasRequiredFeatures() => (RequiredFeatures?.Split('-') ?? []).All(segment => App.Features.Contains(segment));
    
    public void PollState()
    {
        if (StatePoller == null) return;
        StatePoller = StatePoller.Replace("%MAS_ROOT%", App.MasRoot).Replace(" ? ", "?").Replace(" : ", ":");
        var checkable = StatePoller.Split('(')[1].Split(')')[0];
        var yesLabel = StatePoller.Split('?')[1].Split(':')[0];
        var noLabel = StatePoller.Split(':')[1];
        switch (StatePoller.Split('(')[0])
        {
            case "FILE_EXISTS":
                CurrentState = File.Exists(checkable) ? yesLabel : noLabel;
                return;
            case "IS_TRUE":
                CurrentState = checkable switch
                {
                    "AllowCode" => Program.AllowCode ? yesLabel : noLabel,
                    "CodeOpen" => Program.CodeOpen ? yesLabel : noLabel,
                    "FlashDrivesAvailable" => AreThereAnyFlashDrivesMounted() ? yesLabel : noLabel, 
                    _ => CurrentState
                };
                return;
            case "PROCESS_RUNNING":
                CurrentState = Process.GetProcessesByName(checkable).Length > 0 ? yesLabel : noLabel;
                return;
        }
    }

    private static bool AreThereAnyFlashDrivesMounted()
    {
        return DriveInfo.GetDrives().Any(di => File.Exists(Path.Join(di.RootDirectory.FullName, "E_INFO", "edition.txt")) && File.Exists(Path.Join(di.RootDirectory.FullName, "NTFS", "config.sys")));
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
