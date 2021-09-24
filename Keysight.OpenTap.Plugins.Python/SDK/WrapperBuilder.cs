//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenTap;
using OpenTapTraceSource = OpenTap;
using System.ComponentModel;
using Keysight.Plugins.Python;
using System.Reflection.Emit;

namespace Keysight.OpenTap.Plugins.Python
{

    using SF = SyntaxFactory;
    [Obfuscation(Exclude = true)]
    public class WrapperBuilder
    {
        const string loadScript = @"
import sys
def add_dir(x):
    sys.path.append(x)
def get_dirs():
    return str(sys.path)
";
        static OpenTapTraceSource.TraceSource log = OpenTapTraceSource.Log.CreateSource("Python");
        static void userError(string message, string information)
        {
            log.Error(message);
            foreach(var inf in information.Split('\n'))
                log.Info(inf);
            Task.Factory.StartNew(() =>
            {
                var req = new ContinueRequest { message = message + "\n" + information, Response = Python.OkEnum.OK };
                UserInput.Request(req, true);
            });
        }

        static bool initialized = false;
        static bool init_success = false;

        static readonly IEnumerable<string> pyLibNames = new[] { "python27.dll", "python36.dll", "python35.dll", "python34.dll", "python33.dll", "python37.dll", "python38.dll", "libpython2.7.so", "libpython3.6.so", "libpython3.7.so", "libpython3.8.so" };

        static SharedLib tryLoadPython(string path, bool quiet = false)
        {
            var pyLibNames = WrapperBuilder.pyLibNames;
            if (string.IsNullOrWhiteSpace(PythonSettings.Current.PythonVersion) == false)
            {
                var version = PythonSettings.Current.PythonVersion;
                var version2 = version.Replace(".", "");
                pyLibNames = pyLibNames.Where(x => x.Contains(version) || x.Contains(version2));
            }
            string tried_load = null;
            if (false == string.IsNullOrWhiteSpace(path))
            {
                // force load python27.dll, in case it was not found. And to check that py is installed.
                foreach (var pyLibDllPath in pyLibNames)
                {
                    foreach (string subpath in new[] { ".", "Scripts" })
                    {
                        var pydll_location = Path.GetFullPath(Path.Combine(path, subpath, pyLibDllPath));

                        IntPtr py_handle = IntPtr.Zero;
                        if (File.Exists(pydll_location))
                        {
                            try
                            {
                                tried_load = pydll_location;
                                var sharedlib = SharedLib.Load(pydll_location);
                                if (sharedlib != null)
                                    return sharedlib;
                            }
                            catch (Exception ex)
                            {
                                log.Error("Unable to load '{0}' assembly might be in an invalid format.", pydll_location);
                                log.Debug(ex);
                                return null;
                            }

                        }
                    }
                }
            }
            foreach (var pyLibDllPath in pyLibNames)
            {
                try
                {
                    var sharedlib = SharedLib.Load(pyLibDllPath);
                    if (sharedlib != null)
                        return sharedlib;
                }catch(Exception)
                {       
                }
            }

            if (!quiet)
            {
                if (tried_load != null)
                {
                    userError("Unable to load Python.", string.Format("Unable to load '{0}'. Is Python installed in the right version?", tried_load));
                }
                else
                {
                    userError("Unable to load Python.", "Unable to detect any python installation.");
                }
            }

            return null;
        }

        static object loadLock = new object();

        class SharedLib
        {
            static bool isWin => PyThread.IsWin32;
            IntPtr getEntry(string name)
            {
                if (isWin)
                    return GetProcAddress(lib, name);
                return dlsym(lib, name);
            }

            static IntPtr load(string name)
            {
                if (isWin)
                    return LoadLibrary(name);
                return dlopen(name, rtld_now);
            }

            static void checkError()
            {
                if (isWin)
                {
                    var err = Marshal.GetLastWin32Error();
                    if (err != 0)
                    {
                        throw new Win32Exception(err);
                    }
                }
                else
                {
                    var error = dlerror();
                    
                    if(error != IntPtr.Zero)
                    {
                        throw new Exception("Unable to load python: " + error.ToString());
                    }
                }
            }

            static void clearError()
            {
                if (isWin)
                {
                    
                }
                else
                {
                    dlerror();
                }
            }

            [DllImport("kernel32.dll")]
            static extern int GetLastError();

            [DllImport("kernel32.dll")]
            static extern IntPtr LoadLibrary(string dllToLoad);

            [DllImport("kernel32.dll")]
            static extern IntPtr GetProcAddress(IntPtr hModule, string procname);
            const int rtld_now = 2;
            [DllImport("libdl.so")]
            static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl.so")]
            static extern IntPtr dlsym(IntPtr handle, string symbol);
            [DllImport("libdl.so")]

            static extern IntPtr dlerror();

            IntPtr lib = IntPtr.Zero;

            public SharedLib(IntPtr ptr)
            {
                lib = ptr;
            }

            public T GetDelegate<T>(string name)
            {
                var entry = getEntry(name);
                if (entry == null) throw new Exception("Unknown entrypoint " + name);
                return Marshal.GetDelegateForFunctionPointer<T>(entry);
            }

            public static SharedLib Load(string name)
            {
                clearError();
                IntPtr p = load(name);
                if (p == IntPtr.Zero)
                {
                    checkError();
                    return null;
                }
                return new SharedLib(p);
            }


        }

        static bool robustDirectoryExists(string path)
        {
            try
            {
                return Directory.Exists(path); 
            }
            catch
            {
                return false;
            }
        }

        static string LibFolder => PyThread.IsWin32 ? "Lib" : "lib";
        public static bool ReloadPython()
        {
            initialized = false;
            return LoadPython(false);
        }
        delegate IntPtr getStringFcn();
        public static bool LoadPython(bool startSearch = false, bool reinit=false)
        {
            typeof(ModuleBuilder).ToString();
            lock (loadLock)
            {
                if (initialized && !reinit) return init_success;
                initialized = true;
                string pypath = PythonSettings.Current.PythonPath;
                if(string.IsNullOrWhiteSpace(pypath) == false)
                {
                    pypath = Path.GetFullPath(pypath);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pypath);
                    Environment.SetEnvironmentVariable("PYTHONPATH", Path.Combine(pypath, LibFolder));
                }
                SharedLib pylib = null;
                if (robustDirectoryExists(pypath))
                {
                    var path = new DirectoryInfo(pypath);
                    pylib = tryLoadPython(path.FullName);
                }
                else if(string.IsNullOrWhiteSpace(pypath))
                {
                    pylib = tryLoadPython("");
                }

                if (pylib == null)
                {
                    string message = "Please ensure that the Python path is set to a location of an installed Python 2.7, 3.6, 3.7 or 3.8.\nTo set the Python path use the 'tap python set-path <version>' command argument.\nPython 2.7, 3.6, 3.7 or 3.8 can be downloaded from https://www.python.org/\nPlease, see the Python plugin documentation for details.";
                    if (robustDirectoryExists(pypath))
                        message += string.Format("\nIt's possible that the architecture of the installed TAP version ({0})\n does not match the installed Python version.", IntPtr.Size == 4 ? "32-bit" : "64-bit");

                    userError("Unable to load Python.", message);
                    return init_success;
                }

                var f = pylib.GetDelegate<getStringFcn>("Py_GetVersion");
                var pythonVersion = f();
                var pyVersionString = Marshal.PtrToStringAnsi(pythonVersion);
                Log.Info("Python version: {0}", pyVersionString);
                var pyversion = PythonVersion.Parse(pyVersionString);

                if (pyversion == PythonVersion.Unsupported)
                    throw new Exception("Unsupported python version selected: " + pyVersionString);

                if (PythonSettings.Current.DeployPythonNet)
                {
                    try
                    {
                        CheckDeployPythonNet(pyversion, IntPtr.Size == 4);
                    }
                    catch (Exception ex)
                    {
                        log.Warning("Unable to deploy pythonnet");
                        log.Debug(ex);
                        // this could be OK, if python has been manually installed.
                    }
                }
                
                if (startSearch)
                    PluginManager.SearchAsync();

                // Ansure that Python.Runtime.dll is loaded.
                Assembly asm = null;

                var deployedPyNet = Path.Combine(Path.GetDirectoryName(typeof(WrapperBuilder).Assembly.Location), "Python.Runtime.dll");
                var installedPyNet = string.IsNullOrEmpty(pypath) ? pypath : Path.Combine(pypath, "Python.Runtime.dll");
                if (File.Exists(deployedPyNet))
                    asm = Assembly.LoadFrom(deployedPyNet);
                else if(File.Exists(installedPyNet))
                    asm = Assembly.LoadFrom(installedPyNet);

                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    if (e.Name.Contains("Python.Runtime"))
                        return asm;
                    return null;
                };
                log.Debug("Loaded {0}", asm);
                if(asm == null)
                {
                    log.Error("Unable to load DLL from {0}", pypath);
                    return false;
                }

                try
                {
                    init_internal();
                    init_success = true;
                    PythonSettings.LoadedPath = pypath;
                }
                catch (Exception e)
                {
                    log.Error("Caught exception while initializing Python {0}", pyVersionString);
                    log.Debug(e);
                }
                return init_success;                
            }
        }

    
        static void CheckDeployPythonNet(PythonVersion pv, bool is32Bit)
        {
            var names = typeof(PythonVersion).Assembly.GetManifestResourceNames();
            string pvf = Path.GetDirectoryName(typeof(PythonVersion).Assembly.Location);
            string[] files = new[] { "Python.Runtime.dll", "clr.pyd" };
            string filename = string.Format("{0}_{1}", pv.Name, is32Bit ? "32" : "64");
            using (var str = typeof(PythonVersion).Assembly.GetManifestResourceStream(names.First(x => x.Contains("py_deploy.bin"))))
            {
                if(str == null)
                {
                    throw new Exception("Unable to load python deployment library");
                }
                var archive = new System.IO.Compression.ZipArchive(str);
                foreach (var file in files)
                {
                    string fileName;
                    if (PyThread.IsWin32)
                    {
                        fileName = string.Format("{0}/{1}", filename.ToLower(), file);
                    }
                    else
                    {
                        fileName = string.Format("{0}_mono/{1}", filename.ToLower(), file);
                    }

                    var entry = archive.GetEntry(fileName);
                    if(entry == null)
                    {
                        throw new Exception($"Unable to find entry: {fileName}");
                    }
                    string f = Path.Combine(pvf, file);
                    if (File.Exists(f))
                    {
                        bool compareStreams(Stream streama, Stream streamb)
                        {
                            int read_a = 0, read_b = 0;
                            byte[] buffer_a = new byte[1024];
                            byte[] buffer_b = new byte[1024];
                            while(true)
                            {
                                read_a = streama.Read(buffer_a, 0, buffer_a.Length);
                                read_b = streamb.Read(buffer_b, 0, buffer_b.Length);
                                if (read_a != read_b || read_a == 0) break;
                                for(int i = 0; i < read_a; i++)
                                {
                                    if (buffer_a[i] != buffer_b[i])
                                        return false;
                                }
                            }
                            if (read_a != read_b)
                                return false;
                            return true;
                        }

                        using (var estr = entry.Open())
                        {
                            using (var f2 = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                if (compareStreams(estr, f2))
                                    continue;
                            }
                        }

                        File.Delete(f);
                    }
                    using (var outfile = File.Create(f))
                    {
                        using (var estr = entry.Open())
                            estr.CopyTo(outfile);
                    }
                }
            }
        }

        /// <summary>
        /// init_internal refers to Python.Runtime, but to find this we need to help the assembly resolver by adding site-packages to DirectoriesToSearch.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void init_internal()
        {
            var pluginSearchPath = PythonSettings.Current.GetSearchPaths();
            var sem = new System.Threading.SemaphoreSlim(0);
            PyThread.Start(() =>
            {
                if (!PythonEngine.IsInitialized)
                {
                    try
                    {
                        PythonEngine.Initialize(false);
                        PythonEngine.BeginAllowThreads();
                        PyThread.PyInitialized = true;
                        log.Debug($"Loaded Pythonnet for Python Version {PythonEngine.Version}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
                else
                {
                    sem.Release();
                }
                using (Py.GIL())
                {
                    PyObject mod = PythonEngine.ModuleFromString("init_mod", loadScript);
                    mod.InvokeMethod("add_dir", Path.GetDirectoryName(typeof(PythonSettings).Assembly.Location).ToPython());
                    mod.InvokeMethod("add_dir", Path.GetDirectoryName(Path.GetDirectoryName(typeof(PythonSettings).Assembly.Location)).ToPython());
                    mod.InvokeMethod("add_dir", Path.Combine(Path.GetDirectoryName(typeof(PluginManager).Assembly.Location), @"Packages\OpenTAP").ToPython());
                    foreach (var path in pluginSearchPath)
                    {
                        if (Directory.Exists(path))
                            mod.InvokeMethod("add_dir", path.ToPython());
                    }
                    mod.InvokeMethod("get_dirs");
                    try
                    {
                        Py.Import("PythonTap");
                    }
                    catch(PythonException e)
                    {
                        printPythonException(e);
                    }
                }
            });
            sem.Wait();

        }
        public WrapperBuilder()
        {
            typeof(Microsoft.CodeAnalysis.Diagnostic).ToString();
                
            registerType(typeof(PythonSettings));
            registerType(typeof(TestPlan));
            registerType(typeof(BrowsableAttribute));
            registerType(typeof(int));
            registerType(typeof(HashSet<>));
            registerType(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException));
            registerType(typeof(System.Dynamic.ExpandoObject));
            registerType(typeof(System.Xml.Serialization.XmlAttributes));
        }

        HashSet<Assembly> assemblies = new HashSet<Assembly>();

        void registerType(Type t)
        {
            assemblies.Add(t.Assembly);
        }

        string baseCode = @"
using Keysight.Plugins.Python;
[assembly: System.Reflection.AssemblyVersion(""__AssemblyVersion__"")]
[assembly: System.Reflection.AssemblyFileVersion(""__AssemblyVersion__"")]
[assembly: System.Reflection.AssemblyInformationalVersion(""__AssemblyVersion__"")]
[assembly: System.Runtime.InteropServices.GuidAttribute(""__Guid__"")]

namespace Dynamic
{

}
";

        AttributeSyntax loadAttributeData(AttributeData attributeValue)
        {
            Type attrtype = attributeValue.Attribute;
            var attr = SF.Attribute(SF.ParseName(GetCSharpRepresentation(attrtype)));

            var allconstructors = attrtype.GetConstructors();
            foreach (var constructor in allconstructors)
            {
                var param = constructor.GetParameters();

                int idx = 0;
                foreach (object attr_arg in attributeValue.Arguments)
                {
                    if (param.Length <= idx)
                        goto next;
                    if (param[idx].GetCustomAttribute<ParamArrayAttribute>() != null)
                        break;

                    object newobj = attr_arg;
                    try
                    {
                        if(!(newobj == null))
                            if (param[idx].ParameterType.IsAssignableFrom(Convert.ChangeType(newobj, param[idx].ParameterType).GetType()) == false)
                                goto next;
                    }
                    catch
                    {
                        if (!param[idx].ParameterType.IsEnum && newobj is int)
                        {

                            goto next;
                        }
                    }
                    idx++;
                }

                foreach (var attr_arg in attributeValue.Arguments)
                {
                    attr = attr.AddArgumentListArguments(getAttributeArgument(attr_arg?.GetType() ?? typeof(object), attr_arg));
                }

                foreach (var attr_kwarg in attributeValue.KwArguments)
                {

                    var name = attr_kwarg.Item1;
                    var value = attr_kwarg.Item2;

                    var matchingParam = param.Skip(idx).FirstOrDefault(x => x.Name == name);
                    var matchingProp = attrtype.GetProperty(name);
                    if (matchingParam != null)
                    {
                        var arg = getAttributeArgument(matchingParam.ParameterType, ((PyObject)value).AsManagedObject(matchingParam.ParameterType));
                        arg = arg.WithNameColon(SF.NameColon(name));
                        attr = attr.AddArgumentListArguments(arg);

                    }
                    else if (matchingProp != null)
                    {
                        var arg = getAttributeArgument(matchingProp.PropertyType, ((PyObject)value).AsManagedObject(matchingProp.PropertyType));
                        arg = arg.WithNameEquals(SF.NameEquals(name));

                        attr = attr.AddArgumentListArguments(arg);

                    }
                }

                break;
                next:;
            }
            return attr;
        }

        static OpenTapTraceSource.TraceSource Log = OpenTapTraceSource.Log.CreateSource("Wrapper");
        internal static void printPythonException(PythonException pyx)
        {
            Log.Error(pyx.Message);
            var trace = pyx.StackTrace;
            if(trace.Contains(']'))
                trace = trace.Substring(0, trace.IndexOf(']'));
            var stk = trace.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries).Reverse();
            foreach (var line in stk)
            {
                var cleaned = line.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
                cleaned = cleaned.TrimStart(',').Trim();
                Log.Info(cleaned);
            }
        }
        Dictionary<string, string> loadedTypes = new Dictionary<string, string>();
        HashSet<Type> propertyTypes = new HashSet<Type>();
        private static PyObject enumClasses;

        TypeSyntax TypeToSyntax(Type tp)
        {
            
            if (tp.IsGenericType)
            {
                var subTypes = tp.GetGenericArguments().Select(TypeToSyntax);
                var gen = tp.GetGenericTypeDefinition();
                registerType(gen);
                var genstr = gen.FullName;
                var gen2 = genstr.Substring(0, genstr.IndexOf('`'));
                return SF.GenericName(SF.Identifier(gen2), SF.TypeArgumentList().AddArguments(subTypes.ToArray()));
            }

            if (tp.IsArray)
                return SF.ArrayType(TypeToSyntax(tp.GetElementType()))
                    .WithRankSpecifiers(new SyntaxList<ArrayRankSpecifierSyntax>(SF.ArrayRankSpecifier()));
            return SF.ParseTypeName(tp.FullName);
        }

        /// <summary>
        /// Builds an Assembly containing PythonStepWrapper (or others in future) classes that can be loaded in TAP.
        /// </summary>
        /// <param name="pythonCode">The full python code to use as source to build a C# wrapper DLL from</param>
        /// <param name="targetFile">The output path of the C# DLL</param>
        /// <param name="isCode">If the pythonCode is actual source or just contains filenames</param>
        /// <param name="namespacename">The namespace that the wrapper DLL should be within</param>
        public void Build(List<string> pythonCode, string targetFile = null, bool isCode = true, string namespacename = null)
        {
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            var assemblyVersion = GetType().Assembly.GetName().Version.ToString();

            var tree = CSharpSyntaxTree.ParseText(baseCode.Replace("__AssemblyVersion__", assemblyVersion).Replace("__Guid__", Guid.NewGuid().ToString()));
            var root = tree.GetRoot();
            var ns = (NamespaceDeclarationSyntax)root.ChildNodes().LastOrDefault();
            var ns2 = SF.NamespaceDeclaration(SF.IdentifierName("Python." + namespacename ?? "Dynamic"));
            var nsEnum = (NamespaceDeclarationSyntax)root.ChildNodes().LastOrDefault();
            using (var gil = Py.GIL())
            {
                try
                {
                    List<Action> defered = new List<Action>();
                    foreach (var _file in pythonCode)
                    {
                        var file = _file;

                        PyObject tap = PythonEngine.ImportModule("PythonTap");
                        PyObject module = tap.InvokeMethod("LoadModule", file.ToPython());
                        if (Exceptions.ErrorOccurred())
                        {
                            throw new PythonException();
                        }

                        var files = module.GetAttr("__dict__");

                        //for class
                        var classValues = tap.InvokeMethod("get_tap_classes", files.InvokeMethod("values"));
                        enumClasses = tap.InvokeMethod("get_tap_enums", files.InvokeMethod("values"));
                        foreach (var _item in new PyList(classValues))
                        {
                            var item = _item;
                            var moduleName = ((PyObject)item).GetAttr("__module__").ToString();
                            var moduleName2 = module.GetAttr("__name__").ToString();
                            if (!moduleName.StartsWith(moduleName2 + '.'))
                                continue;
                            var attributes = tap.InvokeMethod("get_attributes", item.ToPython());
                            List<AttributeData> attributeDefinitions = new List<AttributeData>();
                            var lst = new PyList(attributes);
                            foreach (var x in lst)
                            {
                                var ad = AttributeData.Create(tap, (PyObject)x);
                                attributeDefinitions.Add(ad);
                            }

                            var _interfaces = tap.InvokeMethod("GetClassInterfaces", item.ToPython());
                            List<Type> interfaces = new List<Type>();
                            foreach (var x in new PyList(_interfaces))
                            {
                                var type = (Type)((PyObject)x).AsManagedObject(typeof(Type));
                                interfaces.Add(type);
                                registerType(type);

                            }
                            ns = buildTapClass((PyObject)item, ns, file, attributeDefinitions, interfaces);

                        }

                        //for enum

                        foreach (var _item in new PyList(enumClasses))
                        {
                            var item = _item;
                            var attributes = tap.InvokeMethod("get_attributes", item.ToPython());
                            List<AttributeData> attributeDefinitions = new List<AttributeData>();
                            var lst = new PyList(attributes);
                            foreach (var x in lst)
                            {
                                var ad = AttributeData.Create(tap, (PyObject)x);
                                attributeDefinitions.Add(ad);
                            }

                            ns = buildTapEnum((PyObject)item, ns, file, attributeDefinitions);
                        }
                    }

                    if (ns.Members.Count == 0)
                    {
                        // This is a limitation from python.net, hopefully we will be able to get the error in the future.
                        throw new Exception("No types generated from python code. This is most likely an error. Could be caused by ImportError.");
                    }

                    foreach (var _ct in ns.Members.OfType<ClassDeclarationSyntax>().ToArray())
                    {
                        var ct = _ct;
                        if (propertiesToLoad.TryGetValue(_ct.Identifier.Text, out List<PropertyData> props))
                        {

                            foreach (var property in props)
                            {
                                string type;
                                if (!loadedTypes.TryGetValue(property.Type, out type))
                                    type = property.Type;
                                var propinfo = property.Value;

                                var get = SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithBody(SF.Block().AddStatements(SF.ReturnStatement(SF.CastExpression(SF.ParseTypeName(type),
                                    SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.ThisExpression(), SF.IdentifierName("getValue")))
                                    .AddArgumentListArguments(
                                        SF.Argument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(property.Name))),
                                        SF.Argument(SF.TypeOfExpression(SF.ParseTypeName(type)))
                                    )))));

                                var set = SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithBody(SF.Block().AddStatements(SF.ExpressionStatement(
                                        SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.ThisExpression(), SF.IdentifierName("setValue"))).AddArgumentListArguments(
                                            SF.Argument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(property.Name))),
                                            SF.Argument(SF.IdentifierName("value"))))));
                                var propDecl = SF.PropertyDeclaration(SF.ParseTypeName(type), property.Name).AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(get, set);

                                var attributes = property.Attributes;

                                foreach (var attribute in attributes)
                                {
                                    var attr = loadAttributeData(attribute.Value);
                                    if (attr != null)
                                        propDecl = propDecl.AddAttributeLists(SF.AttributeList().AddAttributes(attr));
                                }

                                ct = ct.AddMembers(propDecl);
                            }
                        }

                        if (methodsToLoad.TryGetValue(_ct.Identifier.Text, out List<MethodData> methods))
                        {
                            foreach (var method in methods)
                            {
                                List<ArgumentSyntax> args = new List<ArgumentSyntax>();
                                args.Add(SF.Argument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(method.Name))));

                                var parameters = SF.ParameterList();
                                for (int i = 0; i < method.ArgumentNames.Length; i++)
                                {
                                    var parameter = SF.Parameter(SF.Identifier(method.ArgumentNames[i])).WithType(TypeToSyntax(method.ArgumentTypes[i]));
                                    parameters = parameters.AddParameters(parameter);
                                    args.Add(SF.Argument(SF.IdentifierName(method.ArgumentNames[i])));
                                }

                                var callName = method.ReturnType == null ? (SimpleNameSyntax)SF.IdentifierName("Call") : SF.GenericName("Call").AddTypeArgumentListArguments(TypeToSyntax(method.ReturnType));

                                var invocation = SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SF.ThisExpression(), callName))
                                    .AddArgumentListArguments(args.ToArray());

                                var mdecl = SF.MethodDeclaration(method.ReturnType == null ? SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)) : SF.ParseTypeName(GetCSharpRepresentation(method.ReturnType)), method.Name)
                                    .WithParameterList(parameters);
                                mdecl = mdecl.WithBody(
                                    SF.Block(method.ReturnType == null ? (StatementSyntax)SF.ExpressionStatement(invocation) :
                                    SF.ReturnStatement(invocation)));
                                mdecl = mdecl.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

                                var attributes = method.Attributes;

                                foreach (var attribute in attributes)
                                {
                                    var attr = loadAttributeData(attribute.Value);
                                    if (attr != null)
                                        mdecl = mdecl.AddAttributeLists(SF.AttributeList().AddAttributes(attr));
                                }

                                ct = ct.AddMembers(mdecl);
                            }
                        }

                        ns2 = ns2.AddMembers(ct);

                    }
                    foreach (var _ct in ns.Members.OfType<EnumDeclarationSyntax>().ToArray())
                    {
                        ns2 = ns2.AddMembers(_ct);
                    }
                }
                catch (PythonException pyx)
                {

                    printPythonException(pyx);
                    throw;
                }

                catch (Exception ex)
                {
                    Log.Error("Python compilation error: {0}", ex.Message);
                    Log.Debug(ex);
                    throw;
                }
            }

            root = root.ReplaceNode(root.ChildNodes().Last(), ns2);
            root = root.NormalizeWhitespace(" ", true);
            var metadataref = new List<MetadataReference> { };
            // Detect the file location for the library that defines the object type
            var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = MetadataReference.CreateFromFile(systemRefLocation);
            var md = new HashSet<Assembly> { typeof(int).Assembly, typeof(TestStep).Assembly, typeof(WrapperBuilder).Assembly };
            try
            {
                var asm = Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("mscorlib");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System.Core");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System.Runtime");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System.Collections");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System.ComponentModel.TypeConverter");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("System.ObjectModel");
                md.Add(asm);
            }
            catch { }
            try
            {
                var asm = Assembly.Load("Microsoft.CSharp");
                md.Add(asm);
            }
            catch { }
            md.Add(Assembly.GetEntryAssembly());
            foreach (var asm in assemblies)
            {
                md.Add(asm);
            }
            foreach (var path in md)
            {
                var r = MetadataReference.CreateFromFile(path.Location);
                metadataref.Add(r);
            }

            var code = root.ToString();
            bool debugBuildEnabled = true;
            if (debugBuildEnabled)
            {
                // useful for debugging.                
                var srcFile = Path.Combine(Path.GetDirectoryName(targetFile), Path.GetFileNameWithoutExtension(targetFile) + ".cs");
                using (StreamWriter sourceWriter = new StreamWriter(srcFile))
                {
                    sourceWriter.Write(code);
                }
            }
            CSharpCompilation compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(targetFile),
                syntaxTrees: new[] { SF.ParseSyntaxTree(code) },

                references: metadataref,


               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, true, platform: Platform.AnyCpu, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
               .WithReportSuppressedDiagnostics(true)
               );

            var targetfilepath = targetFile;
            FileStream ms = null;
            try
            {
                ms = File.Open(targetfilepath, FileMode.OpenOrCreate);
            }
            catch
            {
                log.Error("Unable to open file '{0}' for writing.", targetfilepath);
                log.Info("Another process might be using the file.");
                throw new BuildException(Enumerable.Empty<string>()) { PrintErrors = false };
            }

            using (ms)
            {
                var result = compilation.Emit(ms);

                var failures = result.Diagnostics.Where(x => x.IsWarningAsError == false && x.Severity == DiagnosticSeverity.Error);
                if (failures.Any())
                {
                    throw new BuildException(result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => x.GetMessage()));
                }
            }

        }

        /// <summary> This makes sure all of Roslyn is loaded. This can take more than a second, so it makes sense to start doing this as early as possible. </summary>
        public static void RoslynWarmup()
        {
            string programText = "class test{}";
            SyntaxTree programTree = CSharpSyntaxTree.ParseText(programText);

            SyntaxTree[] sourceTrees = { programTree };

            List<MetadataReference> references = new List<MetadataReference>();

            void addAsm(Assembly asm)
            {
                references.Add(MetadataReference.CreateFromFile(asm.Location, properties: new MetadataReferenceProperties(MetadataImageKind.Assembly)));
            }
            addAsm(typeof(object).GetTypeInfo().Assembly);
            addAsm(typeof(SyntaxTree).GetTypeInfo().Assembly);
            
            // compilation
            var comp= CSharpCompilation.Create("TestApplication",sourceTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var m = new MemoryStream())
            {
                comp.Emit(m);
            }
        }
        
        public struct AttributeData
        {
            public Type Attribute;
            public object[] Arguments;
            public Tuple<string, PyObject>[] KwArguments;
            public static AttributeData Create(PyObject tap, PyObject attr)
            {
                var tp = new PyTuple(attr);
                Type attribute = (Type)tp[0].AsManagedObject(typeof(object));
                var tupleArg = new PyTuple(tp[1]);
                object[] args = null;

                if (tupleArg.Length() > 1)
                {
                    if((new PyList(enumClasses)).Contains(tupleArg[1].GetPythonType()))
                        args = new object[] { tupleArg.GetItem(0).ToString(), tupleArg.GetItem(1) };
                    else
                        args = (object[])tp[1].AsManagedObject(typeof(object[]));
                }
                else
                    args = (object[])tp[1].AsManagedObject(typeof(object[]));
                PyObject kwargs = (PyObject)tp[2];
                PyDict dict = new PyDict(kwargs);

                List<Tuple<string, PyObject>> kwargsout = new List<Tuple<string, PyObject>>();

                foreach (var ka in tap.InvokeMethod("to_list", dict.Keys()))
                {
                        string key = ka.ToString();
                        kwargsout.Add(Tuple.Create(key, dict.GetItem(key)));
                }
                return new AttributeData { Attribute = attribute, Arguments = args, KwArguments = kwargsout.ToArray() };
            }
        }
        
        public struct PropertyData
        {
            public string Name;
            public object Value;
            public string Type;
            public Dictionary<Type, AttributeData> Attributes;
        }

        public struct EnumData
        {
            public string Name;
            public string Value;
        }

        public struct MethodData
        {
            public string Name;
            public Type ReturnType;
            public Type[] ArgumentTypes;
            public string[] ArgumentNames;
            public Dictionary<Type, AttributeData> Attributes;
        }

        [Obfuscation(Exclude = true)]
        public List<PropertyData> GetPropertyData(PyObject pythonClass, string moduleName)
        {
            List<PropertyData> properties = new List<PropertyData>();
            PyObject tap = Py.Import("PythonTap");
            var items = tap.InvokeMethod("GetClassProperties", pythonClass);
            foreach (PyObject prop in new PyList(items))
            {
                //for property of type enum, use GetPropertyEnumData
                if ((bool)tap.InvokeMethod("isenum", prop.GetAttr("Type")).AsManagedObject(typeof(bool))) continue;

                var propData = new PropertyData()
                {
                    Name = prop.GetAttr("Name").ToString(),
                    Value = prop.GetAttr("Value").ToString(),
                    Attributes = new Dictionary<Type, AttributeData>()
                };

                var type = prop.GetAttr("Type");

                var cstype = (Type)((PyObject)type).AsManagedObject(typeof(Type));
                registerType(cstype);
                if (typeof(IPythonProxy).IsAssignableFrom(cstype))
                {
                    var mod = (type.GetAttr("__module__").ToString()) + "." + (type.GetAttr("__name__").ToString());
                    var plugins = PluginManager.GetPlugins<PythonWrapper>();
                    foreach (var plugin in plugins)
                    {
                        var pyname = plugin.GetCustomAttribute<PythonWrapper.PythonNameAttribute>();
                        if (pyname != null && pyname.PythonName == mod)
                        {
                            propData.Type = GetCSharpRepresentation(plugin);
                            registerType(plugin);
                        }
                    }
                    if (propData.Type == null)
                        propData.Type = mod;
                }
                else
                {
                    propData.Type = GetCSharpRepresentation(cstype);
                }

                foreach (PyObject attr in new PyList(tap.InvokeMethod("to_list", prop.GetAttr("Attributes").InvokeMethod("values"))))
                {
                    var ad = AttributeData.Create(tap, attr);
                    propData.Attributes[ad.Attribute] = ad;
                }
                

                properties.Add(propData);

            }
            return properties;
        }

        [Obfuscation(Exclude = true)]
        public List<PropertyData> GetPropertyEnumData(PyObject pythonClass, string moduleName)
        {
            List<PropertyData> properties = new List<PropertyData>();
            PyObject tap = Py.Import("PythonTap");
            var items = tap.InvokeMethod("GetClassProperties", pythonClass);
            foreach (PyObject prop in new PyList(items))
            {
                //for property of type enum, use GetPropertyEnumData
                if (false == (bool)tap.InvokeMethod("isenum", prop.GetAttr("Type")).AsManagedObject(typeof(bool))) continue;

                var propData = new PropertyData()
                {
                    Name = prop.GetAttr("Name").ToString(),
                    Attributes = new Dictionary<Type, AttributeData>()
                };

                var type = prop.GetAttr("Type");
                propData.Type = moduleName + "." + type.GetAttr("__name__");
                object value = moduleName + "." + prop.GetAttr("Value");
                propData.Value = value.ToPython();

                foreach (PyObject attr in new PyList(tap.InvokeMethod("to_list", prop.GetAttr("Attributes").InvokeMethod("values"))))
                {
                    var ad = AttributeData.Create(tap, attr);
                    propData.Attributes[ad.Attribute] = ad;
                }


                properties.Add(propData);

            }
            return properties;
        }

        public List<MethodData> GetMethodData(PyObject pythonClass, string moduleName)
        {
            List<MethodData> methods = new List<MethodData>();
            PyObject tap = Py.Import("PythonTap");
            var items = tap.InvokeMethod("GetClassMethods", pythonClass);
            foreach (PyObject prop in new PyList(items))
            {
                var methodData = new MethodData()
                {
                    Name = prop.GetAttr("Name").ToString(),
                    ReturnType = (Type)prop.GetAttr("ReturnType").AsManagedObject(typeof(Type)),
                    Attributes = new Dictionary<Type, AttributeData>()
                };

                {
                    List<string> argNames = new List<string>();
                    List<Type> argTypes = new List<Type>();
                    foreach (PyObject _item in new PyList(prop.GetAttr("Arguments")))
                    {
                        var item = PyTuple.AsTuple(_item);
                        argNames.Add(item.GetItem(0).As<string>());
                        argTypes.Add(item.GetItem(1).As<Type>());
                    }
                    methodData.ArgumentNames = argNames.ToArray();
                    methodData.ArgumentTypes = argTypes.ToArray();
                }
                
                for (int i = 0; i < methodData.ArgumentTypes.Length; i++)
                {
                    var cstype = methodData.ArgumentTypes[i];
                    registerType(cstype);
                }
                foreach (PyObject attr in new PyList(tap.InvokeMethod("to_list", prop.GetAttr("Attributes").InvokeMethod("values"))))
                {
                    var ad = AttributeData.Create(tap, attr);
                    methodData.Attributes[ad.Attribute] = ad;
                }
                methods.Add(methodData);

            }
            return methods;
        }

        Dictionary<string, List<PropertyData>> propertiesToLoad = new Dictionary<string, List<PropertyData>>();
        Dictionary<string, List<MethodData>> methodsToLoad = new Dictionary<string, List<MethodData>>();
        Dictionary<string, List<EnumData>> enumsToLoad = new Dictionary<string, List<EnumData>>();

        /// <summary>
        /// Obtain Enum name and members' value of the moduleName specified
        /// </summary>
        /// <param name="pythonEnum">Enum class object in Python</param>
        /// <param name="moduleName">Module name of the Enum class</param>
        /// <returns></returns>
        [Obfuscation(Exclude = true)]
        public List<EnumData> GetEnumData(PyObject pythonEnum, string moduleName)
        {

            List<EnumData> enums = new List<EnumData>();
            PyObject tap = Py.Import("PythonTap");

            var items = tap.InvokeMethod("GetEnumMembers", pythonEnum);
            foreach (PyObject en in new PyList(items))
            {
                var enumData = new EnumData()
                {
                    Name = en.ToString(),
                    Value = tap.InvokeMethod("GetEnumValue", new PyTuple(new PyObject[] { pythonEnum, en })).ToString()
                };

                enums.Add(enumData);
            }
            return enums;
        }

        /// <summary>
        /// Creates PythonStepWrapper Enum classes to be used in the C# DLL based on the input Python code
        /// </summary>
        /// <param name="enumClass">Enum class object</param>
        /// <param name="ns">Namespace to store the C# syntax</param>
        /// <param name="codeFileName">C# file to be generated for debugging</param>
        /// <param name="attrs">Attribute of the Enum class</param>
        NamespaceDeclarationSyntax buildTapEnum(PyObject enumClass, NamespaceDeclarationSyntax ns, string codeFileName, List<AttributeData> attrs)
        {
            PyObject tap = Py.Import("PythonTap");
            //base class Enum will not be processed.
            var enumName = enumClass.GetAttr("__name__").ToString();
            if (enumName.Equals("Enum") || enumName.Equals("AutoNumber"))
                return ns;

            //enum without members will not be processed.
            var enumDatas = GetEnumData(enumClass, codeFileName);
            if (enumDatas.Count() == 0) return ns;

            string name = enumClass.GetAttr("__name__").ToString();
            log.Info("Building " + name + " plugin.");

            var i = 0;
            var members = new List<EnumMemberDeclarationSyntax>();

            foreach (var data in enumDatas)
            {
                int output = 0;
                EnumMemberDeclarationSyntax member = null;

                if (int.TryParse(data.Value, out output))
                {
                    var objectCreationExpression = SF.EqualsValueClause(SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(int.Parse(data.Value))));
                    member = SF.EnumMemberDeclaration(attributeLists: new SyntaxList<AttributeListSyntax>(),
                        identifier: SF.Identifier(data.Name), equalsValue: objectCreationExpression);
                }
                else
                {
                    var objectCreationExpression = SF.EqualsValueClause(SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i + 1)));
                    member = SF.EnumMemberDeclaration(attributeLists: new SyntaxList<AttributeListSyntax>(),
                        identifier: SF.Identifier(data.Name), equalsValue: objectCreationExpression);
                    
                    var attr = SF.Attribute(SF.ParseName("OpenTap.DisplayAttribute"));
                    foreach (var attr_arg in tap.InvokeMethod("GetEnumValue", new PyTuple(new PyObject[] { enumClass, data.Name.ToPython() })))
                    {
                        attr = attr.AddArgumentListArguments(getAttributeArgument(attr_arg.ToString().GetType(), attr_arg.ToString()));
                    }

                    member = member.AddAttributeLists(SF.AttributeList().AddAttributes(attr));
                }
                members.Add(member);
                i++;
            }

            var declaration = SF.EnumDeclaration
                (new SyntaxList<AttributeListSyntax>(),
                baseList: null,
                identifier: SF.Identifier(name),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                members: SF.SeparatedList(members)
                );

            declaration = declaration.AddAttributeLists(
                    SF.AttributeList().AddAttributes(
                            SF.Attribute(
                                SF.ParseName("PythonWrapper.PythonName"))
                                .AddArgumentListArguments(SF.AttributeArgument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(enumClass.GetAttr("__module__").ToString() + "." + enumClass.GetAttr("__name__")))))));
            foreach (var attribute in attrs)
            {
                var attr = loadAttributeData(attribute);
                declaration = declaration.AddAttributeLists(SF.AttributeList().AddAttributes(attr));
            }

            ns = ns.AddMembers(declaration);
            return ns;
        }
        
        /// <summary>
        /// Creates PythonStepWrapper classes to be used in the C# DLL based on the input Python code.
        /// </summary>
        /// <param name="classobj"></param>
        /// <param name="ns"></param>
        /// <param name="codeFileName"></param>
        /// <param name="attrs"></param>
        NamespaceDeclarationSyntax buildTapClass(PyObject classobj, NamespaceDeclarationSyntax  ns, string codeFileName, List<AttributeData> attrs, List<Type> interfaces)
        {
            string name = classobj.GetAttr("__name__").ToString();
            log.Info("Building " + name + " plugin.");
            ClassDeclarationSyntax targetClass = SF.ClassDeclaration(name).WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)));
                
            var load_instance = SF.MethodDeclaration(SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)), "load_instance").WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)))
                .WithBody(SF.Block(SF.ExpressionStatement(
                    SF.InvocationExpression(SF.IdentifierName("load"))
                    .AddArgumentListArguments(
                        SF.Argument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(name))),
                        SF.Argument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(codeFileName)))))));
            targetClass = targetClass.AddMembers(load_instance);
            loadedTypes[classobj.GetAttr("__module__").ToString() + "." + name] = name;
            
            { // Add PythonNameAttribute
                targetClass = targetClass.AddAttributeLists(
                    SF.AttributeList().AddAttributes(
                            SF.Attribute(
                                SF.ParseName("PythonWrapper.PythonName"))
                                .AddArgumentListArguments(SF.AttributeArgument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(classobj.GetAttr("__module__").ToString() + "." + classobj.GetAttr("__name__").ToString()))))));
            }
            
            foreach (var attribute in attrs)
            {
                var attr = loadAttributeData(attribute);
                targetClass = targetClass.AddAttributeLists(SF.AttributeList().AddAttributes(attr));
            }

            var prototype = classobj.Invoke();
            object protoObject = prototype.AsManagedObject(typeof(object));
            Type protoType = protoObject.GetType();
            
            PyObject tap = Py.Import("PythonTap");
            var clsname = tap.InvokeMethod("base_class_name", classobj).ToString();
            bool isabstract = (bool)tap.InvokeMethod("IsAbstract", classobj).AsManagedObject(typeof(bool));
            if (isabstract)
                targetClass = targetClass.AddModifiers(SF.Token(SyntaxKind.AbstractKeyword));
            registerType(protoType);
            var interfacesyntax = interfaces.Select(x => SF.SimpleBaseType(TypeToSyntax(x))).ToArray();
            if (protoObject is ComponentSettings)
            {
                targetClass = targetClass.WithBaseList(SF.BaseList().AddTypes(SF.SimpleBaseType(SF.GenericName("Keysight.OpenTap.Plugins.Python.PythonComponentSettingsWrapper")
                    .AddTypeArgumentListArguments(SF.ParseTypeName(name)))).AddTypes(interfacesyntax));
            }
            else
            {
                var wrappers = Assembly.GetExecutingAssembly().ExportedTypes;
                var wrapperType = wrappers.FirstOrDefault(wrapper =>
                {
                    var attr = wrapper.GetCustomAttribute<PythonWrapper.WrappedTypeAttribute>();
                    return attr != null && attr.WrappedBaseType.IsAssignableFrom(protoType);
                });
                if (wrapperType == null) wrapperType = typeof(GenericPythonWrapper);
                if (clsname.StartsWith("PythonTap."))
                    targetClass = targetClass.AddBaseListTypes(SF.SimpleBaseType(SF.ParseTypeName(wrapperType.FullName)))
                        .AddBaseListTypes(interfacesyntax);
                else
                {
                    var splt = clsname.Split('.');
                    var n = string.Format("{0}.{1}", splt[0], splt.Last());
                    targetClass = targetClass.AddBaseListTypes(SF.SimpleBaseType(SF.ParseTypeName(n))).AddBaseListTypes(interfacesyntax);
                }
            }
            List<PropertyData> pdata = GetPropertyData(classobj, codeFileName);
            pdata.AddRange(GetPropertyEnumData(classobj, codeFileName));

            if (pdata.Count > 0)
                propertiesToLoad[name] = pdata;
            var mdata = GetMethodData(classobj, codeFileName);
            if (mdata.Count > 0)
                methodsToLoad[name] = mdata;
            ns = ns.AddMembers(targetClass);
            return ns;
        }
        static AttributeArgumentSyntax getAttributeArgument(Type type, object value)
        {
            return SF.AttributeArgument(getExpression(type, value));
        }

static ExpressionSyntax getExpression(Type type, object value)
        {
            if (value == null)
                return SF.LiteralExpression(SyntaxKind.NullLiteralExpression);
            var lst = value as IEnumerable<CustomAttributeTypedArgument>;
            var array = value as IEnumerable<object>;
            if (lst == null || value is string)
            {
                if (type == typeof(PyObject))
                {
                    return SF.ParseTypeName(value.ToString());
                }
                if (type.IsEnum)
                    return SF.CastExpression(SF.ParseTypeName(GetCSharpRepresentation(type)), SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal((int)value)));

                if (type.IsNumeric())
                    return getLiteralExpression(value);
                if (type == typeof(string))
                    return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal((string)value));
                if (type == typeof(Type))
                {
                    return SF.TypeOfExpression(SF.ParseTypeName(((Type)value).FullName));
                }
                if (type == typeof(Boolean))
                {
                    if ((bool)value == true)
                        return SF.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                    else
                        return SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                }
            }
            if(!(lst==null))
                return SF.ArrayCreationExpression(SF.ArrayType(SF.ParseTypeName(type.GetElementType().FullName)),
                    SF.InitializerExpression(SyntaxKind.ArrayInitializerExpression, new SeparatedSyntaxList<ExpressionSyntax>().AddRange(lst.Select(x => getExpression(x.ArgumentType, x.Value)))));

            if (!(array == null))
            {
                //initialize string array
                var initStrArray = SF.ArrayType(SF.ParseTypeName(type.GetElementType().FullName),
                    SF.SingletonList<ArrayRankSpecifierSyntax>(SF.ArrayRankSpecifier()));

                return SF.ArrayCreationExpression(SF.Token(SyntaxKind.NewKeyword), initStrArray,
                    SF.InitializerExpression(SyntaxKind.ArrayInitializerExpression, 
                    new SeparatedSyntaxList<ExpressionSyntax>()
                    .AddRange(array.Select(x => getExpression(type, x)))));
            }
            return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal((string)value));
        }

        static LiteralExpressionSyntax getLiteralExpression(object value)
        {
            return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal((dynamic)value));
        }
        static string GetCSharpRepresentation(Type t)
        {
            string baseName;
            if (t.IsNested)
            {
                baseName = GetCSharpRepresentation(t.DeclaringType);
            }
            else
            {
                baseName = t.Namespace;
            }

            string name = baseName + "." + t.Name;

            if (t.IsGenericType)
            {
                // 'List`1' -> 'List'
                var _name = t.Name.Substring(0, t.Name.IndexOf('`'));
                name = baseName + "." + _name;
                var genericArgs = t.GetGenericArguments().ToList();

                // Recursively get generic arguments.
                string genericName = name + "<" + string.Join(",", genericArgs.Select(GetCSharpRepresentation)) + ">";
                name = genericName;
            }
            if (t.IsArray)
            {
                // if t is an array, the []'s are in t.Name, so nothing needs to be done.
            }

            return name;
        }
    }

    public enum OkEnum
    {
        [Display("OK")]
        OK
    }

    class ContinueRequest
    {
        [Browsable(true)]
        public string Message => message;
        internal string message;
        [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)]
        [Submit]
        public OkEnum Response { get; set; }
    }

    [Obfuscation(Exclude = true)]
    static class HelperExtensions
    {
        public static bool IsNumeric(this Type t)
        {
            if (t.IsEnum)
                return false;
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }

    class BuildException : Exception
    {
        public readonly IEnumerable<string> Messages;
        public BuildException(IEnumerable<string> messages): base(string.Join("\n", messages))
        {
            this.Messages = messages;
        }

        public bool PrintErrors { get; internal set; } = true;
    }
}
