using System.Runtime.InteropServices;

namespace AniSort.Core.Utils
{
    public static class PlatformUtils
    {
        // TODO: Check if the linux path limit really is 255
        public static int MaxPathLength => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 255 : 255;
    }
}
