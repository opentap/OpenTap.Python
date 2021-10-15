# Building the Python Examples for Ubuntu

Prior to successfully building the Python Examples, please make sure the steps in [Python Development Setup for Ubuntu](../Python_Development_Setup_for_Ubuntu.md) is executed.

Follow these steps to build the examples:

1. Open the Terminal and navigate to your Python plugin folder (%TAP_PATH%).

2. Set Python version

    For **Python 2.7**: `~/.tap/tap python set-version 2.7`

    For **Python 3.6**: `~/.tap/tap python set-version 3.6`

    For **Python 3.7**: `~/.tap/tap python set-version 3.7`
    
    For **Python 3.8**: `~/.tap/tap python set-version 3.8`
    
3. Build the wrapper for the example modules. Execute:

    `~/.tap/tap python build PluginExample`

    ![](../Images/Python_exe_Ubuntu.png) 

You can [Create and Run a Simple Test Plan](./Create_and_Run_a_Simple_Test_Plan_for_Ubuntu.md) using these steps and resources to become more familiar with OpenTAP.