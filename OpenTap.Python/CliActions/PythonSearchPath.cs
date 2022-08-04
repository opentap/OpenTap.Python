//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.SDK
{
    [Display("search-path", Group: "python",
        Description: "A list containing the additional search paths for finding the Python based plugin modules.")]
    public class PythonSearchPath : ICliAction
    {
        [CommandLineArgument("add", Description = "Add additional search path(s) separated by semi-colon.")]
        public string[] AddSearchPath { get; set; } = Array.Empty<string>();

        [CommandLineArgument("remove", Description = "Remove additional search path(s) separated by semi-colon.")]
        public string[] RemoveSearchPath { get; set; } = Array.Empty<string>();

        [CommandLineArgument("get", Description = "Get the additional search path list.")]
        public bool GetSearchPathList { get; set; } = false;

        [CommandLineArgument("clear", Description = "Clears all search paths.")]
        public bool Clear { get; set; }

        static readonly TraceSource log = Log.CreateSource("CLI");

        public int Execute(CancellationToken cancellationToken)
        {
            var currentPluginSearchPath = PythonSettings.Current.SearchPathList;
            if (Clear)
            {
                log.Info("Clearing search paths.");
                RemoveSearchPath = RemoveSearchPath.Concat(currentPluginSearchPath.Select(x => x.SearchPath)).ToArray();
            }
            foreach (var searchPath in RemoveSearchPath)
            {
                string fullSearchPath = Path.GetFullPath(searchPath);
                if (!Directory.Exists(fullSearchPath))
                {
                    log.Warning("Warning: The directory '{0}' does not exist.", fullSearchPath);
                }
                else if (currentPluginSearchPath.Remove(
                             currentPluginSearchPath.Find(x => string.Compare(x.SearchPath, fullSearchPath) == 0)))
                    log.Info("Removed '{0}' from the additional search path list.", fullSearchPath);
                else
                {
                    log.Warning("Warning: The directory '{0}' is not in the additional search path list.",
                        fullSearchPath);
                }
            }
            foreach (var path in AddSearchPath)
            {
                string fullSearchPath = Path.GetFullPath(path);
                if (!Directory.Exists(fullSearchPath))
                {
                    log.Warning("Warning: The directory '{0}' does not exist.", fullSearchPath);
                }
                else if (currentPluginSearchPath.Exists(x => string.Compare(x.SearchPath, fullSearchPath) == 0))
                    log.Warning("Warning: '{0}' exists in the additional search path list.", fullSearchPath);
                else
                {
                    currentPluginSearchPath.Add(new PluginSearchPath() {SearchPath = fullSearchPath});
                    log.Info("Added' {0}' to the additional search path list.", fullSearchPath);
                }
            }
            


            if (GetSearchPathList)
            {
                if (currentPluginSearchPath.Count == 0)
                    log.Info("The additional search path list is empty.");
                else
                {
                    currentPluginSearchPath.ForEach(x =>
                    {
                        string status = x.Enabled ? "Enabled" : "Disabled";
                        log.Info($"Path: '{x.SearchPath}', Status: {status}");
                    });
                }
            }

            PythonSettings.Current.Save();
            return 0;
        }
    }
}