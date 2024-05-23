// Most of this code is borrowed from (MIT-licensed)
// https://github.com/mcneel/pythonstubs/blob/b7fa142de4f320df969f41ac29934e81a1398ca3/builder/PyStubblerLib/StubBuilder.cs
// This code takes care of building python stub files (.pyi) so that vs code and other applications can do code completion.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenTap.Python.Stubs
{
    internal class StubBuilder
    {
        static readonly TraceSource log = Log.CreateSource("python");
        readonly HashSet<Type> typesToStub = new HashSet<Type>();

        public void AddAssembly(Assembly asm)
        {
            foreach (var type in asm.GetExportedTypes())
            {
                typesToStub.Add(type);
            }
        }

        public void AddAssembly(string location)
        {
            var asm = Assembly.LoadFrom(location);
            AddAssembly(asm);
        }

        public void BuildAssemblyStubs(string destPath)
        {
            DirectoryInfo stubsDirectory;

            stubsDirectory = new DirectoryInfo(destPath);
            var stubDictionary = new Dictionary<string, List<Type>>();
            var extensionMethods = new Dictionary<Type, HashSet<MethodInfo>>();
            foreach (var stubType in typesToStub)
            {
                if (string.IsNullOrEmpty(stubType.Namespace))
                    continue;
                if (!stubDictionary.ContainsKey(stubType.Namespace))
                    stubDictionary[stubType.Namespace] = new List<Type>();
                stubDictionary[stubType.Namespace].Add(stubType);

                // static class?
                if (stubType.IsAbstract && stubType.IsSealed && stubType.GetCustomAttribute<ExtensionAttribute>() != null)
                {
                    var methods = stubType.GetMethods();
                    foreach (var method in methods)
                    {
                        if (method.GetCustomAttribute<ExtensionAttribute>() == null)
                            continue;
                        var parameter = method.GetParameters().FirstOrDefault();
                        if (parameter == null) continue;
                        var type = parameter.ParameterType;
                        if (!extensionMethods.TryGetValue(type, out var methodList))
                        {
                            extensionMethods[type] = methodList = new HashSet<MethodInfo>();
                        }
                        methodList.Add(method);
                    }
                }
            }

            List<string> namespaces = new List<string>(stubDictionary.Keys);

            // generate stubs for each type
            foreach (var stubList in stubDictionary.Values)
                WriteStubList(stubsDirectory, namespaces.ToArray(), stubList);
        }

        public static string BuildAssemblyStubs(string targetAssemblyPath, string destPath = null)
        {
            log.Debug($"Building stubs for {targetAssemblyPath}");
            // prepare configs
            var cfgs = new BuildConfig();

            // pick a dll and load
            Assembly assemblyToStub = Assembly.LoadFrom(targetAssemblyPath);

            // extract types
            Type[] typesToStub = assemblyToStub.GetExportedTypes();
            string rootNamespace = typesToStub[0].Namespace.Split('.')[0];

            // prepare output directory
            DirectoryInfo stubsDirectory;
            if (cfgs.DestPathIsRoot && Directory.Exists(destPath))
            {
                stubsDirectory = new DirectoryInfo(destPath);
            }
            else
            {
                var extendedRootNS = cfgs.Prefix + rootNamespace + cfgs.Postfix;
                if (destPath is null || !Directory.Exists(destPath))
                    stubsDirectory = Directory.CreateDirectory(extendedRootNS);
                else
                    stubsDirectory = Directory.CreateDirectory(Path.Combine(destPath, extendedRootNS));
            }

            // build type db
            var stubDictionary = new Dictionary<string, List<Type>>();
            foreach (var stubType in typesToStub)
            {
                if (!stubDictionary.ContainsKey(stubType.Namespace))
                    stubDictionary[stubType.Namespace] = new List<Type>();
                stubDictionary[stubType.Namespace].Add(stubType);
            }

            List<string> namespaces = new List<string>(stubDictionary.Keys);

            // generate stubs for each type
            foreach (var stubList in stubDictionary.Values)
                WriteStubList(stubsDirectory, namespaces.ToArray(), stubList);

            // update the setup.py version with the matching version of the assembly
            var parentDirectory = stubsDirectory.Parent;
            string setup_py = Path.Combine(parentDirectory.FullName, "setup.py");
            if (File.Exists(setup_py))
            {
                string[] contents = File.ReadAllLines(setup_py);
                for (int i = 0; i < contents.Length; i++)
                {
                    string line = contents[i].Trim();
                    if (line.StartsWith("version="))
                    {
                        line = contents[i].Substring(0, contents[i].IndexOf("="));
                        var version = assemblyToStub.GetName().Version;
                        line = line + $"=\"{version.Major}.{version.Minor}.{version.Build}\",";
                        contents[i] = line;
                    }
                }
                File.WriteAllLines(setup_py, contents);
            }
            return stubsDirectory.FullName;
        }

        static string[] GetChildNamespaces(string parentNamespace, string[] allNamespaces)
        {
            List<string> childNamespaces = new List<string>();
            foreach (var ns in allNamespaces)
            {
                if (ns.StartsWith(parentNamespace + "."))
                {
                    string childNamespace = ns.Substring(parentNamespace.Length + 1);
                    if (!childNamespace.Contains("."))
                        childNamespaces.Add(childNamespace);
                }
            }
            childNamespaces.Sort();
            return childNamespaces.ToArray();
        }

        static void WriteStubList(DirectoryInfo rootDirectory, string[] allNamespaces, List<Type> stubTypes)
        {
            // sort the stub list so we get consistent output over time
            stubTypes.Sort((a, b) => { return a.Name.CompareTo(b.Name); });

            string[] ns = stubTypes[0].Namespace.Split('.');
            string path = rootDirectory.FullName;
            for (int i = 0; i < ns.Length; i++)
                path = Path.Combine(path, ns[i]);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "__init__.pyi");
            var sb = new StringBuilder();

            string[] allChildNamespaces = GetChildNamespaces(stubTypes[0].Namespace, allNamespaces);
            if (allChildNamespaces.Length > 0)
            {
                sb.Append("__all__ = [");
                for (int i = 0; i < allChildNamespaces.Length; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append($"'{allChildNamespaces[i]}'");
                }
                sb.AppendLine("]");
            }
            sb.AppendLine("from typing import Tuple, Set, Iterable, List, overload");
            
            foreach (var stubType in stubTypes)
            {
                var obsolete = stubType.GetCustomAttribute(typeof(System.ObsoleteAttribute));
                if (obsolete != null)
                    continue;

                sb.AppendLine();
                sb.AppendLine();
                if (stubType.IsGenericType)
                    continue; //skip generics for now
                if (stubType.IsEnum)
                {
                    sb.AppendLine($"class {stubType.Name}:");
                    var names = Enum.GetNames(stubType);
                    var values = Enum.GetValues(stubType);
                    for (int i = 0; i < names.Length; i++)
                    {
                        string name = names[i];
                        if (name.Equals("None", StringComparison.Ordinal))
                            name = $"#{name}";

                        object val = Convert.ChangeType(values.GetValue(i), Type.GetTypeCode(stubType));
                        sb.AppendLine($"    {name} = {val}");
                    }
                    continue;
                }
                string interfaces = string.Join(",", stubType.GetInterfaces().Where(x => x.IsPublic).Select(x => ToPythonType(x)));

                if (stubType.BaseType != null &&
                    stubType.BaseType.FullName.StartsWith(ns[0]) &&
                    stubType.BaseType.FullName.IndexOf('+') < 0 &&
                    stubType.BaseType.FullName.IndexOf('`') < 0
                    )
                {
                    interfaces = "," + interfaces;
                    sb.AppendLine($"class {stubType.Name}({ToPythonType(stubType.BaseType)}{interfaces}):");
                }
                else
                {
                    if(interfaces != "")
                        interfaces = "(" + interfaces + ")";
                    sb.AppendLine($"class {stubType.Name}{interfaces}:");
                }

                string classStartString = sb.ToString();

                // constructors
                ConstructorInfo[] constructors = stubType.GetConstructors();
                // sort for consistent output
                Array.Sort(constructors, MethodCompare);
                foreach (var constructor in constructors)
                {
                    if (constructors.Length > 1)
                        sb.AppendLine("    @overload");
                    sb.Append("    def __init__(self");
                    var parameters = constructor.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (0 == i)
                            sb.Append(", ");
                        sb.Append($"{SafePythonName(parameters[i].Name)}: {ToPythonType(parameters[i].ParameterType)}");
                        if (i < (parameters.Length - 1))
                            sb.Append(", ");
                    }
                    sb.AppendLine("): ...");
                }

                // methods
                MethodInfo[] methods = stubType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.IsPrivate == false && m.Name.StartsWith("<") == false)
                    .ToArray();
                
                // sort for consistent output
                Array.Sort(methods, MethodCompare);
                Dictionary<string, int> methodNames = new Dictionary<string, int>();
                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute(typeof(System.ObsoleteAttribute)) != null)
                        continue;

                    int count;
                    if (methodNames.TryGetValue(method.Name, out count))
                        count++;
                    else
                        count = 1;
                    methodNames[method.Name] = count;
                }

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute(typeof(System.ObsoleteAttribute)) != null)
                        continue;

                    if (method.DeclaringType != stubType)
                        continue;
                    var parameters = method.GetParameters();
                    int outParamCount = 0;
                    int refParamCount = 0;
                    foreach (var p in parameters)
                    {
                        if (p.IsOut)
                            outParamCount++;
                        else if (p.ParameterType.IsByRef)
                            refParamCount++;
                    }
                    int parameterCount = parameters.Length - outParamCount;

                    if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                    {
                        string propName = method.Name.Substring("get_".Length);
                        if (method.Name.StartsWith("get_"))
                            sb.AppendLine("    @property");
                        else
                        {
                            sb.AppendLine($"    @{propName}.setter");
                        }
                        sb.Append($"    def {propName}(");
                    }
                    else
                    {
                        if (methodNames[method.Name] > 1)
                            sb.AppendLine("    @overload");
                        sb.Append($"    def {method.Name}(");
                    }

                    bool addComma = false;
                    if (!method.IsStatic )
                    {
                        sb.Append("self");
                        addComma = true;
                    }
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].IsOut)
                            continue;

                        if (addComma)
                            sb.Append(", ");

                        sb.Append($"{SafePythonName(parameters[i].Name)}: {ToPythonType(parameters[i].ParameterType)}");
                        addComma = true;
                    }
                    sb.Append(")");
                    {
                        List<string> types = new List<string>();
                        if (method.ReturnType == typeof(void))
                        {
                            if (outParamCount == 0 && refParamCount == 0)
                                types.Add("None");
                        }
                        else
                            types.Add(ToPythonType(method.ReturnType));

                        foreach (var p in parameters)
                        {
                            if (p.IsOut || (p.ParameterType.IsByRef))
                            {
                                types.Add(ToPythonType(p.ParameterType));
                            }
                        }

                        sb.Append($" -> ");
                        if (outParamCount == 0 && refParamCount == 0)
                            sb.Append(types[0]);
                        else
                        {
                            sb.Append("Tuple[");
                            for (int i = 0; i < types.Count; i++)
                            {
                                if (i > 0)
                                    sb.Append(", ");
                                sb.Append(types[i]);
                            }
                            sb.Append("]");
                        }
                    }
                    sb.AppendLine(": ...");
                }
                // If no strings appended, class is empty. add "pass"
                if (sb.ToString().Length == classStartString.Length)
                {
                    sb.AppendLine($"    pass");
                }
                
            }
            File.WriteAllText(path, sb.ToString());
        }

        static string SafePythonName(string s)
        {
            if (s == "from")
                return "from_";
            return s;
        }

        static string ToPythonType(string s)
        {
            string rc = s;
            if (rc.Contains('`'))
            {
                rc = rc.Substring(0, rc.IndexOf('`'));
            }
            if (rc.EndsWith("&"))
                rc = rc.Substring(0, rc.Length - 1);

            if (rc.EndsWith("`1") || rc.EndsWith("`2"))
                rc = rc.Substring(0, rc.Length - 2);

            if (rc.EndsWith("[]"))
            {
                string partial = ToPythonType(rc.Substring(0, rc.Length - 2));
                return $"Set({partial})";
            }

            if (rc.EndsWith("*"))
                return rc.Substring(0, rc.Length - 1); // ? not sure what we can do for pointers

            if (rc.Equals("String"))
                return "str";
            if (rc.Equals("Double"))
                return "float";
            if (rc.Equals("Boolean"))
                return "bool";
            if (rc.Equals("Int32"))
                return "int";
            return rc;
        }

        static string ToPythonType(Type t)
        {
            if (t.IsGenericType && t.Name.StartsWith("IEnumerable"))
            {
                string rc = ToPythonType(t.GenericTypeArguments[0]);
                return $"Iterable[{rc}]";
            }
            // TODO: Figure out the right way to get at IEnumerable<T>
            if (t.FullName != null && t.FullName.StartsWith("System.Collections.Generic.IEnumerable`1[["))
            {
                string enumerableType = t.FullName.Substring("System.Collections.Generic.IEnumerable`1[[".Length);
                enumerableType = enumerableType.Substring(0, enumerableType.IndexOf(','));
                var pieces = enumerableType.Split('.');
                string rc = ToPythonType(pieces[pieces.Length - 1]);
                return $"Iterable[{rc}]";
            }
            if (t.FullName != null && t.FullName.StartsWith("System.Collections.Generic.IList`1[["))
            {
                string enumerableType = t.FullName.Substring("System.Collections.Generic.IList`1[[".Length);
                enumerableType = enumerableType.Substring(0, enumerableType.IndexOf(','));
                var pieces = enumerableType.Split('.');
                string rc = ToPythonType(pieces[pieces.Length - 1]);
                return $"List[{rc}]";
            }
            return ToPythonType(t.Name);
        }

        static int MethodCompare(MethodBase a, MethodBase b)
        {
            string aSignature = a.Name;
            foreach (var parameter in a.GetParameters())
                aSignature += $"_{parameter.GetType().Name}";
            string bSignature = b.Name;
            foreach (var parameter in b.GetParameters())
                bSignature += $"_{parameter.GetType().Name}";
            return aSignature.CompareTo(bSignature);
        }

        class BuildConfig
        {
            public string Prefix { get; set; } = string.Empty;
            public string Postfix { get; set; } = string.Empty;
            public bool DestPathIsRoot { get; set; } = false;
        }
    }

}
