using System;
using System.IO;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.SDK;

[Display("set-lib-path", Group: "python",
    Description: "Set the path to the python installation or virtual environment.")]
public class PythonSetLibPath : ICliAction
{
    [UnnamedCommandLineArgument("libpath")]
    public string LibPath { get; set; }

    readonly TraceSource log = Log.CreateSource("python");
    public int Execute(CancellationToken cancellationToken)
    {
        if (!File.Exists(LibPath))
            throw new ExitCodeException(1, "Selected path does exist");
        log.Info("Setting the library path to: {0}", LibPath);
        PythonSettings.Current.PythonLibraryPath = LibPath;
        PythonSettings.Current.Save();
        return 0;
    }
}