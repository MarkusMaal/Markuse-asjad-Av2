using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Threading;

namespace AvIntegrationSoftware;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
[SuppressMessage("ReSharper", "RedundantDelegateCreation")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
public class Watchers
{
    private FileSystemWatcher? _checkMaiaTrigger;
    private FileSystemWatcher? _configWatcher;
    private FileSystemWatcher? _masRootWatcher;
    public static bool WeGoodForNow;
    public Watchers()
    {
        if (Directory.Exists(Path.Join(App.MasRoot, "maia")) && App.Features.Contains("RD"))
        {
            _checkMaiaTrigger = new FileSystemWatcher(Path.Join(App.MasRoot, "maia"));
            InitializeWatcher("*.*", _checkMaiaTrigger, new FileSystemEventHandler(CheckMaiaFiles));
            Program.Log("Initialized M.A.I.A. PIN verification watcher");
        }
        if (Directory.Exists(Path.Join(App.MasRoot, "integration_data")) && App.Features.Contains("IP"))
        {
            _configWatcher = new FileSystemWatcher(Path.Join(App.MasRoot, "integration_data"));
            InitializeWatcher("*.json", _configWatcher, new FileSystemEventHandler(CheckIntegrationData));
            Program.Log("Initialized integration menu config change watcher");
        }

        if (!Directory.Exists(Path.Join(App.MasRoot)) || !App.Features.Contains("IP")) return;
        _masRootWatcher = new FileSystemWatcher(App.MasRoot);
        InitializeWatcher("*.*", _masRootWatcher, new FileSystemEventHandler(CheckMasFiles));
        Program.Log("Initialized general integration config change watcher");
    }
    // Create a new watcher
    private static void InitializeWatcher(string filename, FileSystemWatcher watcher, FileSystemEventHandler fn)
    {
        watcher.NotifyFilter = /*NotifyFilters.Attributes
                               | NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | */NotifyFilters.FileName
                                   /*| NotifyFilters.LastAccess*/
                                   | NotifyFilters.LastWrite
                                   /*| NotifyFilters.Security*/
                                   | NotifyFilters.Size;
        watcher.Changed += fn;
        watcher.Error += new ErrorEventHandler(OnError);
        watcher.Filter = filename;
        watcher.IncludeSubdirectories = false;
        watcher.EnableRaisingEvents = true;
    }

    public void CloseWatchers()
    {
        Program.Log("Shutting down watchers");
        _checkMaiaTrigger?.Dispose();
        _configWatcher?.Dispose();
        _masRootWatcher?.Dispose();
    }
    
    private static void CheckMaiaFiles(object sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith("request_permission.maia") || (e.ChangeType == WatcherChangeTypes.Deleted)) return;
        // M.A.I.A. ligipääsu taotlemine
        if (!File.Exists(App.MasRoot + @"/maia/request_permission.maia") &&
            !File.Exists(App.MasRoot + "/maia/request_permission.mai")) return;
        Program.Log("Device pairing request received");
        if (Program.AllowCode)
        {
            if (Program.CodeOpen) return;
            Program.Log("Displaying TOTP code");
            Program.CodeOpen = true;
            Dispatcher.UIThread.Post(() =>
            {
                ShowCode sc = new()
                {
                    Bg = App.Scheme[0],
                    Fg = App.Scheme[1]
                };
                sc.Show(); 
            });
        }
        else
        {
            Program.Log("The pairing request was denied");
            try { File.Delete(App.MasRoot + "/maia/request_permission.maia"); } catch (Exception) when (!Debugger.IsAttached) { File.Delete(App.MasRoot + "/maia/request_permission.mai"); }
        }
    }

    private static void CheckMasFiles(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed) return;
        if (e.FullPath.EndsWith("Config.json") && e.ChangeType == WatcherChangeTypes.Changed)
        {
            if (WeGoodForNow)
            {
                Program.Log("Duplicate file update event was ignored");
                WeGoodForNow = false;
                return;
            }
            Program.Log("Integration settings file was changed, reloading!");
            WeGoodForNow = true;
            Dispatcher.UIThread.Post(() =>
            {
                var currentApp = (App?)Application.Current;
                currentApp?.ToggleBusy(true);
                currentApp?.UpdateConfig();
            });
        } else if (e.FullPath.EndsWith("scheme.cfg") && e.ChangeType == WatcherChangeTypes.Changed)
        {
            if (WeGoodForNow)
            {
                Program.Log("Duplicate file update event was ignored");
                WeGoodForNow = false;
                return;
            }
            Program.Log("Scheme config file was changed, reloading!");
            WeGoodForNow = true;
            Dispatcher.UIThread.Post(() =>
            {
                var currentApp = (App?)Application.Current;
                currentApp?.ToggleBusy(true);
                currentApp?.UpdateScheme();
            });
        } else if (e.FullPath.EndsWith("events.txt") && e.ChangeType == WatcherChangeTypes.Changed)
        {
            if (WeGoodForNow)
            {
                Program.Log("Duplicate file update event was ignored");
                WeGoodForNow = false;
                return;
            }
            Program.Log("Scheduled tasks config file was changed, reloading!");
            WeGoodForNow = true;
            Dispatcher.UIThread.Post(() =>
            {
                var currentApp = (App?)Application.Current;
                currentApp?.ToggleBusy(true);
                currentApp?.UpdateScheduledTasks();
            });
        } else if (e.FullPath.EndsWith("showabout.txt"))
        {
            Program.Log("Received external about dialog request");
            File.Delete(Path.Join(App.MasRoot, "showabout.txt"));
            Dispatcher.UIThread.Post(App.ShowAbout);
        }
    }

    private static void CheckIntegrationData(object sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith("Config.json") || (e.ChangeType != WatcherChangeTypes.Changed)) return;
        if (WeGoodForNow)
        {
            Program.Log("Ignored duplicate menu event");
            return;
        }
        Program.Log("The config file was changed, re-initialize menu!");
        WeGoodForNow = true;
        Dispatcher.UIThread.Post(() =>
        {
            var currentApp = ((App?)Application.Current);   
            currentApp?.ToggleBusy(true);
            currentApp?.ReInit();
        });
    }
    
    // Error handlers
    private static void OnError(object source, ErrorEventArgs e)
    {
        if (e.GetException().GetType() == typeof(OperationCanceledException))
        {
            Console.WriteLine("Error: File system watcher was cancelled");
            return;
        }
        if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
        {
            Console.WriteLine("Error: File System Watcher internal buffer overflow at " + DateTime.Now + "\r\n");
        }
        else
        {
            Console.WriteLine("Error: Watched directory not accessible at " + DateTime.Now + "\r\n");
        }
        NotAccessibleError((FileSystemWatcher)source);
    }


    private static void NotAccessibleError(FileSystemWatcher source)
    {
        source.EnableRaisingEvents = false;
        const int iMaxAttempts = 120;
        const int iTimeOut = 30000;
        var i = 0;
        while (!source.EnableRaisingEvents && i < iMaxAttempts)
        {
            i += 1;
            try
            {
                source.EnableRaisingEvents = true;
            }
            catch
            {
                source.EnableRaisingEvents = false;
                Thread.Sleep(iTimeOut);
            }
        }

    }
}