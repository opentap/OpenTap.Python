using System;
using OpenTap.Package;
using Python.Runtime;

namespace OpenTap.Python;

public class PythonInstallAction : ICustomPackageAction
{
    public int Order() => 0;
    private TraceSource log = Log.CreateSource("python");

    public bool Execute(PackageDef package, CustomPackageActionArgs customActionArgs)
    {
        if (PythonInitializer.LoadPython() == false)
            return true;
        using (Py.GIL())
        {
            var opentap = Py.Import("opentap");

            foreach (var d in package.Files)
            {
                foreach (var cd in d.CustomData)
                {
                    if (cd is PythonRequirements)
                    {
                        log.Info("Installing requirements from {0}", d.FileName);
                        opentap.InvokeMethod("install_package", d.FileName.ToPython());
                    }
                }
            }
        }

        return true;
    }

    public PackageActionStage ActionStage => PackageActionStage.Install;
}