using MapMender.Logging;
using MapMender.Patcher;
using War3Net.IO.Mpq;

namespace MapMender;

/// <summary>
/// Responsible for reading the original map, applying patches and saving the fixed map in the same folder with `.Rfixed` suffix.
/// </summary>
public class MapProcessor
{
    private readonly IList<IMpqPatcher> _patchers;
    private readonly ILogger _logger = new ConsoleLogger();

    public MapProcessor(bool patchRealToString = true, bool patchUnitSkin = true)
    {
        if (!patchRealToString && !patchUnitSkin)
        {
            throw new ArgumentException("Unable to create a MapProcessor that doesn't apply any patches");
        }

        _patchers = new List<IMpqPatcher>();

        if (patchRealToString)
        {
            _patchers.Add(new RealToStringJassPatcher());
        }

        if (patchUnitSkin)
        {
            _patchers.Add(new MissingUnitSkinPatcher());
        }
    }

    /// <summary>
    /// Processes the map. If any patches are applied, fixed map is saved in the same folder with `.Rfixed` suffix.
    /// </summary>
    /// <param name="filePath"></param>
    public void Process(string filePath)
    {
        try
        {
            ProcessFileInternal(filePath);
        }
        catch (IOException e)
        {
            _logger.LogError($"Unable to open a file. Please, ensure that file exists and read access is not blocked by other applications. Details: {e}");
        }
        catch (MpqParserException e)
        {
            _logger.LogError($"Unable to parse the MPQ archive. Please, ensure that the file is a valid MPQ archive. Details: {e}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected exception occurred. Please, check details for more info. Details: {e}");
        }
    }

    private void ProcessFileInternal(string filePath)
    {
        _logger.LogInformation($"Processing map '{filePath}'...");

        if (!File.Exists(filePath))
        {
            _logger.LogError($"Map doesn't exist at '{filePath}'.");
            return;
        }

        using var sourceMap = MpqArchive.Open(filePath, true);

        if (IsProtected(sourceMap))
        {
            _logger.LogWarning(
                "The map is protected. It's impossible to edit and repack MPQ archive that has no listfile (which means it cannot be unpacked). " +
                "If you want to patch protected map, you need to manually reprotect and repack it first with the valid listfile. " +
                "Read more at https://forum.wc3edit.net/viewtopic.php?t=34876");
            return;
        }

        var fixedMapBuilder = new MpqArchiveBuilder(sourceMap);

        if (!ApplyPatches(sourceMap, fixedMapBuilder))
        {
            _logger.LogWarning($"No patches (0/{_patchers.Count}) were applied. Either the map has already been patched or patches could not be applied. Skipping the map...\r\n");
        }
        else
        {
            SaveFixedMap(filePath, fixedMapBuilder);
        }


        DisposePatchers();
    }

    private bool ApplyPatches(MpqArchive archive, MpqArchiveBuilder fixedMapBuilder)
    {
        var patched = false;

        for (var i = 0; i < _patchers.Count; i++)
        {
            var patcher = _patchers[i];
            var patcherName = patcher.GetType().Name;
            _logger.LogInformation($"Executing patcher {i + 1}/{_patchers.Count}: {patcherName}...");

            try
            {
                if (patcher.TryPatch(archive, fixedMapBuilder))
                {
                    patched = true;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    $"An unexcepted error has ocurred when applying patch {patcherName}. Details: {e}.");
            }
        }

        return patched;
    }

    private void SaveFixedMap(string filePath, MpqArchiveBuilder fixedMapBuilder, string suffix = "Rfixed")
    {
        var options = new MpqArchiveCreateOptions
        {
            AttributesCreateMode = MpqFileCreateMode.Overwrite,
            ListFileCreateMode = MpqFileCreateMode.Overwrite,
            SignatureCreateMode = MpqFileCreateMode.None
        };

        var extension = Path.GetExtension(filePath);
        var targetDir = Path.Combine(Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory(), "Fixed");
        var targetFileName = Path.ChangeExtension(Path.GetFileName(filePath), $"{suffix}{extension}");
        var targetPath = Path.Combine(targetDir, targetFileName);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        _logger.LogInformation("Saving the map...");

        fixedMapBuilder.SaveTo(targetPath, options);

        _logger.LogInformation($"Map was successfully saved to '{targetPath}'.\r\n");
    }

    private void DisposePatchers()
    {
        foreach (var patcher in _patchers)
        {
            (patcher as IDisposable)?.Dispose();
        }
    }

    private static bool IsProtected(MpqArchive archive)
    {
        return archive.GetMpqFiles()
            .All(f => f is MpqUnknownFile ||
                      (f is MpqKnownFile file && file.FileName.StartsWith("(") && file.FileName.EndsWith(")")));
    }
}