using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Atlas.Utils;
using log4net;

namespace Atlas
{
    namespace SnapshotIO
    {
        public class RdfSnapshotReader
        {

            private ILog log;

            private SnapshotVisitor visitor;

            public RdfSnapshotReader()
            {
                this.log = LogManager.GetLogger(typeof(RdfSnapshotReader));
            }

            public void read(SnapshotVisitor visitor, string xmlFile)
            {
                this.visitor = visitor;

                XmlTextReader reader = new XmlTextReader(xmlFile);

                readDocument(reader);
            }

            private String getAttributeByName(XmlTextReader reader, String name)
            {
                if (!reader.HasAttributes)
                {
                    return null;
                }

                for (int i = 0; i < reader.AttributeCount; ++i)
                {
                    reader.MoveToAttribute(i);

                    String attrName = reader.Prefix + ":" + reader.LocalName;
                    if (attrName == name)
                    {
                        return reader.Value;
                    }
                }
                return null;
            }

            private void readDocument(XmlTextReader reader)
            {
                string lastOpenedObjectClassName = null;

                visitor.startVisiting();
                while (reader.Read())
                {
                    if (XmlNodeType.Element == reader.NodeType && "RDF" == reader.LocalName)
                    {
                        reader.Read();
                        break;
                    }
                }

                while (reader.Read())
                {
                    if (null != lastOpenedObjectClassName)
                    {
                        continue;
                    }

                    if (XmlNodeType.Element == reader.NodeType)
                    {
                        String className = reader.LocalName;
                        lastOpenedObjectClassName = className;

                        String uid = getAttributeByName(reader, "rdf:about");

                        try
                        {
                            visitor.visitStartDocument(className, uid);
                        }
                        catch (SnapshotVisitorException e)
                        {
                            log.Warn(e);
                            #region skip object
                            while (reader.Read())
                            {
                                if (XmlNodeType.Element == reader.NodeType)
                                {
                                    String rdfResource = getAttributeByName(reader, "rdf:resource");

                                    if (null == rdfResource)
                                    {
                                        reader.Read();
                                    }
                                }
                                else if (XmlNodeType.EndElement == reader.NodeType)
                                {
                                    if (lastOpenedObjectClassName == reader.LocalName)
                                    {
                                        lastOpenedObjectClassName = null;
                                        break;
                                    }
                                }
                            }
                            #endregion skip object
                            visitor.visitEndDocument();
                            continue;
                        }

                        while (reader.Read())
                        {
                            if (XmlNodeType.Element == reader.NodeType)
                            {
                                String fieldName = stringToFieldName(reader.LocalName);

                                String fieldType = stringToFieldType(reader.LocalName);

                                String rdfResource = getAttributeByName(reader, "rdf:resource");

                                if (null != rdfResource)
                                {
                                    if (rdfResource.StartsWith("cim:"))
                                    {
                                        readObjectEnumerationValue(uid, fieldType, fieldName, rdfResource);
                                    }
                                    else
                                    {
                                        readObjectAssociation(uid, fieldType, fieldName, rdfResource);
                                    }
                                }
                                else
                                {
                                    reader.Read();
                                    if (XmlNodeType.Text != reader.NodeType)
                                    {
                                        //TODO
                                        throw new NullReferenceException();
                                    }
                                    String fieldValue = reader.Value;
                                    readObjectField(uid, fieldType, fieldName, fieldValue);
                                }
                            }
                            else if (XmlNodeType.EndElement == reader.NodeType)
                            {
                                if (lastOpenedObjectClassName == reader.LocalName)
                                {
                                    lastOpenedObjectClassName = null;
                                    break;
                                }
                            }
                        }
                        visitor.visitEndDocument();
                    }
                }
                visitor.endVisiting();
            }

            private void readObjectAssociation(string id, string fieldType, string associationName, string targetObjectId)
            {
                try
                {
                    visitor.visitDocumentAssociation(fieldType, associationName,
                            targetObjectId);
                }
                catch (SnapshotVisitorException e)
                {
                    log.Warn(e);
                }

            }

            private void readObjectEnumerationValue(String id,
                     String fieldType, String fieldName,
                     String rdfResource)
            {
                // strip enumeration value. like "cim:UnitSymbol.VAh" to "UnitSymbol.VAh"
                String enumerationValue = rdfResource.Substring(rdfResource
                        .IndexOf(':') + 1);

                try
                {
                    visitor.visitDocumentEnumerationValue(fieldType, fieldName,
                            enumerationValue);
                }
                catch (SnapshotVisitorException e)
                {
                    log.Warn(e);
                }

            }

            private void readObjectField(String id, String fieldType,
                     String fieldName, String fieldValue)
            {

                try
                {
                    visitor.visitDocumentField(fieldType, fieldName, fieldValue);
                }
                catch (SnapshotVisitorException e)
                {
                    log.Warn(e);
                }

            }

            private String stringToFieldName(String name)
            {
                int k = name.LastIndexOf('#');
                int j = name.LastIndexOf(':');
                int i = name.LastIndexOf('.');

                int m = Math.Max(Math.Max(k, j), i);

                if (-1 != m)
                {
                    name = name.Substring(m + 1);
                }
                return name;
            }

            private String stringToFieldType(String name)
            {
                int i = name.IndexOf('.');

                if (-1 != i)
                {
                    name = name.Substring(0, i);
                }
                return name;
            }

        }

    }
}
