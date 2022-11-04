using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python;

[Display("install-requirements", "Install package requirements defined by installed packages", "python")]
public class PythonInstallRequirementsCliAction : ICliAction
{
    public int Execute(CancellationToken cancellationToken)
    {
        var i = new PythonInstallAction();
        foreach (var pkg in Package.Installation.Current.GetPackages())
        {
            i.Execute(pkg, null);
        }

        return 0;
    }
}