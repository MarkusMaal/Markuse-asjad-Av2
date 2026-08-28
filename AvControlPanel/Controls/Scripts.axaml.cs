using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvControlPanel.Models.Menu;

namespace AvControlPanel.Controls;

public partial class Scripts : UserControl
{
    public ScriptMenu ScriptMenuObject
    {
        get => GetValue(ScriptMenuObjectProperty);
        set => SetValue(ScriptMenuObjectProperty, value);
    }
    
    public static readonly StyledProperty<ScriptMenu> ScriptMenuObjectProperty = AvaloniaProperty.Register<Scripts, ScriptMenu>("ScriptMenuObject");

    private const string DefaultText = "Siin kuvatakse teave, kui liigutate kursori teatud nupu peale.";
    
    public string TipText { get; set; } = DefaultText;
    public Scripts()
    {
        InitializeComponent();
    }

    private void ActionButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.Content == null) return;
        ScriptMenuObject.MenuItems.First(p => p.Title == b.Content.ToString()).Script.Run();
    }

    private void ActionButton_Hover(object? sender, PointerEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.Content == null) return;
        UserTip.Text = ScriptMenuObject.MenuItems.First(p => p.Title == b.Content.ToString()).Tooltip ?? DefaultText;
    }


    private void ActionButton_PointerLeave(object? sender, PointerEventArgs e)
    {
        UserTip.Text = DefaultText;
    }
}