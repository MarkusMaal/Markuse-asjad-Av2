using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class JsonEditor : Window
{
    public MenuItemModel? EditableMenuItemModel { get; init; }
    public delegate void OkHandler(MenuModelEventArgs e);
    public event OkHandler? MenuOk;
    public bool ShowSubmenuCheckbox { get; set; }

    public JsonEditor()
    {
        InitializeComponent();
    }
    
    
    public class MenuModelEventArgs(string? id, string? statePoller, string? features, MenuState[]? states, MenuItemModel[]? subItems, bool submenu) : EventArgs
    {
        public MenuItemModel? Result { get; } = new()
        {
            MenuIdentifier = id,
            StatePoller = statePoller,
            RequiredFeatures = features,
            States = states ?? [],
            SubItems = subItems
        };

        public bool Submenu { get; } = submenu;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MenuOk?.Invoke(new MenuModelEventArgs(IdBox.Text, PollBox.Text, FeatureBox.Text, EditableMenuItemModel?.States, EditableMenuItemModel?.SubItems, SubmenuCheckbox.IsChecked ?? false));
        Close();
    }

    private void MenuItemEditor_Loaded(object? sender, RoutedEventArgs e)
    {
        if (EditableMenuItemModel == null) return;
        IdBox.Text = EditableMenuItemModel.MenuIdentifier;
        PollBox.Text = EditableMenuItemModel.StatePoller;
        FeatureBox.Text = EditableMenuItemModel.RequiredFeatures;
        SubmenuCheckbox.IsVisible = ShowSubmenuCheckbox;
    }
}