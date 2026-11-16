using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using SFML.Audio;
using SFML.System;
using File = TagLib.File;

namespace AvParty;

public partial class MainWindow : Window
{
    private Music? _mp;
    private bool _isPlaying;
    private string[]? _playlist;
    private int _track;
    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherTimer _volume = new();
    private bool _custMessage;
    private readonly string _idleString = "Hetkel ei mängi ühtegi pala";
    private string _fileName = "";
    public MainWindow()
    {
        InitializeComponent();
        _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        _volume.Interval = new TimeSpan(0, 0, 0, 0, 20);
        VolumeControl.Value = _mp?.Volume ?? 100;
        SizeCheck();
        // prevent window borders from being rendered if not in a Linux DE
        if (!OperatingSystem.IsLinux())
        {
            //Console.WriteLine("Windows/Mac paranduste aktiveerimine...");
            ExtendClientAreaToDecorationsHint = false;
            WindowDecorations = WindowDecorations.None;
        }
        _timer.Tick += (_, _) =>
            {
                /*if ((this.WindowState != WindowState.Minimized) && (this.WindowState == WindowState.Normal))
                {
                    this.WindowState = WindowState.FullScreen;
                }*/

                if (_mp?.Status == SoundStatus.Stopped)
                {
                    _track++;
                    _custMessage = false;
                    if (Playlist.IsVisible)
                    {
                        Playlist.SelectedIndex = _track;
                    }
                    if (_track >= _playlist?.Length)
                    {
                        _isPlaying = false;
                        BigTitle.Content = _idleString;
                    } else
                    {
                        PlayTrack();
                    }
                }
                if (_mp?.Status == SoundStatus.Playing && !_custMessage)
                {
                    BigTitle.Content = "Hetkel esitusel: " + FormatTitle(_fileName);
                    PausePlay.Content = "||";
                    PausePlay.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                } else if (_mp?.Status != SoundStatus.Playing)
                {
                    PausePlay.Content = ">";
                    PausePlay.Background = new SolidColorBrush(Colors.Lime);
                }

                var vol = (int?)_mp?.Volume ?? 100;
                StatusLabel.Content =
                    $"{DateTime.Now}  |  {vol}%  |  {NiceTime(_mp?.PlayingOffset.AsMilliseconds())} / {NiceTime(_mp?.Duration.AsMilliseconds())}";
                if (_mp?.Status == SoundStatus.Paused) {
                    StatusLabel.Content += " (paus)";
                }
            };
            _volume.Tick += (_, _) =>
            {
                if (_mp?.Duration.AsMilliseconds() < 30000)
                {
                    _mp.Volume = 100;
                    VolumeControl.Value = _mp.Volume;
                    VolumeControl.IsEnabled = true;
                    return;
                }
                if (_mp?.PlayingOffset.AsMilliseconds() < 10000L)
                {
                    _mp?.Volume = (int)((_mp?.PlayingOffset.AsMilliseconds() ?? 0) / 100.0);
                    VolumeControl.Value = _mp?.Volume ?? 100;
                    VolumeControl.IsEnabled = false;
                }
                if ((_mp?.PlayingOffset.AsMilliseconds() >= 10000L) && (_mp?.PlayingOffset.AsMilliseconds() < 10500L))
                {
                    _mp?.Volume = 100;
                    VolumeControl.Value = _mp?.Volume ?? 100;
                    VolumeControl.IsEnabled = true;
                }
                else if (_mp?.PlayingOffset.AsMilliseconds() > _mp?.Duration.AsMilliseconds() - 11000L)
                {
                    var level = (((_mp?.Duration.AsMilliseconds() ?? 0) - 1000 - (_mp?.PlayingOffset.AsMilliseconds() ?? 0)) / 100);
                    if (level < 0)
                    {
                        level = 0;
                    }
                    _mp?.Volume = level;
                    VolumeControl.Value = _mp?.Volume ?? 100;
                    VolumeControl.IsEnabled = false;
                }
            };
            _timer.Start();
            _volume.Start();
    }

    private static string FormatTitle(string fileName)
    {
        var meta = File.Create(fileName);
        if (meta.Tag.Title?.Length == 0) return Path.GetFileNameWithoutExtension(new FileInfo(fileName).Name);
        return meta.Tag.AlbumArtists.Length switch
        {
            0 when (meta.Tag.Performers.Length == 0 && meta.Tag.Album?.Length > 0) =>
                $"{meta.Tag.Title} ({meta.Tag.Album})",
            0 when meta.Tag.Performers.Length == 0 => meta.Tag.Title ??
                                                      Path.GetFileNameWithoutExtension(new FileInfo(fileName).Name),
            _ => meta.Tag.Album?.Length > 0
                ? $"{string.Join(", ", meta.Tag.AlbumArtists.Concat(meta.Tag.Performers).ToArray())} - {meta.Tag.Title} ({meta.Tag.Album})"
                : $"{string.Join(", ", meta.Tag.AlbumArtists.Concat(meta.Tag.Performers).ToArray())} - {meta.Tag.Title}"
        };
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Program.StreamMode)
        {
            WindowState = WindowState.FullScreen;
            WindowState = WindowState.Maximized;
            WindowState = WindowState.Normal;
            ExtendClientAreaToDecorationsHint = false;
            WindowDecorations = WindowDecorations.Full;
            MinimizeButton.Content = "";
            MinimizeButton.Padding = new Thickness(0, 5);
            MinimizeButton.BorderThickness = new Thickness(0);
            Width = 1280;
            Height = 1024;
            Title = "Pidu!";
            CloseButton.IsVisible = false;
            return;
        }
        if (!OperatingSystem.IsMacOS() || Program.StreamMode) return;
        new Thread(() => {
            Thread.Sleep(1000);
            Dispatcher.UIThread.Post(() => {
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
                WindowState = WindowState.FullScreen;
            });
        }).Start();
    }
    
    
    private void SizeCheck()
    {
        if (Program.StreamMode) return;
        if (Width / Height > 3.0)
        {
            var padding = Width / 4;
            MainGrid.Margin = new Thickness(padding, 0, padding, 0);
        }
        else
        {
            MainGrid.Margin = new Thickness(0, 0, 0, 0);
        }
    }
        
    private static string NiceTime(int? millis)
    {
        if (millis == null) return "00:00";
        var totalSecs = (int)millis / 1000;
        if (totalSecs < 0 )
        {
            totalSecs = 0;
        }
        var totalMins = totalSecs / 60;
        var subsetSecs = totalSecs - totalMins * 60;
        return $"{totalMins.ToString().PadLeft(2, '0')}:{subsetSecs.ToString().PadLeft(2, '0')}";
    }

    private void PlayTrack()
    {
        if ((_track <= -1) || (!(_track < _playlist?.Length))) return;
        _fileName = _playlist[_track];
        _custMessage = false;
        _mp?.Dispose();
        _mp = new Music(_playlist[_track]);
        _mp.Play();
        if (Playlist.IsVisible)
        {
            Playlist.SelectedIndex = _track;
        }
    }

    private void ToggleFade(object? sender, RoutedEventArgs e)
    {
        _volume.IsEnabled = !_volume.IsEnabled;
        VolumeControl.IsEnabled = true;
        var me = (Button?)sender;
        me?.Content = _volume.IsEnabled ? "Hajumine välja" : "Hajumine sisse";
    }
    private void AddMusic_Click(object? sender, RoutedEventArgs e)
    {
        AddMusic();
    }

    private async void AddMusic()
    {
        try
        {
            // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = GetTopLevel(this);
            if (topLevel == null) { return; }
            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali muusikapala",
                AllowMultiple = true
            });

            if (files.Count < 1) return;
            if (_playlist != null)
            {
                var oldSongs = _playlist;
                _playlist = new string[_playlist.Length + files.Count];
                for (var i = 0; i < oldSongs.Length; i++)
                {
                    _playlist[i] = oldSongs[i];
                }
                for (var i = 0; i < files.Count; i++)
                {
                    _playlist[oldSongs.Length + i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                }
            }
            else
            {
                _playlist = new string[files.Count];
                for (var i = 0; i < files.Count; i++)
                {
                    _playlist[i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                }
                _track = 0;
                PlayTrack();
            }

            if (!Playlist.IsVisible) return;
            Playlist.Items.Clear();
            foreach (var song in _playlist)
            {
                Playlist.Items.Add(song);
            }
            Playlist.SelectedIndex = _track;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    private void ClearPlaylist_Click(object? sender, RoutedEventArgs e)
    {
        _mp?.Stop();
        _mp?.Dispose();
        _isPlaying = false;
        BigTitle.Content = _idleString;
        _playlist = [];
        Playlist.Items.Clear();
        _track = 0;
    }

    private void NextTrack_Click(object? sender, RoutedEventArgs e)
    {
        _mp?.Stop();
        _track++;
        PlayTrack();
    }

    private void LastTrack_Click(object? sender, RoutedEventArgs e)
    {
        _mp?.Stop();
        _track--;
        PlayTrack();
    }

    private void SeekBack_Click(object? sender, RoutedEventArgs e)
    {
        if (_mp?.PlayingOffset > Time.FromSeconds(10))
        {
            _mp?.PlayingOffset -= Time.FromSeconds(10);
        } else
        {
            _mp?.PlayingOffset = Time.FromMilliseconds(0);
        }
    }

    private void SeekForward_Click(object? sender, RoutedEventArgs e)
    {
        if (_mp?.PlayingOffset.AsSeconds() < _mp?.Duration.AsSeconds() - 10)
        {
            _mp?.PlayingOffset += Time.FromSeconds(10);
        }
        else
        {
            _mp?.PlayingOffset = _mp.Duration;
        }
    }

    private async void Message_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender == null) { return; }
            if (!_custMessage)
            {
                MessageEntry me = new();
                await me.ShowDialog(this);
                if (me.MessageField.Text == null) return;
                ((Button)sender).Content = "Pala pealkiri";
                _custMessage = true;
                BigTitle.Content = me.MessageField.Text;
            } else
            {
                ((Button)sender).Content = "Isikupärastatud teade";
                _custMessage = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
    private void Help_Click(object? sender, RoutedEventArgs e)
    {
        _ = MessageBoxShow("Selle programmiga saate kuulata muusikat ja\ntunda ennast nagu päris DJ. Üleval vasakus nurgas on\nerinevad nupud. Hajumine välja/sisse võmaldab\nheli automaatselt hajutada palade alguses ja lõpus\nVasakul pool ekraani on esitusloend ehk\nPlaylist). Seal on mängiv muusika ja tulevased palad.\nVajutage nupul Lisa muusikat, et lisada Playlisti\nuusi palasid. Tühjenda esitusloend nupp kustutab\nkõik palad playlistist. Teil on võimalik muuta\ntaustapilti playlisti ja nuppude taga. Vaiketausta-\npilt on rahvast pidutsemas, kuid te võite panna\nselle asemel enda pildi. Teil on võimalik ka kuulata\nYouTube-ist muusikat. See avab brauseri, millest saate\nvalida video ja minna muusikapala nime valimise juurde.\nTe saate valida värve ja fonte, et muuta loo pealkirja\nisikupärasemaks. Teave kuvab täpsema info programmi\nkohta ning spikker avab praeguse dialoogi.", "Spikker", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question);
    }

    private void About_Click(object? sender, RoutedEventArgs e)
    {
        _ = MessageBoxShow("Teeme pidu!\nNullist uuesti kirjutatud Avalonia UI ja .NET 10.0 raamistikes\nC# keeles.\n\nKasutab LibVLCSharp teeki\nVersioon 3.0 BEETA / 16.11.2026\nTegi: Markus Maal\nKogu muusika kuulub nende respektiivsetele omanikele.\nAutoriõiguste rikkumine ei ole lubatud!\n\nMis on uut?\n+ Üleminek Av2 platvormile\n* Väiksem failisuurus", "Pidu!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
    }

    // Reimplementation of WinForms MessageBox.Show
    private Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Pidu!", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
        var result = box.ShowWindowDialogAsync(this);
        return result;
    }

    private async void Background_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) { return; }

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali taustapilt",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                BackgroundImage.Source = new Bitmap(Uri.UnescapeDataString(files[0].Path.AbsolutePath));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
    private void BigTitle_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!Program.StreamMode) return;
        using var fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/songname.txt", FileMode.Create, FileAccess.Write);
        fs.Write(Encoding.UTF8.GetBytes(BigTitle?.Content?.ToString()?.Replace("Hetkel esitusel: ", "") ?? ""));
        fs.Close();
    }

    private void ShowPlaylist_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender == null) { return; }
        if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Left)
        {
            Playlist.IsVisible = !Playlist.IsVisible;
            if (_playlist is { Length: > 0 })
            {
                Playlist.Items.Clear();
                foreach (var song in _playlist)
                {
                    Playlist.Items.Add(song);
                }
                Playlist.SelectedIndex = _track;
            }
        }
        ((TextBlock)sender).Foreground = new SolidColorBrush(Color.FromRgb(191, 191, 255));
    }

    private void ShowPlaylist_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        ((TextBlock?)sender)?.Foreground = new SolidColorBrush(Color.FromRgb(128, 255, 255));
    }


    private void Playlist_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (!(Playlist?.SelectedItems?.Count > 0)) return;
        _mp?.Stop();
        _track = Playlist.SelectedIndex;
        PlayTrack();
    }

    private void PausePlay_Click(object? sender,  RoutedEventArgs e)
    {
        if (!_isPlaying)
        {
            _track = 0;
            PlayTrack();
            _isPlaying = true;
        } else if (_mp?.Status == SoundStatus.Playing)
        {
            _mp.Pause();
        }
        else if (_mp?.Status == SoundStatus.Paused)
        {
            _mp.Play();
        }
    }

    private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _mp?.Volume = (int)e.NewValue;
        if ((StatusLabel.Content != null) && (StatusLabel != null))
        {
            StatusLabel.Content = (StatusLabel.Content?.ToString() ?? "").Replace((int)e.OldValue + "%", (int)e.NewValue + "%");
        }
    }
    
    private void Underscore_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void X_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SizeCheck();
    }
    
    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        _mp?.Dispose();
    }

    private void Window_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.Pointer.IsPrimary || (e.ClickCount != 2)) return;
        TopButtons.IsVisible = !TopButtons.IsVisible;
        PlayControls.IsVisible = !PlayControls.IsVisible;
        Overlay.Opacity = (!TopButtons.IsVisible) ? 0 : 0.25;
        new Thread(() =>
        {
            Thread.Sleep(500);
            Dispatcher.UIThread.Post(() =>
            {
                if (!TopButtons.IsVisible && Playlist.IsVisible)
                {
                    Playlist.IsVisible = false;
                }
            });
        }).Start();
    }

    private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.F4)
        {
            _mp?.Stop();
            _playlist = [];
            _track = 0;
            Playlist.Items.Clear();
            BigTitle.Content = _idleString;
            return;
        }
        if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Avalonia.Input.Key.P:
                    if (_mp?.Status == SoundStatus.Playing) { _mp.Pause(); } else { _mp?.Play(); }
                    break;
                case Avalonia.Input.Key.O:
                    AddMusic();
                    break;
                case Avalonia.Input.Key.Left:
                    _mp?.Stop();
                    _track--;
                    PlayTrack();
                    break;
                case Avalonia.Input.Key.Right:
                    _mp?.Stop();
                    _track++;
                    PlayTrack();
                    break;
            }
        } else if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Alt)
        {
            switch (e.Key)
            {
                case Avalonia.Input.Key.Left:
                    _mp?.PlayingOffset -= Time.FromSeconds(10);
                    break;
                case Avalonia.Input.Key.Right:
                    _mp?.PlayingOffset += Time.FromSeconds(10);
                    break;
            }
        }
    }

}