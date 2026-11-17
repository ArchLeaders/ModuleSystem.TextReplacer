using MessageStudio.Formats.BinaryText;
using SarcLibrary;
using TotkCommon;

Console.WriteLine("Text Replacer (c) Arch Leaders 2026 ");

string gameDumpFolderPath;
if (File.Exists("game_dump.txt")) {
    gameDumpFolderPath = File.ReadAllText("game_dump.txt");
    goto Setup;
}

GameDump:
Console.Write("Please enter the path to your game dump: ");

if (Console.ReadLine() is not { } gameFolderPath || !Directory.Exists(gameFolderPath)) {
    Console.WriteLine("Invalid game dump path");
    goto GameDump;
}

File.WriteAllText("game_dump.txt", gameFolderPath);
gameDumpFolderPath = gameFolderPath;

Setup:
var zsDicPackFilePath = Path.Combine(gameDumpFolderPath, "Pack", "ZsDic.pack.zs");
if (!File.Exists(zsDicPackFilePath)) {
    Console.WriteLine($"\nCould not find ZsDic.pack.zs in {Path.Combine(gameDumpFolderPath, "Pack")}");
    goto GameDump;
}

Zstd.Shared.LoadDictionaries(zsDicPackFilePath);

Retry:
Console.Write("Please enter the romfs folder of your mod: ");

if (Console.ReadLine() is not { } modFolderPath || !Directory.Exists(modFolderPath)) {
    Console.WriteLine("Invalid mod folder");
    goto Retry;
}

RetryLocale:
Console.Write("Please enter the target language/region (default USen): ");

var locale = Console.ReadLine() ?? "USen";
var filePath = Directory.GetFiles(Path.Combine(modFolderPath, "Mals")).FirstOrDefault(path => path.Contains(locale));

if (!File.Exists(filePath)) {
    Console.WriteLine($"Could not find {filePath ?? "any files in /Mals"}");
    goto RetryLocale;
}

var sarcLocaleData = Zstd.Shared.Decompress(File.ReadAllBytes(filePath), out int dictionaryId);
var mals = Sarc.FromBinary(sarcLocaleData);

ReplaceText:
Console.Write("Please enter the text you would like to replace: ");

if (Console.ReadLine() is not { } find || string.IsNullOrWhiteSpace(find)) {
    Console.WriteLine("Invalid text");
    goto ReplaceText;
}

RetryReplacementText:
Console.Write("Please enter the replacement text: ");

if (Console.ReadLine() is not { } replacement || string.IsNullOrWhiteSpace(replacement)) {
    Console.WriteLine("Invalid replacement text");
    goto RetryReplacementText;
}

foreach (var (file, data) in mals) {
    Console.WriteLine($"Searching {file}...");

    var msbt = Msbt.FromBinary(data);
    
    bool foundIn = false;
    foreach (var (key, value) in msbt.Where(kvp => kvp.Value.Text?.Contains(find) is true)) {
        Console.WriteLine($"Replaced all occurrences of {find} in {key}");
        value.Text = value.Text!.Replace(find, replacement);
        foundIn = true;
    }
    
    mals[file] = msbt.ToBinary();

    if (foundIn) {
        Console.WriteLine($"Replaced all occurrences of {find} in {file}");
    }
}

using (var ms = new MemoryStream()) {
    mals.Write(ms);

    if (!ms.TryGetBuffer(out var buffer)) {
        buffer = ms.ToArray();
    }
    
    var compressed = Zstd.Shared.Compress(buffer, dictionaryId);
    File.WriteAllBytes(filePath, compressed);
}

Console.WriteLine("Operation completed. Would you like to replace more text? (Y/n): ");
var userResponseKey = Console.ReadKey();

if (userResponseKey.Key != ConsoleKey.N) {
    Console.WriteLine();
    goto ReplaceText;
}