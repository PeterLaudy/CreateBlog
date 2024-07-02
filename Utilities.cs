using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using SkiaSharp;

namespace CreateBlog
{
    internal class ImageInfo
    {
        public ImageInfo(string fullFileName)
        {
            FileName = fullFileName;
            IsRelativeFileName = false;
            if ((null != Settings.Single.ImagesToCheckForMultipleUse) &&
                (Settings.Single.ImagesToCheckForMultipleUse!.Contains($";{Path.GetExtension(fullFileName)};")))
            {
                using var info = SKCodec.Create(FileName);
                Width = info.Info.Width;
                Height = info.Info.Height;
            }
        }

        public ImageInfo(ImageInfo absoluteImageInfo, string htmlFileName)
        {
            if (absoluteImageInfo.IsRelativeFileName)
            {
                throw new ArgumentException($"{nameof(ImageInfo)} needs to be absolute.");
            }

            var img = Path.Combine(Settings.Single.HtmlRootFolder!, absoluteImageInfo.FileName.Substring(Settings.Single.SourceRootFolder!.Length));

            FileName = Path.GetRelativePath(Path.GetDirectoryName(htmlFileName)!, img!).Replace('\\', '/');
            IsRelativeFileName = true;
            Width = absoluteImageInfo.Width;
            Height = absoluteImageInfo.Height;
        }

        public string FileName { get; }
        public bool IsRelativeFileName { get; }
        public int Width { get; }
        public int Height { get; }
    }

    /// <summary>
    /// Class containing utility methods for the application.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>List of images used so far.</summary>
        /// <remarks>Used for caching and to check for multiple use of images.</remarks>
        private static Dictionary<string, ImageInfo> UsedImages { get; } = new();

        /// <summary>
        /// Find the relative path to the given image file.
        /// </summary>
        /// <param name="imgFileName">The name of the image to file to which a relative link is requested.</param>
        /// <param name="htmlFileName">The absolute path to the file in which the link will be included.</param>
        public static ImageInfo FindImage(string imgFileName, string htmlFileName)
        {
            // Since we convert all filenames to lowercase, we also must make that same change here. 
            imgFileName = imgFileName.ToLower();

            // Check if we cached the location of the image.
            if (!UsedImages.TryGetValue(imgFileName, out var result))
            {
                var imgFullName = FindImage(imgFileName, new DirectoryInfo(Settings.Single.SourceRootFolder!));
                if (string.IsNullOrEmpty(imgFullName))
                {
                    throw new FileNotFoundException(imgFileName);
                }

                result = new(imgFullName);
                UsedImages[imgFileName] = result;
            }
            else
            {
                if (null != Settings.Single.ImagesToCheckForMultipleUse)
                {
                    if (Settings.Single.ImagesToCheckForMultipleUse!.Contains($";{Path.GetExtension(imgFileName)};"))
                    {
                        Utilities.LogMessage($"Image \"{imgFileName}\" is referenced multiple times.");
                    }
                }
            }

            if (null != result)
            {
                var img = Path.Combine(Settings.Single.HtmlRootFolder!, result.FileName.Substring(Settings.Single.SourceRootFolder!.Length));
                if (!File.Exists(img!))
                {
                    LogMessage($"Copying image {imgFileName} from {result.FileName} to {img!}");
                    File.Copy(result.FileName, img!, true);
                }

                return new(result, htmlFileName);
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
                indent += Settings.Single.IndentChars;
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
            using var xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings() {
                OmitXmlDeclaration = true, Indent = true, IndentChars = Settings.Single.IndentChars!
            });
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

        /// <summary>
        /// Log a message to the console, depending on the log settings.
        /// </summary>
        /// <param name="msg">The message to log to the console.</param>
        public static void LogMessage(string msg)
        {
            if (Settings.Single.Verbose)
            {
                Console.Out.WriteLine(msg);
            }
        }
    }
}