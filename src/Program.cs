using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MapMender;

internal class Program
{
    private const string W3XExtension = ".w3x";
    private const string FixedW3XSuffix = ".Rfixed.w3x";
    private const string R2SPatchSwitch = "no-r2s-patch";
    private const string UnitSkinPatchSwitch = "no-unitskin-patch";

    private static void Main(string[] args)
    {
        var commandLineArgs = args.ToList();
        var enableR2SPatch = !CheckFlag(commandLineArgs, R2SPatchSwitch);
        var enableUnitSkinPatch = !CheckFlag(commandLineArgs, UnitSkinPatchSwitch);

        var fileNames = commandLineArgs.Count > 0 ? commandLineArgs.AsReadOnly() : GetLocalW3XFiles();

        try
        {
            if (!fileNames.Any())
            {
                Console.WriteLine("No files were specified. Please, copy the map(s) to the MapMender's directory and start the MapMender or specify the path to a map(s) using command line arguments.");

                PrintUsage();
                return;
            }

            if (!enableR2SPatch && !enableUnitSkinPatch)
            {
                Console.WriteLine("At least one patch type should be allowed. It's forbidden to combine `/no-r2s-patch` and `/no-unitskin-patch` flags.");
                PrintUsage();
                return;
            }

            Console.WriteLine($"Detected {fileNames.Count} {(fileNames.Count == 1 ? "map" : "maps")}. Processing...\r\n");

            foreach (var fileName in fileNames)
            {
                var mapProcessor = new MapProcessor(enableR2SPatch, enableUnitSkinPatch);

                mapProcessor.Process(fileName);
            }

            Console.WriteLine("Done!");
        }
        finally
        {
            if (ShouldWaitForKeyPress())
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }
    }

    private static bool CheckFlag(List<string> args, string flagName)
    {
        return args.RemoveAll(arg =>
            arg.Equals($"/{flagName}", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals($"-{flagName}", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals($"--{flagName}", StringComparison.OrdinalIgnoreCase)) > 0;
    }

    private static ReadOnlyCollection<string> GetLocalW3XFiles()
    {
        return Directory.GetFiles(Directory.GetCurrentDirectory())
            .Where(f =>
                f.EndsWith(W3XExtension, StringComparison.OrdinalIgnoreCase) &&
                !f.EndsWith(FixedW3XSuffix, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    private static bool ShouldWaitForKeyPress()
    {
        // Returns True when opening the app from the Explorer, False when running the app from Terminal
        return Environment.UserInteractive && !string.IsNullOrEmpty(Process.GetCurrentProcess().MainWindowTitle);
    }

    private static void PrintUsage()
    {
        var executableName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

        var usageText =
            $"""
            Usage:
              {executableName} [options] [files]

            Arguments:
              files                Specify one or more map files to process.
                                   If no files are specified, MapMender will process maps in its directory.

            Options:
              /{R2SPatchSwitch}        Disables R2S/R2SW patch
              /{UnitSkinPatchSwitch}   Disables UnitSkin patch

            Notes:
              - At least one patch type should be enabled (by default, all patches are enabled).
              - It is forbidden to use both `/no-r2s-patch` and `/no-unitskin-patch` options simultaneously.
            """;

        Console.WriteLine(usageText);
    }
}