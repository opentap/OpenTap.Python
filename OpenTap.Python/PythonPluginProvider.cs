using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using OpenTap.Package;
using Python.Runtime;

namespace OpenTap.Python
{
    [PluginOrder(typeof(DotNetTypeDataSearcher))]
    public class PythonPluginProvider : ITypeDataSearcher, ITypeDataProvider, ITypeDataSourceProvider
    {
        class PythonTypeDataSource : ITypeDataSource
        {
            public string Name { get; }
            public string Location { get; }
            public IEnumerable<ITypeData> Types => DiscoveredTypes.Values;
            public IEnumerable<object> Attributes => Array.Empty<object>();

            public IEnumerable<ITypeDataSource> References => new ITypeDataSource[]
                {TypeData.GetTypeDataSource(TypeData.FromType(typeof(PythonTypeDataSource)))};

            public string Version { get; }
            public Dictionary<string, TypeData> DiscoveredTypes { get; } = new Dictionary<string, TypeData>();

            public PythonTypeDataSource(string name, string location)
            {
                Name = name;
                Location = location;
                Version = Installation.Current.FindPackageContainingFile(Location)?.Version?.ToString() ?? "0.1";
            }
        }

        public PythonPluginProvider()
        {
            if (!PythonEngine.IsInitialized) TapThread.Start(() => PythonInitializer.LoadPython());
        }

        static readonly TraceSource log = Log.CreateSource("Python");

        public void Search()
        {
            // ensure that it's loaded.
            PythonInitializer.LoadPython();
            if (!PythonEngine.IsInitialized) return; //something went wrong and python is not initialized.
            using (Py.GIL())
            {
                var types = new List<TypeData>();

                var modules = new List<string>();

                foreach (var sp in PythonSettings.Current.GetSearchList())
                {
                    if (Directory.Exists(sp) == false) continue;
                    var mod2 = Directory.EnumerateDirectories(sp, "*", SearchOption.TopDirectoryOnly)
                        .Where(dir => Directory.EnumerateFiles(dir, "*.py").Any()).ToList();
                    modules.AddRange(mod2);
                }
                if(modules.Any())
                    Py.Import("opentap");

                var sources = new Dictionary<string, PythonTypeDataSource>();

                var visitedTypes2 = new HashSet<PyType>();
                foreach (var file in modules)
                {
                    var baseModule = Path.GetFileNameWithoutExtension(file);
                    
                    try
                    {
                        
                        var pyFiles = Directory.EnumerateFiles(file, "*.py");
                        foreach (var py in pyFiles)
                        {
                            
                            var subName = Path.GetFileNameWithoutExtension(py);
                            var moduleName = baseModule;
                            if (subName != "__init__")
                            {
                                moduleName = baseModule + "." + subName;
                            }

                            if (moduleName == "Python.opentap")
                                continue;

                            log.Debug("Loading: {0}", moduleName);

                            PyObject module;
                            try
                            {
                                module = Py.Import(moduleName);
                            }
                            catch(Exception e){
                                log.Error("Caught exception loading {0}: {1}", moduleName, e.Message);
                                log.Debug(e);
                                continue;
                            }
                            var files = module.GetAttr("__dict__");

                            var objValues = new PyDict(files);

                            var values = objValues.Values().ToArray();

                            for (int i = 0; i < values.Length; i++)
                            {
                                var _item = values[i];
                                var name = _item.GetPythonType().Name;

                                if (name != "CLRMetatype") continue;

                                var pyType = new PyType(_item);
                                var type = (Type) _item.AsManagedObject(typeof(Type));

                                if (!type.Assembly.IsDynamic)
                                    continue;

                                if (!pyType.HasAttr("__module__")) continue;
                                var mod = pyType.GetAttr("__module__").As<string>();
                                if (!sources.TryGetValue(mod, out var asm))
                                {
                                    asm = sources[mod] = new PythonTypeDataSource(mod, py);
                                }

                                var td = TypeData.FromType(type);
                                if (visitedTypes2.Add(pyType))
                                    types.Add(td);
                                asm.DiscoveredTypes[td.Name] = td;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error("Caught exception loading {0}: {1}", file, e.Message);
                        log.Debug(e);
                    }
                }

                PythonPluginProvider.types =
                    types.ToImmutableDictionary(x => x.Name, x => new PythonTypeDataWrapper(x));
                PythonPluginProvider.typeDataSources = sources.Values
                    .SelectMany(x => x.DiscoveredTypes.Select(y => (y.Value, x)))
                    .ToImmutableDictionary(x => new PythonTypeDataWrapper(x.Value), x => x.x);
            }
        }

        static ImmutableDictionary<PythonTypeDataWrapper, PythonTypeDataSource> typeDataSources =
            ImmutableDictionary<PythonTypeDataWrapper, PythonTypeDataSource>.Empty;

        static ImmutableDictionary<string, PythonTypeDataWrapper> types =
            ImmutableDictionary<string, PythonTypeDataWrapper>.Empty;

        public IEnumerable<ITypeData> Types => types.Values;

        public ITypeDataSource GetSource(ITypeData typeData)
        {
            while (!(typeData is TypeData) && typeData != null)
                typeData = typeData.BaseType;

            if (typeData is TypeData td && td.Type.Assembly.IsDynamic &&
                typeDataSources.TryGetValue(new PythonTypeDataWrapper(td), out var src))
                return src;


            return null;
        }

        public ITypeData GetTypeData(string identifier) => types.GetValueOrDefault(identifier);

        public ITypeData GetTypeData(object obj)
        {
            return null;
        }

        public double Priority => 1.0;
    }
}