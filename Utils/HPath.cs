using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Atlas
{
    namespace Utils
    {
        public class HPath
        {
            class Attribute
            {
                public string Name;
                public string Value;
                public Attribute(string name, string value)
                {
                    Name = name;
                    Value = value;
                }
            }
            class Node
            {
                public string Name;
                public List<Attribute> Attributes = new List<Attribute>();
                public Node(string name)
                {
                    Name = name;
                }
            }

            private List<Node> path = new List<Node>();

            public HPath(string tagName)
            {
                Tag(tagName);
            }

            public HPath() { }

            public HPath Tag(string tagName)
            {
                path.Add(new Node(tagName));
                return this;
            }

            public HPath Attr(string attrName, string attrValue)
            {
                path.Last().Attributes.Add(new Attribute(attrName, attrValue));
                return this;
            }


            public XmlNode eval(XmlNode root)
            {
                var node = root;
                foreach (var tag in path)
                {
                    node = node.FirstChild;
                    while (node != null)
                    {
                        if (XmlNodeType.Element == node.NodeType)
                        {
                            if (tag.Name == node.LocalName)
                            {
                                var constraintSatisfaction = true;
                                foreach (var attr in tag.Attributes)
                                {
                                    if (attr.Value != node.GetAttribute(attr.Name))
                                    {
                                        constraintSatisfaction = false;
                                        break;
                                    }
                                }
                                if (true == constraintSatisfaction)
                                {
                                    break;
                                }
                            }
                        }
                        node = node.NextSibling;
                    }
                    if (null == node)
                    {
                        return null;
                    }
                }

                return node;
            }
        }
    }
}