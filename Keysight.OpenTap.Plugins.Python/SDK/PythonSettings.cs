//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenTap;

namespace Keysight.OpenTap.Plugins.Python
{
    /// <summary>
    /// Provides capability to load Python code into step, build step as PythonStep, and build C# DLL wrapper code.
    /// </summary>
    [Display("Python", "Settings for the Python plugin.")]
    [HelpLink("Packages/Python/TapPythonPluginHelp.chm")]
    [Obfuscation(Exclude = true)]
    public class PythonSettings : ComponentSettings<PythonSettings>
    {
        /// <summary>
        /// Gets or sets whether code reloading is enabled.
        /// </summary>
        [Display("Enable Code Reloading", "Enables runtime code reloading. If enabled, the code of a Python module will be re-loaded when it changes on the disk. (Experimental)", "Debug", Order: 2)]
        public bool EnableCodeReloading { get;set; }

        [Display("Debug Threading Mode", "Enabled debug threading mode, which is more unstable but allows for easier debugging")]
        [Browsable(false)]
        public bool DebugThreadingMode { get; set; } = true;

        string firstPath;
        string currentPath;
        /// <summary>
        /// Makes it possible to configure a custom path to a python installation.
        /// </summary>
        [Display("Python Path", "Enables a custom path to the Python installation. After configuration, TAP should be restarted for the effect to take place.", Order: 0)]
        [DirectoryPath]
        [SuggestedValues(nameof(PyFolders))]
        public string PythonPath
        {
            get => currentPath;
            set
            {
                if (string.IsNullOrWhiteSpace(firstPath) == false)
                {
                    firstPath = value;
                }
                currentPath = value;
            }
        }

        List<PluginSearchPath> searchPathList = new List<PluginSearchPath>();

        [Display("Plugin Module Search Path", "A list containing additional search paths for finding the Python based plugin modules.", Order: 1)]
        public List<PluginSearchPath> SearchPathList
        {
            get
            {
                ModifyPluginManagerDir(searchPathList);
                return searchPathList;
            }
            set
            {
                searchPathList = value;
                ModifyPluginManagerDir(searchPathList);
                LoadedPluginManagerDir = new List<string>(PluginManager.DirectoriesToSearch);
            }
        }

        public string DefaultSearchPath => Path.GetDirectoryName(typeof(PythonSettings).Assembly.Location);

        public IEnumerable<string> GetSearchPaths() =>
            SearchPathList.Where(x => x.Enabled).Select(x => x.SearchPath).Append(DefaultSearchPath);

        public List<string> LoadedPluginManagerDir { get; private set; } = new List<string> { Environment.GetEnvironmentVariable("TAP_PATH") };

        private void ModifyPluginManagerDir(List<PluginSearchPath> PluginSearchPath)
        {
            try
            {
                var PluginManagerDirectories = PluginManager.DirectoriesToSearch;
                var pluginManagerDirToCompare = PluginManagerDirectories.FindAll(x => x != Path.GetDirectoryName(typeof(ComponentSettings).Assembly.Location));
                pluginManagerDirToCompare.Sort();
                var pluginSearchPathToCompare = PluginSearchPath.FindAll(x => x.Enabled == true && Directory.Exists(x.SearchPath) && x.ValidateDirSize()).Select(y => y.SearchPath).ToList();
                pluginSearchPathToCompare.Sort();
                if (pluginManagerDirToCompare.SequenceEqual(pluginSearchPathToCompare))
                    return;

                var dirToBeRemoved = PluginManagerDirectories.Where(x => x != Path.GetDirectoryName(typeof(ComponentSettings).Assembly.Location) && (!PluginSearchPath.Exists(y => y.SearchPath == x) || !PluginSearchPath.Find(z => z.SearchPath == x).Enabled)).ToList();
                dirToBeRemoved.ForEach(x => PluginManagerDirectories.Remove(x));
                var dirToBeAdded = PluginSearchPath.Where(x => x.Enabled && Directory.Exists(x.SearchPath) && !PluginManagerDirectories.Contains(x.SearchPath) && x.ValidateDirSize()).ToList();
                dirToBeAdded.ForEach(x => PluginManagerDirectories.Add(x.SearchPath));
                PluginManager.SearchAsync();
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        public static string LoadedPath { get; set; }
        [Browsable(false)]
        [Display("Python Version")]
        public string PythonVersion { get; set; }

        /// <summary> True if PythonNet should be deployed when started. </summary>
        [Browsable(false)]
        public bool DeployPythonNet { get; set; } = true;

        TraceSource log = global::OpenTap.Log.CreateSource("Python");

        [Browsable(false)]
        [Display("Restart Python", Description: "Restarts the embedded Python instance.", Group: "Debug", Order: 2)]
        public void RestartPython()
        {
            try
            {
                Keysight.Plugins.Python.PythonWrapperExtensions.RestartPython();
            }
            catch(Exception e)
            {
                log.Error("Unable to restart python");
                log.Debug(e);
            }
        }

        [Browsable(true)]
        [Layout(LayoutMode.FullRow, rowHeight: 2)]
        [Display("A note on reloading", Group:"Debug", Order:1)]
        public string ReloadingNote { get; private set; } = "Dynamic code reloading can cause some instabilities. Not all code supports dynamic reloading. "
            + "This is meant as a convenience feature for Python development. In case of errors consider restarting the TAP process.";

        static IEnumerable<string> getPythonsInFolder(string basepath)
        {
            if (!Directory.Exists(basepath))
            {
                return Enumerable.Empty<string>();
            }
            List<string> pys = new List<string>();
            foreach(var dir in Directory.GetDirectories(basepath))
            {
                if (Path.GetFileName(dir).StartsWith("Python", StringComparison.CurrentCultureIgnoreCase))
                {
                    pys.Add(dir);
                }
            }
            return pys;

        }

        static IEnumerable<string> LocatePythons()
        {
            if (PyThread.IsWin32)
            {
                var drives = Directory.GetLogicalDrives();
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                var programFiles2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                var programFiles3 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var programFiles4 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFiles5 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python");

                return drives.Concat(new[] { programFiles, programFiles2, programFiles3, programFiles4, programFiles5 }).SelectMany(getPythonsInFolder).Distinct().ToArray();
            }
            else
            {
                return new[] { "/" };
            }
        }

        public IEnumerable<string> PyFolders => LocatePythons();

        public bool IsLoadedDirUnchange()
        {
            LoadedPluginManagerDir.Sort();
            PluginManager.DirectoriesToSearch.Sort();
            return LoadedPluginManagerDir.SequenceEqual(PluginManager.DirectoriesToSearch);
        }

        public PythonSettings()
        {
            PythonPath = Environment.GetEnvironmentVariable("PYTHONHOME") ?? LocatePythons().FirstOrDefault();
            Rules.Add(() => string.IsNullOrWhiteSpace(LoadedPath) || LoadedPath == currentPath, "This change won't take effect until OpenTAP is restarted.", nameof(PythonPath));
            Rules.Add(() => IsLoadedDirUnchange(), "This change won't take effect until OpenTAP is restarted.", nameof(SearchPathList));
            Rules.Add(() => !SearchPathList.Exists(x => !string.IsNullOrEmpty(x.Error)), "Search path error(s) is found.", nameof(SearchPathList));
        }
    }

    public class PluginSearchPath : ValidatingObject
    {
        string searchPath;

        [Display("Search Path", "A search path for finding the Python based plugin modules.", Order:1)]
        [DirectoryPath]
        public string SearchPath
        {
            get => searchPath;
            set => searchPath = Path.GetFullPath(value);
        }

        [Display("Enabled", "Enable or disable the search path.", Order:0)]
        public bool Enabled { get; set; } = true;

        public PluginSearchPath()
        {
            Rules.Add(() => PythonSettings.Current.SearchPathList.Where(x => string.Compare(x.SearchPath, SearchPath) == 0).ToList().Count <= 1, "This search path duplicates in the list.", nameof(SearchPath));
            Rules.Add(() =>
            {
                if(!string.IsNullOrEmpty(SearchPath))
                    return Directory.Exists(Path.GetFullPath(SearchPath));
                else
                    return false;
            }, "This search path does not exist.", nameof(SearchPath));

            Rules.Add(() => ValidateDirSize(), "This directory is too large. Maximum size: 100mb", nameof(SearchPath));
        }

        private bool dirSize(DirectoryInfo dirInfo, ref long size)
        {
            // add file sizes of the current dir
            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo fi in files)
            {
                size += fi.Length;
                if (size > 100000000) // if the total size is above 100mb, we stop searching and return false.
                    return false;
            }
            // add subdirectory sizes
            DirectoryInfo[] subDirs = dirInfo.GetDirectories();
            foreach (DirectoryInfo di in subDirs)
            {
                bool result = dirSize(di, ref size);
                if (!result)
                    return false;
            }
            return true;
        }

        public bool ValidateDirSize()
        {
            // check the existence of the search path first
            if (!string.IsNullOrWhiteSpace(searchPath) && Directory.Exists(Path.GetFullPath(searchPath)))
            {
                long totalSize = 0;
                DirectoryInfo dirInfo = new DirectoryInfo(searchPath);
                return dirSize(dirInfo, ref totalSize);
            }
            else
                return true;
        }

        TraceSource log = global::OpenTap.Log.CreateSource("Python");

        public int ReadWritePluginSearchPath(string addSearchPath, string remSearchPath, bool getList)
        {
            var currentPluginSearchPath = PythonSettings.Current.SearchPathList;

            if (getList)
            {
                if (currentPluginSearchPath.Count == 0)
                    log.Info("The additional search path list is empty.\n");
                else
                {
                    currentPluginSearchPath.ForEach(x =>
                    {
                        string status = x.Enabled ? "Enabled" : "Disabled";
                        log.Info($"Path: '{x.SearchPath}', Status: {status}\n");
                    });
                }
            }

            if (!string.IsNullOrEmpty(addSearchPath.Trim()))
            {
                var searchPaths = new List<string>(addSearchPath.Split(';')).Select(x => x.Trim()).ToList();
                searchPaths.RemoveAll(x => string.IsNullOrEmpty(x));
                foreach (var path in searchPaths)
                {
                    string fullSearchPath = Path.GetFullPath(path);
                    if (!Directory.Exists(fullSearchPath))
                    {
                        log.Warning("Warning: The directory '{0}' does not exist.\n", fullSearchPath);
                    }
                    else if (currentPluginSearchPath.Exists(x => string.Compare(x.SearchPath, fullSearchPath) == 0))
                        log.Warning("Warning: '{0}' exists in the additional search path list.\n", fullSearchPath);
                    else
                    {
                        currentPluginSearchPath.Add(new PluginSearchPath() { SearchPath = fullSearchPath });
                        log.Info("Added' {0}' to the additional search path list.\n", fullSearchPath);
                    }
                }
            }

            if (!string.IsNullOrEmpty(remSearchPath.Trim()))
            {
                var searchPaths = new List<string>(remSearchPath.Split(';')).Select(x => x.Trim()).ToList();
                searchPaths.RemoveAll(x => string.IsNullOrEmpty(x));
                foreach (var searchPath in searchPaths)
                {
                    string fullSearchPath = Path.GetFullPath(searchPath);
                    if (!Directory.Exists(fullSearchPath))
                    {
                        log.Warning("Warning: The directory '{0}' does not exist.\n", fullSearchPath);
                    }
                    else if (currentPluginSearchPath.Remove(currentPluginSearchPath.Find(x => string.Compare(x.SearchPath, fullSearchPath) == 0)))
                        log.Info("Removed '{0}' from the additional search path list.\n", fullSearchPath);
                    else
                    {
                        log.Warning("Warning: The directory '{0}' is not in the additional search path list.\n", fullSearchPath);
                    }
                }
            }
            PythonSettings.Current.Save();
            return 0;
        }
    }
}
