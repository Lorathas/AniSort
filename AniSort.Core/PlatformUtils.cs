using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AniSort.Core
{
    public static class PlatformUtils
    {
        // TODO: Check if the linux path limit really is 255
        public static int MaxPathLength => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 255 : 255;
    }
}
