using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class InterfaceTest : Window
{
    public InterfaceTest()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        Environment.Exit(0);
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var app = (App?)Application.Current;
        if (app == null) return;
        switch ((sender as Button)?.Content)
        {
            case "Tegumiriba menüü":
                TrayIcon.GetIcons(app)?.First().IsVisible = true;
                break;
            case "Käivitusaken":
                new Splash().Show();
                break;
            case "M.A.I.A. kood":
                new ShowCode().Show();
                break;
            case "Verifile kontrolli nurjumine":
                new VerifileFail().Show();
                break;
            case "Rakenduse kokkujooksmine":
                new Crash().Show();
                break;
        }
    }
}