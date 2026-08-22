using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class VerifileFail : Window
{
    public VerifileFail()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}