
namespace OpenTap.Python.ProjectGenerator
{

    public enum BoolEnum
    {
        No,
        Yes
    }

    enum PluginEnum
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

    enum FileExtensionEnum
    {
        py,
        txt
    }

}