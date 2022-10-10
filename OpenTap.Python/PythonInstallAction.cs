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

public class PythonPackageBuildAction : ICustomPackageAction
{
    public int Order() => 0;
    private TraceSource log = Log.CreateSource("python");

    public bool Execute(PackageDef package, CustomPackageActionArgs customActionArgs)
    {
        if (PythonInitializer.LoadPython() == false)
            return true;
        using (Py.GIL())
        {
            foreach (var d in package.Files)
            {
                foreach (var cd in d.CustomData.ToArray())
                {
                    // if the file is a ProjectFile
                    if (cd is ProjectFile)
                    {
                        if (d.RelativeDestinationPath.StartsWith(package.Name))
                        {
                            d.SourcePath = d.RelativeDestinationPath;
                            d.RelativeDestinationPath = "Packages/" + d.RelativeDestinationPath;
                        }else if (d.RelativeDestinationPath.Contains("/" + package.Name + "/") == false)
                        {
                            d.SourcePath = d.RelativeDestinationPath;
                            d.RelativeDestinationPath = "Packages/" + package.Name + "/" + d.RelativeDestinationPath;
                        }

                        d.CustomData.Remove(cd);
                    }
                }
            }
        }

        return true;
    }

    public PackageActionStage ActionStage => PackageActionStage.Create;
}