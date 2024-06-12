using System.Xml;

namespace CreateBlog
{
    /// <summary>
    /// Class used to store the settings for the application.
    /// </summary>
    /// <remarks>
    /// The settings are read from the settings.xml file.
    /// </remarks>
    internal static class Settings
    {
        /// <summary>
        /// Initializes the static members for this class.
        /// </summary>
        /// <remarks>
        /// The settings are read from the settings.xml file.
        /// </remarks>
        static Settings()
        {
            var settingsDoc = new XmlDocument();
            settingsDoc.Load("settings.xml");

            RootFolder = GetSetting<string>(settingsDoc, "//html/RootFolder");
            IndentChars = GetSetting<string>(settingsDoc, "//html/IndentChars");
        }

        /// <summary>
        /// Read a single setting from the settings.xml file.
        /// </summary>
        /// <param name="settingsDoc">The xml document containing the settings.</param>
        /// <param name="xPath">The XPATH expression pointing to the required setting.</param>
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

                // There is no constructor available which takes a single string as parameter.
                // The only thing we can do is to try to cast the string to T.
                // If that fails, we have a runtime exception.
                return value as T;
            }

            return null;
        }

        /// <summary>
        /// The rootfolder on disk for the blog to convert.
        /// </summary>
       public static string? RootFolder { get; set; }

        /// <summary>
        /// The indentation used for the generated html files.
        /// </summary>
        public static string? IndentChars { get; set; }
    }
}