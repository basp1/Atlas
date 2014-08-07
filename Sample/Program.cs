using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Atlas.Utils;
using Atlas.Core;
using Atlas.SnapshotIO;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Atlas.Profile
{
    class Program
    {
        static MongoDatabase connect(String dbname, Snapshot profile)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Entity)))
            {
                BsonClassMap.RegisterClassMap<Entity>(cm =>
                {
                    cm.AutoMap();
                });
            }

            var connectionString = "mongodb://localhost";
            var client = new MongoClient(connectionString);

            var server = client.GetServer();

            var database = server.GetDatabase(dbname);

            return database;
        }

        static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();

            var profile = new Snapshot();
            var profileVisitor = new ProfileVisitor(profile);
            var profileReader = new EAProfileReader();

            stopWatch.Start();
            {
                profileReader.read(profileVisitor, "data/CIM_PROFILE.xml");
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("profile read: " + stopWatch.ElapsedMilliseconds + "ms");

            var cgen = new Codegen("profile", ".");

            stopWatch.Restart();
            {
                cgen.generateProfile(profile.GetAll(), "profile.dll");
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("profile generate: " + stopWatch.ElapsedMilliseconds + "ms");

            var profileAssembly = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + "/profile.dll");
            var regionIndex = new ObjectCollection();
            var region = new Snapshot(profileAssembly, "profile", regionIndex);
            var snapshotVisitor = new SnapshotVisitor(profile, region);
            var snapshotReader = new RdfSnapshotReader();

            stopWatch.Restart();
            {
                var db = connect("test", profile);
                var col = db.GetCollection<Entity>("MetaClass");
                col.RemoveAll();
                col.InsertBatch(profile.GetAll().Values);

            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("profile connect/map: " + stopWatch.ElapsedMilliseconds + "ms");

            stopWatch.Restart();
            {
                snapshotReader.read(snapshotVisitor, "data/REGION.xml");
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("snapshot read: " + stopWatch.ElapsedMilliseconds + "ms");

            stopWatch.Restart();
            {
                var roots = (from e in (profile.GetAll().Values)
                             where 0 == (e as MetaClass).Parent.Count
                             select e)
                             .ToList();
                while (roots.Count > 0)
                {
                    var class1 = roots[0] as MetaClass;
                    roots.RemoveAt(0);

                    var classType = profileAssembly.GetType("profile." + class1.Id);
                    if (null == classType)
                    {
                        continue;
                    }

                    if (!BsonClassMap.IsClassMapRegistered(classType))
                    {
                        var classMap = BsonClassMap.LookupClassMap(classType);
                    }

                    roots.AddRange(class1.Children);
                }
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("register profile: " + stopWatch.ElapsedMilliseconds + "ms");

            stopWatch.Restart();
            {
                var db = connect("test", profile);
                var col = db.GetCollection<Entity>("Entity");
                col.RemoveAll();
                col.InsertBatch(region.GetAll().Values);

                col = db.GetCollection<Entity>("EntityIndex");
                col.RemoveAll();
                col.InsertBatch(regionIndex.GetAll().Values);
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("snapshot connect/map: " + stopWatch.ElapsedMilliseconds + "ms");

            stopWatch.Restart();
            {
                var dataIndex = new ObjectCollection();
                var data = new Snapshot(profileAssembly, "profile", dataIndex);

                var db = connect("test", profile);

                var col = db.GetCollection<Entity>("Entity");
                foreach (var e in col.FindAll())
                {
                    data.Add(e);
                }

                col = db.GetCollection<Entity>("EntityIndex");
                foreach (var e in col.FindAll())
                {
                    dataIndex.Add(e);
                }
            }
            stopWatch.Stop();
            System.Console.Out.WriteLine("snapshot load/map: " + stopWatch.ElapsedMilliseconds + "ms");

            System.Console.Out.WriteLine("--");
            System.Console.Out.WriteLine("objects count: " + region.GetAll().Count);

            return;
        }
    }
}
