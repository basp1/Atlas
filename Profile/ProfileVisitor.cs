using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Utils;
using Atlas.Core;

namespace Atlas
{
    namespace Profile
    {
        public class ProfileVisitor
        {

            private Snapshot snapshot;

            public ProfileVisitor(Snapshot snapshot)
            {
                this.snapshot = snapshot;
            }


            public void visitClass(String className, String classStereotype)
            {
                MetaClass class1 = addClass(className);
                class1.Defined = true;
                class1.Stereotype = classStereotype;
            }


            public void visitClassAbstrakt(String className, String abstrakt)
            {
                MetaClass class1 = addClass(className);
                switch (abstrakt)
                {
                    case "true":
                        class1.Abstrakt = true;
                        break;

                    case "false":
                        class1.Abstrakt = false;
                        break;
                }
            }


            public void visitClassAssociation(String id,
                     String sourceClassName, String sourceName,
                     String sourceMultiplicity, String sourceAggregation, string sourceDescription,
                     String targetClassName, String targetName,
                     String targetMultiplicity, String targetAggregation, string targetDescription)
            {
                String targetFieldName = sourceName.AsVarname();
                String sourceFieldName = targetName.AsVarname();

                if (targetClassName == targetFieldName || sourceClassName == sourceFieldName)
                {
                    return;
                }

                MetaClass sourceClass = addClass(sourceClassName);
                MetaClass targetClass = addClass(targetClassName);

                MetaAssociation sourceAssoc = addAssociation(sourceClass,
                        sourceFieldName);
                sourceAssoc.Multiplicity = convertMultiplicity(sourceMultiplicity);
                sourceAssoc.Aggregation = sourceAggregation;
                sourceAssoc.EndClass = targetClassName;
                sourceAssoc.EndField = targetFieldName;
                sourceAssoc.Description = sourceDescription;

                MetaAssociation targetAssoc = addAssociation(targetClass,
                        targetFieldName);
                targetAssoc.Multiplicity = convertMultiplicity(targetMultiplicity);
                targetAssoc.Aggregation = targetAggregation;
                targetAssoc.EndClass = sourceClassName;
                targetAssoc.EndField = sourceFieldName;
                targetAssoc.Description = targetDescription;

                // TODO: if(set from little side)
            }


            public void visitClassDescription(String className,
                     String description)
            {
                MetaClass class1 = addClass(className);
                class1.Description = description;
            }


            public void visitClassGeneralization(String id,
                     String sourceName, String targetName)
            {

                MetaClass sourceClass = addClass(sourceName);
                MetaClass targetClass = addClass(targetName);

                sourceClass.Parent.Add(targetClass);
            }


            public void visitClassVisibility(String className, String visibility)
            {
                if (null == visibility)
                {
                    return;
                }

                MetaClass class1 = addClass(className);

                switch (visibility)
                {
                    case "private":
                        class1.Hidden = true;
                        break;

                    case "public":
                        class1.Hidden = false;
                        break;

                    default:
                        throw new ProfileException(String.Format("Class `{0}` has unknown visibility value `{1}`", className, visibility));
                }
            }


            public void visitEnumeration(String name)
            {
                MetaClass enumeration = addClass(name);
                enumeration.Defined = true;
                enumeration.Stereotype = "enum";
            }


            public void visitEnumerationValue(String name, String valName)
            {
                MetaClass enumeration = addClass(name);

                addEnumField(enumeration, valName);
            }


            public void visitFieldChangeable(String className,
                     String fieldName, String changeable)
            {
                MetaClass class1 = addClass(className);

                MetaField field = addField(class1, fieldName);

                switch (changeable)
                {
                    case "frozen":
                        field.Constantly = true;
                        break;

                    case "none":
                        field.Constantly = false;
                        break;

                    default:
                        throw new ProfileException(String.Format("Class `{0}` has unknown changeable value `{1}`", className, changeable));
                }
            }


            public void visitFieldDescription(String className,
                     String fieldName, String desription)
            {
                MetaClass class1 = addClass(className);

                MetaField field = addField(class1, fieldName);

                field.Description = desription;
            }


            public void visitFieldInitialValue(String className,
                     String fieldName, String type, String initialValue)
            {
                MetaClass class1 = addClass(className);

                MetaField field = addField(class1, fieldName);

                if ("Boolean" == type && null != initialValue)
                {
                    initialValue = initialValue.ToLower();
                }
                field.InitialValue = initialValue;
            }


            public void visitFieldType(String className, String fieldName, String type)
            {
                MetaClass class1 = addClass(className);

                MetaField field = addField(class1, fieldName);

                String correctedType = correctPrimitiveType(type);

                MetaClass fieldClass = addClass(correctedType);

                field.Type = correctedType;

                if (isPrimitiveType(correctedType))
                {
                    fieldClass.Primitive = true;
                }

                if ("Value" == fieldName)
                {
                    class1.ValueType = correctPrimitiveType(type);
                }

            }


            private string correctPrimitiveType(string type)
            {
                switch (type)
                {
                    case "Date":
                    case "DateTime":
                        return "String";

                    case "Blob": // base64
                        return "String";
                    case "Float":
                    case "float":
                    case "double":
                        return "Double?";

                    case "Integer":
                        return "Int32?";

                    case "Decimal":
                        return "decimal?";

                    default:
                        return type;
                }
            }


            private bool isPrimitiveType(string s)
            {
                s = s.ToLower();

                return "int" == s || "int32?" == s || "double?" == s || "decimal?" == s || "int32" == s || "float" == s
                        || "double" == s || "boolean" == s
                        || "string" == s || "decimal" == s;
            }



            private MetaAssociation addAssociation(MetaClass class1,
                     String name)
            {
                String assocName = name.AsVarname();

                var _class1 = class1;
                while (true)
                {
                    if (_class1.Associations.ContainsKey(assocName))
                    {
                        return _class1.Associations[assocName];
                    }

                    if (null == _class1.Parent || 0 == _class1.Parent.Count)
                    {
                        break;
                    }
                    _class1 = _class1.Parent.First();
                }

                var associations = class1.Associations;
                MetaAssociation assoc = new MetaAssociation();
                associations[assocName] = assoc;
                return assoc;
            }

            private MetaClass addClass(String className)
            {
                if (snapshot.Contains(className))
                {
                    return (MetaClass)snapshot.Get(className);
                }
                else
                {
                    return new MetaClass(snapshot, className);
                }
            }

            private MetaField addEnumField(MetaClass enumeration,
                     String fieldName)
            {
                return addField(enumeration, fieldName);
            }

            private MetaField addField(MetaClass class1, String key)
            {
                String fieldName = key.AsVarname();
                var _class1 = class1;
                while (true)
                {
                    if (_class1.Fields.ContainsKey(fieldName))
                    {
                        return _class1.Fields[fieldName];
                    }

                    if (null == _class1.Parent || 0 == _class1.Parent.Count)
                    {
                        break;
                    }
                    _class1 = _class1.Parent.First();
                }

                var fields = class1.Fields;
                var field = new MetaField();
                fields[fieldName] = field;
                return field;
            }

            private String convertMultiplicity(String multiplicity)
            {
                switch (multiplicity)
                {
                    case "1..":
                        return "1..*";

                    case "1":
                        return "1..1";

                    default:
                        return multiplicity;
                }
            }
        }

    }
}
