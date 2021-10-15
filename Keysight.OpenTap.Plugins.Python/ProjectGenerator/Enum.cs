using OpenTap;

namespace Keysight.OpenTap.Plugins.Python.ProjectGenerator
{

    public enum BoolEnum
    {
        No,
        Yes
    }

    public enum PluginEnum
    {
        [Display("Step")]
        Step,
        [Display("DUT")]
        Dut,
        [Display("Instrument")]
        Instrument,
        [Display("Result Listener")]
        Result_Listener,
        [Display("Component Setting")]
        Component_Setting
    }

    public enum FileExtensionEnum
    {
        py,
        txt
    }

}