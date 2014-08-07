using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core;
using Atlas.Utils;
using System.Reflection;
using log4net;
using System.Globalization;

namespace Atlas
{
    namespace SnapshotIO
    {
        public class SnapshotVisitor
        {
            private Entity curDoc;

            // DocumentID -> (AssociationName -> List<ID>)
            private Dictionary<String, Dictionary<String, HashSet<String>>> objectMap = new Dictionary<String, Dictionary<String, HashSet<String>>>();

            public Snapshot Profile { get; set; }

            public Snapshot Data { get; set; }

            ILog log = LogManager.GetLogger(typeof(SnapshotVisitor));

            public SnapshotVisitor(Snapshot profile,
                     Snapshot data)
            {
                Data = data;
                Profile = profile;
            }


            public Snapshot endVisiting()
            {
                updateAssociations();
                objectMap.Clear();
                return Data;
            }


            public void startVisiting()
            {
                curDoc = null;
            }


            public void visitDocumentAssociation(String propertyType,
                     String associationName, String targetObjectId)
            {

                if (!objectMap.ContainsKey(curDoc.Id))
                {
                    objectMap[curDoc.Id] = new Dictionary<String, HashSet<String>>();
                }

                Dictionary<String, HashSet<String>> associationMap = objectMap[curDoc.Id];
                if (!associationMap.ContainsKey(associationName))
                {
                    associationMap[associationName] = new HashSet<String>();
                }

                HashSet<String> references = associationMap[associationName];
                references.Add(targetObjectId);
            }


            public void visitDocumentEnumerationValue(String sourceType,
                     String fieldName, String value)
            {
                var propertyType = value.Substring(0, value.IndexOf("."));
                Object enumValue;
                try
                {
                    var enumType = Data.ParseType(propertyType);
                    enumValue = Enum.Parse(enumType, value.Substring(value.LastIndexOf(".") + 1), true);
                }
                catch (Exception e)
                {
                    log.Warn(e);
                    return;
                }
                visitDocumentField(sourceType, fieldName, enumValue);
            }


            public void visitDocumentField(String sourceType,
                     String fieldName, Object fieldValue)
            {
                // String key = StringUtils.uncapitalize(fieldName);
                var _fieldName = "value" == fieldName ? fieldName.Capitalize() : fieldName;

                if (!Profile.Contains(sourceType))
                {
                    throw new SnapshotVisitorException(
                            String.Format(
                                    "Field `{0}` within object `{1}` has null metaclass. Field source class `{2}`. Field name `{3}`. Field value `{4}`. Field ignored",
                                    _fieldName, curDoc.Id, sourceType, _fieldName,
                                    fieldValue));
                }

                MetaClass metaClass = Profile.Get(sourceType) as MetaClass;

                Dictionary<String, MetaField> fields = metaClass.Fields;

                if (!fields.ContainsKey(_fieldName))
                {
                    throw new SnapshotVisitorException(
                            String.Format(
                                    "Field `{0}` within object `{1}` was not found in metaclass. Field source type `{2}`. Field name `{3}`. Field value `{4}`. Field ignored",
                                    _fieldName, curDoc.Id, sourceType, _fieldName,
                                    fieldValue));
                }

                MetaField field = fields[_fieldName];
                String className = field.Type;

                MetaClass fieldMetaClass = Profile.Get(className) as MetaClass;

                String valueType = fieldMetaClass.ValueType;
                if (null == valueType)
                {
                    Object value = parseObjectValue(fieldValue, className);
                    if (null == curDoc.GetType().GetProperty(_fieldName))
                    {
                        throw new SnapshotVisitorException(String.Format(
                                    "Field `{0}` within object `{1}` was not found in profile. Field source type `{2}`. Field name `{3}`. Field value `{4}`. Field ignored",
                                    _fieldName, curDoc.Id, sourceType, _fieldName,
                                    fieldValue));
                    }

                    try
                    {
                        curDoc.GetType().GetProperty(_fieldName).SetValue(curDoc, value);
                    }
                    catch (Exception e)
                    {
                        throw new SnapshotVisitorException(null, e);
                    }
                }
                else
                {
                    Object value = parseObjectValue(fieldValue, valueType);
                    Object embeddedObj;
                    try
                    {
                        embeddedObj = Data.Build(className);
                        embeddedObj.GetType().GetProperty("Value").SetValue(embeddedObj, value);
                        curDoc.GetType().GetProperty(_fieldName).SetValue(curDoc, embeddedObj);
                    }
                    catch (Exception e)
                    {
                        throw new SnapshotVisitorException(null, e);
                    }
                }
            }


            public void visitEndDocument()
            {
                curDoc = null;
            }


            public Entity visitStartDocument(String className,
                     String objectId)
            {
                try
                {
                    curDoc = Data.Build(className, objectId);
                    return curDoc;
                }
                catch (Exception e)
                {
                    throw new SnapshotVisitorException(null, e);
                }
            }

            private Object parseObjectValue(Object ov, String valueClass)
            {
                if (!(ov is String))
                {
                    return ov;
                }
                switch (valueClass)
                {
                    case "Int32":
                    case "Integer":
                    case "int":
                    case "Int32?":
                        return Int32.Parse(ov as String);
                    case "Double":
                    case "double":
                    case "Float":
                    case "float":
                    case "Double?":
                        return Double.Parse(ov as String, CultureInfo.InvariantCulture);
                    case "Boolean":
                    case "bool":
                        return Boolean.Parse(ov as String);
                    default:
                        return ov;
                }
            }

            private void updateAssociations()
            {
                foreach (var id in objectMap.Keys)
                {
                    var doc = Data.Get(id) as Entity;
                    Dictionary<String, HashSet<String>> associationMap = objectMap[id];
                    foreach (var associationName in associationMap.Keys)
                    {
                        HashSet<String> references = associationMap[associationName];
                        var associationProperty = doc.GetType().GetProperty(associationName.Capitalize()).GetValue(doc, null);
                        var associationAdder = associationProperty.GetType().GetMethod("Add");
                        try
                        {
                            associationProperty = doc.GetType().GetProperty(associationName.Capitalize()).GetValue(doc, null);
                            associationAdder = associationProperty.GetType().GetMethod("Add");
                        }
                        catch (Exception e)
                        {
                            throw new SnapshotVisitorException(null, e);
                        }

                        foreach (var targetId in references)
                        {
                            if (!Data.Contains(targetId))
                            {
                                log.Warn(String.Format(
                                         "Object `{0}` reference to null object `{1}`",
                                         id, targetId));
                                continue;
                            }
                            var targetDoc = Data.Get(targetId) as Entity;
                            associationAdder.Invoke(associationProperty, new object[] { targetDoc });
                        }
                    }
                }
            }
        }

    }
}
