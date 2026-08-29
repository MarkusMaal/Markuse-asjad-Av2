using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvControlPanel.Dialogs;
using MasCommon;
using MsBox.Avalonia.Enums;

namespace AvControlPanel.Controls;

public partial class Desktop : UserControl
{
    public DesktopLayout DesktopLayoutObject
    {
        get => GetValue(DesktopLayoutObjectProperty);
        set => SetValue(DesktopLayoutObjectProperty, value);
    }
    
    public static readonly StyledProperty<DesktopLayout> DesktopLayoutObjectProperty = AvaloniaProperty.Register<Desktop, DesktopLayout>("DesktopLayoutObject");
    public Desktop()
    {
        InitializeComponent();
    }

    private void DesktopApps_OnPointerPressed(object? sender, TappedEventArgs tappedEventArgs)
    {
        if (sender is not TableView tv) return;
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        if (tv.SelectedItem is not DesktopIcon desktopIcon) return;
        var dIconEdit = new DesktopIconEdit
        {
            Background = mw.Background,
            Foreground = mw.Foreground,
            EditableIcon = desktopIcon
        };
        dIconEdit.DialogOk += args =>
        {
            if (args.Icon == null) return;
            DesktopLayoutObject.Children[tv.SelectedIndex] = args.Icon;
            DataContext = null;
            DataContext = this;
            SaveDesktopSettings();
            DesktopIconsRestart_OnClick(sender, null);
        };
        dIconEdit.DialogDelete += args =>
        {
            if (args.Icon == null) return;
            DesktopLayoutObject.Children =
            [
                .. DesktopLayoutObject.Children.Where(p =>
                    (p.Icon != desktopIcon.Icon) && (p.Executable != desktopIcon.Executable))
            ];
            DataContext = null;
            DataContext = this;
            SaveDesktopSettings();
            DesktopIconsRestart_OnClick(sender, null);
        };
        dIconEdit.ShowDialog(mw);
    }

    private void SaveDesktopSettings()
    {
        if (TopLevel.GetTopLevel(this)  is not MainWindow mw) return;
        var jsonData = JsonSerializer.Serialize(DesktopLayoutObject, DesktopLayoutSourceGenerationContext.Default.DesktopLayout);
        try
        {
            File.WriteAllText(App.MasRoot + "/DesktopIcons.json", jsonData, encoding: Encoding.UTF8);
            Program.Log("Saved desktop configuration");
        }
        catch (Exception ex)
        {
            Program.Log($"Failed to save desktop configuration: {ex.Message}");
            mw.MessageBoxShow("Sätete salvestamine nurjus. Olge kindlad, et teil oleks kirjutamise ligipääs failile \"DesktopIcons.json\".", "Markuse arvuti juhtpaneel", ButtonEnum.Ok, Icon.Error);
        }
    }

    private void DesktopJSONEditButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Program.Log("Opened desktop configuration JSON file");
        var p = new Process
        {
            StartInfo =
            {
                UseShellExecute = true,
                FileName = Path.Join(App.MasRoot, "DesktopIcons.json"),
            }
        };
        p.Start();
    }

    private void DesktopIconsRestart_OnClick(object? sender, RoutedEventArgs? e)
    {
        foreach (var process in Process.GetProcesses())
        {
            if (process.ProcessName.StartsWith("DesktopIcons"))
            {
                Program.Log("Killed DesktopIcons process");
                process.Kill();
            }
        }
        var p = new Process
        {
            StartInfo = {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = App.MasRoot + "/Markuse asjad/DesktopIcons" + (OperatingSystem.IsWindows() ? ".exe" : ""),
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                    
            }
        };
        // some additional nonsense is required if we're not in Windows 
        if (!OperatingSystem.IsWindows())
        {
            p.StartInfo.Arguments = "-c \"nohup '" + p.StartInfo.FileName + "' > /dev/null 2>&1 &\"";
            p.StartInfo.FileName = "bash";
        }
        p.Start();
        Program.Log("Restarted DesktopIcons process");
    }

    private void DesktopIconsResetDefaults_OnClick(object? sender, RoutedEventArgs e)
    {
        if (File.Exists(App.MasRoot + "/DesktopIcons.json"))
        {
            File.Delete(App.MasRoot + "/DesktopIcons.json");
            Program.Log("Deleted DesktopIcons JSON file");
        }
        if (File.Exists(App.MasRoot + "/DesktopIconsCommand.json"))
        {
            File.Delete(App.MasRoot + "/DesktopIconsCommand.json");
            Program.Log("Deleted DesktopIconsCommand JSON file");
        }

        DesktopIconsRestart_OnClick(sender, e);
    }

    private void DesktopIconsAddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        var dEdit = new DesktopIconEdit()
        {
            Background = mw.Background,
            Foreground = mw.Foreground,
        };
        dEdit.DialogOk += args =>
        {
            if (args.Icon == null) return;
            DesktopLayoutObject.Children = [.. DesktopLayoutObject.Children.Append(args.Icon)];
            DataContext = null;
            DataContext = this;
            SaveDesktopSettings();
            DesktopIconsRestart_OnClick(sender, null);
        };
        dEdit.ShowDialog(mw);
    }

    private void Misc_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        Program.SendDesktopIconCommand(cb.Name switch
        {
            "DesktopIconsCheck" => "IsIconVisible",
            "DesktopLogoCheck" => "IsLogoVisible",
            "DesktopActionCheck" => "IsActionVisible",
            "DesktopLockedCheck" => "Lock",
            _ => ""
        }, cb.IsChecked ?? false ? "true" : "false");
    }

    private void ApplyIconSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SaveDesktopSettings();
        DesktopIconsRestart_OnClick(sender, e);
    }
}