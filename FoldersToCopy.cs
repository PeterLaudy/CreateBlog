using System.IO;
using System.Linq;

namespace CreateBlog
{
    internal static class FoldersToCopy
    {
        internal static void CopyAllStaticFolders()
        {
            Utilities.LogMessage("Copying all static folders");
            Settings.FoldersToCopy!.ForEach(folder =>
            {
                var srcDir = new DirectoryInfo(Path.Combine(Settings.SourceRootFolder!, folder));
                var dstDir = new DirectoryInfo(Path.Combine(Settings.HtmlRootFolder!, folder));
                DeepCopyFolder(srcDir, dstDir);
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
                Utilities.LogMessage($"Copying {file.Name}");
                file.CopyTo(Path.Combine(dstDir.FullName, file.Name));
            });

            Utilities.LogMessage(string.Empty);

            srcDir.GetDirectories().ToList().ForEach(srcSubDir =>
            {
                var dstSubDir = new DirectoryInfo(Path.Combine(dstDir.FullName, srcSubDir.Name));
                DeepCopyFolder(srcSubDir, dstSubDir);
            });
        }
    }
}