using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace CreateBlog
{
    /// <summary>
    /// Class containing utility methods for the application.
    /// </summary>
    internal static class Utilities
    {
         /// <summary>
        /// Find the relative path to the given image file.
        /// </summary>
        /// <param name="imgFileName">The name of the image to file to which a relative link is requested.</param>
        /// <param name="htmlFileName">The absolute path to the file in which the link will be included.</param>
        public static string FindImage(string imgFileName, string htmlFileName)
        {
            // Since we convert all filenames to lowercase, we also must make that same change here. 
            imgFileName = imgFileName.ToLower();
            var imgFullName = FindImage(imgFileName, new DirectoryInfo(Settings.SourceRootFolder!));
            if (null != imgFullName)
            {
                var img = Path.Combine(Settings.HtmlRootFolder!, imgFullName!.Substring(Settings.SourceRootFolder!.Length));
                if (!File.Exists(img!))
                {
                    LogMessage($"Copying image {imgFileName}");
                    File.Copy(imgFullName!, img!, true);
                }

                return Path.GetRelativePath(Path.GetDirectoryName(htmlFileName)!, img!).Replace('\\', '/');
            }

            throw new FileNotFoundException("Image could not be found.", imgFileName, null);
        }

        /// <summary>
        /// Recursively find the given image file.
        /// </summary>
        /// <param name="imgFileName">The name of the image to file to which a relative link is requested.</param>
        /// <param name="folder">The folder in which the file is searched recursively.</param>
        private static string? FindImage(string imgFileName, DirectoryInfo folder)
        {
            var result = folder.GetFiles().ToList().FirstOrDefault(file => file.Name == imgFileName)?.FullName;
            if (null == result)
            {
                folder.GetDirectories()!.ToList().FirstOrDefault(dir =>
                {
                    if (null == result)
                    {
                        result = FindImage(imgFileName, dir);
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

        /// <summary>
        /// Set the indentation of the given text (from a text node).
        /// </summary>
        /// <param name="s">The string for which the indentation must be set.</param>
        /// <param name="parent">The parent node of the text node.</param>
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

        /// <summary>
        /// Save the html content to file.
        /// </summary>
        /// <remarks>
        /// It will use an XDocument so we can ommit the xml header in the file.
        /// </remarks>
        /// <param name="htmlDoc">The html document to save.</param>
        /// <param name="fileName">The full path to the file to save.</param>
        public static void SaveHtmlPage(XmlDocument htmlDoc, string fileName)
        {
            LogMessage($"Saving HTML file {fileName}");

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                LogMessage($"Creating directory {Path.GetDirectoryName(fileName)!}");
                Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            }

            using var xmlReader = new XmlNodeReader(htmlDoc);
            var xDoc = XDocument.Load(xmlReader);
            xDoc.DocumentType!.InternalSubset = null;
            using var xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true, IndentChars = Settings.IndentChars! });
            xDoc.Save(xmlWriter);

            LogMessage(string.Empty);
        }

        /// <summary>
        /// Save the json content to file.
        /// </summary>
        /// <param name="data">The data to save to file in JSON format.</param>
        /// <param name="fileName">The full path to the JSON file.</param>
        public static void SaveJsonFile(object data, string fileName)
        {
            LogMessage($"Saving JSON file {fileName}");

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                LogMessage($"Creating directory {Path.GetDirectoryName(fileName)!}");
                Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            }

            var jsonData = JsonSerializer.Serialize(data);
            using var writer = new StreamWriter(fileName);
            writer.WriteLine(jsonData);
            writer.Close();

            LogMessage(string.Empty);
        }

        public static void LogMessage(string msg)
        {
            if (Settings.Verbose)
            {
                Console.Out.WriteLine(msg);
            }
        }
    }
}