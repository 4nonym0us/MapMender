using MapMender.Logging;
using System.Reflection;
using System.Text;
using War3Net.IO.Mpq;

namespace MapMender.Patcher;

/// <summary>
/// Injects a drop-in replacement of R2S and R2SW into the JASS script of the map and replaces all original usages of these functions.
/// </summary>
public class RealToStringJassPatcher : MpqPatcherBase, IDisposable
{
    private readonly ILogger _logger = new ConsoleLogger();
    private const string War3MapJassFileName = "war3map.j";
    private Stream? _jassScriptSource;
    private MpqFile? _jassScriptFile;

    public override bool TryPatch(MpqArchive archive, MpqArchiveBuilder newArchiveBuilder)
    {
        var jassScript = archive.GetMpqFiles()
            .OfType<MpqKnownFile>()
            .FirstOrDefault(f => f.FileName.EndsWith(War3MapJassFileName, StringComparison.OrdinalIgnoreCase));

        if (jassScript == null)
        {
            _logger.LogWarning($"Failed to locate `{War3MapJassFileName}`. The fix cannot be applied to a protected map that has no associated listfile. Read more at https://forum.wc3edit.net/viewtopic.php?t=34876");
            return false;
        }

        var injectedJass = MinifyJassScript(GetRealToStringReplacement());
        var content = ReadMpqFileContentAsString(jassScript);

        if (content.Contains(injectedJass))
        {
            _logger.LogInformation("RealToString conversion (R2S, R2SW) patch has already been applied to the map.");
            return false;
        }

        // Inject drop-in replacement for R2S/R2SW and switch to using it
        content = content.Replace("endglobals", $"endglobals\r\n{injectedJass}")
            .Replace("R2S(", "R2SF(")
            .Replace("R2SW(", "R2SWF(")
            .Trim() + "\r\n";

        _jassScriptSource = new MemoryStream(Encoding.UTF8.GetBytes(content));
        _jassScriptFile = MpqFile.New(_jassScriptSource, jassScript.FileName);
        _jassScriptFile.CompressionType = jassScript.CompressionType;
        _jassScriptFile.TargetFlags = jassScript.TargetFlags;

        newArchiveBuilder.AddFile(_jassScriptFile, jassScript.TargetFlags);

        _logger.LogInformation("RealToString conversion (R2S, R2SW) patch was successfully applied.");
        return true;
    }

    public static string MinifyJassScript(string input)
    {
        input = input.Trim().Trim('"');

        var processedLines = input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return string.Join("\r\n", processedLines);
    }

    private static string GetRealToStringReplacement()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MapMender.Patcher.Jass.real-to-string.j";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new InvalidDataException("real-to-string.j embedded resource was not found");
        }

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        _jassScriptSource?.Dispose();
        _jassScriptFile?.Dispose();
    }
}