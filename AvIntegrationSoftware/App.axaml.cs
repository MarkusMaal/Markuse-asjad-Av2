using System;
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

namespace AvIntegrationSoftware;

public class App : Application
{
    public static readonly string MasRoot = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mas");
    private readonly MenuModel _menu = new();
    private TrayIcon? _trayIcon;
    private const int PollRate = 5000;
    private bool _shutdownNow;
    private readonly Splash _splashScreen = new();
    public static Color[] Scheme;
    private Watchers fsWatchers = new();
    
    public override void Initialize()
    {
        _splashScreen.Show();
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
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
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
                        Icon = new Bitmap(submenuRealIconPath)
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
                Menu = subMenu
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
                    if (previousState == mi.GetState()) continue;
                    ((NativeMenuItem)_trayIcon!.Menu!.Items[i]).Header = mi.GetState()!.Label;
                    if (previousState!.IconPath != mi.GetState()!.IconPath)
                    {
                        var realIconPath = mi.GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        ((NativeMenuItem)_trayIcon!.Menu!.Items[i]).Icon = new Bitmap(realIconPath);
                    }
                    foreach (var (j, smi) in (((NativeMenuItem)_trayIcon.Menu!.Items[i]).Menu ?? []).Index())
                    {
                        if (mi.SubItems == null) continue;
                        if (mi.SubItems![j].GetState() == null) continue;
                        var submenuPreviousState = mi.SubItems[j].GetState();
                        mi.SubItems[j].PollState();
                        if (submenuPreviousState == mi.SubItems[j].GetState()) continue;
                        ((NativeMenuItem)smi).Header = mi.SubItems[j].GetState()!.Label;
                        if (submenuPreviousState!.IconPath == mi.SubItems[j].GetState()!.IconPath) continue;
                        var submenuRealIconPath = mi.SubItems[j].GetState()!.IconPath.Replace("%MAS_ROOT%", MasRoot);
                        ((NativeMenuItem)smi).Icon = new Bitmap(submenuRealIconPath);
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