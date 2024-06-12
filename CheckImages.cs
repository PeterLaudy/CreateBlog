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
            var images = new DirectoryInfo(Path.Combine(Settings.RootFolder!, "images"));
            CheckAllImages(images);
        }

        /// <summary>
        /// Check all image files recursively.
        /// </summary>
        private static void CheckAllImages(DirectoryInfo dir)
        {
            dir.GetDirectories().ToList().ForEach(subDir => CheckAllImages(subDir));
            dir.GetFiles().ToList().ForEach(image => CheckImageIsLowerCase(image));
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