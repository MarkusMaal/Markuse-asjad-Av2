using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AvIntegrationSoftware;

public class MenuModel
{
    public MenuItemModel[]? MenuItems { get; set; }

    public void Load()
    {
        var cnf = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Join(App.MasRoot, "integration_data", "Config.json")),
            MasMenuModelGenerationContext.Default.MenuModel);
        if (cnf == null) return;
        MenuItems = cnf.MenuItems;
        Program.Log($"Initialized menu model with {MenuItems?.Length} entries");
    }
}

[JsonSerializable(typeof(MenuState))]
[JsonSerializable(typeof(MenuItemModel))]
[JsonSerializable(typeof(MenuModel))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
public partial class MasMenuModelGenerationContext : JsonSerializerContext
{
    
}