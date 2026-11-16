//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using Python.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenTap.Python
{
    static class PythonInitializer
    {
        const string loadScript = @"import sys
def add_dir(x):
    sys.path.insert(0, x)

def add_venv_site(venv):
    import os
    import site
    if os.name == 'nt':
        packages = os.path.join(venv, 'Lib', 'site-packages')
    else:
        packages = os.path.join(venv, 'lib', 'python%d.%d' % sys.version_info[:2], 'site-packages')
    if not os.path.isdir(packages):
        raise RuntimeError('Virtual environment site-packages not found: ' + packages)
    # addsitedir handles .pth files; prefer venv paths over base packages.
    before = list(sys.path)
    site.addsitedir(packages)
    added = [packages] + [p for p in sys.path if p not in before and p != packages]
    sys.path[:] = added + [p for p in sys.path if p not in added]
";

        static readonly TraceSource log = Log.CreateSource("Python");

        static bool initialized;
        static bool initSuccess;
        static readonly object loadLock = new object();

        public static bool LoadPython(bool reinit = false)
        {
            lock (loadLock)
            {
                if (initialized && !reinit) return initSuccess;
                initialized = true;
                initSuccess = false;
                if (!InitInternal())
                    return false;
                initSuccess = true;
                return true;
            }
        }

        /// <summary>
        /// init_internal refers to Python.Runtime, but to find this we need to help the assembly resolver by adding site-packages to DirectoriesToSearch.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool InitInternal()
        {
            if (!PythonEngine.IsInitialized)
            {
                try
                {
                    var installations = PythonDiscoverer.Instance.GetAvailablePythonInstallations()
                        .ToArray();
                    if (!installations.Any())
                    {
                        log.Warning("No Python installations found.");
                        return false;
                    }

                    var (pyLoc, pyPath) = installations.First();

                    if (File.Exists(pyLoc) == false)
                    {
                        log.Warning($"Unable to load Python: File does not exist " + pyLoc);
                        return false;
                    }
                    
                    Runtime.PythonDLL = pyLoc;
                    
                    // In some cases the python home is not known.
                    // if som try to get it from pyPath.
                    // only done on windows, because it is less ambiguous on Linux when 
                    // python is installed with a package.
                    if(pyPath != null && SharedLib.IsWin32)
                        PythonEngine.PythonHome = pyPath;
                    PythonEngine.ProgramName = Assembly.GetEntryAssembly().Location;
                    PythonEngine.Initialize(false);
                    
                    log.Debug("PYTHONPATH: " + Environment.GetEnvironmentVariable("PYTHONPATH"));
                    log.Debug("PYTHONHOME: " + Environment.GetEnvironmentVariable("PYTHONHOME"));
                    log.Debug($"PythonEngine.PythonPath: {PythonEngine.PythonPath}");
                    log.Debug($"PythonEngine.PythonPath: {PythonEngine.PythonHome}");
                    PythonEngine.BeginAllowThreads();
                    log.Debug($"Loaded Python Version {PythonEngine.Version} from '{pyPath}'.");
                }
                catch (Exception ex)
                {
                    log.Error("Unable to load python: " + ex.Message);
                    log.Debug(ex);
                    return false;
                }

                if (!PythonEngine.IsInitialized)
                    return false;

                using (Py.GIL())
                {
                    PyObject mod = PyModule.FromString("init_mod", loadScript);
                    var venv = PythonSettings.Current.VirtualEnvironment;
                    if (!string.IsNullOrWhiteSpace(venv))
                    {
                        try
                        {
                            mod.InvokeMethod("add_venv_site", Path.GetFullPath(venv).ToPython());
                        }
                        catch (PythonException e)
                        {
                            log.Error("Unable to configure virtual environment: " + e.Message);
                            return false;
                        }
                    }
                    foreach (var s in PythonSettings.Current.GetSearchList())
                        mod.InvokeMethod("add_dir", s.ToPython());

                    try
                    {
                        Py.Import("opentap");
                    }
                    catch (PythonException e)
                    {
                        PrintPythonException(e);
                        log.Error("Unable to initialize OpenTAP.");
                                
                        return false;
                    }
                }
            }

            return true;
        }

        static void PrintPythonException(PythonException pyx)
        {
            log.Error(pyx.Message);
            var trace = pyx.StackTrace;
            if (trace.Contains(']'))
                trace = trace.Substring(0, trace.IndexOf(']'));
            var stk = trace.Split(new string[] {"\\n"}, StringSplitOptions.RemoveEmptyEntries).Reverse();
            foreach (var line in stk)
            {
                var cleaned = line.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
                cleaned = cleaned.TrimStart(',').Trim();
                log.Info(cleaned);
            }
        }
    }
}