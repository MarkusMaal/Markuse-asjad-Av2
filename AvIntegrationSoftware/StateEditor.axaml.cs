using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AvIntegrationSoftware;

public partial class StateEditor : Window
{
    public MenuState? EditableMenuState { get; init; }
    public delegate void OkHandler(MenuStateEventArgs e);
    public event OkHandler? StateOk;
    public StateEditor()
    {
        InitializeComponent();
    }
    public class MenuStateEventArgs(string id, string? action, string iconPath, string label) : EventArgs
    {
        public MenuState? Result { get; } = new()
        {
            StateIdentifier = id,
            Action = action,
            IconPath = iconPath,
            Label = label
        };
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IdBox.Text == null || IconBox.Text == null || LabelBox.Text == null) return;
        StateOk?.Invoke(new MenuStateEventArgs(IdBox.Text, ActionBox.Text, IconBox.Text, LabelBox.Text));
        Close();
    }

    private async Task<string?> ShowOpenFileDialog(FilePickerOpenOptions options)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return null;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        return files.Count >= 1 ? Uri.UnescapeDataString(files[0].Path.ToString()).Replace(App.MasRoot, "%MAS_ROOT%").Replace("file://", "") : null;
    }

    private async void IconBrowserButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            FilePickerFileType imageAll  = new("Pildifailid")
            {
                Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"],
                AppleUniformTypeIdentifiers = ["public.image"],
                MimeTypes = ["image/*"]
            };
            IconBox.Text = await ShowOpenFileDialog(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [imageAll],
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
                Title = "Vali ikoon"
            });
        }
        catch (Exception ex)
        {
            Program.Log($"Failed to open icon: {ex.Message}");
        }
    }

    private async void ActionBrowserButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            FilePickerFileType allFiles = new("Kõik failid")
            {
                Patterns = ["*.*"]
            };
            ActionBox.Text = "shell::" + await ShowOpenFileDialog(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [allFiles],
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
                Title = "Vali käivitatav kestaprogramm"
            });
        }
        catch (Exception ex)
        {
            Program.Log($"Failed to open executable: {ex.Message}");
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EditableMenuState is null) return;
        IdBox.Text = EditableMenuState.StateIdentifier;
        ActionBox.Text = EditableMenuState.Action;
        IconBox.Text = EditableMenuState.IconPath;
        LabelBox.Text = EditableMenuState.Label;
    }
}