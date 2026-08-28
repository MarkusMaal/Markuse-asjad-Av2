using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class MenuEditor : Window
{
    private MenuItemModel[]? RootMenu { get; set; }
    public ObservableCollection<MenuItemModel> MenuItems { get; set; }
    public MenuItemModel? SelectedItem { get; private set; }

    private int _subMenuIdx = -1;
    private bool _locked;
    
    public MenuEditor()
    {
        var rootMenu = new MenuModel();
        rootMenu.Load();
        if (rootMenu.MenuItems != null)
        {
            MenuItems = [.. rootMenu.MenuItems];
        }
        else
        {
            MenuItems = [];
        }

        RootMenu = rootMenu.MenuItems;
        InitializeComponent();
    }

    private void Reload(int idx)
    {
        DataContext = null;
        SelectedItem = MenuItems[idx];
        DataContext = this;
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_locked) return;
        if (sender is not TableView tv) return;
        MenuStatesView.IsEnabled = tv.SelectedIndex != -1;
        StateButtons.IsEnabled = tv.SelectedIndex != -1;
        EditItemButton.IsEnabled = tv.SelectedIndex != -1;
        RemoveItemButton.IsEnabled = tv.SelectedIndex != -1;
        if (tv.SelectedIndex == -1) return;
        
        _locked = true;
        var idx = tv.SelectedIndex; 
        Reload(idx);
        tv.SelectedIndex = idx;
        _locked = false;
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TableView tv) return;
        var idx = tv.SelectedIndex;
        if (MenuItems[idx].SubItems == null) return;
        _subMenuIdx = idx;
        RootMenuButton.IsVisible = true;
        AddRootItemButton.IsVisible = false;
        MenuItems = [.. MenuItems[idx].SubItems!];
        Reload(0);
    }

    private void RootMenu_OnClick(object? sender, RoutedEventArgs e)
    {
        MenuItems = [.. RootMenu!];
        _subMenuIdx = -1;
        RootMenuButton.IsVisible = false;
        AddRootItemButton.IsVisible = true;
        Reload(0);
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Save_OnClick(object? sender, RoutedEventArgs e)
    {
        var outputMenu = new MenuModel
        {
            MenuItems = [.. RootMenu!]
        };
        outputMenu.Save();
        Close();
    }

    private void EditItemButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MenuItemsView.SelectedIndex == -1) return;
        var selectedModel = MenuItems[MenuItemsView.SelectedIndex];
        var editor = new JsonEditor()
        {
            Background = Background,
            Foreground = Foreground,
            EditableMenuItemModel = selectedModel
        };
        editor.MenuOk += menuItemModel =>
        {
            if (menuItemModel.Result == null) return;
            SetSelectedMenuItem(menuItemModel.Result);
            var idx = MenuItemsView.SelectedIndex;
            Reload(0);
            MenuItemsView.SelectedIndex = idx;
        };
        editor.ShowDialog(this);
    }
    
    private void AddMenuItem(MenuItemModel value)
    {
        if (_subMenuIdx == -1)
        {
            RootMenu = [.. RootMenu!.Append(value)];
        } else
        {
            RootMenu![_subMenuIdx].SubItems ??= [];
            RootMenu![_subMenuIdx].SubItems = [.. RootMenu![_subMenuIdx].SubItems!.Append(value)];
        }
        ReloadCurrentMenuView();
    }

    private void SetSelectedMenuItem(MenuItemModel value)
    {
        if (_subMenuIdx == -1)
        {
            RootMenu![MenuItemsView.SelectedIndex] = value;
        } else {
            RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex] = value;
        }
        ReloadCurrentMenuView();
    }

    private void ReloadCurrentMenuView()
    {
        MenuItems = _subMenuIdx == -1 ? [.. RootMenu!] : [.. RootMenu![_subMenuIdx].SubItems!];
        Reload(0);
    }

    private void RemoveItemButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MenuItemsView.SelectedIndex == -1) return;
        var selectedModel = MenuItems[MenuItemsView.SelectedIndex];
        if (_subMenuIdx == -1)
        {
            RootMenu = [.. RootMenu!.Where(p => p.MenuIdentifier != selectedModel.MenuIdentifier)];
        } else {
            RootMenu![_subMenuIdx].SubItems = [.. RootMenu![_subMenuIdx].SubItems!.Where(p => p.MenuIdentifier != selectedModel.MenuIdentifier)];
        }
        ReloadCurrentMenuView();
    }

    private void AddRootItemButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var editor = new JsonEditor
        {
            Background = Background,
            Foreground = Foreground,
            EditableMenuItemModel = null,
            ShowSubmenuCheckbox = MenuItemsView.SelectedIndex != -1,
            Title = "Menüü üksuse lisamine"
        };
        editor.MenuOk += newMenuItemModel =>
        {
            if (newMenuItemModel.Result == null) return;
            if (newMenuItemModel.Submenu)
            {
                _subMenuIdx = MenuItemsView.SelectedIndex;
                AddRootItemButton.IsVisible = false;
                RootMenuButton.IsVisible = true;
            }
            AddMenuItem(newMenuItemModel.Result);
        };
        editor.ShowDialog(this);
    }

    private void AddMenuState(MenuState menuState)
    {
        if (_subMenuIdx == -1)
        {
            RootMenu![MenuItemsView.SelectedIndex].States = [.. RootMenu![MenuItemsView.SelectedIndex].States.Append(menuState)];
        } else {
            RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex].States = [.. RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex].States.Append(menuState)];
        }
    }

    private void UpdateMenuState(MenuState menuState)
    {
        if (_subMenuIdx == -1)
        {
            RootMenu![MenuItemsView.SelectedIndex].States[MenuStatesView.SelectedIndex] = menuState;
        } else
        {
            RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex].States[MenuStatesView.SelectedIndex] =
                menuState;
        }
    }

    private void RemoveMenuState(MenuState menuState)
    {
        if (_subMenuIdx == -1)
        {
            RootMenu![MenuItemsView.SelectedIndex].States = [.. RootMenu![MenuItemsView.SelectedIndex].States.Where(p => p.StateIdentifier != menuState.StateIdentifier)];
        } else
        {
            RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex].States = [.. RootMenu![_subMenuIdx].SubItems![MenuItemsView.SelectedIndex].States.Where(p => p.StateIdentifier != menuState.StateIdentifier)];
        }
    }

    private void AddStateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var editor = new StateEditor
        {
            Background = Background,
            Foreground = Foreground,
            EditableMenuState = null,
            Title = "Menüü oleku lisamine"
        };
        editor.StateOk += newMenuState =>
        {
            if (newMenuState.Result == null) return;
            AddMenuState(newMenuState.Result);
            ReloadCurrentMenuView();
        };
        editor.ShowDialog(this);
    }

    private void EditStateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MenuStatesView.SelectedIndex == -1) return;
        var currentState = MenuItems[MenuItemsView.SelectedIndex]
            .States[MenuStatesView.SelectedIndex];
        var editor = new StateEditor
        {
            Background = Background,
            Foreground = Foreground,
            EditableMenuState = currentState,
        };
        editor.StateOk += menuState =>
        {
            if (menuState.Result == null) return;
            UpdateMenuState(menuState.Result);
            ReloadCurrentMenuView();
        };
        editor.ShowDialog(this);
    }

    private void RemoveStateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MenuStatesView.SelectedIndex == -1) return;
        var currentState = MenuItems[MenuItemsView.SelectedIndex]
            .States[MenuStatesView.SelectedIndex];
        RemoveMenuState(currentState);
        ReloadCurrentMenuView();
    }

    private void MenuStatesView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        EditStateButton.IsEnabled = MenuStatesView.SelectedIndex != -1;
        RemoveStateButton.IsEnabled = MenuStatesView.SelectedIndex != -1;
    }
}