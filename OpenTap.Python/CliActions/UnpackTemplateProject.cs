using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.CliActions
{
    [Display(Name: "new-project", Description: "Pack the project templates into a zip file.",
        Groups: new[] { "python" })]
    public class UnpackTemplateProject : ICliAction
    {
        TraceSource log = Log.CreateSource("pack");

        [CommandLineArgument("template-archive")]
        public string TemplateFile { get; set; } = "Packages/Python/OpenTap.Python.ProjectTemplate.zip";

        [CommandLineArgument("directory")] public string OutDir { get; set; }

        [CommandLineArgument("project-name")] public string ProjectName { get; set; }

        public int Execute(CancellationToken cancellationToken)
        {
            using var fstr = File.OpenRead(TemplateFile);
            using var archive = new ZipArchive(fstr, ZipArchiveMode.Read);

            if (File.Exists(OutDir)) throw new ArgumentException("Directory does already exist: " + OutDir);
            var cd = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(OutDir);
            Directory.SetCurrentDirectory(OutDir);
            try
            {
                foreach (var item in archive.Entries)
                {
                    var outName = item.FullName.Replace("OpenTap.Python.ProjectTemplate", ProjectName);
                    var dirname = Path.GetDirectoryName(outName);

                    if (!string.IsNullOrWhiteSpace(dirname) && !Directory.Exists(dirname))
                        Directory.CreateDirectory(dirname);
                    string content = null;

                    using (var reader = item.Open())
                        content = new StreamReader(reader).ReadToEnd();
                    content = content.Replace("OpenTap.Python.ProjectTemplate", ProjectName);
                    log.Debug("Writing: {0}", outName);
                    File.WriteAllText(outName, content);

                }
            }
            finally
            {
                Directory.SetCurrentDirectory(cd);
            }

            return 0;

        }
    }
}