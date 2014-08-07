using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using Atlas.Core;
using Atlas.Utils;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using log4net;
using NVelocity;
using NVelocity.App;
using NVelocity.Context;



namespace Atlas
{
    namespace Profile
    {
        public class Codegen
        {
            ILog log = LogManager.GetLogger(typeof(EAProfileReader));
            private string unitNamespace;
            private string temporaryFilesPath;

            public Codegen(string unitNamespace, string temporaryFilesPath)
            {
                this.temporaryFilesPath = temporaryFilesPath;
                this.unitNamespace = unitNamespace;
                Velocity.Init();
            }

            public bool build(string[] fileNames, string assemblyName)
            {
                var provider = new CSharpCodeProvider();

                CompilerParameters cp = new CompilerParameters();

                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("AtlasCore.dll");
                cp.GenerateExecutable = false;
                cp.OutputAssembly = assemblyName;
                cp.GenerateInMemory = false;

                CompilerResults results = provider.CompileAssemblyFromFile(cp, fileNames);

                if (results.Errors.Count > 0)
                {
                    log.Error(String.Format("Errors building {0} into {1}", fileNames, results.PathToAssembly));
                    foreach (CompilerError errorNote in results.Errors)
                    {
                        log.Error(errorNote);
                        System.Diagnostics.Debug.WriteLine(errorNote);
                    }
                    return false;
                }

                return true;
            }

            public void generateSource(CodeCompileUnit targetUnit, string fileName)
            {

                var provider = CodeDomProvider.CreateProvider("CSharp");
                var options = new CodeGeneratorOptions();
                options.BracingStyle = "C";

                using (var sourceWriter = new StreamWriter(fileName))
                {
                    provider.GenerateCodeFromCompileUnit(
                        targetUnit, sourceWriter, options);
                }
            }

            public void generateProfile(Dictionary<string, Entity> classes, string assemblyName)
            {
                var fileNames = new List<string>();

                foreach (var entity in classes.Values)
                {
                    var class1 = (MetaClass)entity;

                    if (true != class1.Defined)
                    {
                        continue;
                    }

                    if (null == class1.Stereotype)
                    {
                        generateClass(classes, class1);
                    }
                    else
                    {
                        switch (class1.Stereotype)
                        {
                            case "CIMDatatype":
                            case "Datatype":
                                generateClass(classes, class1);
                                break;

                            case "enum":
                                generateEnum(class1);
                                break;

                            default:
                                generateClass(classes, class1);
                                break;
                        }
                    }

                    fileNames.Add(class1.Id + ".cs");
                }

                build(fileNames.ToArray(), assemblyName);
            }

            private void generateClass(Dictionary<string, Entity> entities, MetaClass class1)
            {
                VelocityContext context = new VelocityContext();
                context.Put("namespace", unitNamespace);
                context.Put("class", class1);
                context.Put("classes", entities);
                context.Put("root", typeof(Entity).Name);

                using (TextWriter writer = File.CreateText(class1.Id + ".cs"))
                {
                    Velocity.MergeTemplate(
                        @"vm/rich_class.vm",
                        Encoding.UTF8.WebName,
                        context,
                        writer);
                }
            }

            private void generateEnum(MetaClass class1)
            {
                VelocityContext context = new VelocityContext();
                context.Put("namespace", unitNamespace);
                context.Put("name", class1.Id);
                context.Put("values", class1.Fields.Keys);

                using (TextWriter writer = File.CreateText(class1.Id + ".cs"))
                {
                    Velocity.MergeTemplate(
                        @"vm/enum.vm",
                        Encoding.UTF8.WebName,
                        context,
                        writer);
                }
            }
        }

    }
}


