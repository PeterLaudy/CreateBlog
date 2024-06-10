using System;
using System.Linq;
using System.Xml;

namespace CreateBlog
{
    internal static class Extensions
    {
        public static XmlNode CreateNodeWithAttributes(this XmlDocument doc, string name, string[] attributes, string?[] values)
        {
            if (attributes.Length != values.Length)
            {
                throw new ArgumentException("Attributes and values should contain the same number of elements.");
            }

            var result = doc.CreateNode(XmlNodeType.Element, name, null);
            for (int i = 0; i < attributes.Length; i++)
            {
                if (null != values[i])
                {
                    result.Attributes!.Append(doc.CreateAttribute(attributes[i]));
                    result.Attributes![attributes[i]]!.Value = values[i];
                }
            }

            return result;
        }
    }
}