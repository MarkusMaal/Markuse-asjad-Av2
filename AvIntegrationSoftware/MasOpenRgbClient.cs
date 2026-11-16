using System;
using System.Linq;
using System.Threading;
using OpenRGB.NET;
using OpenRGB.NET.Utils;
using Color = OpenRGB.NET.Color;

namespace AvIntegrationSoftware;

public class MasOpenRgbClient
{
    private readonly OpenRgbClient? _client;
    private readonly Device[]? _devices;
    private readonly string[]? _profiles;
    private (double h, double s, double v) _colors;

    public MasOpenRgbClient()
    {
        try
        {
            _client = new OpenRgbClient();
            _client.Connect();
            Program.Log("Connected to OpenRGB");
            _devices = _client.GetAllControllerData();
            _profiles = _client.GetProfiles();
            _client.LoadProfile(_profiles[0]);
            foreach (var device in _devices)
            {
                _client.SetCustomMode(device.Index);
                Program.Log($"Found RGB device: {device.Name}");
            }
            new Thread(UpdateThread).Start();
        }
        catch (TimeoutException)
        {
            Program.Log("Error connecting to OpenRGB - is the SDK server running?");
        }
    }

    private void UpdateThread()
    {
        Random r = new();
        if (_devices == null || _client == null || _profiles == null) return;

        /*var offsetR = 0.0;
        var offsetG = -3.0;
        var offsetB = -6.0;
        var offsetY = -9.0;*/
        int[] partSize = [r.Next(1,5), r.Next(1,5), r.Next(1,5), r.Next(1,5)];
        var dir = 0.005;
        var opa = 0.25;
        while (!App.ShutdownNow)
        {
            var deviceColors = new Color[_devices.Length][];
            for (var index = 0; index < _devices.Length; index++)
            {
                var arr = new Color[_devices[index].Leds.Length];

                for (var i = 0; i < arr.Length; i++)
                {
                    /*if (_devices[index].Leds.Length < 10)
                    {*/
                        arr[i] = ColorUtils.FromHsv(_colors.h, _colors.s, _colors.v * opa);
                       /* continue;
                    }

                    var pI = i % 32;
                    if (pI < offsetR + partSize[0] && pI > offsetR - partSize[0])
                        arr[i] = new Color(255, 0, 23);
                    else if (pI < offsetG + partSize[1] && pI > offsetG - partSize[1])
                        arr[i] = new Color(94, 255);
                    else if (pI < offsetB + partSize[2] && pI > offsetB - partSize[2])
                        arr[i] = new Color(0, 153, 255);
                    else if (pI < offsetY + partSize[3] && pI > offsetY - partSize[3])
                        arr[i] = new Color(255, 204);
                    else
                        arr[i] = ColorUtils.FromHsv(_colors.h, _colors.s, _colors.v * opa);*/
                }

                deviceColors[index] = [.. arr, .. arr];
            }

            var colorOffsets = Enumerable.Range(0, _devices.Length).Select(x => x).ToArray();
            for (var index = 0; index < _devices.Length; index++)
            {
                var colors = deviceColors[index];
                if (colors.Length == 0)
                    continue;

                var slice = colors.AsSpan().Slice(colorOffsets[index]++ % _devices[index].Leds.Length,
                    _devices[index].Leds.Length);
                _client.UpdateLeds(index, slice);
            }

            Thread.Sleep(16);
            //offsetR += 0.12;
            //offsetG += 0.12;
            //offsetB += 0.12;
            //offsetY += 0.12;
            opa += dir;
            if (opa is < 1.0 and > 0.25) continue;
            dir = -dir;
            opa += dir;
            /*
            if (offsetR > 100)
            {
                partSize[0] = r.Next(1, 5);
                offsetR = r.Next(-64, -9);
            }
            if (offsetG > 100)
            {
                partSize[1] = r.Next(1, 5);
                offsetG = r.Next(-64, -9);
            }
            if (offsetB > 100)
            {
                partSize[2] = r.Next(1, 5);
                offsetB = r.Next(-64, -9);
            }

            if (!(offsetY > 100)) continue;
            partSize[3] = r.Next(1, 5);
            offsetY = r.Next(-64, -9);*/
        }
        Program.Log("Stopping light effects");
        _client.LoadProfile(_profiles[^1]);
    }
    

    public void SetSolidColor(byte r, byte g, byte b)
    {
        if (_devices == null || _client == null)
        {
            Program.Log("OpenRGB not connected - not setting color!");
            return;
        }
        Program.Log($"Setting LED colors to RGB({r}, {g}, {b})");
        _colors = new Color(r, g, b).ToHsv();
    }
}