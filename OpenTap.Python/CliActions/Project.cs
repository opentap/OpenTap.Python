using System.ComponentModel;
using System.IO;
using System.Text;

namespace OpenTap.Python.ProjectGenerator;

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