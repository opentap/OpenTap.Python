using System;
using System.IO;
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
        if (CacheFile != null && File.Exists(CacheFile))
        {
            log.Debug("Cache file exists. Exiting.");
            return 0;
        }

        Directory.CreateDirectory(StubsFolder);
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
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
                        log.Debug($"Cannot generate stubs for {file.FileName }");
                    }
                    
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(CacheFile) && File.Exists(CacheFile) == false)
        {
            File.WriteAllText(CacheFile, "ok");
        }
        return 0;
    }
}
