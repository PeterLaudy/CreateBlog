using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace CreateBlog
{
    /// <summary>
    /// Class used to convert the blog content in xml format to html files.
    /// </summary>
    internal class BlogContent
    {
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

        private static List<PageInfo> AvailablePages { get; } = new(); 
 
        /// <summary>
        /// Find all page{n}.xml files and create html files from them.
        /// The files are searched for in the chapters, which are defined in the
        /// index.xml file. The links to the pages are included in the index.html file.
        /// </summary>
        /// <remarks>
        /// Note that the code will search for consecutive file numbers, starting at 1.
        /// If a file cannot be found, it will stop and continue with the next chapter.
        /// </remarks>
        public static void CreateBlogContent()
        {
            Utilities.LogMessage($"Crteating blog content for {Settings.SourceRootFolder!}");

            var indexFileName = Path.Combine(Settings.SourceRootFolder!, "index.xml");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(indexFileName);

            var htmlDoc = new XmlDocument();
            htmlDoc.Load(Path.Combine(Settings.SourceRootFolder!, "index.html"));

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
                            var img = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, indexFileName)]);

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
            FoldersToCopy.RenameLinksInHtmlFile(htmlDoc);
            Utilities.SaveHtmlPage(htmlDoc, Path.Combine(Settings.HtmlRootFolder!, "index.html"));

            // Save the JSON formatted list of available links.
            Utilities.SaveJsonFile(AvailablePages.ToArray(), Path.Combine(Settings.HtmlRootFolder!, "script", "availablePages.json"));
        }

        private static void CreateChapter(XmlNode chapter, string indexFileName, XmlDocument htmlDoc)
        {
            var indexDiv = htmlDoc.SelectSingleNode("//div[@id='content']")!;

            var link = chapter.Attributes!["link"]!.Value;
            var icon = chapter.Attributes!["icon"]!.Value;
            var title = chapter.InnerText;

            Utilities.LogMessage($"Converting blog chapter {title}");

            // Create the link in the main blog page to the first page of the chapter.
            var p = htmlDoc.CreateNodeWithAttributes("p", [], []);
            var a = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["no_decoration", $"./{link}/page1.html"]);
            var img = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, indexFileName)]);

            indexDiv.AppendChild(p);
            p.AppendChild(a);
            a.AppendChild(img);
            a.AppendChild(htmlDoc.CreateTextNode(title));

            // Create all pages for the found chapter.
            var index = 1;
            while (File.Exists(Path.Combine(Settings.SourceRootFolder!, link, $"page{index}.xml")))
            {
                Utilities.LogMessage($"Converting page {index}");

                AvailablePages.Add(new PageInfo()
                {
                    Link = $"{link}/page{index}.html",
                    Icon = Utilities.FindImage(icon, Path.Combine(Settings.HtmlRootFolder!, "new", "index.html")),
                    Title = title
                });
                CreateBlogPage(
                    link,
                    icon,
                    title,
                    index,
                    File.Exists(Path.Combine(Settings.SourceRootFolder!, link, $"page{index - 1}.xml")),
                    File.Exists(Path.Combine(Settings.SourceRootFolder!, link, $"page{index + 1}.xml"))
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
        private static void CreateBlogPage(string link, string icon, string title, int index, bool prev, bool next)
        {
            var fileName = Path.Combine(Settings.SourceRootFolder!, link, $"page{index}.xml");

            var htmlDoc = new XmlDocument();
            htmlDoc.Load(Path.Combine(Settings.SourceRootFolder!, "page.html"));

            if (prev || next)
            {
                title = $"{title} {index}";
            }

            htmlDoc.DocumentElement!.SelectSingleNode("/html/head/title")!.InnerText = title;

            var image = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(icon, fileName)]);

            var titleNode = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;
            titleNode.AppendChild(image);
            titleNode.AppendChild(htmlDoc.CreateTextNode(title));

            AddNavigationSection(htmlDoc, index, prev, next, fileName);

            ConvertBlogContent(htmlDoc, fileName);
            FoldersToCopy.RenameLinksInHtmlFile(htmlDoc);
            Utilities.SaveHtmlPage(htmlDoc, Path.Combine(Settings.HtmlRootFolder!, link, $"page{index}.html"));
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
        private static void AddNavigationSection(XmlDocument htmlDoc, int index, bool prev, bool next, string fileName)
        {
            var title = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;

            if (!prev && !next)
            {
                var newLink = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["float-right", "../index.html"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("minibus.svg", fileName)]));
                title.AppendChild(newLink);
            }
            else
            {
                var navigation = htmlDoc.CreateNodeWithAttributes("p", ["class"], ["flex"]);
                title.ParentNode!.InsertAfter(navigation, title);

                var newLink = htmlDoc.CreateNodeWithAttributes("a", ["class"], ["left"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("empty.svg", fileName)]));
                navigation.AppendChild(newLink);

                newLink = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["center", "../index.html"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("minibus.svg", fileName)]));
                navigation.AppendChild(newLink);

                newLink = htmlDoc.CreateNodeWithAttributes("a", ["class"], ["right"]);
                newLink.AppendChild(htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage("empty.svg", fileName)]));
                navigation.AppendChild(newLink);

                if (prev)
                {
                    var attribute = htmlDoc.CreateAttribute("href");
                    attribute.Value = $"./page{index - 1}.html";
                    navigation.ChildNodes[0]!.Attributes!.Append(attribute);
                    navigation.ChildNodes[0]!.ChildNodes[0]!.Attributes!["src"]!.Value = Utilities.FindImage("previous.svg", fileName);
                }

                if (next)
                {
                    var attribute = htmlDoc.CreateAttribute("href");
                    attribute.Value = $"./page{index + 1}.html";
                    navigation.ChildNodes[2]!.Attributes!.Append(attribute);
                    navigation.ChildNodes[2]!.ChildNodes[0]!.Attributes!["src"]!.Value = Utilities.FindImage("next.svg", fileName);
                }
            }
        }

        /// <summary>
        /// The actual conversion of the xml file containing the content of the blog to the html file used on the web-server.
        /// </summary>
        /// <param name="htmlDoc">The html document to which the blog content should be added.</param>
        /// <param name="fileName">The full path to the file, used to create relative links to the icons.</param>
        private static void ConvertBlogContent(XmlDocument htmlDoc, string fileName)
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
                    if (!string.IsNullOrEmpty(xmlNode.InnerXml.Trim())) {
                        div.AppendChild(p);
                        p.InnerXml = xmlNode.InnerXml;
                    }

                    if ((images.Count == 1) && (null != images[0].Attributes!.GetNamedItem("location")))
                    {
                        // Only 1 small image, surrounded by text.
                        var span = htmlDoc.CreateNodeWithAttributes("span", ["class"], [$"image-{images[0].Attributes!["location"]!.Value}"]);
                        p.InsertAfter(span, null);

                        var img = htmlDoc.CreateNodeWithAttributes(
                            "img",
                            ["class", "src", "alt"],
                            ["zoom scale", Utilities.FindImage(images[0].InnerText, fileName), images[0].Attributes?["alt"]?.Value]);
                        span.AppendChild(img);
                    }
                    else
                    {
                        // We have found more than 1 image, or the single image is to be displayed over the full width.
                        // We are laying them out in a flex div.
                        if (images.Count > 0)
                        {
                            var flexDiv = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["flex"]);
                            div.InsertAfter(flexDiv, null);

                            images.ForEach(image =>
                            {
                                var imgDiv = htmlDoc.CreateNodeWithAttributes("div", [], []);
                                flexDiv.AppendChild(imgDiv);
                                imgDiv.AppendChild(htmlDoc.CreateNodeWithAttributes(
                                    "img",
                                    ["class", "src", "alt"],
                                    ["zoom scale", Utilities.FindImage(image.InnerText, fileName), image.Attributes?["alt"]?.Value]));
                            });
                        }
                    }
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

                        images.ForEach(image =>
                        {
                            var imgDiv = htmlDoc.CreateNodeWithAttributes("div", [], []);
                            flexDiv.AppendChild(imgDiv);
                            imgDiv.AppendChild(htmlDoc.CreateNodeWithAttributes(
                                "img",
                                ["class", "src", "alt"],
                                ["zoom scale", Utilities.FindImage(image.InnerText, fileName), image.Attributes?["alt"]?.Value]));
                        });
                    }
                }

                if (null != xmlNode)
                {
                    xmlNode = xmlNode.NextSibling;
                }
            }
        }
    }
}