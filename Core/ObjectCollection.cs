using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core
{
    public class ObjectCollection
    {
        public ObjectCollection() { }

        Dictionary<string, Entity> cache = new Dictionary<string, Entity>();

        public bool Contains(string id)
        {
            return cache.ContainsKey(id);
        }

        public Entity Get(string id)
        {
            return cache[id];
        }

        public void Add(Entity e)
        {
            cache[e.Id] = e;
        }

        public Dictionary<string, Entity> GetAll()
        {
            return cache;
        }

        public T CreateObject<T>(String id) where T : Entity
        {
            T obj = Activator.CreateInstance<T>();
            obj.Id = id;
            Add(obj);
            return obj;
        }

        public T GetOrCreateObject<T>(String id) where T : Entity
        {
            if (Contains(id))
            {
                return Get(id) as T;
            }
            else
            {
                return CreateObject<T>(id);
            }
        }

    }
}
