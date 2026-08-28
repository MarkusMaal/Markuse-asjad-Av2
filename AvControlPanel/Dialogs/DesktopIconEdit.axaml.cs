using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MasCommon;

namespace AvControlPanel.Dialogs;

public partial class DesktopIconEdit : Window
{
    public DesktopIcon? EditableIcon { get; init; }

    public delegate void OkHandler(IconEventArgs e);
    public delegate void DeleteHandler(IconEventArgs e);

    public event OkHandler? DialogOk;
    public event DeleteHandler? DialogDelete;

    public class IconEventArgs(string? icon, string? executable, int locationX, int locationY) : EventArgs
    {
        public DesktopIcon? Icon { get; } = new()
        {
            Icon = icon ?? "",
            Executable = executable ?? "",
            LocationX = locationX,
            LocationY = locationY
        };
    }
    
    public DesktopIconEdit()
    {
        InitializeComponent();
    }

    private async void BrowseButtonAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = GetTopLevel(this);

            if (topLevel == null) return;
            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                LocationBox.Text = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
            }
        }
        catch (Exception)
        {
            // TODO: logging
        }
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        DialogDelete?.Invoke(new IconEventArgs(NameBox.Text, LocationBox.Text, EditableIcon?.LocationX ?? -1, EditableIcon?.LocationY ?? -1));
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        DialogOk?.Invoke(new IconEventArgs(NameBox.Text, LocationBox.Text, EditableIcon?.LocationX ?? -1, EditableIcon?.LocationY ?? -1));
        Close();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EditableIcon == null)
        {
            DeleteButton.IsVisible = false;
            return;
        }
        NameBox.SelectedIndex = NameBox.Items.IndexOf(EditableIcon.Icon);
        LocationBox.Text = EditableIcon.Executable;
    }
}