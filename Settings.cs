using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
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
        public static void InitSettings()
        {
            var settingsDoc = new XmlDocument();
            settingsDoc.Load("settings.xml");

            SourceRootFolder = GetSetting<string>(settingsDoc, "//source/RootFolder");
            if (!SourceRootFolder!.EndsWith("\\"))
                SourceRootFolder += "\\";

            ImageFolder = GetSetting<string>(settingsDoc, "//source/ImageFolder");
            if (!ImageFolder!.EndsWith("\\"))
                ImageFolder += "\\";

            FoldersToCopy = GetListOfSetting<string>(settingsDoc, "//source/FoldersToCopy");

            HtmlRootFolder = GetSetting<string>(settingsDoc, "//html/RootFolder");
            if (!HtmlRootFolder!.EndsWith("\\"))
                HtmlRootFolder += "\\";

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
        /// Read a single setting from the settings.xml file.
        /// </summary>
        /// <param name="settingsDoc">The xml document containing the settings.</param>
        /// <param name="xPath">The XPATH expression pointing to the required setting.</param>
        private static List<T> GetListOfSetting<T>(XmlDocument settingsDoc, string xPath) where T : class
        {
            var result = new List<T>();
            var node = settingsDoc.SelectSingleNode(xPath);
            if (null != node)
            {
                var value = node?.Attributes!["value"]?.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    var count = int.Parse(value!);
                    for (int i = 1; i <= count; i++)
                    {
                        value = node!.SelectSingleNode($"Item{i}")?.Attributes!["value"]?.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            var ctor = typeof(T).GetConstructor([typeof(string)]);
                            if (null != ctor)
                            {
                                result.Add((T) ctor.Invoke([value]));
                            }
                            else
                            {
                                // There is no constructor available which takes a single string as parameter.
                                // The only thing we can do is to try to cast the string to T.
                                // If that fails, we have a runtime exception.
                                var t = value as T;
                                if (null != t)
                                {
                                    result.Add(t!);
                                }
                                else
                                {
                                    throw new InvalidCastException($"Could not cast value to {typeof(T).Name}");
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The rootfolder on disk where the blog is read from.
        /// </summary>
       public static string? SourceRootFolder { get; private set; }

        /// <summary>
        /// The name of the subfolder containing the images.
        /// </summary>
        public static string? ImageFolder { get; private set; }

        /// <summary>
        /// The folders which content is copied as is.
        /// </summary>
        public static List<string>? FoldersToCopy { get; private set; }

        /// <summary>
        /// The rootfolder on disk where the blog is written to.
        /// </summary>
        public static string? HtmlRootFolder { get; private set; }

        /// <summary>
        /// The indentation used for the generated html files.
        /// </summary>
        public static string? IndentChars { get; private set; }
    }
}