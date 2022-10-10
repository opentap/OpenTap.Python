using OpenTap.Package;
using Python.Runtime;

namespace OpenTap.Python;

/// <summary>
/// This class takes care of moving 'ProjectFiles' to the right folder inside the package file.
/// It looks for a file marked with ProjectFile and moves it  
/// </summary>
public class PythonPackageBuildAction : ICustomPackageAction
{
    public int Order() => 0;

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