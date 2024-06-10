using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace CreateBlog
{
    internal static class Utilities
    {
        public static string FindImage(string imgFileName, string htmlFileName)
        {
            return Path.GetRelativePath(
                Path.GetDirectoryName(htmlFileName)!,
                FindImage(Settings.RootFolder!, imgFileName)!
            ).Replace('\\', '/');
        }

        private static string? FindImage(DirectoryInfo folder, string imgFileName)
        {
            var result = folder.GetFiles().ToList().FirstOrDefault(file => file.Name == imgFileName)?.FullName;
            if (null == result)
            {
                var dir = folder.GetDirectories()!.ToList().FirstOrDefault(dir =>
                {
                    if (null == result)
                    {
                        result = FindImage(dir, imgFileName);
                        return null != result;
                    }
                    else
                    {
                        return false;
                    }
                });
            }

            return result!;
        }

        public static string SetCorrectIndentForText(string s, XmlNode parent)
        {
            var indent = Environment.NewLine;
            while (parent.ParentNode!.Name.ToLower() != "html")
            {
                parent = parent.ParentNode;
                indent += Settings.IndentChars;
            }

            var lines = s.Split(Environment.NewLine).ToList();
            lines.ForEach(l => l = l.Trim());

            var result = string.Join(indent, lines);
            return result;
        }
    }
}