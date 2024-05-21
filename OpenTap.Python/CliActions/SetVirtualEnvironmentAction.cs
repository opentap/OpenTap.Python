using System.IO;
using System.Threading;
using OpenTap.Cli;
namespace OpenTap.Python.SDK;

[Display("set-virtual-environment", Group: "python", Description: "Set the virtual environment setting.")]
public class SetVirtualEnvironmentAction : ICliAction
{

    [UnnamedCommandLineArgument("VirtualEnvironment")]
    public string VirtualEnvironment { get; set; }

    [CommandLineArgument("unset", Description = "Unset the virtual environment.")]
    public bool Unset { get; set; }
    static readonly TraceSource log = Log.CreateSource("python");
    public int Execute(CancellationToken cancellationToken)
    {
        if (Unset)
        {
            VirtualEnvironment = "";
        }
        else
        {
            VirtualEnvironment = Path.GetFullPath(VirtualEnvironment);
        }
        log.Info($"Setting virtual environment to '{VirtualEnvironment}'");
        PythonSettings.Current.VirtualEnvironment = VirtualEnvironment;
        PythonSettings.Current.Save();
        return 0;
    }
}