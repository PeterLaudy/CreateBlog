using System.IO;

namespace CreateBlog
{
    internal static class Settings
    {
        public static DirectoryInfo? RootFolder { get; set; }

        public static string? IndentChars { get; set; }
    }
}