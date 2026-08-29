using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using AvControlPanel.Dialogs;
using MasCommon;

namespace AvControlPanel.Controls;

public partial class Configuration : UserControl
{
    public CommonConfig MasCommonConfig
    {
        get => GetValue(MasCommonConfigProperty);
        set => SetValue(MasCommonConfigProperty, value);
    }
    
    public static readonly StyledProperty<CommonConfig> MasCommonConfigProperty = AvaloniaProperty.Register<Configuration, CommonConfig>("MasCommonConfig");

    public Bitmap DesktopBackground
    {
        get => GetValue(DesktopBackgroundProperty);
        set => SetValue(DesktopBackgroundProperty, value);
    }
    
    public static readonly StyledProperty<Bitmap> DesktopBackgroundProperty = AvaloniaProperty.Register<Configuration, Bitmap>("DesktopBackground");

    public Bitmap LoginBackground
    {
        get => GetValue(LoginBackgroundProperty);
        set => SetValue(LoginBackgroundProperty, value);
    }
    
    public static readonly StyledProperty<Bitmap> LoginBackgroundProperty = AvaloniaProperty.Register<Configuration, Bitmap>("LoginBackground");

    public Bitmap UncommonBackground
    {
        get => GetValue(UncommonBackgroundProperty);
        set => SetValue(UncommonBackgroundProperty, value);
    }
    
    public static readonly StyledProperty<Bitmap> UncommonBackgroundProperty = AvaloniaProperty.Register<Configuration, Bitmap>("UncommonBackground");
    public Configuration()
    {
        InitializeComponent();
    }

    private void ConfigCheck(object? sender, RoutedEventArgs e)
    {
        // backwards compatibility
        var saveprog = "";
        saveprog += MasCommonConfig.ShowLogo ? "true;" : "false;";
        saveprog += MasCommonConfig.AllowScheduledTasks ? "true;" : "false;";
        saveprog += MasCommonConfig.AutostartNotes ? "true;" : "false;";
        try
        {
            File.WriteAllText(Path.Join(App.MasRoot, "mas.cnf"), saveprog);
            MasCommonConfig.Save(App.MasRoot);
            Program.Log("Saved common configuration");
        }
        catch (Exception ex)
        {
            Program.Log($"Failed to save common configuration: {ex.Message}");
            ConfigNoticeLabel.Content = "Neid sätteid ei saa hetkel muuta. Olge kindlad, et kirjutamise ligipääs failidele mas.cnf ja Config.json oleks saadaval.";
        }
    }

    private async void ChangeDesktop(object? sender, TappedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            var filename = files[0].Path.AbsolutePath;
            if (OperatingSystem.IsWindows())
            {
                ThumbDesktop.Source = null;
                foreach (var p in Process.GetProcesses())
                {
                    if ((p.ProcessName == "Markuse arvuti integratsioonitarkvara.exe") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara.EXE") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara"))
                    {
                        p.Kill();
                    }
                }
                var pr = new Process();
                pr.StartInfo.FileName = App.MasRoot + "/ChangeWallpaper.exe";
                pr.StartInfo.UseShellExecute = false;
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pr.StartInfo.CreateNoWindow = true;
                pr.StartInfo.Arguments = App.MasRoot.Replace("/", "\\") + "\\bg_login.png";
                pr.Start();
                pr.StartInfo.FileName = "cmd.exe";
                pr.StartInfo.Arguments = "/k move " + App.MasRoot + "\\bg_desktop.png " + App.MasRoot + "\\bg_desktop.temp";
                pr.Start();
                while (!File.Exists(App.MasRoot + "/bg_desktop.temp")) { }
                File.Copy(filename, App.MasRoot + "/bg_desktop.png");
                File.Delete(App.MasRoot + "/bg_desktop.temp");
                pr.StartInfo.Arguments = "";
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                pr.StartInfo.CreateNoWindow = false;
                pr.StartInfo.FileName = App.MasRoot + "/Markuse asjad/Markuse arvuti integratsioonitarkvara.exe";
                pr.Start();
                if (File.Exists(App.MasRoot + "/bg_desktop.png"))
                {
                    ThumbDesktop.Source = new Bitmap(App.MasRoot + "/bg_desktop.png");
                }
                foreach (var p in Process.GetProcesses())
                {
                    if (p.ProcessName is "cmd.exe" or "cmd.EXE" or "cmd" or "conhost.exe" or "conhost.EXE" or "conhost")
                    {
                        p.Kill();
                    }
                }
            } else if (OperatingSystem.IsLinux()) {
                ThumbDesktop.Source = null;
                // move existing background image
                Program.RunCommand("mv", "\"" + App.MasRoot + "/bg_desktop.png\" \"" + App.MasRoot + "/bg_desktop.temp\"");
                // replace background image
                while (!File.Exists(App.MasRoot + "/bg_desktop.temp")) { }
                Program.RunCommand("cp", "\"" + filename + "\" \"" + App.MasRoot + "/bg_desktop_l.png\"");
                // delete temporary background
                File.Delete(App.MasRoot + "/bg_desktop.temp");
                // remove cropped background images
                Program.RunCommand("rm", "\"" + App.MasRoot + "/bg_desktop_l.png\"");
                Program.RunCommand("rm", "\"" + App.MasRoot + "/bg_desktop_r.png\"");
                // crop left/right backgrounds
                Program.RunCommand("magick", "\"" + App.MasRoot + "/bg_desktop.png\" -crop 1280x1024+0+0 \"" + App.MasRoot + "/bg_desktop_l.png\"");
                Program.RunCommand("magick", "\"" + App.MasRoot + "/bg_desktop.png\" -crop 1280x1024+3200+0 \"" + App.MasRoot + "/bg_desktop_r.png\"");
                // apply desktop background
                Program.RunCommand("sh", App.MasRoot + "/change_bg.sh");
                if (File.Exists(App.MasRoot + "/bg_desktop.png"))
                {
                    ThumbDesktop.Source = new Bitmap(App.MasRoot + "/bg_desktop.png");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"Change desktop function error: {ex.Message}");
        }
    }

    private async void ChangeLogin(object? sender, TappedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            var filename = files[0].Path.AbsolutePath;
            ThumbLockscreen.Source = null;
            File.Delete(App.MasRoot + "/bg_login.png");
            File.Copy(filename, App.MasRoot + "/bg_login.png");
            if (File.Exists(App.MasRoot + "/bg_desktop.png"))
            {
                ThumbLockscreen.Source = new Bitmap(App.MasRoot + "/bg_login.png");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"ChangeLogin function error: {ex.Message}");
        }
    }

    private async void ChangeMini(object? sender, TappedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            var filename = files[0].Path.AbsolutePath;
            ThumbMiniversion.Source = null;
            File.Delete(App.MasRoot + "/bg_uncommon.png");
            File.Copy(filename, App.MasRoot + "/bg_uncommon.png");
            if (File.Exists(App.MasRoot + "/bg_uncommon.png"))
            {
                ThumbMiniversion.Source = new Bitmap(App.MasRoot + "/bg_uncommon.png");
            }
        }
        catch (Exception ex)
        {
            Program.Log($"ChangeMini function error: {ex.Message}");
        }
    }

    private void SwapBgs(object? sender, RoutedEventArgs e)
    {
            if (OperatingSystem.IsWindows())
            {
                foreach (var p in Process.GetProcesses())
                {
                    if (p.ProcessName is "Markuse arvuti integratsioonitarkvara.exe" or "Markuse arvuti integratsioonitarkvara.EXE" or "Markuse arvuti integratsioonitarkvara")
                    {
                        p.Kill();
                    }
                }
                ThumbDesktop.Source = null;
                ThumbLockscreen.Source = null;
                ThumbMiniversion.Source = null;
                var rootBackSlash = App.MasRoot.Replace("/", "\\");
                //võta kasutusele ajutine taustapilt
                var pr = new Process();
                pr.StartInfo.FileName = App.MasRoot + "/ChangeWallpaper.exe";
                pr.StartInfo.UseShellExecute = false;
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pr.StartInfo.CreateNoWindow = true;
                pr.StartInfo.Arguments = rootBackSlash + "\\bg_login.png";
                pr.Start();
                pr.StartInfo.FileName = "cmd.exe";
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_desktop.png " + rootBackSlash + "\\bg_desktop.temp";
                pr.Start();
                while (!File.Exists(App.MasRoot + "/bg_desktop.temp")) { }
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_uncommon.png " + rootBackSlash + "\\bg_desktop.png";
                pr.Start();
                while (!File.Exists(App.MasRoot + "/bg_desktop.png")) { }
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_desktop.temp " + rootBackSlash + "\\bg_uncommon.png";
                pr.Start();
                while (!File.Exists(App.MasRoot + "/bg_uncommon.png")) { }
                pr.StartInfo.Arguments = "";
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                pr.StartInfo.CreateNoWindow = false;
                pr.StartInfo.FileName = App.MasRoot + "/Markuse asjad/Markuse arvuti integratsioonitarkvara.exe";
                pr.Start();
                ReloadThumbs();
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.ProcessName is "cmd.exe" or "cmd.EXE" or "cmd" or "conhost.exe" or "conhost.EXE" or "conhost")
                        {
                            p.Kill();
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            } else if (OperatingSystem.IsLinux()) {
                // remove cropped background images
                Program.RunCommand("rm", "\"" + App.MasRoot + "/bg_desktop_l.png\"");
                Program.RunCommand("rm", "\"" + App.MasRoot + "/bg_desktop_r.png\"");
                // swap bg_desktop and bg_uncommon (only use when swapping backgrounds)
                Program.RunCommand("mv", "\"" + App.MasRoot + "/bg_desktop.png\" \"" + App.MasRoot + "/temp.png\"");
                Program.RunCommand("mv", "\"" + App.MasRoot + "/bg_uncommon.png\" \"" + App.MasRoot + "/bg_desktop.png\"");
                Program.RunCommand("mv", "\"" + App.MasRoot + "/temp.png\" \"" + App.MasRoot + "/bg_uncommon.png\"");
                // crop left/right backgrounds
                Program.RunCommand("magick", "\"" + App.MasRoot + "/bg_desktop.png\" -crop 1280x1024+0+0 \"" + App.MasRoot + "/bg_desktop_l.png\"");
                Program.RunCommand("magick", "\"" + App.MasRoot + "/bg_desktop.png\" -crop 1280x1024+3200+0 \"" + App.MasRoot + "/bg_desktop_r.png\"");
                // apply desktop background
                Program.RunCommand("sh", App.MasRoot + "/change_bg.sh");
                // update thumbnails
                ReloadThumbs();
            }
    }

    private void ReloadThumbs()
    {
        var bitmapDesktop = new Bitmap(Path.Combine(App.MasRoot, "bg_desktop.png"));
        var bitmapLogin = new Bitmap(Path.Combine(App.MasRoot, "bg_login.png"));
        var bitmapUncommon = new Bitmap(Path.Combine(App.MasRoot, "bg_uncommon.png"));
        DesktopBackground = bitmapDesktop;
        LoginBackground = bitmapLogin;
        UncommonBackground = bitmapUncommon;
        DataContext = null;
        DataContext = this;
    }

    private void EditScheds(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        if (AllowScheduledTasksCheck.IsChecked ?? false)
        {
            if (File.Exists(App.MasRoot + "/events.txt"))
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(App.MasRoot + "/events.txt")
                {
                    UseShellExecute = true,
                };
                p.Start();

            } else
            {
                mw.MessageBoxShow("Sündmuste faili ei eksisteeri", "Probleem", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        } else
        {
            mw.MessageBoxShow("Ajastatud sündmused on keelatud", "Probleem", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
        }
    }

    private void EditFg(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        var cpd = new ColorPickerDialog()
        {
            Background = mw.Background,
            Foreground = mw.Foreground,
            CurrentColor = mw.Scheme.ForegroundColor
        };
        cpd.DialogOk += args =>
        {
            if (args.Color == null) return;
            mw.Scheme.ForegroundColor = args.Color!.Value;
            mw.Scheme.SaveScheme(App.MasRoot);
            mw.Foreground = new SolidColorBrush(args.Color!.Value);
            Program.SendDesktopIconCommand("ReloadTheme");
        };
        cpd.ShowDialog(mw);
    }

    private void EditBg(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
        var cpd = new ColorPickerDialog()
        {
            Background = mw.Background,
            Foreground = mw.Foreground,
            CurrentColor = mw.Scheme.BackgroundColor
        };
        cpd.DialogOk += args =>
        {
            if (args.Color == null) return;
            mw.Scheme.BackgroundColor = args.Color!.Value;
            mw.Scheme.SaveScheme(App.MasRoot);
            mw.Background = new SolidColorBrush(args.Color!.Value);
            Program.SendDesktopIconCommand("ReloadTheme");
        };
        cpd.ShowDialog(mw);
    }
}