using System.Collections.Generic;

namespace CreateBlog
{
    internal interface IContentCreator
    {
        Dictionary<string, string> GetFilesToRename { get; }
    }
}