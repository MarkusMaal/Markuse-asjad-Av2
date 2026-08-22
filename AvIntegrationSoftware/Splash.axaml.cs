using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvIntegrationSoftware;

public partial class Splash : Window
{
    public string Copyright { get; set; } = $"\u00a9 Markus Maal {DateTime.Now.Year}";
    public Splash()
    {
        InitializeComponent();
    }
    

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        if (!File.Exists(Path.Join(App.MasRoot, "edition.txt"))) return;
        var buildNo = "A000000X";
        const string labelFormat = "markuse {0} integratsioon";
        using var stream = File.OpenText(Path.Join(App.MasRoot, "edition.txt"));
        for (var i = 0; i < 3; i++)
        {
            stream.ReadLine();
        }

        buildNo = stream.ReadLine() ?? buildNo;
        stream.Close();
        DeviceLabel.Text = buildNo[^1] switch
        {
            'a' => string.Format(labelFormat, "arvuti"),
            'b' => string.Format(labelFormat, "virtuaalarvuti"),
            'c' => string.Format(labelFormat, "tahvelarvuti"),
            _ => DeviceLabel.Text
        };
    }
}