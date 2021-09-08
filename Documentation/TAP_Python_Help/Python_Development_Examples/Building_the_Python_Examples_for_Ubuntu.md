# Building the Python Examples for Ubuntu

Prior to successfully building the Python Examples, please make sure the steps in [Python Development Setup for Ubuntu](../Python_Development_Setup_for_Ubuntu.md) is executed.

Follow these steps to build the examples:

1. Open the Terminal and navigate to your Python plugin folder (%TAP_PATH%).

2. Setup Python path

    For **Python 2.7**: `~/.tap/tap python set-path <Python installation path> 2.7` (refering to [Python Development Setup for Ubuntu](../Python_Development_Setup_for_Ubuntu.md), the installation path is **/usr/local/lib/pythonplugin**)

	For **Python 3.6**: `~/.tap/tap python set-path <Python installation path> 3.6` (refering to [Python Development Setup for Ubuntu](../Python_Development_Setup_for_Ubuntu.md), the installation path is **/usr/local/lib/pythonplugin**)
    
    For **Python 3.7**: `~/.tap/tap python set-path <Python installation path> 3.7` (refering to [Python Development Setup for Ubuntu](../Python_Development_Setup_for_Ubuntu.md), the installation path is **/usr/local/lib/pythonplugin**)
    
3. Build the wrapper for the example modules. Execute:

    `~/.tap/tap python build PluginExample`

    ![](../Images/Python_exe_Ubuntu.png) 

You can [Create and Run a Simple Test Plan](./Create_and_Run_a_Simple_Test_Plan_for_Ubuntu.md) using these steps and resources to become more familiar with OpenTAP.
