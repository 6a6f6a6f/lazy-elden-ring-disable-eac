using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using System.Collections.Generic;

using Microsoft.Win32;

using EldenBeat.Console;

Console.WriteLine("i: Elden Beat, the more lazy way to bypass EasyAntiCheat on Elden Ring");
Console.WriteLine("i: Developers, please, start reading EaC docs instead of lazy implementations without heartbeat!");
Console.WriteLine();

// dunno if Elden Ring is available for x86
var key = Environment.Is64BitProcess
    ? "SOFTWARE\\Wow6432Node\\Valve\\Steam"
    : "SOFTWARE\\Valve\\Steam";

var eldenRingPath = string.Empty;

using var steamKey = Registry.LocalMachine.OpenSubKey(key);
if (steamKey is not null)
{
    var steamInstallPath = steamKey.GetValue("InstallPath") as string;
    if (Directory.Exists(steamInstallPath))
    {
        var libraryFolders = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFolders))
        {
            var content = File.ReadAllLines(libraryFolders)
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(l => l.Trim())
                .ToList();

            var libraries = new List<string>();
            content.ForEach(l =>
            {
                l = l.Replace("\"", string.Empty);
                if (!l.StartsWith("path")) return;
                l = l.Remove(0, 4).Replace("\\\\", "\\").Trim();
                if (Directory.Exists(l)) libraries.Add(l);
            });
            
            libraries.ForEach(l =>
            {
                var path = Path.Combine(l,
                    "steamapps", "common", "ELDEN RING",
                    "Game", "EasyAntiCheat", "settings.json"
                );

                if (File.Exists(path)) eldenRingPath = l;
            });
        }
    }
}

if (!Directory.Exists(eldenRingPath))
{
    Console.WriteLine("e: Unable to locate Elden Ring root directory, please enter the path manually.");
    Console.Write("> ");
    var input = Console.ReadLine();
    if (!Directory.Exists(input))
    {
        Console.WriteLine("e: \"{0}\" is not a valid directory!", input);
        Environment.Exit(1);
    }
}

var settingsPath = Path.Combine(eldenRingPath,
    "steamapps", "common", "ELDEN RING",
    "Game", "EasyAntiCheat", "settings.json"
);

if (!File.Exists(settingsPath))
{
    Console.WriteLine("e: \"{0}\" is not a valid Elden Ring installation!", eldenRingPath);
    Environment.Exit(1);
}

var backupSettings = Path.Combine(eldenRingPath,
    "steamapps", "common", "ELDEN RING",
    "Game", "EasyAntiCheat", "settings.json.original"
);

if (File.Exists(backupSettings)) File.Delete(backupSettings);
File.Copy(settingsPath, backupSettings);
Console.WriteLine("i: Original settings backup saved at {0}", backupSettings);
Console.WriteLine("i: If you want to have a legitimate play again, just remove .original from the file name, " +
                  "and delete the existent settings.json");

var originalContent = File.ReadAllText(settingsPath, Encoding.UTF8);
var originalSettings = JsonSerializer.Deserialize<Settings>(originalContent);

var patchedSettings = originalSettings! with
{
    ProductId = Guid.NewGuid().ToString(),
    SandboxId = Guid.NewGuid().ToString(),
    DeploymentId = Guid.NewGuid().ToString()
};

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
};
var patchedContent = JsonSerializer.Serialize(patchedSettings, options);
File.WriteAllText(settingsPath, patchedContent);

Console.WriteLine("Done! Now you just need to launch Elden Ring from Steam launcher");