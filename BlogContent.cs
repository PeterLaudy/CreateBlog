using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace CreateBlog
{
    /// <summary>
    /// Class used to convert the blog content in xml format to html files.
    /// </summary>
    internal class BlogContent : IContentCreator
    {
        private FoldersToCopy? foldersToCopy;

        /// <summary>
        /// Class used to create the availablePages.json file.
        /// </summary>
        private class PageInfo
        {
            /// <summary>The link of the available page.</summary>
            public string? Link { get; set; }
            /// <summary>The relative link to the icon file of the available page.</summary>
            public string? Icon { get; set; }
            /// <summary>The title of the available page.</summary>
            public string? Title { get; set; }
        }

        private List<PageInfo> AvailablePages { get; } = new();

        private string AvailablePagesFileName { get; } = $"availablePages-{Guid.NewGuid()}.json";

        private Dictionary<string, ImageDivCss> Css { get; } = new();

        private string CssFileName { get; } = $"images-{Guid.NewGuid()}.css";

        public Dictionary<string, string> GetFilesToRename
        {
            get
            {
                return new() {
                    { "availablePages.json", AvailablePagesFileName },
                    { "images.css", CssFileName }
                };
            }
        }

        /// <summary>
        /// Find all page{n}.xml files and create html files from them.
        /// The files are searched for in the chapters, which are defined in the
        /// index.xml file. The links to the pages are included in the index.html file.
        /// </summary>
        /// <remarks>
        /// Note that the code will search for consecutive file numbers, starting at 1.
        /// If a file cannot be found, it will stop and continue with the next chapter.
        /// </remarks>
        public void CreateBlogContent(FoldersToCopy foldersToCopy)
        {
            this.foldersToCopy = foldersToCopy;

            Utilities.LogMessage($"Creating blog content for {Settings.Single.SourceRootFolder!}");

            var indexFileName = Path.Combine(Settings.Single.SourceRootFolder!, "index.xml");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(indexFileName);

            var htmlDoc = new XmlDocument();
            htmlDoc.Load(Path.Combine(Settings.Single.SourceRootFolder!, "index.html"));

            var indexDiv = htmlDoc.SelectSingleNode("//div[@id='content']")!;

            foreach (XmlNode node in xmlDoc.SelectSingleNode("/blog")!.ChildNodes)
            {
                switch (node.Name.ToLower())
                {
                    case "chapter":
                        CreateChapter(node, indexFileName, htmlDoc);
                        break;
                    case "empty-line":
                        {
                            Utilities.LogMessage("Adding empty line to home page");
                            Utilities.LogMessage(string.Empty);
                            var p = htmlDoc.CreateNodeWithAttributes("p", [], []);
                            p.InnerXml = "&#160;";
                            indexDiv.AppendChild(p);
                        }
                        break;
                    case "link":
                        {
                            var link = node.Attributes!["href"]!.Value;
                            var icon = node.Attributes!["icon"]!.Value;

                            Utilities.LogMessage($"Adding fixed link \"{node.InnerText}\" to home page");

                            var p = htmlDoc.CreateNodeWithAttributes("p", [], []);
                            var a = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["no_decoration", link]);
                            var img = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, indexFileName).FileName]);

                            indexDiv.AppendChild(p);
                            p.AppendChild(a);
                            a.AppendChild(img);
                            a.AppendChild(htmlDoc.CreateTextNode(node.InnerText));

                            Utilities.LogMessage(string.Empty);
                        }
                        break;
                }
            }

            // Save the index.html file of the blog.
            foldersToCopy!.RandomizeLinksInHtmlFile(htmlDoc);
            Utilities.SaveHtmlPage(htmlDoc, Path.Combine(Settings.Single.HtmlRootFolder!, "index.html"));

            // Save the JSON formatted list of available links.
            Utilities.SaveJsonFile(AvailablePages.ToArray(), Path.Combine(Settings.Single.HtmlRootFolder!, "script", AvailablePagesFileName));

            // Save the JSON formatted list of available links.
            Utilities.SaveCssFile(Css.Values.ToList(), Path.Combine(Settings.Single.HtmlRootFolder!, "css", CssFileName));
        }

        /// <summary>
        /// Create the link to the given chapter and create all pages for that chapter.
        /// </summary>
        /// <param name="chapter">The chapter to create the link and all pages for.</param>
        /// <param name="indexFileName">The filename in which the links are generated.</param>
        /// <param name="htmlDoc">The XML document in which the links are generated.</param>
        private void CreateChapter(XmlNode chapter, string indexFileName, XmlDocument htmlDoc)
        {
            var indexDiv = htmlDoc.SelectSingleNode("//div[@id='content']")!;

            var link = chapter.Attributes!["link"]!.Value;
            var icon = chapter.Attributes!["icon"]!.Value;
            var title = chapter.InnerText;

            Utilities.LogMessage($"Converting blog chapter {title}");

            // Create the link in the main blog page to the first page of the chapter.
            var p = htmlDoc.CreateNodeWithAttributes("p", [], []);
            var a = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["no_decoration", $"./{link}/page1.html"]);
            var img = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, indexFileName).FileName]);

            indexDiv.AppendChild(p);
            p.AppendChild(a);
            a.AppendChild(img);
            a.AppendChild(htmlDoc.CreateTextNode(title));

            // Create all pages for the found chapter.
            var index = 1;
            while (File.Exists(Path.Combine(Settings.Single.SourceRootFolder!, link, $"page{index}.xml")))
            {
                Utilities.LogMessage($"Converting page {index}");

                AvailablePages.Add(new PageInfo()
                {
                    Link = $"{link}/page{index}.html",
                    Icon = Utilities.FindImage(icon, Path.Combine(Settings.Single.HtmlRootFolder!, "new", "index.html")).FileName,
                    Title = title
                });
                CreateBlogPage(
                    link,
                    icon,
                    title,
                    index,
                    File.Exists(Path.Combine(Settings.Single.SourceRootFolder!, link, $"page{index - 1}.xml")),
                    File.Exists(Path.Combine(Settings.Single.SourceRootFolder!, link, $"page{index + 1}.xml"))
                );
                index++;
            }
        }

        /// <summary>
        /// Create a single blog page.
        /// </summary>
        /// <param name="link">The relative link to the folder containing the page.</param>
        /// <param name="icon">The name of the file contining the icon for the page.</param>
        /// <param name="title">The title of the page.</param>
        /// <param name="index">The index number of the page.</param>
        /// <param name="prev">True if there is a previous page.</param>
        /// <param name="next">True if there is a next page.</param>
        private void CreateBlogPage(string link, string icon, string title, int index, bool prev, bool next)
        {
            var fileName = Path.Combine(Settings.Single.SourceRootFolder!, link, $"page{index}.xml");

            var htmlDoc = new XmlDocument();
            htmlDoc.Load(Path.Combine(Settings.Single.SourceRootFolder!, "page.html"));

            if (prev || next)
            {
                title = $"{title} {index}";
            }

            htmlDoc.DocumentElement!.SelectSingleNode("/html/head/title")!.InnerText = title;

            var image = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, fileName).FileName]);

            var titleNode = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;
            titleNode.AppendChild(image);
            titleNode.AppendChild(htmlDoc.CreateTextNode(title));

            AddNavigationSection(htmlDoc, index, prev, next, fileName);

            ConvertBlogContent(htmlDoc, fileName);
            foldersToCopy!.RandomizeLinksInHtmlFile(htmlDoc);
            Utilities.SaveHtmlPage(htmlDoc, Path.Combine(Settings.Single.HtmlRootFolder!, link, $"page{index}.html"));
        }

        /// <summary>
        /// Add the section containg the navigation controls, like the home button and the
        /// previous and next arrows.
        /// </summary>
        /// <param name="htmlDoc">The html document to which the control should be added.</param>
        /// <param name="index">The index number of the page.</param>
        /// <param name="prev">True if there is a previous page.</param>
        /// <param name="next">True if there is a next page.</param>
        /// <param name="fileName">The full path to the file, used to create relative links to the icons.</param>
        private void AddNavigationSection(XmlDocument htmlDoc, int index, bool prev, bool next, string fileName)
        {
            var title = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;

            if (!prev && !next)
            {
                var newLink = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["float-right", "../index.html"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("minibus.svg", fileName).FileName]));
                title.AppendChild(newLink);
            }
            else
            {
                var navigation = htmlDoc.CreateNodeWithAttributes("p", ["class"], ["flex"]);
                title.ParentNode!.InsertAfter(navigation, title);

                var newLink = htmlDoc.CreateNodeWithAttributes("a", ["class"], ["left"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("empty.svg", fileName).FileName]));
                navigation.AppendChild(newLink);

                newLink = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["center", "../index.html"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("minibus.svg", fileName).FileName]));
                navigation.AppendChild(newLink);

                newLink = htmlDoc.CreateNodeWithAttributes("a", ["class"], ["right"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("empty.svg", fileName).FileName]));
                navigation.AppendChild(newLink);

                if (prev)
                {
                    var attribute = htmlDoc.CreateAttribute("href");
                    attribute.Value = $"./page{index - 1}.html";
                    navigation.ChildNodes[0]!.Attributes!.Append(attribute);
                    navigation.ChildNodes[0]!.ChildNodes[0]!.Attributes!["src"]!.Value = Utilities.FindImage("previous.svg", fileName).FileName;
                }

                if (next)
                {
                    var attribute = htmlDoc.CreateAttribute("href");
                    attribute.Value = $"./page{index + 1}.html";
                    navigation.ChildNodes[2]!.Attributes!.Append(attribute);
                    navigation.ChildNodes[2]!.ChildNodes[0]!.Attributes!["src"]!.Value = Utilities.FindImage("next.svg", fileName).FileName;
                }
            }
        }

        /// <summary>
        /// The actual conversion of the xml file containing the content of the blog to the html file used on the web-server.
        /// </summary>
        /// <param name="htmlDoc">The html document to which the blog content should be added.</param>
        /// <param name="fileName">The full path to the file, used to create relative links to the icons.</param>
        private void ConvertBlogContent(XmlDocument htmlDoc, string fileName)
        {
            var contentDiv = htmlDoc.DocumentElement!.SelectSingleNode("//div[@id='content']")!;

            var blogDoc = new XmlDocument();
            blogDoc.Load(fileName);
            var xmlNode = blogDoc.DocumentElement!.FirstChild;

            while (null != xmlNode)
            {
                var images = new List<XmlNode>();
                while ((null != xmlNode) && (xmlNode!.Name == "img"))
                {
                    images.Add(xmlNode!);
                    xmlNode = xmlNode.NextSibling;
                }

                if ((null != xmlNode) && (xmlNode!.Name == "txt"))
                {
                    var div = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["container"]);
                    contentDiv.AppendChild(div);

                    var p = htmlDoc.CreateNodeWithAttributes("p", ["class"], ["body"]);
                    if (!string.IsNullOrEmpty(xmlNode.InnerXml.Trim()))
                    {
                        div.AppendChild(p);
                        p.InnerXml = Utilities.SetCorrectIndentForText(xmlNode.InnerXml, p);
                    }

                    AddImagesToContent(images, htmlDoc, fileName, p);
                }
                else
                {
                    // We have reached the last node, but there are still images to add to the HTML page.
                    var div = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["container last"]);
                    contentDiv.AppendChild(div);
                    if (images.Count > 0)
                    {
                        var flexDiv = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["flex"]);
                        div.AppendChild(flexDiv);

                        AddImagesToContent(images, htmlDoc, fileName, div);
                    }
                }

                if (null != xmlNode)
                {
                    xmlNode = xmlNode.NextSibling;
                }
            }
        }

        /// <summary>
        /// Add the images found in the source file to the HTML file.
        /// </summary>
        /// <param name="images">The nodes describing the images as found in the XML source file.</param>
        /// <param name="htmlDoc">The HTML document in which the images are placed.</param>
        /// <param name="fileName">The filename of the HTML document, so we can create a relative path to the image.</param>
        /// <param name="parent">The node to which we add the images.</param>
        private void AddImagesToContent(List<XmlNode> images, XmlDocument htmlDoc, string fileName, XmlNode parent)
        {
            if ((images.Count == 1) && (null != images[0].Attributes!.GetNamedItem("location")))
            {
                var scale = images[0].Attributes!["scale"]?.Value;
                if (string.IsNullOrEmpty(scale))
                {
                    scale = "25";
                }

                // Only 1 small image, surrounded by text.
                var span = htmlDoc.CreateNodeWithAttributes(
                    "span",
                    ["class"],
                    [$"image-{images[0].Attributes!["location"]!.Value} scale{scale}"]);
                parent.InsertAfter(span, null);

                var img = htmlDoc.CreateNodeWithAttributes(
                    "img",
                    ["class", "src", "alt"],
                    ["zoom scale", Utilities.FindImage(images[0].InnerText, fileName).FileName, images[0].Attributes?["alt"]?.Value]);
                span.AppendChild(img);
            }
            else
            {
                // We have found more than 1 image, or the single image is to be displayed over the full width.
                // We are laying them out in a flex div.
                if (images.Count > 0)
                {
                    var imageInfoList = new ImageInfoList();
                    images.ForEach(img =>
                    {
                        var imgInfo = Utilities.FindImage(img.InnerText, fileName, img);
                        imageInfoList.Add(imgInfo);
                    });

                    var flexDiv = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["flex"]);
                    parent.InsertAfter(flexDiv, null);

                    imageInfoList.SetCSS();
                    foreach (ImageInfo image in imageInfoList)
                    {
                        if (!Css.ContainsKey(image.CssForDiv!.Class))
                        {
                            Css.Add(image.CssForDiv!.Class, image.CssForDiv!);
                        }

                        var imgDiv = htmlDoc.CreateNodeWithAttributes("div", ["class"], [$"{image.CssForDiv!.Class}"]);
                        flexDiv!.AppendChild(imgDiv);
                        var img = htmlDoc.CreateNodeWithAttributes(
                            "img",
                            ["class", "src", "alt"],
                            ["zoom scale", image.FileName, image.ImgNode?.Attributes?["alt"]?.Value]);
                        imgDiv!.AppendChild(img);
                    }
                }
            }
        }
    }
}
