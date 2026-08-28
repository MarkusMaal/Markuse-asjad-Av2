namespace AvControlPanel.Models.Menu;

public class MenuItem
{
    public required string Title { get; set; }
    public required string? Tooltip { get; set; }
    public required Script Script { get; set; }
}