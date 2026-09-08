using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvIntegrationSoftware;

public partial class VerifileFail : Window
{
    internal bool ForceClose { get; set; }
    public VerifileFail()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (ForceClose) Environment.Exit(255);
        Hide();
        new Thread(() =>
        {
            Thread.Sleep(100);
            Dispatcher.UIThread.Post(Close);
        }).Start();
    }
}