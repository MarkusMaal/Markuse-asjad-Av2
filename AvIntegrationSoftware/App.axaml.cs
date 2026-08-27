using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using MasCommon;

namespace AvIntegrationSoftware;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public class App : Application
{
    public static readonly string MasRoot = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mas");
    private readonly MenuModel _menu = new();
    private TrayIcon? _trayIcon;
    private static int _pollRate = 5000;
    private bool _shutdownNow;
    private readonly Splash _splashScreen = new();
    public static Color[] Scheme = [];
    private readonly Verifile _vf = new();
    private static readonly CommonConfig MasConfig = new();
    private bool _featureTripped;
    public static string Features = "";
    private readonly TimeSpan _checkInterval = new(1, 0, 0);
    private DateTime _nextCheck;
    private static Watchers? _watchers; // this line must NOT be removed, otherwise M.A.I.A. integration will not work
    private bool _previousBusy = true;
    private TaskScheduler? _taskScheduler;
    
    public override void Initialize()
    {
        _nextCheck = DateTime.Now.Add(_checkInterval);
        TryRefreshFeatures();
        _watchers = new Watchers();
        UpdateConfig();
        if (MasConfig.ShowLogo) _splashScreen.Show();
        if (MasConfig.AutostartNotes)
        {
            if (!File.Exists(Path.Join(MasRoot, "noteopen.txt")))
            {
                DefaultActions.ParseStr("ToggleDesktopNotes");   
            }
        }
        if (File.Exists(Path.Join(MasRoot, "scheme.cfg")))
        {
            UpdateScheme();
        }
        AvaloniaXamlLoader.Load(this);
    }

    private static void TryRefreshFeatures()
    {
        if (!File.Exists(Path.Join(MasRoot, "edition.txt"))) return;
        var es = File.OpenText(Path.Join(MasRoot, "edition.txt"));
        try
        {
            for (var i = 0; i < 8; i++) es.ReadLine();
            Features = es.ReadLine() ?? Features;
            es.Close();
            Program.Log($"Available features {Features}");
        }
        catch
        {
            Program.Log("Failed to query available features, assuming none");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args.Contains("--interface-test"))
            {
                if (_splashScreen.IsVisible) 
                    _splashScreen.Hide();
                new InterfaceTest().Show();
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                return;
            }
            if (desktop.Args.Contains("/e"))
            {
                Crash c = new()
                {
                    TechnicalData =
                    {
                        Text = File.ReadAllText(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "mas_error.log"))
                    }
                };
                c.Show();
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                return;
            }

            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var vff = new VerifileFail();
            if (!Verifile.CheckVerifileTamper())
            {
                vff.InfoTextBlock.Text = $"Verifile 2.x räsi ei ole usaldusväärne. Uuendage integratsiooniprogrammi ja/või asendage fail \"{Path.Join(MasRoot, "verifile2.jar")}\" uuema/ühilduva versiooniga.";
                vff.Show();
                if (_splashScreen.IsVisible) _splashScreen.Close();
                return;
            }

            var vfAttestationResult = _vf.MakeAttestation();
            switch (vfAttestationResult)
            {
                case "VERIFIED":
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    break;
                case "FAILED":
                    vff.InfoTextBlock.Text += "\n\nVeakood: VF_FAILED";
                    vff.Show();
                    break;
                case "BYPASS":
                    vff.InfoTextBlock.Text += "\n\nVeakood: VF_BYPASS";
                    vff.Show();
                    break;
                case "LEGACY":
                    vff.InfoTextBlock.Text += "\n\nVeakood: VF_LEGACY";
                    vff.Show();
                    break;
                case "TAMPERED":
                    vff.InfoTextBlock.Text += "\n\nVeakood: VF_TAMPERED";
                    vff.Show();
                    break;
                case "FOREIGN":
                    vff.InfoTextBlock.Text += "\n\nVeakood: VF_FOREIGN";
                    vff.Show();
                    break;
            }

            if (vfAttestationResult != "VERIFIED" && _splashScreen.IsVisible) _splashScreen.Close();
            if (vfAttestationResult == "VERIFIED" && !Verifile.CheckFiles(Verifile.FileScope.IntegrationSoftware))
            {
                vff.InfoTextBlock.Text += "\n\nVeakood: VF_MISSING_FILES";
                vff.Show();
            }
            if (vfAttestationResult != "VERIFIED") return;

            desktop.ShutdownRequested += (_, _) =>
            {
                _shutdownNow = true;
            };
        }
        _menu.Load();
        InitTrayMenu();
        new Thread(MenuUpdateThread).Start();
        
        base.OnFrameworkInitializationCompleted();
        new Thread(() =>
        {
            Thread.Sleep(_pollRate);
            Dispatcher.UIThread.Post(() => _splashScreen.Close());
        }).Start();
    }

    private void InitTrayMenu(bool reInit = false)
    {
        Watchers.WeGoodForNow = false;
        _trayIcon = TrayIcon.GetIcons(this)!.First();
        if (!reInit)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (Program.FlashAutorun)
                Console.Error.WriteLine(
                    "WARNING: Autorun from flash drives is enabled. This may pose a security risk when inserting unknown USB devices!");
            Console.ResetColor();
        }
        foreach (var mi in _menu.MenuItems ?? [])
        {
            mi.PollState();
            if (mi.GetState() == null) continue;
            NativeMenu? subMenu = null;
            if (mi.SubItems != null)
            {
                subMenu = [];
                foreach (var subItem in mi.SubItems)
                {
                    subItem.PollState();
                    if (subItem.GetState() == null) continue;
                    var submenuRealIconPath = subItem.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                    subMenu.Items.Add(new NativeMenuItem(subItem.GetState()!.Label)
                    {
                        Command = new MenuCommand(() =>
                        {
                            subItem.Execute();
                        }),
                        Icon = new Bitmap(submenuRealIconPath),
                        IsVisible = Debugger.IsAttached || !subItem.MenuIdentifier!.Contains("Debug"),
                        IsEnabled = subItem.HasRequiredFeatures() && subItem.GetState()!.StateIdentifier != "Gray"
                    });
                }
            }
            var realIconPath = mi.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
            _trayIcon.Menu!.Items.Add(new NativeMenuItem(mi.GetState()!.Label)
            {
                Command = new MenuCommand(() =>
                {
                    mi.Execute();
                }),
                Icon = new Bitmap(realIconPath),
                Menu = subMenu,
                IsVisible = Debugger.IsAttached || !mi.MenuIdentifier!.Contains("Debug"),
                IsEnabled = mi.HasRequiredFeatures() && mi.GetState()!.StateIdentifier != "Gray"
            });
        }
    }

    public void ToggleBusy(bool isBusy)
    {
        if (_previousBusy == isBusy) return;
        var hourGlassStream = AssetLoader.Open(new Uri("avares://AvIntegrationSoftware/Assets/hourglass.png"));
        var logoStream = AssetLoader.Open(new Uri("avares://AvIntegrationSoftware/Assets/mas_integration.png"));
        var hourGlass = new Bitmap(hourGlassStream);
        var logo = new Bitmap(logoStream);
        TrayIcon.GetIcons(this)?.First().Icon = new WindowIcon(isBusy ? hourGlass : logo);
        hourGlass.Dispose();
        logo.Dispose();
        hourGlassStream.Close();
        logoStream.Close();
        _previousBusy = isBusy;
    }

    private void MenuUpdateThread()
    {
        Program.Log($"Initialized menu polling with {_pollRate}ms interval");
        while (!_shutdownNow)
        {
            if (_featureTripped) return;
            if (Scheme == null)
            {
                Thread.Sleep(100);
                continue;
            }

            if (_taskScheduler != null && !Design.IsDesignMode)
            {
                var delta = TimeSpan.FromMilliseconds(_pollRate);
                foreach (var task in _taskScheduler.Tasks ?? [])
                {
                    if (task.HasRun) continue;
                    if (DateTime.Now > task.TriggerTime - delta && DateTime.Now < task.TriggerTime + delta)
                    {
                        task.RunTask();
                    }
                }
            }

            if (DateTime.Now > _nextCheck)
            {
                Dispatcher.UIThread.Post(() => ToggleBusy(true));
                _nextCheck = DateTime.Now.Add(_checkInterval);
                TryRefreshFeatures();
                if (!Verifile.CheckVerifileTamper() || !Verifile.CheckFiles(Verifile.FileScope.IntegrationSoftware) || !_vf.IsVerified() || !Features.Contains("IP"))
                {
                    Program.Log("Integrity checks failed: This may be due to a recent hardware or software change!");
                    Environment.Exit(255);
                }
                Program.Log($"Integrity checks passed: Next check at {_nextCheck.ToShortTimeString()}");
            }

            if (!Directory.Exists(Path.Join(MasRoot, "integration_data"))) return;

            
            Dispatcher.UIThread.Post(() =>
            {
                if (_splashScreen.IsVisible) return;
                if (!Features.Contains("IP"))
                {
                    _featureTripped = true;
                    TrayIcon.SetIcons(this, null);
                    var vff = new VerifileFail
                    {
                        InfoTextBlock =
                        {
                            Text = "Integratsioonitarkvara ei ole selle Markuse asjade väljaande jaoks saadaval"
                        }
                    };
                    vff.Show();
                    new Thread(() =>
                    {
                        var exit = false;
                        while (!exit)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (!vff.IsVisible) exit = true;
                            });
                            Thread.Sleep(_pollRate);
                        }

                        Environment.Exit(0);
                    }).Start();
                }

                ToggleBusy(false);
                foreach (var (i, mi) in (_menu.MenuItems ?? []).Index())
                {
                    if (mi.GetState() == null) continue;
                    var previousState = mi.GetState();
                    mi.PollState();
                    if (_trayIcon == null) continue;
                    if (_trayIcon.Menu?.Items.Count <= i) continue;
                    // some operating systems may not neccessarily have the menu items in the same order
                    // we added them in, so we have to find the corresponding menu item by name instead
                    // of the index to make sure states are updated correctly
                    var nativeMenu = (NativeMenuItem?)_trayIcon.Menu!.Items.First(p => ((NativeMenuItem)p).Header == previousState?.Label);
                    if (nativeMenu == null && mi.SubItems?.Length > 0)
                    {
                        throw new NullReferenceException();
                    }
                    foreach (var (_, smi) in (nativeMenu?.Menu ?? []).Index())
                    {
                        var subItemLinq = mi.SubItems?.First(p => p.GetState()?.Label == ((NativeMenuItem)smi).Header);
                        if (subItemLinq?.GetState() == null) continue;
                        var submenuPreviousState = subItemLinq.GetState();
                        subItemLinq.PollState();
                        if (submenuPreviousState == subItemLinq.GetState()) continue;
                        ((NativeMenuItem)smi).Header = subItemLinq.GetState()!.Label;
                        ((NativeMenuItem)smi).IsEnabled = subItemLinq.GetState()!.StateIdentifier != "Gray";
                        if (submenuPreviousState!.IconPath == subItemLinq.GetState()!.IconPath) continue;
                        var submenuRealIconPath = subItemLinq.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        ((NativeMenuItem)smi).Icon = new Bitmap(submenuRealIconPath);
                    }
                    if (previousState == mi.GetState()) continue;
                    nativeMenu?.Header = mi.GetState()!.Label;
                    nativeMenu?.IsEnabled = mi.GetState()!.StateIdentifier != "Gray";
                    if (previousState!.IconPath != mi.GetState()!.IconPath)
                    {
                        var realIconPath = mi.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        nativeMenu?.Icon = new Bitmap(realIconPath);
                    }
                }
            });
            Thread.Sleep(_pollRate);
        }
        Program.Log("Shutting down application");
    }

    public void ReInit()
    {
        _menu.Load();
        TrayIcon.GetIcons(this)?.First().Menu?.Items.Clear();
        InitTrayMenu(true);
    }

    public void UpdateConfig()
    {
        try
        {
            if (File.Exists(Path.Join(MasRoot, "Config.json")))
            {
                MasConfig.Load(MasRoot);
            }
            else if (Directory.Exists(MasRoot))
            {
                // default settings
                MasConfig.PollRate = 5000;
                MasConfig.AutostartNotes = false;
                MasConfig.ShowLogo = true;
                MasConfig.AllowScheduledTasks = true;
                MasConfig.Save(MasRoot);
            }
        }
        catch (JsonException) // the file is updated too quickly, keep the current config for now
        {
            return;
        }

        _pollRate = MasConfig.PollRate;
        _taskScheduler = MasConfig.AllowScheduledTasks ? new TaskScheduler() : null;

        if (Debugger.IsAttached)
        {
            Program.Log($"Current configuration: PollRate={_pollRate}, AutoStartNotes={MasConfig.AutostartNotes}, ShowLogo={MasConfig.ShowLogo}, AllowScheduledTasks={MasConfig.AllowScheduledTasks}");
        }

        Watchers.WeGoodForNow = false;
        if (_pollRate >= 1500) return;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("WARNING: Very fast poll rates may result in high CPU utilization!");
        Console.ResetColor();
    }

    public void UpdateScheme()
    {
        new Thread(() =>
        {
                
            while (true)
            {
                try
                {
                    var bgfg = File.ReadAllText(Path.Join(MasRoot, "scheme.cfg")).Split(';');
                    var bgs = bgfg[0].Split(':');
                    var fgs = bgfg[1].Split(':');
                    Color[] cols = [Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2]))];
                    Scheme = cols;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _splashScreen.Background = new SolidColorBrush(Color.FromArgb(64, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])));
                        _splashScreen.Foreground = new SolidColorBrush(cols[1]);
                        Styles.Add(new Style(x => x.OfType<MenuItem>())
                        {
                            Setters = {
                                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Scheme[0])),
                                new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Scheme[1]))
                            }
                        });
                    });
                    Program.Log($"Current color scheme: {cols[0]}/{cols[1]}");
                    Watchers.WeGoodForNow = false;
                    return;
                }
                catch { Thread.Sleep(100); }
            }
        }).Start();
    }

    public void UpdateScheduledTasks()
    {
        if (!MasConfig.AllowScheduledTasks)
        {
            Program.Log("Scheduled tasks are disabled, ignoring");
            return;
        }
        _taskScheduler?.ReloadTasks();
        Watchers.WeGoodForNow = false;
    }

    public static void ShowAbout()
    {
        new About
        {
            Background = new SolidColorBrush(Scheme[0]),
            Foreground = new SolidColorBrush(Scheme[1]),
            AboutGrid =
            {
                Background = new ImageBrush(new Bitmap(Path.Join(MasRoot, "bg_common.png")))
                {
                    Stretch = Stretch.UniformToFill
                }
            }
        }.Show();
    }

    public static void Exit()
    {
        _watchers?.CloseWatchers();
        ((App?)Current)?.ToggleBusy(true);
        ((IClassicDesktopStyleApplicationLifetime)((App)Current!).ApplicationLifetime!)
            .TryShutdown(); // will have a delay, up to {PollRate} ms
    }
}