using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AvIntegrationSoftware;

public class MenuModel
{
    public MenuItemModel[]? MenuItems { get; set; }

    public void Load()
    {
        var cfgFile = Path.Join(App.MasRoot, "integration_data", "Config.json");
        if (!File.Exists(cfgFile))
        {
            Program.Log("Menu configuration doesn't exist, using empty config");
            MenuItems = [];
            return;
        }
        var cnf = JsonSerializer.Deserialize(
            File.ReadAllText(cfgFile),
            MasMenuModelGenerationContext.Default.MenuModel);
        if (cnf == null) return;
        MenuItems = cnf.MenuItems;
        Program.Log($"Initialized menu model with {MenuItems?.Length} entries");
    }

    public void Save()
    {
        var saveDir = Path.Join(App.MasRoot, "integration_data");
        if (!Directory.Exists(App.MasRoot))
        {
            Program.Log($"The directory '{App.MasRoot}' does not exist, saving operation was cancelled!");
            return;
        }
        if (!Directory.Exists(saveDir))
        {
            Program.Log("Integration data directory does not exist, creating it now...");
            Directory.CreateDirectory(saveDir);
        }
        Program.Log("Serializing menu model");
        var cnf = JsonSerializer.Serialize(this, MasMenuModelGenerationContext.Default.MenuModel);
        Program.Log("Saving menu model as Config.json");
        var outputWriter = File.CreateText(Path.Join(saveDir, "Config.json"));
        outputWriter.Write(cnf);
        outputWriter.Close();
    }
}

[JsonSerializable(typeof(MenuState))]
[JsonSerializable(typeof(MenuItemModel))]
[JsonSerializable(typeof(MenuModel))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class MasMenuModelGenerationContext : JsonSerializerContext
{
    
}