using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AvDeviceManager;

public partial class MainWindow : Window
{
    public ObservableCollection<Device> Devices { get; set; } =
        [.. new DeviceCollection(Path.Join(App.MasRoot, "maia")).GetDevices()];
    public MainWindow()
    {
        InitializeComponent();
        if (File.Exists(Path.Join(App.MasRoot, "bg_common.png")))
        {
            FullGrid.Background = new ImageBrush(new Bitmap(Path.Join(App.MasRoot, "bg_common.png")))
            {
                Stretch = Stretch.UniformToFill
            };
        }

        if (Devices.Count > 0)
        {
            EmptyLabel.IsVisible = false;
            DeviceGrid.IsVisible = true;
        }

        if (!File.Exists(Path.Join(App.MasRoot, "scheme.cfg")))
        {
            return;
        }
        var schemeFile = File.OpenText(Path.Join(App.MasRoot, "scheme.cfg"));
        var line = schemeFile.ReadLine();
        if (line == null)
        {
            return;
        }
        Background = new SolidColorBrush(SplitToColor(line.Split(';')[0].Split(':')));
        Foreground = new SolidColorBrush(SplitToColor(line.Split(';')[1].Split(':')));
        schemeFile.Close();
    }

    private static void StopWatch()
    {
        Program.TotalStartupStopwatch?.Stop();
        if (Debugger.IsAttached) Console.WriteLine($"Terve programmi laadimiseks kulus {Program.TotalStartupStopwatch?.ElapsedMilliseconds}ms");
    }

    private static Color SplitToColor(string[] split) => new(255, byte.Parse(split[0]), byte.Parse(split[1]), byte.Parse(split[2]));

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TableView tableView) return;
        RemoveDeviceButton.IsEnabled = tableView.SelectedItems?.Count > 0;
    }

    private void Refresh()
    {
        DataContext = null;
        Devices = [.. new DeviceCollection(Path.Join(App.MasRoot, "maia")).GetDevices()];
        if (Debugger.IsAttached)
        {
            Console.WriteLine("IP             Tüüp");
            foreach (var d in Devices)
            {
                Console.WriteLine($"{d.DeviceIp,-15}{d.DeviceTypeFriendly} ({d.DeviceType})");
            }
        }
        DataContext = this;
        EmptyLabel.IsVisible = Devices.Count == 0;
        DeviceGrid.IsVisible = Devices.Count != 0;
    }

    private void AddDeviceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var ad = new AddDevice
        {
            Background = Background,
            Foreground = Foreground,
            FullGrid =
            {
                Background = FullGrid.Background
            },
            WindowStartupLocation =  WindowStartupLocation.CenterOwner
        };
        Hide();
        ad.Show();
        new Thread(() =>
        {
            var breakPls = false;
            while (!breakPls)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ad.IsVisible) return;
                    if (ad.Result)
                    {
                        Refresh();
                    }
                    Show();
                    breakPls = true;
                });
                Thread.Sleep(100);
            }
        }).Start();
    }

    private void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void RemoveDeviceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedDevice = DeviceGrid.SelectedItem as Device;
        var allDevices = new DeviceCollection(Path.Join(App.MasRoot, "maia")).GetDevices();
        var output = File.CreateText(Path.Join(App.MasRoot, "maia", "whitelist.txt"));
        foreach (var device in allDevices)
        {
            if (device.DeviceIp != selectedDevice?.DeviceIp)
            {
                output.WriteLine($"{device.DeviceIp} - {device.DeviceType}");
            }
        }
        output.Close();
        Refresh();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        StopWatch();
    }
}