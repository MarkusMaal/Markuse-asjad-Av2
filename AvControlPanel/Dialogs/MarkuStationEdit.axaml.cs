using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvControlPanel.Models.MarkuStation;

namespace AvControlPanel.Dialogs;

public partial class MarkuStationEdit : Window
{
    public Game? EditableGame { get; init; }

    public delegate void OkHandler(GameEventArgs e);
    public delegate void DeleteHandler(GameEventArgs e);

    public event OkHandler? DialogOk;
    public event DeleteHandler? DialogDelete;

    public class GameEventArgs(string? executable, string? name) : EventArgs
    {
        public Game? Game { get; } = new()
        {
            Executable = executable ?? "",
            Name = name ?? ""
        };
    }
    
    public MarkuStationEdit()
    {
        InitializeComponent();
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        DialogDelete?.Invoke(new GameEventArgs(LocationBox.Text, NameBox.Text));
        Close();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        DialogOk?.Invoke(new GameEventArgs(LocationBox.Text, NameBox.Text));
        Close();
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
            // oopsie daisy
        }
    }

    private void Mse_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EditableGame is null)
        {
            DeleteButton.IsVisible = false;
            return;
        }
        NameBox.Text = EditableGame.Name;
        LocationBox.Text = EditableGame.Executable;
    }
}