using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MasCommon;

namespace AvControlPanel.Models.MarkuStation;

public class Config
{
    public List<Game> Games { get; set; } = [];
    public bool CreepypastaIntro { get; set; }
    public bool SpecialIntro { get; set; }
    public bool PlayIntros { get; set; }
    public bool LegacyIntro { get; set; } 
    public MonitorMode MonitorMode { get; set; }
    private readonly string _masRoot = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mas");

    public void SaveConfig()
    {
        // general settings
        TextWriter tw = File.CreateText(Path.Join(_masRoot, "setting.txt"));
        tw.WriteLine(CreepypastaIntro ? "true" : "false");
        tw.WriteLine(SpecialIntro ? "true" : "false");
        tw.WriteLine(PlayIntros ? "true" : "false");
        tw.Write(LegacyIntro ? "true" : "false");
        tw.Close();
        tw.Dispose();
        
        // games list
        TextWriter gameNamesTxt = File.CreateText(Path.Join(_masRoot, "ms_games.txt"));
        TextWriter gameExecsTxt = File.CreateText(Path.Join(_masRoot, "ms_exec.txt"));
        gameNamesTxt.WriteLine("* MarkuStation mängude loetelu *");
        gameExecsTxt.WriteLine("* MarkuStation käivitaja loetelu (ärge eemaldage/lisage semikooloneid)*;");
        foreach (var game in Games)
        {
            gameNamesTxt.WriteLine(game.Name);
            gameExecsTxt.WriteLine(game.Executable + ";");
        }
        
        gameNamesTxt.WriteLine();
        gameExecsTxt.WriteLine();
        gameNamesTxt.Close();
        gameExecsTxt.Close();
        gameNamesTxt.Dispose();
        gameExecsTxt.Dispose();

        // monitor config
        TextWriter monitor = File.CreateText(Path.Join(_masRoot, "ms_display.txt"));
        monitor.WriteLine(MonitorMode switch
        {
            MonitorMode.External => "external",
            MonitorMode.Clone => "clone",
            MonitorMode.Internal => "internal",
            MonitorMode.Extend => "extend",
            _ => throw new ArgumentOutOfRangeException()
        });
        monitor.Close();
        monitor.Dispose();
        Program.Log("Saved MarkuStation configuration");
    }
    
    public void LoadConfig()
    {
        var sFile = Path.Join(_masRoot, "setting.txt");
        if (!Program.CheckFiles(Verifile.FileScope.MarkuStation)) return;
        // general settings
        TextReader tr = File.OpenText(sFile);
        CreepypastaIntro = tr.ReadLine() == "true";
        SpecialIntro = tr.ReadLine() == "true";
        PlayIntros = tr.ReadLine() == "true";
        LegacyIntro = tr.ReadLine() == "true";
        tr.Close();
        tr.Dispose();
        
        // monitor setting
        TextReader monitor = File.OpenText(Path.Join(_masRoot, "ms_display.txt"));
        var modeStr = monitor.ReadLine();
        MonitorMode = modeStr switch
        {
            "internal" => MonitorMode.Internal,
            "extend" => MonitorMode.Extend,
            "external" => MonitorMode.External,
            "clone" => MonitorMode.Clone,
            _ => throw new ArgumentOutOfRangeException()
        };
        monitor.Close();
        monitor.Dispose();
        
        // games list
        TextReader gameNamesTxt = File.OpenText(Path.Join(_masRoot, "ms_games.txt"));
        TextReader gameExecsTxt = File.OpenText(Path.Join(_masRoot, "ms_exec.txt"));
        var gameNames =  gameNamesTxt.ReadToEnd().Split('\n').Skip(1).ToArray();
        var gameExecs =  gameExecsTxt.ReadToEnd().Split(';').Skip(1).ToArray();
        gameNamesTxt.Close();
        gameExecsTxt.Close();
        gameNamesTxt.Dispose();
        gameExecsTxt.Dispose();
        
        Games.Clear();
        for (var i = 0; i <  gameExecs.Length; i++)
        {
            if (gameNames[i] == "") continue;
            Games.Add(new Game
            {
                Executable = gameExecs[i].Replace("\n", ""),
                Name = gameNames[i],
            });
        }
        Program.Log("Loaded MarkuStation configuration");
    }

    public Game[] GetGames()
    {
        return [.. Games];
    }
}