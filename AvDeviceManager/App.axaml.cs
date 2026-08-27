using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MasCommon;

namespace AvDeviceManager;

public class App : Application
{
    public static readonly string MasRoot = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mas");
    private static readonly Verifile Vf = new();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var watch = Stopwatch.StartNew();
        var gootToGo = true;
        var errorHeader = "";
        var errorDescription = "";
        if (!Verifile.CheckVerifileTamper())
        {
            gootToGo = false;
            errorHeader = "Verifile viga";
            errorDescription = "Verifile räsi pole usaldusväärne. Programm sulgub nüüd!";
        }

        if (gootToGo)
        {
            if (Debugger.IsAttached) Console.WriteLine("Verifile räsi OK");
            var result = Vf.MakeAttestation();
            if (result != "VERIFIED")
            {
                errorHeader = "Verifile kontroll nurjus";
                errorDescription = result switch
                {
                    "TAMPERED" =>
                        "Arvuti riistvara ja väljaande info ei vasta genereeritud Verifile räsile. Selle võis põhjustada hiljutine riist- või tarkvara muutus.\n\nKood: VF_TAMPERED",
                    "LEGACY" =>
                        "Arvutisse paigaldatud Markuse asjade väljaanne ei vasta selle programmi toimimiseks vajalikele nõuetele.\n\nKood: VF_LEGACY",
                    "FOREIGN" =>
                        "Tuvastasime, et Markuse arvuti asjad ei ole sellesse arvutisse õigesti paigaldatud. Palun kasutage juurutamise tööriista, et sellesse arvutisse Markuse asjade tarkvara paigaldada.\n\nKood: VF_FOREIGN",
                    "FAILED" =>
                        "Püsivuskontrolli käivitamine nurjus. Veenduge, et arvutisse oleks paigaldatud ajakohane Java versioon ja verifile2.jar oleks Markuse asjad juurkataloogis.\n\nKood: VF_FAILED",
                    "MISSING" =>
                        "Püsivuskontrolli käivitamine nurjus. Veenduge, et arvutisse oleks paigaldatud ajakohane Java versioon ja verifile2.jar oleks Markuse asjad juurkataloogis.\n\nKood: VF_FAILED",
                    "BYPASS" =>
                        "Kood: VF_BYPASS",
                    _ => errorDescription
                };
                gootToGo = false;
            }

            if (gootToGo)
            {
                if (Debugger.IsAttached) Console.WriteLine("Verifile kontroll OK");
                var editionTxt = File.OpenText(Path.Combine(MasRoot, "edition.txt"));
                var edition = editionTxt.ReadToEnd();
                editionTxt.Close();
                var features = edition.Split('\n')[8];
                if (!features.Contains("RD") || !File.Exists(Path.Combine(MasRoot, "maia", "whitelist.txt")))
                {
                    gootToGo = false;
                    errorHeader = "Seade ei vasta nõuetele";
                    errorDescription = "Seda programmi ei ole võimalik kasutada selles Markuse asjad seadmes";
                }
                if (Debugger.IsAttached && gootToGo) Console.WriteLine("Erifunktsiooni kontroll OK");

                string[] extraFiles = ["mas_computers.png", "mas_general.png", "mas_phone.png", "mas_tablet.png", "mas_virtualpc.png"];
                foreach (var eF in extraFiles)
                {
                    if (File.Exists(Path.Combine(MasRoot, "mas_neoglass", eF))) continue;
                    gootToGo = false;
                    errorHeader = "Seade ei vasta nõuetele";
                    errorDescription = $"Vajalikku faili ei eksisteeri: {Path.Combine(MasRoot, "mas_neoglass", eF)}";
                    break;
                }
                if (Debugger.IsAttached && gootToGo) Console.WriteLine("Failikontroll OK");
            }
        }

        watch.Stop();
        if (Debugger.IsAttached) Console.WriteLine($"Kontrollide läbimiseks kulus {watch.ElapsedMilliseconds}ms");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (gootToGo)
            {
                desktop.MainWindow = new MainWindow();
            }
            else
            {
                desktop.MainWindow = new LaunchError
                {
                    ErrorHeader =
                    {
                        Text = errorHeader
                    },
                    ErrorDescription =
                    {
                        Text = errorDescription
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}