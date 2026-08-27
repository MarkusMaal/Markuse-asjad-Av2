using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvDeviceManager;

public partial class LaunchError : Window
{
    public LaunchError()
    {
        InitializeComponent();
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}