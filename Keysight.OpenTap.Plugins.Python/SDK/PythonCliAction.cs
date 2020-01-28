using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [CommandLineArgument("dump-package-xml", ShortName = "x", Description = "Create a package.xml file in the module folder.")]
        public string DumpPackageXml { get; set; } = "";
        [CommandLineArgument("replace-package-xml", Description = "Replaces the package XML in the module folder.", ShortName = "x")]
        public bool ReplacePackageXml { get; set; } = false;

        public int Execute(CancellationToken cancellationToken)
        {
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
                log.Warning("Warning: The directory '{0}' does not exist.\n", Path);
            }
            PythonSettings.Current.PythonPath = System.IO.Path.GetFullPath(Path);
            PythonSettings.Current.Save();
            log.Info("Set Python path to '{0}'", PythonSettings.Current.PythonPath);
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
            if (PyThread.IsWin32)
            {
                log.Error("This option is only supported on linux.");
                return 1;
            }
            if((Version == "2.7" || Version == "3.6" || Version == "3.7" || string.IsNullOrWhiteSpace(Version)) == false)
            {
                log.Error("Only Python version 2.7, 3.6 and 3.7 are supported.");
                return 1;
            }
            PythonSettings.Current.PythonVersion = Version;
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
