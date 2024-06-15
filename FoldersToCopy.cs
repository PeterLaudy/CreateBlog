using System.IO;
using System.Linq;

namespace CreateBlog
{
    internal static class FoldersToCopy
    {
        internal static void CopyAllStaticFolders()
        {
            Settings.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, folder));
                var dstDir = new DirectoryInfo(Path.Combine(Settings.HtmlRootFolder!, folder));
                DeepCopyFolder(srcDir, dstDir);
            });
        }

        private static void DeepCopyFolder(DirectoryInfo srcDir, DirectoryInfo dstDir)
        {
            if (!dstDir.Exists)
            {
                dstDir.Create();
            }

            srcDir.GetFiles().ToList().ForEach(file =>
            {
                file.CopyTo(Path.Combine(dstDir.FullName, file.Name));
            });
            srcDir.GetDirectories().ToList().ForEach(srcSubDir =>
            {
                var dstSubDir = new DirectoryInfo(Path.Combine(dstDir.FullName, srcSubDir.Name));
                DeepCopyFolder(srcSubDir, dstSubDir);
            });
        }
    }
}