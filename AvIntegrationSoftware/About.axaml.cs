using System;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using MasCommon;

namespace AvIntegrationSoftware;

public partial class About : Window
{
    private readonly Verifile _vf = new();
    public static bool AlreadyOpen;
    public About()
    {
        AlreadyOpen = true;
        InitializeComponent();
    }

    private void GetEditionInfo()
    {
        Bitmap cross;
        Bitmap check;
        using (var ms = AssetLoader.Open(new Uri("avares://AvIntegrationSoftware/Assets/failure.gif")))
        {
            cross = new Bitmap(ms);
        }
        using (var ms = AssetLoader.Open(new Uri("avares://AvIntegrationSoftware/Assets/success.gif")))
        {
            check = new Bitmap(ms);
        }
        var masVer = File.ReadAllLines(Path.Join(App.MasRoot, "edition.txt"));
        var edition = masVer[1];
        var fi = new FileInfo(Path.Join(App.MasRoot, "edition.txt"));
        this.GetControl<Label>("MasEditionLabel").Content = edition;
        EditionBox.Fill = edition switch
        {
            "Basic" or "Basic+" => new SolidColorBrush(Colors.Yellow),
            "Starter" => new SolidColorBrush(Colors.Lime),
            "Premium" => new SolidColorBrush(Colors.DarkRed),
            "Pro" => new SolidColorBrush(Colors.DeepSkyBlue),
            "Ultimate" => new SolidColorBrush(Colors.BlueViolet),
            _ => EditionBox.Fill
        };
        var editionDetails = new StringBuilder();
        editionDetails.AppendLine("Versioon: " + masVer[2]);
        editionDetails.AppendLine("Järk: " + masVer[3]);
        MasName.Content = masVer[3][^1] switch
        {
            'a' => "Markuse arvuti asjad",
            'b' => "Markuse virtuaalarvuti asjad",
            'c' => "Markuse tahvelarvuti asjad",
            _ => MasName.Content
        };
        editionDetails.AppendLine("Nimi: " + masVer[10]);
        editionDetails.AppendLine("Keel: " + masVer[6]);
        editionDetails.AppendLine("Juurutatud?: " + (masVer[4] == "Yes" ? "Jah" : "Ei"));
        editionDetails.Append("Muutmisaeg: ")
            .Append(fi.LastWriteTime.ToShortDateString())
            .Append(' ')
            .Append(fi.LastWriteTime.ToShortTimeString());
        editionDetails.AppendLine();
        editionDetails.AppendLine("Kinnituskood: " + masVer[9]);
        EditionDetails.Text = editionDetails.ToString();
        var features = masVer[8].Split('-');
        FeatTs.Source = cross;
        FeatRm.Source = cross;
        FeatIp.Source = cross;
        FeatCs.Source = cross;
        FeatMm.Source = cross;
        FeatRd.Source = cross;
        FeatWx.Source = cross;
        FeatLt.Source = cross;
        FeatGp.Source = cross;
        foreach (var feature in features)
        {
            switch (feature)
            {
                case "MM":
                    FeatMm.Source = check;
                    break;
                case "TS":
                    FeatTs.Source = check;
                    break;
                case "RM":
                    FeatRm.Source = check;
                    break;
                case "IP":
                    FeatIp.Source = check;
                    break;
                case "CS":
                    FeatCs.Source = check;
                    break;
                case "WX":
                    FeatWx.Source = check;
                    break;
                case "RD":
                    FeatRd.Source = check;
                    break;
                case "LT":
                    FeatLt.Source = check;
                    break;
                case "GP":
                    FeatGp.Source = check;
                    break;
            }
        }
        CopyrightLabel.Content = $"\u00a9 Markuse tarkvara {DateTime.Now.Year}";
        new Thread(() =>
        {
            var status = _vf.MakeAttestation();
            Dispatcher.UIThread.Post(() =>
            {
                editionDetails.AppendLine("Olek: " + status);
                EditionDetails.Text = editionDetails.ToString();
            });
        }).Start();
    }

    private void About_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!Design.IsDesignMode) GetEditionInfo();
    }

    private void Image_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control c) return;
        RotateObject([c]);
    }
    private static void RotateObject(Control[] senders)
    {
        const int duration = 1500;
        Animation animation = new()
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            IterationCount = new IterationCount(1),
            PlaybackDirection = PlaybackDirection.Normal,
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseInOut()
        };
        KeyFrame key1 = new()
        {
            KeyTime = TimeSpan.FromMilliseconds(0)
        };
        KeyFrame key2 = new()
        {
            KeyTime = TimeSpan.FromMilliseconds(duration)
        };
        key1.Setters.Add(new Setter(RotateTransform.AngleProperty, -360.0));
        key2.Setters.Add(new Setter(RotateTransform.AngleProperty, 0));
        animation.Children.Add(key1);
        animation.Children.Add(key2);
        foreach (var c in senders)
        {
            _ = animation.RunAsync(c);
        }
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        AlreadyOpen = false;
    }
}