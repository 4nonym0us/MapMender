using System.Text;
using MapMender.Logging;
using MapMender.Slk;
using War3Net.IO.Mpq;

namespace MapMender.Patcher;

public class MissingUnitSkinPatcher : MpqPatcherBase, IDisposable
{
    private readonly ILogger _logger = new ConsoleLogger();
    private const string UnitUiFileName = "UnitUI.slk";
    private const string UnitSkinFileName = @"Units\UnitSkin.txt";
    private Stream? _unitSkinDataStream;
    private MpqFile? _unitSkinMpqFile;

    public override bool TryPatch(MpqArchive archive, MpqArchiveBuilder newArchiveBuilder)
    {
        var mpqFiles = archive.GetMpqFiles().ToList();

        var unitSkin = mpqFiles.OfType<MpqKnownFile>()
            .FirstOrDefault(f => f.FileName.Equals(UnitSkinFileName, StringComparison.OrdinalIgnoreCase));

        if (unitSkin != null)
        {
            _logger.LogWarning($"{UnitSkinFileName} is already present and will not be overwritten. If you wish to generate it from scratch, delete it first.");
            return false;
        }

        var unitUi = mpqFiles.OfType<MpqKnownFile>()
            .FirstOrDefault(f => f.FileName.EndsWith(UnitUiFileName, StringComparison.OrdinalIgnoreCase));

        if (unitUi == null)
        {
            _logger.LogError(
                $"Failed to locate `{UnitUiFileName}`. " +
                $"The UnitSkin fix cannot be applied to a protected map that has no associated listfile. " +
                $"Read more at https://forum.wc3edit.net/viewtopic.php?t=34876");

            return false;
        }

        // Example usage
        var units = SlkReader.Read(unitUi.MpqStream);

        var stringBuilder = new StringBuilder(
            """
            [UIID]
            skinType=unit
            file=
            unitSound=
            armor=

            """);

        foreach (var unit in units)
        {
            var skinType = unit.Value.Properties.GetValueOrDefault("skinType", "unit");
            var file = unit.Value.Properties.GetValueOrDefault("file", "");
            var unitSound = unit.Value.Properties.GetValueOrDefault("unitSound", "");
            var armor = unit.Value.Properties.GetValueOrDefault("armor", "");

            stringBuilder.AppendLine($"[{unit.Key}]");
            stringBuilder.AppendLine($"skinType={skinType}");
            stringBuilder.AppendLine($"file={file}");
            if (unitSound != string.Empty)
            {
                stringBuilder.AppendLine($"unitSound={unitSound}");
            }
            stringBuilder.AppendLine($"armor={armor}");
        }

        _unitSkinDataStream = new MemoryStream(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        _unitSkinMpqFile = MpqFile.New(_unitSkinDataStream, UnitSkinFileName);
        _unitSkinMpqFile.CompressionType = MpqCompressionType.ZLib;
        _unitSkinMpqFile.TargetFlags = MpqFileFlags.CompressedMulti | MpqFileFlags.Encrypted | MpqFileFlags.BlockOffsetAdjustedKey | MpqFileFlags.Exists;

        newArchiveBuilder.AddFile(_unitSkinMpqFile, unitUi.TargetFlags);

        _logger.LogInformation("UnitSkin patch was successfully applied.");
        return true;
    }

    public void Dispose()
    {
        _unitSkinDataStream?.Dispose();
        _unitSkinMpqFile?.Dispose();
    }
}