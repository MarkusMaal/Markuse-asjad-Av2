using System.IO;
using Avalonia.Media;

namespace AvControlPanel.Models;

public class Scheme
{
    public Color BackgroundColor { get; set; }

    public Color ForegroundColor { get; set; }
    
    public void LoadScheme(string masRoot)
    {
        TextReader textReader = File.OpenText(Path.Join(masRoot, "scheme.cfg"));
        var strArray1 = textReader.ReadLine()?.Split(';');
        if (strArray1 == null)
            return;
        var strArray2 = strArray1[0].Split(':');
        var strArray3 = strArray1[1].Split(':');
        BackgroundColor = Color.FromArgb(255, byte.Parse(strArray2[0]), byte.Parse(strArray2[1]), byte.Parse(strArray2[2]));
        ForegroundColor = Color.FromArgb(255, byte.Parse(strArray3[0]), byte.Parse(strArray3[1]), byte.Parse(strArray3[2]));
        textReader.Close();
        textReader.Dispose();
    }

    public void SaveScheme(string masRoot)
    {
        TextWriter text = File.CreateText(Path.Join(masRoot, "scheme.cfg"));
        text.Write($"{BackgroundColor.R}:{BackgroundColor.G}:{BackgroundColor.B}:;{ForegroundColor.R}:{ForegroundColor.G}:{ForegroundColor.B}:;");
        text.Close();
        text.Dispose();
    }
}