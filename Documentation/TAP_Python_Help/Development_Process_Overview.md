# Python Development Process Overview

Creating a plugin with Python involves five basic steps:

1. Write the code to create one or more Python modules that contain TestSteps, Instruments, DUTs, ResultListeners, and/or ComponentSettings.

2. Edit the `__init__.py` file to import the modules.

3. Start OpenTAP and create a test plan using your new test steps and/or resources. Edit your Python modules and rebuild your plugin until development is complete.

4. Build the .TapPackage package, which allows the plugin to be distributed.

For details on these steps, see [Creating an OpenTAP Plugin with Python for Windows](./Creating_a_plugin_with_Python_for_Windows.md).
