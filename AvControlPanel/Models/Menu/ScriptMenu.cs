using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AvControlPanel.Models.Menu;

public class ScriptMenu
{
    public List<MenuItem> MenuItems { get; } = [];

    public ScriptMenu(XDocument xml)
    {
        if (xml.Root == null) ThrowFormatException();
        foreach (var el in xml.Root!.Elements())
        {
            if (el.Element("Title") == null) ThrowFormatException();
            if (el.Element("Script") == null) ThrowFormatException();
            var title = el.Element("Title")!.Value;
            var tooltip = el.Element("Tooltip")?.Value;
            var script = new Script
            {
                Wait = el.Element("Script")!.Attribute("Wait")!.Value == "True"
            };
            foreach (var shEl in el.Element("Script")!.Elements())
            {
                var platform = shEl.Attribute("Platform");
                var status = shEl.Attribute("Status");
                var detach =  shEl.Attribute("Detach");
                var directory =  shEl.Attribute("Directory");
                var shell = new Shell();
                if (platform != null) shell.Platform = platform.Value;
                if (status != null) shell.Status = status.Value;
                if (detach != null) shell.Detach = detach.Value == "True";
                if (directory != null) shell.Directory = directory.Value;
                shell.Command = shEl.Value;
                script.AddCommand(shell);
            }
            MenuItems.Add(new MenuItem() {Script = script, Title = title, Tooltip = tooltip});
        }
        Program.Log("Script menu loaded and validated");
    }

    public MenuItem[] GetMenuItems()
    {
        return [.. MenuItems];
    }

    private static void ThrowFormatException()
    {
        throw new FormatException("XML document is not in the correct format");
    }
}