using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;

namespace AvControlPanel.Models.Desktop;

public class EwwYuck
{
    public List<DesktopEntry> Entries { get; set; } = [];
    private readonly string _cfgPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "eww",
        "eww.yuck");
    private readonly string _stylePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "eww",
        "eww.scss");
    
    public void LoadConfig()
    {
        if (!File.Exists(_cfgPath))
        {
            throw new FileNotFoundException("Eww configuration file does not exist");
        }
        TextReader tr = File.OpenText(_cfgPath);
        var cfg = tr.ReadToEnd();
        tr.Close();
        tr.Dispose();
        foreach (var container in cfg.Split("(defwidget container []")[1]
                     .Split("(box :class \"container\" :orientation \"h\"").Skip(1))
        {
            foreach (var icon in container.Split("(box :class \"icon\" :style \"background-image: url(\'").Skip(1))
            {
                var image = icon.Split('\'')[0];
                var tooltip = icon.Split("button :tooltip \"")[1].Split('\"')[0];
                var scriptArr = icon.Split(":onclick \"")[1].Split("\"")[0].Split(".sh ");
                var script = string.Join(".sh ", scriptArr.Skip(1));
                Entries.Add(new DesktopEntry
                {
                    Image = image,
                    Tooltip = tooltip,
                    Executable = script
                });
            }
        }
        Program.Log($"Loaded eww configuration");
    }

    public void SaveConfig()
    {
        TextReader tr = File.OpenText(_cfgPath);
        var previousConfig = tr.ReadToEnd();
        tr.Close();
        tr.Dispose();
        TextWriter writer = File.CreateText(_cfgPath);
        const string bgToken = "background-image: url(";
        const string dblClick = "/home/$USER/scripts/eww_dbl_click.sh";
        const string tipToken = "button :tooltip";
        var idx = 0;
        foreach (var l in previousConfig.Split('\n'))
        {
            if (l.Contains(bgToken))
            {
                var pref = l.Split(bgToken)[0];   
                writer.WriteLine(pref + bgToken  + "'" + Entries[idx].Image + "')\" (");
            }
            else if (l.Contains(tipToken))
            {
                writer.WriteLine($"              {tipToken} \"{Entries[idx].Tooltip}\" :onclick \"{dblClick} {Entries[idx].Executable}\" \"\")");
                idx++;
            }
            else
            {
                writer.WriteLine(l);
            }
        }

        writer.Close();
        writer.Dispose();
        Program.Log($"Saved eww configuration");
    }

    public void ColorSync(Color background)
    {
        if (!File.Exists(_stylePath)) return;
        Program.Log($"Syncing color changes for eww");
        var outputText = "";   
        var reader = File.OpenText(_stylePath);
        var previousLine = "";
        var rp = Math.Round(background.R / 255.0 * 100.0, 0);
        var gp = Math.Round(background.G / 255.0 * 100.0, 0);
        var bp = Math.Round(background.B / 255.0 * 100.0, 0);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine() ?? "";
            if (previousLine.StartsWith(".icon {"))
            {
                line = $"    background-color: rgba({rp}%, {gp}%, {bp}%,0.7);";
            }
            else if (previousLine.EndsWith("opacity: 0.7;"))
            {
                line = $"    background-color: rgba({rp}%, {gp}%, {bp}%, 0.5);";
            }

            outputText += line + "\n";
            previousLine = line;
        }
        reader.Close();
        var writer = File.CreateText(_stylePath);
        writer.Write(outputText);
        writer.Close();
    }
}