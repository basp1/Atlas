using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Atlas
{
    namespace Core
    {
        public class Snapshot : ObjectCollection
        {
            public String Prefix { get; set; }
            public Assembly ProfileAssembly { get; set; }
            public ObjectCollection Index { get; private set; }

            public Snapshot() { }
            public Snapshot(Assembly profileAssembly, String prefix, ObjectCollection index)
            {
                ProfileAssembly = profileAssembly;
                Prefix = prefix;
                Index = index;
            }

            public List<T> GetAll<T>()
            {
                throw new NotImplementedException();
            }

            public new T CreateObject<T>(String id) where T : Entity
            {
                T obj = base.CreateObject<T>(id);

                registerEntity(obj.GetType(), obj);

                return obj;
            }

            public new T GetOrCreateObject<T>(String id) where T : Entity
            {
                if (Contains(id))
                {
                    return Get(id) as T;
                }
                else
                {
                    T obj = base.CreateObject<T>(id);
                    registerEntity(obj.GetType(), obj);

                    return obj;
                }
            }

            public Object Build(String className)
            {
                var type = ParseType(className);
                Object obj = Activator.CreateInstance(type);
                if (obj is Entity)
                {
                    (obj as Entity).Id = Guid.NewGuid().ToString();
                    Add(obj as Entity);
                    registerEntity(type, obj as Entity);
                }

                return obj;
            }

            public Entity Build(String className, String id)
            {
                var type = ParseType(className);
                Entity obj = Activator.CreateInstance(type) as Entity;

                obj.Id = id;
                Add(obj);
                registerEntity(type, obj as Entity);

                return obj;
            }

            private void registerEntity(Type type, Entity e)
            {
                while (null != type && type != typeof(Entity))
                {
                    var parent = Index.GetOrCreateObject<ClassIndex>(type.Name);
                    parent.Entities.Add(e);
                    type = type.BaseType;
                }
            }

            public Type ParseType(String className)
            {
                return ProfileAssembly.GetType(Prefix + "." + className);
            }
        }
    }
}