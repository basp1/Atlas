using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Atlas
{
    namespace Core
    {
        [BsonKnownTypes(typeof(MetaClass))]
        public class Entity : IComparable
        {
            public int CompareTo(object obj)
            {
                if (obj == null)
                {
                    return 1;
                }
                if (!(obj is Entity))
                {
                    return 1;
                }

                return Id.CompareTo((obj as Entity).Id);
            }

            [BsonId]
            public string Id { get; set; }

            [BsonIgnore]
            public Snapshot MasterSnapshot { get; set; }

            public MappedList<Entity, ClassIndex> _class { get; private set; }
            public bool ShouldSerialize_class()
            {
                return null != _class && _class.Count > 0;
            }

            public Entity(Snapshot snapshot, string id)
            {
                this.Id = id;
                this.MasterSnapshot = snapshot;
                initialize();
                snapshot.Add(this);
            }

            [BsonConstructor]
            public Entity()
            {
                initialize();
            }

            [BsonExtraElements]
            public BsonDocument CatchAll { get; set; }

            public virtual void initialize()
            {
                _class = new MappedList<Entity, ClassIndex>(this, (a, b) => b.Entities.UnsafeAdd(a));
            }
        }
    }
}
