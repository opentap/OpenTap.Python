using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.SDK;

[Display("get-path", Group: "python", Description: "Get the path to the Python library. (Windows only)")]
public class PythonGetPath : ICliAction
{
    readonly TraceSource log = Log.CreateSource("CLI");

    public int Execute(CancellationToken cancellationToken)
    {
        string path = PythonSettings.Current.PythonPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            log.Error("Python library path is empty. Please refer to `tap python set-path` for details.");
            return 1;
        }

        log.Info("Python library path - {0}", path);
        return 0;
    }
}