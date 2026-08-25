using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AvIntegrationSoftware;

public partial class ShowCode : Window
{
    internal Color Bg = Colors.Black;
    internal Color Fg = Colors.White;
    private readonly string? _devType;
    private readonly string? _devIp;
    private int _timeLeft = 80;
    private bool _initialized;
    private readonly DispatcherTimer _waitForClose = new();
    public ShowCode()
    {
        Program.CodeOpen = true;
        InitializeComponent();
        if (Design.IsDesignMode) return;
        Thread.Sleep(1000); // wait for server to finish writing the request_permission.maia file before continuing
        var fileName = App.MasRoot + "/maia/request_permission.maia";
        if (File.Exists(App.MasRoot + "/maia/request_permission.mai"))
        {
            fileName = App.MasRoot + "/maia/request_permission.mai";
        }

        if (!File.Exists(fileName))
        {
            _waitForClose.Tick += WaitForClose;
            _waitForClose.Interval = new TimeSpan(0, 0, 1);
            _waitForClose.Start();
            return;
        }
        var logContent = File.ReadAllText(fileName).Split(';');
        _devType = logContent[0];
        _devIp = logContent[1];
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random r = new();
        var code = "";
        for (var i = 0; i < 8; i++)
        {
            code += chars[r.Next(0, chars.Length)];
        }
        CodeText.Content = code;
        File.WriteAllText(string.Format(App.MasRoot + "/maia/{0}.{1}.maia", _devType, _devIp.Replace(".", "_")), GetHashString(_devType + "__" + code));
        File.Delete(fileName);
        _waitForClose.Tick += WaitForClose;
        _waitForClose.Interval = new TimeSpan(0, 0, 1);
        _waitForClose.Start();
    }
    
    [SuppressMessage("ReSharper", "ArrangeThisQualifier")]
    private void WaitForClose(object? sender, EventArgs e)
    {
        if (ReferenceEquals(CodeText.Content, "AAAAAAAA")) Close(); // no valid code, so close immediately
        if (!_initialized)
        {
            Background = new SolidColorBrush(Bg);
            Foreground = new SolidColorBrush(Fg);
        }
        _initialized = true;
        if (File.Exists(App.MasRoot + "/maia/close_popup.maia"))
        {
            File.Delete(string.Format(App.MasRoot + "/maia/{0}.{1}.maia", _devType, _devIp?.Replace(".", "_")));
            File.Delete(App.MasRoot + "/maia/close_popup.maia");
            Program.CodeOpen = false;
            this.Close();
        }
        else
        {
            _timeLeft -= 1;
            TimerLabel.Content = _timeLeft.ToString();
            if (_timeLeft != 0) return;
            File.Delete(string.Format(App.MasRoot + "/maia/{0}.{1}.maia", _devType, _devIp?.Replace(".", "_")));
            Program.CodeOpen = false;
            this.Close();
        }
    }

    private static byte[] GetHash(string inputString)
    {
        using HashAlgorithm algorithm = SHA256.Create();
        return SHA256.HashData(Encoding.UTF8.GetBytes(inputString));
    }

    private static string GetHashString(string inputString)
    {
        var sb = new StringBuilder();
        foreach (var b in GetHash(inputString))
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        File.Delete(string.Format(App.MasRoot + "/maia/{0}.{1}.maia", _devType, _devIp?.Replace(".", "_")));
        _waitForClose.Stop();
        Program.CodeOpen = false;
        this.Close();
    }
}