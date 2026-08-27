using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvDeviceManager;

public partial class AddDevice : Window
{
    public bool Result;
    public AddDevice()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAddDevice(object? sender, RoutedEventArgs e)
    {
        var maiaTxt = File.AppendText(Path.Join(App.MasRoot, "maia", "whitelist.txt"));
        maiaTxt.WriteLine($"{DevIpTextbox.Text} - {DevTypeComboBox.Text?.Split('(')[1].Split(')')[0]}");
        maiaTxt.Close();
        Result = true;
        Close();
    }

    private void ValidateFields()
    {
        var initialCheck = (DevIpTextbox.Text?.Contains('.') ?? false) && DevIpTextbox.Text?.Split('.').Length == 4 && DevTypeComboBox.SelectedIndex != -1;
        if (!initialCheck)
        {
            AddDeviceButton.IsEnabled = false;
            return;
        }

        foreach (var seg in DevIpTextbox.Text!.Split('.'))
        {
            try
            {
                var num = int.Parse(seg);
                if (num is >= 0 and <= 255) continue; // every segment of an IP address must in range 0-255
                initialCheck = false;
                break;
            }
            catch (OverflowException)
            {
                initialCheck = false;
                break;   
            }
            catch (FormatException)
            {
                initialCheck = false;
                break;
            }
        }

        AddDeviceButton.IsEnabled = initialCheck;
    }

    private void DevIpTextbox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ValidateFields();
    }

    private void DevTypeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ValidateFields();
    }
}