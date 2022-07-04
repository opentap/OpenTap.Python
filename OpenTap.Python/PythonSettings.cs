//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenTap.Python
{
    /// <summary>
    /// Provides capability to load Python code into step, build step as PythonStep, and build C# DLL wrapper code.
    /// </summary>
    [Display("Python", "Settings for the Python plugin.")] 
    [HelpLink("https://opentap.gitlab.io/Plugins/python/")]
    [Obfuscation(Exclude = true)]
    public class PythonSettings : ComponentSettings<PythonSettings>
    {

        /// <summary>
        /// Makes it possible to configure a custom path to a python installation.
        /// </summary>
        [Display("Python Path", "Enables a custom path to the Python installation. After configuration, TAP should be restarted for the effect to take place. If set, this overrides your PYTHONHOME and PYTHONPATH environment variables.", Order: 0)]
        [DirectoryPath]
        public string PythonPath { get; set; }

        public IEnumerable<string> AvailableLibraries => PythonDiscoverer.Instance.AvailablePythonLibraries;

        [Display("Python Library Path", "" +
                                        "Enables a custom path to the Python installation. " +
                                        "After configuration, TAP should be restarted for the effect to take place.",
            Order: 0)]
        [FilePath]
        [SuggestedValues(nameof(AvailableLibraries))]
        public string PythonLibraryPath {get; set; }

        [Display("Plugin Module Search Path", "A list containing additional search paths for finding the Python based plugin modules.", Order: 1)]
        public List<PluginSearchPath> SearchPathList { get; set; } = new ();

        public string[] GetSearchList()
        {
            var lst = new List<string>();
            var dir = Path.GetDirectoryName(GetType().Assembly.Location);
            lst.Add(dir);
            if(File.Exists(Path.Combine(dir, "OpenTap.dll")))
            {
                var dir2 = Path.Combine(dir, "Packages", "Python");
                if (Directory.Exists(dir2))
                    lst.Add(dir2);
            }

            foreach (var path in SearchPathList)
            {
                if (path.Enabled && Directory.Exists(path.SearchPath))
                    lst.Add(path.SearchPath);
            }

            return lst.ToArray();
        }

        public string DefaultSearchPath => Path.GetDirectoryName(typeof(PythonSettings).Assembly.Location);

        public IEnumerable<string> GetSearchPaths() =>
            SearchPathList.Where(x => x.Enabled).Select(x => x.SearchPath).Append(DefaultSearchPath);
        
        [Display("Enable", "Whether to enable the debugging server inside the python interpreter. This can cause instabilities on some platforms. Not verified to work on MacOS.", "Debug")]
        public bool Debug { get; set; }
        
        public PythonSettings()
        {
            Rules.Add(() => !SearchPathList.Exists(x => !string.IsNullOrEmpty(x.Error)), "Search path error(s) is found.", nameof(SearchPathList));
        }
    }
}
