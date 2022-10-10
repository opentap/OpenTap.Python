using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.CliActions;

[Browsable(false)]
[Display(Name: "pack-template-project", Description: "Pack the project templates into a zip file.", Groups: new[] { "python" })]
public class PackTemplateProject : ICliAction
{
    [CommandLineArgument("dir")]
    public string TemplateDir { get; set; }

    [CommandLineArgument("out")] public string OutFile { get; set; } = "OpenTap.Python.ProjectTemplate.zip";

    static readonly string[] acceptedFileExtensions = { "csproj", "py", "md", "cs", "sln" };
    static readonly string[] acceptedFiles = { ".gitversion" };

    static readonly TraceSource log = Log.CreateSource("pack");
    public int Execute(CancellationToken cancellationToken)
    {
        if (TemplateDir == null) throw new ArgumentException("TemplateDir cannot be null", nameof(TemplateDir));
        var dir = Directory.GetCurrentDirectory();
        var outDirName = Path.GetDirectoryName(OutFile);
        if (string.IsNullOrWhiteSpace(outDirName) == false && !Directory.Exists(outDirName))
            Directory.CreateDirectory(outDirName);
        using var fstr = File.OpenWrite(OutFile);
        using var archive = new ZipArchive(fstr, ZipArchiveMode.Create);
        Directory.SetCurrentDirectory(TemplateDir);
        var allFiles = Directory.EnumerateFiles(".", "*", SearchOption.AllDirectories);
            
        var rootFolder = "." + Path.DirectorySeparatorChar;
        var binFolder = Path.Combine(".", "bin") + Path.DirectorySeparatorChar;
        var objFolder =  Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        
        foreach (var file0 in allFiles)
        {
            if (file0.StartsWith(binFolder)) continue;
            if (file0.Contains(objFolder)) continue;
            string file = file0;
            if (file0.StartsWith(rootFolder))
                file = file.Substring(rootFolder.Length);
            var ext = Path.GetExtension(file).TrimStart('.');
            if (file.StartsWith(rootFolder)) continue;
            
            if (acceptedFileExtensions.Contains(ext) == false && acceptedFiles.Contains(Path.GetFileName(file0))) continue;
            log.Debug("Packing: {0}", file);
            using var str = File.OpenRead(file);
            
            var fileEntry = archive.CreateEntry(file);
            using var writestr = fileEntry.Open();
            str.CopyTo(writestr);
        }
        Directory.SetCurrentDirectory(dir);
     
        return 0;
    }
}