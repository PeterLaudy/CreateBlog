using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace CreateBlog
{
    internal class BlogContent
    {
        public static void CreateBlogContent()
        {
            var indexFileName = Path.Combine(Settings.RootFolder!.FullName, "index.xml");
            var availablePages = new Dictionary<string, string>();

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(indexFileName);

            var htmlDoc = new XmlDocument();
            htmlDoc.Load("index.html");

            var indexDiv = htmlDoc.SelectSingleNode("//div[@id='content']")!;
            foreach (XmlNode chapter in xmlDoc.SelectNodes("//chapters/chapter")!)
            {
                var subdir = chapter.InnerText.Replace(" ", string.Empty).ToLower();
                var iconName = chapter.Attributes!["icon"]!.Value;

                // Create the link in the main blog page to the first page of the chapter.
                var p = htmlDoc.CreateNodeWithAttributes("p", [], []);
                var a = htmlDoc.CreateNodeWithAttributes("a", ["class", "href"], ["no_decoration", $"./{subdir}/page1.html"]);
                var img = htmlDoc.CreateNodeWithAttributes("img", ["class", "src"], ["icon", Utilities.FindImage(iconName, indexFileName)]);

                indexDiv.AppendChild(p);
                p.AppendChild(a);
                a.AppendChild(img);
                a.AppendChild(htmlDoc.CreateTextNode(chapter.InnerText));

                // Create all pages for the found chapter.
                var index = 1;
                while (File.Exists(Path.Combine(Settings.RootFolder!.FullName, subdir, $"page{index}.xml")))
                {
                    availablePages.Add($"{subdir}/page{index}.html", Utilities.FindImage(iconName, Path.Combine(Settings.RootFolder!.FullName, "new", "index.html")));
                    CreateBlogPage(
                        char.ToUpper(subdir[0]) + subdir.Substring(1).ToLower(),
                        index,
                        File.Exists(Path.Combine(Settings.RootFolder!.FullName, subdir, $"page{index - 1}.xml")),
                        File.Exists(Path.Combine(Settings.RootFolder!.FullName, subdir, $"page{index + 1}.xml")),
                        iconName
                    );
                    index++;
                }
            }

            // Save the index.html file of the blog.
            SaveHtmlPage(htmlDoc, Path.ChangeExtension(indexFileName, ".html"));

            // Save the JSON formatted list of available links.
            SaveJsonFile(availablePages.ToArray(), Path.Combine(Settings.RootFolder!.FullName, "script", "availablePages.json"));
        }

        private static void CreateBlogPage(string subject, int index, bool prev, bool next, string iconName)
        {
            var fileName = Path.Combine(Settings.RootFolder!.FullName, subject, $"page{index}.xml");

            var htmlDoc = new XmlDocument();
            htmlDoc.Load("page.html");

            htmlDoc.DocumentElement!.SelectSingleNode("/html/head/title")!.InnerText = $"{subject} {index}";

            var title = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;
            var image = htmlDoc.CreateElement("img");

            var attribute = htmlDoc.CreateAttribute("class");
            attribute.Value = "icon";
            image.Attributes.Append(attribute);

            attribute = htmlDoc.CreateAttribute("src");
            attribute.Value = Utilities.FindImage(iconName, fileName);
            image.Attributes.Append(attribute);
            title.AppendChild(image);

            title.AppendChild(htmlDoc.CreateTextNode($"{subject} {index}"));

            AddNavigationSection(htmlDoc, index, prev, next, fileName);

            ConvertBlogContent(htmlDoc, fileName);
            SaveHtmlPage(htmlDoc, Path.ChangeExtension(fileName, ".html"));
        }

        private static void AddNavigationSection(XmlDocument htmlDoc, int index, bool prev, bool next, string fileName)
        {
            var title = htmlDoc.DocumentElement!.SelectSingleNode("//p[@id='title']")!;

            if (!prev && !next)
            {
                title.AppendChild(htmlDoc.CreateNodeWithAttributes("a", ["class"], ["float-right"]));
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
                    div.AppendChild(p);

                    if ((images.Count == 1) && (null != images[0].Attributes!.GetNamedItem("location")))
                    {
                        var span = htmlDoc.CreateNodeWithAttributes("span", ["class"], [$"image-{images[0].Attributes!["location"]!.Value}"]);
                        p.AppendChild(span);

                        var img = htmlDoc.CreateNodeWithAttributes(
                            "img",
                            ["class", "src", "alt"],
                            ["zoom scale", Utilities.FindImage(images[0].InnerText, fileName), images[0].Attributes?["alt"]?.Value]);
                        span.AppendChild(img);
                    }
                    else
                    {
                        var flexDiv = htmlDoc.CreateNodeWithAttributes("div", ["class"], ["flex"]);
                        div.InsertBefore(flexDiv, p);

                        images.ToList().ForEach(image =>
                        {
                            var imgDiv = htmlDoc.CreateNodeWithAttributes("div", [], []);
                            flexDiv.AppendChild(imgDiv);
                            imgDiv.AppendChild(htmlDoc.CreateNodeWithAttributes(
                                "img",
                                ["class", "src", "alt"],
                                ["zoom scale", Utilities.FindImage(image.InnerText, fileName), image.Attributes?["alt"]?.Value]));
                        });
                    }

                    var text = htmlDoc.CreateTextNode(Utilities.SetCorrectIndentForText(xmlNode.InnerText, p));
                    p.AppendChild(text);

                    images.Clear();
                }

                if (null != xmlNode)
                {
                    xmlNode = xmlNode.NextSibling;
                }
            }
        }

        private static void SaveHtmlPage(XmlDocument htmlDoc, string fileName)
        {
            using var xmlReader = new XmlNodeReader(htmlDoc);
            var xDoc = XDocument.Load(xmlReader);
            xDoc.DocumentType!.InternalSubset = null;
            using var xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true });
            xDoc.Save(xmlWriter);
        }

        private static void SaveJsonFile(object data, string fileName)
        {
            var jsonData = JsonSerializer.Serialize(data);
            using var writer = new StreamWriter(fileName);
            writer.WriteLine(jsonData);
            writer.Close();
        }
    }
}