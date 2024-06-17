using System.IO;

namespace CreateBlog
{
    /// <summary>
    /// Main entry class of the application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry mathod of the application.
        /// </summary>
        /// <param name="args">Command line arguments (currently not used).</param>
        public static void Main(string[] args)
        {
            // Read the settings.
            Settings.InitSettings();

            // Delete the destination folder.
            ClearDestinationFolder();

            // Copy all static folder (like css, scipts etc.).
            FoldersToCopy.CopyAllStaticFolders();

            // Check the filenames for all images. Also copy the folder structure.
            CheckImages.CheckAllImages();

            // Create the blog-content. This also includes copying the images and icons to the destination folder.
            BlogContent.CreateBlogContent();
        }

        /// <summary>
        /// Delete the destination folder and then recreate it, so it is empty. 
        /// </summary>
        private static void ClearDestinationFolder()
        {
            var root = new DirectoryInfo(Settings.HtmlRootFolder!);

            Utilities.LogMessage($"Removing {Settings.HtmlRootFolder!}");
            root.Delete(true);

            Utilities.LogMessage($"Recreating {Settings.HtmlRootFolder!}");
            root.Create();

            Utilities.LogMessage(string.Empty);
        }
    }
}