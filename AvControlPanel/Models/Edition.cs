using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvControlPanel.Models;

public partial class Edition
{
    /// <summary>
    /// Edition name (e.g. Pro, Premium, Basic+)
    /// </summary>
    public string EditionName { get; set; }
    
    /// <summary>
    /// Version number (e.g. 10.4)
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Build number - first letter(s) represent(s) the initial(s) for the edition name, next few numbers represent major version number and remaining numbers represent minor revisions. The last lowercase letter represents device type (a = physical desktop computer, b = virtual computer, c = tablet)
    /// </summary>
    public string BuildNo { get; set; }
    
    /// <summary>
    /// Boolean representing if a system integrity check has been run during the deployment process, stored in edition.txt as either "Yes" or "No"
    /// </summary>
    public bool Tested { get; set; }
    
    /// <summary>
    /// The user who initially started the deployment process for this computer
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// System language during the deployment process
    /// </summary>
    public string Language { get; set; }
    
    /// <summary>
    /// Operating system kernel version during the initial deployment process
    /// </summary>
    public string WinVer { get; set; }
    
    /// <summary>
    /// List of optional features, stored in edition.txt with dashes (-) used as separators
    /// </summary>
    public List<string>? Features { get; set; }
    
    /// <summary>
    /// Insecure PIN code for this computer, for legacy compatibility
    /// </summary>
    public string Pin { get; set; }
    
    /// <summary>
    /// Name for the current version
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Verifile 1.0 hash
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Root directory for Markus' stuff deployment
    /// </summary>
    private static string MasRoot => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";

    public Edition()
    {
        var lines =  File.ReadAllLines(Path.Combine(MasRoot, "edition.txt"));
        if (lines[0] != "[Edition_info]") throw new FormatException("The edition file does not start with [Edition_info]");
        EditionName = lines[1];
        Version = lines[2];
        BuildNo = lines[3];
        Tested = lines[4] == "Yes";
        Username = lines[5];
        Language = lines[6];
        WinVer = lines[7];
        Features = [.. lines[8].Split('-')];
        Pin = lines[9];
        Name = lines[10];
        if (lines.Length < 12) return;
        Hash = lines[11];
    }
    
    // Avalonia specific stuff
    [JsonIgnore]
    public string EditionInfo =>
        $"""
         Versioon: {Version}
         Järk: {BuildNo}
         Nimi: {Name}
         Keel: {Language}
         Juurutatud?: {Tested}
         Muutmisaeg: {File.GetLastWriteTime(Path.Join(App.MasRoot, "edition.txt"))}
         Kinnituskood: {Pin}
         Olek: {Program.Status}
         """;
    
    [JsonIgnore]
    public SolidColorBrush EditionColor =>
        new(EditionName switch
        {
            "Pro" => Colors.DeepSkyBlue,
            "Premium" => Colors.DarkRed,
            "Basic" or "Basic+" => Colors.Yellow,
            "Ultimate" => Colors.BlueViolet,
            "Starter" => Colors.Lime,
            _ => Colors.Gray
        });
    
    [JsonIgnore]
    public static string CpanelVersion => "versioon " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0");

    [JsonIgnore]
    public string DeviceDescription => BuildNo[^1] switch
    {
        'a' => "Markuse arvuti asjad",
        'b' => "Markuse virtuaalarvuti asjad",
        'c' => "Markuse tahvelarvuti asjad",
        _ => "Markuse asjad muudele seadmetele"
    };

    public Bitmap FeatMm => GenerateMark(Features?.Contains("MM"));
    public Bitmap FeatIp => GenerateMark(Features?.Contains("IP"));
    public Bitmap FeatTs => GenerateMark(Features?.Contains("TS"));
    public Bitmap FeatRm => GenerateMark(Features?.Contains("RM"));
    public Bitmap FeatCs => GenerateMark(Features?.Contains("CS"));
    public Bitmap FeatRd => GenerateMark(Features?.Contains("RD"));
    public Bitmap FeatWx => GenerateMark(Features?.Contains("WX"));
    public Bitmap FeatLt => GenerateMark(Features?.Contains("LT"));
    public Bitmap FeatGp => GenerateMark(Features?.Contains("GP"));

    private static Bitmap GenerateMark(bool? value)
    {
        return (value ?? false) ? new Bitmap(AssetLoader.Open(new Uri("avares://AvControlPanel/Assets/success.gif"))) : new Bitmap(AssetLoader.Open(new Uri("avares://AvControlPanel/Assets/failure.gif")));
    }


    // Required for generating trimmed executables
    [JsonSerializable(typeof(Edition))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class EditionSourceGenerationContext : JsonSerializerContext
    {

    }
}