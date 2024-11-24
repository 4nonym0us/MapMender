using War3Net.IO.Mpq;

namespace MapMender.Patcher;

/// <summary>
/// Represents a class, which is responsible for analyzing and patching the MPQ archive.
/// </summary>
public interface IMpqPatcher
{
    /// <summary>
    /// Analyzes the map and applies a patch if necessary.
    /// </summary>
    /// <param name="archive">MPQ acrhive, which contains original map</param>
    /// <param name="newArchiveBuilder">A builder for an MPQ archive with a fixed map</param>
    /// <returns>True when an issue was detected and the map was patched, false otherwise</returns>
    public bool TryPatch(MpqArchive archive, MpqArchiveBuilder newArchiveBuilder);
}