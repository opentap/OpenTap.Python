//  Copyright 2012-2022 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0

using System.Threading;
using OpenTap.Cli;
namespace OpenTap.Python.SDK
{
    [Display("search-path", Group: "python", Description: "A list containing the additional search paths for finding the Python based plugin modules.")]
    public class PythonSearchPath : ICliAction
    {
        [CommandLineArgument("add", Description = "Add additional search path(s) separated by semi-colon.")]
        public string AddSearchPath { get; set; } = "";
        [CommandLineArgument("remove", Description = "Remove additional search path(s) separated by semi-colon.")]
        public string RemoveSearchPath { get; set; } = "";
        [CommandLineArgument("get", Description = "Get the additional search path list.")]
        public bool GetSearchPathList { get; set; } = false;

        TraceSource log = global::OpenTap.Log.CreateSource("CLI");
        public int Execute(CancellationToken cancellationToken)
        {
            return new PluginSearchPath().ReadWritePluginSearchPath(AddSearchPath, RemoveSearchPath, GetSearchPathList);
        }
    }

}
