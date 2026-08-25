using System;
using Avalonia.Controls;

namespace AvIntegrationSoftware;

public partial class Splash : Window
{
    public string Copyright { get; set; } = $"\u00a9 Markus Maal {DateTime.Now.Year}";
    public Splash()
    {
        InitializeComponent();
    }
}