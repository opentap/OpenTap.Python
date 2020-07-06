using System;
using System.IO;
using OpenTap;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;

namespace Keysight.OpenTap.Plugins.Python.ProjectGenerator
{
    public class Project
    {
        [Browsable(false)]
        public string Name { get; } = "Project Information.";

        [Display("Name")]
        public string ProjName { get; set; }

        [Display("Output Directory")]
        public string OutputDir { get; set; } = Directory.GetCurrentDirectory();

        [Display("Version")]
        public string Version { get; set; } = "1.0.0";

        [Display("Description")]
        [Submit]
        public string Description { get; set; } = "This is a python-based plugin for OpenTAP.";

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"\tName: {ProjName}");
            sb.AppendLine($"\tDirectory: {Path.GetFullPath(Path.Combine(OutputDir, ProjName))}");
            sb.AppendLine($"\tVersion: {Version}");
            sb.AppendLine($"\tDescription: {Description}");
            return sb.ToString();
        }
    }

    public class Plugin
    {
        [Browsable(false)]
        public string Name { get; } = "Types of OpenTAP Plugin.";

        [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)]
        public PluginEnum PluginType { get; set; }

        [Display("Plugin Name")]
        [Submit]
        public string pluginName { get; set; }
    }

    [Display(Name: "python-project", Description: "Generate a project of the python-based OpenTAP plugins.", Groups: new[] { "sdk", "new" })]
    public class ProjectGenerator : BaseGenerator
    {
        public override int Execute(CancellationToken cancellationToken)
        {
            var project = new Project();
            bool isNameOK;
            bool isVersionOK;
            do
            {
                UserInput.Request(project, true);
                isNameOK = NameValidator(project.ProjName);
                isVersionOK = VersionValidator(project.Version);
            } while (!isNameOK || !isVersionOK);

            string projectDir = Path.GetFullPath(Path.Combine(project.OutputDir, project.ProjName));
            if (Directory.Exists(projectDir))
            {
                log.Error($"Project directory already exists: '{projectDir}'.");
                if (!GetBoolInput("Do you want to override the existing project directory?\nAll existing sub-folders and files will be deleted."))
                {
                    log.Info("Please try again with new project name or new output directory.");
                    return 1;
                }
                else
                {
                    Directory.Delete(projectDir, true);
                    log.Info("Deleted existing project directory.");
                }
            }

            log.Info("A new python project with the following details will be generated.\n");
            log.Info(project.ToString());
            log.Flush();

            if (!GetBoolInput("Do you want to proceed to generate the python project?"))
            {
                log.Info("Aborted project generation.");
                return 1;
            }
            log.Info("Start to generate the python project.");

            Directory.CreateDirectory(projectDir);

            // add the output dir into the python search path
            var currentPluginSearchPath = PythonSettings.Current.SearchPathList;
            if (!currentPluginSearchPath.Exists(x => string.Compare(x.SearchPath, project.OutputDir) == 0) && string.Compare(project.OutputDir, Path.GetDirectoryName(typeof(ProjectGenerator).Assembly.Location)) != 0)
            {
                currentPluginSearchPath.Add(new PluginSearchPath() { SearchPath = project.OutputDir });
                PythonSettings.Current.Save();
            }

            try
            {
                WriteFile(Path.Combine(projectDir, "project.txt"), project.ToString());
                WriteFile(Path.Combine(projectDir, initFile), "import sys");
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }

            if (GetBoolInput("Do you want to generate plugin template now?\nYou may generate plugin template any time after the project generation."))
            {
                do
                {
                    var plugin = new Plugin();
                    do
                    {
                        UserInput.Request(plugin, true);
                    } while (!NameValidator(plugin.pluginName));

                    new PluginGenerator()
                    {
                        Name = plugin.pluginName,
                        Output = projectDir,
                        PluginType = plugin.PluginType
                    }.Execute(cancellationToken);
                } while (GetBoolInput("Generate one more?"));

            }

            log.Info($"Generated python project: '{project.ProjName}'");
            log.Flush();
            return 0;
        }
    }
}
