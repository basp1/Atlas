using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace Atlas
{
    namespace Core
    {
        public interface IMappedList<TFrom, TTo> : ISet<TTo>
        {
            void Add(TTo item);
            void UnsafeAdd(TTo item);
            TTo First { get; }
        }

        public class MappedListSerializer : IBsonSerializer
        {
            //ArraySerializer<MongoDBRef> _serializer;
            ArraySerializer<String> _serializer;

            public MappedListSerializer()
            {
                _serializer = new ArraySerializer<String>();
            }

            public void Serialize(BsonWriter writer, Type type, Object actualObject, IBsonSerializationOptions options)
            {
                var references = new List<String>();
                dynamic objects = actualObject;
                foreach (dynamic obj in objects)
                {
                    try
                    {
                        if (null == obj.Id || "" == obj.Id)
                        {
                            //TODO log/rethrow?
                            continue;
                        }

                    }
                    catch (Exception e)
                    {
                        //TODO log/rethrow?
                        continue;
                    }
                    //references.Add(new MongoDBRef(obj.GetType().Name, obj.Id));
                    references.Add(obj.Id);
                }
                _serializer.Serialize(writer, references.GetType(), references, options);
            }

            public IBsonSerializationOptions GetDefaultSerializationOptions()
            {
                return _serializer.GetDefaultSerializationOptions();
            }

            public object Deserialize(BsonReader reader, Type nominalType, Type actualType, IBsonSerializationOptions options)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(BsonReader reader, Type type, IBsonSerializationOptions options)
            {
                throw new NotImplementedException();
            }

        }

        [BsonSerializer(typeof(MappedListSerializer))]
        public class MappedList<TFrom, TTo> : SortedSet<TTo>, IMappedList<TFrom, TTo>
        {
            Action<TFrom, TTo> mapper;
            TFrom from;
            MappedListSerializer serializer;

            public MappedList(TFrom from, Action<TFrom, TTo> mapper)
            {
                this.mapper = mapper;
                this.from = from;
                this.serializer = new MappedListSerializer();
            }

            public TTo First
            {
                get { return this.First(); }
            }

            public void UnsafeAdd(TTo item)
            {
                if (!base.Contains(item))
                {
                    base.Add(item);
                }
            }

            public void Add(TTo item)
            {
                UnsafeAdd(item);
                mapper(from, item);
            }
        }
    }
}