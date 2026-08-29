using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvControlPanel.Dialogs;
using AvControlPanel.Models.MarkuStation;

namespace AvControlPanel.Controls;

public partial class MarkuStation : UserControl
{
    public Config MarkuStationConfig
    {
        get => GetValue(MarkuStationConfigProperty);
        set => SetValue(MarkuStationConfigProperty, value);
    }
    
    public static readonly StyledProperty<Config> MarkuStationConfigProperty = AvaloniaProperty.Register<MarkuStation, Config>("MarkuStationConfig");
    
    public ObservableCollection<Game> MarkuStationGames
    {
        get => GetValue(MarkuStationGamesProperty);
        set => SetValue(MarkuStationGamesProperty, value);
    }
    
    public static readonly StyledProperty<ObservableCollection<Game>> MarkuStationGamesProperty = AvaloniaProperty.Register<MarkuStation, ObservableCollection<Game>>("MarkuStationGames");
    
    public MarkuStation()
    {
        InitializeComponent();
    }

    private void MsGameEdit(object? sender, TappedEventArgs e)
    {
        if (sender is not TableView tv) return;
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        var selectedGame = MarkuStationGames[tv.SelectedIndex];
        var msE = new MarkuStationEdit()
        {
            Background = mw.Background,
            Foreground = mw.Foreground,
            EditableGame = selectedGame
        };
        msE.DialogOk += args =>
        {
            if (args.Game == null) return;
            MarkuStationGames[tv.SelectedIndex].Name = args.Game.Name;
            MarkuStationGames[tv.SelectedIndex].Executable = args.Game.Executable;
            ForceRefresh();
        };
        msE.DialogDelete += _ =>
        {
            MarkuStationGames =
            [
                .. MarkuStationGames
                    .Where(p => (p.Name != selectedGame.Name && p.Executable != selectedGame.Executable)).ToArray()
            ];
        };
        msE.ShowDialog(mw);
    }

    private void ForceRefresh()
    {
        DataContext = null;
        DataContext = this;
    }

    private async void BrowseButtonAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null) return;

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                LocationBox.Text = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"MarkuStation file browser function error: {ex.Message}");
        }
    }

    private void AddButton(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        var newGames = new Game[MarkuStationGames.Count + 1];
        for (var i = 0; i < MarkuStationGames.Count; i++)
        {
            newGames[i] = MarkuStationGames[i];
        }
        newGames[MarkuStationGames.Count] = new Game()
        {
            Name = GameNameBox.Text ?? "",
            Executable = LocationBox.Text ?? ""
        };
        if ((newGames[MarkuStationGames.Count].Name == "") || (newGames[MarkuStationGames.Count].Executable == ""))
        {
            mw.MessageBoxShow("Palun täitke kõik väljad!", "Mängu lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            return;
        }
        MarkuStationGames = [.. newGames];
    }

    private void RunMs(object? sender, RoutedEventArgs e)
    {
        // käivita MarkuStation 2 kui see eksisteerib
        if (File.Exists(App.MasRoot + "/Markuse asjad/MarkuStation2") || File.Exists(App.MasRoot + "/Markuse asjad/MarkuStation2.exe"))
        {
            Program.Log("Launching MarkuStation 2");
            if (OperatingSystem.IsWindows())
            {
                Program.StartWin32Process(App.MasRoot + "/Markuse asjad/MarkuStation2.exe");
            } else
            {
                var p = new Process();
                p.StartInfo.FileName = App.MasRoot + "/Markuse asjad/MarkuStation2";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
        }
        else
        { // käivita MarkuStation 1 fallback-ina
            Program.Log("Launching MarkuStation (fallback)");
            Program.StartWin32Process(App.MasRoot + "/MarkuStation.exe");
        }
    }

    private void MsLoadSettings(object? sender, RoutedEventArgs e)
    {
        MarkuStationConfig.LoadConfig();
        MarkuStationGames.Clear();
        foreach (var game in MarkuStationConfig.GetGames())
        {
            MarkuStationGames.Add(game);
        }
        ForceRefresh();
    }

    private void MsSaveConfig(object? sender, RoutedEventArgs e)
    {
        MarkuStationConfig.Games = [.. MarkuStationGames];
        MarkuStationConfig.SaveConfig();
    }
}