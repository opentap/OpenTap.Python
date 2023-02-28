using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using OpenTap.Cli;
using OpenTap.Package;

namespace OpenTap.Python.CliActions
{
    [Display(Name: "new-project", Description: "Starts a new OpenTAP/Python project.",
        Groups: new[] { "python" })]
    public class NewProject : ICliAction
    {
        static readonly TraceSource log = Log.CreateSource("pack");

        [Browsable(false)]
        [CommandLineArgument("template-archive", Description = "The template archive from which to pull the templates.")]
        public string TemplateFile { get; set; } = Path.Combine(Installation.Current?.Directory ?? ".", "Packages/Python/OpenTap.Python.ProjectTemplate.zip");

        [CommandLineArgument("directory", Description = "The directory where to put the new project.")] 
        public string Directory { get; set; }

        [CommandLineArgument("project-name",  Description = "The name of the newly create project.")] 
        public string ProjectName { get; set; }

        public int Execute(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ProjectName))
                throw new ArgumentException("The project name (--project-name) must be set.", nameof(ProjectName));
            if (string.IsNullOrEmpty(Directory))
                throw new ArgumentException("The output directory (--directory) must be set.", nameof(Directory));
            
            using var fstr = File.OpenRead(TemplateFile);
            using var archive = new ZipArchive(fstr, ZipArchiveMode.Read);

            if (File.Exists(Directory)) throw new ArgumentException("Directory does already exist: " + Directory);
            var cd = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.CreateDirectory(Directory);
            System.IO.Directory.SetCurrentDirectory(Directory);
            try
            {
                foreach (var item in archive.Entries)
                {
                    var outName = item.FullName.Replace("OpenTap.Python.ProjectTemplate", ProjectName);
                    var dirname = Path.GetDirectoryName(outName);

                    if (!string.IsNullOrWhiteSpace(dirname) && !System.IO.Directory.Exists(dirname))
                        System.IO.Directory.CreateDirectory(dirname);
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
                System.IO.Directory.SetCurrentDirectory(cd);
            }
            
            log.Info("Project '{0}' created at '{1}'.", ProjectName, Directory);

            return 0;
        }
    }
}