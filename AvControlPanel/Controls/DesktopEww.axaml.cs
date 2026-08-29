using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using AvControlPanel.Models.Desktop;

namespace AvControlPanel.Controls;

public partial class DesktopEww : UserControl
{
    public EwwYuck Yuck
    {
        get => GetValue(YuckProperty);
        set => SetValue(YuckProperty, value);
    }
    
    public static readonly StyledProperty<EwwYuck> YuckProperty = AvaloniaProperty.Register<DesktopEww, EwwYuck>("Yuck");
    
    public DesktopEww()
    {
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TableView tv) return;
        EditGrid.IsEnabled = tv.SelectedIndex != -1;
        if (!EditGrid.IsEnabled) IconPreview.Source = null;
        if (tv.SelectedItem is not DesktopEntry desktopEntry) return;
        RefreshIcon(desktopEntry);
        NameBox.Text = desktopEntry.Tooltip;
        IconBox.Text = desktopEntry.Image;
        ExecBox.Text = desktopEntry.Executable;
    }

    private void RefreshIcon(DesktopEntry desktopEntry)
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "eww",
                desktopEntry.Image)))
        {
            IconPreview.Source =
                new Bitmap(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config",
                    "eww", desktopEntry.Image));
        }
        else
        {
            IconPreview.Source = null;
        }
    }

    private void EditButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var backup = YuckTable.SelectedIndex;
        DataContext = null;
        DataContext = this;
        YuckTable.SelectedIndex = backup;
        Yuck.SaveConfig();
    }

    private void NameBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (YuckTable.SelectedIndex == -1) return;
        if (sender is not Control c) return;
        var newEntry = new DesktopEntry
        {
            Executable = ExecBox.Text ?? "",
            Image = IconBox.Text ?? "",
            Tooltip = NameBox.Text ?? ""
        };
        Yuck.Entries[YuckTable.SelectedIndex] = newEntry;
        RefreshIcon(newEntry);
    }
}