using System;
using System.IO;
using System.Linq;

namespace CreateBlog
{
    class Program
    {
        public static void Main(string[] args)
        {
            CheckImages.CheckAllImages();
            BlogContent.CreateBlogContent();
        }
    }
}