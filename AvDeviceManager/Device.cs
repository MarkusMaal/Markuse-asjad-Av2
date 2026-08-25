using System.IO;
using Avalonia.Media.Imaging;

namespace AvDeviceManager;

public class Device
{
    public required string DeviceType { get; init; }
    public required string DeviceIp { get; set; }

    public string DeviceTypeFriendly => DeviceType switch
    {
        "mas" => "Markuse arvuti",
        "masv" => "Markuse virtuaalarvuti",
        "mat" => "Muu Markuse seade",
        "masl" => "Markuse arvuti (Linux)",
        "mtel" => "Markuse telefon",
        "mta" => "Markuse tahvelarvuti",
        "clf" => "Puhverserver",
        _ => DeviceType
    };

    public Bitmap DeviceIcon => new(Path.Join(App.MasRoot, "mas_neoglass", DeviceType switch
    {
        "mas" => "mas_computers.png",
        "masv" => "mas_virtualpc.png",
        "masl" => "mas_computers.png",
        "mtel" => "mas_phone.png",
        "mta" => "mas_tablet.png",
        _ => "mas_general.png"
    }));
}