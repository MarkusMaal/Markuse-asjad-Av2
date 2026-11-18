using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SFML.Audio;

namespace AvMarkuStationTwo;

public partial class MainWindow : Window
{
    private Music? _mp;
    private Music? _mp2;
    private readonly string _masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
    private readonly DispatcherTimer _timer = new();

    private string CurrentMenu
    {
        get;
        set
        {
            Program.Log($"Switch to menu: {value}");
            field = value;
        }
    } = "startup";

    private int _angle = 10;
    private int _angle2 = 40;
    private int _angle3 = 70;
    private int _angle4 = 100;
    private int _opacity;
    private int _sel;
    private bool _invert;
    private int _circleOffset;
    private readonly bool _running;
    private readonly bool _nodata = true;
    private int _browserIdx = -1;
    private readonly Random _rand = new();

    private readonly bool _playIntros = true;   // displays the "Markuse arvuti meelelahutus" screen on start and a logo animation when launching a game
    private readonly bool _legacyIntro; // displays the MarkuStation 1 logo when launching a game

    private DispatcherTimer _moveCircle = new();
    private readonly IDictionary<string, string> _msGames = new Dictionary<string, string>();
    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
        if (!Directory.Exists(_masRoot))
        {
            Directory.CreateDirectory(_masRoot);
        }

        string[] games = [];
        string[] execs = [];
        if (File.Exists(_masRoot + "/ms_games.txt") && File.Exists(_masRoot + "/ms_exec.txt"))
        {
            Program.Log("Reading MarkuStation game data");
            games = File.ReadAllText(_masRoot + "/ms_games.txt", Encoding.UTF8).Split("\n").Skip(1).ToArray();
            execs = File.ReadAllText(_masRoot + "/ms_exec.txt", Encoding.UTF8).Replace("\n", "").Split(";").Skip(1).ToArray();
        }
        var i = 0;
        foreach (var game in games) {
            if (game != "")
            {
                _msGames[game] = execs[i].Replace("\\", "/") ;
            }
            i ++;
        }

        if (_msGames.Count > 0)
        {
            _nodata = false;
            NoData.IsVisible = false;
            Program.Log($"Found {games.Length} games");
        }
        else
        {
            Program.Log("No games found!");
        }

        if (File.Exists(_masRoot + "/setting.txt"))
        {
            Program.Log($"Reading MarkuStation configuration data");
            var settings = File.ReadAllText(_masRoot + "/setting.txt", Encoding.UTF8).Split("\n");
            _playIntros = settings[2] == "true"; // this is true if intros are allowed in cpanel
            _legacyIntro = (settings.Length > 3) && (settings[3] == "true"); // we check length for backwards compatibility reasons
            if (!_playIntros)
            {
                VideoPlayer.IsVisible = false;
            }
        }
        if (Process.GetProcesses().Any(p => p.ProcessName.Contains("MarkuStation")))
        {
            _running = true;
        }
        // extract intro movie
        Program.Log("Extracting data");
        File.WriteAllBytes(_masRoot + "/_temp.mp4", Properties.Resources.MarkuStation_startup_video);
        File.WriteAllBytes(_masRoot + "/_temp.wav", GetStreamBytes(Properties.Resources.MarkuStation_startup));
        File.WriteAllBytes(_masRoot + "/_amb.wav", GetStreamBytes(Properties.Resources.MarkuStation_ambient));
        File.WriteAllBytes(_masRoot + "/_sel.wav", GetStreamBytes(Properties.Resources.MarkuStation_menu_select));
        File.WriteAllBytes(_masRoot + "/_enter.wav", GetStreamBytes(Properties.Resources.MarkuStation_menu_enter));
        _timer.Interval = new TimeSpan(0,0,1);_timer.Tick += (_, _) =>
            {
                switch (CurrentMenu)
                {
                    case "startup":
                        if (!((VideoPlayer.IsPlaying) || VideoPlayer.Position >= VideoPlayer.Duration) || (!_running) || (!_playIntros))
                        {
                            VideoPlayer.Source = null;
                            _mp2 = null;
                            File.Delete(_masRoot + "/_temp.mp4");
                            File.Delete(_masRoot + "/_temp.wav");
                            if (_running)
                            {
                                _mp2 = new Music(_masRoot + "/_amb.wav");
                                _mp2.Play();
                                Program.Log("Playing ambiance");
                            }
                            VideoPlayer.IsVisible = false;
                            CurrentMenu = "enter";
                            _timer.Interval = new TimeSpan(64000);
                            Dot1.Opacity = 0;
                            Dot2.Opacity = 0;
                            Dot3.Opacity = 0;
                            Dot4.Opacity = 0;
                            Dot1.RenderTransform = new RotateTransform(_angle, 200, 0);
                            Dot2.RenderTransform = new RotateTransform(_angle2, 200, 0);
                            Dot3.RenderTransform = new RotateTransform(_angle3, 200, 0);
                            Dot4.RenderTransform = new RotateTransform(_angle4, 200, 0);
                            if (Width > 2560) // the user has an ultra-wide monitor, add some padding to left and right to make the corner text more accessible
                            {
                                var offsetX = (int)(Width / 4);
                                MenuHotkeys.Margin = new Thickness(0,0,15 + offsetX,10);
                                BottomRightText1.Margin = new Thickness(0,0,15 + offsetX,10);
                                BottomRightText2.Margin = new Thickness(0,0,15 + offsetX,10);
                                GameName.Margin = new Thickness(20,20,15 + offsetX,20);
                                BrowserTitle.Margin = new Thickness(20 + offsetX,20,20,20);
                            }
                        }
                        break;
                    case "enter":
                        _opacity += 1;
                        if (_opacity > 100)
                        {
                            _opacity = 100;
                        }
                        Dot1.Opacity = _opacity/ 100.0;
                        Dot2.Opacity = _opacity / 100.0;
                        Dot3.Opacity = _opacity / 100.0;
                        Dot4.Opacity = _opacity / 100.0;
                        RotateCircle();
                        if ((_opacity == 100))
                        {
                            Menu.IsVisible = true;
                            MenuHotkeys.IsVisible = true;
                            CurrentMenu = "main";
                        }
                        break;
                    case "main":
                        if (_sel < 0)
                        {
                            _sel = 1;
                        } else if (_sel > 1)
                        {
                            _sel = 0;
                        }
                        if (_sel == 0)
                        {
                            BrowserSel.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
                            ConfigSel.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                        } else
                        {
                            BrowserSel.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                            ConfigSel.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
                        }
                        RotateCircle();
                        break;
                    case "enter_version":
                        _mp?.Stop();
                        _mp = new Music( _masRoot + "/_enter.wav");
                        _mp.Play();
                        CurrentMenu = "fade_version";
                        _opacity = 100;
                        RotateCircle();
                        break;
                    case "browser":
                        MoveCircle();
                        break;
                    case "enter_browser":
                        if (_opacity > 0)
                        {
                            _opacity -= 1;
                        }
                        Menu.IsVisible = false;
                        KeyEsc.IsVisible = false;
                        KeyReturn.Opacity = _opacity / 100.0;
                        FunkyDots.Opacity = _opacity / 100.0;
                        if (_mp?.Status != SoundStatus.Playing && (_opacity == 0))
                        {
                            MenuHotkeys.IsVisible = false;
                            CurrentMenu = "fade_browser";
                            Browser.IsVisible = true;
                            _opacity = 0;
                            WhiteDot.Margin = new Thickness(-this.Width / 2, 0, 0, 0);
                            if (!_nodata)
                            {
                                var noDataLabel = NoData;
                                GamePanel.Children.Clear();
                                Bitmap icon;
                                using (var ms = new MemoryStream(Properties.Resources.MarkuStation_awesome))
                                {
                                    icon = new Bitmap(ms);
                                }
                                foreach (var kvp in _msGames)
                                {
                                    Image sImage = new()
                                    {
                                        Source = icon,
                                        Margin = new Thickness(20),
                                        Width = 64,
                                        Name = kvp.Key,
                                    };

                                    GamePanel.Children.Add(sImage);
                                }
                                GamePanel.Children.Add(noDataLabel);
                                _browserIdx = 0;
                                GameName.IsVisible = true;
                                GameName.Content = _msGames.First().Key;
                                WhiteDot.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                                WhiteDot.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                            }
                        }
                        break;
                    case "exit_browser":
                        if (_opacity > 0)
                        {
                            _opacity -= 1;
                        }
                        if (_opacity == 0)
                        {
                            Browser.Opacity = 0;
                            Browser.IsVisible = false;
                            Dot1.IsVisible = true;
                            Dot2.IsVisible = true;
                            Dot3.IsVisible = true;
                            Dot4.IsVisible = true;
                            _mp2 = new Music(_masRoot + "/_amb.wav");
                            _mp2.Play();
                            Program.Log("Playing ambiance");
                            CurrentMenu = "return_menu";
                        }
                        Browser.Opacity = _opacity / 100.0;
                        break;
                    case "return_menu":
                        if (_opacity < 100)
                        {
                            _opacity += 1;
                        }
                        FunkyDots.Opacity = _opacity / 100.0;
                        if (_opacity == 100)
                        {
                            CurrentMenu = "main";
                            KeyEsc.Opacity = 1;
                            KeyReturn.Opacity = 1;
                            Menu.IsVisible = true;
                            KeyEsc.IsVisible = true;
                            KeyReturn.IsVisible = true;
                            FunkyDots.IsVisible = true;
                            MenuHotkeys.IsVisible = true;
                        }
                        RotateCircle();
                        break;
                    case "fade_browser":
                        _opacity += 1;
                        Browser.Opacity = _opacity / 100.0;
                        if (_opacity == 100)
                        {
                            if (_msGames.Count > 0)
                            {
                                Image firstEl = (Image)GamePanel.Children[0];
                                MoveCircle(GetAbsolutePosition(firstEl));
                            }
                            WhiteDot.ZIndex = -1;
                            Browser.Opacity = 1;
                            CurrentMenu = "browser";
                        }
                        break;
                    case "fade_version":
                        _opacity -= 5;
                        MenuHotkeys.Opacity = _opacity / 100.0;
                        Menu.Opacity = _opacity / 100.0;
                        Version.Opacity = 1.0 - (_opacity / 100.0);
                        if (_opacity == 0)
                        {
                            Version.Opacity = 1;
                            MenuHotkeys.Opacity = 0;
                            Menu.Opacity = 0;
                            CurrentMenu = "version";
                        }
                        RotateCircle();
                        break;
                    case "unfade_version":
                        if (_opacity == 0)
                        {
                            _mp?.Stop();
                            _mp = new Music(_masRoot + "/_enter.wav");
                            _mp.Play();
                        }
                        _opacity += 5;
                        MenuHotkeys.Opacity = _opacity / 100.0;
                        Menu.Opacity = _opacity / 100.0;
                        Version.Opacity = 1.0 - (_opacity / 100.0);
                        if (_opacity >= 100)
                        {
                            Version.Opacity = 0;
                            MenuHotkeys.Opacity = 1;
                            Menu.Opacity = 1;
                            CurrentMenu = "main";
                        }
                        RotateCircle();
                        break;
                    case "version":
                        RotateCircle();
                        break;
                    case "run_game":
                        switch (_opacity)
                        {
                            case 100:
                                MoveCircle(new Point(this.Width, this.Height), true);
                                break;
                            case 0:
                            {
                                Program.Log("Showing MarkuStation logo");
                                if (_playIntros)
                                {
                                    VideoPlayer.Source = null;
                                    VideoPlayer.Source = new Uri(_masRoot + "/_temp.mp4");
                                    _mp2 = new Music(_masRoot + "/_temp.wav");
                                    VideoPlayer.IsVisible = true;
                                    VideoPlayer.Play();
                                    _mp2.Play();
                                }
                                CurrentMenu = "prepare_game";
                                break;
                            }
                        }
                        _opacity--;
                        Browser.Opacity = _opacity / 100.0;
                        break;
                    case "prepare_game":
                        if (!(VideoPlayer.IsPlaying && VideoPlayer.Position < VideoPlayer.Duration) || !_playIntros)
                        {
                            _mp?.Dispose();
                            _mp = null;
                            if (File.Exists(_msGames[_msGames.Keys.ToArray()[_browserIdx]]))
                            {
                                try
                                {
                                    File.Delete(_masRoot + "/_temp.mp4");
                                    File.Delete(_masRoot + "/_temp.wav");
                                    Program.Log("Deleting temporary files used");
                                }
                                catch
                                {
                                    Program.Log("Error deleting temporary files");
                                }
                                Process p = new();
                                p.StartInfo.FileName = _msGames[_msGames.Keys.ToArray()[_browserIdx]];
                                p.StartInfo.UseShellExecute = true;
                                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(_msGames[_msGames.Keys.ToArray()[_browserIdx]]);
                                
                                if (OperatingSystem.IsLinux()) // fix for games not launching under Linux
                                {
                                    var actualArgs = $"-c \"nohup {p.StartInfo.FileName.Replace("\"", "\\\"")} {p.StartInfo.Arguments.Replace("\"", "\\\"")} > /dev/null 2>&1 &\"";
                                    p.StartInfo.FileName = "/usr/bin/bash";
                                    p.StartInfo.Arguments = actualArgs;
                                }
                                Program.Log("Starting game");
                                p.Start();
                                Program.Log("Closing launcher");
                                Close();
                            } else
                            {
                                Program.Log("Error: Game does not exist, returning to browser");
                                VideoPlayer.IsVisible = false;
                                CurrentMenu = "fade_browser";
                                _moveCircle.Stop();
                                _browserIdx = 0;
                                GameName.Content = _msGames.Keys.First();
                            }
                        }
                        break;
                }
            };
    }


    private void MoveCircle(Point p, bool slowly = false)
    {
        if (slowly)
        {
            if ((_moveCircle.IsEnabled))
            {
                _moveCircle.Stop();
            }
            _moveCircle.Interval = new TimeSpan(64000);
            _moveCircle.Tick += (_, _) =>
            {
                var adjusted = false;
                var marginX = WhiteDot.Margin.Left;
                var marginY = WhiteDot.Margin.Top;
                if (WhiteDot.Margin.Left < p.X)
                {
                    marginX += 1;
                    adjusted = true;
                }
                else if (WhiteDot.Margin.Left > p.X)
                {
                    marginX += -1;
                    adjusted = true;
                }
                if (WhiteDot.Margin.Top < p.Y)
                {
                    marginY += 1;
                    adjusted = true;
                }
                else if (WhiteDot.Margin.Top > p.Y)
                {
                    marginY += -1;
                    adjusted = true;
                }
                if (adjusted)
                {
                    WhiteDot.Margin = new Thickness(marginX, marginY, 0, 0);
                } else
                {
                    _moveCircle.Stop();
                    _moveCircle = new DispatcherTimer();
                }
            };
            _moveCircle.Start();
        } else
        {
            WhiteDot.Margin = new Thickness(p.X, p.Y, 0, 0);
        }
    }

    private static Point GetAbsolutePosition(Control control) // ChatGPT helped a bit with this
    {
        if (GetTopLevel(control) is not Control topLevel)
        {
            return new Point(0, 0);
        }
        var transform = control.TransformToVisual(topLevel);
        return transform?.Transform(new Point(0,0)) ?? new Point(0, 0);
    }

    private void MoveCircle()
    {
        if (_nodata)
        {
            if (!_invert)
            {
                WhiteDot.RenderTransform = new TranslateTransform(_circleOffset, _circleOffset);
                _circleOffset += 1;
                if (!(_circleOffset > Height + WhiteDot.Height)) return;
                _invert = !_invert;
                WhiteDot.Margin = new Thickness(0, 0, -Width / 4, 0);
            }
            else
            {
                WhiteDot.RenderTransform = new TranslateTransform(-_circleOffset, _circleOffset);
                _circleOffset -= 1;
                if (!(_circleOffset < -WhiteDot.Height)) return;
                _invert = !_invert;
                WhiteDot.Margin = new Thickness(-Width / 2, 0, 0, 0);
            }
        } else
        {
            if (!_invert)
            {
                _circleOffset += 1;
                if (_circleOffset > 100)
                {
                    _invert = !_invert;
                    _circleOffset = 100;
                }
                WhiteDot.Opacity = _circleOffset / 100.0;
            }
            else
            {
                _circleOffset -= 1;
                if (_circleOffset < 0)
                {
                    _invert = !_invert;
                    _circleOffset = 0;
                }
                WhiteDot.Opacity = _circleOffset / 100.0;
            }
        }
    }

    private void RotateCircle()
    {
        _angle += _rand.Next(1, 3);
        _angle = _angle > 359 ? _angle - 360 : _angle;
        _angle2 += _rand.Next(1, 3);
        _angle2 = _angle2 > 359 ? _angle2 - 360 : _angle2;
        _angle3 += _rand.Next(1, 3);
        _angle3 = _angle3 > 359 ? _angle3 - 360 : _angle3;
        _angle4 += _rand.Next(1, 3);
        _angle4 = _angle4 > 359 ? _angle4 - 360 : _angle4;
        Dot1.RenderTransform = new RotateTransform(_angle, 200, 0);
        Dot2.RenderTransform = new RotateTransform(_angle2, 200, 0);
        Dot3.RenderTransform = new RotateTransform(_angle3, 200, 0);
        Dot4.RenderTransform = new RotateTransform(_angle4, 200, 0);
    }

    private static byte[] GetStreamBytes(UnmanagedMemoryStream ms)
    {
        var i = 0;
        using var umm = ms;
        var buffer = umm.ReadByte();
        var bytes = new byte[umm.Length];
        while (buffer > -1)
        {
            bytes[i] = (byte)buffer;
            i++;
            buffer = umm.ReadByte();
        }
        umm.Close();

        return bytes;
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Program.Log("Initializing startup sequence");
        _timer.Start();
        _mp2 = new Music(_masRoot + "/_temp.wav");
        VideoPlayer.Source = new Uri(_masRoot + "/_temp.mp4");

        // hide mouse cursor
        new Thread(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None));
            }
        }).Start();
        if (!_running || !_playIntros) return;
        Program.Log("Playing MCE video");
        VideoPlayer.Play();
        _mp2.Play();
    }

    private void Navigate(Avalonia.Input.Key key)
    {
        switch (CurrentMenu)
        {
            case "main":
                switch (key)
                {
                    case Avalonia.Input.Key.Up:
                    case Avalonia.Input.Key.Down:
                        SelSound();
                        _sel -= 1;
                        break;
                    case Avalonia.Input.Key.Enter:
                        Enter();
                        break;
                    case Avalonia.Input.Key.Escape:
                        VersionContent.IsVisible = true;
                        ConfigContent.IsVisible = false;
                        CurrentMenu = "enter_version";
                        break;
                }

                break;
            case "version":
                CurrentMenu = "unfade_version";
                break;
            case "browser":
                switch (key)
                {
                    case Avalonia.Input.Key.Left:
                    case Avalonia.Input.Key.Up:
                        if (_msGames.Count > 0)
                        {
                            SelSound();
                            _browserIdx -= 1;
                            if (_browserIdx < 0)
                            {
                                _browserIdx = _msGames.Count - 1;
                            }
                            GameName.Content = _msGames.Keys.ToArray()[_browserIdx];
                            MoveCircle(GetAbsolutePosition(GamePanel.Children[_browserIdx]));
                        }
                        break;
                    case Avalonia.Input.Key.Down:
                    case Avalonia.Input.Key.Right:
                        if (_msGames.Count > 0)
                        {
                            SelSound();
                            _browserIdx += 1;
                            if (_browserIdx >= _msGames.Count)
                            {
                                _browserIdx = 0;
                            }
                            GameName.Content = _msGames.Keys.ToArray()[_browserIdx];
                            MoveCircle(GetAbsolutePosition(GamePanel.Children[_browserIdx]));
                        }
                        break;
                    case Avalonia.Input.Key.Escape:
                        CurrentMenu = "exit_browser";
                        break;
                    case Avalonia.Input.Key.Enter:
                        Enter();
                        break;
                }

                break;
        }
    }

    private void SelSound()
    {
        _mp?.Stop();
        _mp = new Music(_masRoot + "/_sel.wav");
        _mp.Play();
    }

    private void Enter()
    {
        if (CurrentMenu == "main")
        {
            _mp?.Stop();
            _mp = new(_masRoot + "/_enter.wav");
            _mp.Play();
            if (_sel == 0)
            {
                CurrentMenu = "enter_browser";
                _mp2?.Stop();
            } else
            {
                VersionContent.IsVisible = false;
                ConfigContent.IsVisible = true;
                CurrentMenu = "enter_version";
            }
        } else if ((CurrentMenu == "browser") && (_msGames.Count > 0))
        {
            _mp?.Stop();
            _mp = new Music(_masRoot + "/_enter.wav");
            _mp.Play();
            CurrentMenu = "run_game";
            File.WriteAllBytes(_masRoot + "/_temp.mp4", _legacyIntro ? Properties.Resources.MarkuStation_legacy_animation : Properties.Resources.MarkuStation_logo_animation);
            File.WriteAllBytes(_masRoot + "/_temp.wav", GetStreamBytes(_legacyIntro ? Properties.Resources.MarkuStation_logo_old : Properties.Resources.MarkuStation_logo));
        }
    }

    private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        Navigate(e.Key);
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            Program.Log("Deleting extracted temporary assets");
            DeleteIfExists(_masRoot + "/_sel.wav");
            DeleteIfExists(_masRoot + "/_enter.wav");
            DeleteIfExists(_masRoot + "/_amb.wav");
            DeleteIfExists(_masRoot + "/_temp.wav");
            DeleteIfExists(_masRoot + "/_temp.mp4");
        } catch
        {
            Program.Log(@"Failed to remove extracted temporary assets, please manually erase the files at """ + _masRoot + @"""");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}