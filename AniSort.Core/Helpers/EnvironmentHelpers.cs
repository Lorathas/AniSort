using System;

namespace AniSort.Core.Helpers
{
    public static class EnvironmentHelpers
    {
        private static bool? isConsolePresent;

        public static bool IsConsolePresent
        {
            get
            {
                if (isConsolePresent == null)
                {
                    isConsolePresent = true;

                    try
                    {
                        int _ = Console.WindowHeight;
                    }
                    catch
                    {
                        isConsolePresent = false;
                    }
                }

                return isConsolePresent.Value;
            }
        }
    }
}