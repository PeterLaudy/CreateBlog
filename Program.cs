using System;
using System.IO;
using System.Linq;

namespace CreateBlog
{
    class Program
    {
        public static void Main(string[] args)
        {
            Settings.IndentChars = "  ";
            Settings.RootFolder = new(args[0]);
            CheckImages.CheckAllImages();
            BlogContent.CreateBlogContent();
        }
    }
}