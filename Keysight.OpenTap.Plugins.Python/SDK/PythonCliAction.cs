//  Copyright 2012-2019 Keysight Technologies
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using OpenTap;
using OpenTap.Cli;
namespace Keysight.OpenTap.Plugins.Python.SDK
{
    [Display("build", Group: "python", Description: "Build a python plugin")]
    public class PythonCliAction : ICliAction
    {
        [UnnamedCommandLineArgument("ModuleName", Required = true)]
        public string ModuleName { get; set; } = "";
        [CommandLineArgument("build-package", ShortName = "b", Description = "Build a Package for the Python module." )]
        public bool BuildTapPlugin { get; set; } = false;   
        [CommandLineArgument("include-pyc", ShortName = "p", Description = "Include .pyc files in the Python module instead of .py files. This option can only be used with build-package")]
        public bool IncludePyc { get; set; } = false;
        [CommandLineArgument("dump-package-xml", ShortName = "x", Description = "Create/update a package.xml file in the module folder.")]
        public string DumpPackageXml { get; set; } = "";
        [CommandLineArgument("replace-package-xml", ShortName = "r", Description = "Replaces the package XML in the module folder.")]
        public bool ReplacePackageXml { get; set; } = false;

        public int Execute(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ModuleName))
            {
                Log.CreateSource("CLI").Error("Module name cannot be empty.");
                return 1;
            }

            // It is important that the module we are building is not loaded if it was already there.
            PluginManager.AddAssemblyLoadFilter((x, v) => (x != "Python." + ModuleName) && (x != ModuleName));
            TapThread.Start(WrapperBuilder.RoslynWarmup); // Saves ~1s compilation time.
            return (int) Builder.doBuild(ModuleName, IncludePyc, BuildTapPlugin, ReplacePackageXml, DumpPackageXml);
        }
    }

    [Display("set-path", Group: "python", Description: "Set the path to the python installation or virtual environment.")]
    public class PythonSetPath : ICliAction
    {
        [UnnamedCommandLineArgument("Path", Required = true)]
        public string Path { get; set; }

        TraceSource log = global::OpenTap.Log.CreateSource("CLI");
        public int Execute(CancellationToken cancellationToken)
        {
            if(Directory.Exists(Path) == false)
            {
                log.Error("The directory '{0}' does not exist.\n", Path);
                return 1;
            }
            PythonSettings.Current.PythonPath = System.IO.Path.GetFullPath(Path);
            PythonSettings.Current.Save();
            log.Info("Set Python path to '{0}'", PythonSettings.Current.PythonPath);
            return 0;
        }
    }

    [Display("get-path", Group: "python", Description: "Get the path to the Python library. (Windows only)")]
    public class PythonGetPath : ICliAction
    {
        readonly TraceSource log = Log.CreateSource("CLI");

        public int Execute(CancellationToken cancellationToken)
        {
            string path;
            if (PyThread.IsWin32)
            {
                path = PythonSettings.Current.PythonPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    log.Error("Python library path is empty. Please refer to `tap python set-path` for details.");
                    return 1;
                }
            }
            else
            {
                log.Error("This option is only supported on Windows.");
                return 1;
            }

            log.Info("Python library path - {0}", path);
            return 0;
        }
    }

    [Display("set-version", Group: "python", Description: "Set the Python version to use. E.g 'tap python set-version 2.7' (Linux only)")]
    public class PythonSetVersion : ICliAction
    {
        [UnnamedCommandLineArgument("Version")]
        public string Version { get; set; }

        TraceSource log = global::OpenTap.Log.CreateSource("CLI");
        public int Execute(CancellationToken cancellationToken)
        {
            string trimmedVersion = "";
            if (PyThread.IsWin32)
            {
                log.Error("This option is only supported on linux.");
                return 1;
            }
            else if (!string.IsNullOrWhiteSpace(Version))
            {
                trimmedVersion = Version.Trim();
                Regex rx = new Regex(@"(^2\.7$)|(^3\.[6-8]{1}$)");
                if (!rx.IsMatch(trimmedVersion))
                {
                    log.Error("Only Python version 2.7, 3.6, 3.7 and 3.8 are supported.");
                    return 1;
                }
            }
            PythonSettings.Current.PythonVersion = trimmedVersion;
            PythonSettings.Current.Save();
            log.Info("Set Python version to '{0}'", PythonSettings.Current.PythonVersion);
            return 0;
        }
    }

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
