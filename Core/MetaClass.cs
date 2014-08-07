using System;
using System.Collections.Generic;
using Atlas.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Atlas
{
    namespace Core
    {
        public class MetaClass : Entity
        {
            public Dictionary<string, MetaAssociation> Associations { get; set; }
            public bool ShouldSerializeAssociations()
            {
                return null != Associations && Associations.Count > 0;
            }
            public MappedList<MetaClass, MetaClass> Children { get; private set; }
            public bool ShouldSerializeChildren()
            {
                return null != Children && Children.Count > 0;
            }

            [BsonDefaultValue(null)]
            [BsonIgnoreIfDefault]
            public string Description { get; set; }

            public Dictionary<string, MetaField> Fields { get; set; }
            public bool ShouldSerializeFields()
            {
                return null != Fields && Fields.Count > 0;
            }

            public MappedList<MetaClass, MetaClass> Parent { get; private set; }
            public bool ShouldSerializeParent()
            {
                return null != Parent && Parent.Count > 0;
            }

            [BsonDefaultValue(null)]
            [BsonIgnoreIfDefault]
            public string Stereotype { get; set; }

            [BsonDefaultValue(null)]
            [BsonIgnoreIfDefault]
            public string ValueType { get; set; }

            [BsonDefaultValue(false)]
            [BsonIgnoreIfDefault]
            public bool Abstrakt { get; set; }

            [BsonDefaultValue(true)]
            [BsonIgnoreIfDefault]
            public bool Defined { get; set; }

            [BsonDefaultValue(false)]
            [BsonIgnoreIfDefault]
            public bool Hidden { get; set; }

            [BsonDefaultValue(false)]
            [BsonIgnoreIfDefault]
            public bool Primitive { get; set; }

            public MetaClass(Snapshot snapshot, string id)
                : base(snapshot, id)
            {
                initialize();
            }

            public MetaClass()
                : base()
            {
                initialize();
            }

            public override void initialize()
            {
                base.initialize();
                Associations = new Dictionary<string, MetaAssociation>();
                Children = new MappedList<MetaClass, MetaClass>(this, (a, b) => b.Parent.UnsafeAdd(a));
                Parent = new MappedList<MetaClass, MetaClass>(this, (a, b) => b.Children.UnsafeAdd(a));
                Fields = new Dictionary<string, MetaField>();
            }
        }
    }
}