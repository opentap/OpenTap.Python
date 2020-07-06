using OpenTap;
using OpenTap.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Keysight.OpenTap.Plugins.Python.ProjectGenerator
{
    public class PluginGenerator : BaseGenerator
    {
        [UnnamedCommandLineArgument("plugin name", Required = true)]
        public virtual string Name { get; set; }

        [CommandLineArgument("out", ShortName = "o", Description = "Output directory of the generated file.")]
        public virtual string Output { get; set; } = Directory.GetCurrentDirectory();

        public virtual PluginEnum PluginType { get; set; }

        public override int Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (!NameValidator(Name))
                    return 1;

                if (!File.Exists(Path.Combine(Output, initFile)))
                {
                    log.Error($"'{Output}' is not a project directory for the OpenTAP python plugins.");
                    return 1;
                }

                GeneratePlugin();
                return 0;
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }
        }

        public virtual void GeneratePlugin()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{resources}.{PluginType}.{FileExtensionEnum.txt}")))
            {
                string content = reader.ReadToEnd().Replace("ACTUAL_PLUGIN_NAME", Name);
                WriteFile(Path.Combine(Output, $"{Name}.{FileExtensionEnum.py}"), content);
                File.AppendAllText(Path.Combine(Output, initFile), $"\nfrom .{Name} import *");
            }
        }
    }

    [Display("python-instrument", "Generate a template of the python-based instrument plugin for OpenTAP.", Groups: new[] { "sdk", "new" })]
    public class InstrumentGenerator : PluginGenerator
    {
        public InstrumentGenerator()
        {
            PluginType = PluginEnum.Instrument;
        }
    }

    [Display("python-dut", "Generate a template of the python-based dut plugin for OpenTAP.", Groups: new[] { "sdk", "new" })]
    public class DutGenerator : PluginGenerator
    {
        public DutGenerator()
        {
            PluginType = PluginEnum.Dut;
        }
    }

    [Display("python-step", "Generate a template of the python-based step plugin for OpenTAP.", Groups: new[] { "sdk", "new" })]
    public class StepGenerator : PluginGenerator
    {
        public StepGenerator()
        {
            PluginType = PluginEnum.Step;
        }
    }

    [Display("python-result-listener", "Generate a template of the python-based result listener plugin for OpenTAP.", Groups: new[] { "sdk", "new" })]
    public class ResultListenerGenerator : PluginGenerator
    {
        public ResultListenerGenerator()
        {
            PluginType = PluginEnum.Result_Listener;
        }
    }

    [Display("python-component-setting", "Generate a template of the python-based component setting plugin for OpenTAP.", Groups: new[] { "sdk", "new" })]
    public class ComponentSettingGenerator : PluginGenerator
    {
        public ComponentSettingGenerator()
        {
            PluginType = PluginEnum.Component_Setting;
        }
    }
}
