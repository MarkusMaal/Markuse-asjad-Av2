using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvControlPanel.Models;
using AvControlPanel.Models.MarkuStation;
using AvControlPanel.Models.Menu;
using MasCommon;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace AvControlPanel;

public partial class MainWindow : Window
{
    public readonly Scheme Scheme = new();
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        new Thread(Loading).Start();
    }
    // Reimplementation of WinForms MessageBox.Show
    internal Task MessageBoxShow(string message, string caption = "Markuse arvuti juhtpaneel", ButtonEnum buttons = ButtonEnum.Ok, Icon icon = MsBox.Avalonia.Enums.Icon.None)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon, WindowStartupLocation.CenterOwner);
        var result = box.ShowWindowDialogAsync(this);
        return result;
    }

    private void DisplayLoadMessage(string loadMessage, int progress)
    {
        ProgressStatusLabel.Text = loadMessage;
        CollectProgress.Value = progress;
    }

    internal void Reload()
    {
        CheckSysLabel.IsVisible = true;
        LogoPanel.IsVisible = false;
        MainTabControl.IsVisible = false;
        CollectProgress.Value = 0;
        ProgressStatusLabel.Text = "Ettevalmistamine";
        new Thread(Loading).Start();
    }

    private void StopError(string status)
    {
        CollectProgress.Value = 0;
        LoaderLogo.IsVisible = false;
        FailGif.IsVisible = true;
        ProgressStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        ProgressStatusLabel.TextWrapping = TextWrapping.Wrap;
        ProgressStatusLabel.MaxWidth = Width / 2;
        ProgressStatusLabel.Text = status;
        CollectProgress.IsVisible = false;
        
        InfoCollectLabel.Content = "Programmi laadimine nurjus";
        this.Title = "Markuse asjad";
        if (status.Contains("VF_BYPASS"))
        {
            ErrorExitButton.Content = "Ignoreeri";
            ErrorExitButton.Click -= ErrorExitButton_OnClick;
            ErrorExitButton.Click += (_, _) =>
            {
                LogoPanel.IsVisible = true;
                CheckSysLabel.IsVisible = false;
                MainTabControl.IsVisible = true;
                MainTabControl.IsEnabled = true;
            };
        }

        ErrorExitButton.IsVisible = true;
    }

    private void Loading()
    {
        if (!Verifile.CheckVerifileTamper())
        {
            Dispatcher.UIThread.Post(() => StopError("Püsivuskontroll ei ole usaldusväärne, sest Verifile 2.0 räsi ei ole sobiv. Palun uuendage juhtpaneeli ja/või Verifile 2.0 tarkvara kataloogis\n\"" + App.MasRoot + "\". Täpsem info standardväljundis."));
            return;
        }
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Värviskeemi laadimine", 12));
        Scheme.LoadScheme(App.MasRoot);
        Dispatcher.UIThread.Post(() =>
        {
            Background = new SolidColorBrush(Scheme.BackgroundColor);
            Foreground = new SolidColorBrush(Scheme.ForegroundColor);
        });
        if (File.Exists(App.MasRoot + "/irunning.log"))
        {
            Dispatcher.UIThread.Post(() => WindowState = WindowState.FullScreen);
        }
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Verifile püsivuskontrolli sooritamine", 24));
        Program.MakeAttestation();
        switch (Program.Status)
        {
            case "VERIFIED":
                break;
            case "FOREIGN":
                Dispatcher.UIThread.Post(() => StopError("See programm töötab ainult Markuse arvutis.\nVeakood: VF_FOREIGN"));
                return;
            case "FAILED":
                Dispatcher.UIThread.Post(() => StopError("Verifile püsivuskontrolli läbimine nurjus.\nVeakood: VF_FAILED"));
                return;
            case "TAMPERED":
                Dispatcher.UIThread.Post(() => StopError( "See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista.\nVeakood: VF_TAMPERED"));
                return;
            case "LEGACY":
                Dispatcher.UIThread.Post(() => StopError("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga.\nVeakood: VF_LEGACY."));
                return;
            case "BYPASS":
                Dispatcher.UIThread.Post(() => StopError("Veakood: VF_BYPASS"));
                return;
        }
        if (!Verifile.CheckFiles(Verifile.FileScope.ControlPanel) || !File.Exists(Path.Join(App.MasRoot, "Config.json")))
        {
            Dispatcher.UIThread.Post(() => StopError("Markuse asjade tarkvara ei ole õigesti juurutatud. Palun juurutage seade kasutades juurutamise tööriista."));
            return;
        }
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("MarkuStationi konfiguratsiooni laadimine", 36));
        Config? cfg = null;
        if (File.Exists(Path.Join(App.MasRoot, "ms_games.txt")) && File.Exists(Path.Join(App.MasRoot, "ms_exec.txt")) &&
            File.Exists(Path.Join(App.MasRoot, "ms_display.txt")) && File.Exists(Path.Join(App.MasRoot, "setting.txt")))
        {
            cfg = new Config();
            cfg.LoadConfig();
        }

        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Üldise konfiguratsiooni laadimine", 48));
        var commonConfig = new CommonConfig();
        commonConfig.Load(App.MasRoot);
        var edition = new Edition();
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Skriptimenüü analüüsimine", 60));
        ScriptMenu? scriptMenu = null;
        if (File.Exists(Path.Join(App.MasRoot, "ScriptMenu.xml")))
        {
            scriptMenu = new ScriptMenu(XDocument.Load(Path.Join(App.MasRoot, "ScriptMenu.xml")));
        }
        else
        {
            Dispatcher.UIThread.Post(() => ScriptTab.IsVisible = false);
        }

        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Taustapiltide laadimine", 72));
        var bitmapDesktop = new Bitmap(Path.Combine(App.MasRoot, "bg_desktop.png"));
        var bitmapLogin = new Bitmap(Path.Combine(App.MasRoot, "bg_login.png"));
        var bitmapUncommon = new Bitmap(Path.Combine(App.MasRoot, "bg_uncommon.png"));
        
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Töölauaikoonide avastamine", 84));
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = App.MasRoot + "/Markuse asjad/DesktopIcons" + (OperatingSystem.IsMacOS() ? ".app/Contents/MacOS/DesktopIcons" : "") +
                           (OperatingSystem.IsWindows() ? ".exe" : ""),
                Arguments = "--icons",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        Program.AvailableIcons.Clear();
        while (!proc.StandardOutput.EndOfStream)
        {
            var line = proc.StandardOutput.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;
            Program.AvailableIcons.Add(line);
        }
        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Töölauaikoonide konfiguratsiooni laadimine", 96));
        if (File.Exists(Path.Combine(App.MasRoot, "DesktopIcons.json")))
        {
            var desktopLayoutReader = File.OpenText(Path.Combine(App.MasRoot, "DesktopIcons.json"));
            var json = desktopLayoutReader.ReadToEnd();
            desktopLayoutReader.Close();
            var dl = JsonSerializer.Deserialize(json, DesktopLayoutSourceGenerationContext.Default.DesktopLayout);
            if (dl != null)
            {
                Dispatcher.UIThread.Post(() =>
                    DesktopPanel.DesktopLayoutObject = dl);
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() => DesktopTab.IsVisible = false);
        }

        Dispatcher.UIThread.Post(() => DisplayLoadMessage("Kasutajaliidese ettevalmistamine", 100));
        Dispatcher.UIThread.Post(() =>
        {
            RootGrid.Background = new ImageBrush(new Bitmap(Path.Join(App.MasRoot, "bg_common.png")))
            {
                Stretch = Stretch.UniformToFill,
            };
            if (cfg != null)
            {
                MarkuStationPanel.MarkuStationConfig = cfg;
                MarkuStationPanel.MarkuStationGames = [.. cfg.GetGames()];
            }
            else
            {
                MarkuStationTab.IsVisible = false;
            }

            if (scriptMenu != null)
            {
                ScriptsPanel.ScriptMenuObject = scriptMenu;
            }

            if (edition.Name.Contains("basic", StringComparison.InvariantCultureIgnoreCase))
            {
                ConfigTab.IsVisible = false;
            }
            AboutPanel.EditionInfo = edition;
            ConfigurationPanel.MasCommonConfig = commonConfig;
            ConfigurationPanel.DesktopBackground = bitmapDesktop;
            ConfigurationPanel.LoginBackground = bitmapLogin;
            ConfigurationPanel.UncommonBackground = bitmapUncommon;
            CheckSysLabel.IsVisible = false;
            LogoPanel.IsVisible = true;
            MainTabControl.IsVisible = true;
            Title = "Juhtpaneel";
        });
    }

    private void ErrorExitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(Environment.ExitCode);
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
        {
            MainTabControl.SelectedIndex = e.Key switch
            {
                Key.A => 0,
                Key.M => File.Exists(Path.Combine(App.MasRoot, "Markuse asjad", "MarkuStation2" + (OperatingSystem.IsWindows() ? ".exe" : ""))) ? 1 : MainTabControl.SelectedIndex,
                Key.K => 2,
                Key.D => File.ReadAllText(Path.Combine(App.MasRoot, "edition.txt")).Contains("TS") ? 3 : MainTabControl.SelectedIndex,
                Key.T => 4,
                _ => MainTabControl.SelectedIndex
            };
        }
        if (e.Key is Key.LeftAlt or Key.RightAlt)
        {
            TipLabel.IsVisible = true;
            CloseButton.IsVisible = false;
        }
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        TipLabel.IsVisible = false;
        CloseButton.IsVisible = File.Exists(App.MasRoot + "/irunning.log");
    }

    private void InputElement_OnKeyUp(object? sender, KeyEventArgs e)
    {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key == Key.H)
            {
                _ = MessageBoxShow(
                    "Alt - Kuva klaviatuuri otseteed\n" +
                    (OperatingSystem.IsMacOS() ? "Cmd + Q" :"Alt + F4") + " - Sulge rakendus\n" +
                    "Alt + H - Kuva kõik kiirklahvid\n" +
                    "Alt + A - Navigeeri avalehele\n" +
                    "Alt + M - Navigeeri MarkuStationi vahekaardile\n" +
                    "Alt + K - Ava konfiguratsiooni vahekaart\n" +
                    "Alt + D - Ava töölaua vahekaart\n" +
                    "Alt + T - Ava teabe vahekaart\n" +
                    "CTRL + TAB - Järgmine vahekaart\n" +
                    "CTRL + SHIFT + TAB - Eelmine vahekaart\n", "Kiirklahvid", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question
                    );
            }
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || (e.Key != Key.Tab)) return;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (MainTabControl.SelectedIndex != 0)
                {
                    MainTabControl.SelectedIndex--;
                    if (DoWeSkipTab())
                    {
                        MainTabControl.SelectedIndex --;
                    }
                }
                else
                {
                    MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1;
                }
            }
            else
            {
                if (MainTabControl.SelectedIndex != MainTabControl.Items.Count - 1)
                {
                    MainTabControl.SelectedIndex += 1;
                    if (DoWeSkipTab())
                    {
                        MainTabControl.SelectedIndex += 1;
                    }
                }
                else
                {
                    MainTabControl.SelectedIndex = 0;
                }
            }
    }

    private bool DoWeSkipTab()
    {
        return ((MainTabControl.SelectedIndex == 1) && !File.Exists(Path.Combine(App.MasRoot, "Markuse asjad",
                   "MarkuStation2" + (OperatingSystem.IsWindows() ? ".exe" : "")))) ||
               ((MainTabControl.SelectedIndex == 3) &&
                !File.ReadAllText(Path.Combine(App.MasRoot, "edition.txt")).Contains("TS"));
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}