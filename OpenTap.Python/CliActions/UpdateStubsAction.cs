using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using OpenTap.Package;
namespace OpenTap.Python.SDK;

[Display("update-stubs", "", "python")]
public class UpdateStubsAction : ICliAction
{
    static readonly TraceSource log = Log.CreateSource("python");
    [CommandLineArgument("output-folder")]
    public string StubsFolder { get; set; }

    [CommandLineArgument("cache-file", Description = "If this file exists the update action will be skipped.")]
    public string CacheFile { get; set; }

    public int Execute(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(StubsFolder))
            throw new ExitCodeException(10, "output-folder must be set.");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.IsDynamic == false)
            .OrderBy(x => x.FullName)
            .ToArray();
        var packages = Installation.Current.GetPackages();
        var cacheKey = string.Join(",", assemblies.Select(x => x.FullName)) + "|" + string.Join(",", packages.OrderBy(x => x.Name).Select(x => x.Name + x.Version));

        if (CacheFile != null && File.Exists(CacheFile))
        {
            if (string.Equals(File.ReadAllText(CacheFile), cacheKey))
            {
                log.Debug("Cache file exists. Exiting.");
                return 0;
            }

            File.Delete(CacheFile);
        }

        Directory.CreateDirectory(StubsFolder);
        foreach (var item in assemblies)
        {
            if (item.IsDynamic) continue;
            try
            {
                Stubs.StubBuilder.BuildAssemblyStubs(item.Location, StubsFolder);
            }
            catch
            {
                log.Debug($"Cannot generate stubs for {item.Location}");
            }
        }
        foreach (var package in Installation.Current.GetPackages())
        {
            foreach (var file in package.Files)
            {
                if (file.FileName.EndsWith(".dll"))
                {
                    try
                    {
                        Stubs.StubBuilder.BuildAssemblyStubs(file.FileName, StubsFolder);
                    }
                    catch
                    {
                        log.Debug($"Cannot generate stubs for {file.FileName}");
                    }

                }
            }
        }
        if (!string.IsNullOrWhiteSpace(CacheFile) && File.Exists(CacheFile) == false)
        {
            File.WriteAllText(CacheFile, cacheKey);
        }
        return 0;
    }
}
