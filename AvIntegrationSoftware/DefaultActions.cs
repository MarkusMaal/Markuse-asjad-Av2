using System;
using System.Diagnostics;
using System.IO;

// ReSharper disable UnusedMember.Global

namespace AvIntegrationSoftware;

public class DefaultActions
{
    public static void ParseStr(string str)
    {
        switch (str)
        {
            case "ToggleDesktopNotes":
                ToggleDesktopNotes();
                break;
            case "OpenHomeDir":
                OpenHomeDir();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private static void OpenHomeDir()
    {
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
            File.Delete(Path.Join(App.MasRoot, "noteopen.txt"));
            File.WriteAllText(Path.Join(App.MasRoot, "closenote.log"), "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
            return;
        }
        File.WriteAllText(Path.Join(App.MasRoot, "noteopen.txt"), "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
        Process.Start(Path.Join(App.MasRoot, "Markuse asjad", "TöölauaMärkmed" + suff));
    }
}