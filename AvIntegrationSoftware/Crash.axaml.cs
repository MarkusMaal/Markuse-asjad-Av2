using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class Crash : Window
{
    public Crash()
    {
        InitializeComponent();
    }


    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        App.Exit();
    }

    private void ResetButton_Click(object? sender, RoutedEventArgs e)
    {
        var exePath = Environment.ProcessPath;
        Process.Start(new ProcessStartInfo(exePath!) { UseShellExecute = true });
        App.Exit();
    }
}