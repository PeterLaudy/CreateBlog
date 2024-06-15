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
            Settings.InitSettings();
            FoldersToCopy.CopyAllStaticFolders();
            CheckImages.CheckAllImages();
            BlogContent.CreateBlogContent();
        }
    }
}