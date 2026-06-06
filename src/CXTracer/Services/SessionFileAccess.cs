using System.IO;

namespace CXTracer.Services;

internal static class SessionFileAccess
{
    public const int BufferSize = 64 * 1024;

    public static FileStream OpenReadShared(string filePath)
    {
        return new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: BufferSize,
            options: FileOptions.SequentialScan);
    }
}
