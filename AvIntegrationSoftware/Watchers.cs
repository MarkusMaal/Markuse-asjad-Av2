using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Avalonia.Threading;

namespace AvIntegrationSoftware;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
[SuppressMessage("ReSharper", "RedundantDelegateCreation")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
public class Watchers
{
    private FileSystemWatcher _checkMaiaTrigger;
    public Watchers()
    {
        _checkMaiaTrigger = new FileSystemWatcher(Path.Join(App.MasRoot, "maia"));
        InitializeWatcher("*.*", _checkMaiaTrigger, new FileSystemEventHandler(CheckMaiaFiles));
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
    
    private static void CheckMaiaFiles(object sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith("request_permission.maia") || (e.ChangeType == WatcherChangeTypes.Deleted)) return;
        // M.A.I.A. ligipääsu taotlemine
        if (!File.Exists(App.MasRoot + @"/maia/request_permission.maia") &&
            !File.Exists(App.MasRoot + "/maia/request_permission.mai")) return;
        if (Program.AllowCode)
        {
            if (Program.CodeOpen) return;
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
            try { File.Delete(App.MasRoot + "/maia/request_permission.maia"); } catch (Exception) when (!Debugger.IsAttached) { File.Delete(App.MasRoot + "/maia/request_permission.mai"); }
        }
    }
    // Error handlers
    private static void OnError(object source, ErrorEventArgs e)
    {
        if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
        {
            Console.WriteLine("Error: File System Watcher internal buffer overflow at " + DateTime.Now + "\r\n");
        }
        else
        {
            Console.WriteLine("Error: Watched directory not accessible at " + DateTime.Now + "\r\n");
        }
        NotAccessibleError((FileSystemWatcher)source, e);
    }


    private static void NotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
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