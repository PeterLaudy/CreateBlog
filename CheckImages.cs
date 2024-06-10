using System.IO;
using System.Linq;

namespace CreateBlog
{
    internal static class CheckImages
    {
        public static void CheckAllImages()
        {
            var images = new DirectoryInfo(Path.Combine(Settings.RootFolder!.FullName, "images"));
            CheckAllImages(images);
        }

        private static void CheckAllImages(DirectoryInfo dir)
        {
            dir.GetDirectories().ToList().ForEach(subDir => CheckAllImages(subDir));
            dir.GetFiles().ToList().ForEach(image => CheckImageIsLowerCase(image));
        }

        private static void CheckImageIsLowerCase(FileInfo image)
        {
            if (image.Name != image.Name.ToLower())
            {
                image.MoveTo(image.FullName.ToLower());
            }
        }
    }
}