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
    /// To prevent caching issues, certain files (like css and js) will have their name randomized.
    /// Any references to these files from html files are updated accordingly.
    /// </remarks>
    internal class FoldersToCopy
    {
        private delegate void FileCopier(FileInfo file, DirectoryInfo dstDir);

        /// <summary>
        /// Dictionary containing methods to copy specific file types.
        /// </summary>
        /// <remarks>
        /// Copying the files will also replace references to files which filename
        /// has been randomized, so there is no cashing issue after an update.
        /// For now we only handle html and javascript files.
        /// </remarks>
        private Dictionary<string, FileCopier> FileCopiers { get; }

        /// <summary>
        /// The filetypes in which the references to files which name have been randomized
        // need to be updated. 
        /// </summary>
        /// <remarks>
        /// For now we only handle html and javascript files.
        /// </remarks>
        private List<string> ExtensionToCheckForRandomizedLinks { get; } = new() { ".html", ".js" };

        /// <summary>
        /// The filetypes for which the name needs to be randomized to avoid cashing issues. 
        /// </summary>
        /// <remarks>
        /// For now we only handle css and javascript files.
        /// </remarks>
        private List<string> ExtensionToRandomize { get; } = new() { ".css", ".js" };

        /// <summary>
        /// The files for which the name is randomized to avoid cashing issues. 
        /// </summary>
        private Dictionary<string, string> RandomizedFiles { get; } = new();

        public FoldersToCopy()
        {
            this.FileCopiers = new() { { ".html", this.CopyHtml }, { ".js", this.CopyJs } };
        }

        /// <summary>
        /// Copy all folders with static files as indicated in the settings.xml file.
        /// </summary>
        public void CopyAllStaticFolders(List<IContentCreator> contentCreators)
        {
            RandomizedFiles.Clear();
            contentCreators.ForEach(cc =>
            {
                cc.GetFilesToRename.Keys.ToList().ForEach(key => {
                    RandomizedFiles.Add(key, cc.GetFilesToRename[key]);
                });
            });

            Utilities.LogMessage("Getting static files to be randomized");
            Settings.Single.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.Single.SourceRootFolder!, folder));
                FindAllFilesToRandomize(srcDir);
            });

            Utilities.LogMessage("Copying all static folders");
            Settings.Single.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.Single.SourceRootFolder!, folder));
                var dstDir = new DirectoryInfo(Path.Combine(Settings.Single.HtmlRootFolder!, folder.ToLower()));
                DeepCopyFolder(srcDir, dstDir);
            });
        }

        /// <summary>
        /// Check for all files in the given folder and its subfolders if their name has to be randomized
        /// to avoid any caching issues.
        /// <summary>
        /// <param name="folder"/>The folder to recursively check for filenames to randomize.</param>
        private void FindAllFilesToRandomize(DirectoryInfo folder)
        {
            folder.GetFiles().ToList().ForEach(file =>
            {
                if (ExtensionToRandomize.Contains(file.Extension.ToLower()))
                {
                    RandomizedFiles.Add(file.Name.ToLower(), $"{file.Name.Substring(0, file.Name.IndexOf('.'))}-{Guid.NewGuid()}{file.Extension}".ToLower());
                    Utilities.LogMessage($"File to randomize: {RandomizedFiles.Last().Key} => {RandomizedFiles.Last().Value}");
                }
            });

            folder.GetDirectories().ToList().ForEach(subFolder =>
            {
                FindAllFilesToRandomize(subFolder);
            });
        }

        /// <summary>
        /// Copy all files in the given folder (and subfolders) and randomize some filenames.
        /// Also update the references to these randomized filenames.
        /// </summary>
        /// <param name="srcDir">The folder for which all files recursively will be copied.</param>
        /// <param name="dstDir">The folder to which all files recursively will be copied.</param>
        private void DeepCopyFolder(DirectoryInfo srcDir, DirectoryInfo dstDir)
        {
            Utilities.LogMessage($"Copying {srcDir.FullName} => {dstDir.FullName}");
            if (!dstDir.Exists)
            {
                Utilities.LogMessage($"Creating directory {dstDir.FullName}");
                dstDir.Create();
            }

            srcDir.GetFiles().ToList().ForEach(file =>
            {
                CopyFile(file, dstDir);
            });

            Utilities.LogMessage(string.Empty);

            srcDir.GetDirectories().ToList().ForEach(srcSubDir =>
            {
                var dstSubDir = new DirectoryInfo(Path.Combine(dstDir.FullName, srcSubDir.Name));
                DeepCopyFolder(srcSubDir, dstSubDir);
            });
        }

        /// <summary>
        /// Copy one file and randomize its filename if required.
        /// Also update the references to any randomized filenames in that file.
        /// </summary>
        /// <param name="file">The file to copy</param>
        /// <param name="dstDir">The destination directory to which the file must be copied.</param>
        private void CopyFile(FileInfo file, DirectoryInfo dstDir)
        {
            Utilities.LogMessage($"Copying {file.Name.ToLower()}");
            if (ExtensionToCheckForRandomizedLinks.Contains(file.Extension.ToLower()))
            {
                FileCopiers[file.Extension.ToLower()]!.Invoke(file, dstDir);
            }
            else
            {
                file.CopyTo(Path.Combine(dstDir.FullName, GetRandomizedFileName(file.Name.ToLower())));
            }
        }

        /// <summary>
        /// Copy one html file.
        /// Also update the references to any randomized filenames in that file.
        /// </summary>
        /// <remarks>
        /// This method is stored in the <see cref="FoldersToCopy.FileCopiers"/> dictionary.
        /// </remarks>
        /// <param name="file">The file to copy</param>
        /// <param name="dstDir">The destination directory to which the file must be copied.</param>
        private void CopyHtml(FileInfo file, DirectoryInfo dstDir)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file.FullName);

            RandomizeLinksInHtmlFile(xmlDoc);

            Utilities.SaveHtmlPage(xmlDoc, Path.Combine(dstDir.FullName, file.Name.ToLower()));
        }

        /// <summary>
        /// Change the references to any file who's name has been randomized in a HTML file. 
        /// </summary>
        /// <param name="xmlDoc">The content of the html file.</param>,
        public void RandomizeLinksInHtmlFile(XmlDocument xmlDoc)
        {
            Utilities.LogMessage("Updating randomized links in Html");

            foreach (XmlNode node in xmlDoc.DocumentElement!.SelectNodes("/html/head/script | /html/head/link")!)
            {
                RandomizedFiles.Keys.ToList().ForEach(file =>
                {
                    node!.InnerXml = node!.InnerXml.Replace(file, RandomizedFiles[file]);
                    foreach (XmlAttribute attribute in node!.Attributes!)
                    {
                        attribute.Value = attribute.Value.Replace(file, RandomizedFiles[file]);
                    }
                });
            }
        }

        /// <summary>
        /// Copy one javascript file and update its name, as javascript filenames will be randomized.
        /// Also update the references to any randomized filenames in that file.
        /// </summary>
        /// <remarks>
        /// This method is stored in the <see cref="FoldersToCopy.FileCopiers"/> dictionary.
        /// </remarks>
        /// <param name="file">The file to copy</param>
        /// <param name="dstDir">The destination directory to which the file must be copied.</param>
        private void CopyJs(FileInfo file, DirectoryInfo dstDir)
        {
            var js = File.ReadAllText(file.FullName);

            RandomizeLinksInJsFile(ref js);

            File.WriteAllText(Path.Combine(dstDir.FullName, GetRandomizedFileName(file.Name.ToLower())), js);
        }

        /// <summary>
        /// Change the references to any file who's name has been randomized in a javascript file. 
        /// </summary>
        /// <param name="js">The content of the javascript file.</param>
        public void RandomizeLinksInJsFile(ref string js)
        {
            Utilities.LogMessage("Updating randomized links in JavaScript");

            foreach (var key in RandomizedFiles.Keys)
            {
                js = js.Replace(key, RandomizedFiles[key]);
            };
        }

        /// <summary>
        /// Get the randomized name of the given file.
        /// If it has not been randomized, it is only converted to lower case.
        /// </summary>
        /// <param name="fileName">The original filename to randomize if required.</param>
        private string GetRandomizedFileName(string fileName)
        {
            if (RandomizedFiles.ContainsKey(fileName.ToLower()))
            {
                return RandomizedFiles[fileName.ToLower()];
            }

            return fileName.ToLower();
        }
    }
}