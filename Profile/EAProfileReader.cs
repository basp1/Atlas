using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Atlas.Utils;
using log4net;


namespace Atlas
{
    namespace Profile
    {
        public class EAProfileReader
        {
            ILog log;

            HPath pathAssociationEnd = new HPath("Association.connection").Tag("AssociationEnd");
            HPath pathAssociationDescription = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "description");
            HPath pathDescription = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "description");
            HPath pathDocumentation = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "documentation");
            HPath pathFeature = new HPath("Classifier.feature");
            HPath pathInitialValue = new HPath("Attribute.initialValue").Tag("Expression");
            HPath pathSourceName = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "ea_sourceName");
            HPath pathStereotype = new HPath("ModelElement.stereotype").Tag("Stereotype");
            HPath pathTargetName = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "ea_targetName");
            HPath pathType = new HPath("ModelElement.taggedValue").Tag("TaggedValue").Attr("tag", "type");
            HPath pathXmi = new HPath("XMI");

            public EAProfileReader()
            {
                this.log = LogManager.GetLogger(typeof(EAProfileReader));
            }


            public void read(ProfileVisitor visitor, string xmiFileName)
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(xmiFileName);

                XmlNode xmiNode = pathXmi.eval(dom);
                if (null == xmiNode)
                {
                    throw new ProfileException();
                }

                readDocument(visitor, xmiNode);
            }

            public void readAssociation(ProfileVisitor visitor, XmlNode node)
            {
                string id = node.GetAttribute("xmi.id");

                string stereotype = pathStereotype.eval(node).GetAttribute("name");
                if ("N/A" == stereotype)
                {
                    log.Info(String.Format(
                            "Association `{0}` has N/A stereotype. Association ignored",
                            id));
                    return;
                }

                string sourceClassName = pathSourceName.eval(node).GetAttribute("value");
                string targetClassName = pathTargetName.eval(node).GetAttribute("value");

                XmlNode sourceEnd = pathAssociationEnd.eval(node);

                if (null == sourceClassName || null == targetClassName
                        || null == sourceEnd)
                {
                    log.Warn(String
                            .Format("Association `{0}` has null attributes. Source classname `{1}`. Target classname `{2}`.  Association ignored",
                                    id, sourceClassName, targetClassName));
                    return;
                }

                string sourceName = sourceEnd.GetAttribute("name");
                string sourceMultiplicity = sourceEnd.GetAttribute("multiplicity");
                string sourceAggregation = sourceEnd.GetAttribute("aggregation");
                string sourceDescription = pathAssociationDescription.eval(sourceEnd).GetAttribute("value");

                XmlNode targetEnd = sourceEnd.NextSibling;
                if (null != targetEnd && XmlNodeType.Element != targetEnd.NodeType)
                {
                    targetEnd = targetEnd.NextSibling;
                }

                string targetName = targetEnd.GetAttribute("name");
                string targetMultiplicity = targetEnd.GetAttribute("multiplicity");
                string targetAggregation = targetEnd.GetAttribute("aggregation");
                string targetDescription = pathAssociationDescription.eval(targetEnd).GetAttribute("value");

                if (null == targetEnd || null == sourceName || null == targetName)
                {
                    log.Warn(String
                            .Format("Association `{0}` has null attributes. Source classname `{1}`. Target classname `{2}`.  Association ignored",
                                    id, sourceClassName, targetClassName));
                }
                else
                {
                    try
                    {
                        visitor.visitClassAssociation(id, sourceClassName, sourceName,
                                sourceMultiplicity, sourceAggregation, sourceDescription, targetClassName,
                                targetName, targetMultiplicity, targetAggregation, targetDescription);
                    }
                    catch (ProfileException e)
                    {
                        log.Warn(e);
                    }
                }
            }

            public void readClass(ProfileVisitor visitor, XmlNode node)
            {
                string className = node.GetAttribute("name").AsVarname();

                if (null == className)
                {
                    log.Warn("Class has null name. Class ignored");
                    return;
                }

                string classStereotype = pathStereotype.eval(node).GetAttribute("name");

                if (null != classStereotype)
                {
                    // FIXME: if ("N/A" == classStereotype || "" == classStereotype)
                    switch (classStereotype)
                    {
                        case "":
                            log.Info(String.Format("Class `{0}` has empty stereotype. Class ignored", className));
                            return;

                        case "Primitive": return;

                        case "enumeration":
                            readEnumeration(visitor, node);
                            return;
                    }
                }

                try
                {
                    visitor.visitClass(className, classStereotype);

                    string documentation = pathDocumentation.eval(node).GetAttribute("value");

                    visitor.visitClassDescription(className, documentation);

                    visitor.visitClassVisibility(className, node.GetAttribute("visibility"));

                    visitor.visitClassAbstrakt(className, node.GetAttribute("isAbstract"));
                }
                catch (ProfileException e)
                {
                    log.Warn(e);
                }

                XmlNode fields = pathFeature.eval(node);

                if (null == fields)
                {
                    return;
                }

                XmlNode field = fields.FirstChild;
                while (null != field)
                {
                    if (XmlNodeType.Element == field.NodeType)
                    {
                        readField(visitor, field, className);
                    }
                    field = field.NextSibling;
                }
            }

            public void readDocument(ProfileVisitor visitor, XmlNode root)
            {
                var node = root.FirstChild;
                while (null != node)
                {
                    if (XmlNodeType.Element != node.NodeType)
                    {
                        node = node.NextSibling;
                        continue;
                    }

                    string localName = node.LocalName;

                    switch (localName)
                    {
                        case "Model":
                        case "Namespace.ownedElement":
                        case "Package":
                        case "XMI.content":
                            readDocument(visitor, node);
                            break;

                        case "Generalization":
                            readGeneralization(visitor, node);
                            break;

                        case "Class":
                            readClass(visitor, node);
                            break;

                        case "Association":
                            readAssociation(visitor, node);
                            break;
                    }

                    node = node.NextSibling;
                }
            }

            public void readEnumeration(ProfileVisitor visitor, XmlNode root)
            {
                XmlNode node = root;
                string enumerationName = node.GetAttribute("name").AsVarname();

                if (null == enumerationName)
                {
                    log.Warn("Enumeration has null name. Enumeration ignored");
                    return;
                }

                try
                {
                    visitor.visitEnumeration(enumerationName);
                }
                catch (ProfileException e)
                {
                    log.Warn(e);
                }

                XmlNode values = pathFeature.eval(node);

                if (null == values)
                {
                    log.Info(String.Format("Enumeration `{0}` has not values", enumerationName));
                    return;
                }

                XmlNode value = values.FirstChild;
                while (null != value)
                {
                    if (XmlNodeType.Element == value.NodeType)
                    {
                        string valueName = value.GetAttribute("name").AsVarname();
                        if (null != valueName)
                        {
                            try
                            {
                                visitor.visitEnumerationValue(enumerationName,
                                        valueName);
                            }
                            catch (ProfileException e)
                            {
                                log.Warn(e);
                            }
                        }
                    }
                    value = value.NextSibling;
                }

            }

            public void readField(ProfileVisitor visitor, XmlNode field, string className)
            {
                string fieldName = field.GetAttribute("name").AsVarname();

                if (null == fieldName)
                {
                    log.Warn(String.Format(
                            "Field has null name. Classname `{0}`. Field ignored", className));
                    return;
                }

                string fieldStereotype = pathStereotype.eval(field).GetAttribute("name");

                if ("" == fieldStereotype)
                {
                    log.Info(String
                            .Format("Field `{0}` has N/A stereotype. Classname `{1}`. Field ignored",
                                    fieldName, className));
                    return;
                }

                string fieldType = pathType.eval(field).GetAttribute("value");
                string fieldDescription = pathDescription.eval(field).GetAttribute("value");

                if (null == fieldType || "" == fieldType || "?" == fieldType)
                {
                    log.Info(String.Format("Field `{0}` has empty type. Classname `{1}`. Field ignored",
                fieldName, className));
                    return;
                }

                try
                {
                    if (null == fieldType)
                    {
                        visitor.visitFieldType(className, fieldName, "string");
                    }
                    else
                    {
                        visitor.visitFieldType(className, fieldName, fieldType);
                    }

                    string initialValue = pathInitialValue.eval(field).GetAttribute("body");

                    visitor.visitFieldInitialValue(className, fieldName, fieldType, initialValue);

                    visitor.visitFieldChangeable(className, fieldName,
                            field.GetAttribute("changeable"));

                    visitor.visitFieldDescription(className, fieldName,
                            fieldDescription);

                }
                catch (ProfileException e)
                {
                    log.Warn(e);
                }

            }

            public void readGeneralization(ProfileVisitor visitor, XmlNode node)
            {
                string id = node.GetAttribute("xmi.id");

                string sourceName = pathSourceName.eval(node).GetAttribute("value");
                string targetName = pathTargetName.eval(node).GetAttribute("value");

                if (null == sourceName || null == targetName)
                {
                    log.Warn(String
                            .Format("Generalization `{0}` has null end. TargetName `{1}`.  SourceName `{2}`. Generalization ignored",
                                    id, targetName, sourceName));
                    return;
                }

                try
                {
                    visitor.visitClassGeneralization(id, sourceName, targetName);
                }
                catch (ProfileException e)
                {
                    log.Warn(e);
                }
            }
        }

    }
}