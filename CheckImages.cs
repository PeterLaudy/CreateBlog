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
            Utilities.LogMessage("Checking all image filenames and copying folder structure");

            var sourceImages = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, "images"));
            var htmlImages = new DirectoryInfo(Path.Combine(Settings.HtmlRootFolder!, "images"));
            CheckAllImages(sourceImages, htmlImages);
        }

        /// <summary>
        /// Check all image files recursively and copy the folder structure.
        /// </summary>
        private static void CheckAllImages(DirectoryInfo source, DirectoryInfo html)
        {
            Utilities.LogMessage($"Checking the images in {source.FullName}");

            // Create the destination folder if it does not exist. 
            if (!html.Exists)
            {
                Utilities.LogMessage($"Creating directory {html.FullName}");
                html.Create();
            }

            source.GetFiles().ToList().ForEach(image => CheckImageIsLowerCase(image));
            Utilities.LogMessage(string.Empty);

            source.GetDirectories().ToList().ForEach(subDir =>
            {
                CheckAllImages(subDir, new DirectoryInfo(Path.Combine(html.FullName, subDir.Name)));
            });
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
                Utilities.LogMessage($"Changing image name {image.Name} to lowercase");
                image.MoveTo(image.FullName.ToLower());
            }
            else
            {
                Utilities.LogMessage($"Image name {image.Name} is lowercase");
            }
        }
    }
}