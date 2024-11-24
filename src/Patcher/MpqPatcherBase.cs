using System.Text;
using War3Net.IO.Mpq;

namespace MapMender.Patcher;

/// <inheritdoc cref="IMpqPatcher"/>
public abstract class MpqPatcherBase : IMpqPatcher
{
    /// <inheritdoc cref="IMpqPatcher"/>
    public abstract bool TryPatch(MpqArchive archive, MpqArchiveBuilder newArchiveBuilder);

    /// <summary>
    /// Reads content of <see cref="MpqFile"/> into a string.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    protected string ReadMpqFileContentAsString(MpqFile file)
    {
        using var reader = new StreamReader(file.MpqStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}