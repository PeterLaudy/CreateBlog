using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace CreateBlog
{
    /// <summary>
    /// Class to copy folders containing static files.
    /// </summary>
    /// <remarks>
    /// To prevent caching issues, certain files (like css and js) will be renamed.
    /// Any references to these files from html files are updated.
    /// </remarks>
    internal static class FoldersToCopy
    {
        private static Dictionary<string, Action<DirectoryInfo, FileInfo>> FileCopiers = new()
        {
            { ".html", FoldersToCopy.CopyHtml }, { ".js", FoldersToCopy.CopyJs }
        };

        private static List<string> extensionToCheckForRenamedLinks { get; } = new() { ".html", ".js" };

        private static List<string> extensionToRename { get; } = new() { ".css", ".js" };

        private static Dictionary<string, string> renamedFiles { get; set; } = new();

        internal static void CopyAllStaticFolders()
        {
            renamedFiles.Clear();

            Utilities.LogMessage("Getting static files to rename");
            Settings.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, folder));
                FindAllFilesToRename(srcDir);
            });

            Utilities.LogMessage("Copying all static folders");
            Settings.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, folder));
                var dstDir = new DirectoryInfo(Path.Combine(Settings.HtmlRootFolder!, folder.ToLower()));
                DeepCopyFolder(srcDir, dstDir);
            });
        }

        private static void FindAllFilesToRename(DirectoryInfo folder)
        {
            folder.GetFiles().ToList().ForEach(file =>
            {
                if (extensionToRename.Contains(file.Extension.ToLower()))
                {
                    renamedFiles.Add(file.Name.ToLower(), $"{file.Name.Substring(0, file.Name.IndexOf('.'))}-{Guid.NewGuid()}{file.Extension}".ToLower());
                    Utilities.LogMessage($"File to rename: {renamedFiles.Last().Key} => {renamedFiles.Last().Value}");
                }
            });

            folder.GetDirectories().ToList().ForEach(subFolder =>
            {
                FindAllFilesToRename(subFolder);
            });
        }

        private static void DeepCopyFolder(DirectoryInfo srcDir, DirectoryInfo dstDir)
        {
            Utilities.LogMessage($"Copying {srcDir.FullName} => {dstDir.FullName}");
            if (!dstDir.Exists)
            {
                Utilities.LogMessage($"Creating directory {dstDir.FullName}");
                dstDir.Create();
            }

            srcDir.GetFiles().ToList().ForEach(file =>
            {
                CopyFile(dstDir, file);
            });

            Utilities.LogMessage(string.Empty);

            srcDir.GetDirectories().ToList().ForEach(srcSubDir =>
            {
                var dstSubDir = new DirectoryInfo(Path.Combine(dstDir.FullName, srcSubDir.Name));
                DeepCopyFolder(srcSubDir, dstSubDir);
            });
        }

        private static void CopyFile(DirectoryInfo dstDir, FileInfo file)
        {
            Utilities.LogMessage($"Copying {file.Name.ToLower()}");
            if (extensionToCheckForRenamedLinks.Contains(file.Extension.ToLower()))
            {
                FileCopiers[file.Extension.ToLower()]!.Invoke(dstDir, file);
            }
            else
            {
                file.CopyTo(Path.Combine(dstDir.FullName, GetRenamedFileName(file.Name.ToLower())));
            }
        }

        private static void CopyHtml(DirectoryInfo dstDir, FileInfo file)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file.FullName);

            RenameLinksInHtmlFile(xmlDoc);

            Utilities.SaveHtmlPage(xmlDoc, Path.Combine(dstDir.FullName, GetRenamedFileName(file.Name.ToLower())));
        }

        public static void RenameLinksInHtmlFile(XmlDocument xmlDoc)
        {
            Utilities.LogMessage("Renaming links in Html");

            foreach (XmlNode node in xmlDoc.DocumentElement!.SelectNodes("/html/head/script | /html/head/link")!)
            {
                renamedFiles.Keys.ToList().ForEach(file =>
                {
                    node!.InnerXml = node!.InnerXml.Replace(file, renamedFiles[file]);
                    foreach (XmlAttribute attribute in node!.Attributes!)
                    {
                        attribute.Value = attribute.Value.Replace(file, renamedFiles[file]);
                    }
                });
            }
        }

        private static void CopyJs(DirectoryInfo dstDir, FileInfo file)
        {
            var js = File.ReadAllText(file.FullName);

            RenameLinksInJsFile(ref js);

            File.WriteAllText(Path.Combine(dstDir.FullName, GetRenamedFileName(file.Name.ToLower())), js);
        }

        public static void RenameLinksInJsFile(ref string js)
        {
            Utilities.LogMessage("Renaming links in JavaScript");

            foreach (var key in renamedFiles.Keys)
            {
                js = js.Replace(key, renamedFiles[key]);
            };
        }

        private static string GetRenamedFileName(string fileName)
        {
            if (renamedFiles.ContainsKey(fileName.ToLower()))
            {
                return renamedFiles[fileName.ToLower()];
            }

            return fileName.ToLower();
        }
    }
}