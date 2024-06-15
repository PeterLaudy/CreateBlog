using System.IO;
using System.Linq;

namespace CreateBlog
{
    /// <summary>
    /// Class to check all image files.
    /// </summary>
    internal static class CheckImages
    {
        /// <summary>
        /// Check all image files.
        /// </summary>
        public static void CheckAllImages()
        {
            var sourceImages = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, "images"));
            var htmlImages = new DirectoryInfo(Path.Combine(Settings.HtmlRootFolder!, "images"));
            CheckAllImages(sourceImages, htmlImages);
        }

        /// <summary>
        /// Check all image files recursively and copy the folder structure.
        /// </summary>
        private static void CheckAllImages(DirectoryInfo source, DirectoryInfo html)
        {
            // Create the destination folder if it does not exist. 
            if (!html.Exists)
            {
                html.Create();
            }

            source.GetDirectories().ToList().ForEach(subDir =>
            {
                CheckAllImages(subDir, new DirectoryInfo(Path.Combine(html.FullName, subDir.Name)));
            });
            source.GetFiles().ToList().ForEach(image => CheckImageIsLowerCase(image));
        }

        /// <summary>
        /// Check all image file names for being lowercase.
        /// </summary>
        /// <remarks>
        /// Linux servers treat file names case sensitive.
        /// </remarks>
        private static void CheckImageIsLowerCase(FileInfo image)
        {
            if (image.Name != image.Name.ToLower())
            {
                image.MoveTo(image.FullName.ToLower());
            }
        }
    }
}