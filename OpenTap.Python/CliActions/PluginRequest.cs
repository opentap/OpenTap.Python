using System.ComponentModel;

namespace OpenTap.Python.ProjectGenerator;

[Display("Plugin")]
class PluginRequest
{
    [Browsable(false)]
    public string Name { get; } = "Types of OpenTAP Plugin.";

    [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)]
    public PluginEnum PluginType { get; set; }

    [Display("Plugin Name")]
    [Submit]
    public string pluginName { get; set; }
}