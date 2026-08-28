using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvControlPanel.Models;

namespace AvControlPanel.Controls;

public partial class About : UserControl
{
    public Edition EditionInfo
    {
        get => GetValue(EditionInfoProperty);
        set => SetValue(EditionInfoProperty, value);
    }
    
    public static readonly StyledProperty<Edition> EditionInfoProperty = AvaloniaProperty.Register<About, Edition>("EditionInfo");

    public static string Copyright => $"\u00a9 Markus Maal {DateTime.Now.Year}";

    public static string WhatNew => """
                             Mis on uut?
                             + Üleminek uuele koodistikule
                             + Keritavad paneelid
                             + Uus logo
                             + Avaleht asendati skriptide vahekaardiga
                             + Võimalus käivitada väljaannetes ilma MarkuStation toeta
                             """;
    
    public About()
    {
        InitializeComponent();
    }

    private void ComputerInfoClicked(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start("msinfo32");
        } else if (OperatingSystem.IsLinux())
        {
            Process.Start("kinfocenter");
        } else if (OperatingSystem.IsMacOS()) {
            Program.RunCommand("open", "-a \"About This Mac\"");
        }
    }

    private void OpenMasRootClicked(object? sender, RoutedEventArgs e)
    {
        var p = new Process();
        p.StartInfo = new ProcessStartInfo(App.MasRoot)
        {
            UseShellExecute = true,
        };
        p.Start();
    }

    private void Reload_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        mw.Reload();
    }
}