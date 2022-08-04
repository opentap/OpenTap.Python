using System;
using System.IO;
using System.Threading;
using System.Linq;
using OpenTap.Cli;

namespace OpenTap.Python.CliActions
{
    //[Display(Name: "new-project", Description: "Generate a project of the python-based OpenTAP plugins.", Groups: new[] { "python" })]
    class NewProjectAction : ICliAction
    {
        [UnnamedCommandLineArgument("project-name")]
        public string ProjectName { get; set; }

        private TraceSource log = Log.CreateSource("python");
        public int Execute(CancellationToken cancellationToken)
        {
            var projectName = Path.GetFileName(ProjectName);
            var directory = Path.GetDirectoryName(ProjectName);
            if (!Directory.Exists(ProjectName))
                Directory.CreateDirectory(ProjectName);
            File.WriteAllText(Path.Combine(ProjectName, "requirements.txt"), "");
            
            // add the output dir into the python search path
            var searchPaths = PythonSettings.Current.SearchPathList;
            if (searchPaths.Any(x => x.SearchPath == directory))
            {
                searchPaths.Add(new PluginSearchPath(){SearchPath = directory});
            }
            
            try
            {
                File.WriteAllText(Path.Combine(ProjectName, "package.xml"), "");
                File.WriteAllText(Path.Combine(ProjectName, "step1.py"), "");
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }
            
            return 0;
        }
    }
}
