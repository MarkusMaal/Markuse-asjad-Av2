using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MasCommon;

namespace AvIntegrationSoftware;

public class App : Application
{
    public static readonly string MasRoot = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mas");
    private readonly MenuModel _menu = new();
    private TrayIcon? _trayIcon;
    private static int PollRate = 5000;
    private bool _shutdownNow;
    private readonly Splash _splashScreen = new();
    public static Color[] Scheme;
    private Watchers fsWatchers = new();
    private Verifile vf = new();
    private static readonly CommonConfig MasConfig = new();
    
    public override void Initialize()
    {
        if (File.Exists(Path.Join(MasRoot, "Config.json")))
        {
            MasConfig.Load(MasRoot);
        }
        else
        {
            // default settings
            MasConfig.PollRate = 5000;
            MasConfig.AutostartNotes = false;
            MasConfig.ShowLogo = true;
            MasConfig.AllowScheduledTasks = true;
            MasConfig.Save(MasRoot);
        }

        PollRate = MasConfig.PollRate;
        if (MasConfig.ShowLogo) _splashScreen.Show();
        if (MasConfig.AutostartNotes)
        {
            if (!File.Exists(Path.Join(App.MasRoot, "noteopen.txt")))
            {
                DefaultActions.ParseStr("ToggleDesktopNotes");   
            }
        }
        if (File.Exists(Path.Join(MasRoot, "scheme.cfg")))
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
                        });
                        return;
                    }
                    catch { Thread.Sleep(100); }
                }
            }).Start();
        }
        AvaloniaXamlLoader.Load(this);
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
                vff.InfoTextBlock.Text += "\n\nVeakood: VF_INCOMPATIBLE_HASH";
                vff.Show();
                return;
            }

            var vfAttestationResult = vf.MakeAttestation();
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

            if (!Verifile.CheckFiles(Verifile.FileScope.IntegrationSoftware))
            {
                vff.InfoTextBlock.Text += "\n\nVeakood: VF_MISSING_FILES";
                vff.Show();
                return;
            }

            if (vfAttestationResult != "VERIFIED")
            {
                return;
            }
            
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
            Thread.Sleep(PollRate);
            Dispatcher.UIThread.Post(() => _splashScreen.Close());
        }).Start();
    }

    private void InitTrayMenu()
    {
        _trayIcon = TrayIcon.GetIcons(this)!.First();
        foreach (var mi in _menu.MenuItems)
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
                        Command = new MenuCommand(() => subItem.Execute()),
                        Icon = new Bitmap(submenuRealIconPath),
                        IsVisible = Debugger.IsAttached || !subItem.MenuIdentifier!.Contains("Debug"),
                        IsEnabled = subItem.GetState()!.StateIdentifier != "Gray"
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
                IsEnabled = mi.GetState()!.StateIdentifier != "Gray"
            });
        }
    }

    private void MenuUpdateThread()
    {
        while (!_shutdownNow)
        {
            if (Scheme == null)
            {
                Thread.Sleep(100);
                continue;
            }

            
            Dispatcher.UIThread.Post(() =>
            {
                if (_splashScreen.IsVisible) return;
                foreach (var (i, mi) in _menu.MenuItems.Index())
                {
                    if (mi.GetState() == null) continue;
                    var previousState = mi.GetState();
                    mi.PollState();
                    
                    foreach (var (j, smi) in (((NativeMenuItem)_trayIcon.Menu!.Items[i]).Menu ?? []).Index())
                    {
                        if (mi.SubItems == null) continue;
                        if (mi.SubItems![j].GetState() == null) continue;
                        var submenuPreviousState = mi.SubItems[j].GetState();
                        mi.SubItems[j].PollState();
                        if (submenuPreviousState == mi.SubItems[j].GetState()) continue;
                        ((NativeMenuItem)smi).Header = mi.SubItems[j].GetState()!.Label;
                        ((NativeMenuItem)smi).IsEnabled = mi.SubItems[j].GetState()!.StateIdentifier != "Gray";
                        if (submenuPreviousState!.IconPath == mi.SubItems[j].GetState()!.IconPath) continue;
                        var submenuRealIconPath = mi.SubItems[j].GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        ((NativeMenuItem)smi).Icon = new Bitmap(submenuRealIconPath);
                    }
                    if (previousState == mi.GetState()) continue;
                    ((NativeMenuItem)_trayIcon!.Menu!.Items[i]).Header = mi.GetState()!.Label;
                    ((NativeMenuItem)_trayIcon!.Menu!.Items[i]).IsEnabled = mi.GetState()!.StateIdentifier != "Gray";
                    if (previousState!.IconPath != mi.GetState()!.IconPath)
                    {
                        var realIconPath = mi.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        ((NativeMenuItem)_trayIcon!.Menu!.Items[i]).Icon = new Bitmap(realIconPath);
                    }
                }
            });
            Thread.Sleep(PollRate);
        }
    }

    public static void Exit()
    {
        ((IClassicDesktopStyleApplicationLifetime)((App)Current!).ApplicationLifetime!)
            .TryShutdown(); // will have a delay, up to {PollRate} ms
    }
}