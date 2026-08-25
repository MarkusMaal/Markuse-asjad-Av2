using System.Collections.Generic;
using System.IO;

namespace AvDeviceManager;

public class DeviceCollection
{
    private List<Device> Devices { get; }

    public DeviceCollection(string maiaRoot)
    {
        var maiaFile = Path.Join(maiaRoot, "whitelist.txt");
        var txt = File.OpenText(maiaFile);
        Devices = [];
        while (!txt.EndOfStream)
        {
            var line = txt.ReadLine();
            if (!line?.Contains('-') ?? false) continue;
            if (line == null) continue;
            Devices.Add(new Device()
            {
                DeviceIp = line.Split('-')[0].TrimEnd(),
                DeviceType = line.Split('-')[1].TrimStart()
            });
        }
        txt.Close();
    }

    public Device[] GetDevices()
    {
        return [.. Devices];
    }
}