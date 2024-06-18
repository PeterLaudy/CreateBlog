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
            // Delete the destination folder.
            ClearDestinationFolder();

            // Construct the class which creates the content of the blog.
            var blogContent = new BlogContent();

            // Construct the class which copies the static files.
            var foldersToCopy = new FoldersToCopy();
            
            // Copy all static folder (like css, scipts etc.).
            foldersToCopy.CopyAllStaticFolders(new() { blogContent });

            // Check the filenames for all images. Also copy the folder structure.
            new CheckImages().CheckAllImages();

            // Create the blog-content. This also includes copying the images and icons to the destination folder.
            blogContent.CreateBlogContent(foldersToCopy);
        }

        /// <summary>
        /// Delete the destination folder and then recreate it, so it is empty. 
        /// </summary>
        private static void ClearDestinationFolder()
        {
            var root = new DirectoryInfo(Settings.Single.HtmlRootFolder!);

            Utilities.LogMessage($"Removing {root.FullName}");
            root.Delete(true);

            Utilities.LogMessage($"Recreating {root.FullName}");
            root.Create();

            Utilities.LogMessage(string.Empty);
        }
    }
}