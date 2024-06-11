using System.IO;
using System.Reflection;
using System.Xml;

namespace CreateBlog
{
    internal static class Settings
    {
        static Settings()
        {
            var settingsDoc = new XmlDocument();
            settingsDoc.Load("settings.xml");

            RootFolder = GetSetting<string>(settingsDoc, "//html/RootFolder");
            IndentChars = GetSetting<string>(settingsDoc, "//html/IndentChars");
        }

        private static T? GetSetting<T>(XmlDocument settingsDoc, string xPath) where T : class
        {
            var value = settingsDoc.SelectSingleNode(xPath)?.Attributes!["value"]?.Value;
            if (!string.IsNullOrEmpty(value))
            {
                var ctor = typeof(T).GetConstructor([typeof(string)]);
                if (null != ctor)
                {
                    return (T) ctor.Invoke([value]);
                }
                if (typeof(T) == typeof(string))
                {
                    return value as T;
                }
            }

            return null;
        }

        public static string? RootFolder { get; set; }

        public static string? IndentChars { get; set; }
    }
}