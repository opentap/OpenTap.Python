using System.IO;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.SDK;

[Display("set-path", Group: "python", Description: "Set the path to the python installation or virtual environment.")]
public class PythonSetPath : ICliAction
{
    [UnnamedCommandLineArgument("Path", Required = true)]
    public string Path { get; set; }

    static readonly TraceSource log = Log.CreateSource("CLI");
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