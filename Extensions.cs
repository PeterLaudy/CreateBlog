using System;
using System.Xml;

namespace CreateBlog
{
    /// <summary>
    /// Class containing extension methods.
    /// </summary>
    /// <remarks>
    /// These are mainly convenience methods.
    /// </remarks>
    internal static class Extensions
    {
        /// <summary>
        /// Method to generate an XML node with the given attributes having the given values.
        /// </summary>
        /// <param name="doc">The xml document for which the node is created.</param>
        /// <param name="name">The tag name of the node to create.</param>
        /// <param name="attributes">The list of attributes to create for the node.</param>
        /// <param name="values">The list of values for the attributes of the node.</param>
        public static XmlNode CreateNodeWithAttributes(this XmlDocument doc, string name, string[]? attributes = null, string?[]? values = null)
        {
            if ((null == attributes) && (null != values))
            {
                throw new ArgumentException("Attributes and values should both be null or not.");
            }

            if ((null != attributes) && (null == values))
            {
                throw new ArgumentException("Attributes and values should both be null or not.");
            }

            if ((null != attributes) && (attributes!.Length != values!.Length))
            {
                throw new ArgumentException("Attributes and values should contain the same number of elements.");
            }

            var result = doc.CreateNode(XmlNodeType.Element, name, null);
            if (null != attributes)
            {
                for (int i = 0; i < attributes!.Length; i++)
                {
                    result.Attributes!.Append(doc.CreateAttribute(attributes![i]));
                    if (null != values![i])
                    {
                        result.Attributes![attributes[i]]!.Value = values![i];
                    }
                }
            }

            return result;
        }
    }
}