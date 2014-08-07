using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Atlas
{
    namespace Utils
    {
        public static class XmlNodeExtensions
        {
            public static string GetAttribute(this XmlNode root, string attributeName)
            {
                if (null == root)
                {
                    return null;
                }

                var attrs = root.Attributes;
                if (null == attrs)
                {
                    return null;
                }

                XmlNode attr = attrs.GetNamedItem(attributeName);
                if (null == attr)
                {
                    return null;
                }

                return attr.Value;
            }
        }
    }
}