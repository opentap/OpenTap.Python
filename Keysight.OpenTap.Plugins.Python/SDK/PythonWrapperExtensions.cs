//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using OpenTap;
using OpenTapTraceSource = OpenTap;
using Keysight.OpenTap.Plugins.Python;

namespace Keysight.Plugins.Python
{
    /// <summary> These extension methods are used by generated C# code. </summary>
    [Obfuscation(Exclude = true)]
    public static class PythonWrapperExtensions
    {
        public static object getValue(this IPythonWrapper wrapper, string attr, Type type)
        {
            object result = null;
            PyThread.Invoke(() =>
            {
                if (wrapper.PythonObject.HasAttr(attr) == false)
                {
                    result = null;
                    return;
                }
                PyObject at = wrapper.PythonObject.GetAttr(attr);
                result = PythonConverter.FromPythonObject(at, type);
            }, true);
            return result;
        }

        public static void setValue(this IPythonWrapper wrapper, string attr, object value)
        {
            PyThread.Invoke(() =>
            {
                PyObject result = PythonConverter.ToPythonObject(value);
                wrapper.PythonObject.SetAttr(attr, result);
            }, true);
        }

        /// <summary> Calls a named method on the python object without any return value. </summary>
        /// <param name="wrapper"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public static void Call(this IPythonWrapper wrapper, string method, params object[] args)
        {
            wrapper.Call<object>(method, args);
        }

        /// <summary>
        /// Calls a named python method on the class with a return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Call<T>(this IPythonWrapper wrapper, string method, params object[] args)
        {
            T result = default(T);
            PyThread.Invoke(() =>
            {
                if (wrapper.PythonObject.HasAttr(method) == false)
                    throw new MissingMethodException(method);
                PyObject at = wrapper.PythonObject.GetAttr(method);
                PyObject[] args2 = new PyObject[args.Length];

                for(int i = 0; i < args.Length; i++)
                    args2[i] = PythonConverter.ToPythonObject(args[i]);
                var pyResult = at.Invoke(args2);
                result = (T)PythonConverter.FromPythonObject(pyResult, typeof(T));
            });
            return result;
        }

        static Dictionary<string, PythonModule> moduleLookup = new Dictionary<string, PythonModule>();
        static List<string> transverseObj = new List<string>();
        /// <summary>
        /// Loads python code and creates a PythonStep object using the provided name as the name of the class to create a PythonStep from.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="moduleName"></param>
        public static void Load(IPythonWrapper self, string name, string moduleName)
        {
            PyThread.Invoke(() =>
            {
                PythonModule mod;
                if (!moduleLookup.TryGetValue(moduleName, out mod))
                {
                    mod = new PythonModule() { Name = moduleName, Folder = Path.GetDirectoryName(name) };
                    moduleLookup[moduleName] = mod;
                    mod.Load();
                }

                if (transverseObj.Contains(name))
                {
                    PythonModule.log.Error(string.Format("Captured infinite loop when loading module: {0}", name));
                    throw new Exception();
                }
                try
                {
                    transverseObj.Add(name);

                    self.PythonObject = mod.CreateInstance(name, self);
                    var wrappedObject = self.PythonObject.As<IValidatingObject>();
                    if(wrappedObject != null)
                        wrappedObject.PropertyChanged += (s, e) => ((IValidatingObject)self).OnPropertyChanged(e.PropertyName);
                    transverseObj.Remove(name);
                }
                catch(PythonException pex)
                {
                    WrapperBuilder.printPythonException(pex);
                }
                catch (Exception)
                {

                }
                finally
                {
                    transverseObj.Remove(name);
                }
            });
        }

        /// <summary>
        /// Restarts the running python instance. Note, this may cause undefined behavior.
        /// </summary>
        internal static void RestartPython()
        {
            Exception exception = null;
            if(PyThread.PyInitialized == false)
            {
                WrapperBuilder.LoadPython(reinit: true);
                return;
            }
            PyThread.Invoke(() =>
            {
                List<IPythonWrapper> wrappers = new List<IPythonWrapper>();
                foreach (var thing in moduleLookup.Values)
                {
                    foreach (var wrapper in thing.elements)
                    {
                        wrappers.Add(wrapper);
                    }
                }
                PythonEngine.Shutdown();
                
                moduleLookup.Clear();
                transverseObj.Clear();
                try
                {
                    WrapperBuilder.ReloadPython();
                }catch(Exception e)
                {
                    exception = e;
                    return;
                }

                foreach (var wrapper in wrappers)
                {
                    if(wrapper != null)
                        PythonWrapper.Load(wrapper);
                }
            });
            if (exception != null)
                throw exception;
        }
    }

    /// <summary> Plugin class for converting to/from python. </summary>
    [Obfuscation(Exclude = true)]
    [Display("Python Value Converter")]
    public interface IPythonConverter : ITapPlugin
    {
        /// <summary>
        /// Creates a python object from an CLR object. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        PyObject ToPythonObject(object value);

        /// <summary>
        /// Creates an CLR object from a python object. Should return UnableToConvert.Instance if it is not able to convert the value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        object FromPythonObject(PyObject value, Type type);

        /// <summary>
        /// Defines the order of use.
        /// </summary>
        double Order { get; }
    }

    /// <summary> This class makes it possible to convert to/from python objects. </summary>
    public static class PythonConverter
    {
        static List<IPythonConverter> converters = new List<IPythonConverter>();

        static List<IPythonConverter> Converters
        {
            get
            {
                var pl = PluginManager.GetPlugins<IPythonConverter>();
                if (pl.Count != converters.Count)
                {
                    converters.Clear();
                    foreach (var type in pl)
                    {
                        var ins = (IPythonConverter)Activator.CreateInstance(type);
                        converters.Add(ins);
                    }
                    converters.Sort((x, y) => x.Order.CompareTo(y.Order));
                }
                return converters;
            }
        }

        /// <summary>
        /// Convert an object to a python object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static PyObject ToPythonObject(object obj)
        {
            PyObject result = null;
            foreach (var conv in Converters)
            {
                result = conv.ToPythonObject(obj);
                if (result != null)
                    break;
            }
            if (result == null)
                result = obj.ToPython();
            return result;
        }

        /// <summary>
        /// Converts a python object to a .NET object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object FromPythonObject(PyObject obj, Type targetType)
        {
            foreach (var ins in Converters)
            {
                var r = ins.FromPythonObject(obj, targetType);
                if (r is UnableToConvert == false)
                    return r;
            }
            
            return obj.AsManagedObject(targetType);
        }
    }
    /// <summary>
    /// Placeholder class used to signal that a IPythonConverter was not able to convert a value.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public class UnableToConvert
    {
        public static readonly UnableToConvert Instance = new UnableToConvert();
    }

    /// <summary> IPythonConverter for enum types. </summary>
    [Obfuscation(Exclude = true)]
    public class EnumPythonConverter : IPythonConverter
    {
        
        public double Order => 0;

        public object FromPythonObject(PyObject at, Type type)
        {
            if (type.IsEnum)
            {
                // enums can have different format depending on their origin.
                // python enums can be seen as strings.
                if (type.IsDefined(typeof(PythonWrapper.PythonNameAttribute), false))
                {
                    var splitted = at.ToString().Split('.');
                    if (splitted.Length > 1)
                        return Enum.Parse(type, splitted[1]);
                    else return at;
                }
                else
                {
                    //to handle enum that is defined outside of Python
                    
                    try
                    {
                        var i = at.AsManagedObject(typeof(int));
                        if (i != null)
                        {
                            return Enum.ToObject(type, (int)i);
                        }   
                    }
                    catch
                    {
                        
                    }
                    return Enum.Parse(type, at.ToString());
                }
            }
            else
            {
                return UnableToConvert.Instance;
            }
        }

        public PyObject ToPythonObject(object value)
        {
            if (value is Enum == false) return null;

            var tp = value.GetType();
            if(tp.IsDefined(typeof(PythonWrapper.PythonNameAttribute), false))
            {
                var pyattr = (PythonWrapper.PythonNameAttribute)tp.GetCustomAttributes(typeof(PythonWrapper.PythonNameAttribute), false)[0];
                var split = pyattr.PythonName.Split('.');
                var modname = string.Join(".", split.Take(split.Length - 1));
                PyObject mod = PythonEngine.ImportModule(modname);
                PyObject tap = PythonEngine.ImportModule("PythonTap");
                var enm = mod.GetAttr(split[split.Length - 1]);
                
                try
                {
                    var pyint = ((int)value).ToPython();
                    return enm.Invoke(pyint);
                }
                catch
                {
                    var members = enm.GetAttr("__members__");
                    var values = tap.InvokeMethod("to_list", members.InvokeMethod("values"));
                    var values2 = new PyList(values);
                    var pyint = ((int)value - 1).ToPython();
                    return values2.GetItem(pyint);
                }
            }
            return value.ToPython();
        }
    }

    /// <summary>
    /// IPythonConverter for python proxies.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public class PythonProxyConverter : IPythonConverter
    {
        public double Order => 1;
        public object FromPythonObject(PyObject value, Type type)
        {
            var proxy = value.AsManagedObject(typeof(object)) as IPythonProxy;
            if (proxy != null)
            {
                // If the object is a IPythonProxy, we need to return the wrapper object
                // and not the object itself.
                return proxy.Wrapper;
            }
            return UnableToConvert.Instance;    
        }

        public PyObject ToPythonObject(object value)
        {
            if (value is IPythonWrapper wrapper)
                return wrapper.PythonObject;
            return null;
        }
    }
    class PythonModule
    {
        public string Name;

        /// <summary> When was the module last reloaded. </summary>
        DateTime lastLoad = DateTime.Now;
        PyObject module;
        public string Folder;
        public HashSet<IPythonWrapper> elements = new HashSet<IPythonWrapper>();
        public void Load()
        {
            module = PythonEngine.ImportModule(Name);
            Folder = Path.GetDirectoryName((string)module.GetAttr("__file__").AsManagedObject(typeof(string)));
            var watcher = new FileSystemWatcher(Folder, "*.py")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.LastAccess | NotifyFilters.CreationTime,
                IncludeSubdirectories = false
            };
            watcher.Changed += ReloadPyOnChange;
        }

        public PyObject CreateInstance(string typename, IPythonWrapper wrapper)
        {
            PyObject tap = PythonEngine.ImportModule("PythonTap");
            var cls = (PyObject)tap.InvokeMethod("GetPlugin", typename.ToPython()).AsManagedObject(typeof(object));
            if (cls == null)
                cls = module.GetAttr(typename);
            PyObject obj = cls.Invoke();
            elements.Add(wrapper);
            return obj;
        }

        /// <summary>
        /// Reloads python code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ReloadPyOnChange(object sender, FileSystemEventArgs e)
        {
            if (false == PythonSettings.Current.EnableCodeReloading)
                return;
            if ((DateTime.Now - lastLoad).TotalMilliseconds < 250)
                return; // throttle edits.
            lastLoad = DateTime.Now;
            try
            {
                var sw = Stopwatch.StartNew();
                PyThread.Invoke(() =>
                {
                    PyObject tap = PythonEngine.ImportModule("PythonTap");
                    tap.InvokeMethod("reload_module", module);
                    module = PythonEngine.ImportModule(Name);
                    foreach (IPythonWrapper elem in elements.ToArray())
                    {
                        try
                        {
                            elem.PythonObject.InvokeMethod("reload");
                        }catch(PythonException pex)
                        {
                            WrapperBuilder.printPythonException(pex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                if (ex is PythonException)
                {
                    if (ex.Message.Contains("SyntaxError"))
                    {
                        log.Error("Syntax error");
                        log.Info(ex.Message);
                        return;
                    }
                }
                log.Error("Error reloading Python script.");
                log.Debug(ex);
            }
        }
        public static OpenTapTraceSource.TraceSource log = Log.CreateSource("Python");
    }

}
