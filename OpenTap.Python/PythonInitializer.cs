﻿//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using Python.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenTapTraceSource = OpenTap;

namespace OpenTap.Python
{
    static class PythonInitializer
    {
        const string loadScript = @"import sys
def add_dir(x):
    sys.path.insert(0, x)
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
                InitInternal();
                initSuccess = true;
                return true;
            }
        }

        /// <summary>
        /// init_internal refers to Python.Runtime, but to find this we need to help the assembly resolver by adding site-packages to DirectoriesToSearch.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void InitInternal()
        {
            if (!PythonEngine.IsInitialized)
            {
                try
                {
                    var installations = PythonDiscoverer.Instance.GetAvailablePythonInstallations()
                        .ToArray();
                    if (!installations.Any())
                    {
                        log.Warning("No python installations found.");
                        return;
                    }

                    var (pyLoc, pyPath) = installations.First();

                    if (File.Exists(pyLoc) == false)
                    {
                        log.Warning($"Unable to load Python.net: File does not exist " + pyLoc);
                        return;
                    }

                    Runtime.PythonDLL = pyLoc;

                    PythonEngine.Initialize(false);
                    PythonEngine.BeginAllowThreads();
                    log.Debug($"Loaded PythonNet for Python Version {PythonEngine.Version}");
                }
                catch (Exception ex)
                {
                    log.Error("Unable to load python: " + ex.Message);
                    log.Debug(ex);
                    return;
                }

                if (!PythonEngine.IsInitialized)
                    return;

                using (Py.GIL())
                {
                    PyObject mod = PyModule.FromString("init_mod", loadScript);
                    foreach (var s in PythonSettings.Current.GetSearchList())
                        mod.InvokeMethod("add_dir", s.ToPython());

                    try
                    {
                        Py.Import("opentap");
                    }
                    catch (PythonException e)
                    {
                        PrintPythonException(e);
                    }
                }
            }
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