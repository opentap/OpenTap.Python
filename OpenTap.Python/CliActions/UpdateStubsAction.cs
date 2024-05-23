using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using OpenTap.Package;
using OpenTap.Python.Stubs;
namespace OpenTap.Python.SDK;

[Display("build-stubs", "", "python")]
[Browsable(false)]
public class UpdateStubsAction : ICliAction
{
    static readonly TraceSource log = Log.CreateSource("python");
    [CommandLineArgument("output-folder")]
    public string OutputFolder { get; set; }

    [CommandLineArgument("cache-file", Description = "If this file exists the update action will be skipped.")]
    public string CacheFile { get; set; }

    public int Execute(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(OutputFolder))
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

        Directory.CreateDirectory(OutputFolder);


        var stubBuilder = new StubBuilder();

        stubBuilder.AddAssembly(typeof(Int32).Assembly);
        foreach (var asm in assemblies)
        {
            if (asm.IsDynamic) continue;
            
            stubBuilder.AddAssembly(asm);
        }
        foreach (var package in packages)
        {
            foreach (var file in package.Files)
            {
                if (file.FileName.EndsWith(".dll"))
                {
                    try
                    {
                        stubBuilder.AddAssembly(file.FileName);
                    }
                    catch(Exception e)
                    {
                        log.Debug($"Cannot generate stubs for {file.FileName}. Error: {0}", e.Message);
                        log.Debug(e);
                    }
                }
            }
        }
        stubBuilder.BuildAssemblyStubs(OutputFolder);
        if (!string.IsNullOrWhiteSpace(CacheFile) && File.Exists(CacheFile) == false)
        {
            File.WriteAllText(CacheFile, cacheKey);
        }
        return 0;
    }
}
