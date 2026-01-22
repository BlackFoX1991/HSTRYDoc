// ByteFormat.cs
using System;

namespace HSTRYDoc
{
    public static class ByteFormat
    {
        public static string ToHumanSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return unit == 0 ? $"{bytes} {units[unit]}" : $"{size:0.##} {units[unit]}";
        }
    }
}
